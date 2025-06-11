using UnityEngine;
using Photon.Pun;

// 테스트용 더미 아이템 박스 (PhotonView 없는 안전한 버전)
public class DummyItemBox : MonoBehaviour // ← MonoBehaviourPun 제거!
{
  [Header("더미 설정")]
  //public bool useDropTable = true;
  //public ItemDropTable dropTable;

  [Header("백업 아이템 목록 (드랍 테이블 없을 때)")]
  public string[] dummyItems = {
    "Blackhole", "RPG"
  };

  [Header("이펙트")]
  public GameObject pickupEffect;
  public AudioClip pickupSound;

  // ✅ 중복 습득 방지 플래그
  private bool isPickedUp = false;

  void Start()
  {
    // 물음표 표시를 위한 간단한 애니메이션 (선택사항)
    //StartCoroutine(FloatingAnimation());
  }

  void OnTriggerEnter2D(Collider2D other)
  {
    // ✅ 중복 습득 방지
    if (isPickedUp) return;

    // 플레이어만 아이템 습득 가능
    if (other.CompareTag("Player"))
    {
      // 네트워크 오브젝트라면 소유권 체크
      PhotonView pv = other.GetComponent<PhotonView>();
      if (pv != null && !pv.IsMine)
      {
        return; // 내 캐릭터가 아니면 무시
      }

      // ✅ 중복 습득 방지
      isPickedUp = true;

      // 랜덤 아이템 선택
      string itemName = GetRandomDummyItem();

      Debug.Log($"🎁 {other.name}이 아이템 습득: {itemName}");

      // ✅ 수정: 마스터 클라이언트에게 RPC로 요청
      string playerName = GetPlayerName(other);
      Vector3 boxPosition = transform.position;

      // 모든 클라이언트에서 즉시 이펙트 재생
      PlayPickupEffect();

      if (ItemSpawner.Instance != null)
      {
        // 마스터 클라이언트에게 아이템 습득 알림
        ItemSpawner.Instance.photonView.RPC("RPC_RequestItemPickup", RpcTarget.MasterClient,
            playerName, itemName, boxPosition.x, boxPosition.y, boxPosition.z, gameObject.GetInstanceID());
      }
      else
      {
        Debug.LogWarning("ItemSpawner.Instance가 null입니다!");
      }

      // ✅ 일단 로컬에서 박스 비활성화 (시각적 피드백)
      gameObject.SetActive(false);
    }
  }

  // ✅ 새로 추가: 플레이어 이름 가져오기
  string GetPlayerName(Collider2D playerCollider)
  {
    PhotonView pv = playerCollider.GetComponent<PhotonView>();
    if (pv != null && pv.Owner != null)
    {
      return pv.Owner.NickName;
    }
    else
    {
      // 봇인 경우
      return playerCollider.gameObject.name;
    }
  }

  // 랜덤 더미 아이템 선택
  string GetRandomDummyItem()
  {
    int randomIndex = Random.Range(0, dummyItems.Length);
    return dummyItems[randomIndex];
  }

  // 습득 이펙트 재생
  public void PlayPickupEffect()
  {
    // 이펙트 생성
    if (pickupEffect != null)
    {
      Instantiate(pickupEffect, transform.position, Quaternion.identity);
    }

    // 사운드 재생
    if (pickupSound != null)
    {
      AudioSource.PlayClipAtPoint(pickupSound, transform.position);
    }
  }

  // 둥둥 떠다니는 애니메이션
  System.Collections.IEnumerator FloatingAnimation()
  {
    Vector3 startPos = transform.position;
    float time = 0f;

    while (true)
    {
      time += Time.deltaTime;
      float yOffset = Mathf.Sin(time * 2f) * 0.2f; // 위아래로 0.2만큼 움직임
      transform.position = startPos + Vector3.up * yOffset;
      yield return null;
    }
  }

  // 생성 시 파티클 이펙트 (선택사항)
  void OnEnable()
  {
    Debug.Log("🎁 아이템 박스 생성됨!");
  }
}