using System.Collections.Generic;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;


class LeaderboardsDeployment : MonoBehaviour
{
    public List <TextMeshProUGUI> ListTextScore;
    public List <TextMeshProUGUI> ListScore;
    public List <TextMeshProUGUI> Places;
    [SerializeField] private GameObject loadingSpinner;

    private async void Start()
    {
        loadingSpinner.SetActive(true);
        SetTableText(await LeaderBoardNonDestroyObject.Instance.GetPlayerAndLowerScores());
        loadingSpinner.SetActive(false);
    }

    private async void LoadTable() {
        loadingSpinner.SetActive(true);
        SetTableText(await LeaderBoardNonDestroyObject.Instance.GetPlayerAndLowerScores());
        loadingSpinner.SetActive(false);
    }

    public async void SetTopScore() {
         loadingSpinner.SetActive(true);
        SetTableText(await LeaderBoardNonDestroyObject.Instance.GetTop10());
        loadingSpinner.SetActive(false);
    }

    public async void SetWorstScore()
    {
        loadingSpinner.SetActive(true);
        SetTableText(await LeaderBoardNonDestroyObject.Instance.GetWorst10());
        loadingSpinner.SetActive(false);
    }

    public async void SetUpScore()
    {
        loadingSpinner.SetActive(true);
        SetTableText(await LeaderBoardNonDestroyObject.Instance.GetPageUp());
        loadingSpinner.SetActive(false);
    }

    public async void SetDownScore()
    {
        loadingSpinner.SetActive(true);
        SetTableText(await LeaderBoardNonDestroyObject.Instance.GetPageDown());
        loadingSpinner.SetActive(false);
    }


    public void CloseLeaderboard() { 
    
        Destroy( gameObject);
    
    }


    public void ChangeTable()
    {
        LeaderBoardNonDestroyObject.Instance.SetLeaderboardType();
        LoadTable();

    }

    private void SetTableText(List<LeaderboardEntry> entries)
    {

        for (int i = 0; i < ListTextScore.Count; i++)
        {
            if (i < entries.Count)
            {   
                var entry = entries[i];
                string name = string.IsNullOrEmpty(entry.PlayerName) ? entry.PlayerId : entry.PlayerName;
                ListTextScore[i].text = $"{name.Split('#')[0]}";
                ListScore[i].text = $"{entry.Score}";
                Places[i].text = $"{entry.Rank +1}";
            }
            else
            {
                ListTextScore[i].text = ""; // Vyèistit nevyužité položky
                ListScore[i].text = ""; // Vyèistit nevyužité položky
                Places[i].text = ""; // Vyèistit nevyužité položky
            }
        }

    }

}
