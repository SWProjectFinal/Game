using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 친구의 무기 시스템과 연동하기 위한 데미지 처리 유틸리티
public static class DamageSystem
{
  [Header("데미지 설정")]
  public static bool showDebugGizmos = true; // 폭발 범위 시각화
  public static Color explosionGizmoColor = Color.red; // 폭발 범위 색상

  // 폭발 범위 내 모든 플레이어 찾기 (친구 무기 시스템에서 사용)
  public static List<IDamageable> GetPlayersInRadius(Vector3 explosionCenter, float explosionRadius)
  {
    List<IDamageable> targets = new List<IDamageable>();

    Debug.Log($"💥 폭발 데미지 검색: 중심 {explosionCenter}, 반경 {explosionRadius}");

    // 씬에서 PlayerHealth 컴포넌트를 가진 모든 오브젝트 찾기
    PlayerHealth[] allPlayers = Object.FindObjectsOfType<PlayerHealth>();

    foreach (PlayerHealth player in allPlayers)
    {
      if (player == null || !player.IsAlive) continue;

      float distance = Vector3.Distance(player.transform.position, explosionCenter);

      if (distance <= explosionRadius)
      {
        targets.Add(player);
        Debug.Log($"  - {player.name} 발견! 거리: {distance:F2}m");
      }
      else
      {
        Debug.Log($"  - {player.name} 범위 밖: {distance:F2}m > {explosionRadius}m");
      }
    }

    Debug.Log($"💥 총 {targets.Count}명의 플레이어가 폭발 범위 내에 있습니다.");
    return targets;
  }

  // 단일 타겟에 데미지 적용 (친구 무기 시스템에서 사용)
  public static void ApplyDamageToTarget(IDamageable target, float damage, Vector3 explosionCenter, float explosionRadius)
  {
    if (target == null || !target.IsAlive) return;

    target.TakeDamage(damage, explosionCenter, explosionRadius);

    Debug.Log($"💥 {target.GetTransform().name}에게 데미지 {damage:F1} 적용!");
  }

  // 폭발 데미지 적용 (친구 무기 시스템에서 사용하는 메인 함수)
  public static void ApplyExplosionDamage(Vector3 explosionCenter, float explosionRadius, float maxDamage, AnimationCurve damageFalloff = null)
  {
    var targets = GetPlayersInRadius(explosionCenter, explosionRadius);

    if (targets.Count == 0)
    {
      Debug.Log("💥 폭발 범위 내에 플레이어가 없습니다.");
      return;
    }

    foreach (var target in targets)
    {
      if (target == null || !target.IsAlive) continue;

      // 거리 기반 데미지 계산
      float distance = Vector3.Distance(target.GetTransform().position, explosionCenter);
      float damageMultiplier = CalculateDamageMultiplier(distance, explosionRadius, damageFalloff);
      float finalDamage = maxDamage * damageMultiplier;

      // 데미지 적용
      target.TakeDamage(finalDamage, explosionCenter, explosionRadius);

      Debug.Log($"💥 {target.GetTransform().name}: 거리 {distance:F2}m, 데미지 {finalDamage:F1} ({damageMultiplier:P0})");
    }

    // 폭발 이펙트 표시 (디버그용)
    if (showDebugGizmos)
    {
      ShowExplosionDebug(explosionCenter, explosionRadius);
    }
  }

  // 거리 기반 데미지 배율 계산
  static float CalculateDamageMultiplier(float distance, float maxRadius, AnimationCurve damageFalloff = null)
  {
    // 중심에서 멀어질수록 데미지 감소
    float distanceRatio = distance / maxRadius;

    if (damageFalloff != null)
    {
      // 커스텀 감쇠 곡선 사용
      return damageFalloff.Evaluate(1f - distanceRatio);
    }
    else
    {
      // 기본 선형 감쇠: 중심(100%) → 가장자리(0%)
      return Mathf.Clamp01(1f - distanceRatio);
    }
  }

  // 레이캐스트 기반 직선 공격 (관통형 무기용)
  public static List<IDamageable> GetPlayersInLine(Vector3 startPos, Vector3 direction, float maxDistance, float lineWidth = 0.5f)
  {
    List<IDamageable> targets = new List<IDamageable>();

    // 레이캐스트로 히트된 모든 콜라이더 가져오기
    RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, maxDistance);

    foreach (var hit in hits)
    {
      if (hit.collider == null) continue;

      PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
      if (playerHealth != null && playerHealth.IsAlive)
      {
        // 라인 공격은 관통되므로 폭발 범위 대신 라인 위치 전달
        Vector3 hitPoint = hit.point;
        targets.Add(playerHealth);

        Debug.Log($"⚡ 라인 공격 히트: {playerHealth.name} at {hitPoint}");
      }
    }

    return targets;
  }

  // 라인 공격 데미지 적용
  public static void ApplyLineDamage(Vector3 startPos, Vector3 direction, float maxDistance, float damage, float lineWidth = 0.5f)
  {
    var targets = GetPlayersInLine(startPos, direction, maxDistance, lineWidth);

    foreach (var target in targets)
    {
      if (target == null || !target.IsAlive) continue;

      // 라인 공격은 거리 감쇠 없이 풀 데미지
      Vector3 hitPoint = GetLineHitPoint(startPos, direction, target.GetTransform().position);
      target.TakeDamage(damage, hitPoint, lineWidth);

      Debug.Log($"⚡ {target.GetTransform().name}에게 라인 데미지 {damage:F1} 적용!");
    }
  }

  // 라인과 타겟의 교차점 계산
  static Vector3 GetLineHitPoint(Vector3 lineStart, Vector3 lineDirection, Vector3 targetPosition)
  {
    // 타겟에서 라인에 가장 가까운 점 계산
    Vector3 toTarget = targetPosition - lineStart;
    float projectionLength = Vector3.Dot(toTarget, lineDirection.normalized);
    Vector3 closestPoint = lineStart + lineDirection.normalized * projectionLength;

    return closestPoint;
  }

  // 폭발 디버그 시각화
  static void ShowExplosionDebug(Vector3 center, float radius)
  {
    // 임시 오브젝트로 폭발 범위 표시
    GameObject debugObj = new GameObject("ExplosionDebug");
    debugObj.transform.position = center;

    // 3초 후 삭제
    Object.Destroy(debugObj, 3f);

    // ✅ 수정: DebugExtensions의 DrawCircle 사용
    DebugExtensions.DrawCircle(center, radius, explosionGizmoColor, 3f);
  }

  // 원형 범위 데미지 (폭발과 유사하지만 즉시 적용)
  public static void ApplyRadialDamage(Vector3 center, float radius, float damage)
  {
    ApplyExplosionDamage(center, radius, damage);
  }

  // 콘 형태 범위 공격 (샷건 스타일)
  public static void ApplyConeDamage(Vector3 origin, Vector3 direction, float range, float coneAngle, float damage)
  {
    PlayerHealth[] allPlayers = Object.FindObjectsOfType<PlayerHealth>();

    foreach (PlayerHealth player in allPlayers)
    {
      if (player == null || !player.IsAlive) continue;

      Vector3 toTarget = player.transform.position - origin;
      float distance = toTarget.magnitude;

      if (distance > range) continue;

      // 각도 체크
      float angle = Vector3.Angle(direction, toTarget);
      if (angle <= coneAngle / 2f)
      {
        // 거리 기반 데미지 감쇠
        float damageMultiplier = 1f - (distance / range);
        float finalDamage = damage * damageMultiplier;

        player.TakeDamage(finalDamage, origin, 1f);

        Debug.Log($"🔫 콘 공격: {player.name} - 거리 {distance:F2}m, 각도 {angle:F1}°, 데미지 {finalDamage:F1}");
      }
    }
  }

  // 힐링 적용 (회복 아이템용)
  public static void ApplyHealing(Vector3 center, float radius, float healAmount)
  {
    var targets = GetPlayersInRadius(center, radius);

    foreach (var target in targets)
    {
      if (target == null || !target.IsAlive) continue;

      PlayerHealth playerHealth = target as PlayerHealth;
      if (playerHealth != null)
      {
        playerHealth.Heal(healAmount);
        Debug.Log($"💚 {playerHealth.name} 체력 회복: {healAmount:F1}");
      }
    }
  }

  // 디버그용 - 모든 플레이어에게 테스트 데미지
  public static void DebugDamageAllPlayers(float damage)
  {
    PlayerHealth[] allPlayers = Object.FindObjectsOfType<PlayerHealth>();

    foreach (PlayerHealth player in allPlayers)
    {
      if (player != null && player.IsAlive)
      {
        player.TakeDamage(damage);
        Debug.Log($"🧪 테스트 데미지: {player.name} - {damage:F1}");
      }
    }
  }
}

// 디버그용 확장 메서드
public static class DebugExtensions
{
  // 원형 그리기 (디버그용)
  public static void DrawCircle(Vector3 center, float radius, Color color, float duration = 0.1f)
  {
    int segments = 36;
    float angleStep = 360f / segments;

    Vector3 prevPoint = center + Vector3.right * radius;

    for (int i = 1; i <= segments; i++)
    {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

      Debug.DrawLine(prevPoint, newPoint, color, duration);
      prevPoint = newPoint;
    }
  }
}