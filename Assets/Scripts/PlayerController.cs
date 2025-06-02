using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public int playerId;
    public string playerName; // <<< 이 줄이 public이어야 Inspector에 보임!

    public bool isAlive = true;

    public void Die()
    {
        isAlive = false;
        TurnManager.Instance.CheckWinCondition();
        gameObject.SetActive(false);
    }
}