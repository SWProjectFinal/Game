using UnityEngine;

public class AIWeaponSelector : MonoBehaviour
{
    public int SelectWeaponIndex()
    {
        // 인벤토리 정보 WeaponManager에서 가져오기
        var inventory = WeaponManager.Instance.inventory;

        // 1. 특수무기 우선 (RPG > Blackhole > BasicGun)
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].type == WeaponType.RPG && inventory[i].ammoCount > 0)
                return i;
        }

        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].type == WeaponType.Blackhole && inventory[i].ammoCount > 0)
                return i;
        }

        // 2. 그 외 기본무기 (탄약 무한)
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].type == WeaponType.BasicGun)
                return i;
        }

        // 3. 예외처리 (혹시 아무것도 없을 때)
        return 0;
    }
}
