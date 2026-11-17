using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class ResultScoreScript : MonoBehaviour
{
    
    //[SerializeField] private GameObject settings;
  //  private int countStar = 1;
 
    private GameObject scroll;
    private GameObject herbsInventory;
    public TextMeshProUGUI scoreText; 
    public TextMeshProUGUI gemsText; 
    //private string thisLevel;

    private void Awake()
    {
       // thisLevel = PlayerPrefs.GetString("LastLevel");
    }
    private void Start()
    {
        scroll = GameObject.FindWithTag("Scroll");
        herbsInventory = GameObject.FindWithTag("HerbsInventory");
        setPanelInActive(false);

    }


    public void SetScore(int score)
    {
        scoreText.text = score.ToString();
        scoreText.gameObject.SetActive(true);
        scoreText.transform.parent.gameObject.SetActive(true);

    }

    public void SetGems(int gems)
    {
        Cosmetics.AddGems(gems);


        gemsText.text = gems.ToString();
        gemsText.gameObject.SetActive(true);
        gemsText.transform.parent.gameObject.SetActive(true);

    }
    public void StartNextLevel() {        
       SceneManager.LoadScene("Game");
    }
    public void SetPause()
    {
        Time.timeScale = 0f;
        var image = gameObject.transform.Find("Image");
        var play = image.transform.Find("Play");
        play.gameObject.SetActive(true);
 
    }
    public void Play()
    {
        Time.timeScale = 1f;
        setPanelInActive(true);
        Destroy(gameObject);

    }
    public void ResetGame()
    {
        // PlayerPrefs.SetString("LastLevel", thisLevel);
        Time.timeScale = 1f;
        var sceneName = SceneManager.GetActiveScene().name;
        SceneLoader.Load(sceneName);
        
    }


    public void EndGame()
    {
        Time.timeScale = 1f;
        // Z levelu kampaně zpět na výběr levelů, ne do menu.
        if (CampaignSession.IsPlaying)
        {
            CampaignSession.Clear();
            SceneLoader.Load(CampaignSession.StoryScene);
            return;
        }
        SceneLoader.Load("Menu");
    }


    private void setPanelInActive(bool active)
    {
        herbsInventory.SetActive(active);
        scroll.SetActive(active);



    }

    public void Settings()
    {
       // var music = GameObject.Find("GameBackground").GetComponents<AudioSource>();
       // GameObject settingsObject = Instantiate(settings, transform.position, transform.rotation);
       // AudioSource[] audioSources = { music };
       // settingsObject.GetComponent<SettingsMenu>().SetVolumeMusic(music);

    }
}
