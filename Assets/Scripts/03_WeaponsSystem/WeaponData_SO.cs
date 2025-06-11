using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData_SO : ScriptableObject
{
    public string weaponName;
    public Sprite icon;
    public GameObject projectilePrefab;

    [Header("무기 설정")]
    public float damage = 10f;
    public float range = 10f;
    public float cooldown = 1f;
    public float bulletSpeed = 20f;

    [Header("이펙트 & 사운드")]
    public GameObject explosionEffectPrefab;
    public AudioClip fireSound;

    [Header("특수 옵션")]
    public float explosionRadius = 0f;
    public float explosionForce = 0f;

    public bool isInstantUse = false;

    [Header("물리 옵션")]
    public bool useGravity = false;

}
