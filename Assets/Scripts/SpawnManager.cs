using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject playerPrefab;

    public Vector2 spawnAreaMin = new Vector2(-10f, 0f);
    public Vector2 spawnAreaMax = new Vector2(10f, 5f);

    public LayerMask groundLayer;
    public int maxAttempts = 20;

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = spawnAreaMax.y;

            // �Ʒ��� ����ĳ��Ʈ (������ ã�� ����)
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, y), Vector2.down, 10f, groundLayer);

            if (hit.collider != null)
            {
                Vector2 spawnPos = hit.point + Vector2.up * 0.5f; // �� ���� �ణ ����
                Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                Debug.Log("ĳ���� ������: " + spawnPos);
                return;
            }
        }

        Debug.LogWarning("���� ��ġ�� ã�� ���߽��ϴ�!");
    }
}
