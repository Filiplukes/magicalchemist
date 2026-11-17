using System.Collections.Generic;
using UnityEngine;

// Schéma Resources/Campaign/campaign.json (čte se přes JsonUtility).

[System.Serializable]
public class CampaignLine
{
    public string who;      // id postavy, "narrator" = vypravěč bez portrétu
    public string text;
    public string action;   // "book" = scéna s knihou, "background" = změna pozadí (viz background)
    public string background; // pro action "background": nové pozadí, prolne se přes tmu
}

[System.Serializable]
public class CampaignCharacter
{
    public string id;
    public string name;
    public string portrait; // název souboru v Resources/Campaign/Portraits
    public string side;     // "left" nebo "right"
}

[System.Serializable]
public class CampaignLevel
{
    public int id;
    public string chapter;
    public string title;
    public string background; // pozadí na začátku levelu, viz CampaignBackgrounds
    public string goal;
    public CampaignGameRules game;      // hratelnost levelu (scéna Campaign), viz CampaignGame
    public List<string> unlocksRecipes; // recepty, které se dohráním levelu odemknou (viz RecipeUnlocks)
    public List<CampaignLine> intro;
    public List<CampaignLine> outro;
}

/// <summary>
/// Pravidla hraní levelu kampaně. Nevyplněná čísla (0) znamenají "bez omezení".
/// Cíl: "potions" (uvařit target lektvarů), "score" (získat target bodů), "survive" (vydržet timeLimit).
/// </summary>
[System.Serializable]
public class CampaignGameRules
{
    public List<string> recipes;    // recepty levelu; stejný název víckrát = častěji
    public List<string> herbs;      // byliny v pořadí slotů talíře (zleva doprava, shora dolů)
    public string goal;
    public int target;
    public int minQuality;          // počítají se jen lektvary s kvalitou aspoň takovou (Divine = 65)
    public int minSteps;            // počítají se jen recepty s aspoň tolika kroky
    public float timeLimit;         // sekundy, 0 = bez časového limitu
    public int lives;               // 0 = bez životů; život stojí zkažený lektvar a odešlý zákazník
    public bool customers;          // objednávky od zákazníků (pacienti, družina)
    public float customerPatience;  // násobič trpělivosti zákazníků, 0 = 1
    public List<string> customerPortraits; // id postav místo náhodných zákazníků
    public bool sequential;         // recepty popořadě místo náhodně
    public bool poison;             // otrava z výparů (až od levelu 5, kdy ji mistr vysvětlí)
    public float poisonPerSecond;   // otrava stoupá sama od sebe
    public bool poisonKills;        // plná otrava = prohra
    // Finále: bylina substituteHerb smí nahradit substituteFor a změní konec (substituteEnding).
    public string substituteFor;
    public string substituteHerb;
    public string substituteEnding;
    // Level končí jedním z konců kampaně: zkažený lektvar nebo kvalita pod failBelowQuality
    // = failEnding, jinak successEnding (nebo substituteEnding).
    public bool endsCampaign;
    public int failBelowQuality;
    public string failEnding;
    public string successEnding;

    public bool HasGameplay => recipes != null && recipes.Count > 0;
}

[System.Serializable]
public class CampaignEnding
{
    public string id;           // failure / obedience / betrayal
    public string title;
    public string achievement;
    public List<string> unlocksRecipes;
    public List<CampaignLine> lines;
}

[System.Serializable]
public class CampaignData
{
    public List<CampaignCharacter> characters;
    public List<CampaignLevel> levels;
    public List<CampaignEnding> endings;
    public string bookPage1;  // text knihy v levelu 2; <page> ho dělí na levou a pravou stránku
    // Druhá strana knihy (poznámky o krvi obdarovaného Světlem). V kampani se nikdy neukáže,
    // mistr knihu vytrhne dřív. Celý text uvidí hráč v knize receptů po konci Root of Betrayal.
    public string bookPage2;
    public string allEndingsAchievement;
    public List<string> codexOnlyRecipes; // odemknou se jen do knihy receptů, nikdy do TimeMode/LifeMode

    public static CampaignData Load()
    {
        var json = Resources.Load<TextAsset>("Campaign/campaign");
        if (json == null)
        {
            Debug.LogError("Chybí Resources/Campaign/campaign.json");
            return null;
        }
        return JsonUtility.FromJson<CampaignData>(json.text);
    }
}

/// <summary>
/// Pozadí kampaně podle klíče. "Workshop" = pokoj, který si hráč vybral v obchodě, jinak obrázek
/// z Resources/Campaign/Backgrounds. Přípona "Night" (třeba "WorkshopNight") pozadí ztmaví do noci.
/// </summary>
public static class CampaignBackgrounds
{
    public const string Workshop = "Workshop";
    private const string NightSuffix = "Night";
    public static readonly Color NightTint = new Color(0.4f, 0.46f, 0.72f);

    /// <summary>Vrátí obrázek pozadí a barvu, kterou se má obarvit (bílá = beze změny).</summary>
    public static Sprite Resolve(string key, out Color tint)
    {
        tint = Color.white;
        if (string.IsNullOrEmpty(key)) key = Workshop;
        if (key.Length > NightSuffix.Length && key.EndsWith(NightSuffix, System.StringComparison.Ordinal))
        {
            tint = NightTint;
            key = key.Substring(0, key.Length - NightSuffix.Length);
        }

        Sprite sprite = key == Workshop ? null : Resources.Load<Sprite>("Campaign/Backgrounds/" + key);
        if (sprite == null)
        {
            var room = Cosmetics.GetSelected(CosmeticType.Room);
            if (room != null) sprite = room.Sprite;
        }
        return sprite;
    }
}

/// <summary>
/// Předává rozehraný level mezi scénami: CampaignStory (dialogy) → Campaign (hraní) → zpět.
/// Žije jen v paměti, po restartu hry se level hraje znovu od úvodu.
/// </summary>
public static class CampaignSession
{
    public const string GameScene = "Campaign";
    public const string StoryScene = "CampaignStory";

    public static int LevelId { get; private set; }   // 0 = žádný level neběží
    public static bool Won { get; private set; }      // hraní skončilo úspěchem
    public static string Ending { get; private set; } // konec kampaně, který hraní zvolilo
    public static string Background { get; private set; } // pozadí, na kterém skončil úvod (hraní i závěr)
    /// <summary>Hráč po výhře zvolil „Další“: po závěru rovnou další level, ne výběr levelů.</summary>
    public static bool ContinueToNext { get; set; }

    public static bool IsPlaying => LevelId > 0;

    public static void Begin(int levelId, string background)
    {
        LevelId = levelId;
        Background = background;
        Won = false;
        Ending = null;
        ContinueToNext = false;
    }

    public static void Finish(string ending)
    {
        Won = true;
        Ending = ending;
    }

    public static void Clear()
    {
        LevelId = 0;
        Background = null;
        Won = false;
        Ending = null;
        ContinueToNext = false;
    }
}

/// <summary>Postup v kampani a zhlédnuté konce (PlayerPrefs).</summary>
public static class CampaignProgress
{
    private const string ReachedKey = "CampaignLevelReached";
    private const string DonePrefix = "CampaignDone_";
    private const string EndingPrefix = "CampaignEnding_";

    public static int Reached => Mathf.Max(1, PlayerPrefs.GetInt(ReachedKey, 1));
    public static bool IsUnlocked(int level) => level <= Reached;
    public static bool IsCompleted(int level) => PlayerPrefs.GetInt(DonePrefix + level, 0) == 1;

    public static void Complete(int level)
    {
        PlayerPrefs.SetInt(DonePrefix + level, 1);
        if (level + 1 > Reached) PlayerPrefs.SetInt(ReachedKey, level + 1);
        PlayerPrefs.Save();
    }

    public static bool HasSeenEnding(string id) => PlayerPrefs.GetInt(EndingPrefix + id, 0) == 1;

    public static void MarkEnding(string id)
    {
        PlayerPrefs.SetInt(EndingPrefix + id, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Zapíše výhru hned po hraní, ještě před závěrečným dialogem, aby se neztratila,
    /// když hráč během závěru aplikaci zavře. Recepty dorovná RecipeUnlocks při dalším
    /// spuštění, achievementy za konce CampaignController.SyncEndingAchievements.
    /// </summary>
    public static void RecordWin(int level, string endingId)
    {
        RecipeUnlocks.Preload(); // před zápisem, jinak by se nové recepty neoznámily
        if (!string.IsNullOrEmpty(endingId)) PlayerPrefs.SetInt(EndingPrefix + endingId, 1);
        Complete(level); // ukládá PlayerPrefs
    }

    /// <summary>Smaže postup v kampani (achievementy zůstanou). Jen pro Editor: Tools > Magic Alchemist.</summary>
    public static void Reset(IEnumerable<CampaignLevel> levels, IEnumerable<CampaignEnding> endings)
    {
        PlayerPrefs.DeleteKey(ReachedKey);
        foreach (var l in levels) PlayerPrefs.DeleteKey(DonePrefix + l.id);
        foreach (var e in endings) PlayerPrefs.DeleteKey(EndingPrefix + e.id);
        PlayerPrefs.Save();
    }
}
