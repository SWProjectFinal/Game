// ? 상자 먹기 처리

using UnityEngine;
using Photon.Pun;

public class ItemBox : MonoBehaviourPunCallbacks
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") && col.GetComponent<PhotonView>().IsMine)
        {
            WeaponType randomItem = GetRandomItem();
            WeaponData newItem = WeaponManager.Instance.GetWeaponByType(randomItem);

            // 탄 수 및 무한 여부 설정
            newItem.ammoCount = 1;
            newItem.isInfiniteAmmo = false;

            WeaponManager.Instance.AddWeapon(newItem);
            PhotonNetwork.Destroy(gameObject);
        }
    }


    WeaponType GetRandomItem()
    {
        return (WeaponType)Random.Range(1, System.Enum.GetValues(typeof(WeaponType)).Length);
    }
}

