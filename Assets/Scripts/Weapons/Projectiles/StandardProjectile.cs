using UnityEngine;

public class StandardProjectile : MonoBehaviour
{
    public WeaponData_SO weaponData;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb != null && weaponData != null)
        {
            rb.velocity = transform.right * weaponData.bulletSpeed;
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
