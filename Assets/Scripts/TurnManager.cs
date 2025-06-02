using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("플레이어 리스트")]
    public List<PlayerController> players = new List<PlayerController>();

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

    public void StartTurn()
    {
        if (isGameOver) return;

        PlayerController currentPlayer = players[currentPlayerIndex];
        if (!currentPlayer.isAlive)
        {
            NextTurn();
            return;
        }

        UIManager.Instance.UpdateTurnDisplay(currentPlayer.playerName);
        TurnTimer.Instance.StartTimer();
    }

    public void EndTurn()
    {
        TurnTimer.Instance.StopTimer();
        NextTurn();
    }

    private void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn();
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
            TurnTimer.Instance.StopTimer();
            UIManager.Instance.ShowWin(lastAlive.playerName);
        }
    }
}