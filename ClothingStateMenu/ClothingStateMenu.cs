using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using ChaCustom;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;
using MoreAccessoriesKOI;
using TMPro;
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

        //private const float CoordWidth = 25f;
        private const float Height = 20f;
        private const float Margin = 5f;
        private const float Width = 120f;

        private readonly List<ClothButton> _buttons = new List<ClothButton>();

        private Vector2 _accessorySlotsScrollPos = Vector2.zero;
        private Rect _accesorySlotsRect;

        private ChaControl _chaCtrl;
        //private TMP_Dropdown _ddCoordinate;

        #region Entry point
        
        private void Start()
        {
            if (!KoikatuAPI.CheckRequiredPlugin(this, KoikatuAPI.GUID, new Version(KoikatuAPI.VersionConst)))
                return;

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
            e.AddSidebarControl(new SidebarToggle("Show clothing state menu", false, this)).ValueChanged.Subscribe(b => ShowInterface = b);
        }

        #endregion

        private bool _showInterface;
        private bool ShowInterface
        {
            get => _showInterface && _chaCtrl != null && 
                string.IsNullOrEmpty(Manager.Scene.Instance.AddSceneName) && 
                !MakerAPI.GetMakerBase().customCtrl.hideFrontUI;
            set
            {
                _showInterface = value;

                _chaCtrl = null;
                //_ddCoordinate = null;

                if (!_showInterface) return;

                //var customControl = MakerAPI.GetMakerBase().customCtrl;
                //_ddCoordinate = (TMP_Dropdown)typeof(CustomControl).GetField("ddCoordinate", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(customControl)
                //    ?? throw new InvalidOperationException("Failed to get CustomControl.ddCoordinate");

                _chaCtrl = MakerAPI.GetCharacterControl();

                var position = GetDisplayRect();
                foreach (ChaFileDefine.ClothesKind kind in Enum.GetValues(typeof(ChaFileDefine.ClothesKind)))
                {
                    if(kind == ChaFileDefine.ClothesKind.shoes_outer) continue;
                    _buttons.Add(new ClothButton(position, kind, _chaCtrl));
                    position.y += Height;
                }

                _accesorySlotsRect = _buttons.Last().Position;
                _accesorySlotsRect.y = _accesorySlotsRect.y + (Height + Margin);
                _accesorySlotsRect.height = 300f;
            }
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
                    clothButton.TriggerUpdate();
            }

            /*for (var i = 0; i < 7; i++)
            {
                var position = _buttons[i].Position;
                position.x -= CoordWidth + Margin;
                position.width = CoordWidth;
                if (GUI.Button(position, (i + 1).ToString()))
                    _ddCoordinate.value = i;
            }*/

            GUILayout.BeginArea(_accesorySlotsRect);
            {
                var showAccessory = _chaCtrl.fileStatus.showAccessory;
                if (showAccessory.Length > 1)
                {
                    if (GUILayout.Button("All accs ON"))
                        _chaCtrl.SetAccessoryStateAll(true);
                    if (GUILayout.Button("All accs OFF"))
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
        }

        private void DrawAccesoryButton(int accIndex, bool isOn)
        {
            if (GUILayout.Button($"Slot {accIndex}: {(isOn ? "On" : "Off")}"))
                _chaCtrl.SetAccessoryState(accIndex, !isOn);
        }
    }
}
