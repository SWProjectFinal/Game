using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Settings")]
    public List<PlayerController> players = new List<PlayerController>();

    [Header("Turn Settings")]
    public float turnTime = 30f;
    private float timer;
    private int currentPlayerIndex = 0;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        StartTurn();
    }

    private void Update()
    {
        if (!isGameOver)
        {
            TimerUpdate();
        }
    }

    public void StartTurn()
    {
        if (isGameOver) return;

        PlayerController currentPlayer = players[currentPlayerIndex];
        if (!currentPlayer.isAlive)
        {
            NextTurn();
            return;
        }

        timer = turnTime;
        UIManager.Instance.UpdateTurnDisplay(currentPlayer.playerName);
    }

    public void EndTurn()
    {
        NextTurn();
    }

    private void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn();
    }

    private void TimerUpdate()
    {
        timer -= Time.deltaTime;
        UIManager.Instance.UpdateTimerDisplay(timer);

        if (timer <= 0)
        {
            EndTurn();
        }
    }

    public void CheckWinCondition()
    {
        int aliveCount = 0;
        PlayerController lastAlive = null;

        foreach (var player in players)
        {
            if (player.isAlive)
            {
                aliveCount++;
                lastAlive = player;
            }
        }

        if (aliveCount == 1 && lastAlive != null)
        {
            isGameOver = true;
            UIManager.Instance.ShowWin(lastAlive.playerName);
        }
    }
}
