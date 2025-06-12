using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public Text[] slotCounts;

    [Header("아이템 아이콘 슬롯 (Icon Image 연결)")]
    public Image[] slotIcons; // 슬롯 안의 Icon 이미지들을 배열로 받음
    public Image[] slotBackgrounds; // 슬롯 자체의 배경 (테두리 색 강조용)

    [Header("슬롯 강조 색 설정")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private int selectedSlotIndex = -1; // 선택된 슬롯 인덱스 (0~8)

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddItem(Sprite icon)
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i].sprite == null)
            {
                slotIcons[i].sprite = icon;
                slotIcons[i].enabled = true;
                return;
            }
        }
        Debug.LogWarning("인벤토리가 꽉 찼습니다!");
    }

    public void UpdateInventoryUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i < WeaponManager.Instance.inventory.Count)
            {
                var weapon = WeaponManager.Instance.inventory[i];
                Sprite icon = weapon.icon;

                if (icon != null)
                {
                    slotIcons[i].sprite = icon;
                    slotIcons[i].enabled = true;

                    slotIcons[i].preserveAspect = true;
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].enabled = false;
                }

                // 🔫 탄 수 UI 추가
                if (slotCounts != null && i < slotCounts.Length && slotCounts[i] != null)
                {
                    slotCounts[i].text = weapon.isInfiniteAmmo ? "∞" : weapon.ammoCount.ToString();
                    slotCounts[i].enabled = true;
                }
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;

                if (slotCounts != null && i < slotCounts.Length && slotCounts[i] != null)
                {
                    slotCounts[i].text = "";
                    slotCounts[i].enabled = false;
                }
            }
        }


        // 슬롯 테두리 색상 업데이트
        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            if (slotBackgrounds[i] != null)
            {
                slotBackgrounds[i].color = (i == selectedSlotIndex) ? selectedColor : normalColor;
            }
        }
    }

    public void SetSelectedSlot(int index)
    {
        selectedSlotIndex = index;
        UpdateInventoryUI();
    }

}