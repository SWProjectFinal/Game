//투사체 충돌 처리

using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviourPunCallbacks
{
    void OnCollisionEnter2D(Collision2D col)
    {
        if (photonView.IsMine)
        {
            // 예: 데미지 계산, 이펙트 발생
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
