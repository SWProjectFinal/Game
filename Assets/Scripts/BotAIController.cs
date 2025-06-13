using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class BotAIController : MonoBehaviourPunCallbacks
{
    [Header("봇 AI 설정")]
    public BotDifficulty difficulty = BotDifficulty.Normal;
    public float thinkingTime = 1f;
    public float movementTime = 2f;
    public float aimingTime = 1.5f;

    [Header("이동 설정")]
    public float moveDistance = 3f;
    public float moveSpeed = 3f;

    [Header("맵 경계 설정")]
    public Vector2 mapBounds = new Vector2(15f, 8f);
    public float boundaryBuffer = 1f;

    [Header("낙사 설정")]
    public float fallLimitY = -15f;
    public float fallWarningY = -10f;

    [Header("지형 감지 설정")]
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 3f;

    [Header("조준 설정")]
    public float aimAccuracy = 0.8f;
    public float maxAimAngle = 60f;

    [Header("무기 선택")]
    public float weaponSwitchChance = 0.3f;

    [Header("고급 AI 설정")]
    public bool useAdvancedAI = true;
    public float strategicThinkingTime = 3f;
    public float coverSeekingChance = 0.6f;
    public float aggressiveChance = 0.3f;

    [Header("밸런싱 설정")]
    public bool adaptiveDifficulty = true;
    public float difficultyAdjustRate = 0.1f;
    public bool fairPlay = true;

    [Header("디버깅 설정")]
    public bool showDebugInfo = true;
    public bool showAIThoughts = true;
    public bool showGizmos = true;

    // 컴포넌트 참조
    private CatController catController;
    private Rigidbody2D rb;
    private Transform headPivot;
    private Transform firePoint;
    private Collider2D myCollider;
    private PlayerHealth playerHealth;

    // AI 상태
    private bool isMyTurn = false;
    private bool isThinking = false;
    private Vector3 targetPosition;
    private GameObject currentTarget;
    private Coroutine aiCoroutine;

    // 물리 상태
    private bool isGrounded = false;
    private bool isDead = false;
    private bool isInitialized = false;

    // 동기화 상태
    private Vector3 syncPosition;
    private float syncHeadAngle;
    private bool syncIsMoving;
    private float syncMoveDirection;

    // 고급 AI
    private AIStrategy currentStrategy = AIStrategy.Balanced;

    // 디버깅
    private string currentAIState = "대기 중";
    private string currentThought = "";
    private float lastThoughtTime = 0f;

    // 밸런싱
    private static int totalBotKills = 0;
    private static int totalPlayerKills = 0;
    private static float averagePlayerWinRate = 0.5f;
    private int myKills = 0;
    private int myDeaths = 0;
    private float myAccuracy = 1.0f;
    private List<float> recentShotAccuracy = new List<float>();

    // 타겟 리스트
    private List<GameObject> potentialTargets = new List<GameObject>();

    public enum BotDifficulty
    {
        Easy, Normal, Hard
    }

    public enum AIStrategy
    {
        Aggressive, Defensive, Balanced, Opportunistic
    }

    void Start()
    {
        Debug.Log($"🤖 [{(PhotonNetwork.IsMasterClient ? "Master" : "Client")}] {gameObject.name} Start() 시작");

        SetupComponents();
        SetupPhysics();
        SetupDifficultySettings();
        LoadGameStats();

        StartCoroutine(InitializePosition());

        if (PhotonNetwork.IsMasterClient && TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart += OnTurnStarted;
            TurnManager.Instance.OnTurnEnd += OnTurnEnded;
        }

        syncPosition = transform.position;
        syncHeadAngle = 0f;
        syncIsMoving = false;
        syncMoveDirection = 0f;

        Debug.Log($"🤖 {gameObject.name} AI 초기화 완료!");
    }

    IEnumerator InitializePosition()
    {
        yield return new WaitForSeconds(0.5f);

        Vector3 currentPos = transform.position;
        Vector3 safePosition = FindSafeGroundPosition(currentPos);

        if (safePosition != currentPos)
        {
            transform.position = safePosition;
            Debug.Log($"🤖 {gameObject.name} 안전한 위치로 이동: {safePosition}");
        }

        isInitialized = true;
    }

    Vector3 FindSafeGroundPosition(Vector3 startPos)
    {
        Vector2 rayOrigin = new Vector2(startPos.x, startPos.y + 2f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, 10f, groundLayer);

        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            Vector3 groundPosition = new Vector3(startPos.x, hit.point.y + 0.5f, startPos.z);
            groundPosition = ClampToMapBounds(groundPosition);
            return groundPosition;
        }

        return startPos;
    }

    void SetupComponents()
    {
        catController = GetComponent<CatController>();
        if (catController != null)
        {
            catController.enabled = false;
            Debug.Log($"🤖 {gameObject.name}: CatController 비활성화 완료");
        }

        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        playerHealth = GetComponent<PlayerHealth>();

        headPivot = transform.Find("HeadPivot");
        firePoint = GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "FirePoint");
    }

    void SetupPhysics()
    {
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            Debug.Log($"🤖 {gameObject.name} 물리 설정 완료");
        }

        if (myCollider != null)
        {
            myCollider.isTrigger = false;
        }
    }

    void SetupDifficultySettings()
    {
        switch (difficulty)
        {
            case BotDifficulty.Easy:
                thinkingTime = 2f;
                movementTime = 3f;
                aimingTime = 2f;
                aimAccuracy = 0.4f;
                weaponSwitchChance = 0.1f;
                break;

            case BotDifficulty.Normal:
                thinkingTime = 1f;
                movementTime = 2f;
                aimingTime = 1.5f;
                aimAccuracy = 0.7f;
                weaponSwitchChance = 0.3f;
                break;

            case BotDifficulty.Hard:
                thinkingTime = 0.5f;
                movementTime = 1.5f;
                aimingTime = 1f;
                aimAccuracy = 0.9f;
                weaponSwitchChance = 0.5f;
                break;
        }

        if (useAdvancedAI)
        {
            SetupAdvancedDifficultySettings();
        }

        Debug.Log($"🤖 난이도 설정: {difficulty} - 정확도: {aimAccuracy:P0}");
    }

    void SetupAdvancedDifficultySettings()
    {
        switch (difficulty)
        {
            case BotDifficulty.Easy:
                strategicThinkingTime = 1f;
                coverSeekingChance = 0.2f;
                aggressiveChance = 0.6f;
                currentStrategy = AIStrategy.Aggressive;
                break;

            case BotDifficulty.Normal:
                strategicThinkingTime = 2f;
                coverSeekingChance = 0.5f;
                aggressiveChance = 0.4f;
                currentStrategy = AIStrategy.Balanced;
                break;

            case BotDifficulty.Hard:
                strategicThinkingTime = 3f;
                coverSeekingChance = 0.8f;
                aggressiveChance = 0.2f;
                currentStrategy = AIStrategy.Opportunistic;
                break;
        }

        Debug.Log($"🧠 {gameObject.name} 고급 AI 설정 - 전략: {currentStrategy}");
    }

    void Update()
    {
        CheckFallDeath();

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            isDead = true;
            UpdateAIState("사망");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            ApplyClientSync();
        }

        if (Time.time % 10f < 0.1f && showDebugInfo)
        {
            MonitorPerformance();
        }
    }

    void FixedUpdate()
    {
        if (isDead || !isInitialized) return;

        CheckGrounded();
        ConstrainToMapBounds();
    }

    void CheckFallDeath()
    {
        if (isDead) return;

        float currentY = transform.position.y;

        if (currentY <= fallLimitY)
        {
            if (playerHealth != null && playerHealth.IsAlive)
            {
                Debug.Log($"💀 🤖 {gameObject.name} 낙사 즉사! 높이: {currentY:F2}");

                if (PhotonNetwork.IsMasterClient)
                {
                    playerHealth.TakeDamage(999f);
                }
            }
            isDead = true;
        }
        else if (currentY <= fallWarningY && currentY > fallLimitY)
        {
            Debug.Log($"⚠️ 🤖 {gameObject.name} 낙사 위험! 높이: {currentY:F2}");
        }
    }

    void ConstrainToMapBounds()
    {
        Vector3 pos = transform.position;
        Vector3 clampedPos = ClampToMapBounds(pos);

        if (pos != clampedPos)
        {
            transform.position = clampedPos;

            if (rb != null)
            {
                Vector2 velocity = rb.velocity;

                if (Mathf.Abs(pos.x - clampedPos.x) > 0.01f)
                {
                    velocity.x = 0f;
                }

                rb.velocity = velocity;
            }

            Debug.Log($"🚧 🤖 {gameObject.name} 맵 경계 제한: {clampedPos}");
        }
    }

    Vector3 ClampToMapBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, -mapBounds.x + boundaryBuffer, mapBounds.x - boundaryBuffer),
            position.y,
            Mathf.Clamp(position.z, -mapBounds.y + boundaryBuffer, mapBounds.y - boundaryBuffer)
        );
    }

    void CheckGrounded()
    {
        Vector2 rayOrigin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = hit.collider != null;
    }

    void ApplyClientSync()
    {
        if (Vector3.Distance(transform.position, syncPosition) > 0.5f)
        {
            Vector3 targetPos = ClampToMapBounds(syncPosition);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);
        }

        if (rb != null)
        {
            Vector2 velocity = rb.velocity;

            if (syncIsMoving)
            {
                velocity.x = syncMoveDirection * moveSpeed;
            }
            else
            {
                velocity.x = 0f;
            }

            rb.velocity = velocity;
        }

        if (headPivot != null)
        {
            float currentAngle = headPivot.localEulerAngles.z;
            if (currentAngle > 180f) currentAngle -= 360f;

            if (Mathf.Abs(currentAngle - syncHeadAngle) > 1f)
            {
                headPivot.localEulerAngles = new Vector3(0, 0,
                    Mathf.LerpAngle(currentAngle, syncHeadAngle, Time.deltaTime * 10f));
            }
        }
    }

    void OnTurnStarted(Photon.Realtime.Player currentPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || isDead) return;

        if (currentPlayer == null && TurnManager.Instance != null)
        {
            string currentPlayerName = TurnManager.Instance.allPlayers[TurnManager.Instance.currentPlayerIndex];

            if (currentPlayerName == gameObject.name)
            {
                StartBotTurn();
            }
        }
    }

    void OnTurnEnded(Photon.Realtime.Player currentPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (isMyTurn)
        {
            EndBotTurn();
        }
    }

    void StartBotTurn()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) return;

        isMyTurn = true;
        isThinking = true;

        Debug.Log($"🤖 [Master] {gameObject.name}의 턴 시작!");

        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
        }
        aiCoroutine = StartCoroutine(ExecuteBotTurn());
    }

    void EndBotTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isMyTurn = false;
        isThinking = false;

        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
            aiCoroutine = null;
        }

        SyncBotMovement(0f, false);

        Debug.Log($"🤖 [Master] {gameObject.name}의 턴 종료!");
    }

    IEnumerator ExecuteBotTurn()
    {
        if (useAdvancedAI)
        {
            yield return StartCoroutine(ExecuteBotTurnAdvanced());
        }
        else
        {
            yield return StartCoroutine(ExecuteBotTurnBasic());
        }
    }

    IEnumerator ExecuteBotTurnBasic()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        Debug.Log($"🤖 [Master] {gameObject.name} 기본 AI 행동 시작");

        yield return new WaitForSeconds(thinkingTime);
        SelectTarget();

        if (Random.value < weaponSwitchChance)
        {
            SelectWeapon();
            yield return new WaitForSeconds(0.5f);
        }

        if (ShouldMove())
        {
            yield return StartCoroutine(MoveToPosition());
        }

        yield return StartCoroutine(AimAtTarget());
        yield return StartCoroutine(FireWeapon());

        Debug.Log($"🤖 [Master] {gameObject.name} 기본 AI 행동 완료");
    }

    IEnumerator ExecuteBotTurnAdvanced()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        UpdateAIState("전략적 사고 중");
        ShowAIThought("흠... 어떻게 할까?");
        Debug.Log($"🤖 [Master] {gameObject.name} 고급 AI 행동 시작");

        yield return new WaitForSeconds(strategicThinkingTime);

        UpdateAIState("상황 분석 중");
        UpdateStrategyDynamically();

        float myHealth = playerHealth != null ? playerHealth.GetHealthPercentage() : 100f;
        if (myHealth < 30f)
        {
            ShowAIThought("체력이 위험해... 조심해야겠다");
        }
        else if (myHealth > 80f)
        {
            ShowAIThought("체력이 충분하니 공격적으로 가자!");
        }

        UpdateAIState("타겟 선택 중");
        SelectTargetStrategically();

        if (currentTarget != null)
        {
            ShowAIThought($"{currentTarget.name}을 노려보자");
        }

        if (Random.value < weaponSwitchChance)
        {
            UpdateAIState("무기 선택 중");
            ShowAIThought("다른 무기를 써볼까?");
            SelectWeaponStrategically();
            yield return new WaitForSeconds(0.5f);
        }

        if (ShouldMoveStrategically())
        {
            UpdateAIState("이동 중");

            if (Random.value < coverSeekingChance)
            {
                ShowAIThought("안전한 곳으로 이동하자");
                Vector3 coverPos = FindCoverPosition();
                if (Vector3.Distance(coverPos, transform.position) > 1f)
                {
                    yield return StartCoroutine(MoveToSpecificPosition(coverPos));
                }
            }
            else
            {
                ShowAIThought("위치를 바꿔보자");
                yield return StartCoroutine(MoveToPosition());
            }
        }

        UpdateAIState("조준 중");
        ShowAIThought("정확히 조준해야지...");
        yield return StartCoroutine(AimAtTargetAdvanced());

        UpdateAIState("발사!");
        ShowAIThought("발사!");
        yield return StartCoroutine(FireWeapon());

        UpdateAIState("턴 완료");
        ShowAIThought("잘했다!");
        Debug.Log($"🤖 [Master] {gameObject.name} 고급 AI 행동 완료");
    }

    void SelectTarget()
    {
        if (useAdvancedAI)
        {
            SelectTargetStrategically();
        }
        else
        {
            SelectTargetBasic();
        }
    }

    void SelectTargetBasic()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        potentialTargets.Clear();

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allBots = GameObject.FindObjectsOfType<GameObject>()
                                        .Where(obj => obj.name.Contains("Bot") && obj != gameObject)
                                        .ToArray();

        var allTargets = allPlayers.Concat(allBots);

        foreach (GameObject target in allTargets)
        {
            if (target == gameObject) continue;

            PlayerHealth health = target.GetComponent<PlayerHealth>();
            if (health != null && health.IsAlive)
            {
                potentialTargets.Add(target);
            }
        }

        if (potentialTargets.Count > 0)
        {
            currentTarget = potentialTargets
                .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
                .First();

            Debug.Log($"🎯 [Master] {gameObject.name}이 {currentTarget.name}을 타겟으로 선택");
        }
        else
        {
            currentTarget = null;
            Debug.Log($"🤖 [Master] {gameObject.name}: 타겟을 찾을 수 없음");
        }
    }

    void SelectTargetStrategically()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        potentialTargets.Clear();

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allBots = GameObject.FindObjectsOfType<GameObject>()
                                        .Where(obj => obj.name.Contains("Bot") && obj != gameObject)
                                        .ToArray();

        var allTargets = allPlayers.Concat(allBots);

        foreach (GameObject target in allTargets)
        {
            if (target == gameObject) continue;

            PlayerHealth health = target.GetComponent<PlayerHealth>();
            if (health != null && health.IsAlive)
            {
                potentialTargets.Add(target);
            }
        }

        if (potentialTargets.Count == 0)
        {
            currentTarget = null;
            return;
        }

        switch (currentStrategy)
        {
            case AIStrategy.Aggressive:
                currentTarget = potentialTargets
                    .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
                    .First();
                break;

            case AIStrategy.Defensive:
                currentTarget = potentialTargets
                    .OrderByDescending(t => Vector3.Distance(transform.position, t.transform.position))
                    .First();
                break;

            case AIStrategy.Balanced:
                var sortedByDistance = potentialTargets
                    .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
                    .ToList();
                int middleIndex = sortedByDistance.Count / 2;
                currentTarget = sortedByDistance[middleIndex];
                break;

            case AIStrategy.Opportunistic:
                currentTarget = potentialTargets
                    .OrderBy(t =>
                    {
                        PlayerHealth health = t.GetComponent<PlayerHealth>();
                        return health != null ? health.GetHealthPercentage() : 100f;
                    })
                    .First();
                break;
        }

        Debug.Log($"🎯 [Master] {gameObject.name} 전략적 타겟 선택: {currentTarget.name} (전략: {currentStrategy})");
    }

    void SelectWeapon()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (WeaponManager.Instance == null) return;

        var inventory = WeaponManager.Instance.inventory;
        if (inventory.Count > 1)
        {
            int randomIndex = Random.Range(1, inventory.Count);
            WeaponManager.Instance.currentWeaponIndex = randomIndex;

            Debug.Log($"🔫 [Master] {gameObject.name}이 {inventory[randomIndex].displayName} 선택");
        }
    }

    void SelectWeaponStrategically()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (WeaponManager.Instance == null) return;

        var inventory = WeaponManager.Instance.inventory;
        if (inventory.Count <= 1) return;

        float distance = currentTarget != null ?
            Vector3.Distance(transform.position, currentTarget.transform.position) : 5f;
        float myHealth = playerHealth != null ? playerHealth.GetHealthPercentage() : 100f;

        int bestWeaponIndex = 0;

        for (int i = 1; i < inventory.Count; i++)
        {
            var weapon = inventory[i];

            switch (weapon.type)
            {
                case WeaponType.RPG:
                    if (distance > 7f && Random.value < 0.6f)
                        bestWeaponIndex = i;
                    break;

                case WeaponType.Blackhole:
                    if (myHealth < 50f || potentialTargets.Count > 2)
                        bestWeaponIndex = i;
                    break;
            }
        }

        if (bestWeaponIndex > 0)
        {
            WeaponManager.Instance.currentWeaponIndex = bestWeaponIndex;
            Debug.Log($"🔫 [Master] {gameObject.name} 전략적 무기 선택: {inventory[bestWeaponIndex].displayName}");
        }
    }

    bool ShouldMove()
    {
        if (!PhotonNetwork.IsMasterClient) return false;

        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        return distance < 2f || distance > 10f || Random.value < 0.6f;
    }

    bool ShouldMoveStrategically()
    {
        if (!PhotonNetwork.IsMasterClient || currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float myHealth = playerHealth != null ? playerHealth.GetHealthPercentage() : 100f;

        switch (currentStrategy)
        {
            case AIStrategy.Aggressive:
                return Random.value < 0.8f;

            case AIStrategy.Defensive:
                if (distance < 3f) return true;
                if (distance > 12f) return true;
                return Random.value < 0.3f;

            case AIStrategy.Balanced:
                return distance < 2f || distance > 10f || Random.value < 0.6f;

            case AIStrategy.Opportunistic:
                if (myHealth < 30f)
                {
                    return distance < 5f;
                }
                else
                {
                    return Random.value < 0.7f;
                }
        }

        return false;
    }

    Vector3 FindCoverPosition()
    {
        Vector3 currentPos = transform.position;
        Vector3 bestCoverPos = currentPos;
        float bestScore = 0f;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 testPos = currentPos + direction * 3f;

            testPos = ClampToMapBounds(testPos);

            Vector2 rayOrigin = new Vector2(testPos.x, testPos.y + 2f);
            RaycastHit2D groundHit = Physics2D.Raycast(rayOrigin, Vector2.down, 5f, groundLayer);

            if (groundHit.collider == null) continue;

            float score = CalculateCoverScore(testPos);

            if (score > bestScore)
            {
                bestScore = score;
                bestCoverPos = new Vector3(testPos.x, groundHit.point.y + 0.5f, testPos.z);
            }
        }

        return bestCoverPos;
    }

    float CalculateCoverScore(Vector3 position)
    {
        float score = 0f;

        if (currentTarget == null) return score;

        float distance = Vector3.Distance(position, currentTarget.transform.position);
        if (distance > 4f && distance < 10f)
        {
            score += 3f;
        }

        if (position.y > currentTarget.transform.position.y)
        {
            score += 2f;
        }

        float distanceFromCenter = Vector3.Distance(position, Vector3.zero);
        if (distanceFromCenter < mapBounds.x * 0.7f)
        {
            score += 1f;
        }

        return score;
    }

    IEnumerator MoveToPosition()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        Debug.Log($"🚶 [Master] {gameObject.name} 이동 시작");

        float moveDirection = ChooseSafeMoveDirection();

        SyncBotMovement(moveDirection, true);

        float moveStartTime = Time.time;

        while (Time.time - moveStartTime < movementTime && !isDead)
        {
            if (rb != null)
            {
                Vector2 velocity = rb.velocity;
                velocity.x = moveDirection * moveSpeed;
                rb.velocity = velocity;

                Vector3 nextPos = transform.position + Vector3.right * moveDirection * moveSpeed * Time.fixedDeltaTime;
                if (IsNearBoundary(nextPos))
                {
                    Debug.Log($"🚧 {gameObject.name} 경계 근처 - 이동 중단");
                    break;
                }
            }

            if ((Time.time - moveStartTime) % 0.2f < 0.1f)
            {
                SyncBotPosition(transform.position);
            }

            yield return new WaitForFixedUpdate();
        }

        SyncBotMovement(0f, false);

        if (rb != null)
        {
            Vector2 velocity = rb.velocity;
            velocity.x = 0f;
            rb.velocity = velocity;
        }

        SyncBotPosition(transform.position);

        Debug.Log($"🚶 [Master] {gameObject.name} 이동 완료");
    }

    IEnumerator MoveToSpecificPosition(Vector3 targetPosition)
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        Debug.Log($"🏃 [Master] {gameObject.name} 특정 위치로 이동: {targetPosition}");

        float moveDirection = targetPosition.x > transform.position.x ? 1f : -1f;
        float distanceToTarget = Mathf.Abs(targetPosition.x - transform.position.x);
        float moveTime = Mathf.Min(movementTime, distanceToTarget / moveSpeed);

        SyncBotMovement(moveDirection, true);

        float moveStartTime = Time.time;

        while (Time.time - moveStartTime < moveTime && !isDead)
        {
            if (rb != null)
            {
                Vector2 velocity = rb.velocity;
                velocity.x = moveDirection * moveSpeed;
                rb.velocity = velocity;

                if (Mathf.Abs(transform.position.x - targetPosition.x) < 0.5f)
                {
                    break;
                }
            }

            if ((Time.time - moveStartTime) % 0.2f < 0.1f)
            {
                SyncBotPosition(transform.position);
            }

            yield return new WaitForFixedUpdate();
        }

        SyncBotMovement(0f, false);

        if (rb != null)
        {
            Vector2 velocity = rb.velocity;
            velocity.x = 0f;
            rb.velocity = velocity;
        }

        SyncBotPosition(transform.position);

        Debug.Log($"🏃 [Master] {gameObject.name} 특정 위치 이동 완료");
    }

    float ChooseSafeMoveDirection()
    {
        float baseDirection = Random.value > 0.5f ? 1f : -1f;

        if (currentTarget != null)
        {
            float targetDirection = currentTarget.transform.position.x > transform.position.x ? 1f : -1f;

            if (Random.value > 0.3f)
            {
                baseDirection = targetDirection;
            }
        }

        Vector3 currentPos = transform.position;
        Vector3 testPos = currentPos + Vector3.right * baseDirection * 2f;

        if (IsNearBoundary(testPos))
        {
            baseDirection = -baseDirection;
            Debug.Log($"🚧 {gameObject.name} 경계 회피 - 방향 변경: {baseDirection}");
        }

        return baseDirection;
    }

    bool IsNearBoundary(Vector3 position)
    {
        return Mathf.Abs(position.x) > mapBounds.x - boundaryBuffer * 2f ||
               Mathf.Abs(position.z) > mapBounds.y - boundaryBuffer * 2f;
    }

    IEnumerator AimAtTarget()
    {
        if (useAdvancedAI)
        {
            yield return StartCoroutine(AimAtTargetAdvanced());
        }
        else
        {
            yield return StartCoroutine(AimAtTargetBasic());
        }
    }

    IEnumerator AimAtTargetBasic()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        if (currentTarget == null || headPivot == null)
        {
            yield return new WaitForSeconds(aimingTime);
            yield break;
        }

        Debug.Log($"🎯 [Master] {gameObject.name} 기본 조준 시작");

        float aimStartTime = Time.time;
        Vector3 targetPos = currentTarget.transform.position;

        while (Time.time - aimStartTime < aimingTime && !isDead)
        {
            Vector3 directionToTarget = (targetPos - transform.position).normalized;

            float aimError = (1f - aimAccuracy) * 30f;
            float randomError = Random.Range(-aimError, aimError);

            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            targetAngle += randomError;
            targetAngle = Mathf.Clamp(targetAngle, -maxAimAngle, maxAimAngle);

            float progress = (Time.time - aimStartTime) / aimingTime;
            float currentAngle = Mathf.Lerp(0f, targetAngle, progress);

            headPivot.localEulerAngles = new Vector3(0, 0, currentAngle);

            if ((Time.time - aimStartTime) % 0.1f < 0.05f)
            {
                SyncBotAiming(currentAngle);
            }

            yield return null;
        }

        Debug.Log($"🎯 [Master] {gameObject.name} 기본 조준 완료");
    }

    IEnumerator AimAtTargetAdvanced()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        if (currentTarget == null || headPivot == null)
        {
            yield return new WaitForSeconds(aimingTime);
            yield break;
        }

        bool shouldMiss = ShouldPlayFairly();
        float effectiveAccuracy = shouldMiss ? aimAccuracy * 0.3f : aimAccuracy;

        Debug.Log($"🎯 [Master] {gameObject.name} 고급 조준 시작 (정확도: {effectiveAccuracy:P0})");

        float aimStartTime = Time.time;

        while (Time.time - aimStartTime < aimingTime && !isDead)
        {
            Vector3 targetPos = currentTarget.transform.position;

            if (difficulty == BotDifficulty.Hard && !shouldMiss)
            {
                Rigidbody2D targetRb = currentTarget.GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    Vector3 predictedPos = targetPos + (Vector3)targetRb.velocity * 0.5f;
                    targetPos = predictedPos;
                }
            }

            Vector3 directionToTarget = (targetPos - transform.position).normalized;

            float aimError = (1f - effectiveAccuracy) * 30f;
            float randomError = Random.Range(-aimError, aimError);

            if (shouldMiss)
            {
                randomError += Random.Range(-20f, 20f);
            }

            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            targetAngle += randomError;
            targetAngle = Mathf.Clamp(targetAngle, -maxAimAngle, maxAimAngle);

            float progress = (Time.time - aimStartTime) / aimingTime;
            float currentAngle = Mathf.Lerp(0f, targetAngle, progress);

            headPivot.localEulerAngles = new Vector3(0, 0, currentAngle);

            if ((Time.time - aimStartTime) % 0.1f < 0.05f)
            {
                SyncBotAiming(currentAngle);
            }

            yield return null;
        }

        RecordShotAccuracy(!shouldMiss);

        Debug.Log($"🎯 [Master] {gameObject.name} 고급 조준 완료");
    }

    IEnumerator FireWeapon()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) yield break;

        Debug.Log($"🔫 [Master] {gameObject.name} 발사!");

        if (WeaponManager.Instance != null && firePoint != null)
        {
            Vector2 fireDirection = firePoint.right.normalized;
            float power = Random.Range(WeaponManager.Instance.minPower, WeaponManager.Instance.maxPower);

            WeaponManager.Instance.FireWeapon(fireDirection, power);
        }

        yield return new WaitForSeconds(0.5f);
    }

    // AI 상태 및 밸런싱 함수들
    void UpdateStrategyDynamically()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        float myHealth = playerHealth != null ? playerHealth.GetHealthPercentage() : 100f;
        int aliveEnemies = potentialTargets.Count;

        AIStrategy newStrategy = currentStrategy;

        if (myHealth < 25f)
        {
            newStrategy = AIStrategy.Defensive;
        }
        else if (myHealth > 75f && aliveEnemies <= 2)
        {
            newStrategy = AIStrategy.Aggressive;
        }
        else if (aliveEnemies == 1)
        {
            newStrategy = AIStrategy.Opportunistic;
        }
        else
        {
            newStrategy = AIStrategy.Balanced;
        }

        if (newStrategy != currentStrategy)
        {
            currentStrategy = newStrategy;
            Debug.Log($"🧠 {gameObject.name} 전략 변경: {currentStrategy} (체력: {myHealth:F0}%, 적: {aliveEnemies}명)");
        }
    }

    bool ShouldPlayFairly()
    {
        if (!fairPlay) return false;

        var humanPlayers = GameObject.FindGameObjectsWithTag("Player")
            .Where(p => p.GetComponent<PhotonView>() != null)
            .ToArray();

        foreach (var player in humanPlayers)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsAlive && health.GetHealthPercentage() < 20f)
            {
                if (Random.value < 0.3f)
                {
                    ShowAIThought("이번엔 살짝 빗나가 주자...");
                    return true;
                }
            }
        }

        return false;
    }

    void RecordShotAccuracy(bool wasAccurate)
    {
        recentShotAccuracy.Add(wasAccurate ? 1f : 0f);

        if (recentShotAccuracy.Count > 10)
        {
            recentShotAccuracy.RemoveAt(0);
        }

        myAccuracy = recentShotAccuracy.Average();

        Debug.Log($"📊 {gameObject.name} 사격 정확도: {myAccuracy:P0} (최근 {recentShotAccuracy.Count}발)");
    }

    void UpdateAdaptiveDifficulty()
    {
        if (!adaptiveDifficulty || !PhotonNetwork.IsMasterClient) return;

        float totalGames = totalBotKills + totalPlayerKills;
        if (totalGames > 0)
        {
            float botWinRate = totalBotKills / totalGames;
            averagePlayerWinRate = totalPlayerKills / totalGames;

            if (botWinRate > 0.7f)
            {
                AdjustDifficultyDown();
                Debug.Log($"🎯 봇 승률 높음 ({botWinRate:P0}) - 난이도 하향 조정");
            }
            else if (botWinRate < 0.3f)
            {
                AdjustDifficultyUp();
                Debug.Log($"🎯 봇 승률 낮음 ({botWinRate:P0}) - 난이도 상향 조정");
            }
        }
    }

    void AdjustDifficultyDown()
    {
        aimAccuracy = Mathf.Max(0.1f, aimAccuracy - difficultyAdjustRate);
        thinkingTime = Mathf.Min(5f, thinkingTime + 0.2f);
        weaponSwitchChance = Mathf.Max(0.1f, weaponSwitchChance - 0.1f);

        ShowAIThought("좀 더 실수를 해볼까...");
    }

    void AdjustDifficultyUp()
    {
        aimAccuracy = Mathf.Min(0.95f, aimAccuracy + difficultyAdjustRate);
        thinkingTime = Mathf.Max(0.3f, thinkingTime - 0.2f);
        weaponSwitchChance = Mathf.Min(0.8f, weaponSwitchChance + 0.1f);

        ShowAIThought("더 정확하게 해보자!");
    }

    void LoadGameStats()
    {
        totalBotKills = PlayerPrefs.GetInt("TotalBotKills", 0);
        totalPlayerKills = PlayerPrefs.GetInt("TotalPlayerKills", 0);
        averagePlayerWinRate = PlayerPrefs.GetFloat("AveragePlayerWinRate", 0.5f);

        Debug.Log($"📊 게임 통계 로드 - 봇승: {totalBotKills}, 플레이어승: {totalPlayerKills}");
    }

    // 디버깅 및 시각화 함수들
    void ShowAIThought(string thought)
    {
        if (!showAIThoughts) return;

        currentThought = thought;
        lastThoughtTime = Time.time;

        Debug.Log($"💭 {gameObject.name}: {thought}");
    }

    void UpdateAIState(string newState)
    {
        currentAIState = newState;
        if (showDebugInfo)
        {
            Debug.Log($"🤖 [{gameObject.name}] 상태 변경: {newState}");
        }
    }

    void MonitorPerformance()
    {
        long memoryUsage = System.GC.GetTotalMemory(false);
        float memoryMB = memoryUsage / (1024f * 1024f);
        float fps = 1f / Time.deltaTime;

        if (memoryMB > 500f || fps < 30f)
        {
            Debug.LogWarning($"⚠️ 성능 경고 - 메모리: {memoryMB:F1}MB, FPS: {fps:F1}");
        }

        Debug.Log($"🔍 성능 모니터링 - 메모리: {memoryMB:F1}MB, FPS: {fps:F1}, 봇 수: {GameObject.FindObjectsOfType<BotAIController>().Length}");
    }

    // 동기화 RPC 함수들
    void SyncBotPosition(Vector3 position)
    {
        if (PlayerSpawner.Instance?.photonView != null)
        {
            PlayerSpawner.Instance.photonView.RPC("RPC_SyncBotPosition", RpcTarget.Others,
                gameObject.name, position.x, position.y, position.z);
        }
    }

    void SyncBotMovement(float moveDirection, bool isMoving)
    {
        if (PlayerSpawner.Instance?.photonView != null)
        {
            PlayerSpawner.Instance.photonView.RPC("RPC_SyncBotMovement", RpcTarget.Others,
                gameObject.name, moveDirection, isMoving);
        }
    }

    void SyncBotAiming(float headAngle)
    {
        if (PlayerSpawner.Instance?.photonView != null)
        {
            PlayerSpawner.Instance.photonView.RPC("RPC_SyncBotAiming", RpcTarget.Others,
                gameObject.name, headAngle);
        }
    }

    // 동기화 상태 업데이트
    public void UpdateSyncPosition(Vector3 position)
    {
        syncPosition = position;
    }

    public void UpdateSyncMovement(float moveDirection, bool isMoving)
    {
        syncMoveDirection = moveDirection;
        syncIsMoving = isMoving;
    }

    public void UpdateSyncAiming(float headAngle)
    {
        syncHeadAngle = headAngle;
    }

    // GUI 및 Gizmos
    void OnGUI()
    {
        if (!showDebugInfo) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);

        if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width &&
            screenPos.y > 0 && screenPos.y < Screen.height)
        {
            Rect infoRect = new Rect(screenPos.x - 100, Screen.height - screenPos.y - 80, 200, 80);

            GUI.Box(infoRect, "");

            GUI.Label(new Rect(infoRect.x + 5, infoRect.y + 5, 190, 20),
                     $"{gameObject.name} ({difficulty})");

            GUI.Label(new Rect(infoRect.x + 5, infoRect.y + 20, 190, 20),
                     $"전략: {currentStrategy}");

            GUI.Label(new Rect(infoRect.x + 5, infoRect.y + 35, 190, 20),
                     $"상태: {currentAIState}");

            if (playerHealth != null)
            {
                float healthPercent = playerHealth.GetHealthPercentage();
                Color healthColor = healthPercent > 70f ? Color.green :
                                   healthPercent > 30f ? Color.yellow : Color.red;

                GUI.color = healthColor;
                GUI.Label(new Rect(infoRect.x + 5, infoRect.y + 50, 190, 20),
                         $"체력: {healthPercent:F0}%");
                GUI.color = Color.white;
            }

            if (showAIThoughts && Time.time - lastThoughtTime < 3f && !string.IsNullOrEmpty(currentThought))
            {
                Rect thoughtRect = new Rect(screenPos.x - 120, Screen.height - screenPos.y - 150, 240, 60);

                GUI.color = new Color(1f, 1f, 0.8f, 0.9f);
                GUI.Box(thoughtRect, "");
                GUI.color = Color.black;

                GUIStyle thoughtStyle = new GUIStyle(GUI.skin.label);
                thoughtStyle.alignment = TextAnchor.MiddleCenter;
                thoughtStyle.wordWrap = true;

                GUI.Label(thoughtRect, $"💭 {currentThought}", thoughtStyle);
                GUI.color = Color.white;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Color botColor = Color.white;
        switch (currentStrategy)
        {
            case AIStrategy.Aggressive: botColor = Color.red; break;
            case AIStrategy.Defensive: botColor = Color.blue; break;
            case AIStrategy.Balanced: botColor = Color.green; break;
            case AIStrategy.Opportunistic: botColor = Color.yellow; break;
        }

        Gizmos.color = botColor;
        Gizmos.DrawWireSphere(transform.position, 1f);

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentTarget.transform.position, 0.5f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, moveDistance);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        Gizmos.color = Color.magenta;
        Vector3 center = Vector3.zero;
        Gizmos.DrawWireCube(center, new Vector3(mapBounds.x * 2, 1, mapBounds.y * 2));
    }

    // 컨텍스트 메뉴 및 정리
    [ContextMenu("봇 성능 통계 출력")]
    public void PrintPerformanceStats()
    {
        Debug.Log($"=== {gameObject.name} 성능 통계 ===");
        Debug.Log($"킬/데스: {myKills}/{myDeaths}");
        Debug.Log($"사격 정확도: {myAccuracy:P0}");
        Debug.Log($"현재 전략: {currentStrategy}");
        Debug.Log($"현재 체력: {(playerHealth != null ? playerHealth.GetHealthPercentage().ToString("F0") + "%" : "Unknown")}");
        Debug.Log($"적응형 난이도: {(adaptiveDifficulty ? "활성" : "비활성")}");
        Debug.Log($"공정한 플레이: {(fairPlay ? "활성" : "비활성")}");

        Debug.Log($"=== 전체 게임 통계 ===");
        Debug.Log($"총 봇 승수: {totalBotKills}");
        Debug.Log($"총 플레이어 승수: {totalPlayerKills}");
        Debug.Log($"플레이어 평균 승률: {averagePlayerWinRate:P0}");
    }

    void OnDestroy()
    {
        if (PhotonNetwork.IsMasterClient && TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnd -= OnTurnEnded;
        }
    }
}