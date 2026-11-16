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
        if (stepCoroutine == null)
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
        int index = Random.Range(0, allRecepts.Count);
        aktualRecept = allRecepts[index];
        textNameOfCreatedPotion.text = aktualRecept.name;
        imageOfCreatedPotionl.sprite = ResourceCache.Load<Sprite>("Potions/" + aktualRecept.name);
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
                    if (herbIndex<step.herbs.Count && usedHerb == step.herbs[herbIndex])
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
        int quality = Mathf.RoundToInt(((float)point / maxPoints) * 100f);
        cauldronPotion.CreateCompletePotion(namePotion,quality);
        totalScore = totalScore + point;
        if (quality > 10)
        {

            totalGems = totalGems + (quality/4);
        }
        UpdateScore();

        if (poison != null)
        {
            bool failed = namePotion == "Broken";
            float poisonGain = failed ? 0.15f : 0.05f;
            poison.AddPoison(poisonGain, failed);
        }
        stepCoroutine = null;

        yield break;

    }

    // 🔴 Veřejná metoda pro ukončení receptové coroutine
    async public void EndGame(string mode)
     {
        PlayerPrefs.SetInt(FlowersThrownKey, totalFlowersThrown);
        PlayerPrefs.Save();
        if (stepCoroutine != null)
        {
            StopCoroutine(stepCoroutine);
            stepCoroutine = null;
           
        }
        var result = Instantiate(resultScore);
        if (mode == "TimeMode") {
            await LeaderBoardNonDestroyObject.Instance.AddScoreAsync(totalScore, "Time_Score_Leaderboard");
        } 
        else { 
            await LeaderBoardNonDestroyObject.Instance.AddScoreAsync(totalScore, "Life_Score_Leaderboard"); 
        }
        
        result.GetComponent<ResultScoreScript>().SetScore(totalScore);
        result.GetComponent<ResultScoreScript>().SetGems(totalGems);
    }

    public void PauseGame() {
        var result = Instantiate(resultScore);
        result.GetComponent<ResultScoreScript>().SetPause();

    }

    private void UpdateScore()
    {
        scoreTextUI.text = "Score: " + totalScore;

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
