//무기 전환 & 발사 시스템

using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class WeaponManager : MonoBehaviourPunCallbacks
{
    public static WeaponManager Instance;
    public List<WeaponData> inventory = new List<WeaponData>();
    public int currentWeaponIndex = 0;
    public int maxSlots = 9;

    // 무기 등록용 리스트
    public List<WeaponData> allWeapons = new List<WeaponData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadWeapons();  // 무기 초기 등록
    }

    void Start()
    {
        AddWeapon(GetWeaponByType(WeaponType.BasicGun));
    }

    void Update()
    {
        for (int i = 1; i <= inventory.Count; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                if (inventory[i - 1].isInstantUse)
                {
                    UseInstantItem(inventory[i - 1]);
                    inventory.RemoveAt(i - 1);
                }
                else
                {
                    currentWeaponIndex = i - 1;
                }
                break;
            }
        }
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (inventory.Count < maxSlots)
        {
            inventory.Add(weapon);
            // TODO: UI 갱신
        }
    }

    public void FireWeapon(Vector2 dir, float power)
    {
        WeaponData weapon = inventory[currentWeaponIndex];
        photonView.RPC("RPC_Fire", RpcTarget.All, (int)weapon.type, dir, power);
    }

    [PunRPC]
    void RPC_Fire(int weaponTypeInt, Vector2 dir, float power)
    {
        WeaponType type = (WeaponType)weaponTypeInt;
        WeaponData weapon = GetWeaponByType(type);
        GameObject proj = Instantiate(weapon.projectilePrefab, transform.position, Quaternion.identity);
        proj.GetComponent<Rigidbody2D>()?.AddForce(dir * power, ForceMode2D.Impulse);
    }

    void UseInstantItem(WeaponData item)
    {
        if (item.type == WeaponType.Heal)
        {
            // 체력 회복 처리
            Debug.Log("체력 회복!");
        }
    }

    public WeaponData GetWeaponByType(WeaponType type)
    {
        return allWeapons.FirstOrDefault(w => w.type == type);
    }

    void LoadWeapons()
    {
        // Inspector에서 연결 가능 / 예시로는 빈 값 사용
        allWeapons.Add(new WeaponData
        {
            type = WeaponType.BasicGun,
            displayName = "기본 무기",
            damage = 30,
            isInstantUse = false,
            icon = null,
            projectilePrefab = null
        });

        allWeapons.Add(new WeaponData
        {
            type = WeaponType.RPG,
            displayName = "RPG",
            damage = 80,
            isInstantUse = false,
            icon = null,
            projectilePrefab = null
        });

        allWeapons.Add(new WeaponData
        {
            type = WeaponType.Heal,
            displayName = "회복 아이템",
            damage = 0,
            isInstantUse = true,
            icon = null,
            projectilePrefab = null
        });

        // 블랙홀, 화염병 등도 여기에 추가 가능
    }
}
