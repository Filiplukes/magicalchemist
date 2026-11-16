using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class EffectManagerScript : MonoBehaviour
{
    public List<GameObject> spellEffectObjects;
    public AudioMixer myMixer;

    private Dictionary<string, GameObject> spellEffect;
    void Start()
    {
        InitializeSpellEffectDictionary();
        SetAllAudioVolume();
    }

    public void PlaySpellEffect(string nameSpellAktual)
    {
        if (!spellEffect.TryGetValue(nameSpellAktual, out GameObject effect)) return;

        // Uložíme pùvodní stav
        Vector3 originalScale = effect.transform.localScale;
        Vector3 originalPosition = effect.transform.position;

        effect.SetActive(true);

        StartCoroutine(ShrinkAndReset(effect, originalScale, originalPosition, 2f));
    }

    private IEnumerator ShrinkAndReset(GameObject obj, Vector3 originalScale, Vector3 originalPosition, float duration)
    {
        float timer = 0f;
        Vector3 startScale = originalScale;

        while (timer < duration)
        {
            float t = timer / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            timer += Time.deltaTime;
            yield return null;
        }

        // Ujisti se, že je nula
        obj.transform.localScale = Vector3.zero;

        // Deaktivuj a resetuj
        obj.SetActive(false);
        obj.transform.localScale = originalScale;
        obj.transform.position = originalPosition;
    }

    private void InitializeSpellEffectDictionary()
    {
        spellEffect = new Dictionary<string, GameObject>();

        foreach (GameObject obj in spellEffectObjects)
        {
            if (obj != null)
            {
                string key = obj.name;
                if (!spellEffect.ContainsKey(key))
                {
                    spellEffect.Add(key, obj);
                }
                else
                {
                    Debug.LogWarning($"Duplicate spellEffect key: {key} – ignoring.");
                }
            }
        }

    }

    public void SetAllAudioVolume()
    {
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        var MusicActive = float.Parse(PlayerPrefs.GetString("MusicActive", "1"));
        var SoundActive = float.Parse(PlayerPrefs.GetString("SoundActive", "1"));

 

        foreach (AudioSource audio in allAudioSources)
        {
            audio.volume = SoundActive; // napø. 0f = ticho, 1f = plná hlasitost
        }

        if (SoundActive == 0)
        {
            SoundActive = -80f;
        }
        myMixer.SetFloat("PotionVolume", SoundActive);

        gameObject.GetComponent<AudioSource>().volume = MusicActive;
    }
}
