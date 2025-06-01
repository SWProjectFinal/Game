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
    public Transform firePoint; // 🔥 총알 발사 위치
    public float angleSpeed = 60f; // ↑↓ 키 회전 속도
    private float angle = 0f;

    [Header("차징 설정")]
    private bool isCharging = false;
    private float chargePower = 0f;
    public float minPower = 5f;
    public float maxPower = 20f;
    public float chargeSpeed = 20f;

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Photon 서버 연결 성공!");
    }

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null) Instance = this;

        // 오프라인 모드 설정
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
        // 🔼🔽 방향키로 발사 각도 조절
        float input = Input.GetKey(KeyCode.UpArrow) ? 1 :
                      Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

        angle += input * angleSpeed * Time.deltaTime;
        angle = Mathf.Clamp(angle, -80f, 80f);

        if (firePoint != null)
            firePoint.rotation = Quaternion.Euler(0, 0, angle);

        // 숫자 키(1~9)로 무기 선택 or 즉시 사용 아이템 사용
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

        // 스페이스바 누르면 차징 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            chargePower = minPower;
        }

        // 누르고 있으면 파워 증가
        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            chargePower += chargeSpeed * Time.deltaTime;
            chargePower = Mathf.Clamp(chargePower, minPower, maxPower);
        }

        // 떼면 발사!
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
            // TODO: 인벤토리 UI 갱신
        }
    }

    public void FireWeapon(Vector2 dir, float power)
    {
        WeaponData weapon = inventory[currentWeaponIndex];

        // 오프라인 테스트용 직접 호출
        RPC_Fire((int)weapon.type, dir, power);
    }

    // PhotonView 없이 직접 호출 가능 (오프라인 테스트용)
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

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        GameObject proj = Instantiate(weapon.projectilePrefab, spawnPos, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
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
            // TODO: 실제 플레이어 체력 회복 구현
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
            projectilePrefab = null // 나중에 연결
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
