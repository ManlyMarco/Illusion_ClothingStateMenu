using System;
using System.Collections.Generic;
using System.Linq;

using Character;

#if SVS
using SaveData;
#elif AC
using static Character.HumanAccessory;

using AC.Scene;
using Actor = AC.User.ActorData;
#endif


namespace IllusionMods;

/// <summary>
/// TODO move to a separate API dll
/// </summary>
internal static class GameUtilities
{
    // IDHI: I did not try to get equivalent alternative to SVS some of them kind of
    // don't port like sometimes using a copy of Actors so no main actor
    // one my wish list is:
    //  1 - InsideGame
    //  2 - When the game start from a save game
#if SVS
    public const string GameProcessName = "SamabakeScramble";
#elif AC
    public const string GameProcessName = "Aicomi";
#endif
    /// <summary>
    /// True in character maker, both in main menu and in-game maker.
    /// </summary>
    public static bool InsideMaker => CharacterCreation.HumanCustom.Initialized;
#if SVS
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
#elif AC
    /// <summary>
    /// True if an H Scene is currently playing.
    /// </summary>
    public static bool InsideHScene => H.HScene.IsActive();

    /// <summary>
    /// True if and ADV or Touch scene is playing
    /// </summary>
    public static bool InsideCommunication
    {
        get
        {
            var es = ExploreScene.Instance;
            return es != null
                && es.CommunicationUI != null
                && es.CommunicationUI.isActiveAndEnabled
                && es.CommunicationUI._targets.Count > 0
                && !InsideHScene;
        }
    }
#endif
    /// <summary>
    /// Get a display name of the character. Only use in interface, not for keeping track of the character.
    /// If <paramref name="translated"/> is true and AutoTranslator is active, try to get a translated version of the name in current language. Otherwise, return the original name.
    /// </summary>
    public static string GetCharaName(this Actor chara, bool translated)
    {
#if SVS
        var fullname = chara?.charFile?.Parameter.GetCharaName(translated);
#elif AC
        var fullname = chara?.HumanData.Parameter.firstname;
#endif
        if (!string.IsNullOrEmpty(fullname))
        {
            if (translated)
            {
                TranslationHelper.TryTranslate(fullname, out var translatedName);
                if (!string.IsNullOrEmpty(translatedName))
                    return translatedName;
            }
            return fullname;
        }
#if SVS
        return chara?.chaCtrl?.name ?? chara?.ToString();
#elif AC
        // TODO: Find a better fallback
        return string.Empty;
#endif
    }

    /// <summary>
    /// Get a display name of the character. Only use in interface, not for keeping track of the character.
    /// If <paramref name="translated"/> is true and AutoTranslator is active, try to get a translated version of the name in current language. Otherwise, return the original name.
    /// </summary>
    public static string GetCharaName(this HumanDataParameter param, bool translated)
    {
        var fullname = param?.fullname;
        if (!string.IsNullOrEmpty(fullname))
        {
            if (translated)
            {
                TranslationHelper.TryTranslate(fullname, out var translatedName);
                if (!string.IsNullOrEmpty(translatedName))
                    return translatedName;
            }
            return fullname;
        }
        return "";
    }

#if SVS
    /// <summary>
    /// Get ID of this character in the main character list (in save data). Returns -1 if the character is a copy, or if it is not saved to the save data.
    /// </summary>
    public static int TryGetActorId(this Actor currentAdvChara)
    {
        if (currentAdvChara == null) throw new ArgumentNullException(nameof(currentAdvChara));

        var found = Manager.Game.Charas.AsManagedEnumerable().FirstOrDefault(x => currentAdvChara.Equals(x.Value));
        return found.Value != null ? found.Key : -1;
    }

    /// <summary>
    /// Get ID of this character (or ID of the original instance of this character copy) in the main character list (in save data). Returns -1 if the character is not on the main game map and is not saved to the save data.
    /// </summary>
    public static int FindMainActorId(this Actor currentAdvChara)
    {
        if (currentAdvChara == null) throw new ArgumentNullException(nameof(currentAdvChara));

        var mainActorInstance = currentAdvChara.FindMainActorInstance();
        return mainActorInstance.Value != null ? mainActorInstance.Key : -1;
    }

    /// <summary>
    /// Get Humans involved in the current scene.
    /// If <paramref name="mainInstances"/> is true, the original overworld characters are returned (which are saved to the save file; if not found the character is not included in the result).
    /// If <paramref name="mainInstances"/> is false, the actors in the current scene are returned (which are copies of the original characters in H and ADV scenes; in maker nothing is returned since there is no actor).
    /// </summary>
    public static IEnumerable<Character.Human> GetCurrentHumans(bool mainInstances)
    {
        if (InsideMaker)
        {
            var maker = CharacterCreation.HumanCustom.Instance;
            if (!mainInstances)
                return new[] { maker.Human };

            var result = maker.HumanData.About.FindMainActorInstance().Value?.chaCtrl;
            return result != null ? new[] { result } : Enumerable.Empty<Character.Human>();
        }

        return GetCurrentActors(mainInstances).Select(x => x.Value.chaCtrl).Where(x => x != null);
    }
#elif AC
    /// <summary>
    /// Get Humans involved in the current scene.
    /// </summary>
    public static IEnumerable<Character.Human> GetCurrentHumans()
    {
        if (InsideMaker)
        {
            var maker = CharacterCreation.HumanCustom.Instance;
            if (maker != null)
            {
                return new[] { maker.Human };
            }
        }
        else if (InsideHScene)
        {
            var HActors = H.HScene.Instance.GetActors(H.HScene.ActorType.All);

            var ha = HActors.Select(ha => ha.Human).Where(ha => ha != null).ToArray();
            if (ha != null)
            {
                return ha;
            }
        }
        else if (InsideCommunication)
        {
            var dictAllHumans = GetHumansDictionary();
            if (dictAllHumans != null)
            {
                var h = ExploreScene.Instance?.CommunicationUI?._targets
                    .AsManagedEnumerable().Where(x => x?.BaseData != null)
                    .Select(x =>
                    {
                        dictAllHumans.TryGetValue(x.BaseData, out var sa);
                        return sa;
                    })
                    .Where(x => x != null).ToArray();
                if (h != null)
                {
                    return h;
                }
            }
        }
        return Enumerable.Empty<Human>();
    }

    // IDHI: Human information as found under AC.Scene.Explor.Actor
    // Dictionary<AC.User.ActorData, AC.Scene.Explore.Actor.Params.Chara>
    public static Dictionary<Actor, Human> GetHumansDictionary()
    {
        var sa = ExploreScene.Instance?._actors.ToArray();
        var sah = sa.Select(x => x).Where(x => x != null)
            .ToDictionary(x => x.BaseData, x => x.Params.Chara);
        if (sah != null)
        {
            return sah;
        }
        return null;
    }
#endif

#if SVS
    /// <summary>
    /// Get actors involved in the current scene and their IDs.
    /// If <paramref name="mainInstances"/> is true, the original overworld characters are returned with their save data IDs (the characters that are saved to the save file; if not found the character is not included in the result).
    /// If <paramref name="mainInstances"/> is false, the actors in the current scene are returned with their relative IDs (which are copies of the original characters in H and ADV scenes; in maker nothing is returned since there is no actor).
    /// </summary>
    public static IEnumerable<KeyValuePair<int, Actor>> GetCurrentActors(bool mainInstances)
    {
        if (InsideMaker)
        {
            if (mainInstances)
            {
                var actor = CharacterCreation.HumanCustom.Instance.HumanData.About.FindMainActorInstance();
                if (actor.Value != null)
                    return new[] { actor };
            }

            return Enumerable.Empty<KeyValuePair<int, Actor>>();
        }

        if (SV.H.HScene.Active())
        {
            // HScene.Actors contains copies of the actors
            if (mainInstances)
                return SV.H.HScene._instance.Actors.Select(FindMainActorInstance).Where(x => x.Value != null);
            else
                return SV.H.HScene._instance.Actors.Select((ha, i) => new KeyValuePair<int, Actor>(i, ha.Actor)).Where(x => x.Value != null);
        }

        var talkManager = Manager.TalkManager._instance;
        if (talkManager != null && ADV.ADVManager._instance?.IsADV == true)
        {
            var npcs = new List<KeyValuePair<int, Actor>>
            {
                // PlayerHi and Npc1-4 contain copies of the Actors
                new(0,talkManager.Npc1),
                new(1,talkManager.Npc2),
                new(2,talkManager.Npc3),
                new(3,talkManager.Npc4),
                new(4,talkManager.PlayerHi),
            }.AsEnumerable();
            if (mainInstances)
                npcs = npcs.Select(pair => pair.Value.FindMainActorInstance());
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
    public static KeyValuePair<int, Actor> FindMainActorInstance(this SV.H.HActor x) => x?.Actor.FindMainActorInstance() ?? default;

    /// <summary>
    /// Get the main character instance of the actor (the one that is visible on the main map and saved to the save file).
    /// </summary>
    public static KeyValuePair<int, Actor> FindMainActorInstance(this Actor x) => x?.charFile.About.FindMainActorInstance() ?? default;

    /// <summary>
    /// Get the main character instance of the actor (the one that is visible on the main map and saved to the save file).
    /// TODO: Find a better way to get the originals
    /// </summary>
    public static KeyValuePair<int, Actor> FindMainActorInstance(this HumanDataAbout x) => x == null ? default : Manager.Game.Charas.AsManagedEnumerable().FirstOrDefault(y => x.dataID == y.Value.charFile.About.dataID);
#endif

#if AC
    // IDHI: For Aicomi HideCatetory is a flag H = 1, Bath = 2, Overnight = 4

    public static void SetHiddenAccessoryState(this Human self, HideCategory category, bool show, bool invertH = false)
    {
        var flags = new HideCategoryFlag[] { HideCategoryFlag.H, HideCategoryFlag.Bath, HideCategoryFlag.Overnight };

        SetAccessoryStateHidden(self, flags[(int)category], show, invertH);
    }

    public static void SetAccessoryStateHidden(this Human self, HideCategoryFlag category, bool show, bool invertH = false)
    {
        if (self == null)
        {
            return;
        }

        if (invertH && category != HideCategoryFlag.H)
        {
            invertH = false;
        }

        var accessories = self.coorde._nowCoordinate.Accessory.parts;

        for (var slot = 0; slot < accessories.Length; slot++)
        {
            var acc = accessories[slot];
            var categorySet = acc.hideCategory.IsHideCategory(category);
            if (invertH)
            {
                categorySet = !categorySet;
            }

            if (categorySet)
            {
                self.acs.SetAccessoryState(slot, show);
            }
        }
    }

    #region HideCategory
    public enum HideCategoryFlag
    {
        None = 0,
        H = 1,
        Bath = 2,
        Overnight = 4
    }

    private static readonly HideCategoryFlag[] flags =
        new HideCategoryFlag[] { HideCategoryFlag.H, HideCategoryFlag.Bath, HideCategoryFlag.Overnight };

    public static bool IsHideCategory(this int self, HideCategory category)
        => IsHideCategory(self, flags[(int)category]);

    internal static bool IsHideCategory(this int self, HideCategoryFlag category)
        => (self & (int)category) == (int)category;
    #endregion
#endif
}
