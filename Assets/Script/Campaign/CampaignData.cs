using System.Collections.Generic;
using UnityEngine;

// Schéma Resources/Campaign/campaign.json (čte se přes JsonUtility).

[System.Serializable]
public class CampaignLine
{
    public string who;      // id postavy, "narrator" = vypravěč bez portrétu
    public string text;
    public string action;   // "book" = scéna s knihou, "choice" = volba konce (prototyp)
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
    public string background; // Street / Workshop / Clinic / WarCamp / Dungeon
    public string goal;
    public List<string> unlocksRecipes; // recepty, které se dohráním levelu odemknou (viz RecipeUnlocks)
    public List<CampaignLine> intro;
    public List<CampaignLine> outro;
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
    public string bookPage1;
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

    /// <summary>Smaže postup v kampani (achievementy zůstanou).</summary>
    public static void Reset(IEnumerable<CampaignLevel> levels, IEnumerable<CampaignEnding> endings)
    {
        PlayerPrefs.DeleteKey(ReachedKey);
        foreach (var l in levels) PlayerPrefs.DeleteKey(DonePrefix + l.id);
        foreach (var e in endings) PlayerPrefs.DeleteKey(EndingPrefix + e.id);
        PlayerPrefs.Save();
    }
}
