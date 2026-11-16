using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementsManagerScript : MonoBehaviour
{


    private AchievementList allAchievements;
    public GameObject achivNotification;
    public static AchievementsManagerScript Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        TextAsset jsonText = Resources.Load<TextAsset>("Achievements/achievements"); // bez .json
        if (jsonText != null)
        {
            allAchievements = JsonUtility.FromJson<AchievementList>(jsonText.text);

        }
        else
        {
            Debug.LogError("JSON file not found in Resources!");
        }
    }

    public void UnlockAchievement(string id)
    {

        var achievement = allAchievements.achievements.FirstOrDefault(a => a.id == id);
        if (PlayerPrefs.GetInt(id, 0) == 0)
        {
            PlayerPrefs.SetInt(id, 1);
            PlayerPrefs.Save();
            achivNotification.GetComponent<AchievementNotification>().ShowNotification(achievement.name);
        }    
        
    }


    [System.Serializable]
    public class UnlockedWrapper
    {
        public List<string> unlocked;
    }

}