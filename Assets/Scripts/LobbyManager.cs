using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPun
{
    [Header("UI References")]
    public InputField nicknameInput;
    public Button playButton;

    void Start()
    {
        // Play 버튼 클릭 이벤트 연결
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        string nickname = nicknameInput.text;

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.Log("닉네임을 입력해주세요!");
            return;
        }

        // 닉네임 설정
        PhotonNetwork.NickName = nickname;

        // Photon 서버 연결
        PhotonNetwork.ConnectUsingSettings();

        Debug.Log("서버 연결 중...");
    }
}