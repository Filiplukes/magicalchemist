using System.Collections;
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

    [Tooltip("Soubor levelu v Resources (bez přípony).")]
    public string levelFile = "Levels/levelTime";
    [Tooltip("Kolik bylin se při obměně talíře vymění navíc kromě těch, které chybí.")]
    public int extraHerbSwaps = 2;

    // Všechny byliny levelu. Když jich je víc než slotů, talíř se mezi recepty obměňuje.
    private List<string> herbPool = new List<string>();

    void Start()
    {
        LoadLevel(levelFile);
    }

    /// <summary>
    /// Zajistí, že na talíři jsou všechny byliny receptu. Chybějící se doplní do slotů,
    /// které recept nepotřebuje, a pár dalších se vymění, aby si hráč nemohl jen pamatovat pozice.
    /// Byliny, které zůstávají, zůstanou na svém místě. Volat jen mezi recepty.
    /// </summary>
    public void PrepareHerbsFor(Recept recept)
    {
        if (recept == null || herbPool.Count <= herbSlots.Count) return;

        var needed = new HashSet<string>();
        foreach (var step in recept.steps)
            foreach (var h in step.herbs) needed.Add(h);

        var onPlate = new HashSet<string>();
        foreach (var slot in herbSlots) onPlate.Add(SlotHerb(slot));

        var missing = new List<string>();
        foreach (var h in needed)
            if (!onPlate.Contains(h) && herbPool.Contains(h)) missing.Add(h);
        if (missing.Count == 0) return;

        // Sloty, které recept nepotřebuje, v náhodném pořadí.
        var free = new List<int>();
        for (int i = 0; i < herbSlots.Count; i++)
            if (!needed.Contains(SlotHerb(herbSlots[i]))) free.Add(i);
        Shuffle(free);

        // Nové byliny: nejdřív chybějící, pak náhodné z ostatních, co na talíři nejsou.
        var incoming = new List<string>(missing);
        var others = herbPool.FindAll(h => !onPlate.Contains(h) && !needed.Contains(h));
        Shuffle(others);
        for (int i = 0; i < extraHerbSwaps && i < others.Count; i++) incoming.Add(others[i]);

        int count = Mathf.Min(incoming.Count, free.Count);
        for (int i = 0; i < count; i++)
            StartCoroutine(SwapSlot(herbSlots[free[i]], incoming[i], i * 0.06f));
    }

    private static string SlotHerb(Image slot)
    {
        var script = slot.GetComponent<HerbItemScript>();
        return script != null ? script.herbName : null;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void AssignSlot(Image slot, string herbName)
    {
        slot.sprite = ResourceCache.Load<Sprite>("Herbs/" + herbName);
        var script = slot.GetComponent<HerbItemScript>();
        if (script != null) script.herbName = herbName;
    }

    // Stará bylina se zmenší, vymění a nová "vyskočí" s malým překmitem.
    private IEnumerator SwapSlot(Image slot, string herbName, float delay)
    {
        Transform t = slot.transform;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        const float half = 0.15f;
        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            t.localScale = Vector3.one * (1f - e / half);
            yield return null;
        }
        AssignSlot(slot, herbName);
        for (float e = 0f; e < half * 1.6f; e += Time.deltaTime)
        {
            float k = e / (half * 1.6f);
            t.localScale = Vector3.one * (Mathf.Sin(k * Mathf.PI) * 0.25f + k);
            yield return null;
        }
        t.localScale = Vector3.one;
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
        herbPool = new List<string>(levelData.herbs);
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
