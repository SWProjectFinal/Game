using UnityEngine;

public class FallDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // "Water" 레이어에 닿았을 때만 낙사 처리
        if (collision.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            Debug.Log("🌊 물에 빠짐! 낙사!");
            Destroy(gameObject); // 리스폰 없이 제거만
        }
    }
}
