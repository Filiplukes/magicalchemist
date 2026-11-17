using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HerbalCodexScript : MonoBehaviour
{
    private int page = 0;
    public TextMeshProUGUI namePotion;
    public TextMeshProUGUI steps;
    public TextMeshProUGUI description;
    public Image potionsImage;
    [Tooltip("Výška pole popisu, aby se delší popisy (recepty z kampaně) vešly na stránku.")]
    public float descriptionHeight = 300f;
    public float descriptionMinFontSize = 26f;
    AllReceptsData allRecepts;

    void Start()
    {
        var rt = description.rectTransform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, descriptionHeight);
        // V prefabu má popis záporný spodní okraj, takže text přetékal pod rámeček. Teď je místo dané výškou.
        var m = description.margin;
        description.margin = new Vector4(m.x, m.y, m.z, 0f);
        description.fontSizeMax = description.fontSize;
        description.fontSizeMin = descriptionMinFontSize;
        description.enableAutoSizing = true;

        TextAsset jsonRecepts = Resources.Load<TextAsset>("Levels/potions");

        if (jsonRecepts == null)
        {
            Debug.LogError("Missing JSON file!");
            return;
        }
        allRecepts = JsonUtility.FromJson<AllReceptsData>(jsonRecepts.text);
        // Recepty z kampaně jsou vidět až po dohrání levelu, kde se poprvé objeví.
        allRecepts.potions = allRecepts.potions.FindAll(r => RecipeUnlocks.IsUnlocked(r.name));

        LoadPotion();
    }

    void LoadPotion()
    {

        var aktualRecept = allRecepts.potions[page];

        var spriteForPotions = Resources.Load<Sprite>("Potions/" + aktualRecept.name);
        if (spriteForPotions == null)
        {
            spriteForPotions = Resources.Load<Sprite>("Potions/" + "Broken");
        }
        potionsImage.sprite = spriteForPotions;

        namePotion.text = aktualRecept.name;
        description.text = "Description:\r\n" + aktualRecept.description;
        string stepsText = "";
        int countStep = 1;
        foreach (var step in aktualRecept.steps) {
            stepsText = stepsText+"Step " + countStep + ": \r\n" +  step.element+ "\r\n";
            foreach (var herb in step.herbs)
            {
                stepsText = stepsText + herb + "\r\n";
            }
            countStep++;
        }
        steps.text = stepsText;   

    }

    public void NextPage() {

        page++;
        if (page > allRecepts.potions.Count - 1) { page = 0; }
        LoadPotion();
    }

    public void PreviousPage(){
        page--;
        if (page < 0) { page = allRecepts.potions.Count-1; }
        LoadPotion();
    }
    public void CloseHerbal() {
        Destroy(gameObject);
    }
}

