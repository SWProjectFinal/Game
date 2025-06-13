using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnergyWaveController : MonoBehaviour
{
    public float rayLength = 50f;
    public float beamWidth = 1f;
    public float destroyDelay = 0.5f;
    public float damage = 80f;
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public GameObject beamEffectPrefab;

    void Start()
    {
        FireBeam();
        Destroy(gameObject, destroyDelay);
    }

    void FireBeam()
    {
        Vector2 startPos = transform.position;
        Vector2 dir = transform.right;

        // 1️⃣ 지형 파괴
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, dir, rayLength, groundLayer);
        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                DestroyGroundAt(hit.point);
            }
        }

        // 2️⃣ 적에게 데미지
        RaycastHit2D[] enemyHits = Physics2D.RaycastAll(startPos, dir, rayLength, enemyLayer);
        foreach (var hit in enemyHits)
        {
            if (hit.collider != null)
            {
                // 적 스크립트에 맞게 수정 가능
                Debug.Log("적 히트: " + hit.collider.name);
                // 예: hit.collider.GetComponent<Enemy>().TakeDamage(damage);
            }
        }

        // 3️⃣ 이펙트 생성
        if (beamEffectPrefab != null)
        {
            GameObject beam = Instantiate(beamEffectPrefab, startPos, transform.rotation);
            beam.transform.localScale = new Vector3(rayLength, beamWidth, 1);
            Destroy(beam, destroyDelay);
        }
    }

    void DestroyGroundAt(Vector2 worldPos)
    {
        WeaponManager.Instance?.photonView?.RPC("RPC_DestroyTerrain",
            Photon.Pun.RpcTarget.All,
            worldPos.x, worldPos.y,
            worldPos.x, worldPos.y, 0,
            20 // 파괴 반경은 적절히 조정 가능 (지금 20)
        );
    }
}
