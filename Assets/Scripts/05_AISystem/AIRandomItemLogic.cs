using UnityEngine;

public class AIRandomItemLogic : MonoBehaviour
{
    public void OnItemPickedUp(WeaponType itemType)
    {
        Debug.Log($"AI가 랜덤박스에서 {itemType} 획득!");

        // 향후 전략에 따라 사용 무기 리스트 업데이트 가능
    }
}
