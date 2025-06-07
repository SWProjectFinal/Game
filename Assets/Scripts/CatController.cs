using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // ← 추가 필요!

public class CatController : MonoBehaviour
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

    private int moveInput;
    private float lookInput;
    private bool isDead = false;
    private bool isGrounded;

    // ===== 턴제 연결을 위한 추가 변수 =====
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // ===== 턴제 이벤트 구독 =====
        TurnManager.OnPlayerMovementChanged += OnMovementStateChanged;
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
        if (isDead || !canMove) return;

        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return;

        isGrounded = IsGrounded();

        Vector2 velocity = rb.velocity;

        // 경사 보정 로직
        if (isGrounded && moveInput != 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.5f, groundLayer);
            if (hit.collider != null)
            {
                // 경사면 방향 계산 (법선 벡터 기준으로 오른쪽 방향을 구함)
                Vector2 slopeDir = Vector2.Perpendicular(hit.normal).normalized;
                if (slopeDir.y < 0) slopeDir *= -1f; // 오른쪽 경사로 보정

                velocity = slopeDir * moveInput * moveSpeed;
            }
            else
            {
                // 경사 정보 없을 땐 기본 수평 이동
                velocity.x = moveInput * moveSpeed;
            }
        }
        else if (!isGrounded)
        {
            velocity.x = moveInput * moveSpeed * 0.9f;
        }
        else
        {
            // 정지 시 y속도 고정
            velocity.y = 0f;
            velocity.x = 0f;
        }

        rb.velocity = velocity;
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