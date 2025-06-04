using UnityEngine;

public class RPGProjectile_SO : MonoBehaviour
{
    public WeaponData_SO weaponData;
    public float power = 1f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb != null && weaponData != null)
        {
            rb.gravityScale = weaponData.useGravity ? 1f : 0f;
            float finalPower = Mathf.Max(0.1f, power);
            rb.velocity = transform.right.normalized * weaponData.bulletSpeed * finalPower;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (weaponData.explosionEffectPrefab != null)
        {
            Instantiate(weaponData.explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 폭발 반경 처리 예시 (물리효과 넣고 싶으면 여기에 추가)
        // Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, weaponData.explosionRadius);

        Destroy(gameObject);
    }
}
