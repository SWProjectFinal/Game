using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class WeaponManager : MonoBehaviourPunCallbacks
{
    public static WeaponManager Instance;
    [SerializeField] public SpriteRenderer currentSpriteRenderer;

    [Header("무기 시스템")]
    public List<WeaponData> inventory = new List<WeaponData>();
    public int currentWeaponIndex = 0;
    public int maxSlots = 9;
    public List<WeaponData> allWeapons = new List<WeaponData>();

    [Header("발사 설정")]
    public Transform firePoint;
    public float angleSpeed = 60f;
    private float angle = 0f;
    private bool facingRight = true; // 현재 바라보는 방향 추가

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
        // 현재 바라보는 방향 업데이트
        if (currentSpriteRenderer != null)
        {
            facingRight = !currentSpriteRenderer.flipX;
        }

        // ↑↓ 각도 조절
        float input = Input.GetKey(KeyCode.UpArrow) ? 1 :
                      Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

        angle += input * angleSpeed * Time.deltaTime;
        angle = Mathf.Clamp(angle, -80f, 80f);

        // firePoint 회전 적용 (방향에 따라 각도 조정)
        if (firePoint != null)
        {
            float finalAngle = facingRight ? angle : 180f - angle;
            firePoint.localEulerAngles = new Vector3(0, 0, finalAngle);
        }

        // 무기 선택 (1~9번)
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

        // 차징 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            chargePower = minPower;
        }

        // 차징 유지
        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            chargePower += chargeSpeed * Time.deltaTime;
            chargePower = Mathf.Clamp(chargePower, minPower, maxPower);
        }

        // 발사
        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            isCharging = false;

            // firePoint의 회전된 방향을 그대로 사용
            Vector2 dir = firePoint != null ? firePoint.right.normalized : 
                         (facingRight ? Vector2.right : Vector2.left);

            FireWeapon(dir, chargePower);
        }
    }

    public void FireWeapon(Vector2 dir, float power)
    {
        WeaponData weapon = inventory[currentWeaponIndex];
        RPC_Fire((int)weapon.type, dir, power);
    }

    void RPC_Fire(int weaponTypeInt, Vector2 dir, float power)
    {
        Debug.Log($"발사 시도됨 - 방향: {dir}, 파워: {power}, 오른쪽 향함: {facingRight}");

        WeaponType type = (WeaponType)weaponTypeInt;

        // 발사 위치 계산 (firePoint의 right 방향으로 약간 앞쪽)
        Vector3 spawnOffset = firePoint != null ? firePoint.right * 0.5f : 
                             (facingRight ? Vector3.right * 0.5f : Vector3.left * 0.5f);
        Vector3 spawnPos = firePoint != null ? firePoint.position + spawnOffset : 
                          transform.position + spawnOffset;

        // 발사체 회전값 계산
        Quaternion spawnRotation = firePoint != null ? firePoint.rotation : 
                                  Quaternion.LookRotation(Vector3.forward, Vector3.up);

        if (type == WeaponType.BasicGun && basicGunSO != null)
        {
            GameObject proj = Instantiate(basicGunSO.projectilePrefab, spawnPos, spawnRotation);
            var standardProj = proj.GetComponent<StandardProjectile>();
            if (standardProj != null)
            {
                standardProj.weaponData = basicGunSO;
                standardProj.power = power;
                standardProj.shootDirection = dir;
            }
            return;
        }

        if (type == WeaponType.Blackhole && blackholeSO != null)
        {
            GameObject proj = Instantiate(blackholeSO.projectilePrefab, spawnPos, spawnRotation);
            var blackholeProj = proj.GetComponent<BlackholeProjectile_SO>();
            if (blackholeProj != null)
            {
                blackholeProj.weaponData = blackholeSO;
                blackholeProj.power = power;
                blackholeProj.shootDirection = dir;
            }
            return;
        }

        if (type == WeaponType.RPG && rpgSO != null)
        {
            GameObject proj = Instantiate(rpgSO.projectilePrefab, spawnPos, spawnRotation);
            var rpgProj = proj.GetComponent<RPGProjectile_SO>();
            if (rpgProj != null)
            {
                rpgProj.weaponData = rpgSO;
                rpgProj.power = power;
                rpgProj.shootDirection = dir;
            }
            return;
        }

        // 기타 무기
        WeaponData weapon = GetWeaponByType(type);
        if (weapon.projectilePrefab == null)
        {
            Debug.LogError("❌ projectilePrefab이 null입니다!");
            return;
        }

        GameObject bullet = Instantiate(weapon.projectilePrefab, spawnPos, spawnRotation);
        SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Projectile";
            sr.sortingOrder = 5;
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

    public void SetFirePoint(Transform newFirePoint)
    {
        firePoint = newFirePoint;
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (inventory.Count < maxSlots)
        {
            inventory.Add(weapon);
            Debug.Log($"무기 추가됨: {weapon.displayName}");
        }
        else
        {
            Debug.Log("무기 슬롯이 가득 찼습니다.");
        }
    }
}