using UnityEngine;

public class BlackholeProjectile : BaseProjectile
{
    public GameObject blackholePrefab;

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (blackholePrefab != null)
        {
            Instantiate(blackholePrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
