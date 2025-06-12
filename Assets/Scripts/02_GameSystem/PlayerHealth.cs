using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun, IDamageable
{
  [Header("체력 설정")]
  public float maxHealth = 100f;
  public float currentHealth = 100f;

  [Header("낙사 설정")]
  public float fallDeathY = -15f; // 이 높이 이하로 떨어지면 즉사

  [Header("사망 이펙트")]
  public GameObject deathEffect;
  public AudioClip deathSound;

  [Header("데미지 이펙트")]
  public GameObject damageEffect;
  public AudioClip damageSound;

  // 상태 관리
  public bool IsAlive { get; private set; } = true;
  public bool IsFalling { get; private set; } = false;

  // 컴포넌트 참조
  private CatController catController;
  private SpriteRenderer spriteRenderer;
  private Collider2D myCollider;
  private Rigidbody2D rb;

  // 이벤트
  public static System.Action<string> OnPlayerDied; // 플레이어 사망 시 발생
  public static System.Action<string, float> OnPlayerHealthChanged; // 체력 변경 시 발생

  void Start()
  {
    // 컴포넌트 가져오기
    catController = GetComponent<CatController>();
    spriteRenderer = GetComponent<SpriteRenderer>();
    myCollider = GetComponent<Collider2D>();
    rb = GetComponent<Rigidbody2D>();

    // 초기 체력 설정
    currentHealth = maxHealth;

    // 체력 UI 초기화
    UpdateHealthUI();

    Debug.Log($"[{gameObject.name}] PlayerHealth 초기화 완료 - HP: {currentHealth}/{maxHealth}");
  }

  void Update()
  {
    // 낙사 체크 (지형파괴로 떨어질 때)
    if (IsAlive && transform.position.y < fallDeathY)
    {
      if (!IsFalling)
      {
        IsFalling = true;
        Debug.Log($"[{gameObject.name}] 낙사 감지! 높이: {transform.position.y}");

        // 낙사는 즉사
        FallDeath();
      }
    }
  }

  // IDamageable 인터페이스 구현
  public void TakeDamage(float damage, Vector3 explosionCenter, float explosionRadius)
  {
    if (!IsAlive) return;

    // ✅ 중복 방지: 자신의 캐릭터만 데미지 처리
    PhotonView pv = GetComponent<PhotonView>();
    if (pv != null && !pv.IsMine) return; // 내 캐릭터가 아니면 무시

    // 실제 데미지 계산 (거리 기반)
    float distance = Vector3.Distance(transform.position, explosionCenter);
    float damageMultiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
    float actualDamage = damage * damageMultiplier;

    // ✅ 네트워크 플레이어는 RPC로 모든 클라이언트에 동기화
    if (pv != null)
    {
      pv.RPC("ApplyDamage", RpcTarget.All, actualDamage, explosionCenter.x, explosionCenter.y, explosionCenter.z);
    }
    else
    {
      // 봇은 로컬에서만 처리
      ApplyDamageLocal(actualDamage, explosionCenter);
    }
  }

  // 기본 데미지 (폭발 범위 없음)
  public void TakeDamage(float damage)
  {
    if (!IsAlive) return;

    // ✅ 봇 안전성 체크 추가
    PhotonView pv = GetComponent<PhotonView>();
    if (pv != null && !pv.IsMine) return;

    // ✅ 봇과 플레이어 구분 처리
    if (pv != null)
    {
      // 네트워크 플레이어
      pv.RPC("ApplyDamage", RpcTarget.All, damage, transform.position.x, transform.position.y, transform.position.z);
    }
    else
    {
      // 봇: 로컬에서만 직접 데미지 적용
      ApplyDamageLocal(damage, transform.position);
      Debug.Log($"🤖 봇 데미지: {gameObject.name} - {damage:F1}");
    }
  }

  void ApplyDamageLocal(float damage, Vector3 sourcePos)
  {
    if (!IsAlive) return;

    // 체력 감소
    currentHealth -= damage;
    currentHealth = Mathf.Max(0f, currentHealth);

    Debug.Log($"[{gameObject.name}] 데미지 {damage:F1} 받음! 현재 HP: {currentHealth:F1}/{maxHealth}");

    // 데미지 이펙트
    PlayDamageEffect(sourcePos);

    // 체력 UI 업데이트
    UpdateHealthUI();

    // 사망 체크
    if (currentHealth <= 0f && IsAlive)
    {
      Die();
    }
  }


  // RPC: 모든 클라이언트에서 데미지 적용
  [PunRPC]
  void ApplyDamage(float damage, float sourceX, float sourceY, float sourceZ)
  {
    if (!IsAlive) return;

    // 체력 감소
    currentHealth -= damage;
    currentHealth = Mathf.Max(0f, currentHealth);

    Debug.Log($"[{gameObject.name}] 데미지 {damage:F1} 받음! 현재 HP: {currentHealth:F1}/{maxHealth}");

    // 데미지 이펙트
    PlayDamageEffect(new Vector3(sourceX, sourceY, sourceZ));

    // 체력 UI 업데이트
    UpdateHealthUI();

    // 사망 체크
    if (currentHealth <= 0f && IsAlive)
    {
      Die();
    }
  }

  // 체력 회복
  public void Heal(float amount)
  {
    if (!IsAlive) return;
    if (!photonView.IsMine) return;

    photonView.RPC("ApplyHeal", RpcTarget.All, amount);
  }

  [PunRPC]
  void ApplyHeal(float amount)
  {
    if (!IsAlive) return;

    currentHealth += amount;
    currentHealth = Mathf.Min(maxHealth, currentHealth);

    Debug.Log($"[{gameObject.name}] 체력 {amount:F1} 회복! 현재 HP: {currentHealth:F1}/{maxHealth}");

    UpdateHealthUI();
  }

  // 낙사 처리
  void FallDeath()
  {
    if (!photonView.IsMine) return;

    Debug.Log($"[{gameObject.name}] 낙사로 인한 즉사!");

    // RPC로 모든 클라이언트에 낙사 알림
    photonView.RPC("ApplyFallDeath", RpcTarget.All);
  }

  [PunRPC]
  void ApplyFallDeath()
  {
    currentHealth = 0f;
    UpdateHealthUI();
    Die();
  }

  // 사망 처리
  void Die()
  {
    if (!IsAlive) return;

    IsAlive = false;

    string playerName = GetPlayerName();
    Debug.Log($"[{playerName}] 사망!");

    // 사망 이펙트
    PlayDeathEffect();

    // 캐릭터 비활성화
    DisableCharacter();

    // 체력 UI 업데이트
    UpdateHealthUI();

    // 사망 이벤트 발생 (GameManager가 수신)
    OnPlayerDied?.Invoke(playerName);

    // 내 캐릭터가 죽었다면 턴 강제 종료
    if (photonView.IsMine && TurnManager.Instance != null && TurnManager.Instance.IsMyTurn())
    {
      // 턴 즉시 종료 (사망 시 턴 넘어감)
      StartCoroutine(DelayedTurnEnd());
    }
  }

  System.Collections.IEnumerator DelayedTurnEnd()
  {
    yield return new WaitForSeconds(1f); // 사망 이펙트 시간

    if (TurnManager.Instance != null)
    {
      TurnManager.Instance.ForceEndTurn();
    }
  }

  // 캐릭터 비활성화
  void DisableCharacter()
  {
    // 움직임 비활성화
    if (catController != null)
    {
      catController.enabled = false;
    }

    // 물리 비활성화
    if (rb != null)
    {
      rb.velocity = Vector2.zero;
      rb.isKinematic = true;
    }

    // 충돌 비활성화
    if (myCollider != null)
    {
      myCollider.enabled = false;
    }

    // 스프라이트 어둡게 (사망 표시)
    if (spriteRenderer != null)
    {
      Color deadColor = spriteRenderer.color;
      deadColor.a = 0.5f; // 반투명
      spriteRenderer.color = deadColor;
    }
  }

  // 데미지 이펙트 재생
  void PlayDamageEffect(Vector3 damageSource)
  {
    if (damageEffect != null)
    {
      GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
      Destroy(effect, 2f);
    }

    if (damageSound != null)
    {
      AudioSource.PlayClipAtPoint(damageSound, transform.position);
    }

    // 간단한 피격 효과 (스프라이트 깜빡임)
    if (spriteRenderer != null)
    {
      StartCoroutine(DamageFlash());
    }
  }

  // 피격 시 깜빡임 효과
  System.Collections.IEnumerator DamageFlash()
  {
    Color originalColor = spriteRenderer.color;
    spriteRenderer.color = Color.red;

    yield return new WaitForSeconds(0.1f);

    if (spriteRenderer != null && IsAlive)
    {
      spriteRenderer.color = originalColor;
    }
  }

  // 사망 이펙트 재생
  void PlayDeathEffect()
  {
    if (deathEffect != null)
    {
      GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
      Destroy(effect, 3f);
    }

    if (deathSound != null)
    {
      AudioSource.PlayClipAtPoint(deathSound, transform.position);
    }
  }

  // 체력 UI 업데이트
  void UpdateHealthUI()
  {
    string playerName = GetPlayerName();
    float healthPercentage = (currentHealth / maxHealth) * 100f;

    // GameUIManager에 체력 업데이트 알림
    OnPlayerHealthChanged?.Invoke(playerName, healthPercentage);
  }

  // 플레이어 이름 가져오기
  string GetPlayerName()
  {
    PhotonView pv = GetComponent<PhotonView>();
    if (pv != null && pv.Owner != null)
    {
      return pv.Owner.NickName;
    }
    else
    {
      // 봇인 경우
      return gameObject.name;
    }
  }

  // IDamageable 인터페이스 구현
  public Transform GetTransform()
  {
    return transform;
  }

  // 현재 체력 퍼센트 가져오기
  public float GetHealthPercentage()
  {
    return (currentHealth / maxHealth) * 100f;
  }

  // 디버그용 - 강제 데미지
  [ContextMenu("테스트 데미지 (20)")]
  public void TestDamage()
  {
    TakeDamage(20f);
  }

  [ContextMenu("테스트 힐 (30)")]
  public void TestHeal()
  {
    Heal(30f);
  }

  [ContextMenu("테스트 즉사")]
  public void TestInstantDeath()
  {
    TakeDamage(999f);
  }
}

// 데미지를 받을 수 있는 인터페이스 (친구 무기 시스템에서 사용)
public interface IDamageable
{
  void TakeDamage(float damage, Vector3 explosionCenter, float explosionRadius);
  Transform GetTransform();
  bool IsAlive { get; }
}