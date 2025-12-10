using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class Panel
    {
        public string panelName;
        public GameObject panelObject;
    }
    public List<Panel> panels = new List<Panel>();
    public TMP_InputField nameInputField;
    public GameController gameController;
    private string playerName;

    void Start()
    {
        ShowPanel("Start");
    }

    public void ShowPanel(string panelName)
    {
        foreach (var panel in panels)
        {
            panel.panelObject.SetActive(panel.panelName == panelName);
        }
    }

    public void OnStartButtonPressed()
    {
        ShowPanel("Name Input");
    }

    public void OnNameSubmitted()
    {
        playerName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("Please enter a name!");
            return;
        }
        PlayerPrefs.SetString("PlayerName", playerName);
        ShowPanel("Begin Game");
    }

    public void OnBeginGamePressed()
    {
        ShowPanel("Game");
        gameController.StartGame(playerName);
    }

    public void OnFinishButtonPressed()
    {
        gameController.EndGame();
    }

    public void OnRestartButtonPressed()
    {
        gameController.RestartGame();
    }


    public void OnPlayAgainButtonPressed()
    {
        if (gameController != null)
        {
            gameController.FullReset();
        }
        ShowPanel("Name Input");
    }
}