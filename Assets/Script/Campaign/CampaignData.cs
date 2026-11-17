using System.Collections.Generic;
using UnityEngine;

// Schéma Resources/Campaign/campaign.json (čte se přes JsonUtility).

[System.Serializable]
public class CampaignLine
{
    public string who;      // id postavy, "narrator" = vypravěč bez portrétu
    public string text;
    public string cs;       // český překlad (pomůcka pro vývoj, viz CampaignText)
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
    public string chapterCs;
    public string title;
    public string titleCs;
    public string background; // Street / Workshop / Clinic / WarCamp / Dungeon
    public string goal;
    public string goalCs;
    public List<CampaignLine> intro;
    public List<CampaignLine> outro;
}

[System.Serializable]
public class CampaignEnding
{
    public string id;           // failure / obedience / betrayal
    public string title;
    public string titleCs;
    public string achievement;
    public List<CampaignLine> lines;
}

[System.Serializable]
public class CampaignData
{
    public List<CampaignCharacter> characters;
    public List<CampaignLevel> levels;
    public List<CampaignEnding> endings;
    public string bookPage1;
    public string bookPage1Cs;
    // Druhá strana knihy (varování před Mandrake). Ve hře se teď neukazuje, mistr knihu
    // vytrhne dřív, než ji hráč otočí. Zůstává jako podklad pro děj.
    public string bookPage2;
    public string bookPage2Cs;
    public string allEndingsAchievement;

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
/// Český překlad pod anglickým textem kampaně. Pomůcka pro vývoj: před vydáním vypnout
/// (ShowCzech = false), nebo nahradit skutečnou lokalizací.
/// </summary>
public static class CampaignText
{
    public static bool ShowCzech = true;

    // Bez vlastní barvy: přebírá barvu textového pole (tmavá na bublině, zlatá na kartách),
    // takže je čitelná na světlém i tmavém podkladu.
    private const string CsStyle = "<size=76%><b><i>";
    private const string CsStyleEnd = "</i></b></size>";

    /// <summary>Anglický text a pod ním menší tučná kurzíva s češtinou.</summary>
    public static string With(string en, string cs)
    {
        if (!ShowCzech || string.IsNullOrEmpty(cs)) return en;
        return $"{en}\n{CsStyle}{cs}{CsStyleEnd}";
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
