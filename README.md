# ClothingStateMenu
A BepInEx plugin for some games made by Illusion/Illgames (Koikatu, Koikatsu Party, EmotionCreators, SamabakeScramble) that adds ability to toggle state of clothes and accessories of characters anywhere in maker and during gameplay. This makes it easy to see how clothes look when partially removed (useful for making clothes overlays) and to make partially dressed character card preview images.

## How to use
Installation instructions are different depending on whether the game uses mono or IL2CPP runtime. The functionality of the plugin is mostly the same between the two, but there are some differences.

### Mono (KK, KKS, EC)
1. Install the latest versions of [BepInEx v5](https://github.com/BepInEx/BepInEx), [ModdingAPI](https://github.com/ManlyMarco/KKAPI) and [MoreAccessories](https://github.com/jalil49/MoreAccessories) for your game are installed, and your game is updated.
2. Download the latest release.
3. Extract the release to your game root. The plugin .dll should end up inside your 'BepInEx\plugins\ConfigurationManager' folder. Overwrite if asked.
4. Remove BepInEx\ClothingStateMenu.dll if you have it (this is the old version).
5. Start character maker. You should see a new checkbox in the bottom right corner. You can also talk to someone, then press Tab+Shift and a new window should appear. You can configure this plugin (including the hotkey) in plugin settings by searching for its name.

### IL2CPP (SVS)
1. Install the latest versions of [BepInEx v6](https://builds.bepis.io/bepinex_be) and [BepInEx.ConfigurationManager.IL2CPP](https://github.com/BepInEx/BepInEx.ConfigurationManager) for your game are installed, and your game is updated.
2. Download the latest release.
3. Extract the release to your game root. The plugin .dll should end up inside your 'BepInEx\plugins\ConfigurationManager' folder. Overwrite if asked.
4. Remove BepInEx\ClothingStateMenu.dll if you have it (this is an old version).
5. Start character maker or talk to someone, then press Tab+Shift and a new window should appear. You can configure this plugin (including the hotkey) in plugin settings by searching for its name.

![preview](https://user-images.githubusercontent.com/39247311/54150673-51efb780-4439-11e9-9f4f-e682def8e173.png)

## Credits
Based on the original ClothingStateMenu plugin made by essu.
