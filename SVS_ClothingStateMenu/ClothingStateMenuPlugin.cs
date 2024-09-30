#define KK
#define KKS
#define EC

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Character;
using ClothingStateMenu.Utils;
using IllusionMods;
using MonoMod.RuntimeDetour;
using UnityEngine;


// todo it needs a rewrite to use in svs and hc
namespace ClothingStateMenu
{
    [BepInPlugin(GUID, "Clothing State Menu", Version)]
    [BepInProcess(GameUtilities.GameProcessName)]
    public class ClothingStateMenuPlugin : BasePlugin
    {
        public const string Version = "4.0.1";
        public const string GUID = "ClothingStateMenu";

        internal static ConfigEntry<KeyboardShortcut> Keybind { get; set; }
        internal static ManualLogSource Logger { get; private set; }

        private static ClothingStateMenuPlugin Instance { get; set; }

        public override void Load()
        {
            Logger = Log;
            Instance = this;

            // todo remove whenever bepinex il2cpp gets it
            var colorConverter = new TypeConverter
            {
                ConvertToString = (obj, type) => ColorUtility.ToHtmlStringRGBA((Color)obj),
                ConvertToObject = (str, type) =>
                {
                    if (!ColorUtility.TryParseHtmlString("#" + str.Trim('#', ' '), out var c))
                        throw new FormatException("Invalid color string, expected hex #RRGGBBAA");
                    return c;
                }
            };
            TomlTypeConverter.AddConverter(typeof(Color), colorConverter);

            BackgroundColor = Config.Bind("General", "Background Color", new Color(0, 0, 0, 0.5f), "Tint of the background color of the clothing state menu (subtle change). When mouse cursor hovers over the menu, transparency is forced to 1.");

            RetainStatesBetweenOutfits = Config.Bind("Options", "Retain Acc States Between Outfits", false, "Acc slots toggled off in one outfit will remain toggled off in others.\nIf disabled, the accs sync up to the vanilla buttons on outfit change.");

            Keybind = Config.Bind("General", "Toggle clothing state menu", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift), "Keyboard shortcut to toggle the clothing state menu on and off.\nCan be used outside of character maker in some cases - works for males in H scenes (the male has to be visible for the menu to appear) and in some conversations with girls.");

            AddComponent<ClothingStateMenuComponent>();
        }

        private sealed class ClothingStateMenuComponent : MonoBehaviour
        {
            private void Update()
            {
                Instance.Update();
            }

            private void OnGUI()
            {
                Instance.OnGUI();
            }
            /*
               CharacterCreation.HumanCustom.Instance.Human.cloth.fileStatus.clothesState
               if (InsideMaker)
                   return CharacterCreation.HumanCustom.Instance.SetClothesVisible(ChaFileDefine.ClothesKind.top, ) then updatestate
    */
        }

        private const float Margin = 5f;
        private const float WindowWidth = 125f;

        private static readonly GUILayoutOption[] _NoLayoutOptions = Array.Empty<GUILayoutOption>();
        private static readonly List<GUIContent[][]> _AccessoryButtonContentCache = new();

        private readonly List<IStateToggleButton> _buttons = new();
        private const int CoordCount = 3; //todo not hardcoded
        private readonly CoordButton[] _coordButtons = new CoordButton[CoordCount];

        private Rect _windowRect;
        private Vector2 _accessorySlotsScrollPos = Vector2.zero;

        private Human[] _visibleCharas = Array.Empty<Human>();
        private Human _selectedChara;
        private ImguiComboBox _charaDropdown = new();
        private GUIContent[] _visibleCharasContents = Array.Empty<GUIContent>();
        private GUIContent _selectedCharaContent;

        private ConfigEntry<Color> BackgroundColor { get; set; }
        private float _currentBackgroundAlpha;
        private ConfigEntry<bool> RetainStatesBetweenOutfits { get; set; }

        private int _coordMemory = -1;

        private bool _showInterface;
        private bool ShowInterface
        {
            get
            {
                // Calculate only once in Update since this gets called many times a frame in OnGUI
                return _showInterface;
            }
            set
            {
                // todo open if any chara is there, have a combobox with all acceptable charas

                if (!value)
                {
                    _showInterface = false;
                    return;
                }

                RefreshCharacterList();

                _showInterface = _selectedChara != null;


                // if (MakerAPI.InsideMaker)
                // {
                //     ShowInMaker.Value = value;
                //     if (_sidebarToggle != null) _sidebarToggle.Value = value;
                // }
                // else
                //     _showOutsideMaker = value;
                //
                // _chaCtrl = null;
                // _buttons.Clear();
                //
                // if (!value) return;
                //
                // FindTargetCharacter();
                //
                // if (_chaCtrl == null)
                // {
                //     _showOutsideMaker = false;
                //     return;
                // }
                //
                // // Don't try until maker is fully loaded
                // if (!MakerAPI.InsideMaker || MakerAPI.InsideAndLoaded)
                //     SetupInterface();
            }
        }

        private bool RefreshCharacterList()
        {
            _visibleCharas = GameUtilities.GetCurrentHumans(false).ToArray();
            if (_selectedChara == null || !_visibleCharas.Contains(_selectedChara))
                _selectedChara = _visibleCharas.FirstOrDefault();

            _visibleCharasContents = _visibleCharas.Select(x => new GUIContent(x.fileParam.GetCharaName(true))).ToArray();
            
            var anyChara = _selectedChara != null;
            if (anyChara)
            {
                _selectedCharaContent = _visibleCharasContents[Array.IndexOf(_visibleCharas, _selectedChara)];
                SetupInterface();
            }
            else
            {
                _selectedCharaContent = null;
                _showInterface = false;
            }
            return anyChara;
        }


        private void Update()
        {
            if (Keybind.Value.IsDown())
                ShowInterface = !ShowInterface;

            //todo check for charas here? only if chara was destroyed and then trigger chara list refresh?
            //_showInterface = CanShow();

            if (_showInterface)
            {
                if (!_selectedChara?.transform)
                {
                    if(!RefreshCharacterList())
                        return;
                }

                var mouseHover = _windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
                _currentBackgroundAlpha = Mathf.Lerp(_currentBackgroundAlpha, mouseHover ? 1f : BackgroundColor.Value.a, Time.deltaTime * 10f);
            }
            else
            {
                _currentBackgroundAlpha = 0;
            }

            // todo necessary?
            //if (GameUtilities.InsideMaker && _chaCtrl != null)
            //{
            //    if (!RetainStatesBetweenOutfits.Value && _coordMemory != _chaCtrl.fileStatus.coordinateType)
            //    {
            //        var drawCtrl = CharacterCreation.HumanCustom.Instance.customCtrl.cmpDrawCtrl;
            //        var toggleMain = drawCtrl.tglShowAccessory[0];
            //        var toggleSub = drawCtrl.tglShowAccessory[1];
            //        toggleMain.onValueChanged.Invoke(toggleMain.isOn);
            //        toggleSub.onValueChanged.Invoke(toggleSub.isOn);
            //    }
            //    _coordMemory = _chaCtrl.fileStatus.coordinateType;
            //}
        }

        private void OnGUI()
        {
            if (!ShowInterface)
                return;

            var backgroundColor = BackgroundColor.Value;
            backgroundColor.a = _currentBackgroundAlpha;

            GUI.backgroundColor = backgroundColor;

            // To allow mouse draging the skin has to have solid background, box works fine
            var style = _currentBackgroundAlpha == 0 ? GUI.skin.label : GUI.skin.box;
            _windowRect = GUILayout.Window(90876322, _windowRect, (GUI.WindowFunction)WindowFunc, GUIContent.none, style, _NoLayoutOptions);

            void WindowFunc(int id)
            {
                GUI.backgroundColor = backgroundColor;

                _charaDropdown.Show(_selectedCharaContent, () => _visibleCharasContents, i =>
                {
                    _selectedChara = _visibleCharas[i];
                    RefreshCharacterList();
                }, (int)_windowRect.yMax);

                foreach (var clothButton in _buttons)
                {
                    if (clothButton == null)
                    {
                        GUILayout.Space(7);
                    }
                    else
                    {
                        if (GUILayout.Button(clothButton.Content, _NoLayoutOptions))
                            clothButton.OnClick();
                        GUILayout.Space(-5);
                    }
                }
                GUILayout.Space(5);

                var showAccessory = _selectedChara.fileStatus.showAccessory;

                _accessorySlotsScrollPos = GUILayout.BeginScrollView(_accessorySlotsScrollPos, _NoLayoutOptions);
                {
                    // Not worthwhile to virtualize, far too few items
                    for (var j = 0; j < showAccessory.Length; j++)
                    {
                        if (_selectedChara.cloth.nowCoordinate.Accessory.parts[j].type != 120)
                            DrawAccesoryButton(j, showAccessory[j]);
                    }
                }
                GUILayout.EndScrollView();

                var w = _windowRect.width;
                _windowRect = IMGUIUtils.DragResizeEat(id, _windowRect);
                // Only resize width
                _windowRect.width = w;

                _charaDropdown.DrawDropdownIfOpen();
            }

            //if (!MakerAPI.InsideMaker || ShowCoordinateButtons.Value)
            if (!GameUtilities.InsideMaker)
            {
                for (var i = 0; i < CoordCount; i++)
                {
                    var btn = _coordButtons[i];
                    if (GUI.Button(new Rect(btn.Position.x + _windowRect.x, btn.Position.y + _windowRect.y, btn.Position.width, btn.Position.height), btn.Content))
                        btn.OnClick();
                }
            }
        }

        private void DrawAccesoryButton(int accIndex, bool isOn)
        {
            // Populate the cache with enough entries to cover the current index
            // Major speedup over creating the content every frame (using string still creates new GUIContent internally)
            while (_AccessoryButtonContentCache.Count <= accIndex)
            {
                var index = (_AccessoryButtonContentCache.Count + 1).ToString();
                _AccessoryButtonContentCache.Add(new GUIContent[][] // on/off
                {
                    new GUIContent[] // show/hide in h scene
                    {
                        new($"S Slot {index}: On"),
                        new($"H Slot {index}: On"),
                        new($"Slot {index}: On"),
                    },
                    new GUIContent[]
                    {
                        new($"S Slot {index}: Off"),
                        new($"H Slot {index}: Off"),
                        new($"Slot {index}: Off"),
                    }
                });
            }

            //var accTypeIndex = ShowMainSub.Value ? (_chaCtrl.cloth.nowCoordinate.Accessory.parts[accIndex].hideCategory) : 2;
            var accTypeIndex = _selectedChara.cloth.nowCoordinate.Accessory.parts[accIndex].hideCategory;

            var acc = _AccessoryButtonContentCache[accIndex][isOn ? 0 : 1][accTypeIndex];
            if (GUILayout.Button(acc, _NoLayoutOptions))
            {
                _selectedChara.acs.SetAccessoryState(accIndex, !isOn);
                //todo needed? _chaCtrl.acs.UpdateVisible();
            }

            GUILayout.Space(-5);
        }

        private bool _lastCatShown = true;
        private bool _lastCatHidden = true;

        private void SetupInterface()
        {
            const float coordWidth = 25f;
            const float coordHeight = 20f;

            _buttons.Clear();

            // Let the window auto-size and keep the position while outside maker
            _windowRect = new Rect(x: _windowRect.x != 0 ? _windowRect.x : Margin + coordWidth,
                                   y: _windowRect.y != 0 ? _windowRect.y : Screen.height - Margin - 400,
                                   width: WindowWidth,
                                   height: 200);

            // Clothing piece state buttons
            foreach (ChaFileDefine.ClothesKind kind in Enum.GetValues(typeof(ChaFileDefine.ClothesKind)))
            {
                _buttons.Add(new ClothButton(kind, _selectedChara));
            }
            // Invisible body
            _buttons.Add(new BodyButton(_selectedChara));


            _buttons.Add(null);
            _buttons.Add(new ActionButton("All accs On", () => _selectedChara.acs.SetAccessoryStateAll(true)));
            _buttons.Add(new ActionButton("All accs Off", () => _selectedChara.acs.SetAccessoryStateAll(false)));

            _buttons.Add(null);

            //_chaCtrl.cloth.nowCoordinate.Accessory.parts.FirstOrDefault(x=>x.type != 120 && x.hideCategory == 0 && x.)));
            _buttons.Add(new ActionButton("Shown in H", () => _selectedChara.acs.SetAccessoryStateCategory(0, _lastCatShown = !_lastCatShown)));
            _buttons.Add(new ActionButton("Hidden in H", () => _selectedChara.acs.SetAccessoryStateCategory(1, _lastCatHidden = !_lastCatHidden)));

            // Coordinate change buttons
            Action<int> setCoordAction;
            //todo
            //var customControl = MakerAPI.GetMakerBase()?.customCtrl;
            //if (customControl != null)
            //{
            //    var coordDropdown = customControl.ddCoordinate;
            //    setCoordAction = newVal => coordDropdown.value = newVal;
            //}
            //else
            //{
            //    setCoordAction = newVal => _chaCtrl.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType)newVal);
            //}
            setCoordAction = newVal =>
            {
                _selectedChara.coorde.SetNowCoordinate(_selectedChara.data.Coordinates[newVal]);
                _selectedChara.ReloadCoordinate();
            };

            for (var i = 0; i < CoordCount; i++)
            {
                var position = new Rect(x: -coordWidth,
                                        y: 4 + coordHeight * i,
                                        width: coordWidth,
                                        height: coordHeight);
                _coordButtons[i] = new CoordButton(i, setCoordAction, position);
            }
        }

        /*#if KK || KKS
                private static void ToggleAccButtons(bool state)
                {
                    Transform root = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl.tglShowAccessory[0].transform.parent.parent;
                    for (int i = 0; i < root.childCount; i++)
                    {
                        if (root.GetChild(i).gameObject.name == "txtAccessory")
                        {
                            root.GetChild(i + 0).gameObject.SetActive(state);
                            root.GetChild(i + 1).gameObject.SetActive(state);
                            root.GetChild(i + 2).gameObject.SetActive(state);
                            break;
                        }
                    }
                }

                private static void RegisterToggleEvents()
                {
                    var drawCtrl = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl;
                    var toggleMain = drawCtrl.tglShowAccessory[0];
                    var toggleSub = drawCtrl.tglShowAccessory[1];
                    toggleMain.onValueChanged.AddListener(x => drawCtrl.chaCtrl.SetAccessoryStateCategory(0, toggleMain.isOn));
                    toggleSub.onValueChanged.AddListener(x => drawCtrl.chaCtrl.SetAccessoryStateCategory(0, toggleMain.isOn));
                }
        #endif
        #if KK
                private static void ToggleShoeButtons(bool state)
                {
                    Transform root = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl.tglShoesType[0].transform.parent.parent;
                    for (int i = 0; i < root.childCount; i++)
                    {
                        if (root.GetChild(i).gameObject.name == "txtShoes")
                        {
                            root.GetChild(i + 0).gameObject.SetActive(state);
                            root.GetChild(i + 1).gameObject.SetActive(state);
                            root.GetChild(i + 2).gameObject.SetActive(state);
                            break;
                        }
                    }
                }
        #endif*/
    }
}