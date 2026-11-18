using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// Souhlas s reklamami (Google UMP, GDPR). Po startu hry zjistí, jestli hráč musí dát souhlas,
/// a když ano, ukáže dialog nastavený v AdMobu (Soukromí a zprávy). Teprve potom spustí
/// reklamní SDK. AdIns i Rewarded proto reklamy načítají až přes WhenReady.
/// Zprávu i její texty spravuje AdMob podle App ID, v kódu žádné ID není.
/// </summary>
public class AdsConsent : MonoBehaviour
{
    private static AdsConsent instance;
    private static bool sdkStarting;
    private static bool sdkReady;
    private static readonly List<Action> waiting = new List<Action>();

    // Callbacky UMP a SDK můžou přijít mimo hlavní vlákno Unity, Update je provede na něm.
    private readonly Queue<Action> mainThread = new Queue<Action>();

    /// <summary>Hráč si může souhlas změnit (v EU). Podle toho se v nastavení ukáže tlačítko.</summary>
    public static bool PrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    // Play mode bez reloadu domény by jinak držel statiku z minulého spuštění.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        sdkStarting = false;
        sdkReady = false;
        waiting.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("AdsConsent");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AdsConsent>();
    }

    /// <summary>
    /// Zavolá akci, až bude rozhodnuto o souhlasu a SDK poběží (hned, pokud už běží).
    /// Dokud reklamy nejsou povolené, akce čeká (třeba na změnu v nastavení soukromí).
    /// </summary>
    public static void WhenReady(Action action)
    {
        if (sdkReady) action();
        else waiting.Add(action);
    }

    private void Start()
    {
        ConsentInformation.Update(new ConsentRequestParameters(), updateError => Post(() =>
        {
            if (updateError != null)
                Debug.LogWarning($"AdsConsent: stav souhlasu se nepodařilo zjistit ({updateError.Message}).");

            // Ukáže dialog jen tam, kde je potřeba (EU, ještě nerozhodnuto). Jinak hned skončí.
            ConsentForm.LoadAndShowConsentFormIfRequired(formError => Post(() =>
            {
                if (formError != null)
                    Debug.LogWarning($"AdsConsent: dialog souhlasu se nezobrazil ({formError.Message}).");
                StartAds();
            }));
        }));
    }

    private void StartAds()
    {
        // Souhlas z minula platí i bez sítě, proto se ptá až po Update i když skončil chybou.
        if (sdkStarting) return;
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.Log("AdsConsent: reklamy zatím nejsou povolené.");
            return;
        }

        sdkStarting = true;
        MobileAds.Initialize(_ => Post(() =>
        {
            sdkReady = true;
            var actions = waiting.ToArray();
            waiting.Clear();
            foreach (var action in actions) action();
        }));
    }

    /// <summary>Znovu otevře dialog souhlasu (tlačítko v nastavení).</summary>
    public static void ShowPrivacyOptions()
    {
        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            if (error != null) Debug.LogWarning($"AdsConsent: nastavení soukromí se neotevřelo ({error.Message}).");
            // Když hráč souhlas teď povolil poprvé, rozjedou se reklamy, které dosud čekaly.
            if (instance != null) instance.Post(instance.StartAds);
        });
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
}
