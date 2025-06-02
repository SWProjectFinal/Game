using UnityEngine;

public class RPGProjectile : BaseProjectile
{

    protected override void Start()
    {
        base.Start();
        Debug.Log("🚀 RPGProjectile 발사됨!");
    }

    [Header("폭발 설정")]
    public float explosionRadius = 3f;
    public float explosionForce = 20f;
    public GameObject explosionEffectPrefab;

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in colliders)
        {
            Rigidbody2D rb = col.attachedRigidbody;
            if (rb != null)
            {
                Vector2 dir = rb.position - (Vector2)transform.position;
                rb.AddForce(dir.normalized * explosionForce, ForceMode2D.Impulse);
            }

            // TODO: 플레이어나 적이면 데미지 처리도 추가 가능
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
