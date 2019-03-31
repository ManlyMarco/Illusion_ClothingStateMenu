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
        internal const string Version = "2.0";

        private const float Height = 20f;
        private const float Margin = 5f;
        private const float Width = 117f;

        private readonly List<ClothButton> _buttons = new List<ClothButton>();

        private Vector2 _accessorySlotsScrollPos = Vector2.zero;
        private Rect _accesorySlotsRect;

        private ChaControl _chaCtrl;

        [Browsable(false)]
        private ConfigWrapper<bool> Shown { get; set; }

        [DisplayName("Show coordinate change buttons")]
        [Description("Adds buttons to the menu that allow quickly switching between clothing sets. Same as using the clothing dropdown.")]
        private ConfigWrapper<bool> ShowCoordinateButtons { get; set; }

        #region Entry point

        private void Start()
        {
            if (!KoikatuAPI.CheckRequiredPlugin(this, KoikatuAPI.GUID, new Version(KoikatuAPI.VersionConst)) ||
                !KoikatuAPI.CheckRequiredPlugin(this, "com.joan6694.illusionplugins.moreaccessories", new Version("1.0.3")))
                return;

            Shown = new ConfigWrapper<bool>("Shown", this, false);
            ShowCoordinateButtons = new ConfigWrapper<bool>("ShowCoordinateButtons", this, false);

            KoikatuAPI.CheckIncompatiblePlugin(this, "MoreAccessories_CSM");

            MakerAPI.RegisterCustomSubCategories += MakerAPI_Enter;
            MakerAPI.MakerExiting += MakerAPI_Exit;
        }

        private void MakerAPI_Exit(object sender, EventArgs e)
        {
            ShowInterface = false;
        }

        private void MakerAPI_Enter(object sender, RegisterSubCategoriesEvent e)
        {
            ShowInterface = Shown.Value;
            e.AddSidebarControl(new SidebarToggle("Show clothing state menu", Shown.Value, this)).ValueChanged.Subscribe(b => ShowInterface = b);
        }

        #endregion

        private Action<int> _setCoordAction;

        private bool _showInterface;
        private bool ShowInterface
        {
            get => _showInterface && _chaCtrl != null &&
                string.IsNullOrEmpty(Manager.Scene.Instance.AddSceneName) &&
                !MakerAPI.GetMakerBase().customCtrl.hideFrontUI;
            set
            {
                _showInterface = value;
                Shown.Value = value;

                _chaCtrl = null;
                _buttons.Clear();

                if (!_showInterface) return;

                _chaCtrl = MakerAPI.GetCharacterControl();

                SetupCoordButtons();

                var position = GetDisplayRect();
                foreach (ChaFileDefine.ClothesKind kind in Enum.GetValues(typeof(ChaFileDefine.ClothesKind)))
                {
                    if (kind == ChaFileDefine.ClothesKind.shoes_outer) continue;
                    _buttons.Add(new ClothButton(position, kind, _chaCtrl));
                    position.y += Height;
                }

                _accesorySlotsRect = _buttons.Last().Position;
                _accesorySlotsRect.x = _accesorySlotsRect.x + 7;
                _accesorySlotsRect.width = _accesorySlotsRect.width - 7;
                _accesorySlotsRect.y = _accesorySlotsRect.y + (Height + Margin);
                _accesorySlotsRect.height = 300f;
            }
        }

        private void SetupCoordButtons()
        {
            var customControl = MakerAPI.GetMakerBase().customCtrl;
            var coordDropdown = typeof(CustomControl).GetField("ddCoordinate", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(customControl)
                                ?? throw new InvalidOperationException("Failed to get CustomControl.ddCoordinate");
            var coordProp = coordDropdown.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public);
            _setCoordAction = newVal => coordProp.SetValue(coordDropdown, newVal, null);
        }

        private static Rect GetDisplayRect()
        {
            var distanceFromRightEdge = Screen.width / 10f;
            var x = Screen.width - distanceFromRightEdge - Width - Margin;
            return new Rect(x, Margin, Width, Height);
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
                        if (charaMakerData?.showAccessories == null)
                            return;

                        var showAccessories = MoreAccessories._self._charaMakerData.showAccessories;
                        for (var k = 0; k < showAccessories.Count; k++)
                            DrawAccesoryButton(k + 20, showAccessories[k]);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
            
            if (ShowCoordinateButtons.Value)
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
            if (GUILayout.Button($"Slot {accIndex}: {(isOn ? "On" : "Off")}"))
                _chaCtrl.SetAccessoryState(accIndex, !isOn);
            GUILayout.Space(-5);
        }
    }
}
