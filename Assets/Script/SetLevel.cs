using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LevelData
{
    public List<string> herbs;
    public List<string> recepts;

}

[System.Serializable]
public class ReceptStep
{
    public List<string> herbs;
    public string element;
}

[System.Serializable]
public class Recept
{
    public string name;
    public string description;
    public List<ReceptStep> steps;
}

[System.Serializable]
public class AllReceptsData
{
    public List<Recept> potions;
}


public class SetLevel : MonoBehaviour
{
    public List<Image> herbSlots;
    public List<Recept> loadedRecepts; // ← zde uložíme vybrané recepty

    void Start()
    {
        LoadLevel("Levels/levelTime");
    }

    void LoadLevel(string levelFileName)
    {
        TextAsset jsonText = Resources.Load<TextAsset>(levelFileName);
        TextAsset jsonRecepts = Resources.Load<TextAsset>("Levels/potions");

        if (jsonText == null || jsonRecepts == null)
        {
            Debug.LogError("Missing JSON file!");
            return;
        }

        // Načti level data
        LevelData levelData = JsonUtility.FromJson<LevelData>(jsonText.text);

        // Načti všechy recepty
        AllReceptsData allRecepts = JsonUtility.FromJson<AllReceptsData>(jsonRecepts.text);

        // Najdi pouze ty recepty, co jsou uvedené v levelu
        loadedRecepts = new List<Recept>();

        foreach (string receptName in levelData.recepts)
        {
            Recept match = allRecepts.potions.Find(r => r.name == receptName);
            if (match != null)
            {
                loadedRecepts.Add(match);
            }
            else
            {
                Debug.LogWarning("Recept nenalezen: " + receptName);
            }
        }

        // Nastav byliny na UI
        for (int i = 0; i < levelData.herbs.Count && i < herbSlots.Count; i++)
        {
            string herbName = levelData.herbs[i];
            Sprite herbSprite = Resources.Load<Sprite>("Herbs/" + herbName);
            if (herbSprite != null)
            {
                herbSlots[i].sprite = herbSprite;
                PoisonEffect.ApplyTo(herbSlots[i]); // zelená/rozmazání při otravě
                herbSlots[i].transform.gameObject.SetActive(true);

                HerbItemScript script = herbSlots[i].GetComponent<HerbItemScript>();
                if (script != null)
                {
                    script.herbName = herbName;
                }
            }
        }

    }

}
