using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Text turnDisplay;
    public Text timerDisplay;
    public Text winDisplay;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void UpdateTurnDisplay(string playerName)
    {
        turnDisplay.text = "Current Turn: " + playerName;
    }

    public void UpdateTimerDisplay(float time)
    {
        timerDisplay.text = "Time Left: " + Mathf.Ceil(time) + "s";
    }

    public void ShowWin(string playerName)
    {
        winDisplay.text = playerName + " Wins!";
    }
}