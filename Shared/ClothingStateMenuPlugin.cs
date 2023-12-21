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

        private const float Height = 20f;
        private const float Margin = 5f;
        private const float Width = 117f;

        private readonly List<IStateToggleButton> _buttons = new List<IStateToggleButton>();

        private Vector2 _accessorySlotsScrollPos = Vector2.zero;
        private Rect _accesorySlotsRect;

        private ChaControl _chaCtrl;
        private SidebarToggle _sidebarToggle;
        private List<bool> showAccessoryMemory;

        private bool _showOutsideMaker;
        private ConfigEntry<bool> ShowInMaker { get; set; }

#if KK || KKS
        private ConfigEntry<bool> ShowCoordinateButtons { get; set; }
        private ConfigEntry<bool> RetainStatesBetweenOutfits { get; set; }
        private ConfigEntry<bool> ShowVanillaButtons { get; set; }
        private Action<int> _setCoordAction;

        private int coordMemory = -1;
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

#if KK || KKS
            ShowVanillaButtons = Config.Bind("General", "Show Vanilla Acc Buttons", true, "Show the vanilla \"Main\" and \"Sub\" accessory toggle buttons. Disabling them can free up some space for other things in the sidebar.");
            ShowVanillaButtons.SettingChanged += (sender, args) => ToggleAccButtons(ShowVanillaButtons.Value);
            RetainStatesBetweenOutfits = Config.Bind("General", "Retain Acc States Between Outfits", false, "Acc slots toggled off in one outfit will remain toggled off in others.\nIf disabled, the accs sync up to the vanilla buttons on outfit change.");
            MakerAPI.MakerFinishedLoading += (sender, e) => { RegisterToggleEvents(); ToggleAccButtons(ShowVanillaButtons.Value); };

            ShowCoordinateButtons = Config.Bind("General", "Show coordinate change buttons in Character Maker", false, "Adds buttons to the menu that allow quickly switching between clothing sets. Same as using the clothing dropdown.\nThe buttons are always shown outside of character maker.");
            ShowCoordinateButtons.SettingChanged += (sender, args) =>
            {
                if (ShowInterface)
                    ShowInterface = true;
            };
#endif

            Keybind = Config.Bind("General", "Toggle clothing state menu", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift), "Keyboard shortcut to toggle the clothing state menu on and off.\nCan be used outside of character maker in some cases - works for males in H scenes (the male has to be visible for the menu to appear) and in some conversations with girls.");

            MakerAPI.RegisterCustomSubCategories += (sender, e) =>
            {
                _sidebarToggle = e.AddSidebarControl(new SidebarToggle("Show clothing state menu", ShowInMaker.Value, this));
                _sidebarToggle.ValueChanged.Subscribe(b => ShowInterface = b);
            };
            MakerAPI.MakerExiting += (sender, e) =>
            {
                _chaCtrl = null;
#if KK || KKS
                _setCoordAction = null;
#endif
                _sidebarToggle = null;
            };
        }

        private bool ShowInterface
        {
            get
            {
                if (MakerAPI.InsideMaker)
                {
                    if (!ShowInMaker.Value)
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

                return CanShow();
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

                SetupInterface();
            }
        }

        private bool CanShow()
        {
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

            if (MakerAPI.InsideMaker && _chaCtrl != null)
            {
                var showAccessory = _chaCtrl.fileStatus.showAccessory;
                if (showAccessoryMemory != null && showAccessory.Length == showAccessoryMemory.Count)
                {
                    for (int i = 0; i < showAccessoryMemory.Count; i++)
                    {
                        if (showAccessory[i] != showAccessoryMemory[i] && _chaCtrl.nowCoordinate.accessory.parts[i].type != 120)
                        {
                            _chaCtrl.SetAccessoryState(i, showAccessoryMemory[i]);
                        }
                    }
                }
                showAccessoryMemory = showAccessory.ToList();

#if KK || KKS
                if (coordMemory != _chaCtrl.fileStatus.coordinateType && !RetainStatesBetweenOutfits.Value)
                    showAccessoryMemory.Clear();
                coordMemory = _chaCtrl.fileStatus.coordinateType;
#endif
            }
        }

        private void OnGUI()
        {
            if (!ShowInterface)
                return;

            foreach (var clothButton in _buttons)
            {
                if (GUI.Button(clothButton.Position, clothButton.Text))
                    clothButton.NextState();
            }

            GUILayout.BeginArea(_accesorySlotsRect);
            {
                if (showAccessoryMemory.Count > 1)
                {
                    if (GUILayout.Button("All accs On"))
                    {
                        _chaCtrl.SetAccessoryStateAll(true);
                        showAccessoryMemory.Clear();
                    }
                    GUILayout.Space(-5);
                    if (GUILayout.Button("All accs Off"))
                    {
                        _chaCtrl.SetAccessoryStateAll(false);
                        showAccessoryMemory.Clear();
                    }
                }

                _accessorySlotsScrollPos = GUILayout.BeginScrollView(_accessorySlotsScrollPos);
                {
                    GUILayout.BeginVertical();
                    {
                        for (var j = 0; j < showAccessoryMemory.Count; j++)
                        {
                            if (_chaCtrl.nowCoordinate.accessory.parts[j].type != 120)
                                DrawAccesoryButton(j, showAccessoryMemory[j]);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

#if KK || KKS
            if (!MakerAPI.InsideMaker || ShowCoordinateButtons.Value)
            {
                const float coordWidth = 25f;

                for (var i = 0; i < 7; i++)
                {
                    var position = _buttons[i].Position;
                    position.x -= coordWidth + Margin;
                    position.width = coordWidth;
                    if (GUI.Button(position, (i + 1).ToString()))
                        _setCoordAction(i);
                }
            }
#endif
        }

        private void DrawAccesoryButton(int accIndex, bool isOn)
        {
            if (GUILayout.Button($"Slot {accIndex + 1}: {(isOn ? "On" : "Off")}"))
            {
                _chaCtrl.SetAccessoryState(accIndex, !isOn);
                showAccessoryMemory[accIndex] = !isOn;
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
            var x = Screen.width - distanceFromRightEdge - Width - Margin;
            var windowRect = new Rect(x, Margin, Width, Height);

            // Clothing piece state buttons
            foreach (ChaFileDefine.ClothesKind kind in Enum.GetValues(typeof(ChaFileDefine.ClothesKind)))
            {
#if KK || KKS
                if (kind == ChaFileDefine.ClothesKind.shoes_outer) continue;
#endif
                _buttons.Add(new ClothButton(windowRect, kind, _chaCtrl));
                windowRect.y += Height;
            }
            // Invisible body
            if (MakerAPI.InsideMaker)
                _buttons.Add(new BodyButton(_chaCtrl, windowRect));

            // Accessories
            _accesorySlotsRect = _buttons.Last().Position;
            _accesorySlotsRect.x += 7;
            _accesorySlotsRect.width -= 7;
            _accesorySlotsRect.y += Height + Margin;
            _accesorySlotsRect.height = 300f;

#if KK || KKS
            // Coordinate change buttons
            var customControl = MakerAPI.GetMakerBase()?.customCtrl;
            if (customControl != null)
            {
                var coordDropdown = customControl.ddCoordinate;
                _setCoordAction = newVal => coordDropdown.value = newVal;
            }
            else
            {
                _setCoordAction = newVal => _chaCtrl.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType)newVal);
            }
#endif
        }

#if KK || KKS
        private void ToggleAccButtons(bool _state)
        {
            Transform root = UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name == "txtClothesState").FirstOrDefault().transform.parent;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).gameObject.name == "txtAccessory")
                {
                    root.GetChild(i + 0).gameObject.SetActive(_state);
                    root.GetChild(i + 1).gameObject.SetActive(_state);
                    root.GetChild(i + 2).gameObject.SetActive(_state);
                    break;
                }
            }
        }

        private void RegisterToggleEvents()
        {
            UnityEngine.UI.Toggle toggle1 = UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name == "imgTglCol01").FirstOrDefault().GetComponent<UnityEngine.UI.Toggle>();
            UnityEngine.UI.Toggle toggle2 = UnityEngine.Object.FindObjectsOfType<GameObject>().Where(x => x.name == "imgTglCol02").FirstOrDefault().GetComponent<UnityEngine.UI.Toggle>();
            toggle1.onValueChanged.AddListener((x) => { showAccessoryMemory.Clear(); });
            toggle2.onValueChanged.AddListener((x) => { showAccessoryMemory.Clear(); });
        }
#endif
    }
}
