using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
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
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories")]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInProcess("Koikatu")]
    public class ClothingStateMenu : BaseUnityPlugin
    {
        internal const string Version = "2.3";

        private const float Height = 20f;
        private const float Margin = 5f;
        private const float Width = 117f;

        private readonly List<IStateToggleButton> _buttons = new List<IStateToggleButton>();

        private Vector2 _accessorySlotsScrollPos = Vector2.zero;
        private Rect _accesorySlotsRect;

        private ChaControl _chaCtrl;

        [Browsable(false)]
        private ConfigWrapper<bool> Show { get; set; }

        [DisplayName("Show coordinate change buttons in chara maker")]
        [Description("Adds buttons to the menu that allow quickly switching between clothing sets. Same as using the clothing dropdown.\n" +
                     "The buttons are always shown outside of character maker.")]
        private ConfigWrapper<bool> ShowCoordinateButtons { get; set; }

        [DisplayName("Show clothing state menu outside chara maker")]
        [Description("Works for males in H scenes (the male has to be visible for the menu to appear) and in some conversations with girls.")]
        private SavedKeyboardShortcut Keybind { get; set; }

        #region Entry point

        private void Start()
        {
            if (!KoikatuAPI.CheckRequiredPlugin(this, KoikatuAPI.GUID, new Version("1.2")) ||
                !KoikatuAPI.CheckRequiredPlugin(this, "com.joan6694.illusionplugins.moreaccessories", new Version("1.0.3")))
                return;

            Show = new ConfigWrapper<bool>("Show", this, false);
            ShowCoordinateButtons = new ConfigWrapper<bool>("ShowCoordinateButtons", this, false);
            Keybind = new SavedKeyboardShortcut("keybind", this, new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift));

            KoikatuAPI.CheckIncompatiblePlugin(this, "MoreAccessories_CSM");

            MakerAPI.RegisterCustomSubCategories += MakerAPI_Enter;
            MakerAPI.MakerExiting += MakerAPI_Exit;
        }

        private void MakerAPI_Exit(object sender, EventArgs e)
        {
            _showInterface = false;
            _chaCtrl = null;
            _setCoordAction = null;
        }

        private void MakerAPI_Enter(object sender, RegisterSubCategoriesEvent e)
        {
            e.AddSidebarControl(new SidebarToggle("Show clothing state menu", Show.Value, this)).ValueChanged.Subscribe(b => ShowInterface = b);
        }

        #endregion

        private Action<int> _setCoordAction;

        private bool _showInterface;
        private bool ShowInterface
        {
            get
            {
                if (!_showInterface) return false;

                return CanShow();
            }
            set
            {
                _showInterface = value;

                if (MakerAPI.InsideMaker)
                    Show.Value = value;

                _chaCtrl = null;
                _buttons.Clear();

                if (!_showInterface) return;

                FindTargetCharacter();

                if (!MakerAPI.InsideMaker && !CanShow())
                {
                    _showInterface = false;
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
            if (!MakerAPI.InsideMaker && Keybind.IsDown())
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
