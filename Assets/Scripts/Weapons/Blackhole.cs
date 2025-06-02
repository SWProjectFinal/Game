using UnityEngine;

public class Blackhole : MonoBehaviour
{
    [Header("이펙트 설정")]
    public GameObject suctionEffectPrefab;

    [Header("물리 설정")]
    public float pullForce = 15f;
    public float duration = 3f;

    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, duration);

        // 흡입 이펙트 소환
        if (suctionEffectPrefab != null)
        {
            Instantiate(suctionEffectPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;

        if (rb != null && rb.gameObject != this.gameObject)
        {
            Vector2 direction = (transform.position - rb.transform.position).normalized;
            float distance = Vector2.Distance(transform.position, rb.transform.position);

            // 가까울수록 세게 당기도록
            float force = pullForce / Mathf.Max(distance, 0.5f); // 너무 가까운 건 제한
            rb.AddForce(direction * force * Time.deltaTime, ForceMode2D.Impulse);
        }
    }
}
