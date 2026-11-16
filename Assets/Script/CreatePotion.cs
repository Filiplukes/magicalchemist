using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class CreatePotion : MonoBehaviour
{
    public GameObject potion;
    public AudioClip clip;
    public AudioMixer mixer;
    private AudioSource boil;
    private float defaultVolume = 1f;
    private const string PlayerPrefsKey = "Cauldron";

    private void Awake()
    {
        LoadSpriteFromResources(PlayerPrefs.GetString(PlayerPrefsKey));

    }
    private void Start()
    {   
        boil = gameObject.AddComponent<AudioSource>();
        boil.outputAudioMixerGroup = mixer.FindMatchingGroups("Master")[0];
        boil.clip = clip;
        boil.volume = defaultVolume;
        boil.loop = true;
        boil.Play();
    }
    public void CreateCompletePotion(string name, int quality) {

        if (quality > 0) {
            AchievementsManagerScript.Instance.UnlockAchievement("first_brew");        
        };
        Vector3 spawnPosition = transform.position + Vector3.up * 1f;  // 1 jednotka nad objektem
        var createdPotion = Instantiate(potion, spawnPosition, Quaternion.identity);
        createdPotion.GetComponent<RiseAndScalePotion>().SetSprite(name, quality);
    }
    

    public void SetVolumeDb(float dB)
    {
        mixer.SetFloat("PotionVolume", dB);
    }

    public void BoostVolumeSmooth(float boostedDb = 10f, float duration = 2f)
    {
        var SoundActive = float.Parse(PlayerPrefs.GetString("SoundActive", "1"));
        if (SoundActive == 1) {
            StartCoroutine(VolumeBoostRoutine(boostedDb, duration));
        }
    }

    private IEnumerator VolumeBoostRoutine(float boostedDb, float duration)
    {
        mixer.SetFloat("PotionVolume", boostedDb);
        yield return new WaitForSeconds(duration);
        mixer.SetFloat("PotionVolume", 0f);  // zpìt na normální hlasitost (0 dB)
    }

    public void LoadSpriteFromResources(string spriteName)
    {
        spriteName = spriteName.Substring(0, spriteName.Length - 2);
        if (string.IsNullOrEmpty(spriteName))
        {
            Debug.LogWarning("LoadSpriteFromResources: spriteName je prázdný.");
            return;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogWarning("Na objektu není SpriteRenderer!");
            return;
        }

        Sprite loadedSprite = Resources.Load<Sprite>("Cauldrons/" + spriteName);

        if (loadedSprite == null)
        {
            Debug.LogWarning($"Sprite '{spriteName}' nebyl nalezen v Resources/Cauldrons.");
            return;
        }

        sr.sprite = loadedSprite;
    }
}


