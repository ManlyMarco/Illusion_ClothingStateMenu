using System.Linq;
using BepInEx;
using KKAPI;
using KKAPI.MainGame;
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

            var hFlag = FindObjectOfType<HFlag>();
            if (hFlag != null) _chaCtrl = hFlag.player.chaCtrl;
            if (_chaCtrl != null) return;

            _chaCtrl = GameAPI.GetCurrentHeroine()?.chaCtrl;
            if (_chaCtrl != null) return;

            // Fall back to brute search, prefer characters that aren't male and only run if we have a decent chance of getting the character that the player wants
            var chaControls = FindObjectsOfType<ChaControl>().Where(x => x.GetActiveTop() && x.visibleAll).ToArray();
            if (chaControls.Length < 4)
                _chaCtrl = chaControls.OrderBy(x => x.sex > 0).FirstOrDefault();
        }
    }
}