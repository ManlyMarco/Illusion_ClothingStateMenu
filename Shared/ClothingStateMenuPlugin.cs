using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using UniRx;
using UnityEngine;

namespace ClothingStateMenu
{
    public partial class ClothingStateMenuPlugin : BaseUnityPlugin
    {
        public const string Version = "3.2";
        public const string GUID = "ClothingStateMenu";

        private const float Margin = 5f;
        private const float WindowWidth = 125f;

        private static readonly Color _BackgroundColor = new Color(0, 0, 0, 0.6f);
        private static readonly GUILayoutOption[] _NoLayoutOptions = new GUILayoutOption[0];
        private static readonly List<GUIContent[][]> _AccessoryButtonContentCache = new List<GUIContent[][]>();

        private readonly List<IStateToggleButton> _buttons = new List<IStateToggleButton>();
        private const int CoordCount = 7;
        private readonly CoordButton[] _coordButtons = new CoordButton[CoordCount];

        private Rect _windowRect;
        private Vector2 _accessorySlotsScrollPos = Vector2.zero;

        private ChaControl _chaCtrl;
        private SidebarToggle _sidebarToggle;
        private static List<bool> _showAccessoryMemory;

        private bool _showOutsideMaker;

        private ConfigEntry<bool> ShowInMaker { get; set; }
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
            ShowInMaker = Config.Bind("General", "Show in Character Maker", false, "Show the clothing state menu in character maker. Can be enabled from maker interface or by pressing the keyboard shortcut.");
            ShowInMaker.SettingChanged += (sender, args) =>
            {
                if (MakerAPI.InsideMaker)
                    ShowInterface = ShowInMaker.Value;
            };
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
            ShowMainSub = Config.Bind("Options", "Show Main/Sub acc type in list", false, "Show in the toggle list whether an accessory's category is Main (M) or Sub (S).");
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
                _showAccessoryMemory.Clear();
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

            if (MakerAPI.InsideMaker && _chaCtrl != null)
            {
                var showAccessory = _chaCtrl.fileStatus.showAccessory;
                if (_showAccessoryMemory != null && showAccessory.Length == _showAccessoryMemory.Count)
                {
                    for (int i = 0; i < _showAccessoryMemory.Count; i++)
                    {
                        if (showAccessory[i] != _showAccessoryMemory[i] && _chaCtrl.nowCoordinate.accessory.parts[i].type != 120)
                        {
                            _chaCtrl.SetAccessoryState(i, _showAccessoryMemory[i]);
                        }
                    }
                }
                _showAccessoryMemory = showAccessory.ToList();

#if KK || KKS
                if (_coordMemory != _chaCtrl.fileStatus.coordinateType && !RetainStatesBetweenOutfits.Value)
                    _showAccessoryMemory.Clear();
                _coordMemory = _chaCtrl.fileStatus.coordinateType;
#endif
            }
        }

        private void OnGUI()
        {
            if (!ShowInterface)
                return;

            GUI.backgroundColor = _BackgroundColor;

            _windowRect = GUILayout.Window(90876322, _windowRect, WindowFunc, GUIContent.none, GUI.skin.label, _NoLayoutOptions);

            void WindowFunc(int id)
            {
                GUI.backgroundColor = _BackgroundColor;

                foreach (var clothButton in _buttons)
                {
                    if (clothButton is SpaceButton)
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
                GUILayout.Space(7);

                _accessorySlotsScrollPos = GUILayout.BeginScrollView(_accessorySlotsScrollPos, _NoLayoutOptions);
                {
                    // Not worthwhile to virtualize, far too few items
                    for (var j = 0; j < _showAccessoryMemory.Count; j++)
                    {
                        if (_chaCtrl.nowCoordinate.accessory.parts[j].type != 120)
                            DrawAccesoryButton(j, _showAccessoryMemory[j]);
                    }
                }
                GUILayout.EndScrollView();
            }

#if KK || KKS
            if (!MakerAPI.InsideMaker || ShowCoordinateButtons.Value)
            {
                for (var i = 0; i < CoordCount; i++)
                {
                    var btn = _coordButtons[i];
                    if (GUI.Button(btn.Position, btn.Content))
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
            {
                _chaCtrl.SetAccessoryState(accIndex, !isOn);
                _showAccessoryMemory[accIndex] = !isOn;
            }
            GUILayout.Space(-5);
        }

        private void SetupInterface()
        {
#if KK || EC
            var distanceFromRightEdge = Screen.width / 10f;
#elif KKS
            var distanceFromRightEdge = Screen.width / 8.5f;
#endif
            var x = Screen.width - distanceFromRightEdge - WindowWidth - Margin;
            _windowRect = new Rect(x, Margin, WindowWidth, 600);

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
                    _buttons.Add(new SpaceButton());
                    _buttons.Add(new ShoeButton(toggleIndoors, toggleOutdoors));
                }
                else
                {
                    Logger.LogWarning("Couldn't find shoe type toggles");
                }
            }
#endif

            _buttons.Add(new SpaceButton());
            _buttons.Add(new ActionButton("All accs On", () =>
            {
                _chaCtrl.SetAccessoryStateAll(true);
                _showAccessoryMemory.Clear();
            }));
            _buttons.Add(new ActionButton("All accs Off", () =>
            {
                _chaCtrl.SetAccessoryStateAll(false);
                _showAccessoryMemory.Clear();
            }));

#if KK || KKS
            if (MakerAPI.InsideMaker && MoveVanillaButtons.Value)
            {
                _buttons.Add(new SpaceButton());

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

            // Coordinate change buttons
            Action<int> setCoordAction;
            var customControl = MakerAPI.GetMakerBase()?.customCtrl;
            if (customControl != null)
            {
                var coordDropdown = customControl.ddCoordinate;
                setCoordAction = newVal => coordDropdown.value = newVal;
            }
            else
            {
                setCoordAction = newVal => _chaCtrl.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType)newVal);
            }

            for (var i = 0; i < CoordCount; i++)
            {
                const float coordWidth = 25f;
                const float coordHeight = 20f;
                var position = new Rect(x: _windowRect.x - coordWidth,
                                        y: _windowRect.y + 4 + coordHeight * i,
                                        width: coordWidth,
                                        height: coordHeight);
                _coordButtons[i] = new CoordButton(i, setCoordAction, position);
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
            var acs = MakerAPI.GetMakerBase().customCtrl.cmpDrawCtrl.tglShowAccessory;
            var toggleMain = acs[0];
            var toggleSub = acs[1];
            toggleMain.onValueChanged.AddListener(x => { _showAccessoryMemory.Clear(); });
            toggleSub.onValueChanged.AddListener(x => { _showAccessoryMemory.Clear(); });
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
