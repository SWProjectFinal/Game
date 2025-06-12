using UnityEngine;
using System.Collections;

public class AIBotController : MonoBehaviour
{
    private AIAimSystem aimSystem;
    private AIWeaponSelector weaponSelector;
    public Transform firePoint;

    private Rigidbody2D rb;

    // 봇 상태 추적
    private bool isInitialized = false;

    [Header("맵 경계 제한")]
    public float mapLeftBound = -8f;
    public float mapRightBound = 8f;

    void Awake()
    {
        // 컴포넌트들이 추가될 때까지 대기
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(InitializeComponents());
    }

    IEnumerator InitializeComponents()
    {
        // 컴포넌트들이 모두 추가될 때까지 대기
        yield return new WaitForEndOfFrame();

        aimSystem = GetComponent<AIAimSystem>();
        weaponSelector = GetComponent<AIWeaponSelector>();

        // FirePoint 찾기
        firePoint = transform.Find("FirePoint");
        if (firePoint == null)
        {
            Debug.LogWarning($"AI {name}: FirePoint를 찾을 수 없어서 자식에서 검색 중...");
            firePoint = GetComponentInChildren<Transform>().Find("FirePoint");

            if (firePoint == null)
            {
                Debug.LogError($"AI {name}: FirePoint를 전혀 찾을 수 없습니다!");
            }
        }

        isInitialized = true;
        Debug.Log($"🤖 AI {name} 초기화 완료");
    }

    public void DoTurn()
    {
        Debug.Log($"🤖 AI {name} 턴 시작");

        if (!isInitialized)
        {
            Debug.LogError($"AI {name}가 아직 초기화되지 않았습니다!");
            EndTurn();
            return;
        }

        StartCoroutine(ExecuteTurn());
    }

    IEnumerator ExecuteTurn()
    {
        // 1. 약간의 대기 시간 (자연스러움을 위해)
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        yield return MoveBeforeFire();

        // 2. 무기 선택
        if (!SelectWeapon())
        {
            Debug.LogError($"AI {name}: 무기 선택 실패");
            EndTurn();
            yield break;
        }

        // 3. 조준 및 발사
        yield return new WaitForSeconds(0.5f);
        if (!AimAndShoot())
        {
            Debug.LogError($"AI {name}: 발사 실패");
        }

        // 4. 발사 후 잠시 대기
        yield return new WaitForSeconds(1f);

        // 5. 턴 종료
        EndTurn();
    }

    bool SelectWeapon()
    {
        Debug.Log($"🤖 AI {name}: 무기 선택 중...");

        if (weaponSelector == null)
        {
            Debug.LogError($"AI {name}: weaponSelector가 null입니다!");
            return false;
        }

        if (WeaponManager.Instance == null)
        {
            Debug.LogError("WeaponManager.Instance가 null입니다!");
            return false;
        }

        try
        {
            int weaponIndex = weaponSelector.SelectWeaponIndex();
            WeaponManager.Instance.SelectWeaponByIndex(weaponIndex);
            Debug.Log($"🤖 AI {name}: 무기 인덱스 {weaponIndex} 선택 완료");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI {name}: 무기 선택 중 오류 발생: {e.Message}");
            return false;
        }
    }

    bool AimAndShoot()
    {
        Debug.Log($"🤖 AI {name}: 조준 및 발사 시작");

        if (aimSystem == null)
        {
            Debug.LogError($"AI {name}: aimSystem이 null입니다!");
            return false;
        }

        if (firePoint == null)
        {
            Debug.LogError($"AI {name}: firePoint가 null입니다!");
            return false;
        }

        GameObject target = FindTargetPlayer();
        if (target == null)
        {
            Debug.LogWarning($"AI {name}: 타겟을 찾을 수 없습니다!");
            return FireRandomDirection();
        }

        Vector2 targetPos = target.transform.position;
        Vector2 myPos = transform.position;
        float distance = Vector2.Distance(myPos, targetPos);

        Debug.Log($"🤖 AI {name}: 타겟 발견 - {target.name} (거리: {distance:F1})");
        Debug.Log($"🤖 AI 위치: {myPos}, 타겟 위치: {targetPos}");

        try
        {
            // AI 방향 설정
            bool targetIsRight = targetPos.x > myPos.x;
            aimSystem.facingRight = targetIsRight;

            int direction = Random.value < 0.5f ? -1 : 1;

            // 캐릭터 스프라이트 방향 설정
            Vector3 scale = transform.localScale;
            scale.x = targetIsRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;

            Debug.Log($"🤖 AI {name}: 방향 설정 - facingRight: {aimSystem.facingRight}");

            // 1단계: 최적 파워 계산
            float optimalPower = aimSystem.CalculateOptimalPower(myPos, targetPos);
            Debug.Log($"🤖 AI {name}: 계산된 최적 파워 = {optimalPower:F1}");

            // 2단계: 해당 파워로 발사 각도 계산
            float angle = aimSystem.CalculateFireAngle(myPos, targetPos, optimalPower);
            Debug.Log($"🤖 AI {name}: 계산된 각도 = {angle:F1}도");

            // 3단계: 실제 발사 방향 계산
            Vector2 fireDirection = aimSystem.GetFireDirection(angle);
            Debug.Log($"🤖 AI {name}: 발사 방향 = {fireDirection}");

            // FirePoint 회전 설정
            float fireAngle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0, 0, fireAngle);

            // 4단계: 계산된 파워로 발사
            float normalizedPower = optimalPower / aimSystem.maxFirePower; // 0~1 사이 값으로 정규화
            Debug.Log($"🤖 AI {name}: 정규화된 파워 = {normalizedPower:F2} (실제파워: {optimalPower:F1})");

            return FireWeapon(fireDirection, optimalPower);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI {name}: 조준/발사 중 오류 발생: {e.Message}");
            return false;
        }
    }

    bool FireRandomDirection()
    {
        Debug.Log($"🤖 AI {name}: 타겟 없음 - 랜덤 방향으로 발사");

        // 랜덤 각도와 파워
        float randomAngle = Random.Range(30f, 150f);
        float randomPower = Random.Range(0.3f, 0.8f); // 30%~80% 파워

        Vector2 fireDir = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                                      Mathf.Sin(randomAngle * Mathf.Deg2Rad));

        // 랜덤하게 좌우 방향 결정
        if (Random.value < 0.5f)
        {
            fireDir.x = -fireDir.x;
        }

        firePoint.rotation = Quaternion.Euler(0, 0, randomAngle);

        return FireWeapon(fireDir.normalized, aimSystem.maxFirePower * randomPower);
    }

    bool FireWeapon(Vector2 dir, float actualPower)
    {
        try
        {
            if (WeaponManager.Instance == null)
            {
                Debug.LogError("WeaponManager.Instance가 null입니다!");
                return false;
            }

            if (WeaponManager.Instance.basicGunSO == null)
            {
                Debug.LogError("basicGunSO가 null입니다!");
                return false;
            }

            var projectilePrefab = WeaponManager.Instance.basicGunSO.projectilePrefab;
            if (projectilePrefab == null)
            {
                Debug.LogError("projectilePrefab이 null입니다!");
                return false;
            }

            // 발사 위치 계산 (조금 앞쪽으로 스폰)
            Vector3 spawnPos = firePoint.position + (Vector3)(dir * 0.5f);
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            var standardProj = proj.GetComponent<StandardProjectile>();
            if (standardProj != null)
            {
                standardProj.weaponData = WeaponManager.Instance.basicGunSO;
                standardProj.power = actualPower;  // 이제 실제 발사 속도로 넘긴다
                standardProj.shootDirection = dir;

                Debug.Log($"🔥 AI {name} 발사 성공! (실제 파워: {actualPower:F1})");
                return true;
            }
            else
            {
                Debug.LogError("StandardProjectile 컴포넌트를 찾을 수 없습니다!");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI {name}: 발사 중 오류 발생: {e.Message}");
            return false;
        }
    }




    GameObject FindTargetPlayer()
    {
        try
        {
            if (PlayerSpawner.Instance == null)
            {
                Debug.LogError("PlayerSpawner.Instance가 null입니다!");
                return null;
            }

            // 스폰된 플레이어들 중에서 살아있는 플레이어 찾기
            foreach (var playerObj in PlayerSpawner.Instance.spawnedPlayers)
            {
                if (playerObj != null && playerObj.activeInHierarchy)
                {
                    Debug.Log($"🎯 AI {name}: 타겟 발견 - {playerObj.name}");
                    return playerObj;
                }
            }

            Debug.LogWarning($"AI {name}: 활성화된 플레이어를 찾을 수 없습니다!");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI {name}: 타겟 검색 중 오류 발생: {e.Message}");
            return null;
        }
    }

    void EndTurn()
    {
        Debug.Log($"🤖 AI {name} 턴 종료");

        try
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.ForceEndTurn();
            }
            else
            {
                Debug.LogError("TurnManager.Instance가 null입니다!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI {name}: 턴 종료 중 오류 발생: {e.Message}");
        }
    }

    IEnumerator MoveBeforeFire()
    {
        Debug.Log($"🤖 AI {name}: 발사 전 이동 시작");

        Vector3 originalPos = transform.position;

        int direction = Random.value < 0.5f ? -1 : 1;

        float moveAmount = Random.Range(2f, 4f);
        float targetX = originalPos.x + (moveAmount * direction);

        if (targetX < mapLeftBound || targetX > mapRightBound)
        {
            Debug.Log("📛 이동 범위 초과 - 이동 취소");
            yield break;
        }

        Vector3 scale = transform.localScale;
        scale.x = direction > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        float moveSpeed = 1f;
        float duration = Mathf.Abs(moveAmount) / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // ✅ 물리말고 transform으로 자연스럽게 이동
            transform.position += Vector3.right * direction * moveSpeed * Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"✅ 이동 완료 → {transform.position}");
    }


    // Start는 제거 (Awake에서 처리)
}