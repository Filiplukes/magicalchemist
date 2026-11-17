using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI textNameOfCreatedPotion;
    public TextMeshProUGUI scoreTextUI;
    public Image imageOfCreatedPotionl;
    public Image useHerbImage;
    public SpellDrawScript spellDrawScript;
    public GameObject cauldron;
    public GameObject resultScore;
    public GameObject spellSounds;
    public GameObject effectManager;
    public GameObject poisonEffekt;
    [Tooltip("Zákazník, který si objednává aktuální recept. Může být prázdné.")]
    public CustomerScript customer;
    [Tooltip("Recepty jdou popořadě místo náhodně (testovací kampaň, ať se vystřídají všechny).")]
    public bool sequentialRecipes = false;
    private int nextReceptIndex = 0;

    // ---- Háčky pro kampaň (CampaignGame). TimeMode a LifeMode je nepoužívají. ----
    [Tooltip("O kolik stoupne otrava po dobrém lektvaru (záporné číslo otravu léčí).")]
    public float poisonOnSuccess = 0.05f;
    [Tooltip("O kolik stoupne otrava po zkaženém lektvaru.")]
    public float poisonOnFailure = 0.15f;
    /// <summary>Lektvar dovařen: název ("Broken" = zkažený), kvalita 0–100, recept.</summary>
    public System.Action<string, int, Recept> OnPotionFinished;
    /// <summary>Zákazníkovi došla trpělivost.</summary>
    public System.Action OnOrderMissed;
    /// <summary>(očekávaná bylina, použitá bylina) → true = použitá bylina se počítá jako správná.</summary>
    public System.Func<string, string, bool> AcceptHerbSubstitute;

    public int TotalScore => totalScore;
    public int TotalGems => totalGems;

    private int totalFlowersThrown;
    private int perfectTrace;
    private int failedTen;
    List<float> brewTimestamps = new List<float>(); // čas v sek. kdy byl lektvar uvařen

    private float percentSpells;
    private string usedHerb;

    private SetLevel setLevel;
    private List<Recept> allRecepts;
    private Recept aktualRecept;

    private bool herbInputUpdated = false;
    private bool spellInputUpdated = false;
    private Coroutine stepCoroutine;
    private int totalScore = 0;
    private int totalGems = 0;
    private bool gameOver = false;

    // Komponenty se hledají jednou ve Start, ne při každé bylině/kouzlu.
    private CreatePotion cauldronPotion;
    private EffectManagerScript effects;
    private SpellSounds sounds;
    private PoisonEffect poison;

    void Start()
    {

        cauldronPotion = cauldron.GetComponent<CreatePotion>();
        effects = effectManager.GetComponent<EffectManagerScript>();
        sounds = spellSounds.GetComponent<SpellSounds>();
        if (poisonEffekt != null) poison = poisonEffekt.GetComponent<PoisonEffect>();
        if (customer != null) customer.OnTimedOut = OnCustomerTimedOut;

        UpdateScore();
        totalFlowersThrown = LoadFlowersThrown();
        setLevel = GetComponent<SetLevel>();

        if (setLevel != null)
        {
            allRecepts = setLevel.loadedRecepts;
            stepCoroutine = StartCoroutine(ExecuteReceptSteps());
        }
        else
        {
            Debug.LogError("SetLevel not found on this GameObject.");
        }
    }

    // Počítadlo bylinek má vlastní klíč. Dřív se ukládalo pod "flower_thrower", což je zároveň
    // id achievementu (příznak odemčení). Na Androidu má klíč jen jeden typ, takže se string
    // počítadla a int příznaku navzájem přepisovaly a achievement se spouštěl každou hru.
    private const string FlowersThrownKey = "flowers_thrown_count";
    private const string FlowerAchievementId = "flower_thrower";
    private const int FlowerAchievementThreshold = 50;

    private static int LoadFlowersThrown()
    {
        if (PlayerPrefs.HasKey(FlowersThrownKey))
            return PlayerPrefs.GetInt(FlowersThrownKey, 0);

        // Jednorázová migrace ze starého klíče.
        int count = 0;
        if (PlayerPrefs.HasKey(FlowerAchievementId))
        {
            string old = PlayerPrefs.GetString(FlowerAchievementId, "");
            if (int.TryParse(old, out int parsed))
            {
                // Pod klíčem bylo počítadlo → klíč vrátit achievementu.
                count = parsed;
                if (count > FlowerAchievementThreshold)
                    PlayerPrefs.SetInt(FlowerAchievementId, 1);
                else
                    PlayerPrefs.DeleteKey(FlowerAchievementId);
            }
            else if (PlayerPrefs.GetInt(FlowerAchievementId, 0) == 1)
            {
                // Pod klíčem byl příznak odemčení → počítadlo je aspoň na hranici.
                count = FlowerAchievementThreshold + 1;
            }
        }

        PlayerPrefs.SetInt(FlowersThrownKey, count);
        PlayerPrefs.Save();
        return count;
    }

    void Update()
    {
        if (stepCoroutine == null && !gameOver)
        {
            stepCoroutine = StartCoroutine(ExecuteReceptSteps());
        }
    }

    public void SetPecentSpell(float percentSpells)
    {
        this.percentSpells = percentSpells;
        spellInputUpdated = true;
    }

    public void SetUsedHerbs(string usedHerb)
    {
        this.usedHerb = usedHerb;
        herbInputUpdated = true;
    }

    private void GetRandomRecept()
    {
        int index;
        if (sequentialRecipes)
        {
            index = nextReceptIndex % allRecepts.Count;
            nextReceptIndex++;
        }
        else
        {
            index = Random.Range(0, allRecepts.Count);
        }
        aktualRecept = allRecepts[index];
        if (setLevel != null) setLevel.PrepareHerbsFor(aktualRecept);
        textNameOfCreatedPotion.text = aktualRecept.name;
        imageOfCreatedPotionl.sprite = ResourceCache.Load<Sprite>("Potions/" + aktualRecept.name);
        if (customer != null) customer.Arrive(aktualRecept);
    }

    // Zákazníkovi došla trpělivost: strhnout body a zahodit rozvařený recept.
    // Update() pak spustí nový recept s novým zákazníkem.
    private void OnCustomerTimedOut()
    {
        if (gameOver) return;
        ApplyCustomerPenalty(CustomerScript.Outcome.TimedOut);
        if (stepCoroutine != null)
        {
            StopCoroutine(stepCoroutine);
            stepCoroutine = null;
        }
        OnOrderMissed?.Invoke();
    }

    // Série Divine/Legendary lektvarů za sebou. Prahy odpovídají hodnocení
    // v RiseAndScalePotion.GetRating: Legendary (>= 80) a Divine (>= 65).
    // - Každý další Legendary v sérii zvedne násobič: 1×, 2×, 4×, 6×, 8×, max 10×.
    // - Každý další Divine v sérii přidá +5 bodů k bonusu: 0, +5, +10, … max +25.
    // - Divine a Legendary se navzájem neruší, oba platí pro každý lektvar v sérii.
    // - Cokoliv horšího než Divine (i rozbitý lektvar) sérii vynuluje.
    // Body = body za recept × násobič + bonus (nejdřív se násobí).
    private const int LegendaryQualityThreshold = 80;
    private const int DivineQualityThreshold = 65;
    private const int MaxLegendaryMultiplier = 10;
    private const int DivineBonusStep = 5;
    private const int MaxDivineBonus = 25;

    private int legendaryMultiplier = 1;
    private int divineBonus = 0;
    private bool hadLegendary = false;
    private bool hadDivine = false;

    private int ApplyQualityStreak(int point, int quality)
    {
        if (quality >= LegendaryQualityThreshold)
        {
            if (hadLegendary)
            {
                // 1 → 2, pak po dvou: 4, 6, 8, 10.
                int next = legendaryMultiplier == 1 ? 2 : legendaryMultiplier + 2;
                legendaryMultiplier = Mathf.Min(next, MaxLegendaryMultiplier);
            }
            hadLegendary = true;
        }
        else if (quality >= DivineQualityThreshold)
        {
            // První Divine v sérii bonus ještě nedá, až každý další.
            if (hadDivine)
                divineBonus = Mathf.Min(divineBonus + DivineBonusStep, MaxDivineBonus);
            hadDivine = true;
        }
        else
        {
            legendaryMultiplier = 1;
            divineBonus = 0;
            hadLegendary = false;
            hadDivine = false;
            return point;
        }

        return point * legendaryMultiplier + divineBonus;
    }

    private IEnumerator ExecuteReceptSteps()
    {
        GetRandomRecept();
        int maxPoints = 0;
        string namePotion = aktualRecept.name;
        int point = 0;
        bool breakPotion = false;
        foreach (ReceptStep step in aktualRecept.steps)
        {
            
            int herbIndex = 0;
            int countSteps = 0;
            
            spellDrawScript.SetSpell(step.element);
            while (step.herbs.Count+1 > countSteps) {              
                spellInputUpdated = false;
                herbInputUpdated = false;
                if (herbIndex<step.herbs.Count) {
                    useHerbImage.gameObject.SetActive(true);
                    useHerbImage.sprite = ResourceCache.Load<Sprite>("Herbs/" + step.herbs[herbIndex]);
                }
                else{
                    useHerbImage.gameObject.SetActive(false);
                }
                

                yield return new WaitUntil(() => herbInputUpdated || spellInputUpdated);
                countSteps++;
                if (herbInputUpdated) {
                    cauldronPotion.BoostVolumeSmooth();
                    totalFlowersThrown++;
                    if (totalFlowersThrown > FlowerAchievementThreshold) {
                        AchievementsManagerScript.Instance.UnlockAchievement(FlowerAchievementId);
                    }
                    if (herbIndex<step.herbs.Count && (usedHerb == step.herbs[herbIndex]
                        || (AcceptHerbSubstitute != null && AcceptHerbSubstitute(step.herbs[herbIndex], usedHerb))))
                    {
                        point = point + 25;                      
                    }
                    else
                    {
                        AchievementsManagerScript.Instance.UnlockAchievement("explosion_chef");
                        //Debug.LogWarning($"Wrong herb at step {herbIndex + 1}. Expected: {step.herbs[herbIndex]}, Got: {usedHerb}");
                        point = 0;
                        namePotion = "Broken";
                        breakPotion = true;
                        break;

                    }
                    maxPoints = maxPoints + 25;
                    herbIndex++;
                }

                if (spellInputUpdated) {

                    if (percentSpells >= 10f)
                    {
                        if (percentSpells >= 100) {
                            AchievementsManagerScript.Instance.UnlockAchievement("perfect_spell");
                        }
                       // Debug.Log($"Spell power correct: {percentSpells}% used for element {step.element}");
 
                        float raw = 150 * (percentSpells / 100f);
                        point = point + Mathf.RoundToInt(raw / 5f) * 5;
                        var nameSpellAktual = step.element.Split('.')[0];
                        effects.PlaySpellEffect(nameSpellAktual);
                        sounds.PlaySound(nameSpellAktual);
                        perfectTrace++;
                        if (perfectTrace > 9) {
                            AchievementsManagerScript.Instance.UnlockAchievement("perfect_trace");
                        }
                    }
                    else
                    {
                        //Debug.LogWarning($"Spell power too low. Need at least 10%. Got: {percentSpells}%");
                        point = 0;
                        perfectTrace = 0;
                        namePotion = "Broken";
                        sounds.PlaySound("Explosion");
                        breakPotion = true;                      
                        break;                                            
                    }
                    maxPoints = maxPoints + 150;
                    
                }
                
            }
            if (breakPotion) { break; }
        }
        if (namePotion == "Broken")
        {
            failedTen++;
            if (failedTen > 9)
            {
                AchievementsManagerScript.Instance.UnlockAchievement("failed_ten");
            }
        }
        else {
            OnPotionBrewed();
            failedTen = 0;
        }
        if (customer != null)
            customer.Leave(namePotion == "Broken" ? CustomerScript.Outcome.Broken : CustomerScript.Outcome.Served);
        int quality = Mathf.RoundToInt(((float)point / maxPoints) * 100f);
        cauldronPotion.CreateCompletePotion(namePotion,quality);
        totalScore = totalScore + ApplyQualityStreak(point, quality);
        if (namePotion == "Broken") ApplyCustomerPenalty(CustomerScript.Outcome.Broken);
        if (quality > 10)
        {

            totalGems = totalGems + (quality/4);
        }
        UpdateScore();

        if (poison != null)
        {
            bool failed = namePotion == "Broken";
            float poisonGain = failed ? poisonOnFailure : poisonOnSuccess;
            poison.AddPoison(poisonGain, failed);
        }
        stepCoroutine = null;
        OnPotionFinished?.Invoke(namePotion, quality, aktualRecept);

        yield break;

    }

    // 🔴 Veřejná metoda pro ukončení receptové coroutine
    async public void EndGame(string mode)
     {
        StopGame();
        var result = Instantiate(resultScore);
        result.GetComponent<ResultScoreScript>().SetTitle(mode == "TimeMode" ? "Time's up!" : mode == "LifeMode" ? "Out of lives!" : "Game over");
        if (mode == "Campaign") {
            // Kampaň do žebříčku nepíše.
        }
        else if (mode == "TimeMode") {
            await LeaderBoardNonDestroyObject.Instance.AddScoreAsync(totalScore, "Time_Score_Leaderboard");
        } 
        else { 
            await LeaderBoardNonDestroyObject.Instance.AddScoreAsync(totalScore, "Life_Score_Leaderboard"); 
        }
        
        result.GetComponent<ResultScoreScript>().SetScore(totalScore);
        result.GetComponent<ResultScoreScript>().SetGems(totalGems);
    }

    /// <summary>Zastaví vaření i zákazníky bez výsledkové obrazovky (konec levelu kampaně).</summary>
    public void StopGame()
    {
        if (gameOver) return;
        gameOver = true;
        if (customer != null) customer.Stop();
        PlayerPrefs.SetInt(FlowersThrownKey, totalFlowersThrown);
        PlayerPrefs.Save();
        if (stepCoroutine != null)
        {
            StopCoroutine(stepCoroutine);
            stepCoroutine = null;
        }
    }

    public void PauseGame() {
        // Po konci hry (výhra/prohra v kampani, výsledková obrazovka) už pauza nemá co přerušit.
        // Z pauzy by šlo odejít a v kampani by se tím ztratila právě vyhraná úroveň.
        if (gameOver) return;
        var result = Instantiate(resultScore);
        result.GetComponent<ResultScoreScript>().SetPause();

    }

    // Nesplněná objednávka: vypršel čas (-50) nebo zkažený lektvar (-30). Skóre nejde pod 0.
    private void ApplyCustomerPenalty(CustomerScript.Outcome outcome)
    {
        if (customer == null) return;
        int penalty = customer.PenaltyFor(outcome);
        if (penalty <= 0) return;
        totalScore = Mathf.Max(0, totalScore - penalty);
        UpdateScore();
        if (scoreFlash != null) StopCoroutine(scoreFlash);
        scoreFlash = StartCoroutine(FlashScorePenalty());
    }

    private void UpdateScore()
    {
        scoreTextUI.text = "Score: " + totalScore;

    }

    // Krátké červené bliknutí a „cuknutí“ ukazatele skóre, aby byl postih vidět.
    private Coroutine scoreFlash;
    private Color? scoreBaseColor;
    private IEnumerator FlashScorePenalty()
    {
        if (scoreBaseColor == null) scoreBaseColor = scoreTextUI.color;
        Color baseColor = scoreBaseColor.Value;
        Transform t = scoreTextUI.transform;
        const float duration = 0.8f;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            float k = e / duration;
            scoreTextUI.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), baseColor, k * k);
            t.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        scoreTextUI.color = baseColor;
        t.localScale = Vector3.one;
        scoreFlash = null;
    }



    void OnPotionBrewed()
    {
        float now = Time.time;
        brewTimestamps.Add(now);

        // Odstraň všechny záznamy starší než 60 s
        brewTimestamps.RemoveAll(t => now - t > 60f);

        // Pokud jich je 10 a více => achievement
        if (brewTimestamps.Count >= 10)
        {
            AchievementsManagerScript.Instance.UnlockAchievement("fast_brewer");
        }
    }

}
