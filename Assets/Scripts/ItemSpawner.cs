using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq; // ← 이 줄이 없다면 추가

public class ItemSpawner : MonoBehaviourPun
{
    public static ItemSpawner Instance { get; private set; }

    [Header("스폰 설정")]
    public GameObject itemBoxPrefab;        // 아이템 박스 프리팹 (더미 또는 친구 것)
    public Vector2 mapBounds = new Vector2(15f, 8f); // 스폰 범위
    public float spawnHeight = 2f;          // 스폰 높이
    public LayerMask groundLayer = 1;       // 바닥 레이어

    [Header("스폰 개수 확률")]
    [Range(0f, 1f)] public float spawn3Chance = 0.1f;  // 3개 스폰 확률 (20%)
    [Range(0f, 1f)] public float spawn2Chance = 0.2f;  // 2개 스폰 확률 (30%)
    // 1개 스폰 확률은 나머지 (70%)

    [Header("스폰 관리")]
    public List<GameObject> spawnedBoxes = new List<GameObject>(); // 현재 스폰된 박스들 (무제한)

    [Header("아이템 밸런스 확률")]
    public ItemDropTable itemDropTable;

    [Header("더미 테스트용")]
    public bool useDummySystem = true;      // 더미 시스템 사용 여부
    public GameObject dummyBoxPrefab;       // 더미 박스 프리팹

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // TurnManager 이벤트 구독
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart += OnTurnStarted;
        }
        else
        {
            Debug.LogWarning("TurnManager.Instance가 null입니다! 나중에 다시 시도합니다.");
            // 나중에 다시 시도
            StartCoroutine(WaitForTurnManager());
        }

        Debug.Log("ItemSpawner 초기화 완료!");
    }

    // TurnManager가 준비될 때까지 대기
    System.Collections.IEnumerator WaitForTurnManager()
    {
        while (TurnManager.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        TurnManager.Instance.OnTurnStart += OnTurnStarted;
        Debug.Log("TurnManager 연결 완료!");
    }

    // 턴 시작 시 호출되는 함수
    void OnTurnStarted(Photon.Realtime.Player currentPlayer)
    {
        // 마스터 클라이언트만 아이템 스폰 관리
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"턴 시작: {currentPlayer?.NickName ?? "봇"} - 아이템 스폰 시작");

        // 스폰할 개수 결정 (1~3개)
        int spawnCount = DecideSpawnCount();
        Debug.Log($"이번 턴 스폰 개수: {spawnCount}개");

        // 결정된 개수만큼 스폰
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnItemBox();
        }

        // null 참조 정리 (파괴된 박스들)
        CleanupNullReferences();
    }

    // 스폰할 아이템 개수 결정
    int DecideSpawnCount()
    {
        float randomValue = Random.Range(0f, 1f);

        if (randomValue <= spawn3Chance)
        {
            Debug.Log($"3개 스폰! ({randomValue:F2} <= {spawn3Chance:F2})");
            return 3;
        }
        else if (randomValue <= spawn3Chance + spawn2Chance)
        {
            Debug.Log($"2개 스폰! ({randomValue:F2} <= {spawn3Chance + spawn2Chance:F2})");
            return 2;
        }
        else
        {
            Debug.Log($"1개 스폰! ({randomValue:F2} > {spawn3Chance + spawn2Chance:F2})");
            return 1;
        }
    }

    // 아이템 박스 스폰
    void SpawnItemBox()
    {
        Vector3 spawnPosition = GetValidSpawnPosition();

        // 유효한 위치를 찾지 못하면 스폰 안 함
        if (spawnPosition == Vector3.zero)
        {
            Debug.LogWarning("🚫 이번 턴은 스폰 위치를 찾지 못해 아이템 박스를 생성하지 않습니다.");
            return;
        }

        // 네트워크로 모든 클라이언트에 스폰 알림
        photonView.RPC("RPC_SpawnItemBox", RpcTarget.All, spawnPosition);
    }

    // 유효한 스폰 위치 찾기 (Z 완전 음수, Ground 레이어)
    // 유효한 스폰 위치 찾기 (기존 박스와 거리 체크 포함)
    Vector3 GetValidSpawnPosition()
    {
        int maxAttempts = 15; // 시도 횟수 늘리기

        for (int i = 0; i < maxAttempts; i++)
        {
            // ✅ mapBounds 사용하도록 수정
            float x = Random.Range(-mapBounds.x, mapBounds.x);   // 전체 맵 범위 사용
            float z = Random.Range(-mapBounds.y, mapBounds.y);   // 전체 맵 범위 사용

            // 단, Z가 양수가 되지 않도록 제한 (카메라 뒤쪽 방지)
            if (z > -0.2f) z = Random.Range(-mapBounds.y, -0.2f);

            // 충분히 높은 곳에서 시작
            Vector3 rayStart = new Vector3(x, 10f, z);

            // Ground 레이어만 감지하도록 Raycast
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 15f, groundLayer);

            if (hit.collider != null)
            {
                // 지형 위 0.3만큼 위에 스폰 (더 낮게)
                Vector3 spawnPos = new Vector3(x, hit.point.y + 0.3f, z);

                // ✅ 기존 박스들과 거리 체크 추가
                if (IsPositionValid(spawnPos))
                {
                    Debug.Log($"✅ 유효한 스폰 위치: {spawnPos} (지형: {hit.point.y:F2})");
                    return spawnPos;
                }
                else
                {
                    Debug.Log($"❌ 다른 박스와 너무 가까움: {spawnPos}");
                }
            }
            else
            {
                Debug.LogWarning($"❌ Ground 레이어 감지 실패: {rayStart}");
            }
        }

        // 모든 시도 실패하면 null 반환 (스폰 안 함)
        Debug.LogWarning($"⚠️ 적절한 스폰 위치를 찾지 못했습니다!");
        return Vector3.zero; // 스폰 안 함
    }

    // 위치 유효성 검사 (거리 체크)
    bool IsPositionValid(Vector3 position)
    {
        float itemMinDistance = 1.0f; // 아이템 간 최소 거리 (미터)
        float playerMinDistance = 1.0f; // 플레이어와 최소 거리 (미터)

        // 1. 기존 박스들과 거리 체크
        foreach (GameObject box in spawnedBoxes)
        {
            if (box != null)
            {
                float distance = Vector3.Distance(position, box.transform.position);
                if (distance < itemMinDistance)
                {
                    Debug.Log($"📦 아이템 거리 체크 실패: {distance:F2}m < {itemMinDistance}m");
                    return false; // 너무 가까움
                }
            }
        }

        // 2. 플레이어들과 거리 체크
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                float distance = Vector3.Distance(position, player.transform.position);
                if (distance < playerMinDistance)
                {
                    Debug.Log($"🏃 플레이어 거리 체크 실패: {distance:F2}m < {playerMinDistance}m");
                    return false; // 플레이어와 너무 가까움
                }
            }
        }

        Debug.Log($"✅ 모든 거리 체크 통과!");
        return true; // 모든 체크 통과
    }

    // RPC: 모든 클라이언트에서 아이템 박스 생성
    [PunRPC]
    void RPC_SpawnItemBox(Vector3 position)
    {
        GameObject prefabToUse = GetItemBoxPrefab();

        if (prefabToUse != null)
        {
            GameObject newBox = Instantiate(prefabToUse, position, Quaternion.identity);

            // 스폰된 박스 목록에 추가
            spawnedBoxes.Add(newBox);

            Debug.Log($"아이템 박스 스폰 완료: {position} (총 {spawnedBoxes.Count}개)");
        }
        else
        {
            Debug.LogError("아이템 박스 프리팹이 없습니다!");
        }
    }

    // 사용할 아이템 박스 프리팹 결정
    GameObject GetItemBoxPrefab()
    {
        if (useDummySystem && dummyBoxPrefab != null)
        {
            return dummyBoxPrefab; // 더미 시스템 사용
        }
        else if (itemBoxPrefab != null)
        {
            return itemBoxPrefab; // 친구의 실제 프리팹 사용
        }

        return null;
    }

    // 박스 목록에서 제거 (플레이어가 먹었을 때 호출)
    public void RemoveBoxFromList(GameObject box)
    {
        if (spawnedBoxes.Contains(box))
        {
            spawnedBoxes.Remove(box);
            Debug.Log($"박스 제거: 남은 박스 {spawnedBoxes.Count}개");
        }
    }

    // null 참조 정리 (파괴된 박스들)
    void CleanupNullReferences()
    {
        int beforeCount = spawnedBoxes.Count;
        spawnedBoxes.RemoveAll(box => box == null);
        int afterCount = spawnedBoxes.Count;

        if (beforeCount != afterCount)
        {
            Debug.Log($"null 참조 정리: {beforeCount - afterCount}개 제거, 현재 {afterCount}개");
        }
    }

    // ✅ 아이템 습득 처리 (DummyItemBox에서 호출)
    public void OnItemPickedUp(GameObject itemBox, string playerName, string itemName)
    {
        // 마스터 클라이언트만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"🎁 [Master] {playerName}이 {itemName} 습득 - 네트워크 동기화 시작");

        // 습득 이펙트 재생
        DummyItemBox dummyBox = itemBox.GetComponent<DummyItemBox>();
        if (dummyBox != null)
        {
            dummyBox.PlayPickupEffect();
        }

        // RPC로 모든 클라이언트에 아이템 습득 알림
        photonView.RPC("RPC_ItemPickedUp", RpcTarget.All, playerName, itemName, itemBox.transform.position.x, itemBox.transform.position.y, itemBox.transform.position.z);

        // 박스 목록에서 제거
        RemoveBoxFromList(itemBox);

        // 박스 삭제
        Destroy(itemBox);
    }

    // ✅ RPC: 모든 클라이언트에서 아이템 습득 처리 (수정된 버전)
    [PunRPC]
    void RPC_ItemPickedUp(string playerName, string itemName, float posX, float posY, float posZ)
    {
        Debug.Log($"🎁 [RPC] {playerName}이 아이템 습득: {itemName} 위치: ({posX:F2}, {posY:F2}, {posZ:F2})");

        // ✅ spawnedBoxes 리스트에서 해당 위치의 박스 찾기
        Vector3 itemPosition = new Vector3(posX, posY, posZ);
        GameObject boxToRemove = FindBoxByPosition(itemPosition);

        if (boxToRemove != null)
        {
            Debug.Log($"🎁 [RPC] 위치 매칭된 박스 발견: {boxToRemove.name}");

            // 이펙트 재생 (아직 안 했다면)
            DummyItemBox dummyBox = boxToRemove.GetComponent<DummyItemBox>();
            if (dummyBox != null)
            {
                dummyBox.PlayPickupEffect();
            }

            // 박스 제거
            RemoveBoxFromList(boxToRemove);
            Destroy(boxToRemove);
            Debug.Log($"🎁 [RPC] 박스 삭제 완료: {boxToRemove.name}");
        }
        else
        {
            Debug.LogWarning($"🎁 [RPC] 해당 위치의 박스를 찾지 못했습니다: ({posX:F2}, {posY:F2}, {posZ:F2})");

            // ✅ 백업: 모든 박스 중에서 가장 가까운 것 찾기
            GameObject closestBox = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject box in spawnedBoxes.ToArray())
            {
                if (box != null)
                {
                    float distance = Vector3.Distance(box.transform.position, itemPosition);
                    if (distance < closestDistance && distance < 2f) // 2미터 이내
                    {
                        closestDistance = distance;
                        closestBox = box;
                    }
                }
            }

            if (closestBox != null)
            {
                Debug.Log($"🎁 [RPC] 가장 가까운 박스 찾음: {closestBox.name} (거리: {closestDistance:F2}m)");
                RemoveBoxFromList(closestBox);
                Destroy(closestBox);
            }
        }

        // 여기서 추후 인벤토리 시스템과 연결 가능
        // 예: InventoryManager.Instance.AddItem(playerName, itemName);
    }

    // 모든 박스 제거 (게임 종료 시 등)
    public void ClearAllBoxes()
    {
        foreach (GameObject box in spawnedBoxes)
        {
            if (box != null)
            {
                Destroy(box);
            }
        }
        spawnedBoxes.Clear();

        Debug.Log("모든 아이템 박스 제거 완료");
    }

    // 디버그: 강제 스폰 (테스트용)
    [ContextMenu("강제 아이템 스폰 (1개)")]
    public void ForceSpawnItem()
    {
        if (PhotonNetwork.IsMasterClient || !Application.isPlaying)
        {
            SpawnItemBox();
        }
    }

    [ContextMenu("강제 아이템 스폰 (3개)")]
    public void ForceSpawn3Items()
    {
        if (PhotonNetwork.IsMasterClient || !Application.isPlaying)
        {
            for (int i = 0; i < 3; i++)
            {
                SpawnItemBox();
            }
        }
    }

    // Gizmos로 스폰 범위 시각화
    void OnDrawGizmosSelected()
    {
        // 스폰 범위 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(10f, 1, 4f)); // 실제 스폰 범위에 맞춤

        // 현재 스폰된 박스들 표시
        Gizmos.color = Color.yellow;
        foreach (GameObject box in spawnedBoxes)
        {
            if (box != null)
            {
                Gizmos.DrawWireSphere(box.transform.position, 1f);
            }
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart -= OnTurnStarted;
        }
    }

    // ✅ 새로 추가: 아이템 습득 요청 RPC (모든 클라이언트에서 호출 가능)
    [PunRPC]
    void RPC_RequestItemPickup(string playerName, string itemName, float posX, float posY, float posZ, int gameObjectId)
    {
        // 마스터 클라이언트에서만 처리
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"🎁 [Master] 아이템 습득 요청 받음: {playerName} - {itemName}");

        // 실제 아이템 습득 처리
        Vector3 itemPosition = new Vector3(posX, posY, posZ);

        // 모든 클라이언트에 아이템 습득 알림
        photonView.RPC("RPC_ItemPickedUp", RpcTarget.All, playerName, itemName, posX, posY, posZ);

        // 해당 위치의 박스 찾아서 제거
        GameObject boxToRemove = FindBoxByPosition(itemPosition);
        if (boxToRemove != null)
        {
            RemoveBoxFromList(boxToRemove);
            Destroy(boxToRemove);
            Debug.Log($"🎁 [Master] 박스 삭제 완료: {boxToRemove.name}");
        }
        else
        {
            Debug.LogWarning($"🎁 [Master] 해당 위치의 박스를 찾지 못함: {itemPosition}");
        }
    }

    // ✅ 새로 추가: 위치로 박스 찾기
    GameObject FindBoxByPosition(Vector3 position)
    {
        foreach (GameObject box in spawnedBoxes.ToArray())
        {
            if (box != null && Vector3.Distance(box.transform.position, position) < 0.5f)
            {
                return box;
            }
        }
        return null;
    }

}