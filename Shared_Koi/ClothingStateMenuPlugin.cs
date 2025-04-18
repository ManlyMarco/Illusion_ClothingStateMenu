using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Utilities;
using UniRx;
using UnityEngine;

namespace ClothingStateMenu
{
    public partial class ClothingStateMenuPlugin : BaseUnityPlugin
    {
        public const string Version = Constants.Version;
        public const string GUID = Constants.GUID;

        private const float Margin = 5f;
        private const float WindowWidth = 125f;

        private static readonly GUILayoutOption[] _NoLayoutOptions = new GUILayoutOption[0];
        private static readonly List<GUIContent[][]> _AccessoryButtonContentCache = new List<GUIContent[][]>();

        private readonly List<IStateToggleButton> _buttons = new List<IStateToggleButton>();
        private readonly List<CoordButton> _coordButtons = new List<CoordButton>();

        private Rect _windowRect;
        private Vector2 _accessorySlotsScrollPos = Vector2.zero;

        private ChaControl _chaCtrl;
        private SidebarToggle _sidebarToggle;

        private bool _showOutsideMaker;

        private ConfigEntry<bool> ShowInMaker { get; set; }
        private ConfigEntry<Color> BackgroundColor { get; set; }
        private float _currentBackgroundAlpha;
#if KK
        private ConfigEntry<bool> MoveShoeButtons { get; set; }
#endif
#if KK || KKS
        private ConfigEntry<bool> ShowCoordinateButtons { get; set; }
        private ConfigEntry<bool> RetainStatesBetweenOutfits { get; set; }
        private ConfigEntry<bool> MoveVanillaButtons { get; set; }
        private ConfigEntry<bool> ShowMainSub { get; set; }

        private int _coordMemory = -1;
#endif
        private ConfigEntry<KeyboardShortcut> Keybind { get; set; }

        private void Start()
        {
            Hooks.Init();

            ShowInMaker = Config.Bind("General", "Show in Character Maker", false, "Show the clothing state menu in character maker. Can be enabled from maker interface or by pressing the keyboard shortcut.");
            ShowInMaker.SettingChanged += (sender, args) =>
            {
                if (MakerAPI.InsideMaker)
                    ShowInterface = ShowInMaker.Value;
            };

            BackgroundColor = Config.Bind("General", "Background Color", new Color(0, 0, 0, 0.5f), "Tint of the background color of the clothing state menu (subtle change). When mouse cursor hovers over the menu, transparency is forced to 1.");
#if KK
            MoveShoeButtons = Config.Bind("Options", "Move Shoe Type Buttons", false, "Move the vanilla shoe type buttons from the sidebar to the plugin menu.");
            MoveShoeButtons.SettingChanged += (sender, args) =>
            {
                if (MakerAPI.InsideAndLoaded)
                {
                    ToggleShoeButtons(!MoveShoeButtons.Value);
                    SetupInterface();
                }
            };
#endif
#if KK || KKS
            ShowMainSub = Config.Bind("Options", "Show Main/Sub acc type in list", true, "Show in the toggle list whether an accessory's category is Main (M) or Sub (S).");
            MoveVanillaButtons = Config.Bind("Options", "Move Vanilla Acc Buttons", false, "Move the vanilla \"Main\" and \"Sub\" accessory toggle buttons from the sidebar to the plugin menu.");
            MoveVanillaButtons.SettingChanged += (sender, args) =>
            {
                if (MakerAPI.InsideAndLoaded)
                {
                    ToggleAccButtons(!MoveVanillaButtons.Value);
                    SetupInterface();
                }
            };
            RetainStatesBetweenOutfits = Config.Bind("Options", "Retain Acc States Between Outfits", false, "Acc slots toggled off in one outfit will remain toggled off in others.\nIf disabled, the accs sync up to the vanilla buttons on outfit change.");

            ShowCoordinateButtons = Config.Bind("Options", "Show coordinate change buttons in Character Maker", false, "Adds buttons to the menu that allow quickly switching between clothing sets. Same as using the clothing dropdown.\nThe buttons are always shown outside of character maker.");
#endif

            Keybind = Config.Bind("General", "Toggle clothing state menu", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift), "Keyboard shortcut to toggle the clothing state menu on and off.\nCan be used outside of character maker in some cases - works for males in H scenes (the male has to be visible for the menu to appear) and in some conversations with girls.");

            MakerAPI.RegisterCustomSubCategories += (sender, e) =>
            {
                _sidebarToggle = e.AddSidebarControl(new SidebarToggle("Show clothing state menu", ShowInMaker.Value, this));
                _sidebarToggle.ValueChanged.Subscribe(b => ShowInterface = b);
            };
            MakerAPI.MakerFinishedLoading += (sender, args) =>
            {
#if KK || KKS
                RegisterToggleEvents();
                if (MoveVanillaButtons.Value)
                    ToggleAccButtons(false);
#endif
#if KK
                if (MoveShoeButtons.Value)
                    ToggleShoeButtons(false);
#endif
                if (ShowInMaker.Value)
                    SetupInterface();
            };
            MakerAPI.MakerExiting += (sender, e) =>
            {
                _chaCtrl = null;
                _sidebarToggle = null;
            };
        }

        private bool _cachedShowInterface;
        private bool ShowInterface
        {
            get
            {
                // Calculate only once in Update since this gets called many times a frame in OnGUI
                return _cachedShowInterface;
            }
            set
            {
                if (MakerAPI.InsideMaker)
                {
                    ShowInMaker.Value = value;
                    if (_sidebarToggle != null) _sidebarToggle.Value = value;
                }
                else
                    _showOutsideMaker = value;

                _chaCtrl = null;
                _buttons.Clear();

                if (!value) return;

                FindTargetCharacter();

                if (_chaCtrl == null)
                {
                    _showOutsideMaker = false;
                    return;
                }

                // Don't try until maker is fully loaded
                if (!MakerAPI.InsideMaker || MakerAPI.InsideAndLoaded)
                    SetupInterface();
            }
        }

        private bool CanShow()
        {
            if (MakerAPI.InsideMaker)
            {
                if (!ShowInMaker.Value || !MakerAPI.InsideAndLoaded)
                    return false;
            }
            else
            {
                if (!_showOutsideMaker)
                    return false;
                if (_chaCtrl == null)
                {
                    ShowInterface = false;
                    return false;
                }
            }

            if (_chaCtrl == null) return false;
            if (!_chaCtrl.visibleAll) return false;

            if (MakerAPI.InsideMaker && !MakerAPI.IsInterfaceVisible()) return false;

            if (SceneApi.GetAddSceneName() == "Config") return false;
            if (SceneApi.GetIsOverlap()) return false;
            if (SceneApi.GetIsNowLoadingFade()) return false;

            return true;
        }

        private void Update()
        {
            if (Keybind.Value.IsDown())
                ShowInterface = !ShowInterface;

            _cachedShowInterface = CanShow();

            if (_cachedShowInterface)
            {
                var mouseHover = _windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
                _currentBackgroundAlpha = Mathf.Lerp(_currentBackgroundAlpha, mouseHover ? 1f : BackgroundColor.Value.a, Time.deltaTime * 10f);
            }
            else
            {
                _currentBackgroundAlpha = 0;
            }

#if KK || KKS
            if (MakerAPI.InsideMaker && _chaCtrl != null)
            {
                if (!RetainStatesBetweenOutfits.Value && _coordMemory != _chaCtrl.fileStatus.coordinateType)
                {
                    var drawCtrl = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl;
                    var toggleMain = drawCtrl.tglShowAccessory[0];
                    var toggleSub = drawCtrl.tglShowAccessory[1];
                    toggleMain.onValueChanged.Invoke(toggleMain.isOn);
                    toggleSub.onValueChanged.Invoke(toggleSub.isOn);
                }
                _coordMemory = _chaCtrl.fileStatus.coordinateType;
            }
#endif
        }

        private void OnGUI()
        {
            if (!ShowInterface)
                return;

            var backgroundColor = BackgroundColor.Value;
            backgroundColor.a = _currentBackgroundAlpha;

            GUI.backgroundColor = backgroundColor;

            // To allow mouse draging the skin has to have solid background, box works fine. No dragging in maker.
            var style = MakerAPI.InsideMaker || _currentBackgroundAlpha == 0 ? GUI.skin.label : GUI.skin.box;
            _windowRect = GUILayout.Window(90876322, _windowRect, WindowFunc, GUIContent.none, style, _NoLayoutOptions);

            void WindowFunc(int id)
            {
                GUI.backgroundColor = backgroundColor;

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

                var showAccessory = _chaCtrl.fileStatus.showAccessory;

                _accessorySlotsScrollPos = GUILayout.BeginScrollView(_accessorySlotsScrollPos, _NoLayoutOptions);
                {
                    // Not worthwhile to virtualize, far too few items
                    for (var j = 0; j < showAccessory.Length; j++)
                    {
                        if (_chaCtrl.nowCoordinate.accessory.parts[j].type != 120)
                            DrawAccesoryButton(j, showAccessory[j]);
                    }
                }
                GUILayout.EndScrollView();

                // Keep pinned in maker, but allow dragging outside.
                if (!MakerAPI.InsideMaker)
                {
                    var w = _windowRect.width;
                    _windowRect = IMGUIUtils.DragResizeEatWindow(id, _windowRect);
                    // Only resize width
                    _windowRect.width = w;
                }
            }

#if KK || KKS
            if (!MakerAPI.InsideMaker || ShowCoordinateButtons.Value)
            {
                var coordinateCount = _chaCtrl.chaFile.coordinate.Length;
                for (var i = 0; i < coordinateCount; i++)
                {
                    if (_coordButtons.Count <= i)
                    {
                        const float coordWidth = 25f;
                        const float coordHeight = 20f;
                        _coordButtons.Add(new CoordButton(coordId: i,
                                                          changeAction: val =>
                                                          {
                                                              // Coordinate change buttons
                                                              var customControl = MakerAPI.GetMakerBase()?.customCtrl;
                                                              if (customControl != null)
                                                                  customControl.ddCoordinate.value = val;
                                                              else
                                                                  _chaCtrl.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType)val);
                                                          },
                                                          position: new Rect(x: -coordWidth, y: 4 + coordHeight * i, width: coordWidth, height: coordHeight)));
                    }

                    var btn = _coordButtons[i];
                    if (GUI.Button(new Rect(btn.Position.x + _windowRect.x, btn.Position.y + _windowRect.y, btn.Position.width, btn.Position.height), btn.Content))
                        btn.OnClick();
                }
            }
#endif
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
                    new GUIContent[] // main/sub
                    {
                        new GUIContent($"M Slot {index}: On"),
                        new GUIContent($"S Slot {index}: On"),
                        new GUIContent($"Slot {index}: On"),
                    },
                    new GUIContent[]
                    {
                        new GUIContent($"M Slot {index}: Off"),
                        new GUIContent($"S Slot {index}: Off"),
                        new GUIContent($"Slot {index}: Off"),
                    }
                });
            }

#if KK || KKS
            var accTypeIndex = ShowMainSub.Value ? (_chaCtrl.nowCoordinate.accessory.parts[accIndex].hideCategory) : 2;
#elif EC
            var accTypeIndex = 2;
#endif
            var acc = _AccessoryButtonContentCache[accIndex][isOn ? 0 : 1][accTypeIndex];
            if (GUILayout.Button(acc, _NoLayoutOptions))
                _chaCtrl.SetAccessoryState(accIndex, !isOn);
            GUILayout.Space(-5);
        }

        private void SetupInterface()
        {
            _buttons.Clear();

#if KK || EC
            var distanceFromRightEdge = Screen.width / 10f;
#elif KKS
            var distanceFromRightEdge = Screen.width / 8.5f;
#endif
            var x = Screen.width - distanceFromRightEdge - WindowWidth - Margin;
            if (MakerAPI.InsideMaker)
            {
                _windowRect = new Rect(x, Margin, WindowWidth, 600);
            }
            else
            {
                // Let the window auto-size and keep the position while outside maker
                _windowRect = new Rect(x: _windowRect.x != 0 ? _windowRect.x : x,
                                       y: _windowRect.y != 0 ? _windowRect.y : Margin,
                                       width: WindowWidth,
                                       height: 200);
            }

            // Clothing piece state buttons
            foreach (ChaFileDefine.ClothesKind kind in Enum.GetValues(typeof(ChaFileDefine.ClothesKind)))
            {
#if KK || KKS
                if (kind == ChaFileDefine.ClothesKind.shoes_outer) continue;
#endif
                _buttons.Add(new ClothButton(kind, _chaCtrl));
            }
            // Invisible body
            if (MakerAPI.InsideMaker)
                _buttons.Add(new BodyButton(_chaCtrl));

#if KK
            if (MakerAPI.InsideMaker && MoveShoeButtons.Value)
            {
                var shoeToggles = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl.tglShoesType;
                var toggleIndoors = shoeToggles[0];
                var toggleOutdoors = shoeToggles[1];
                if (toggleIndoors != null && toggleOutdoors != null)
                {
                    _buttons.Add(null);
                    _buttons.Add(new ShoeButton(toggleIndoors, toggleOutdoors));
                }
                else
                {
                    Logger.LogWarning("Couldn't find shoe type toggles");
                }
            }
#endif

            _buttons.Add(null);
            _buttons.Add(new ActionButton("All accs On", () => _chaCtrl.SetAccessoryStateAll(true)));
            _buttons.Add(new ActionButton("All accs Off", () => _chaCtrl.SetAccessoryStateAll(false)));

#if KK || KKS
            if (MakerAPI.InsideMaker && MoveVanillaButtons.Value)
            {
                _buttons.Add(null);

                var acs = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl.tglShowAccessory;
                var toggleMain = acs[0];
                var toggleSub = acs[1];
                if (toggleMain != null && toggleSub != null)
                {
                    _buttons.Add(new ToggleButton(toggleMain, new GUIContent("Main accs: On"), new GUIContent("Main accs: Off")));
                    _buttons.Add(new ToggleButton(toggleSub, new GUIContent("Sub accs: On"), new GUIContent("Sub accs: Off")));
                }
                else
                {
                    Logger.LogWarning("Couldn't find toggleMain/toggleSub toggles");
                }
            }
#endif
        }

#if KK || KKS
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
#endif
    }
}
