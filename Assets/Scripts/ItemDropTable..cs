using UnityEngine;
using System.Collections.Generic;

// 아이템별 드랍 확률 관리 시스템
[CreateAssetMenu(fileName = "ItemDropTable", menuName = "Game/Item Drop Table")]
public class ItemDropTable : ScriptableObject
{
    [Header("아이템 드랍 확률 설정")]
    public List<ItemDropData> itemDrops = new List<ItemDropData>();

    [Header("총 확률 검증")]
    [SerializeField, Range(0f, 100f)]
    private float totalPercentage = 100f; // 에디터에서 확인용

    // 아이템 드랍 데이터 구조
    [System.Serializable]
    public class ItemDropData
    {
        [Header("아이템 정보")]
        public string itemName;                    // 아이템 이름 (표시용)
        public WeaponType weaponType;              // 무기 타입 (친구 코드와 동일)

        [Header("확률 설정")]
        [Range(0f, 100f)]
        public float dropChance = 10f;             // 드랍 확률 (%)

        [Header("밸런스 설정")]
        public ItemRarity rarity = ItemRarity.Common;     // 아이템 희귀도
        public string description;                 // 아이템 설명

        [Header("시각적 정보")]
        public Color rarityColor = Color.white;    // 희귀도별 색상
    }

    // 아이템 희귀도
    public enum ItemRarity
    {
        Common,     // 일반 (60-70%)
        Uncommon,   // 고급 (20-30%)
        Rare,       // 희귀 (5-15%)
        Epic,       // 전설 (1-5%)
        Legendary   // 신화 (0.1-1%)
    }

    void OnValidate()
    {
        // 에디터에서 총 확률 계산
        CalculateTotalPercentage();
    }

    // 총 확률 계산 (100%가 되는지 확인)
    void CalculateTotalPercentage()
    {
        totalPercentage = 0f;
        foreach (var item in itemDrops)
        {
            totalPercentage += item.dropChance;
        }
    }

    // 랜덤 아이템 선택
    public WeaponType GetRandomItem()
    {
        // 총 확률이 0이면 기본 아이템 반환 (하지만 기본무기는 제외)
        if (totalPercentage <= 0f || itemDrops.Count == 0)
        {
            Debug.LogWarning("아이템 드랍 테이블이 비어있거나 총 확률이 0입니다!");
            return WeaponType.RPG; // 기본무기 대신 RPG 반환
        }

        // 0~총확률 사이의 랜덤값 생성
        float randomValue = Random.Range(0f, totalPercentage);
        float currentSum = 0f;

        // 누적 확률로 아이템 선택
        foreach (var item in itemDrops)
        {
            // ⚠️ 안전장치: 기본무기는 스킵
            if (item.weaponType == WeaponType.BasicGun)
            {
                Debug.LogWarning("기본무기가 드랍 테이블에 있습니다! 스킵합니다.");
                continue;
            }

            currentSum += item.dropChance;
            if (randomValue <= currentSum)
            {
                Debug.Log($"아이템 선택: {item.itemName} ({item.dropChance}% 확률, {randomValue:F2}/{totalPercentage:F2})");
                return item.weaponType;
            }
        }

        // 혹시 모를 경우를 대비해 마지막 아이템 반환 (기본무기 아닌 것)
        var lastItem = itemDrops[itemDrops.Count - 1];
        if (lastItem.weaponType != WeaponType.BasicGun)
        {
            Debug.Log($"마지막 아이템 선택: {lastItem.itemName}");
            return lastItem.weaponType;
        }

        // 정말 마지막 안전장치
        return WeaponType.RPG;
    }

    // 특정 아이템의 정보 가져오기
    public ItemDropData GetItemData(WeaponType weaponType)
    {
        foreach (var item in itemDrops)
        {
            if (item.weaponType == weaponType)
            {
                return item;
            }
        }
        return null;
    }

    // 희귀도별 아이템 목록 가져오기
    public List<ItemDropData> GetItemsByRarity(ItemRarity rarity)
    {
        List<ItemDropData> result = new List<ItemDropData>();

        foreach (var item in itemDrops)
        {
            if (item.rarity == rarity)
            {
                result.Add(item);
            }
        }

        return result;
    }

    // 기본 아이템 드랍 테이블 생성 (컨텍스트 메뉴)
    [ContextMenu("기본 드랍 테이블 생성")]
    void CreateDefaultDropTable()
    {
        itemDrops.Clear();

        itemDrops.Add(new ItemDropData
        {
            itemName = "블랙홀",
            weaponType = WeaponType.Blackhole,
            dropChance = 50f,
            rarity = ItemRarity.Uncommon,
            rarityColor = Color.blue,
            description = "강력한 흡입력을 가진 블랙홀"
        });

        itemDrops.Add(new ItemDropData
        {
            itemName = "RPG",
            weaponType = WeaponType.RPG,
            dropChance = 50f,
            rarity = ItemRarity.Common,
            rarityColor = Color.green,
            description = "폭발력이 강한 로켓 런처"
        });

        Debug.Log("✅ 블랙홀 & RPG만 포함된 드랍 테이블 생성 완료");
        CalculateTotalPercentage();
    }

    // 확률 정규화 (총 100%로 맞추기)
    [ContextMenu("확률 정규화 (100%로 맞추기)")]
    void NormalizeDropChances()
    {
        CalculateTotalPercentage();

        if (totalPercentage <= 0f || itemDrops.Count == 0)
        {
            Debug.LogWarning("정규화할 데이터가 없습니다!");
            return;
        }

        // 각 아이템의 확률을 100%에 맞춰 조정
        foreach (var item in itemDrops)
        {
            item.dropChance = (item.dropChance / totalPercentage) * 100f;
        }

        Debug.Log("확률 정규화 완료! 총 100%로 조정됨");
        CalculateTotalPercentage();
    }
}