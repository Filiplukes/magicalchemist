using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kampaň: výběr levelů a průběh příběhu. Prototyp: hratelnost se zatím přeskakuje,
/// level = úvodní karta, úvodní dialog, "level splněn", závěrečný dialog.
/// Ve finále se konec volí tlačítkem, dokud nebude napojené vaření.
/// </summary>
public class CampaignController : MonoBehaviour
{
    public DialoguePlayer dialogue;
    public StoryBook book;

    [Header("Pozadí")]
    public Image background;
    public AspectRatioFitter backgroundFitter;

    [Header("Výběr levelů")]
    public GameObject levelSelectRoot;
    public Transform levelGrid;
    public Button levelButtonTemplate;

    [Header("Úvodní karta levelu")]
    public GameObject titleCardRoot;
    public TextMeshProUGUI titleChapter;
    public TextMeshProUGUI titleName;
    public TextMeshProUGUI titleGoal;
    public Button titleTap;

    [Header("Oznámení (level splněn, konec)")]
    public GameObject bannerRoot;
    public TextMeshProUGUI bannerText;

    [Header("Volba konce (prototyp)")]
    public GameObject choiceRoot;
    public Button choiceFailure;
    public Button choiceObedience;
    public Button choiceBetrayal;

    private CampaignData data;
    private bool titleTapped;
    private string chosenEnding;
    private bool endingPlayed;

    void Start()
    {
        Time.timeScale = 1f;
        data = CampaignData.Load();
        if (data == null) return;

        dialogue.Init(data.characters);
        titleTap.onClick.AddListener(() => titleTapped = true);
        choiceFailure.onClick.AddListener(() => chosenEnding = "failure");
        choiceObedience.onClick.AddListener(() => chosenEnding = "obedience");
        choiceBetrayal.onClick.AddListener(() => chosenEnding = "betrayal");

        titleCardRoot.SetActive(false);
        bannerRoot.SetActive(false);
        choiceRoot.SetActive(false);
        levelButtonTemplate.gameObject.SetActive(false);
        ShowLevelSelect();
    }

    // ---------- Výběr levelů ----------

    private void ShowLevelSelect()
    {
        SetBackground("Workshop");
        levelSelectRoot.SetActive(true);

        foreach (Transform child in levelGrid)
            if (child != levelButtonTemplate.transform) Destroy(child.gameObject);

        foreach (var level in data.levels)
        {
            var button = Instantiate(levelButtonTemplate, levelGrid);
            button.gameObject.SetActive(true);
            bool unlocked = CampaignProgress.IsUnlocked(level.id);
            bool done = CampaignProgress.IsCompleted(level.id);

            var texts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            texts[0].text = level.id.ToString();
            texts[1].text = unlocked ? level.title : "Locked";

            button.interactable = unlocked;
            var colors = button.colors;
            colors.disabledColor = Color.white; // ztlumení řeší barva obrázku níž
            button.colors = colors;
            var image = button.GetComponent<Image>();
            image.color = !unlocked ? new Color(0.38f, 0.34f, 0.3f, 1f)
                        : done ? new Color(1f, 0.92f, 0.65f) : Color.white;

            var captured = level;
            button.onClick.AddListener(() => StartCoroutine(PlayLevel(captured)));
        }
    }

    public void BackToMenu() => SceneLoader.Load("Menu");

    /// <summary>Testovací scéna se všemi novými recepty.</summary>
    public void OpenRecipeTest() => SceneLoader.Load("Campaign");

    public void ResetProgress()
    {
        CampaignProgress.Reset(data.levels, data.endings);
        ShowLevelSelect();
    }

    // ---------- Průběh levelu ----------

    private IEnumerator PlayLevel(CampaignLevel level)
    {
        levelSelectRoot.SetActive(false);
        SetBackground(level.background);
        endingPlayed = false;

        titleChapter.text = level.chapter;
        titleName.text = $"{level.id}. {level.title}";
        titleGoal.text = level.goal;
        titleCardRoot.SetActive(true);
        titleTapped = false;
        while (!titleTapped) yield return null;
        titleCardRoot.SetActive(false);

        yield return dialogue.Play(level.intro, HandleAction);

        // Tady bude hratelnost. Prototyp ji přeskočí.
        if (!endingPlayed) yield return Banner("Level complete!", 1.3f);

        yield return dialogue.Play(level.outro, HandleAction);

        CampaignProgress.Complete(level.id);
        ShowLevelSelect();
    }

    private IEnumerator HandleAction(CampaignLine line)
    {
        switch (line.action)
        {
            case "book":
                yield return book.Play(data.bookPage1, data.bookPage2);
                break;
            case "choice":
                yield return PlayEnding();
                break;
            default:
                Debug.LogWarning("Neznámá akce v kampani: " + line.action);
                break;
        }
    }

    private IEnumerator PlayEnding()
    {
        chosenEnding = null;
        choiceRoot.SetActive(true);
        while (chosenEnding == null) yield return null;
        choiceRoot.SetActive(false);

        var ending = data.endings.Find(e => e.id == chosenEnding);
        if (ending == null) yield break;

        yield return dialogue.Play(ending.lines, HandleAction);
        endingPlayed = true;

        CampaignProgress.MarkEnding(ending.id);
        Unlock(ending.achievement);
        if (data.endings.TrueForAll(e => CampaignProgress.HasSeenEnding(e.id)))
            Unlock(data.allEndingsAchievement);

        yield return Banner($"The End\n<size=60%>{ending.title}</size>", 2.5f);
    }

    private static void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (AchievementsManagerScript.Instance != null)
            AchievementsManagerScript.Instance.UnlockAchievement(id);
        else
            PlayerPrefs.SetInt(id, 1);
    }

    private IEnumerator Banner(string text, float seconds)
    {
        bannerText.text = text;
        bannerRoot.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        bannerRoot.SetActive(false);
    }

    // ---------- Pozadí ----------

    private void SetBackground(string key)
    {
        Sprite sprite = null;
        if (key != "Workshop")
            sprite = Resources.Load<Sprite>("Campaign/Backgrounds/" + key);
        if (sprite == null)
        {
            var room = Cosmetics.GetSelected(CosmeticType.Room);
            if (room != null) sprite = room.Sprite;
        }
        if (sprite == null) return;

        background.sprite = sprite;
        if (backgroundFitter != null)
            backgroundFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }
}
