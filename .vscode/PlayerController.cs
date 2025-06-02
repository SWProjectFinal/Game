using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public int playerId;
    public string playerName;
    public bool isAlive = true;

    // 테스트용으로 플레이어를 죽이는 함수 (나중에 실제 게임 로직에서 호출)
    public void Die()
    {
        isAlive = false;
        GameManager.Instance.CheckWinCondition();
        gameObject.SetActive(false); // 죽으면 사라짐
    }
}
