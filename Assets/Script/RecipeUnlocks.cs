using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recepty z kampaně. Každý je zamčený (není v knize receptů ani v TimeMode/LifeMode), dokud hráč
/// nedohraje level nebo konec kampaně, kde se poprvé objeví (unlocksRecipes v campaign.json).
/// Recepty z codexOnlyRecipes se odemknou jen do knihy receptů. Ostatní recepty jsou volné vždy.
/// Odemčení se ukládá do PlayerPrefs a vydrží i reset postupu v kampani.
/// </summary>
public static class RecipeUnlocks
{
    private const string Prefix = "RecipeUnlocked_";

    private static HashSet<string> gated;
    private static HashSet<string> codexOnly;

    private static void EnsureLoaded()
    {
        if (gated != null) return;
        gated = new HashSet<string>();
        codexOnly = new HashSet<string>();

        var data = CampaignData.Load();
        if (data == null) return;

        foreach (var level in data.levels)
        {
            if (level.unlocksRecipes == null) continue;
            gated.UnionWith(level.unlocksRecipes);
            // Postup uložený dřív, než existovalo odemykání receptů.
            if (CampaignProgress.IsCompleted(level.id)) SetFlags(level.unlocksRecipes);
        }
        foreach (var ending in data.endings)
        {
            if (ending.unlocksRecipes == null) continue;
            gated.UnionWith(ending.unlocksRecipes);
            if (CampaignProgress.HasSeenEnding(ending.id)) SetFlags(ending.unlocksRecipes);
        }
        if (data.codexOnlyRecipes != null) codexOnly.UnionWith(data.codexOnlyRecipes);
    }

    private static void SetFlags(IEnumerable<string> names)
    {
        foreach (var name in names) PlayerPrefs.SetInt(Prefix + name, 1);
    }

    /// <summary>Patří recept mezi ty, které se odemykají v kampani?</summary>
    public static bool IsCampaignRecipe(string name)
    {
        EnsureLoaded();
        return gated.Contains(name);
    }

    /// <summary>Smí se recept ukázat v knize receptů?</summary>
    public static bool IsUnlocked(string name)
    {
        EnsureLoaded();
        return !gated.Contains(name) || PlayerPrefs.GetInt(Prefix + name, 0) == 1;
    }

    /// <summary>Smí se recept vařit v TimeMode a LifeMode?</summary>
    public static bool IsAvailableInModes(string name)
    {
        EnsureLoaded();
        return IsUnlocked(name) && !codexOnly.Contains(name);
    }

    /// <summary>Odemkne recepty a vrátí ty, které byly do teď zamčené.</summary>
    public static List<string> Unlock(IEnumerable<string> names)
    {
        EnsureLoaded();
        var fresh = new List<string>();
        if (names == null) return fresh;
        foreach (var name in names)
        {
            if (PlayerPrefs.GetInt(Prefix + name, 0) == 1) continue;
            PlayerPrefs.SetInt(Prefix + name, 1);
            fresh.Add(name);
        }
        if (fresh.Count > 0) PlayerPrefs.Save();
        return fresh;
    }
}
