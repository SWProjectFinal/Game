using UnityEngine;
using Photon.Pun;

// 테스트용 더미 아이템 박스 (PhotonView 없는 안전한 버전)
public class DummyItemBox : MonoBehaviour // ← MonoBehaviourPun 제거!
{
  [Header("더미 설정")]
  public bool useDropTable = true;
  public ItemDropTable dropTable;

  [Header("백업 아이템 목록 (드랍 테이블 없을 때)")]
  public string[] dummyItems = {
        "블랙홀", "RPG", "화염병", "카펫폭탄", "에너지웨이브", "회복템"
    }; // ⚠️ 기본무기 제외하고 2~7번만

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

      // ✅ ItemSpawner를 통해 네트워크 동기화
      if (ItemSpawner.Instance != null)
      {
        ItemSpawner.Instance.OnItemPickedUp(gameObject, other.name, itemName);
      }
      else
      {
        // 백업: ItemSpawner가 없으면 로컬에서만 처리
        Debug.LogWarning("ItemSpawner.Instance가 null입니다! 로컬 처리만 됩니다.");
        PlayPickupEffect();
        Destroy(gameObject);
      }
    }
  }

  // 랜덤 더미 아이템 선택
  string GetRandomDummyItem()
  {
    if (useDropTable && dropTable != null)
    {
      // 드랍 테이블 사용
      WeaponType selectedWeapon = dropTable.GetRandomItem();
      var itemData = dropTable.GetItemData(selectedWeapon);

      if (itemData != null)
      {
        return $"{itemData.itemName} ({itemData.rarity})";
      }
      else
      {
        return selectedWeapon.ToString();
      }
    }
    else
    {
      // 백업 시스템: 균등 확률
      if (dummyItems.Length > 0)
      {
        int randomIndex = Random.Range(0, dummyItems.Length);
        return dummyItems[randomIndex];
      }
    }

    return "알 수 없는 아이템";
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