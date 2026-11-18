using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// Reklama s odměnou (AdMob). Žije přes všechny scény, sama se načte po startu hry
/// a po každém zhlédnutí načte další. Výsledková tabule se ptá na IsReady a volá Show.
/// V Editoru a vývojových buildech jede testovací jednotka Googlu, skutečná jen v release buildu
/// (klikání na vlastní skutečné reklamy porušuje pravidla AdMobu).
/// </summary>
public class Rewarded : MonoBehaviour
{
#if UNITY_ANDROID
    private const string ReleaseUnitId = "ca-app-pub-2771443006207374/9130330574";
    private const string TestUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
    private const string ReleaseUnitId = "unused"; // iOS jednotka zatím neexistuje
    private const string TestUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string ReleaseUnitId = "unused";
    private const string TestUnitId = "unused";
#endif

    private static string UnitId => Debug.isDebugBuild ? TestUnitId : ReleaseUnitId;

    private const float MinRetryDelay = 5f;
    private const float MaxRetryDelay = 120f;

    private static Rewarded instance;

    /// <summary>Změnila se dostupnost reklamy (načetla se, nebo se právě spotřebovala).</summary>
    public static event Action ReadyChanged;

    public static bool IsReady => instance != null && instance.ad != null && instance.ad.CanShowAd();

    private RewardedAd ad;
    private bool loading;
    private float retryDelay = MinRetryDelay;
    private Action pendingReward;
    private Action pendingSkip;
    private bool rewardEarned;

    // Callbacky SDK můžou přijít mimo hlavní vlákno Unity, proto jen zařadí práci sem
    // a Update ji provede na hlavním vlákně.
    private readonly Queue<Action> mainThread = new Queue<Action>();

    private static bool bootstrapping;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("Rewarded");
        DontDestroyOnLoad(go);
        bootstrapping = true;
        instance = go.AddComponent<Rewarded>();
        bootstrapping = false;
    }

    private void Awake()
    {
        // Jediná instance vzniká v Bootstrap. Komponenta přidaná ručně do scény nebo prefabu se zruší.
        if (bootstrapping) return;
        enabled = false; // vypnutá komponenta nedostane Start ani Update
        Destroy(this);
    }

    private void Start()
    {
        // Načítá se až po souhlasu (GDPR) a spuštění SDK, viz AdsConsent.
        AdsConsent.WhenReady(Load);
    }

    // Play mode bez reloadu domény by jinak držel statiku z minulého spuštění.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        bootstrapping = false;
        ReadyChanged = null;
    }

    private void Update()
    {
        while (true)
        {
            Action work;
            lock (mainThread)
            {
                if (mainThread.Count == 0) break;
                work = mainThread.Dequeue();
            }
            work();
        }
    }

    private void Post(Action work)
    {
        lock (mainThread) mainThread.Enqueue(work);
    }

    private void Load()
    {
        if (loading || ad != null) return;
        loading = true;

        RewardedAd.Load(UnitId, new AdRequest(), (RewardedAd loaded, LoadAdError error) => Post(() =>
        {
            loading = false;
            if (error != null || loaded == null)
            {
                Debug.LogWarning($"Rewarded: reklama se nenačetla ({error?.GetMessage()}), další pokus za {retryDelay} s.");
                StartCoroutine(RetryLater(retryDelay));
                retryDelay = Mathf.Min(retryDelay * 2f, MaxRetryDelay);
                return;
            }

            retryDelay = MinRetryDelay;
            ad = loaded;
            ad.OnAdFullScreenContentClosed += () => Post(OnAdFinished);
            ad.OnAdFullScreenContentFailed += failure => Post(() =>
            {
                Debug.LogWarning($"Rewarded: reklama se nezobrazila ({failure?.GetMessage()}).");
                OnAdFinished();
            });
            ReadyChanged?.Invoke();
        }));
    }

    private IEnumerator RetryLater(float delay)
    {
        // Realtime: výsledková tabule může mít zastavený čas.
        yield return new WaitForSecondsRealtime(delay);
        Load();
    }

    /// <summary>
    /// Ukáže reklamu. Po zavření se zavolá (na hlavním vlákně) onRewarded, když ji hráč dokoukal,
    /// jinak onSkipped. Vrátí false, když reklama není připravená.
    /// </summary>
    public static bool Show(Action onRewarded, Action onSkipped = null)
    {
        if (!IsReady) return false;
        var self = instance;
        self.pendingReward = onRewarded;
        self.pendingSkip = onSkipped;
        self.rewardEarned = false;
        self.ad.Show(_ => self.Post(() => self.rewardEarned = true));
        return true;
    }

    // Reklama se zavřela (nebo se nepodařilo ji otevřít): odměna, úklid a načtení další.
    private void OnAdFinished()
    {
        // Odměna může dorazit těsně po zavření, proto se vyhodnotí až o snímek později.
        StartCoroutine(FinishNextFrame());
    }

    private IEnumerator FinishNextFrame()
    {
        yield return null;
        var done = rewardEarned ? pendingReward : pendingSkip;
        pendingReward = null;
        pendingSkip = null;
        rewardEarned = false;

        if (ad != null)
        {
            ad.Destroy();
            ad = null;
        }
        ReadyChanged?.Invoke();
        done?.Invoke();
        Load();
    }
}
