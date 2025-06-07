using UnityEngine;

public class StandardProjectile : MonoBehaviour
{
    public WeaponData_SO weaponData;
    private Rigidbody2D rb;

    public Texture2D holeTexture;
    public float power = 1f; // WeaponManager에서 넘겨주는 값

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
            Vector2 dir = rb.velocity.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
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

        // 폭발 반경 내에 물리력 가하기
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

        // 땅 파괴 로직
        if (collision.collider.CompareTag("Ground"))
        {
            SpriteRenderer sr = collision.collider.GetComponent<SpriteRenderer>();
            PolygonCollider2D pc = collision.collider.GetComponent<PolygonCollider2D>();

            // 새로운 텍스처 생성 (알파 지원)
            Texture2D tex = new Texture2D(
                sr.sprite.texture.width,
                sr.sprite.texture.height,
                TextureFormat.RGBA32,
                false
            );
            tex.SetPixels(sr.sprite.texture.GetPixels());

            // 충돌 지점 → 로컬 → 픽셀 좌표
            Vector2 worldPos = collision.GetContact(0).point;
            Vector2 localPos = sr.transform.InverseTransformPoint(worldPos);

            int pixelX = Mathf.RoundToInt((localPos.x + sr.sprite.bounds.extents.x) * tex.width / sr.sprite.bounds.size.x);
            int pixelY = Mathf.RoundToInt((localPos.y + sr.sprite.bounds.extents.y) * tex.height / sr.sprite.bounds.size.y);

            int radius = 20; // 구멍 반지름 (픽셀 단위)

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
                            tex.SetPixel(px, py, new Color(0, 0, 0, 0)); // 완전 투명
                        }
                    }
                }
            }

            tex.Apply();

            // Sprite 갱신 (pivot 유지)
            sr.sprite = Sprite.Create(
                tex,
                sr.sprite.rect,
                sr.sprite.pivot / sr.sprite.rect.size,
                sr.sprite.pixelsPerUnit
            );

            // Collider 재생성
            Destroy(pc);
            sr.gameObject.AddComponent<PolygonCollider2D>();

            Destroy(gameObject); // 총알 제거
        }
    }
}
