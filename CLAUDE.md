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
- Build scenes, in order: `Menu`, `TimeMode`, `LifeMode`. `Campaign.unity` exists but is **not** in Build Settings, even though `MenuController.LoadCampaign()` loads it.
- Android release artifacts (`magic.aab`, `user.keystore`) are in the repo root. Do not modify or commit the keystore.

## Code layout

Game code is only in two folders. Everything else under `Assets/` is third-party asset packs, samples, Google Mobile Ads and External Dependency Manager.
- `Assets/Script/`: in-game (brewing scene) logic.
- `Assets/ScriptIntroducionMenu/`: main menu, shop, leaderboards, achievements UI, settings and the herbal codex.

## Architecture

**Data-driven content via `Resources/`.** Almost all content is loaded at runtime by string name with `Resources.Load`, so asset file names work as identifiers:
- `Resources/Levels/potions.json`: every recipe (`Recept`: name, description, a list of `ReceptStep`, where each step is herbs[] plus an element). `Resources/Levels/levelTime.json` picks which herbs and recipes a level uses. The schema classes (`LevelData`, `Recept`, `ReceptStep`, `AllReceptsData`) live in `Script/SetLevel.cs` and are parsed with `JsonUtility`.
- Sprites are looked up by name: `Potions/<recipe name>`, `Herbs/<herb name>`, `Spells/<element>`, `Cauldrons/…`, `Fires/…`, `Rooms/…`. If you rename a recipe, herb or element, rename the matching sprite too.
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
- Shop items (rooms, cauldrons, fires) use `ItemStateManager`. It stores JSON at `Application.persistentDataPath/Items/<name>.json` and generates items with `GenerateItems` as `<baseName><i>` with tiered prices. The root `Items/*.json` files are copies or samples of that data; the game does not read them.
- Selected cosmetics are saved as `"<Name><n>_<m>"`. Code strips the suffix with `Substring(0, len-2)` before loading the sprite (see `RoomManager`, `CreatePotion.LoadSpriteFromResources`), so keep that format.

**Ads.** `AdIns.cs` (interstitial) and `Rewarded.cs` use Google Mobile Ads with production ad unit IDs. `NoAds` in PlayerPrefs turns ads off.

## Gotchas

- `ResultScoreScript` loads a scene named `"Game"`, and no scene has that name.
- `AchievementNotification.cs` has `using NUnit.Framework;` in runtime code. It works in the Editor but can break player builds if the test framework isn't available.
- `Assets/AssistantCustomInstructions.txt` is empty.
