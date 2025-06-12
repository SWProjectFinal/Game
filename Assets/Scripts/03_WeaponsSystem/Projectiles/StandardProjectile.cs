using UnityEngine;
using Photon.Pun;

public class StandardProjectile : MonoBehaviour
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
        if (rb != null && weaponData != null)
        {
            rb.gravityScale = weaponData.useGravity ? 1f : 0f;
            float finalPower = Mathf.Max(0.1f, power);
            rb.velocity = shootDirection.normalized * weaponData.bulletSpeed * finalPower;
        }

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
            Vector2 dir = rb.velocity.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 explosionCenter = transform.position;

        // ✅ 데미지 적용 (모든 클라이언트에서 실행)
        if (weaponData.damage > 0f)
        {
            float damageRadius = 5f; // 기본무기 5미터 범위

            Debug.Log($"💥 기본무기 폭발: 중심 {explosionCenter}, 데미지 범위 {damageRadius}m, 데미지 {weaponData.damage}");

            DamageSystem.ApplyExplosionDamage(
                explosionCenter,
                damageRadius,           // 5미터 데미지 범위
                weaponData.damage       // 18 데미지
            );
        }

        // ✅ 폭발 이펙트 (모든 클라이언트에서 실행)
        if (weaponData.explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(weaponData.explosionEffectPrefab, explosionCenter, Quaternion.identity);
            float scaleFactor = weaponData.explosionRadius / 30f;
            fx.transform.localScale = Vector3.one * scaleFactor;
            Destroy(fx, 2f);
        }

        // ✅ 물리적 폭발력 (모든 클라이언트에서 실행)
        if (weaponData.explosionRadius > 0f && weaponData.explosionForce > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(explosionCenter, weaponData.explosionRadius);
            foreach (var hit in hits)
            {
                Rigidbody2D targetRb = hit.attachedRigidbody;
                if (targetRb != null)
                {
                    Vector2 dir = targetRb.position - (Vector2)explosionCenter;
                    targetRb.AddForce(dir.normalized * weaponData.explosionForce, ForceMode2D.Impulse);
                }
            }
        }

        // ✅ 지형 파괴 RPC 동기화
        if (collision.collider.CompareTag("Ground"))
        {
            // 모든 클라이언트에 지형파괴 RPC 전송
            Vector2 worldPos = collision.GetContact(0).point;
            int radius = Mathf.RoundToInt(weaponData.explosionRadius > 0 ? weaponData.explosionRadius : 20f);

            // WeaponManager를 통해 RPC 전송
            if (WeaponManager.Instance != null && WeaponManager.Instance.photonView != null)
            {
                WeaponManager.Instance.photonView.RPC("RPC_DestroyTerrain", RpcTarget.All,
                    worldPos.x, worldPos.y, explosionCenter.x, explosionCenter.y, explosionCenter.z, radius);
            }
            else
            {
                // 백업: 로컬에서만 실행
                DestroyTerrain(collision, explosionCenter);
            }
        }

        // ✅ 총알 제거
        Destroy(gameObject);
    }

    // 백업용 로컬 지형 파괴 함수
    void DestroyTerrain(Collision2D collision, Vector3 explosionCenter)
    {
        SpriteRenderer sr = collision.collider.GetComponent<SpriteRenderer>();
        PolygonCollider2D pc = collision.collider.GetComponent<PolygonCollider2D>();

        if (sr == null || sr.sprite == null) return;

        // 새로운 텍스처 생성
        Texture2D tex = new Texture2D(
            sr.sprite.texture.width,
            sr.sprite.texture.height,
            TextureFormat.RGBA32,
            false
        );
        tex.SetPixels(sr.sprite.texture.GetPixels());

        // 충돌 지점 → 픽셀 좌표 변환
        Vector2 worldPos = collision.GetContact(0).point;
        Vector2 localPos = sr.transform.InverseTransformPoint(worldPos);

        int pixelX = Mathf.RoundToInt((localPos.x + sr.sprite.bounds.extents.x) * tex.width / sr.sprite.bounds.size.x);
        int pixelY = Mathf.RoundToInt((localPos.y + sr.sprite.bounds.extents.y) * tex.height / sr.sprite.bounds.size.y);

        // 지형 파괴 반경
        int radius = Mathf.RoundToInt(weaponData.explosionRadius > 0 ? weaponData.explosionRadius : 20f);

        // 원형으로 픽셀 제거
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

        // 스프라이트 업데이트
        sr.sprite = Sprite.Create(
            tex,
            sr.sprite.rect,
            sr.sprite.pivot / sr.sprite.rect.size,
            sr.sprite.pixelsPerUnit
        );

        // 콜라이더 재생성
        if (pc != null)
        {
            Destroy(pc);
            sr.gameObject.AddComponent<PolygonCollider2D>();
        }
    }
}