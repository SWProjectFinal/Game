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

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Photon 서버 연결 성공!");
    }

    public List<WeaponData> allWeapons = new List<WeaponData>();

    void Awake()
    {
        PhotonNetwork.Disconnect();
        PhotonNetwork.OfflineMode = true;

        Debug.Log("🔥 WeaponManager Awake");

        LoadWeapons();
    }

    void Start()
    {
        Debug.Log("🚀 WeaponManager Start");

        WeaponData basicGun = GetWeaponByType(WeaponType.BasicGun);

        if (basicGun == null)
        {
            Debug.LogError("❌ 기본 무기를 못 찾았어!");
        }
        else
        {
            Debug.Log($"✅ 기본 무기 로딩 성공: {basicGun.displayName}");

            if (basicGun.projectilePrefab == null)
                Debug.LogError("❌ 하지만 총알 프리팹은 null임!");
        }

        AddWeapon(basicGun);
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 dir = Vector2.right;
            float power = 10f;
            FireWeapon(dir, power);
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

        // 오프라인 테스트용 직접 호출
        RPC_Fire((int)weapon.type, dir, power);
    }

    // PhotonView 없이 바로 호출 가능 (오프라인 테스트용)
    void RPC_Fire(int weaponTypeInt, Vector2 dir, float power)
    {
        Debug.Log("발사 시도됨");
        WeaponType type = (WeaponType)weaponTypeInt;
        WeaponData weapon = GetWeaponByType(type);

        if (weapon.projectilePrefab == null)
        {
            Debug.LogError("❌ projectilePrefab이 null입니다!");
            return;
        }

        GameObject proj = Instantiate(weapon.projectilePrefab, transform.position, Quaternion.identity);
        proj.GetComponent<Rigidbody2D>()?.AddForce(dir * power, ForceMode2D.Impulse);
    }

    void UseInstantItem(WeaponData item)
    {
        if (item.type == WeaponType.Heal)
        {
            Debug.Log("체력 회복!");
        }
    }

    public WeaponData GetWeaponByType(WeaponType type)
    {
        WeaponData w = allWeapons.FirstOrDefault(w => w.type == type);
        if (w == null) Debug.LogError($"❌ 무기 {type} 못 찾음!");
        return w;
    }

    void LoadWeapons()
    {
        var bullet = Resources.Load<GameObject>("Prefabs/Bullet");
        if (bullet == null)
        {
            Debug.LogError("❌ Bullet 프리팹 로딩 실패! 경로 확인 필요");
        }
        else
        {
            Debug.Log("✅ Bullet 프리팹 로딩 성공");
        }

        allWeapons.Add(new WeaponData
        {
            type = WeaponType.BasicGun,
            displayName = "기본 무기",
            damage = 30,
            isInstantUse = false,
            icon = null,
            projectilePrefab = bullet
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
    }
}
