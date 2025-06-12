using UnityEngine;

public class AIAimSystem : MonoBehaviour
{
    [Header("물리 파라미터")]
    public float gravity = 9.8f;
    
    [Header("파워 설정")]
    public float minFirePower = 10f;    // 최소 파워
    public float maxFirePower = 25f;    // 최대 파워
    public float optimalPowerRatio = 0.7f; // 최적 파워 비율 (0.7 = 70%)

    [Header("정확도 설정")]
    [Range(0f, 10f)]
    public float aimErrorDegree = 5f;
    
    [Header("파워 조절 설정")]
    [Range(0f, 5f)]
    public float powerErrorRange = 2f;  // 파워 오차 범위

    // AI 시점에서 어느쪽 보고 있는지
    public bool facingRight = true;

    // 계산된 최적 파워 저장
    private float calculatedOptimalPower = 15f;

    public float CalculateOptimalPower(Vector2 shooterPos, Vector2 targetPos)
    {
        float distance = Vector2.Distance(shooterPos, targetPos);
        float heightDiff = Mathf.Abs(targetPos.y - shooterPos.y);
        
        Debug.Log($"🎯 AI 파워 계산: 거리={distance:F1}, 높이차={heightDiff:F1}");

        // 거리에 따른 기본 파워 계산
        float basePower = Mathf.Lerp(minFirePower, maxFirePower, distance / 20f);
        
        // 높이 차이에 따른 파워 보정
        if (targetPos.y > shooterPos.y) // 타겟이 위에 있으면
        {
            basePower += heightDiff * 0.5f; // 파워 증가
        }
        else // 타겟이 아래에 있으면
        {
            basePower -= heightDiff * 0.2f; // 파워 약간 감소
        }

        // 최적 파워 비율 적용 (너무 세게 쏘지 않도록)
        float optimalPower = basePower * optimalPowerRatio;
        
        // 파워 범위 제한
        optimalPower = Mathf.Clamp(optimalPower, minFirePower, maxFirePower);
        
        // 약간의 랜덤 오차 추가 (AI가 너무 완벽하지 않게)
        float powerError = Random.Range(-powerErrorRange, powerErrorRange);
        float finalPower = optimalPower + powerError;
        
        // 최종 파워 범위 제한
        finalPower = Mathf.Clamp(finalPower, minFirePower, maxFirePower);
        
        calculatedOptimalPower = finalPower;
        
        Debug.Log($"🎯 AI 파워 결정: 기본={basePower:F1} → 최적={optimalPower:F1} → 최종={finalPower:F1}");
        
        return finalPower;
    }

    public float CalculateFireAngle(Vector2 shooterPos, Vector2 targetPos, float firePower)
    {
        // 절대 거리 계산
        float dx = Mathf.Abs(targetPos.x - shooterPos.x);
        float dy = targetPos.y - shooterPos.y;

        Debug.Log($"🎯 AI 각도 계산: dx={dx:F1}, dy={dy:F1}, power={firePower:F1}");

        // 거리가 너무 가까우면 직접 조준
        if (dx < 1f)
        {
            float directAngle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            directAngle = Mathf.Clamp(directAngle, 10f, 80f);
            Debug.Log($"🎯 근거리 직접 조준: {directAngle:F1}도");
            return directAngle;
        }

        // 포물선 물리 계산
        float g = gravity;
        float v = firePower;
        
        // 판별식 계산
        float discriminant = (v * v * v * v) - g * (g * dx * dx + 2 * dy * v * v);
        
        if (discriminant < 0)
        {
            Debug.LogWarning($"🎯 파워 부족 (판별식: {discriminant:F1}) - 파워 증가 후 재계산");
            
            // 파워를 증가시켜 재시도
            float increasedPower = Mathf.Min(v * 1.2f, maxFirePower);
            discriminant = (increasedPower * increasedPower * increasedPower * increasedPower) - 
                          g * (g * dx * dx + 2 * dy * increasedPower * increasedPower);
            
            if (discriminant < 0)
            {
                Debug.LogWarning("🎯 최대 파워로도 도달 불가 - 45도로 발사");
                return 45f;
            }
            
            calculatedOptimalPower = increasedPower;
            v = increasedPower;
        }

        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        
        // 두 가지 각도 계산
        float lowAngleRad = Mathf.Atan((v * v - sqrtDiscriminant) / (g * dx));
        float highAngleRad = Mathf.Atan((v * v + sqrtDiscriminant) / (g * dx));
        
        float lowAngleDeg = lowAngleRad * Mathf.Rad2Deg;
        float highAngleDeg = highAngleRad * Mathf.Rad2Deg;
        
        Debug.Log($"🎯 계산된 각도: 낮은궤도={lowAngleDeg:F1}도, 높은궤도={highAngleDeg:F1}도");

        // 적절한 각도 선택 (보통 낮은 궤도 선호, 하지만 장애물 있을 수 있으니 상황에 따라)
        float selectedAngle = lowAngleDeg;
        
        // 각도가 너무 낮거나 높으면 다른 궤도 선택
        if (lowAngleDeg < 5f || lowAngleDeg > 85f)
        {
            selectedAngle = highAngleDeg;
        }
        
        // 높은 궤도도 비정상이면 중간값
        if (selectedAngle < 5f || selectedAngle > 85f)
        {
            selectedAngle = 45f;
        }

        // 각도 범위 제한
        selectedAngle = Mathf.Clamp(selectedAngle, 10f, 80f);

        // 에임 오차 적용
        float error = Random.Range(-aimErrorDegree, aimErrorDegree);
        float finalAngle = selectedAngle + error;
        
        // 최종 각도 범위 제한
        finalAngle = Mathf.Clamp(finalAngle, 5f, 85f);

        Debug.Log($"🎯 최종 발사 각도: {finalAngle:F1}도 (오차: {error:F1}도)");
        
        return finalAngle;
    }

    // 호환성을 위한 오버로드 (기존 코드와 호환)
    public float CalculateFireAngle(Vector2 shooterPos, Vector2 targetPos)
    {
        float optimalPower = CalculateOptimalPower(shooterPos, targetPos);
        return CalculateFireAngle(shooterPos, targetPos, optimalPower);
    }

    // 계산된 최적 파워 반환
    public float GetCalculatedPower()
    {
        return calculatedOptimalPower;
    }

    // 실제 발사 방향 계산
    public Vector2 GetFireDirection(float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        
        // 방향에 따라 좌우 반전
        if (!facingRight)
        {
            direction.x = -direction.x;
        }
        
        return direction.normalized;
    }

    // 디버그용 - 파워별 도달 가능 거리 계산
    public float GetMaxRange(float power)
    {
        return (power * power) / gravity;
    }
}