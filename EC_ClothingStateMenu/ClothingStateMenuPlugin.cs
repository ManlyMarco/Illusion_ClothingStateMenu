﻿using BepInEx;
using KKAPI;
using KKAPI.Maker;

namespace ClothingStateMenu
{
    [BepInPlugin(GUID, "Clothing State Menu", Version)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class ClothingStateMenuPlugin
    {
        private void FindTargetCharacter()
        {
            _chaCtrl = MakerAPI.GetCharacterControl();
            if (_chaCtrl != null) return;

            _chaCtrl = FindObjectOfType<ChaControl>();
        }
    }
}