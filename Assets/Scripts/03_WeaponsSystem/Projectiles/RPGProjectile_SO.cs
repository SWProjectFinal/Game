using UnityEngine;
using Photon.Pun; // ← 추가

public class RPGProjectile_SO : MonoBehaviour
{
    public WeaponData_SO weaponData;
    public float power = 1f;
    public Vector2 shootDirection = Vector2.right;

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
            Debug.LogWarning("❌ weaponData가 null 상태로 RPG 생성됨");
            return;
        }

        rb.gravityScale = weaponData.useGravity ? 1f : 0f;
        float finalPower = Mathf.Max(0.1f, power);
        rb.velocity = shootDirection.normalized * weaponData.bulletSpeed * finalPower;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Projectile";
            sr.sortingOrder = 5;
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
        Vector3 explosionCenter = transform.position;

        // ✅ 수정: 모든 클라이언트에서 데미지 적용 (마스터 체크 제거)
        if (weaponData.damage > 0f)
        {
            // RPG는 10미터 범위 데미지
            float damageRadius = 10f;

            Debug.Log($"💥 RPG 폭발: 중심 {explosionCenter}, 데미지 범위 {damageRadius}m, 데미지 {weaponData.damage}");

            DamageSystem.ApplyExplosionDamage(
                explosionCenter,
                damageRadius,           // 10미터 데미지 범위
                weaponData.damage       // 28 데미지
            );
        }

        // 폭발 이펙트 (모든 클라이언트에서 실행)
        if (weaponData.explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(weaponData.explosionEffectPrefab, explosionCenter, Quaternion.identity);
            float scaleFactor = weaponData.explosionRadius / 50f;
            fx.transform.localScale = Vector3.one * scaleFactor;
            Destroy(fx, 3f);
        }

        // 강력한 물리적 폭발력 (모든 클라이언트에서 실행)
        if (weaponData.explosionRadius > 0f && weaponData.explosionForce > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(explosionCenter, weaponData.explosionRadius);
            foreach (var hit in hits)
            {
                Rigidbody2D targetRb = hit.attachedRigidbody;
                if (targetRb != null)
                {
                    Vector2 dir = targetRb.position - (Vector2)explosionCenter;
                    // RPG는 폭발력이 더 강함
                    targetRb.AddForce(dir.normalized * weaponData.explosionForce * 1.5f, ForceMode2D.Impulse);
                }
            }
        }

        // 지형 파괴 (모든 클라이언트에서 실행)
        if (collision.collider.CompareTag("Ground"))
        {
            DestroyTerrain(collision, explosionCenter);
        }

        // RPG 제거
        Destroy(gameObject);
    }

    // 지형 파괴 로직 (기존과 동일)
    void DestroyTerrain(Collision2D collision, Vector3 explosionCenter)
    {
        SpriteRenderer sr = collision.collider.GetComponent<SpriteRenderer>();
        PolygonCollider2D pc = collision.collider.GetComponent<PolygonCollider2D>();

        if (sr == null || sr.sprite == null) return;

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

        // RPG 지형 파괴 반경 (weaponData.explosionRadius 사용 - 50픽셀)
        int radius = Mathf.RoundToInt(weaponData.explosionRadius);

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
                        tex.SetPixel(px, py, new Color(0, 0, 0, 0)); // 투명하게
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

        if (pc != null)
        {
            Destroy(pc);
            sr.gameObject.AddComponent<PolygonCollider2D>();
        }
    }
}