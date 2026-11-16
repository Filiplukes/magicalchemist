using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManagerScript : MonoBehaviour
{
    public GameObject[] inactiveGameObject;
    public GameObject[] panels;
    private string[] texts;

    public TextMeshProUGUI text;
    private int currentStep = 0;
    public string tutorialName = "TutorialTime";
    void Start()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "LifeMode")
        {
         texts = new string[] {
            "Welcome, young Alchemist.\r\nThis is your workshop — a place of wonder, mystery, and power.\r\nLook around carefully… ",
            "Here, you can craft potions and elixirs using magic and herbs",
            "The top panel reveals the potion you’re about to craft,\r\n along with subtle clues about the herbs required.",
            "To your left are the herbs you must drag into the cauldron. \r\n Choose wisely — a single wrong herb may corrupt your entire potion.",
            "To your right lies a scroll bearing an enchanted symbol. Trace its form to invoke the spell. The more precise your hand,\r\n the stronger the potion shall become.",
            "In the top right corner lies the timer — a reminder that time waits for no one. Your current score shimmers in the top left.",
            "Now step forth, apprentice... your journey to becoming the Magic Master Alchemist begins!",
            };


            if (PlayerPrefs.GetInt(tutorialName, 0) == 0)
            {
                Time.timeScale = 0f; // zmrazíme hru
                foreach (GameObject panel in panels)
                {
                    panel.SetActive(false);
                }
                ShowStep();
            }
            else
            {

                gameObject.SetActive(false);
            }
        }
        else {

            texts = new string[] {
            "Welcome, young Alchemist.\r\nThis is your workshop — a place of wonder, mystery, and power.\r\nLook around carefully… ",
            "Here, you can craft potions and elixirs using magic and herbs",
            "The top panel reveals the potion you’re about to craft,\r\n along with subtle clues about the herbs required.",
            "To your left are the herbs you must drag into the cauldron. \r\n Choose wisely — a single wrong herb may corrupt your entire potion.",
            "To your right lies a scroll bearing an enchanted symbol. Trace its form to invoke the spell. The more precise your hand,\r\n the stronger the potion shall become.",
            "In the top right corner, you can see your lives — each one shapes your fate. Your current score shimmers in the top left.",
            "Now step forth, apprentice... your journey to becoming the Magic Master Alchemist begins!",
            };

            tutorialName = "TutorialLife";
            if (PlayerPrefs.GetInt(tutorialName, 0) == 0)
            {
                Time.timeScale = 0f; // zmrazíme hru
                foreach (GameObject panel in panels)
                {
                    panel.SetActive(false);
                }
                ShowStep();
            }
            else
            {
                gameObject.SetActive(false);

            }

            
        }
    }

    void ShowStep()
    {
     
            text.text = texts[currentStep];
        if (currentStep > 1 && currentStep < 5)
        {
            panels[currentStep - 2].SetActive(true);
            if (currentStep == 3 || currentStep == 4) {
                panels[currentStep - 2].GetComponent<CanvasGroup>().blocksRaycasts = false;
            }
           
           
        }else if (currentStep == 5) {

            panels[currentStep - 2].SetActive(true);
            panels[currentStep - 1].SetActive(true);
        }


    }

    public void NextStep()
    {
        currentStep++;
        if (currentStep >= texts.Length)
        {
            EndTutorial();
        }
        else
        {
            ShowStep();
        }
    }

    void EndTutorial()
    {
        panels[1].GetComponent<CanvasGroup>().blocksRaycasts = true;
        panels[2].GetComponent<CanvasGroup>().blocksRaycasts = true;
        Time.timeScale = 1f; // spustíme hru
        gameObject.SetActive(false); // nebo deaktivuj TutorialManager
        PlayerPrefs.SetInt(tutorialName, 1);
        
        }
}