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
    private void Awake()
    {
        var cauldron = Cosmetics.GetSelected(CosmeticType.Cauldron);
        var sr = GetComponent<SpriteRenderer>();
        if (cauldron != null && cauldron.Sprite != null && sr != null)
            sr.sprite = cauldron.Sprite;
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
        mixer.SetFloat("PotionVolume", 0f);  // zpět na normální hlasitost (0 dB)
    }
}
