using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Services.Leaderboards.Models;

public class LeaderBoardNonDestroyObject : MonoBehaviour
{
    public static LeaderBoardNonDestroyObject Instance { get; private set; }

    private static string leaderboardId = "Time_Score_Leaderboard";
    private LeaderboardEntry playerEntryCached;
    private GameObject leaderboardDeployment;
    public int aktualPageIndex;
    PageBase baseType = PageBase.Player;

    private const int itemsPerPage = 10;
    private async void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // zniË duplicitu
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // nezniËiteln˝ mezi scÈnami

        // Inicializace sluûeb
        await InitializeServices();
        await SignInAnonymously();
      //  await LogScoreAsync();
    }

    async Task InitializeServices()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
    }

    public async Task SignInAnonymously()
    {
        try
        {
            // Inicializace UGS (jen jednou)
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                // Zavol·nÌ nastavenÌ jmÈna
                await SetNameForScore();
            }
            else
            {
                await SetNameForScore(); // i tak m˘ûeö zavolat, pokud uû je p¯ihl·öen
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Chyba p¯i p¯ihl·öenÌ: " + e.Message);
        }
    }


    public async Task AddScoreAsync(int score, string table)
    {
        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(table, score);
        }
        catch
        {
            Debug.Log("Chyba z·pisu skÛre");
        }
    }

    public async Task<string> LogScoreAsync()
    {
        try
        {
            var result = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);

            if (result != null)
            {
                return result.Score.ToString();
            }
            else
            {
                Debug.LogWarning("TopScore nenÌ p¯i¯azen!");
                return "Play games to score points!";
            }
        }
        catch
        {
            return "Check your internet connection!";
        }

    }

    public async Task SetNameForScore()
    {
        // Ocisteni opravi i jmena ulozena starsi verzi (s mezerou je Unity odmitla
        // a hrac zustal v zebricku s nahodnym jmenem). Zakazane jmeno se nenahraje.
        string playerName = NameRules.Sanitize(PlayerPrefs.GetString("PlayerName"));

        if (!string.IsNullOrWhiteSpace(playerName) && NameRules.IsAllowed(playerName))
        {
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            }
            catch (Exception e)
            {
                Debug.LogError("Nepoda¯ilo se nastavit jmÈno: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("JmÈno nebylo nalezeno v PlayerPrefs.");
        }
    }

    // NaËte str·nku obsahujÌcÌ hr·Ëe (nastavÌ aktualPageIndex na hr·Ëovu str·nku)
    public async Task<List<LeaderboardEntry>> GetPlayerAndLowerScores()
    {
        try
        {
            LeaderboardEntry playerEntry = null;
            try
            {
                playerEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                return await GetTop10(); // fallback
            }

            if (playerEntry == null)
            {
                return await GetTop10(); // fallback
            }

            playerEntryCached = playerEntry;

            // UrËÌme index str·nky, ve kterÈ je hr·Ë (0-based)
            int playerRank = Mathf.Max(0, playerEntry.Rank);
            int playerPageIndex = playerRank / itemsPerPage;

            // NaËteme str·nku zaËÌnajÌcÌ na playerPageIndex * itemsPerPage
            var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
            {
                Offset = playerPageIndex * itemsPerPage,
                Limit = itemsPerPage
            });

            int totalCount = page.Total;
            int maxPageIndex = Mathf.Max(0, Mathf.CeilToInt((float)totalCount / itemsPerPage) - 1);
            aktualPageIndex = Mathf.Clamp(playerPageIndex, 0, maxPageIndex);
            baseType = PageBase.Player;

            // Pokud je m·lo poloûek (nap¯. hr·Ë blÌzko konce), m˘ûeme fallbacknout na poslednÌ str·nku
            if (page.Results.Count < itemsPerPage && playerPageIndex >= maxPageIndex)
            {
                return await GetWorst10();
            }

            return page.Results;
        }
        catch (Exception e)
        {
            Debug.LogError($"Chyba p¯i naËÌt·nÌ leaderboardu: {e.Message}");
            return null;
        }
    }

    public async Task<List<LeaderboardEntry>> GetTop10()
    {
        try
        {
            baseType = PageBase.Top;
            aktualPageIndex = 0;

            var topScores = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
            {
                Offset = 0,
                Limit = itemsPerPage
            });

            leaderboardDeployment = GameObject.FindWithTag("LeaderBoardPrefab");
            return topScores.Results;
        }
        catch (Exception e)
        {
            Debug.LogError($"Chyba p¯i naËÌt·nÌ top 10: {e.Message}");
            return null;
        }
    }

    public async Task<List<LeaderboardEntry>> GetWorst10()
    {
        try
        {
            baseType = PageBase.Bottom;

            // zjistÌme celkov˝ poËet z·znam˘
            var firstPage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
            {
                Offset = 0,
                Limit = 1
            });

            int totalCount = firstPage.Total;
            if (totalCount == 0)
            {
                aktualPageIndex = 0;
                return new List<LeaderboardEntry>();
            }

            int maxPageIndex = Mathf.Max(0, Mathf.CeilToInt((float)totalCount / itemsPerPage) - 1);
            aktualPageIndex = maxPageIndex;

            int offset = maxPageIndex * itemsPerPage;

            var worstScores = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
            {
                Offset = offset,
                Limit = itemsPerPage
            });

            leaderboardDeployment = GameObject.FindWithTag("LeaderBoardPrefab");
            return worstScores.Results;
        }
        catch (Exception e)
        {
            Debug.LogError($"Chyba p¯i naËÌt·nÌ nejhoröÌch 10: {e.Message}");
            return null;
        }
    }

    public async Task<List<LeaderboardEntry>> GetPageUp()
    {
        var totalCount = await getTotalCount();
        int maxPageIndex = Mathf.Max(0, Mathf.CeilToInt((float)totalCount / itemsPerPage) - 1);

        // posun na p¯edchozÌ str·nku (smÏrem k zaË·tku)
        aktualPageIndex = Mathf.Clamp(aktualPageIndex - 1, 0, maxPageIndex);

        // pokud jsme na prvnÌ str·nce, vraù Top10
        if (aktualPageIndex == 0)
        {
            return await GetTop10();
        }

        int offset = aktualPageIndex * itemsPerPage;
        var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
        {
            Offset = offset,
            Limit = itemsPerPage
        });

        return page.Results;
    }

    public async Task<List<LeaderboardEntry>> GetPageDown()
    {
        var totalCount = await getTotalCount();
        int maxPageIndex = Mathf.Max(0, Mathf.CeilToInt((float)totalCount / itemsPerPage) - 1);

        // posun na dalöÌ str·nku (smÏrem k konci)
        aktualPageIndex = Mathf.Clamp(aktualPageIndex + 1, 0, maxPageIndex);

        // pokud jsme na poslednÌ str·nce, vraù Worst10
        if (aktualPageIndex == maxPageIndex)
        {
            return await GetWorst10();
        }

        int offset = aktualPageIndex * itemsPerPage;
        var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
        {
            Offset = offset,
            Limit = itemsPerPage
        });

        return page.Results;
    }


    private async Task<int> getTotalCount()
    {
        var firstScorePage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
        {
            Offset = 0,
            Limit = 1
        });
        return firstScorePage.Total;
    }
    public void SetLeaderboardType()
    {
        switch (leaderboardId)
        {
            case "Time_Score_Leaderboard":
                leaderboardId = "Life_Score_Leaderboard";
                break;
            case "Life_Score_Leaderboard":
                leaderboardId = "Time_Score_Leaderboard";
                break;
            default:
                leaderboardId = "Time_Score_Leaderboard";
                break;
        }
    }
}

public enum PageBase
{
    Player,
    Top,
    Bottom
}