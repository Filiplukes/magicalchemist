using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject leadeBoardPrefab;
    public GameObject achievementsPrefab;
    public GameObject settingsPrefab;
    public GameObject nameChecker;
    public GameObject herbalCodexPrefab;
    public GameObject shopPrefab;
    // Naète scénu podle jména

    private void Start()
    {
        gameObject.GetComponent<AudioSource>().volume = float.Parse(PlayerPrefs.GetString("MusicActive", "1"));
    }
    public void LoadTimeMode()
    {
       // PlayerPrefs.DeleteKey("PlayerName");
      //  PlayerPrefs.Save();

        if (nameChecker.GetComponent<NameChecker>().CheckName()) {
            SceneManager.LoadScene("TimeMode");
        }      
    }

    public void LifeMode()
    {
        // PlayerPrefs.DeleteKey("PlayerName");
        //  PlayerPrefs.Save();

        if (nameChecker.GetComponent<NameChecker>().CheckName())
        {
            SceneManager.LoadScene("LifeMode");
        }
    }

    public void ExitGame()
    {
 
        Application.Quit();

    }
    public void OpenLeaderboard() {

        Instantiate(leadeBoardPrefab);
    }
    public void OpenAchievements()
    {

        Instantiate(achievementsPrefab);
    }
    public void OpenSettings() {

        Instantiate(settingsPrefab);
    }   
    public void OpenHerbalCodex() {

        Instantiate(herbalCodexPrefab);
    }
    public void OpenShop()
    {

        Instantiate(shopPrefab);
    }


    public void LoadCampaign()
     {
        
          SceneManager.LoadScene("Campaign");
    }
}