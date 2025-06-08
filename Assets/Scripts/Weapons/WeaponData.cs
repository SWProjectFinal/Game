//무기 정보 저장용 클래스

using UnityEngine;

[System.Serializable]
public class WeaponData
{
    public WeaponType type;
    public string displayName;
    public Sprite icon;
    public GameObject projectilePrefab;
    public float damage;
    public bool isInstantUse;
}

