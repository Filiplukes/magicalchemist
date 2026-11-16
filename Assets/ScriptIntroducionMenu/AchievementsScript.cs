using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Achievements : MonoBehaviour
{

    private List<AchievementData> achievements;

    public GameObject achievementUIPrefab;
    public Transform achievementsContainer;
    public Sprite successSprite;
    public Sprite failSprite;

    void Start()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("Achievements/achievements"); // bez .json
        if (jsonText != null)
        {
            AchievementList list = JsonUtility.FromJson<AchievementList>(jsonText.text);
            achievements = list.achievements;
            foreach (var achievement in achievements) {
                CreateAchievementUI(achievement);
            }
        }
        else
        {
            Debug.LogError("JSON file not found in Resources!");
        }
    
}

    void CreateAchievementUI(AchievementData data)
    {
        GameObject item = Instantiate(achievementUIPrefab, achievementsContainer);
            // Ikona úspìšnosti (root objekt má Image komponentu)
            Image icon = item.GetComponent<Image>();
      
        


        if (icon != null)
        {
            if (PlayerPrefs.GetInt(data.id, 0)==0) {
                icon.sprite = failSprite;
            }
            else
            {
                item.GetComponent<AchivDescriptionScript>().SetIsActiveAchiv(true);
                icon.sprite = successSprite;
                Color color = icon.color;
                color.a = 1f; // 1 = plnì neprùhledný, 0 = plnì prùhledný
                icon.color = color;
                Transform nameObj = item.transform.Find("Name");
                if (nameObj != null)
                {
                    nameObj.gameObject.SetActive(true);
                    TextMeshProUGUI name = nameObj.GetComponent<TextMeshProUGUI>();
                    if (name != null) name.text = data.name;
                }
                Transform descriptionObj = item.transform.Find("Description");


                if (descriptionObj != null)
                {                   
                    TextMeshProUGUI description = descriptionObj.GetComponent<TextMeshProUGUI>();
                    if (description != null) description.text = data.description;
                }
            }

        }



    }


    public void BackMenu()
    {

        Destroy(gameObject);


    }
}

[System.Serializable]
public class AchievementData
{
    public string id;
    public string name;
    public string description;
    public bool unlocked;
}

[Serializable]
public class AchievementList
{
    public List<AchievementData> achievements;
}