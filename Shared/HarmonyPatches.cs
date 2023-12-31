using HarmonyLib;
using ChaCustom;

namespace ClothingStateMenu {
    public static class HarmonyPatches {
        internal static void Init() {
            Hooks.SetupHooks();
        }

        private static class Hooks {
            public static void SetupHooks() {
                Harmony.CreateAndPatchAll(typeof(Hooks), null);
            }

            // Makes OnObjectVisibilityToggled fire for folders
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsDrawCtrl), "UpdateAccessoryDraw")]
            private static bool CvsDrawDisableUpdateAccessoryDraw() {
                return false;
            }
        }    
    }
}
