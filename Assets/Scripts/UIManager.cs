using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;  // ✅ 싱글톤 선언

    public TMP_Text turnDisplay;
    public TMP_Text timerDisplay;
    public TMP_Text winDisplay;

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