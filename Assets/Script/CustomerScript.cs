using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zákazník, který si objednává právě vařený recept. Objeví se vpravo (hlava z Resources/Customers),
/// v bublině krátce řekne, co chce, a pod ním ubývá čas. Každý zákazník má jiný spěch,
/// takže jinak dlouhou trpělivost. Když čas vyprší, odejde, GameManager strhne body
/// a zruší rozvařený recept.
/// </summary>
public class CustomerScript : MonoBehaviour
{
    [System.Serializable]
    public class Haste
    {
        public string name;
        [Tooltip("Násobí základní čas receptu. Menší = netrpělivější zákazník.")]
        public float patienceMultiplier = 1.5f;
        public Color timerColor = Color.green;
        [Tooltip("{0} se nahradí názvem receptu.")]
        public string[] lines;
    }

    [Header("UI")]
    public CanvasGroup group;
    public RectTransform body;          // posouvá se při příchodu/odchodu
    public Image portrait;
    public TextMeshProUGUI bubbleText;
    public RectTransform timerFill;     // šířka přes anchorMax.x
    public Image timerFillImage;

    [Header("Balance")]
    [Tooltip("Čas, který má hráč vždy, nezávisle na délce receptu.")]
    public float baseSeconds = 4f;
    public float secondsPerHerb = 2.5f;
    public float secondsPerSpell = 6f;
    [Tooltip("Čas navíc za každý další krok receptu (nové kouzlo, nová sada bylin).")]
    public float extraSecondsPerStep = 8f;
    [Tooltip("Kolik bodů stojí zákazník, kterému vypršel čas.")]
    public int missedOrderPenalty = 50;
    [Tooltip("Kolik bodů stojí zkažený lektvar. Menší než za vypršení času, aby se vyplatilo zkusit i těžký recept.")]
    public int brokenOrderPenalty = 30;

    public int PenaltyFor(Outcome outcome) =>
        outcome == Outcome.TimedOut ? missedOrderPenalty
      : outcome == Outcome.Broken ? brokenOrderPenalty
      : 0;

    public Haste[] hastes =
    {
        new Haste { name = "Relaxed", patienceMultiplier = 2.0f, timerColor = new Color(0.45f, 0.8f, 0.35f),
            lines = new[] { "{0}, when you can.", "No rush. {0}, please.", "I'd like a {0}." } },
        new Haste { name = "Normal", patienceMultiplier = 1.6f, timerColor = new Color(0.95f, 0.75f, 0.25f),
            lines = new[] { "One {0}, please.", "I need a {0}.", "A {0}, alchemist." } },
        new Haste { name = "Hurried", patienceMultiplier = 1.25f, timerColor = new Color(0.9f, 0.3f, 0.25f),
            lines = new[] { "{0}! Quickly!", "Hurry, a {0}!", "{0}, now!" } },
    };

    public string[] happyLines = { "Thank you!", "Perfect!", "Splendid!" };
    public string[] brokenLines = { "That's ruined...", "Ugh, no thanks.", "Is that… smoking?" };
    public string[] timeoutLines = { "Too slow!", "I'm leaving!", "Forget it!" };

    public enum Outcome { Served, Broken, TimedOut }

    /// <summary>Volá se, když zákazníkovi dojde trpělivost.</summary>
    public System.Action OnTimedOut;

    private const float SlideDistance = 120f;
    private const float SlideDuration = 0.25f;
    private const float ReactionDuration = 0.6f;

    private Sprite[] portraits;
    private int lastPortrait = -1;
    private Vector2 bodyHome;
    private Coroutine arriveRoutine;
    private Coroutine leaveRoutine;
    private bool leaving;           // předchozí zákazník ještě reaguje / odchází
    private bool waiting;           // běží odpočet
    private bool stopped;

    void Awake()
    {
        portraits = Resources.LoadAll<Sprite>("Customers");
        if (portraits.Length == 0) Debug.LogWarning("CustomerScript: v Resources/Customers nejsou žádné portréty.");
        bodyHome = body.anchoredPosition;
        group.alpha = 0f;
    }

    /// <summary>Přijde nový zákazník a objedná si recept. Předchozí nejdřív dořekne reakci a odejde.</summary>
    public void Arrive(Recept recept)
    {
        if (stopped || recept == null) return;
        if (arriveRoutine != null) StopCoroutine(arriveRoutine);
        arriveRoutine = StartCoroutine(ArriveRoutine(recept));
    }

    /// <summary>Zákazník odejde s reakcí podle výsledku.</summary>
    public void Leave(Outcome outcome)
    {
        if (stopped) return;
        if (arriveRoutine != null) { StopCoroutine(arriveRoutine); arriveRoutine = null; }
        waiting = false;
        if (leaving || group.alpha <= 0f) return; // nový zákazník ještě nepřišel
        StartLeave(outcome);
    }

    /// <summary>Konec hry: zastaví odpočet a nechá zákazníka zmizet bez trestu.</summary>
    public void Stop()
    {
        stopped = true;
        waiting = false;
        StopAllCoroutines();
        StartCoroutine(Fade(group.alpha, 0f, SlideDuration, true));
    }

    private void StartLeave(Outcome outcome)
    {
        if (leaveRoutine != null) StopCoroutine(leaveRoutine);
        leaving = true;
        leaveRoutine = StartCoroutine(LeaveRoutine(outcome));
    }

    private float PatienceFor(Recept recept, Haste haste)
    {
        float seconds = baseSeconds;
        foreach (var step in recept.steps)
            seconds += step.herbs.Count * secondsPerHerb + secondsPerSpell;
        seconds += Mathf.Max(0, recept.steps.Count - 1) * extraSecondsPerStep;
        return seconds * haste.patienceMultiplier;
    }

    private IEnumerator ArriveRoutine(Recept recept)
    {
        while (leaving) yield return null;

        var haste = hastes[Random.Range(0, hastes.Length)];
        float patience = PatienceFor(recept, haste);

        if (portraits.Length > 0)
        {
            int i = Random.Range(0, portraits.Length);
            if (portraits.Length > 1 && i == lastPortrait) i = (i + 1) % portraits.Length;
            lastPortrait = i;
            portrait.sprite = portraits[i];
        }

        bubbleText.text = string.Format(haste.lines[Random.Range(0, haste.lines.Length)], recept.name);
        timerFillImage.color = haste.timerColor;
        SetTimer(1f);

        yield return Fade(0f, 1f, SlideDuration, false);

        // Odpočet. Time.deltaTime, aby pauza (timeScale = 0) zastavila i zákazníka.
        waiting = true;
        float left = patience;
        while (left > 0f)
        {
            left -= Time.deltaTime;
            SetTimer(left / patience);
            yield return null;
        }
        waiting = false;
        SetTimer(0f);

        arriveRoutine = null;
        StartLeave(Outcome.TimedOut);
        OnTimedOut?.Invoke();
    }

    private IEnumerator LeaveRoutine(Outcome outcome)
    {
        string[] lines = outcome == Outcome.Served ? happyLines
                       : outcome == Outcome.Broken ? brokenLines
                       : timeoutLines;
        string line = lines[Random.Range(0, lines.Length)];
        int penalty = PenaltyFor(outcome);
        if (penalty > 0)
            line += $" -{penalty}";
        bubbleText.text = line;

        yield return new WaitForSeconds(ReactionDuration);
        yield return Fade(group.alpha, 0f, SlideDuration, true);
        leaving = false;
        leaveRoutine = null;
    }

    // Příchod: vyjede zprava a zprůhlední se. Odchod: zajede doprava.
    private IEnumerator Fade(float from, float to, float duration, bool slideOut)
    {
        Vector2 offscreen = bodyHome + new Vector2(SlideDistance, 0f);
        Vector2 start = slideOut ? body.anchoredPosition : (from <= 0f ? offscreen : bodyHome);
        Vector2 end = slideOut ? offscreen : bodyHome;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            group.alpha = Mathf.Lerp(from, to, k);
            body.anchoredPosition = Vector2.Lerp(start, end, k);
            yield return null;
        }
        group.alpha = to;
        body.anchoredPosition = to > 0f ? bodyHome : offscreen;
    }

    private void SetTimer(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        timerFill.anchorMax = new Vector2(fraction, 1f);
    }

    public bool IsWaiting => waiting;

    /// <summary>Zákazníci s vlastními portréty (kampaň: družina). Volat před prvním příchodem.</summary>
    public void SetPortraits(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return;
        portraits = sprites;
        lastPortrait = -1;
    }

    /// <summary>Vynásobí trpělivost všech zákazníků (kampaň: nápor raněných).</summary>
    public void ScalePatience(float factor)
    {
        if (factor <= 0f || Mathf.Approximately(factor, 1f)) return;
        foreach (var h in hastes) h.patienceMultiplier *= factor;
    }
}
