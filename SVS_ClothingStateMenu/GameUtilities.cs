using System.Collections.Generic;
using System.Linq;
using Character;
using ClothingStateMenu.Utils;
using SaveData;

namespace ClothingStateMenu;

/// <summary>
/// TODO move to a separate API dll
/// </summary>
internal static class GameUtilities
{
    public const string GameProcessName = "SamabakeScramble";

    /// <summary>
    /// True in character maker, both in main menu and in-game maker.
    /// </summary>
    public static bool InsideMaker => CharacterCreation.HumanCustom.Initialized;

    /// <summary>
    /// True if the game is running, e.g. a new game was started or a game was loaded. False in main menu, main menu character maker, etc.
    /// </summary>
    public static bool InsideGame => Manager.Game.saveData.WorldTime > 0;

    /// <summary>
    /// True if an H Scene is currently playing.
    /// </summary>
    public static bool InsideHScene => SV.H.HScene.Active();

    /// <summary>
    /// True if an ADV Scene is currently playing (both when talking with a VN text box at the bottom, and when the right-side conversation menu is shown).
    /// </summary>
    public static bool InsideADVScene => ADV.ADVManager.Initialized && ADV.ADVManager.Instance.IsADV;

    /// <summary>
    /// Get a display name of the character. Only use in interface, not for keeping track of the character.
    /// If <paramref name="translated"/> is true and AutoTranslator is active, try to get a translated version of the name in current language. Otherwise, return the original name.
    /// </summary>
    public static string GetCharaName(this Actor chara, bool translated)
    {
        var fullname = chara?.charFile?.Parameter?.fullname;
        if (!string.IsNullOrEmpty(fullname))
        {
            if (translated) throw new System.NotImplementedException();
            return fullname;
        }
        return chara?.chaCtrl?.name ?? chara?.ToString();
    }

    /// <summary>
    /// Get ID of this character in the main character list (in save data). Returns -1 if the character is not on the main game map and is not saved to the save data.
    /// </summary>
    public static int GetMainActorId(this Actor currentAdvChara)
    {
        var mainActorInstance = currentAdvChara.GetMainActorInstance();
        return mainActorInstance.Value != null ? mainActorInstance.Key : -1;
    }

    /// <summary>
    /// Get Humans involved in the current scene.
    /// If <paramref name="mainInstances"/> is true, the original overworld characters are returned (which are saved to the save file; if not found the character is not included in the result).
    /// If <paramref name="mainInstances"/> is false, the actors in the current scene are returned (which are copies of the original characters in H and ADV scenes; in maker nothing is returned since there is no actor).
    /// </summary>
    public static IEnumerable<Character.Human> GetVisibleHumans(bool mainInstances)
    {
        if (InsideMaker)
        {
            var maker = CharacterCreation.HumanCustom.Instance;
            return new[] { mainInstances ? maker.HumanData.About.GetMainActorInstance().Value?.chaCtrl : maker.Human };
        }

        return GetVisibleActors(mainInstances).Select(x => x.Value.chaCtrl);
    }

    /// <summary>
    /// Get actors involved in the current scene and their IDs.
    /// If <paramref name="mainInstances"/> is true, the original overworld characters are returned with their save data IDs (the characters that are saved to the save file; if not found the character is not included in the result).
    /// If <paramref name="mainInstances"/> is false, the actors in the current scene are returned with their relative IDs (which are copies of the original characters in H and ADV scenes; in maker nothing is returned since there is no actor).
    /// </summary>
    public static IEnumerable<KeyValuePair<int, Actor>> GetVisibleActors(bool mainInstances)
    {
        if (InsideMaker)
        {
            if (mainInstances)
            {
                var actor = CharacterCreation.HumanCustom.Instance.HumanData.About.GetMainActorInstance();
                if (actor.Value != null)
                    return new[] { actor };
            }

            return Enumerable.Empty<KeyValuePair<int, Actor>>();
        }

        if (SV.H.HScene.Active())
        {
            // HScene.Actors contains copies of the actors
            if (mainInstances)
                return SV.H.HScene._instance.Actors.Select(GetMainActorInstance).Where(x => x.Value != null);
            else
                return SV.H.HScene._instance.Actors.Select((ha, i) => new KeyValuePair<int, Actor>(i, ha.Actor)).Where(x => x.Value != null);
        }

        var talkManager = Manager.TalkManager._instance;
        if (talkManager != null && ADV.ADVManager._instance?.IsADV == true)
        {
            var npcs = new List<KeyValuePair<int, Actor>>
            {
                // PlayerHi and Npc1-4 contain copies of the Actors
                new(0,talkManager.PlayerHi),
                new(1,talkManager.Npc1),
                new(2,talkManager.Npc2),
                new(3,talkManager.Npc3),
                new(4,talkManager.Npc4),
            }.AsEnumerable();
            if (mainInstances)
                npcs = npcs.Select(pair => pair.Value.GetMainActorInstance());
            return npcs.Where(x => x.Value != null);
        }

        return GetMainActors();
    }

    /// <summary>
    /// Get all overworld characters together with their save data IDs (the characters that are saved to the save file).
    /// </summary>
    public static IEnumerable<KeyValuePair<int, Actor>> GetMainActors()
    {
        return Manager.Game.saveData.Charas.AsManagedEnumerable().Where(x => x.Value != null);
    }

    /// <summary>
    /// Get the main character instance of the actor (the one that is visible on the main map and saved to the save file).
    /// </summary>
    public static KeyValuePair<int, Actor> GetMainActorInstance(this SV.H.HActor x) => x?.Actor.GetMainActorInstance() ?? default;

    /// <summary>
    /// Get the main character instance of the actor (the one that is visible on the main map and saved to the save file).
    /// </summary>
    public static KeyValuePair<int, Actor> GetMainActorInstance(this Actor x) => x?.charFile.About.GetMainActorInstance() ?? default;

    /// <summary>
    /// Get the main character instance of the actor (the one that is visible on the main map and saved to the save file).
    /// TODO: Find a better way to get the originals
    /// </summary>
    public static KeyValuePair<int, Actor> GetMainActorInstance(this HumanDataAbout x) => x == null ? default : Manager.Game.Charas.AsManagedEnumerable().FirstOrDefault(y => x.dataID == y.Value.charFile.About.dataID);

}
