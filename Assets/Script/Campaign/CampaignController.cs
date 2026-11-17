using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kampaň: výběr levelů a průběh příběhu. Level = úvodní karta, úvodní dialog, hraní ve scéně
/// Campaign (CampaignGame), závěrečný dialog. Po výhře se scéna CampaignStory načte znovu
/// a podle CampaignSession pokračuje závěrem. Ve finále určí konec výsledek vaření.
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

    private CampaignData data;
    private bool titleTapped;
    private string currentBackground; // klíč pozadí, které je právě vidět

    void Start()
    {
        Time.timeScale = 1f;
        data = CampaignData.Load();
        if (data == null) return;

        dialogue.Init(data.characters);
        titleTap.onClick.AddListener(() => titleTapped = true);

        titleCardRoot.SetActive(false);
        bannerRoot.SetActive(false);
        levelButtonTemplate.gameObject.SetActive(false);

        // Návrat z hraní: vyhraný level pokračuje závěrem, jinak zpět na výběr.
        var played = CampaignSession.IsPlaying && CampaignSession.Won
            ? data.levels.Find(l => l.id == CampaignSession.LevelId) : null;
        string ending = CampaignSession.Ending;
        string playedBackground = CampaignSession.Background;
        bool continueToNext = CampaignSession.ContinueToNext;
        CampaignSession.Clear();
        if (played != null) StartCoroutine(FinishLevel(played, ending, playedBackground, continueToNext));
        else
        {
            SyncEndingAchievements();
            ShowLevelSelect();
        }
    }

    /// <summary>
    /// Konec se zapisuje hned po hraní (CampaignProgress.RecordWin), achievement až po jeho dialogu.
    /// Když hráč aplikaci zavřel uprostřed konce, achievement se tu doplní potichu.
    /// Nevolat před přehráním čerstvého konce, jinak by se nezobrazilo oznámení.
    /// </summary>
    private void SyncEndingAchievements()
    {
        bool any = false;
        foreach (var e in data.endings)
        {
            if (!CampaignProgress.HasSeenEnding(e.id) || string.IsNullOrEmpty(e.achievement)) continue;
            if (PlayerPrefs.GetInt(e.achievement, 0) == 1) continue;
            PlayerPrefs.SetInt(e.achievement, 1);
            any = true;
        }
        if (!string.IsNullOrEmpty(data.allEndingsAchievement)
            && data.endings.TrueForAll(e => CampaignProgress.HasSeenEnding(e.id))
            && PlayerPrefs.GetInt(data.allEndingsAchievement, 0) == 0)
        {
            PlayerPrefs.SetInt(data.allEndingsAchievement, 1);
            any = true;
        }
        if (any) PlayerPrefs.Save();
    }

    // ---------- Výběr levelů ----------

    private void ShowLevelSelect()
    {
        SetBackground(CampaignBackgrounds.Workshop);
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

    public void BackToMenu() => SceneLoader.Load("Menu", fade: true);

    // ---------- Průběh levelu ----------

    private IEnumerator PlayLevel(CampaignLevel level)
    {
        levelSelectRoot.SetActive(false);
        SetBackground(level.background);

        titleChapter.text = level.chapter;
        titleName.text = $"{level.id}. {level.title}";
        titleGoal.text = level.goal;
        titleCardRoot.SetActive(true);
        titleTapped = false;
        while (!titleTapped) yield return null;
        titleCardRoot.SetActive(false);

        yield return dialogue.Play(level.intro, HandleAction);

        if (level.game != null && level.game.HasGameplay)
        {
            // Hraní i závěr pokračují na pozadí, kde úvod skončil (level 1: z ulice do dílny).
            CampaignSession.Begin(level.id, currentBackground);
            SceneLoader.Load(CampaignSession.GameScene); // do hry s načítací obrazovkou
            yield break; // pokračuje FinishLevel po návratu
        }

        yield return FinishLevel(level, null, currentBackground, false);
    }

    /// <summary>
    /// Po hraní: případný konec kampaně, závěrečný dialog, odemčení. Pak výběr levelů,
    /// nebo rovnou další level, když hráč na výsledkové tabuli zvolil „Další“.
    /// </summary>
    private IEnumerator FinishLevel(CampaignLevel level, string endingId, string backgroundKey, bool continueToNext)
    {
        levelSelectRoot.SetActive(false);
        SetBackground(string.IsNullOrEmpty(backgroundKey) ? level.background : backgroundKey);

        if (!string.IsNullOrEmpty(endingId)) yield return PlayEnding(endingId);

        yield return dialogue.Play(level.outro, HandleAction);

        CampaignProgress.Complete(level.id);
        yield return AnnounceRecipes(RecipeUnlocks.Unlock(level.unlocksRecipes));

        var next = continueToNext ? NextLevel(data, level) : null;
        if (next != null) yield return PlayLevel(next);
        else ShowLevelSelect();
    }

    /// <summary>Level po zadaném (podle pořadí v campaign.json), jen když už je odemčený.</summary>
    public static CampaignLevel NextLevel(CampaignData data, CampaignLevel level)
    {
        if (data == null || level == null) return null;
        int index = data.levels.IndexOf(level);
        if (index < 0 || index + 1 >= data.levels.Count) return null;
        var next = data.levels[index + 1];
        return CampaignProgress.IsUnlocked(next.id) ? next : null;
    }

    /// <summary>Banner pro každý nově odemčený recept (kniha receptů, TimeMode, LifeMode).</summary>
    private IEnumerator AnnounceRecipes(List<string> recipes)
    {
        foreach (var recipe in recipes)
            yield return Banner($"New recipe!\n<size=60%>{recipe}</size>", 2f);
    }

    private IEnumerator HandleAction(CampaignLine line)
    {
        switch (line.action)
        {
            case "background":
                yield return FadeBackground(line.background);
                break;
            case "book":
                // Druhou stranu knihy hráč nikdy neuvidí, mistr ji vytrhne dřív.
                yield return book.Play(data.bookPage1);
                break;
            default:
                Debug.LogWarning("Neznámá akce v kampani: " + line.action);
                break;
        }
    }

    private IEnumerator PlayEnding(string endingId)
    {
        var ending = data.endings.Find(e => e.id == endingId);
        if (ending == null)
        {
            Debug.LogWarning("Neznámý konec kampaně: " + endingId);
            yield break;
        }

        yield return dialogue.Play(ending.lines, HandleAction);

        CampaignProgress.MarkEnding(ending.id);
        Unlock(ending.achievement);
        if (data.endings.TrueForAll(e => CampaignProgress.HasSeenEnding(e.id)))
            Unlock(data.allEndingsAchievement);

        var recipes = RecipeUnlocks.Unlock(ending.unlocksRecipes);
        yield return Banner($"The End\n<size=60%>{ending.title}</size>", 2.5f);
        yield return AnnounceRecipes(recipes);
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
        currentBackground = key;
        var sprite = CampaignBackgrounds.Resolve(key, out var tint);
        background.color = tint;
        if (sprite == null) return;

        background.sprite = sprite;
        if (backgroundFitter != null)
            backgroundFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    private const float BackgroundFade = 0.4f;

    /// <summary>Změna pozadí uprostřed dialogu: ztmavne do černa, vymění se a zase se rozjasní.</summary>
    private IEnumerator FadeBackground(string key)
    {
        Color from = background.color;
        for (float t = 0f; t < BackgroundFade; t += Time.unscaledDeltaTime)
        {
            background.color = Color.Lerp(from, Color.black, t / BackgroundFade);
            yield return null;
        }
        SetBackground(key);
        Color to = background.color;
        for (float t = 0f; t < BackgroundFade; t += Time.unscaledDeltaTime)
        {
            background.color = Color.Lerp(Color.black, to, t / BackgroundFade);
            yield return null;
        }
        background.color = to;
    }
}
