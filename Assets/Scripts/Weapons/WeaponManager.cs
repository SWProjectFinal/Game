public class WeaponManager : MonoBehaviourPunCallbacks
{
    public static WeaponManager Instance;

    public int selectedWeaponId = 0;  // 선택된 무기 ID
    public GameObject[] weaponPrefabs;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [PunRPC]
    public void SelectWeapon(int weaponId)
    {
        selectedWeaponId = weaponId;
        // UI 상태 갱신, 아이콘 강조 등
    }

    public void FireWeapon(Vector2 dir, float power)
    {
        photonView.RPC("RPC_FireWeapon", RpcTarget.All, dir, power);
    }

    [PunRPC]
    void RPC_FireWeapon(Vector2 dir, float power)
    {
        GameObject proj = PhotonNetwork.Instantiate(weaponPrefabs[selectedWeaponId].name, transform.position, Quaternion.identity);
        proj.GetComponent<Rigidbody2D>().AddForce(dir * power, ForceMode2D.Impulse);
    }
}
