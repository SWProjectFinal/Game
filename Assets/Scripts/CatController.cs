using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatController : MonoBehaviour
{
    [Header("이동 관련")]
    public float moveSpeed = 5f;
    public float slopeAssistForce = 30f;

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

    private Vector2 slopeNormal = Vector2.up; // 경사 보정용

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isDead) return;

        moveInput = (int)Input.GetAxisRaw("Horizontal");
        lookInput = Input.GetAxisRaw("Vertical");

        if (moveInput != 0)
            spriteRenderer.flipX = moveInput < 0;

        float horizontalSpeed = Mathf.Abs(rb.velocity.x);
        animator.SetBool("isWalking", horizontalSpeed > 0.01f);

        // 고개 회전
        if (headPivot != null)
        {
            float rotation = -lookInput * lookAngleSpeed * Time.deltaTime;
            headPivot.Rotate(0f, 0f, rotation);

            float z = headPivot.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            z = Mathf.Clamp(z, -60f, 60f);
            headPivot.localEulerAngles = new Vector3(0, 0, z);
        }

        // 낙사 감지
        if (transform.position.y < fallLimitY)
            Die();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (IsGrounded(out slopeNormal))
        {
            // Slope 보정 방향 계산
            Vector2 moveDir = Vector2.Perpendicular(slopeNormal).normalized;
            rb.velocity = moveDir * moveInput * moveSpeed;

            // 미끄럼 방지 (경사에서 가만히 있으면 멈춤)
            if (moveInput == 0)
            {
                rb.velocity = Vector2.zero;
            }
        }
        else
        {
            // 공중에서는 X만 보정
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        }
    }

    bool IsGrounded(out Vector2 groundNormal)
    {
        groundNormal = Vector2.up;

        if (groundCheck == null)
            return false;

        RaycastHit2D hit = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, Vector2.down, 0.05f, groundLayer);

        if (hit.collider != null)
        {
            groundNormal = hit.normal;
            return true;
        }

        return false;
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
}
