using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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