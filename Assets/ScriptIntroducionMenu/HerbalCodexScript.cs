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
    AllReceptsData allRecepts;

    void Start()
    {
        TextAsset jsonRecepts = Resources.Load<TextAsset>("Levels/potions");

        if (jsonRecepts == null)
        {
            Debug.LogError("Missing JSON file!");
            return;
        }
        allRecepts = JsonUtility.FromJson<AllReceptsData>(jsonRecepts.text);

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

