using System.Collections;
using UnityEngine;
using TMPro;  // Import TextMeshPro namespace

public class TimerInGame : MonoBehaviour
{
    public float roundDuration = 120f;                // Délka kola v sekundách
    public TextMeshProUGUI timerText;                // TextMeshPro text pro zobrazení èasu
    public GameManager gameManager;
    [Tooltip("Mód pøedaný do GameManager.EndGame (TimeMode = žebøíèek, Campaign = bez žebøíèku).")]
    public string endGameMode = "TimeMode";
    private GameObject ad;
    private float timeMultiplier = 1f;
    private float speedModif = 120f;
    bool marathonUnlocked = false;

    void Start()
    {
        ad = GameObject.Find("AdIns");
        if (timerText != null)
        {
            timerText.text = roundDuration.ToString("F0");
        }

        StartCoroutine(RunGameRound());
    }

    IEnumerator RunGameRound()
    {
        float elapsed = 0f;
        float speedModifElapsed = 0f;
        while (elapsed < roundDuration)
        {
            timeMultiplier = 1f + Mathf.Floor(elapsed / speedModif) * 0.3f;

            speedModifElapsed += Time.deltaTime;

            // Po uplynutí speedModif sekund snížíme speedModif o 10, pokud je vìtší než 40
            if (speedModifElapsed >= speedModif && speedModif > 40f)
            {
                speedModif -= 10f;
                speedModifElapsed = 0f;  // resetujeme èasovaè
            }


            elapsed += Time.deltaTime * timeMultiplier;

            float timeLeft = Mathf.Clamp(roundDuration - elapsed, 0, roundDuration);

            if (timerText != null)
            {
                timerText.text = timeLeft.ToString("F0"); // celé sekundy
            }
            if(!marathonUnlocked && elapsed >= 240)
            {
                AchievementsManagerScript.Instance.UnlockAchievement("potion_marathon");
                marathonUnlocked = true;
            }
            
            yield return null;
        }

        EndRound();
    }

    void EndRound()
    {
        if (timerText != null)
        {
            timerText.text = "0";
        }
        gameManager.EndGame(endGameMode);
        // Kdyz na konci hry prijde rada na hodnoceni, reklama se tentokrat neukaze.
        if (!InAppReview.OnGameFinished() && ad != null) { ad.GetComponent<AdIns>().ShowInterstitialAd(); }
    }

    public void AddTime(int time) {

        roundDuration = roundDuration + time;
    }

}
