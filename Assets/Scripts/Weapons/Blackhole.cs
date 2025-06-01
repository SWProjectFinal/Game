using UnityEngine;

public class Blackhole : MonoBehaviour
{
    public float pullRadius = 5f;
    public float pullForce = 10f;
    public float duration = 3f;

    private float timer;

    void Start()
    {
        timer = duration;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, pullRadius);
        foreach (Collider2D col in targets)
        {
            if (col.attachedRigidbody != null && col.gameObject != this.gameObject)
            {
                Vector2 dir = (transform.position - col.transform.position).normalized;
                col.attachedRigidbody.AddForce(dir * pullForce);
            }
        }
    }
}
