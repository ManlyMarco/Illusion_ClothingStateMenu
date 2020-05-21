using System.Linq;
using System.Reflection;
using BepInEx;
using KKAPI;
using KKAPI.Maker;

namespace ClothingStateMenu
{
    [BepInPlugin(GUID, "Clothing State Menu", Version)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInIncompatibility("MoreAccessories_CSM")]
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.0.3")]
    [BepInDependency(KoikatuAPI.GUID, "1.5")]
    public partial class ClothingStateMenuPlugin
    {
        private void FindTargetCharacter()
        {
            _chaCtrl = MakerAPI.GetCharacterControl();
            if (_chaCtrl != null) return;

            var hFlag = FindObjectOfType<HFlag>();
            if (hFlag != null) _chaCtrl = hFlag.player.chaCtrl;
            if (_chaCtrl != null) return;

            _chaCtrl = GetCurrentVisibleGirl();
            if (_chaCtrl != null) return;

            // Fall back to brute search, prefer characters that aren't male and only run if we have a decent chance of getting the character that the player wants
            var chaControls = FindObjectsOfType<ChaControl>().Where(x => x.GetActiveTop() && x.visibleAll).ToArray();
            if (chaControls.Length < 4)
                _chaCtrl = chaControls.OrderBy(x => x.sex > 0).FirstOrDefault();
        }

        private ChaControl GetCurrentVisibleGirl()
        {
            var result = FindObjectOfType<TalkScene>()?.targetHeroine;
            if (result != null) return result.chaCtrl;

            var advScene = Manager.Game.Instance?.actScene?.AdvScene;
            if (advScene == null) return null;

            if (advScene.Scenario?.currentHeroine != null) return advScene.Scenario.currentHeroine.chaCtrl;

            // In event
            var character = Manager.Character.Instance;
            if (character != null && character.dictEntryChara.Count > 0)
            {
                // Main event char is usually (not always) the first one.
                // Also will pull the currently visible character, like the teacher or the mom.
                return character.dictEntryChara[0];
            }

            try
            {
                var advSceneTargetHeroineProp = typeof(ADV.ADVScene).GetField("m_TargetHeroine", BindingFlags.Instance | BindingFlags.NonPublic);
                var heroine = advSceneTargetHeroineProp?.GetValue(advScene.nowScene) as SaveData.Heroine;
                return heroine?.chaCtrl;
            }
            catch
            {
                return null;
            }
        }
    }
}