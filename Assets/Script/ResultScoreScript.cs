using UnityEngine;
using TMPro;

/// <summary>
/// Výsledková tabule (prefab ResultScore) pro všechny módy: konec TimeMode/LifeMode,
/// výhra a prohra v levelu kampaně i pauza. Nadpis, podtitulek a tlačítka nastaví ten,
/// kdo tabuli vytvořil (GameManager, CampaignGame). Viditelná tlačítka se vycentrují.
/// </summary>
public class ResultScoreScript : MonoBehaviour
{
    private const float ButtonSpacing = 170f;

    private GameObject scroll;
    private GameObject herbsInventory;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Tlačítka (pořadí zleva doprava)")]
    public RectTransform homeButton;
    public RectTransform restartButton;
    public RectTransform nextButton;
    public RectTransform playButton;

    private void Awake()
    {
        SetTitle(null);
        LayoutButtons();
    }

    private void Start()
    {
        scroll = GameObject.FindWithTag("Scroll");
        herbsInventory = GameObject.FindWithTag("HerbsInventory");
        setPanelInActive(false);
    }

    /// <summary>Nadpis tabule a menší řádek pod ním. Prázdný text se skryje.</summary>
    public void SetTitle(string title, string subtitle = null)
    {
        SetText(titleText, title);
        SetText(subtitleText, subtitle);
    }

    /// <summary>Která tlačítka jsou vidět. Výchozí stav prefabu je Domů + Znovu.</summary>
    public void SetButtons(bool home, bool restart, bool next, bool play)
    {
        SetActive(homeButton, home);
        SetActive(restartButton, restart);
        SetActive(nextButton, next);
        SetActive(playButton, play);
        LayoutButtons();
    }

    public void SetScore(int score)
    {
        scoreText.text = score.ToString();
        scoreText.gameObject.SetActive(true);
        scoreText.transform.parent.gameObject.SetActive(true);
    }

    /// <summary>Zobrazí gemy a připíše je hráči.</summary>
    public void SetGems(int gems)
    {
        Cosmetics.AddGems(gems);

        gemsText.text = gems.ToString();
        gemsText.gameObject.SetActive(true);
        gemsText.transform.parent.gameObject.SetActive(true);
    }

    /// <summary>Kampaň: závěr vyhraného levelu a hned další level (bez výběru levelů).</summary>
    public void StartNextLevel()
    {
        if (!CampaignSession.IsPlaying || !CampaignSession.Won) return;
        Time.timeScale = 1f;
        CampaignSession.ContinueToNext = true;
        SceneLoader.Load(CampaignSession.StoryScene, fade: true);
    }

    public void SetPause()
    {
        Time.timeScale = 0f;
        SetTitle("Paused");
        SetButtons(home: true, restart: true, next: false, play: true);
    }

    public void Play()
    {
        Time.timeScale = 1f;
        setPanelInActive(true);
        Destroy(gameObject);
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        SceneLoader.Load(sceneName);
    }

    public void EndGame()
    {
        Time.timeScale = 1f;
        if (CampaignSession.IsPlaying)
        {
            // Vyhraný level ještě přehraje závěrečný dialog, pak výběr levelů.
            // Z rozehraného nebo prohraného levelu rovnou na výběr levelů.
            if (!CampaignSession.Won) CampaignSession.Clear();
            SceneLoader.Load(CampaignSession.StoryScene, fade: true);
            return;
        }
        SceneLoader.Load("Menu");
    }

    private void setPanelInActive(bool active)
    {
        if (herbsInventory != null) herbsInventory.SetActive(active);
        if (scroll != null) scroll.SetActive(active);
    }

    public void Settings()
    {
    }

    private void LayoutButtons()
    {
        var buttons = new[] { homeButton, restartButton, nextButton, playButton };
        int count = 0;
        foreach (var b in buttons) if (b != null && b.gameObject.activeSelf) count++;
        if (count == 0) return;

        var board = (RectTransform)homeButton.parent;
        float center = board.rect.width * (0.5f - homeButton.anchorMin.x);
        float x = center - (count - 1) * ButtonSpacing / 2f;
        foreach (var b in buttons)
        {
            if (b == null || !b.gameObject.activeSelf) continue;
            b.anchoredPosition = new Vector2(x, b.anchoredPosition.y);
            x += ButtonSpacing;
        }
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text == null) return;
        text.text = value ?? "";
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private static void SetActive(RectTransform rt, bool active)
    {
        if (rt != null) rt.gameObject.SetActive(active);
    }
}
