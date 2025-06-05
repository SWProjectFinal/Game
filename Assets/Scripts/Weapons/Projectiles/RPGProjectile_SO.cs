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

        // 나중에 폭발 반경 구현 가능
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

}
