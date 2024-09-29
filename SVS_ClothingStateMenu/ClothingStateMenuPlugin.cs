using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

// todo it needs a rewrite to use in svs and hc
namespace ClothingStateMenu
{
    [BepInPlugin(GUID, "Clothing State Menu", Version)]
    [BepInProcess(GameUtilities.GameProcessName)]
    public  class ClothingStateMenuPlugin : BasePlugin
    {
        public const string Version = "4.0.1";
        public const string GUID = "ClothingStateMenu";

        internal static ConfigEntry<KeyboardShortcut> Keybind { get; set; }
        internal static ManualLogSource Logger { get; private set; }


        public override void Load()
        {
            Logger = Log;
            Keybind = Config.Bind("Hotkeys", "Open Menu", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift));
            AddComponent<ClothingStateMenuComponent>();
        }
    }

    public sealed class ClothingStateMenuComponent : MonoBehaviour
    {
        private void Update()
        {
            if (ClothingStateMenuPlugin.Keybind.Value.IsDown())
            {
                Console.WriteLine("hit");
            }
        }
        /*
           CharacterCreation.HumanCustom.Instance.Human.cloth.fileStatus.clothesState
           if (InsideMaker)
               return CharacterCreation.HumanCustom.Instance.SetClothesVisible(ChaFileDefine.ClothesKind.top, ) then updatestate
*/
    }
}