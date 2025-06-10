using UnityEngine;

public class BlackholeProjectile_SO : MonoBehaviour
{
    public WeaponData_SO weaponData;
    public float power = 1f;
    public Vector2 shootDirection = Vector2.right;

    public GameObject blackholePrefab;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb == null)
        {
            Debug.LogWarning("❌ Rigidbody2D 누락됨");
            return;
        }

        if (weaponData == null)
        {
            Debug.LogWarning("❌ weaponData가 null 상태로 블랙홀 생성됨");
            return;
        }

        rb.gravityScale = weaponData.useGravity ? 1f : 0f;
        float finalPower = Mathf.Max(0.1f, power);
        rb.velocity = shootDirection.normalized * weaponData.bulletSpeed * finalPower; // ✅ 변경

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Projectile";
            sr.sortingOrder = 5;
        }
    }




    void FixedUpdate()
    {
        if (rb.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            // 블랙홀 생성
            if (blackholePrefab != null)
            {
                Instantiate(blackholePrefab, transform.position, Quaternion.identity);
            }

            // 시각 이펙트 (옵션)
            if (weaponData.explosionEffectPrefab != null)
            {
                Instantiate(weaponData.explosionEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        Destroy(gameObject); // 총알 제거
    }

}
