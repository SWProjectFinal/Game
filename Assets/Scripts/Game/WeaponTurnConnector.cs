using UnityEngine;
using Photon.Pun;
using System.Linq;

public class WeaponTurnConnector : MonoBehaviourPun
{
    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart += OnTurnStarted;
        }
    }

    void OnTurnStarted(Photon.Realtime.Player currentPlayer)
    {
        if (currentPlayer == null) return;

        GameObject playerObj = PlayerSpawner.Instance.GetPlayerObject(currentPlayer.NickName);
        if (playerObj == null)
        {
            Debug.LogWarning($"❌ 현재 턴 플레이어 GameObject를 찾지 못함: {currentPlayer.NickName}");
            return;
        }

        Transform firePoint = playerObj.GetComponentsInChildren<Transform>(true)
                                       .FirstOrDefault(t => t.name == "FirePoint");

        if (firePoint != null)
        {
            WeaponManager.Instance.SetFirePoint(firePoint);
            Debug.Log($"🎯 FirePoint 연결 성공: {currentPlayer.NickName}");
        }
        else
        {
            Debug.LogWarning("❌ FirePoint를 자식에서 못 찾음");
        }
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart -= OnTurnStarted;
        }
    }
}
