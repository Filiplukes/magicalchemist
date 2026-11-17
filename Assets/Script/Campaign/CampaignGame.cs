using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hraní levelu kampaně ve scéně Campaign. Vytvoří se samo po načtení scény, když běží
/// CampaignSession (level spuštěný z CampaignStory). Bez sezení zůstane scéna Campaign
/// testovacím vařením všech nových receptů.
///
/// Podle CampaignGameRules nastaví byliny, recepty, zákazníky, čas a pozadí, hlídá cíl
/// a na konci buď vrátí hráče do příběhu (závěrečný dialog), nebo nabídne opakování.
/// </summary>
public class CampaignGame : MonoBehaviour
{
    private const float PoisonCure = 0.12f;   // úbytek otravy po dobrém lektvaru (level s otravou)
    private const float FinishDelay = 2.2f;      // ať hráč uvidí hotový lektvar a hodnocení

    private CampaignData data;
    private CampaignLevel level;
    private CampaignGameRules rules;

    private GameManager game;
    private PoisonEffect poison;
    private TextMeshProUGUI timerText;
    private TMP_FontAsset font;

    private int progress;
    private int lives;
    private float timeLeft;
    private bool substituted;
    private bool finished;

    private TextMeshProUGUI goalText;
    private int shownScore = -1;
    private int shownSeconds = -1;
    private RectTransform canvasRoot;

    // ---------- Zavedení do scény ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // sceneLoaded běží po Awake objektů scény, ale před jejich Start. Proto stihneme
    // nastavit SetLevel, časovač i zákazníka dřív, než se rozjedou.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != CampaignSession.GameScene || !CampaignSession.IsPlaying) return;
        new GameObject("CampaignGame").AddComponent<CampaignGame>();
    }

    void Awake()
    {
        data = CampaignData.Load();
        level = data?.levels.Find(l => l.id == CampaignSession.LevelId);
        rules = level?.game;
        game = FindFirstObjectByType<GameManager>();
        if (rules == null || !rules.HasGameplay || game == null)
        {
            Debug.LogError($"Kampaň: level {CampaignSession.LevelId} nemá pravidla hraní nebo ve scéně chybí GameManager.");
            enabled = false;
            return;
        }

        lives = rules.lives;
        timeLeft = rules.timeLimit;
        font = game.scoreTextUI != null ? game.scoreTextUI.font : TMP_Settings.defaultFontAsset;

        SetupLevel();
        SetupCustomers();
        SetupTimer();
        SetupBackground();
        HookGame();
        BuildHud();
        UpdateHud();
    }

    private void SetupLevel()
    {
        var setLevel = game.GetComponent<SetLevel>();
        if (setLevel != null)
        {
            setLevel.addUnlockedCampaignRecipes = false;
            setLevel.levelOverride = new LevelData
            {
                recepts = new List<string>(rules.recipes),
                herbs = new List<string>(rules.herbs ?? new List<string>()),
            };
        }
        game.sequentialRecipes = rules.sequential;
        game.showHerbHints = !rules.hideHerbHints;

        // Otrava se objeví až od levelu, kde Wren začíná jako učeň (mistr ji tam vysvětlí).
        // Do té doby se vypne úplně. Jinak platí výchozí hodnoty GameManageru jako v TimeMode,
        // jen level s otravou (poisonPerSecond) dovolí dobrým lektvarem otravu léčit.
        poison = game.poisonEffekt != null ? game.poisonEffekt.GetComponent<PoisonEffect>() : null;
        if (!rules.poison)
        {
            game.poisonOnSuccess = 0f;
            game.poisonOnFailure = 0f;
        }
        else if (rules.poisonPerSecond > 0f)
        {
            game.poisonOnSuccess = -PoisonCure;
        }
    }

    void Start()
    {
        // Každý level začíná bez otravy. PoisonEffect.Start ji nuluje taky, tohle je pojistka
        // nezávislá na pořadí Start metod.
        if (poison != null) poison.SetPoison(0f);
    }

    private void SetupCustomers()
    {
        var customer = game.customer;
        if (customer == null) return;
        if (!rules.customers)
        {
            customer.gameObject.SetActive(false);
            game.customer = null;
            return;
        }

        customer.ScalePatience(rules.customerPatience);
        if (rules.customerPortraits != null && rules.customerPortraits.Count > 0)
        {
            var sprites = new List<Sprite>();
            foreach (var id in rules.customerPortraits)
            {
                var c = data.characters.Find(ch => ch.id == id);
                var sprite = c != null ? Resources.Load<Sprite>("Campaign/Portraits/" + c.portrait) : null;
                if (sprite != null) sprites.Add(sprite);
            }
            customer.SetPortraits(sprites.ToArray());
        }
    }

    // Čas počítá kampaň sama: TimerInGame zrychluje a za dobré lektvary přidává čas,
    // což by u „vydrž 150 s“ cíl posouvalo. Komponenta zůstane (RiseAndScalePotion ji hledá), jen neběží.
    private void SetupTimer()
    {
        var timer = game.GetComponent<TimerInGame>();
        if (timer != null)
        {
            timer.enabled = false;
            timerText = timer.timerText;
        }
        if (timerText != null && rules.timeLimit <= 0f)
        {
            var holder = timerText.transform.parent;
            if (holder != null && holder.GetComponent<Canvas>() == null) holder.gameObject.SetActive(false);
            else timerText.gameObject.SetActive(false);
        }
    }

    private void SetupBackground()
    {
        if (string.IsNullOrEmpty(level.background) || level.background == "Workshop") return; // dílna = vybraný pokoj
        var sprite = Resources.Load<Sprite>("Campaign/Backgrounds/" + level.background);
        var room = FindFirstObjectByType<ScaleWithResolution>();
        var sr = room != null ? room.GetComponent<SpriteRenderer>() : null;
        if (sprite != null && sr != null) sr.sprite = sprite; // měřítko dopočítá ScaleWithResolution.Start
    }

    private void HookGame()
    {
        game.OnPotionFinished = OnPotionFinished;
        game.OnOrderMissed = OnOrderMissed;
        if (!string.IsNullOrEmpty(rules.substituteFor) && !string.IsNullOrEmpty(rules.substituteHerb))
        {
            game.AcceptHerbSubstitute = (expected, used) =>
            {
                if (expected != rules.substituteFor || used != rules.substituteHerb) return false;
                substituted = true;
                return true;
            };
        }
    }

    // ---------- Průběh ----------

    void Update()
    {
        if (finished) return;

        // Skóre před časem: když cíl padne ve stejném snímku, kdy vyprší čas, platí výhra.
        if (rules.goal == "score")
        {
            if (game.TotalScore >= rules.target)
            {
                Win(null);
                return;
            }
            if (game.TotalScore != shownScore) UpdateHud();
        }

        if (rules.timeLimit > 0f)
        {
            timeLeft -= Time.deltaTime; // pauza (timeScale 0) zastaví i čas levelu
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
            if (timerText != null && seconds != shownSeconds)
            {
                shownSeconds = seconds;
                timerText.text = seconds.ToString();
            }
            if (timeLeft <= 0f)
            {
                if (rules.goal == "survive") Win(null);
                else Lose("Time is up!");
                return;
            }
        }

        if (poison != null && rules.poisonPerSecond > 0f)
        {
            poison.AddPoisonGradual(rules.poisonPerSecond * Time.deltaTime);
            if (rules.poisonKills && poison.poisonAmount >= 0.999f)
            {
                Lose("The poison got the better of you.");
                return;
            }
        }

    }

    private void OnPotionFinished(string potion, int quality, Recept recept)
    {
        if (finished) return;
        bool broken = potion == "Broken";

        if (rules.endsCampaign)
        {
            bool failed = broken || quality < rules.failBelowQuality;
            string ending = failed ? rules.failEnding
                          : substituted && !string.IsNullOrEmpty(rules.substituteEnding) ? rules.substituteEnding
                          : rules.successEnding;
            Win(ending);
            return;
        }

        // Level „zpaměti“: po chybě se nápověda na jeden lektvar ukáže.
        if (rules.hideHerbHints) game.showHerbHints = broken;

        if (broken)
        {
            LoseLife();
            substituted = false;
            return;
        }

        if (rules.goal == "potions"
            && quality >= rules.minQuality
            && (recept == null || recept.steps.Count >= rules.minSteps))
        {
            progress++;
            UpdateHud();
            if (progress >= rules.target) Win(null);
        }
    }

    private void OnOrderMissed()
    {
        if (finished) return;
        LoseLife();
    }

    private void LoseLife()
    {
        if (rules.lives <= 0) return;
        lives--;
        UpdateHud();
        if (lives <= 0) Lose("You ran out of lives.");
    }

    private void Win(string ending)
    {
        if (finished) return;
        finished = true;
        game.StopGame();
        CampaignSession.Finish(ending);
        CampaignProgress.RecordWin(level.id, ending);
        UpdateHud();
        StartCoroutine(WinRoutine(ending != null));
    }

    private IEnumerator WinRoutine(bool silent)
    {
        yield return new WaitForSecondsRealtime(FinishDelay);

        // Gemy jen za výhru, ať se kampaň nedá farmit opakovanými prohrami.
        int gems = game.TotalGems;
        Cosmetics.AddGems(gems);

        // Finále rovnou pokračuje koncem příběhu, bez „Level complete“.
        if (silent)
        {
            SceneLoader.Load(CampaignSession.StoryScene);
            yield break;
        }

        string sub = gems > 0 ? $"+{gems} gems" : "";
        ShowOverlay("Level complete!", sub, new Color(1f, 0.84f, 0.3f),
            ("Continue", () => SceneLoader.Load(CampaignSession.StoryScene)));
    }

    private void Lose(string reason)
    {
        if (finished) return;
        finished = true;
        game.StopGame();
        UpdateHud();
        StartCoroutine(LoseRoutine(reason));
    }

    private IEnumerator LoseRoutine(string reason)
    {
        yield return new WaitForSecondsRealtime(1f);
        ShowOverlay("Level failed", reason, new Color(1f, 0.45f, 0.4f),
            ("Try again", () => SceneLoader.Load(CampaignSession.GameScene)),
            ("Back", () =>
            {
                CampaignSession.Clear();
                SceneLoader.Load(CampaignSession.StoryScene);
            }));
    }

    // ---------- UI ----------

    private string GoalLine()
    {
        switch (rules.goal)
        {
            case "score": return $"Score {Mathf.Min(game.TotalScore, rules.target)} / {rules.target}";
            case "survive": return "Survive!";
            default:
                if (rules.endsCampaign) return level.goal;
                string what = rules.customers ? "Orders" : "Potions";
                return $"{what} {progress} / {rules.target}";
        }
    }

    // Volá se jen při změně (lektvar, život, skóre), ne každý snímek.
    private void UpdateHud()
    {
        if (goalText == null) return;
        shownScore = game.TotalScore;
        string text = GoalLine();
        if (rules.lives > 0)
            text += $"\n<size=80%>Lives {Mathf.Max(0, lives)} / {rules.lives}</size>";
        goalText.text = text;
    }

    private void BuildHud()
    {
        var go = new GameObject("CampaignHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        // Stejné měřítko jako herní UI, aby velikosti seděly na všech rozlišeních.
        var scaler = go.GetComponent<CanvasScaler>();
        var gameScaler = game.scoreTextUI != null ? game.scoreTextUI.canvas.rootCanvas.GetComponent<CanvasScaler>() : null;
        if (gameScaler != null)
        {
            scaler.uiScaleMode = gameScaler.uiScaleMode;
            scaler.referenceResolution = gameScaler.referenceResolution;
            scaler.screenMatchMode = gameScaler.screenMatchMode;
            scaler.matchWidthOrHeight = gameScaler.matchWidthOrHeight;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2880, 1620);
            scaler.matchWidthOrHeight = 0.5f;
        }
        canvasRoot = (RectTransform)go.transform;

        // Cíl levelu vlevo pod skóre (nahoře uprostřed je tabule receptu). Panel nechytá dotyky.
        var panel = CreateImage("Goal", canvasRoot, new Color(0f, 0f, 0f, 0.55f));
        panel.raycastTarget = false;
        var rt = panel.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(525f, 230f);
        rt.anchoredPosition = new Vector2(175f, -265f);

        goalText = CreateText("Text", rt, 70f, Color.white);
        goalText.raycastTarget = false;
        goalText.enableAutoSizing = true;
        goalText.fontSizeMin = 40f;
        goalText.fontSizeMax = 70f;
        Stretch(goalText.rectTransform, 20f);
    }

    private void ShowOverlay(string title, string subtitle, Color titleColor, params (string label, System.Action action)[] buttons)
    {
        var dim = CreateImage("Overlay", canvasRoot, new Color(0f, 0f, 0f, 0.7f)); // blokuje dotyky do hry
        Stretch(dim.rectTransform, 0f);

        var box = CreateImage("Box", dim.rectTransform, new Color(0.12f, 0.09f, 0.07f, 0.95f));
        var boxRt = box.rectTransform;
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(1500f, 1000f);

        var titleText = CreateText("Title", boxRt, 150f, titleColor);
        titleText.fontStyle = FontStyles.Bold;
        Place(titleText.rectTransform, new Vector2(0f, 290f), new Vector2(1400f, 220f));

        var subText = CreateText("Subtitle", boxRt, 80f, Color.white);
        Place(subText.rectTransform, new Vector2(0f, 70f), new Vector2(1400f, 200f));
        subText.text = subtitle;
        titleText.text = title;

        float width = 560f, gap = 80f;
        float total = buttons.Length * width + (buttons.Length - 1) * gap;
        for (int i = 0; i < buttons.Length; i++)
        {
            var (label, action) = buttons[i];
            var img = CreateImage(label, boxRt, new Color(0.85f, 0.66f, 0.3f));
            float x = -total / 2f + width / 2f + i * (width + gap);
            Place(img.rectTransform, new Vector2(x, -290f), new Vector2(width, 200f));
            var button = img.gameObject.AddComponent<Button>();
            bool clicked = false;
            button.onClick.AddListener(() =>
            {
                if (clicked) return;
                clicked = true;
                Time.timeScale = 1f;
                action();
            });
            var text = CreateText("Label", img.rectTransform, 90f, new Color(0.15f, 0.08f, 0.02f));
            text.fontStyle = FontStyles.Bold;
            text.text = label;
            Stretch(text.rectTransform, 6f);
        }
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private static void Place(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }
}
