using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Image[] slotIcons; // 🔹 Slot 내부의 Icon 이미지들
    public Sprite emptySlotSprite;

    void Start()
    {
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i < WeaponManager.Instance.inventory.Count)
            {
                slotIcons[i].sprite = WeaponManager.Instance.inventory[i].icon;
                slotIcons[i].color = Color.white;
            }
            else
            {
                slotIcons[i].sprite = emptySlotSprite;
                slotIcons[i].color = Color.gray;
            }
        }
    }
}
