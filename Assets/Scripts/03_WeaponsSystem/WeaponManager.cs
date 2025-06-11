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
    private bool facingRight = true;

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
        inventory.Clear();
        allWeapons.Clear();

        if (basicGunSO != null)
        {
            var basicWeapon = new WeaponData
            {
                type = WeaponType.BasicGun,
                displayName = basicGunSO.weaponName,
                isInstantUse = basicGunSO.isInstantUse,
                icon = basicGunSO.icon,
                projectilePrefab = basicGunSO.projectilePrefab,
                damage = basicGunSO.damage,
                ammoCount = 999,
                isInfiniteAmmo = true
            };

            inventory.Add(basicWeapon);
            allWeapons.Add(basicWeapon);
        }

        if (blackholeSO != null)
        {
            var blackholeWeapon = new WeaponData
            {
                type = WeaponType.Blackhole,
                displayName = blackholeSO.weaponName,
                isInstantUse = blackholeSO.isInstantUse,
                icon = blackholeSO.icon,
                projectilePrefab = blackholeSO.projectilePrefab,
                damage = blackholeSO.damage,
                ammoCount = 50,
                isInfiniteAmmo = false
            };

            inventory.Add(blackholeWeapon);
            allWeapons.Add(blackholeWeapon);
        }

        if (rpgSO != null)
        {
            var rpgWeapon = new WeaponData
            {
                type = WeaponType.RPG,
                displayName = rpgSO.weaponName,
                isInstantUse = rpgSO.isInstantUse,
                icon = rpgSO.icon,
                projectilePrefab = rpgSO.projectilePrefab,
                damage = rpgSO.damage,
                ammoCount = 50,
                isInfiniteAmmo = false
            };

            inventory.Add(rpgWeapon);
            allWeapons.Add(rpgWeapon);
        }

        FindObjectOfType<InventoryManager>().UpdateInventoryUI();
        InventoryManager.Instance.SetSelectedSlot(0);
    }

    void Update()
    {
        if (currentSpriteRenderer != null)
        {
            facingRight = !currentSpriteRenderer.flipX;
        }

        float input = Input.GetKey(KeyCode.UpArrow) ? 1 :
                      Input.GetKey(KeyCode.DownArrow) ? -1 : 0;

        angle += input * angleSpeed * Time.deltaTime;
        angle = Mathf.Clamp(angle, -80f, 80f);

        if (firePoint != null)
        {
            float finalAngle = facingRight ? angle : 180f - angle;
            firePoint.localEulerAngles = new Vector3(0, 0, finalAngle);
        }

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

            Vector2 dir = firePoint != null ? firePoint.right.normalized :
                         (facingRight ? Vector2.right : Vector2.left);

            FireWeapon(dir, chargePower);
        }
    }

    public void FireWeapon(Vector2 dir, float power)
    {
        if (inventory.Count == 0 || currentWeaponIndex >= inventory.Count)
            return;

        WeaponData weapon = inventory[currentWeaponIndex];

        // ✅ 수정: 현재 플레이어의 위치와 방향도 함께 전송
        Vector3 shooterPosition = transform.position;
        bool shooterFacingRight = facingRight;

        // 현재 플레이어의 FirePoint 정보 가져오기
        Transform currentFirePoint = GetCurrentPlayerFirePoint();
        Vector3 firePosition = currentFirePoint != null ? currentFirePoint.position : shooterPosition;
        Vector3 fireDirection = currentFirePoint != null ? currentFirePoint.right : (shooterFacingRight ? Vector3.right : Vector3.left);

        // ✅ 플레이어 정보와 함께 RPC 전송
        string shooterName = PhotonNetwork.LocalPlayer.NickName;
        photonView.RPC("RPC_Fire", RpcTarget.All,
            (int)weapon.type,
            dir.x, dir.y,
            power,
            shooterName,  // 발사자 이름
            firePosition.x, firePosition.y, firePosition.z,  // 발사 위치
            fireDirection.x, fireDirection.y, fireDirection.z  // 발사 방향
        );

        // 탄약 소모는 로컬에서만 처리
        if (!weapon.isInfiniteAmmo)
        {
            weapon.ammoCount--;
            InventoryManager.Instance.UpdateInventoryUI();

            if (weapon.ammoCount <= 0)
            {
                inventory.RemoveAt(currentWeaponIndex);
                InventoryManager.Instance.UpdateInventoryUI();
                currentWeaponIndex = Mathf.Clamp(currentWeaponIndex - 1, 0, inventory.Count - 1);
                InventoryManager.Instance.SetSelectedSlot(currentWeaponIndex);
            }
        }
    }

    // ✅ 현재 플레이어의 FirePoint 가져오기
    Transform GetCurrentPlayerFirePoint()
    {
        // 현재 턴인 플레이어의 FirePoint 찾기
        if (TurnManager.Instance != null)
        {
            var currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                // PlayerSpawner에서 해당 플레이어의 GameObject 찾기
                GameObject playerObj = PlayerSpawner.Instance.GetPlayerObject(currentPlayer.NickName);
                if (playerObj != null)
                {
                    Transform firePointTransform = playerObj.GetComponentsInChildren<Transform>(true)
                                                            .FirstOrDefault(t => t.name == "FirePoint");
                    return firePointTransform;
                }
            }
        }
        return firePoint; // 백업으로 기존 firePoint 사용
    }


    // ✅ 수정: RPC 메서드에서 발사자 정보 활용
    [PunRPC]
    void RPC_Fire(int weaponTypeInt, float dirX, float dirY, float power,
                  string shooterName,
                  float firePosX, float firePosY, float firePosZ,
                  float fireDirX, float fireDirY, float fireDirZ)
    {
        Vector2 dir = new Vector2(dirX, dirY);
        Vector3 firePosition = new Vector3(firePosX, firePosY, firePosZ);
        Vector3 fireDirection = new Vector3(fireDirX, fireDirY, fireDirZ);

        Debug.Log($"🔥 RPC_Fire: {shooterName}이 {firePosition}에서 발사!");

        WeaponType type = (WeaponType)weaponTypeInt;

        // ✅ 전송받은 위치 정보 사용 (고정된 firePoint 대신)
        Vector3 spawnOffset = fireDirection.normalized * 0.5f;
        Vector3 spawnPos = firePosition + spawnOffset;
        Quaternion spawnRotation = Quaternion.LookRotation(Vector3.forward, fireDirection);

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

        WeaponData weapon = GetWeaponByType(type);
        if (weapon?.projectilePrefab == null)
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
        WeaponData template = allWeapons.FirstOrDefault(w => w.type == type);
        if (template == null)
        {
            Debug.LogError($"❌ 무기 {type} 못 찾음!");
            return null;
        }

        // ✅ 새 인스턴스로 복제해서 반환
        return new WeaponData
        {
            type = template.type,
            displayName = template.displayName,
            icon = template.icon,
            projectilePrefab = template.projectilePrefab,
            damage = template.damage,
            isInstantUse = template.isInstantUse,
            ammoCount = template.ammoCount,
            isInfiniteAmmo = template.isInfiniteAmmo
        };
    }

    public void SetFirePoint(Transform newFirePoint)
    {
        firePoint = newFirePoint;
    }

    public void AddWeapon(WeaponData newWeapon)
    {
        var existing = inventory.FirstOrDefault(w => w.type == newWeapon.type && !w.isInfiniteAmmo);

        if (existing != null)
        {
            existing.ammoCount += 1;
            Debug.Log($"{existing.displayName} 탄약 +1 → 총 {existing.ammoCount}발");
        }
        else if (inventory.Count < maxSlots)
        {
            inventory.Add(newWeapon);
            Debug.Log($"무기 추가됨: {newWeapon.displayName}");
        }
        else
        {
            Debug.Log("무기 슬롯이 가득 찼습니다.");
        }

        InventoryManager.Instance.UpdateInventoryUI();
    }

    // WeaponManager.cs에 추가할 RPC 메서드들

    /*[PunRPC]
    void SyncBlackholeDestruction(int centerX, int centerY, int innerRadius, int outerRadius, int blackholeId, string creatorName)
    {
        Debug.Log($"🌌 WeaponManager RPC 수신: 블랙홀 #{blackholeId} 지형파괴 (생성자: {creatorName})");

        // Blackhole 클래스의 정적 메서드 호출
        Blackhole.ReceiveTerrainDestructionRPC(centerX, centerY, innerRadius, outerRadius, blackholeId, creatorName);
    }*/

}