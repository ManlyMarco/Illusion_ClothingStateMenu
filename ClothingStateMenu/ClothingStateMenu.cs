using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using ChaCustom;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using MoreAccessoriesKOI;
using UniRx;
using UnityEngine;

namespace KK_ClothingStateMenu
{
    [BepInPlugin("KK_ClothingStateMenu", "Clothing State Menu", Version)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInIncompatibility("MoreAccessories_CSM")]
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.0.3")]
    [BepInDependency(KoikatuAPI.GUID, "1.5")]
    public class ClothingStateMenu : BaseUnityPlugin
    {
        internal const string Version = "2.3.2";

        private const float Height = 20f;
        private const float Margin = 5f;
        private const float Width = 117f;

        private readonly List<IStateToggleButton> _buttons = new List<IStateToggleButton>();

        private Vector2 _accessorySlotsScrollPos = Vector2.zero;
        private Rect _accesorySlotsRect;

        private ChaControl _chaCtrl;

        private ConfigEntry<bool> ShowInMaker { get; set; }
        private ConfigEntry<bool> ShowCoordinateButtons { get; set; }
        private ConfigEntry<KeyboardShortcut> Keybind { get; set; }

        #region Entry point

        private void Start()
        {
            ShowInMaker = Config.Bind("General", "Show in Character Maker", false, "Show the clothing state menu in character maker. Can be enabled from maker interface or by pressing the keyboard shortcut.");
            ShowInMaker.SettingChanged += (sender, args) =>
            {
                if (MakerAPI.InsideMaker)
                    ShowInterface = ShowInMaker.Value;
            };

            ShowCoordinateButtons = Config.Bind("General", "Show coordinate change buttons in Character Maker", false, "Adds buttons to the menu that allow quickly switching between clothing sets. Same as using the clothing dropdown.\nThe buttons are always shown outside of character maker.");
            ShowCoordinateButtons.SettingChanged += (sender, args) =>
            {
                if (ShowInterface)
                    ShowInterface = true;
            };

            Keybind = Config.Bind("General", "Toggle clothing state menu", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift), "Keyboard shortcut to toggle the clothing state menu on and off.\nCan be used outside of character maker in some cases - works for males in H scenes (the male has to be visible for the menu to appear) and in some conversations with girls.");
            
            MakerAPI.RegisterCustomSubCategories += (sender, e) =>
            {
                _sidebarToggle = e.AddSidebarControl(new SidebarToggle("Show clothing state menu", ShowInMaker.Value, this));
                _sidebarToggle.ValueChanged.Subscribe(b => ShowInterface = b);
            };
            MakerAPI.MakerExiting += (sender, e) =>
            {
                _chaCtrl = null;
                _setCoordAction = null;
                _sidebarToggle = null;
            };
        }

        #endregion

        private Action<int> _setCoordAction;

        private bool _showOutsideMaker;
        private SidebarToggle _sidebarToggle;

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

                SetupCoordButtons();
                SetupAccRect();
            }
        }

        private bool CanShow()
        {
            if (_chaCtrl == null) return false;
            if (!_chaCtrl.visibleAll) return false;

            if (MakerAPI.InsideMaker && MakerAPI.GetMakerBase().customCtrl.hideFrontUI) return false;

            if (Manager.Scene.Instance.AddSceneName == "Config") return false;
            if (Manager.Scene.Instance.AddSceneName != Manager.Scene.Instance.AddSceneNameOverlapRemoved) return false;
            if (Manager.Scene.Instance.IsNowLoadingFade) return false;

            return true;
        }

        private void Update()
        {
            if (Keybind.Value.IsDown())
                ShowInterface = !ShowInterface;
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
                var showAccessory = _chaCtrl.fileStatus.showAccessory;
                if (showAccessory.Length > 1)
                {
                    if (GUILayout.Button("All accs On"))
                        _chaCtrl.SetAccessoryStateAll(true);
                    GUILayout.Space(-5);
                    if (GUILayout.Button("All accs Off"))
                        _chaCtrl.SetAccessoryStateAll(false);
                }

                _accessorySlotsScrollPos = GUILayout.BeginScrollView(_accessorySlotsScrollPos);
                {
                    GUILayout.BeginVertical();
                    {
                        for (var j = 0; j < showAccessory.Length; j++)
                        {
                            if (_chaCtrl.nowCoordinate.accessory.parts[j].type != 120)
                                DrawAccesoryButton(j, showAccessory[j]);
                        }

                        var charaMakerData = MoreAccessories._self._charaMakerData;
                        if (charaMakerData?.showAccessories != null)
                        {
                            var showAccessories = charaMakerData.showAccessories;
                            for (var k = 0; k < showAccessories.Count; k++)
                                DrawAccesoryButton(k + 20, showAccessories[k]);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();

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
        }

        private void DrawAccesoryButton(int accIndex, bool isOn)
        {
            if (GUILayout.Button($"Slot {accIndex + 1}: {(isOn ? "On" : "Off")}"))
                _chaCtrl.SetAccessoryState(accIndex, !isOn);
            GUILayout.Space(-5);
        }

        private void FindTargetCharacter()
        {
            _chaCtrl = MakerAPI.GetCharacterControl();
            if (_chaCtrl != null) return;

            var hFlag = FindObjectOfType<HFlag>();
            if (hFlag != null) _chaCtrl = hFlag.player.chaCtrl;
            else _chaCtrl = GetCurrentVisibleGirl()?.chaCtrl;
        }

        private static SaveData.Heroine GetCurrentVisibleGirl()
        {
            var result = FindObjectOfType<TalkScene>()?.targetHeroine;
            if (result != null)
                return result;

            try
            {
                var nowScene = Manager.Game.Instance?.actScene?.AdvScene?.nowScene;
                if (!nowScene) return null;

                var advSceneTargetHeroineProp = typeof(ADV.ADVScene).GetField("m_TargetHeroine", BindingFlags.Instance | BindingFlags.NonPublic);
                return advSceneTargetHeroineProp?.GetValue(nowScene) as SaveData.Heroine;
            }
            catch
            {
                return null;
            }
        }

        private void SetupCoordButtons()
        {
            var position = GetDisplayRect();
            foreach (ChaFileDefine.ClothesKind kind in Enum.GetValues(typeof(ChaFileDefine.ClothesKind)))
            {
                if (kind == ChaFileDefine.ClothesKind.shoes_outer) continue;
                _buttons.Add(new ClothButton(position, kind, _chaCtrl));
                position.y += Height;
            }

            if (MakerAPI.InsideMaker)
                _buttons.Add(new BodyButton(_chaCtrl, position));

            var customControl = MakerAPI.GetMakerBase()?.customCtrl;
            if (customControl != null)
            {
                var coordDropdown = typeof(CustomControl).GetField("ddCoordinate", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(customControl);
                var coordProp = coordDropdown?.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public);
                _setCoordAction = newVal => coordProp?.SetValue(coordDropdown, newVal, null);
            }
            else
            {
                _setCoordAction = newVal => _chaCtrl.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType)newVal);
            }
        }

        private void SetupAccRect()
        {
            _accesorySlotsRect = _buttons.Last().Position;
            _accesorySlotsRect.x = _accesorySlotsRect.x + 7;
            _accesorySlotsRect.width = _accesorySlotsRect.width - 7;
            _accesorySlotsRect.y = _accesorySlotsRect.y + (Height + Margin);
            _accesorySlotsRect.height = 300f;
        }

        private static Rect GetDisplayRect()
        {
            var distanceFromRightEdge = Screen.width / 10f;
            var x = Screen.width - distanceFromRightEdge - Width - Margin;
            return new Rect(x, Margin, Width, Height);
        }
    }
}
