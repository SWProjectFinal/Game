public class Projectile : MonoBehaviourPunCallbacks
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (photonView.IsMine)
        {
            // 예: terrain 충돌 시 지형 파괴
            Vector2 hitPoint = collision.contacts[0].point;
            photonView.RPC("RPC_DestroyTerrain", RpcTarget.All, hitPoint);

            PhotonNetwork.Destroy(gameObject);  // 투사체 제거
        }
    }

    [PunRPC]
    void RPC_DestroyTerrain(Vector2 point)
    {
        // 지형 파괴 처리 로직
    }
}
