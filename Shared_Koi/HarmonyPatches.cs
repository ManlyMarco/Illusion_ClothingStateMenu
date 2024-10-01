using HarmonyLib;

namespace ClothingStateMenu
{
    internal static class Hooks
    {
        internal static void Init()
        {
#if !EC
            Harmony.CreateAndPatchAll(typeof(Hooks), ClothingStateMenuPlugin.GUID);
        }

        // Disables aggressive syncing of the accessory toggle states to the vanilla buttons
        // Unnecessary in EC
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaCustom.CvsDrawCtrl), nameof(ChaCustom.CvsDrawCtrl.UpdateAccessoryDraw))]
        private static bool CvsDrawDisableUpdateAccessoryDraw()
        {
            return false;
#endif
        }
    }
}
