using UnityEngine;

public class StandardProjectile : MonoBehaviour
{
    public WeaponData_SO weaponData;
    private Rigidbody2D rb;

    public float power = 1f; // 외부에서 WeaponManager가 넘겨줄 파워


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb != null && weaponData != null)
        {
            rb.gravityScale = weaponData.useGravity ? 1f : 0f;

            // ✅ 최소 파워 보정
            float finalPower = Mathf.Max(0.1f, power);
            rb.velocity = transform.right.normalized * weaponData.bulletSpeed * finalPower;
        }
    }


    void FixedUpdate()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            Vector2 dir = rb.velocity.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        // 이펙트 생성
        if (weaponData.explosionEffectPrefab != null)
        {
            Instantiate(weaponData.explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 폭발 범위 처리
        if (weaponData.explosionRadius > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, weaponData.explosionRadius);
            foreach (var hit in hits)
            {
                Rigidbody2D targetRb = hit.attachedRigidbody;
                if (targetRb != null)
                {
                    Vector2 dir = targetRb.position - (Vector2)transform.position;
                    targetRb.AddForce(dir.normalized * weaponData.explosionForce, ForceMode2D.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }
}
