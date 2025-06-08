
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
    public WeaponData_SO blackholeSO;
    public WeaponData_SO rpgSO;

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Photon 서버 연결 성공!");
    }

    void Awake()
    {
        if (Instance == null) Instance = this;

        //PhotonNetwork.Disconnect();
        //PhotonNetwork.OfflineMode = true;

        Debug.Log("🔥 WeaponManager Awake");
    }

    void Start()
    {
        Debug.Log("🚀 WeaponManager Start");

        inventory.Clear();

        if (basicGunSO != null)
        {
            inventory.Add(new WeaponData
            {
                type = WeaponType.BasicGun,
                displayName = basicGunSO.weaponName,
                isInstantUse = basicGunSO.isInstantUse,
                icon = basicGunSO.icon,
                projectilePrefab = basicGunSO.projectilePrefab,
                damage = basicGunSO.damage
            });
        }

        if (blackholeSO != null)
        {
            inventory.Add(new WeaponData
            {
                type = WeaponType.Blackhole,
                displayName = blackholeSO.weaponName,
                isInstantUse = blackholeSO.isInstantUse,
                icon = blackholeSO.icon,
                projectilePrefab = blackholeSO.projectilePrefab,
                damage = blackholeSO.damage
            });
        }

        if (rpgSO != null)
        {
            inventory.Add(new WeaponData
            {
                type = WeaponType.RPG,
                displayName = rpgSO.weaponName,
                isInstantUse = rpgSO.isInstantUse,
                icon = rpgSO.icon,
                projectilePrefab = rpgSO.projectilePrefab,
                damage = rpgSO.damage
            });
        }

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

        WeaponType type = (WeaponType)weaponTypeInt;

        if (type == WeaponType.BasicGun && basicGunSO != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject proj = Instantiate(basicGunSO.projectilePrefab, spawnPos, firePoint.rotation);

            var standardProj = proj.GetComponent<StandardProjectile>();
            if (standardProj != null)
            {
                standardProj.weaponData = basicGunSO;
                standardProj.power = power;
            }
            return;
        }

        if (type == WeaponType.Blackhole && blackholeSO != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject proj = Instantiate(blackholeSO.projectilePrefab, spawnPos, firePoint.rotation);

            var blackholeProj = proj.GetComponent<BlackholeProjectile_SO>();
            if (blackholeProj != null)
            {
                blackholeProj.weaponData = blackholeSO;
                blackholeProj.power = power;
            }
            return;
        }

        if (type == WeaponType.RPG && rpgSO != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject proj = Instantiate(rpgSO.projectilePrefab, spawnPos, firePoint.rotation);

            var rpgProj = proj.GetComponent<RPGProjectile_SO>();
            if (rpgProj != null)
            {
                rpgProj.weaponData = rpgSO;
                rpgProj.power = power;
            }
            return;
        }

        WeaponData weapon = GetWeaponByType(type);

        if (weapon.projectilePrefab == null)
        {
            Debug.LogError("❌ projectilePrefab이 null입니다!");
            return;
        }

        Vector3 spawn = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(weapon.projectilePrefab, spawn, Quaternion.identity);
        SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Projectile";
            sr.sortingOrder = 5;  // 숫자가 높을수록 위에 표시됨
        }
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
}

