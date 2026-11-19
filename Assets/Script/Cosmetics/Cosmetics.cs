using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum CosmeticPurchaseResult
{
    Invalid,
    Locked,
    NotEnoughGems,
    SaveFailed,
    Purchased,
    Selected
}

/// <summary>
/// Jediné místo, které ví, co hráč vlastní a co má vybrané.
/// Data: Application.persistentDataPath/cosmetics.json (+ .bak záloha).
///
/// Při prvním spuštění nové verze se jednorázově převedou stará data:
///  - vlastnictví z persistentDataPath/Items/{RoomsData,CauldronsData,FiresData}.json
///  - výběr z PlayerPrefs "RoomName" / "Cauldron" / "Fire" (formát "Cauldron3_0")
/// Stará data se nemažou.
/// </summary>
public static class Cosmetics
{
    public const string GemsPrefsKey = "GemsCount";
    private const string SaveFileName = "cosmetics.json";
    private const int SaveVersion = 1;

    [Serializable]
    private class SaveData
    {
        public int version = SaveVersion;
        public List<string> owned = new List<string>();
        public string selectedRoom;
        public string selectedCauldron;
        public string selectedFire;
    }

    private static CosmeticDatabase database;
    private static SaveData data;
    private static HashSet<string> ownedSet;

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    private static string BackupPath => SavePath + ".bak";

    // Kvůli Enter Play Mode bez domain reloadu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        database = null;
        data = null;
        ownedSet = null;
    }

    public static CosmeticDatabase Database
    {
        get
        {
            if (database == null)
            {
                database = Resources.Load<CosmeticDatabase>(CosmeticDatabase.ResourcesPath);
                if (database == null)
                {
                    Debug.LogError($"[Cosmetics] Chybí Resources/{CosmeticDatabase.ResourcesPath}.asset");
                    database = ScriptableObject.CreateInstance<CosmeticDatabase>();
                }
            }
            return database;
        }
    }

    public static int Gems => PlayerPrefs.GetInt(GemsPrefsKey, 0);

    /// <summary>Přičte gemy (odměna za hru) a hned uloží.</summary>
    public static void AddGems(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(GemsPrefsKey, Gems + amount);
        PlayerPrefs.Save();
    }

    public static IReadOnlyList<CosmeticItem> GetItems(CosmeticType type) => Database.GetItems(type);

    public static bool IsOwned(CosmeticItem item)
    {
        if (item == null) return false;
        EnsureLoaded();
        return item.OwnedByDefault || ownedSet.Contains(item.Id)
            || (item.CampaignReward && CampaignProgress.IsCampaignFinished);
    }

    /// <summary>Zamčená = nejde koupit ani vybrat (odměna za kampaň bez ceny, dokud hráč kampaň nedohraje).</summary>
    public static bool IsLocked(CosmeticItem item)
    {
        return item != null && item.CampaignReward && item.Price <= 0 && !IsOwned(item);
    }

    public static bool IsSelected(CosmeticItem item)
    {
        return item != null && GetSelected(item.Type) == item;
    }

    /// <summary>Vybraná položka; nikdy nevrací null, pokud databáze daný typ obsahuje.</summary>
    public static CosmeticItem GetSelected(CosmeticType type)
    {
        EnsureLoaded();
        var item = Database.FindById(type, GetSelectedId(type));
        if (item != null && IsOwned(item)) return item;

        // Nikdy nevracet nevlastněnou položku (jinak by ji hra zobrazila zdarma).
        var fallback = Database.GetDefault(type);
        if (fallback != null && IsOwned(fallback)) return fallback;
        foreach (var candidate in GetItems(type))
            if (IsOwned(candidate)) return candidate;

        if (fallback != null)
            Debug.LogWarning($"[Cosmetics] Žádná položka typu {type} není vlastněná – zkontroluj ownedByDefault v databázi.");
        return fallback;
    }

    public static int GetSelectedIndex(CosmeticType type)
    {
        var selected = GetSelected(type);
        var items = GetItems(type);
        for (int i = 0; i < items.Count; i++)
            if (items[i] == selected) return i;
        return 0;
    }

    /// <summary>Koupí položku (pokud ji hráč nemá) a vybere ji.</summary>
    public static CosmeticPurchaseResult BuyOrSelect(CosmeticItem item)
    {
        if (item == null) return CosmeticPurchaseResult.Invalid;
        EnsureLoaded();

        if (IsLocked(item)) return CosmeticPurchaseResult.Locked;

        bool buying = !IsOwned(item);
        int gems = Gems;
        if (buying && gems < item.Price) return CosmeticPurchaseResult.NotEnoughGems;

        string previousSelected = GetSelectedId(item.Type);
        if (buying)
        {
            ownedSet.Add(item.Id);
            data.owned.Add(item.Id);
        }
        SetSelectedIdOn(data, item.Type, item.Id);

        // Nejdřív uložit vlastnictví; gemy se odečtou, jen když se uložení povedlo.
        if (!Save())
        {
            if (buying)
            {
                ownedSet.Remove(item.Id);
                data.owned.Remove(item.Id);
            }
            SetSelectedIdOn(data, item.Type, previousSelected);
            return CosmeticPurchaseResult.SaveFailed;
        }

        if (!buying) return CosmeticPurchaseResult.Selected;

        PlayerPrefs.SetInt(GemsPrefsKey, gems - item.Price);
        PlayerPrefs.Save();
        return CosmeticPurchaseResult.Purchased;
    }

    // ------------------------------------------------------------------
    // Načítání / ukládání
    // ------------------------------------------------------------------

    private static string TempPath => SavePath + ".tmp";

    private static void EnsureLoaded()
    {
        if (data != null) return;

        data = TryRead(SavePath);
        if (data == null)
        {
            // Hlavní soubor chybí nebo je poškozený. Pokud chybí, mohla aplikace spadnout uprostřed
            // File.Replace – pak je .tmp kompletní a nejnovější.
            if (!File.Exists(SavePath)) data = TryRead(TempPath);
            data ??= TryRead(BackupPath);

            if (data != null)
            {
                // Obnovit hlavní soubor hned, aby další Save() nepřepsal dobrou zálohu poškozenými daty.
                Quarantine(SavePath);
                Save();
            }
        }

        if (data == null)
        {
            // Nečitelné soubory nepřepisovat – odložit stranou pro případnou ruční záchranu.
            Quarantine(SavePath);
            Quarantine(BackupPath);
            data = MigrateLegacy();
            Save();
        }

        data.owned ??= new List<string>();
        ownedSet = new HashSet<string>(data.owned);
    }

    private static SaveData TryRead(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cosmetics] Nelze načíst {path}: {e.Message}");
            return null;
        }
    }

    private static void Quarantine(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Move(path, $"{path}.corrupt-{DateTime.UtcNow.Ticks}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cosmetics] Nelze odložit {path}: {e.Message}");
        }
    }

    /// <summary>Zapíše data přes .tmp a File.Replace (stará verze → .bak).</summary>
    private static bool Save()
    {
        try
        {
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath))
                File.Replace(TempPath, SavePath, BackupPath);
            else
                File.Move(TempPath, SavePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cosmetics] Uložení selhalo: {e.Message}");
            return false;
        }
    }

    private static string GetSelectedId(CosmeticType type)
    {
        switch (type)
        {
            case CosmeticType.Room: return data.selectedRoom;
            case CosmeticType.Cauldron: return data.selectedCauldron;
            case CosmeticType.Fire: return data.selectedFire;
            default: return null;
        }
    }

    // ------------------------------------------------------------------
    // Migrace ze staré verze (RoomManager + ItemStateManager + PlayerPrefs)
    // ------------------------------------------------------------------

    private static readonly (CosmeticType type, string prefsKey, string fileName)[] Legacy =
    {
        (CosmeticType.Room, "RoomName", "RoomsData"),
        (CosmeticType.Cauldron, "Cauldron", "CauldronsData"),
        (CosmeticType.Fire, "Fire", "FiresData"),
    };

    [Serializable] private class LegacyItem { public string Id; public bool Owned; public bool Active; }
    [Serializable] private class LegacyCollection { public List<LegacyItem> Items = new List<LegacyItem>(); }

    private static SaveData MigrateLegacy()
    {
        var result = new SaveData();
        var owned = new HashSet<string>();

        foreach (var (type, prefsKey, fileName) in Legacy)
        {
            string activeFromFile = null;
            string path = Path.Combine(Application.persistentDataPath, "Items", fileName + ".json");
            try
            {
                if (File.Exists(path))
                {
                    var legacy = JsonUtility.FromJson<LegacyCollection>(File.ReadAllText(path));
                    if (legacy?.Items != null)
                    {
                        foreach (var item in legacy.Items)
                        {
                            if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                            if (item.Owned) owned.Add(item.Id);
                            if (item.Owned && item.Active) activeFromFile = item.Id;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Cosmetics] Migrace {path} selhala: {e.Message}");
            }

            // PlayerPrefs bylo to, co stará hra skutečně zobrazovala → má přednost.
            string selected = ParseLegacyId(PlayerPrefs.GetString(prefsKey, "")) ?? activeFromFile;
            if (!string.IsNullOrEmpty(selected))
            {
                owned.Add(selected); // vybrané = zaplacené, i kdyby chyběl záznam v souboru
                SetSelectedIdOn(result, type, selected);
            }
        }

        result.owned = new List<string>(owned);
        Debug.Log($"[Cosmetics] Migrace dokončena, vlastněno: {string.Join(", ", result.owned)}");
        return result;
    }

    /// <summary>"Cauldron3_0" → "Cauldron3"; "" → null.</summary>
    private static string ParseLegacyId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        int underscore = value.LastIndexOf('_');
        string id = underscore > 0 ? value.Substring(0, underscore) : value;
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    private static void SetSelectedIdOn(SaveData target, CosmeticType type, string id)
    {
        switch (type)
        {
            case CosmeticType.Room: target.selectedRoom = id; break;
            case CosmeticType.Cauldron: target.selectedCauldron = id; break;
            case CosmeticType.Fire: target.selectedFire = id; break;
        }
    }
}
