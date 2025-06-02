using UnityEngine;

public class TurnTimer : MonoBehaviour
{
    public static TurnTimer Instance;

    [Header("타이머 설정")]
    public float turnDuration = 30f;
    private float timer;
    private bool isTimerRunning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        timer -= Time.deltaTime;
        UIManager.Instance.UpdateTimerDisplay(timer);

        if (timer <= 0f)
        {
            TurnManager.Instance.EndTurn();
        }
    }

    public void StartTimer()
    {
        timer = turnDuration;
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }
}