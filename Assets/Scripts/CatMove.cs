using UnityEngine;

public class CatMove : MonoBehaviour
{
    public float speed = 2f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float move = Input.GetAxisRaw("Horizontal");

        rb.velocity = new Vector2(move * speed, rb.velocity.y);

        if (move != 0)
            sr.flipX = move < 0;

        // ✅ 요게 핵심!
        animator.SetBool("isMoving", move != 0);
    }
}
