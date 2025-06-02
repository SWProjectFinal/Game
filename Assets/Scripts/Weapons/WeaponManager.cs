using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class WeaponManager : MonoBehaviourPunCallbacks
{
    public static WeaponManager Instance;

    [Header("무기 시스템")]
    public List<WeaponData> inventory = new List<WeaponData>();
    public int currentWeaponIndex = 0;
    public int maxSlots = 9;
    public List<WeaponData> allWeapons = new List<WeaponData>();

    [Header("발사 설정")]
    public Transform firePoint;
    public float angleSpeed = 60f;
    private float angle = 0f;

    [Header("차징 설정")]
    private bool isCharging = false;
    private float chargePower = 0f;
    public float minPower = 5f;
    public float maxPower = 20f;
    public float chargeSpeed = 20f;

    [Header("Scriptable 무기")]
    public WeaponData_SO basicGunSO;

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Photon 서버 연결 성공!");
    }

    void Awake()
    {
        if (Instance == null) Instance = this;

        PhotonNetwork.Disconnect();
        PhotonNetwork.OfflineMode = true;

        Debug.Log("🔥 WeaponManager Awake");

        LoadWeapons();
    }

    void Start()
    {
        Debug.Log("🚀 WeaponManager Start");

        WeaponData basicGun = GetWeaponByType(WeaponType.BasicGun);
        WeaponData blackhole = GetWeaponByType(WeaponType.Blackhole);
        WeaponData rpg = GetWeaponByType(WeaponType.RPG);

        if (basicGun == null || blackhole == null)
        {
            Debug.LogError("❌ 무기를 못 찾았어!");
            return;
        }

        Debug.Log($"✅ 기본 무기 로딩 성공: {basicGun.displayName}");
        if (basicGun.projectilePrefab == null)
            Debug.LogError("❌ 하지만 기본 무기 총알 프리팹은 null임!");

        AddWeapon(basicGun);     // Slot 1
        AddWeapon(blackhole);    // Slot 2
        AddWeapon(rpg);          // Slot 3

        FindObjectOfType<InventoryManager>().UpdateInventoryUI();
        InventoryManager.Instance.SetSelectedSlot(0);
    }

    void Update()
    {
        float input = Input.GetKey(KeyCode.UpArrow) ? 1 :
                      Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

        angle += input * angleSpeed * Time.deltaTime;
        angle = Mathf.Clamp(angle, -80f, 80f);

        if (firePoint != null)
            firePoint.rotation = Quaternion.Euler(0, 0, angle);

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
                    InventoryManager.Instance.SetSelectedSlot(currentWeaponIndex);
                }
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            chargePower = minPower;
        }

        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            chargePower += chargeSpeed * Time.deltaTime;
            chargePower = Mathf.Clamp(chargePower, minPower, maxPower);
        }

        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            isCharging = false;

            Vector2 dir = firePoint != null ? firePoint.right.normalized : Vector2.right;
            FireWeapon(dir, chargePower);
        }
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (inventory.Count < maxSlots)
        {
            inventory.Add(weapon);
        }
    }

    public void FireWeapon(Vector2 dir, float power)
    {
        WeaponData weapon = inventory[currentWeaponIndex];
        RPC_Fire((int)weapon.type, dir, power);
    }

    void RPC_Fire(int weaponTypeInt, Vector2 dir, float power)
    {
        Debug.Log("발사 시도됨");
        Debug.Log("🔫 BasicGun 발사 준비됨");
        Debug.Log("🚀 프리팹 인스턴스 생성 완료");
        Debug.Log("✅ weaponData 주입!");

        WeaponType type = (WeaponType)weaponTypeInt;

        // ✅ ScriptableObject 기반 발사 (BasicGun만 적용 중)
        if (type == WeaponType.BasicGun && basicGunSO != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

            GameObject proj = Instantiate(basicGunSO.projectilePrefab, spawnPos, firePoint.rotation);

            var standardProj = proj.GetComponent<StandardProjectile>();
            if (standardProj != null)
            {
                standardProj.weaponData = basicGunSO;
                standardProj.power = power; // ✅ 여기!
            }
            else
            {
                Debug.LogWarning("⚠ StandardProjectile 스크립트가 projectile에 안 붙어 있음!");
            }

            return;
        }

        // ✅ 기존 방식 유지
        WeaponData weapon = GetWeaponByType(type);

        if (weapon.projectilePrefab == null)
        {
            Debug.LogError("❌ projectilePrefab이 null입니다!");
            return;
        }

        Vector3 spawn = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(weapon.projectilePrefab, spawn, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(dir * power, ForceMode2D.Impulse);
        }
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
        var blackholeProj = Resources.Load<GameObject>("Prefabs/BlackholeProjectile");
        var rpgProj = Resources.Load<GameObject>("Prefabs/RPGProjectile");

        var iconBasic = Resources.Load<Sprite>("Icons/03");
        var iconBlackhole = Resources.Load<Sprite>("Icons/machine_gun_blue");
        var iconRPG = Resources.Load<Sprite>("Icons/rocket _launcher_blue");

        allWeapons.Add(new WeaponData
        {
            type = WeaponType.BasicGun,
            displayName = "기본 무기",
            damage = 30,
            isInstantUse = false,
            icon = iconBasic,
            projectilePrefab = bullet
        });

        allWeapons.Add(new WeaponData
        {
            type = WeaponType.RPG,
            displayName = "RPG",
            damage = 80,
            isInstantUse = false,
            icon = iconRPG,
            projectilePrefab = rpgProj
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

        allWeapons.Add(new WeaponData
        {
            type = WeaponType.Blackhole,
            displayName = "블랙홀",
            damage = 0,
            isInstantUse = false,
            icon = iconBlackhole,
            projectilePrefab = blackholeProj
        });
    }
}
