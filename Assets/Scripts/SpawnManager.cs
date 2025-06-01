using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public LayerMask groundLayer;
    public Vector2 spawnAreaMin;
    public Vector2 spawnAreaMax;
    public int maxAttempts = 20;

    void Start()
    {
        SpawnPlayer(); // 게임 시작 시 자동 실행
    }

    public void SpawnPlayer()
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                spawnAreaMax.y
            );

            RaycastHit2D hit = Physics2D.Raycast(randomPos, Vector2.down, 20f, groundLayer);
            if (hit.collider != null)
            {
                Vector2 spawnPoint = hit.point + Vector2.up * 0.5f;
                Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
                Debug.Log($"🎉 캐릭터 생성됨: {spawnPoint}");
                return;
            }
        }

        Debug.LogWarning("⚠️ 캐릭터 스폰 실패: 땅이 없음");
    }
}
