using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Okno Google Play pro hodnocení hry přímo ve hře (In-App Review).
/// Volá se po dohrané hře (výhra v kampani, konec TimeMode/LifeMode). Nabídne se po třetí hře
/// a ještě jednou o pár dní později, pak už nikdy. Google má navíc vlastní limit a okno někdy
/// neukáže vůbec, i když o něj hra požádá; jestli hráč hodnotil, se hra nedozví.
/// Okno funguje jen ve hře nainstalované z Google Play (i z interního testování),
/// v Editoru a v buildu nahraném přímo do telefonu se jen zapíše do logu.
/// </summary>
public class InAppReview : MonoBehaviour
{
    private const string GamesKey = "ReviewGamesFinished";
    private const string LastAskKey = "ReviewLastAskDay";
    private const string AskCountKey = "ReviewAskCount";

    private const int MinGames = 3;      // první žádost až po třetí dohrané hře
    private const int CooldownDays = 3;  // druhá (poslední) žádost nejdřív za 3 dny
    private const int MaxAsks = 2;       // pak už nikdy, ať hráče neotravuje
    private const float Delay = 1.5f;    // hráč nejdřív uvidí výsledek, pak okno

    private static InAppReview runner;

    private static int Today => (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalDays;

    /// <summary>
    /// Započte dohranou hru a když je čas, za chvíli požádá o hodnocení.
    /// Vrátí true, když o něj žádá: interstitial reklama se pak tentokrát neukazuje.
    /// </summary>
    public static bool OnGameFinished()
    {
        int games = PlayerPrefs.GetInt(GamesKey, 0) + 1;
        PlayerPrefs.SetInt(GamesKey, games);

        int asks = PlayerPrefs.GetInt(AskCountKey, 0);
        bool due = games >= MinGames
                   && asks < MaxAsks
                   && (!PlayerPrefs.HasKey(LastAskKey) || Today - PlayerPrefs.GetInt(LastAskKey) >= CooldownDays);
        if (due)
        {
            PlayerPrefs.SetInt(LastAskKey, Today);
            PlayerPrefs.SetInt(AskCountKey, asks + 1);
            Runner.StartCoroutine(RequestAfterDelay());
        }
        PlayerPrefs.Save();
        return due;
    }

    private static InAppReview Runner
    {
        get
        {
            if (runner == null)
            {
                var go = new GameObject("InAppReview");
                DontDestroyOnLoad(go);
                runner = go.AddComponent<InAppReview>();
            }
            return runner;
        }
    }

    private static IEnumerator RequestAfterDelay()
    {
        // Realtime: výsledková tabule může mít zastavený čas.
        yield return new WaitForSecondsRealtime(Delay);
        Launch();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void Launch()
    {
        try
        {
            var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            var manager = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory")
                .CallStatic<AndroidJavaObject>("create", activity);

            var request = manager.Call<AndroidJavaObject>("requestReviewFlow");
            request.Call<AndroidJavaObject>("addOnCompleteListener", new CompleteListener(task =>
            {
                if (!task.Call<bool>("isSuccessful"))
                {
                    Debug.LogWarning("InAppReview: Google Play okno pro hodnocení nepovolil.");
                    return;
                }
                var reviewInfo = task.Call<AndroidJavaObject>("getResult");
                manager.Call<AndroidJavaObject>("launchReviewFlow", activity, reviewInfo);
            }));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"InAppReview: hodnocení se nepodařilo spustit ({e.Message}).");
        }
    }

    private class CompleteListener : AndroidJavaProxy
    {
        private readonly Action<AndroidJavaObject> done;

        public CompleteListener(Action<AndroidJavaObject> done)
            : base("com.google.android.gms.tasks.OnCompleteListener")
        {
            this.done = done;
        }

        // Volá Java (Google Play), jméno musí zůstat přesně takhle.
        public void onComplete(AndroidJavaObject task) => done(task);
    }
#else
    private static void Launch()
    {
        Debug.Log("InAppReview: tady by se ukázalo okno Google Play pro hodnocení (jen v Android buildu z Google Play).");
    }
#endif
}
