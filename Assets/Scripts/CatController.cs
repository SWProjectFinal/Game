using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // ← 추가 필요!
using System.Linq; // ← 이거 추가
using UnityEngine.UI; // ← 이 줄 추가!

public class CatController : MonoBehaviour, IPunObservable // ← IPunObservable 추가!
{
    [Header("이동 관련")]
    public float moveSpeed = 3f;
    public float slopeAssist = 0.75f;

    [Header("고개 회전 관련")]
    public Transform headPivot;
    public float lookAngleSpeed = 60f;

    [Header("낙사 처리 (강화됨)")]
    public float fallLimitY = -15f; // ← 기본값 변경 (지형파괴 고려)
    public float fallWarningY = -10f; // ← 낙사 경고 높이 추가
    public bool showFallWarning = true; // ← 낙사 경고 표시 여부

    [Header("바닥 감지")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.05f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider; // ← 추가
    private PlayerHealth playerHealth; // ← PlayerHealth 연동 추가

    private int moveInput;
    private float lookInput;
    private bool isDead = false;
    private bool isGrounded;
    private bool isFallWarningShown = false; // ← 낙사 경고 상태

    // ===== 턴제 연결을 위한 추가 변수 =====
    private bool canMove = false; // ✅ false로 변경! - 게임 시작 전에는 움직이면 안됨

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>(); // ← 추가
        playerHealth = GetComponent<PlayerHealth>(); // ← PlayerHealth 연동

        // ✅ 필수 컴포넌트 null 체크
        if (spriteRenderer == null)
            Debug.LogError($"[{gameObject.name}] SpriteRenderer가 없습니다!");
        if (headPivot == null)
            Debug.LogWarning($"[{gameObject.name}] HeadPivot이 설정되지 않았습니다!");
        if (playerHealth == null)
            Debug.LogWarning($"[{gameObject.name}] PlayerHealth 컴포넌트가 없습니다!");

        // ===== 턴제 이벤트 구독 =====
        TurnManager.OnPlayerMovementChanged += OnMovementStateChanged;

        // ===== 다른 플레이어와의 충돌 무시 설정 ===== ← 추가
        IgnorePlayerCollisions();

        //닉네임 적용
        SetPlayerDisplayName();

        // ✅ PhotonView 디버깅
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
        {
            Debug.Log($"[{gameObject.name}] PhotonView 확인 - IsMine: {pv.IsMine}");
        }
    }

    // 새로 추가할 함수
    void SetPlayerDisplayName()
    {
        Text nameText = GetComponentInChildren<Text>();
        if (nameText != null)
        {
            PhotonView pv = GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null)
            {
                // 실제 플레이어
                nameText.text = pv.Owner.NickName;
            }
            else
            {
                // 봇인 경우
                nameText.text = gameObject.name;
            }

            Debug.Log($"닉네임 설정 완료: {nameText.text}");
        }
    }

    // ===== 다른 플레이어와의 충돌 무시 함수 ===== ← 새로 추가
    void IgnorePlayerCollisions()
    {
        // 모든 Cat 오브젝트 찾기
        GameObject[] allCats = GameObject.FindGameObjectsWithTag("Player");

        if (allCats.Length == 0)
        {
            // Tag가 없으면 이름으로 찾기
            allCats = FindObjectsOfType<GameObject>().Where(obj => obj.name.Contains("Cat")).ToArray();
        }

        foreach (GameObject otherCat in allCats)
        {
            if (otherCat != gameObject) // 자기 자신 제외
            {
                Collider2D otherCollider = otherCat.GetComponent<Collider2D>();
                if (otherCollider != null && myCollider != null)
                {
                    Physics2D.IgnoreCollision(myCollider, otherCollider, true);
                    Debug.Log($"충돌 무시 설정: {gameObject.name} ↔ {otherCat.name}");
                }
            }
        }
    }

    void Update()
    {
        // ===== 사망 체크 먼저 =====
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            isDead = true;
            return; // 사망 시 모든 입력 무시
        }

        // ===== 턴제 조건 추가 =====
        if (isDead || !canMove)
        {
            // ← 낙사 체크는 움직임과 관계없이 계속 확인
            CheckFallStatus();
            return;
        }

        // ===== 소유권 체크 추가 =====
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
        {
            // 다른 플레이어 캐릭터라도 낙사는 체크
            CheckFallStatus();
            return; // 내 캐릭터가 아니면 입력 무시!
        }

        moveInput = (int)Input.GetAxisRaw("Horizontal");
        lookInput = Input.GetAxisRaw("Vertical");

        // 스프라이트 반전
        if (moveInput != 0)
            spriteRenderer.flipX = moveInput < 0;

        // 애니메이션
        if (animator != null)
            animator.SetBool("isWalking", Mathf.Abs(rb.velocity.x) > 0.01f);

        // 고개 회전
        if (headPivot != null)
        {
            float rot = -lookInput * lookAngleSpeed * Time.deltaTime;
            headPivot.Rotate(0f, 0f, rot);
            float z = headPivot.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            z = Mathf.Clamp(z, -60f, 60f);
            headPivot.localEulerAngles = new Vector3(0, 0, z);
        }

        // ← 낙사 체크 (매 프레임마다)
        CheckFallStatus();
    }

    void FixedUpdate()
    {
        // ===== 사망 체크 먼저 =====
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            isDead = true;
            // 사망 시 물리 정지
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
            }
            return;
        }

        // ===== 턴제 조건 추가 =====
        if (isDead || !canMove) return;

        // ===== 소유권 체크 추가 =====
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return; // 내 캐릭터가 아니면 물리 처리 안 함!

        isGrounded = IsGrounded();

        Vector2 velocity = rb.velocity;

        if (isGrounded)
        {
            velocity.x = moveInput * moveSpeed;

            // 경사에서 미끄러지지 않도록 약간의 수직 보정
            if (moveInput == 0)
                velocity.y = 0f;
        }
        else
        {
            // 공중에서는 x속도는 유지
            velocity.x = moveInput * moveSpeed * 0.9f;
        }

        rb.velocity = velocity;
    }

    // ← 낙사 상태 체크 (강화된 버전)
    void CheckFallStatus()
    {
        float currentY = transform.position.y;

        // 1. 낙사 경고 (노란색 영역)
        if (showFallWarning && currentY <= fallWarningY && currentY > fallLimitY)
        {
            if (!isFallWarningShown)
            {
                isFallWarningShown = true;
                ShowFallWarning();
                Debug.Log($"⚠️ [{gameObject.name}] 낙사 경고! 높이: {currentY:F2}");
            }
        }
        // 경고 영역에서 벗어나면 경고 해제
        else if (currentY > fallWarningY)
        {
            if (isFallWarningShown)
            {
                isFallWarningShown = false;
                HideFallWarning();
            }
        }

        // 2. 낙사 처리 (빨간색 영역 - 즉사)
        if (currentY <= fallLimitY)
        {
            if (!isDead && playerHealth != null && playerHealth.IsAlive)
            {
                Debug.Log($"💀 [{gameObject.name}] 낙사 즉사! 높이: {currentY:F2}");

                // PlayerHealth를 통해 낙사 처리 (RPC로 모든 클라이언트에 동기화됨)
                if (GetComponent<PhotonView>().IsMine)
                {
                    playerHealth.TakeDamage(999f); // 즉사 데미지
                }
            }
        }
    }

    // ← 낙사 경고 표시
    void ShowFallWarning()
    {
        // 캐릭터를 노란색으로 깜빡임 (경고 효과)
        if (spriteRenderer != null)
        {
            StartCoroutine(FallWarningEffect());
        }

        // UI에 경고 메시지 표시 (선택사항)
        string playerName = GetPlayerName();
        Debug.Log($"⚠️ {playerName} 낙사 위험!");

        // 향후 UI 경고창 표시 가능
        // UIManager.ShowWarning($"{playerName} 낙사 위험!");
    }

    // ← 낙사 경고 숨기기
    void HideFallWarning()
    {
        // 경고 효과 중단
        StopCoroutine(FallWarningEffect());

        // 원래 색상으로 복구
        if (spriteRenderer != null && playerHealth != null && playerHealth.IsAlive)
        {
            // 원래 플레이어 색상으로 복구 (LobbyManager에서 가져오기)
            RestoreOriginalColor();
        }
    }

    // ← 낙사 경고 깜빡임 효과
    IEnumerator FallWarningEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        Color warningColor = Color.yellow;

        while (isFallWarningShown && playerHealth != null && playerHealth.IsAlive)
        {
            // 노란색으로 변경
            spriteRenderer.color = warningColor;
            yield return new WaitForSeconds(0.3f);

            // 원래 색상으로 복구
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.3f);
        }

        // 마지막에 원래 색상으로 복구
        if (spriteRenderer != null && playerHealth != null && playerHealth.IsAlive)
        {
            spriteRenderer.color = originalColor;
        }
    }

    // ← 원래 플레이어 색상 복구
    void RestoreOriginalColor()
    {
        // PlayerSpawner에서 설정한 색상으로 복구
        if (PlayerSpawner.Instance != null)
        {
            // ✅ 수정: 올바른 매개변수 전달
            PlayerSpawner.Instance.ApplyPlayerColorFromLobby(gameObject);
        }
        else
        {
            // 백업: LobbyManager에서 직접 가져오기
            LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
            if (lobbyManager != null)
            {
                string playerName = GetPlayerName();
                bool isBot = gameObject.name.Contains("Bot");

                Color playerColor = isBot ?
                    lobbyManager.GetBotColorAsColor(playerName) :
                    lobbyManager.GetPlayerColorAsColor(playerName);

                if (spriteRenderer != null)
                    spriteRenderer.color = playerColor;
            }
        }
    }

    // ← 플레이어 이름 가져오기
    string GetPlayerName()
    {
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && pv.Owner != null)
        {
            return pv.Owner.NickName;
        }
        else
        {
            // 봇인 경우
            return gameObject.name;
        }
    }

    // ===== 새로운 플레이어가 스폰될 때 호출할 함수 ===== ← 새로 추가
    public void RefreshPlayerCollisions()
    {
        IgnorePlayerCollisions();
    }

    // ===== 턴제 연결을 위한 추가 함수 =====
    void OnMovementStateChanged(bool canMoveState)
    {
        canMove = canMoveState;

        // 움직임이 차단될 때 입력 초기화
        if (!canMove)
        {
            moveInput = 0;
            lookInput = 0;

            // 애니메이션도 정지
            if (animator != null)
                animator.SetBool("isWalking", false);
        }

        Debug.Log($"CatController: 움직임 상태 변경 - {canMove}");
    }

    // ✅ IPunObservable 구현 - 고개 회전 동기화 (안전한 버전)
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내가 조종하는 캐릭터 데이터 전송
            // ✅ null 체크 추가
            if (spriteRenderer != null)
                stream.SendNext(spriteRenderer.flipX);
            else
                stream.SendNext(false);

            if (headPivot != null)
            {
                float headRotZ = headPivot.localEulerAngles.z;
                // 각도 정규화 (-180 ~ 180)
                if (headRotZ > 180f) headRotZ -= 360f;
                stream.SendNext(headRotZ);
            }
            else
            {
                stream.SendNext(0f);
            }
        }
        else
        {
            // 다른 플레이어 캐릭터 데이터 수신
            bool flipX = (bool)stream.ReceiveNext();
            float headRotZ = (float)stream.ReceiveNext();

            // ✅ null 체크 추가
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = flipX;
            }

            if (headPivot != null)
            {
                // 수신한 각도 적용
                headPivot.localEulerAngles = new Vector3(0, 0, headRotZ);
                // Debug.Log($"[{gameObject.name}] 고개 회전 동기화: {headRotZ:F1}도");
            }
        }
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    // ← 기존 Die 함수는 PlayerHealth에서 처리하므로 제거하거나 단순화
    void Die()
    {
        isDead = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
        }

        Debug.Log($"[{gameObject.name}] CatController 사망 처리 완료");
    }

    // ← Gizmos로 낙사 영역 시각화
    void OnDrawGizmosSelected()
    {
        // 바닥 감지 영역
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // 낙사 경고 영역 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(-100f, fallWarningY, 0f),
            new Vector3(100f, fallWarningY, 0f)
        );

        // 낙사 즉사 영역 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(-100f, fallLimitY, 0f),
            new Vector3(100f, fallLimitY, 0f)
        );

        // 현재 위치 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    // ===== 이벤트 구독 해제 =====
    void OnDestroy()
    {
        TurnManager.OnPlayerMovementChanged -= OnMovementStateChanged;
    }
}