using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class CatMove : MonoBehaviour
{
    public float speed = 2f;  // 이동 속도
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private GroundCheck groundCheck;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        groundCheck = GetComponent<GroundCheck>();
    }

    void Update()
    {
        float move = Input.GetAxisRaw("Horizontal");

        // 걷기
        rb.velocity = new Vector2(move * speed, rb.velocity.y);

        // 방향 전환
        if (move != 0)
            sr.flipX = move < 0;

        // 걷는 애니메이션
        animator.SetBool("isMoving", move != 0);

        // 낙사 처리
        if (!groundCheck.IsGrounded() && transform.position.y < -10f)
        {
            Debug.Log("떨어져서 죽음!");
            Destroy(gameObject);  // 또는 Respawn 호출
        }
    }
}
