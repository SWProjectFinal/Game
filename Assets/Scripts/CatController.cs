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

    [Header("낙사 처리")]
    public float fallLimitY = -10f;

    [Header("바닥 감지")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.05f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider; // ← 추가

    private int moveInput;
    private float lookInput;
    private bool isDead = false;
    private bool isGrounded;

    // ===== 턴제 연결을 위한 추가 변수 =====
    private bool canMove = false; // ✅ false로 변경! - 게임 시작 전에는 움직이면 안됨

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>(); // ← 추가

        // ✅ 필수 컴포넌트 null 체크
        if (spriteRenderer == null)
            Debug.LogError($"[{gameObject.name}] SpriteRenderer가 없습니다!");
        if (headPivot == null)
            Debug.LogWarning($"[{gameObject.name}] HeadPivot이 설정되지 않았습니다!");

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
        // ===== 턴제 조건 추가 =====
        if (isDead || !canMove) return;

        // ===== 소유권 체크 추가 =====
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return; // 내 캐릭터가 아니면 입력 무시!

        moveInput = (int)Input.GetAxisRaw("Horizontal");
        lookInput = Input.GetAxisRaw("Vertical");

        // 스프라이트 반전
        if (moveInput != 0)
            spriteRenderer.flipX = moveInput < 0;

        // 애니메이션
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

        // 낙사
        if (transform.position.y < fallLimitY)
            Die();
    }

    void FixedUpdate()
    {
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
                Debug.Log($"[{gameObject.name}] 고개 회전 동기화: {headRotZ:F1}도");
            }
        }
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void Die()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        GetComponent<Collider2D>().enabled = false;
        animator.SetBool("isWalking", false);
        Destroy(gameObject, 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // ===== 이벤트 구독 해제 =====
    void OnDestroy()
    {
        TurnManager.OnPlayerMovementChanged -= OnMovementStateChanged;
    }
}