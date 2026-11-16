using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ItemStateManager
{
    private string _filePath = Path.Combine(Application.persistentDataPath, "Items");
    private ItemStateCollection _data;


    public ItemStateManager(string fileName)
    {
   
        _filePath = Path.Combine(_filePath, fileName+".json");

        EnsureFileExists();
        Load();
    }

    // --------------------------------------------------
    // Inicializace souboru
    // --------------------------------------------------
    private void EnsureFileExists()
    {
        string directory = Path.GetDirectoryName(_filePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_filePath))
        {
            _data = new ItemStateCollection();
            Save();
        }
    }
    // --------------------------------------------------
    // Pøidání itemu (jen pokud neexistuje)
    // --------------------------------------------------
    public void AddIfNotExists(List<ItemState> items)
    {

        bool modified = false;

        foreach (var item in items)
        {
            bool exists = _data.Items.Any(i => i.Id == item.Id);

            if (!exists)
            {
                _data.Items.Add(item);
                modified = true;
            }
        }

        if (modified)
        {
            Save();
        }
    }

    // --------------------------------------------------
    // Získání itemu podle ID
    // --------------------------------------------------
    public ItemState GetById(string id)
    {
        return _data.Items.FirstOrDefault(i => i.Id == id);
    }

    // --------------------------------------------------
    // Save / Load
    // --------------------------------------------------
    private void Save()
    {
        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(_filePath, json);
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _data = new ItemStateCollection();
            return;
        }

        string json = File.ReadAllText(_filePath);

        if (string.IsNullOrEmpty(json))
            _data = new ItemStateCollection();
        else
            _data = JsonUtility.FromJson<ItemStateCollection>(json);
    }


    public bool BuyOrActivate(string id)
    {
        var targetItem = _data.Items.FirstOrDefault(i => i.Id == id);

        if (targetItem == null)
            return false;

        int currentGems = PlayerPrefs.GetInt("GemsCount", 0);

        // Pokud není koupený -> zkusíme koupit
        if (!targetItem.Owned)
        {
            if (currentGems < targetItem.Price)
            {
                // Nemá dost gemù
                return false;
            }

            // Odeèteme gemy
            currentGems -= targetItem.Price;
            PlayerPrefs.SetInt("GemsCount", currentGems);
            PlayerPrefs.Save();

            targetItem.Owned = true;
        }

        // Deaktivujeme všechny ostatní
        foreach (var item in _data.Items)
        {
            item.Active = false;
        }

        // Aktivujeme cílový
        targetItem.Active = true;

        Save();

        return true;
    }

    public void GenerateItems(int count, string baseName, string baseId, int basePrice)
    {
        List<ItemState> items = new List<ItemState>();

        for (int i = 0; i <= count; i++)
        {
            // Kolikrát už jsme pøekroèili pìtici
            int group = i / 5;

            // Cena: basePrice + 50% za každou pìtici
            int price = basePrice + (int)(basePrice * 0.5 * group);

            // Id a Name
            string id = $"{baseId}{i}";
            string name = $"{baseName}{i}";

            bool owned = (i == 0);   // první položka koupená
            bool active = (i == 0);  // první položka aktivní

            items.Add(new ItemState(id, name, price, owned, active));
        }

        AddIfNotExists(items);
    }
}