using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public class ScoreData
{
    public string playerName;
    public int score;
}

[System.Serializable]
public class LeaderboardData
{
    public List<ScoreData> scores = new List<ScoreData>();
}

public class LeaderboardManager : MonoBehaviour
{
    public TMP_Text leaderboardText;

    [Header("Live Leaderboard Data")]
    public LeaderboardData leaderboardData;

    private const string LeaderboardSaveKey = "Leaderboard";

    void Start()
    {
        LoadLeaderboard();
        UpdateDisplay();
    }

    public void AddScore(string playerName, int score)
    {
        if (string.IsNullOrEmpty(playerName) || playerName.Trim().Length == 0)
        {
            Debug.LogWarning("Attempted to save a score with an empty player name. Aborting.");
            return;
        }

        leaderboardData.scores.Add(new ScoreData { playerName = playerName, score = score });
        leaderboardData.scores = leaderboardData.scores.OrderByDescending(s => s.score).ToList();

        if (leaderboardData.scores.Count > 10)
        {
            leaderboardData.scores = leaderboardData.scores.GetRange(0, 10);
        }

        SaveLeaderboard();
        UpdateDisplay();
    }


    [ContextMenu("1. Load Saved Data into Inspector")]
    private void LoadDataForEditing()
    {
        LoadLeaderboard();
        Debug.Log("Leaderboard data loaded from PlayerPrefs into the Inspector.");
    }

    [ContextMenu("2. Save Inspector Data to PlayerPrefs")]
    private void SaveManualChanges()
    {
        SaveLeaderboard();
        Debug.Log("Current Inspector data has been saved to PlayerPrefs.");
        UpdateDisplay();
    }

    [ContextMenu("3. Clear All Saved Data")]
    private void ClearAllLeaderboardData()
    {
        leaderboardData = new LeaderboardData();
        SaveLeaderboard();
        Debug.LogWarning("All leaderboard data has been cleared.");
        UpdateDisplay();
    }


    private void SaveLeaderboard()
    {
        string json = JsonUtility.ToJson(leaderboardData);
        PlayerPrefs.SetString(LeaderboardSaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LeaderboardSaveKey))
        {
            string json = PlayerPrefs.GetString(LeaderboardSaveKey);
            leaderboardData = JsonUtility.FromJson<LeaderboardData>(json);
        }
        else
        {
            leaderboardData = new LeaderboardData();
        }
    }

    private void UpdateDisplay()
    {
        if (leaderboardText == null) return;

        StringBuilder sb = new StringBuilder();

        if (leaderboardData.scores.Count == 0)
        {
            sb.AppendLine("\n<align=center>No scores yet!</align>");
        }
        else
        {
            for (int i = 0; i < leaderboardData.scores.Count; i++)
            {
                sb.AppendLine($"<align=left><size=55%>{i + 1}. {leaderboardData.scores[i].playerName}<pos=70%><nobr>{leaderboardData.scores[i].score}</nobr></align>");
            }
        }

        leaderboardText.text = sb.ToString();
    }
}

