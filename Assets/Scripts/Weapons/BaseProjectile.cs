using UnityEngine;

public class BaseProjectile : MonoBehaviour
{
    public float power = 10f;
    protected Rigidbody2D rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            RotateTowardVelocity();
        }
    }

    protected virtual void FixedUpdate()
    {
        RotateTowardVelocity(); // 계속 회전 업데이트
    }

    protected virtual void RotateTowardVelocity()
    {
        Vector2 velocity = rb.velocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject); // 충돌 시 총알 제거
    }

}
