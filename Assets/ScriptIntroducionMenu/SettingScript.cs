using UnityEngine;
using UnityEngine.UI;

public class SettingScript : MonoBehaviour
{
    public Image MusicButton;
    public Image SoundButton;
    public Sprite MusicOnSprite;
    public Sprite MusicOffSprite;
    public Sprite SoundOnSprite;
    public Sprite SoundOffSprite;
    [Tooltip("Tlacitko nastaveni soukromi reklam (GDPR). Je videt jen tam, kde to Google vyzaduje.")]
    public GameObject privacyButton;
    private AudioSource menuAudio;
    private void Start()
    {
        if (privacyButton != null) privacyButton.SetActive(AdsConsent.PrivacyOptionsRequired);

        menuAudio = GameObject.Find("MenuCanvas").GetComponent<AudioSource>();

        string musicActive = PlayerPrefs.GetString("MusicActive", "1");
        string soundActive = PlayerPrefs.GetString("SoundActive", "1");

        // Hudba
        if (musicActive == "1")
        {
            MusicButton.sprite = MusicOnSprite;
            menuAudio.volume = 1f;
        }
        else
        {
            MusicButton.sprite = MusicOffSprite;
            menuAudio.volume = 0f;
        }

        // Zvuky (zatím se sprite jen pøepíná, bez ztlumení mixeru)
        if (soundActive == "1")
        {
            SoundButton.sprite = SoundOnSprite;
        }
        else
        {
            SoundButton.sprite = SoundOffSprite;
        }

    }
    public void turnOffMusic() {

        var active = PlayerPrefs.GetString("MusicActive","1");
        if (active == "1")
        {
            active = "0";
            MusicButton.sprite = MusicOffSprite;
            menuAudio.volume = 0f;
        }
        else
        {
            active = "1";
            MusicButton.sprite = MusicOnSprite;
            menuAudio.volume = 1f;
        }

        PlayerPrefs.SetString("MusicActive", active);
        
    }
    public void turnOffSound()
    {
        var active = PlayerPrefs.GetString("SoundActive","1");

        if (active == "1")
        {
            active = "0";
            SoundButton.sprite = SoundOffSprite;
        }
        else
        {
            active = "1";
            SoundButton.sprite = SoundOnSprite;
        }
        PlayerPrefs.SetString("SoundActive", active);

    }

    public void OpenPrivacyOptions()
    {
        AdsConsent.ShowPrivacyOptions();
    }

    public void BackMenu()
    {

        Destroy(gameObject);


    }
}