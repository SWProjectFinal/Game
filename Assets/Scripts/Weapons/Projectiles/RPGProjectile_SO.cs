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

    void FixedUpdate()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 폭발 이펙트
        if (weaponData.explosionEffectPrefab != null)
        {
            Instantiate(weaponData.explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 폭발 반경 내 물리 반응
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

        // 지형 파괴
        if (collision.collider.CompareTag("Ground"))
        {
            SpriteRenderer sr = collision.collider.GetComponent<SpriteRenderer>();
            PolygonCollider2D pc = collision.collider.GetComponent<PolygonCollider2D>();

            Texture2D tex = new Texture2D(
                sr.sprite.texture.width,
                sr.sprite.texture.height,
                TextureFormat.RGBA32,
                false
            );
            tex.SetPixels(sr.sprite.texture.GetPixels());

            Vector2 worldPos = collision.GetContact(0).point;
            Vector2 localPos = sr.transform.InverseTransformPoint(worldPos);

            int pixelX = Mathf.RoundToInt((localPos.x + sr.sprite.bounds.extents.x) * tex.width / sr.sprite.bounds.size.x);
            int pixelY = Mathf.RoundToInt((localPos.y + sr.sprite.bounds.extents.y) * tex.height / sr.sprite.bounds.size.y);

            int radius = Mathf.RoundToInt(weaponData.explosionRadius); // ✅ 반경은 무기 설정에서

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = pixelX + x;
                        int py = pixelY + y;

                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, new Color(0, 0, 0, 0)); // 알파 0 = 투명
                        }
                    }
                }
            }

            tex.Apply();

            sr.sprite = Sprite.Create(
                tex,
                sr.sprite.rect,
                sr.sprite.pivot / sr.sprite.rect.size,
                sr.sprite.pixelsPerUnit
            );

            Destroy(pc);
            sr.gameObject.AddComponent<PolygonCollider2D>();
        }

        Destroy(gameObject); // RPG 제거
    }
}
