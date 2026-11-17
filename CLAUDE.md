# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Komunikace

S uživatelem vždy komunikuj česky. Kód a identifikátory piš dál anglicky, jak je v projektu zvykem.

## Project

Magic Alchemist Master is a 2D mobile (Android, `com.PalAthGame.unity.MagicAlchemistMaster`) potion-brewing game. It runs on Unity **6000.3.6f1** with URP and the 2D feature set, uses uGUI + TextMeshPro for UI, and has no assembly definitions, so all game code compiles into `Assembly-CSharp`. Code comments, log messages and some UI strings are in Czech. Several older files are saved in a non-UTF-8 codepage (they show `�` for Czech characters). Leave their encoding alone unless you have to edit them.

## Commands

There is no test suite and no lint setup. Compile, run and build through the Unity Editor:
- Use the `unity` CLI (on PATH) or the `unity-editor-mcp` tools to talk to a running Editor: `script_recompile`, `console_get`, `editor_playmode_start`, `build_run`. Check the console after editing scripts, because compile errors only show up there.
- `Packages/com.unity.pipeline` (experimental) and `com.coplaydev.unity-mcp` are installed for Editor automation.
- Build scenes, in order: `Menu`, `TimeMode`, `LifeMode`, `Campaign`, `CampaignStory`.
- Android release artifacts (`magic.aab`, `user.keystore`) are in the repo root. Do not modify or commit the keystore.

## Code layout

Game code is only in two folders. Everything else under `Assets/` is third-party asset packs, samples, Google Mobile Ads and External Dependency Manager.
- `Assets/Script/`: in-game (brewing scene) logic.
- `Assets/ScriptIntroducionMenu/`: main menu, shop, leaderboards, achievements UI, settings and the herbal codex.

## Architecture

**Data-driven content via `Resources/`.** Almost all content is loaded at runtime by string name with `Resources.Load`, so asset file names work as identifiers:
- `Resources/Levels/potions.json`: every recipe (`Recept`: name, description, a list of `ReceptStep`, where each step is herbs[] plus an element). `Resources/Levels/levelTime.json` picks which herbs and recipes a level uses. The schema classes (`LevelData`, `Recept`, `ReceptStep`, `AllReceptsData`) live in `Script/SetLevel.cs` and are parsed with `JsonUtility`.
- Sprites are looked up by name: `Potions/<recipe name>`, `Herbs/<herb name>`, `Spells/<element>`, `Cauldrons/…`, `Fires/…`, `Rooms/…`. If you rename a recipe, herb or element, rename the matching sprite too.
- Campaign recipes are locked until the player finishes the campaign level (or ending) that lists them in `unlocksRecipes` in `Resources/Campaign/campaign.json`. `Script/RecipeUnlocks.cs` filters the herbal codex and adds unlocked recipes (and their herbs) to TimeMode/LifeMode in `SetLevel`. Recipes in `codexOnlyRecipes` (The Usurper's Draught) never reach the modes.
- **Campaign flow.** `MenuController.LoadCampaign()` opens `CampaignStory` (`Script/Campaign/CampaignController.cs`: level select, title card, dialogue). After the intro it calls `CampaignSession.Begin(id)` and loads the `Campaign` scene. There `CampaignGame` adds itself on `sceneLoaded` and applies the level's `game` rules from `campaign.json` (recipes, plate herbs in slot order, goal `potions`/`score`/`survive`, time limit, lives, customers, poison, the finale's Mandrake substitution and ending). It drives its own clock (it disables `TimerInGame`) and draws its own HUD. A win reloads `CampaignStory`, which plays the ending (finale only) and the outro. With no session, the `Campaign` scene is the recipe test scene (`levelCampaignTest.json`).
- `Resources/Achievements/achievements.json` defines achievements. Achievement ids are hard-coded at the unlock call sites.

**Brewing loop (`Script/GameManager.cs`).** `SetLevel` loads the recipes. `GameManager.ExecuteReceptSteps()` is a coroutine that picks a random recipe and, for each step, waits on one of two input flags:
- `SetUsedHerbs()`, called from the herb drag item `HerbItemScript`.
- `SetPecentSpell()`, called from the spell tracing surface.

A wrong herb, or a spell traced under 10% accuracy, marks the potion "Broken". When a potion finishes, `Update()` restarts the coroutine. A brewed potion feeds score, gems, achievements, `CreatePotion` (spawns the potion visual) and `PoisonEffect`. `EndGame(mode)` is called by `TimerInGame` (TimeMode) or `HpScript` (LifeMode). It submits the score to the UGS leaderboard `Time_Score_Leaderboard` or `Life_Score_Leaderboard`.

**Spell tracing.** `Script/DrawLayerScript.cs` contains the class **`SpellDrawScript`**; the file name does not match the class name. It paints onto a `Texture2D` on a `RawImage`, takes the black pixels of the reference sprite as the target, and scores coverage minus overdraw. The dictionaries `spellMove` (strokes needed per element) and `spellColor` must contain every element used in `potions.json`.

**Singletons.**
- `AchievementsManagerScript.Instance`: lives per scene.
- `LeaderBoardNonDestroyObject.Instance`: `DontDestroyOnLoad`. It initializes Unity Services and anonymous auth, and wraps the Leaderboards API.

**Persistence.**
- `PlayerPrefs` holds settings (`SoundActive` / `MusicActive` stored as the strings "1"/"0"), `GemsCount`, `PlayerName`, achievement flags (key = achievement id) and the selected cosmetics.
- **Cosmetics (rooms, cauldrons, fires)** are `CosmeticItem` ScriptableObjects in `Assets/Cosmetics/Items/`, listed in `Assets/Resources/Cosmetics/CosmeticDatabase.asset`. Each item holds its sprite, price, default-owned flag and, for fires, the flame gradient. All ownership and selection goes through the static `Script/Cosmetics/Cosmetics.cs` (`GetSelected`, `IsOwned`, `BuyOrSelect`). That data is saved to `persistentDataPath/cosmetics.json`, with a `.bak` backup. Shop tabs use `RoomManager` (the name is kept for prefab refs), set to a `CosmeticType`.
- **Never change a `CosmeticItem.Id` once it has shipped.** Ids are stored in player saves.
- To add a cosmetic, drop `CauldronN.png` (or `RoomN` / `FireN`) into its `Resources/` folder and run **Tools > Magic Alchemist > Build Cosmetic Database**. The tool creates any missing items without overwriting your edits.
- **Legacy migration:** builds before the database stored ownership in `persistentDataPath/Items/*Data.json` and the selection in PlayerPrefs `RoomName` / `Cauldron` / `Fire`, as `"Cauldron3_0"`. `Cosmetics.MigrateLegacy()` converts this once, when no `cosmetics.json` exists yet. Keep that code so players updating from old versions don't lose purchases. In this Editor `Application.persistentDataPath` is empty, so the Editor's own save data (`Items/`, `cosmetics.json`) ends up in the project root. Those paths are gitignored.

**Ads.** `AdIns.cs` (interstitial) and `Rewarded.cs` use Google Mobile Ads with production ad unit IDs. `NoAds` in PlayerPrefs turns ads off.

## Gotchas

- `ResultScoreScript` loads a scene named `"Game"`, and no scene has that name.
- `AchievementNotification.cs` has `using NUnit.Framework;` in runtime code. It works in the Editor but can break player builds if the test framework isn't available.
- `Assets/AssistantCustomInstructions.txt` is empty.
