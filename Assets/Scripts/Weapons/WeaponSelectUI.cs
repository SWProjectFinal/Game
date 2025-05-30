public class WeaponSelectUI : MonoBehaviour
{
    public void OnClickSelectWeapon(int weaponId)
    {
        PhotonView view = PhotonView.Get(WeaponManager.Instance);
        view.RPC("SelectWeapon", RpcTarget.All, weaponId);
    }
}
