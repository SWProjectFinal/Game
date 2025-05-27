using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("Login UI")]
    public GameObject loginPanel;
    public InputField nicknameInput;
    public Button playButton;

    [Header("Room List UI")]
    public GameObject roomListPanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Transform roomListContent;
    public GameObject roomItemButton;

    [Header("Create Room UI")]
    public GameObject createRoomPanel;
    public InputField roomNameInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;

    [Header("Room UI")]
    public GameObject roomPanel;
    public Text roomNameText;
    public Text playersText;
    public Button startGameButton;
    public Button leaveRoomButton;

    private string selectedRoomName = "";
    private List<string> roomList = new List<string>();

    void Start()
    {
        // 초기 UI 설정
        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);

        // 버튼 이벤트 연결
        playButton.onClick.AddListener(OnPlayButtonClicked);
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        confirmCreateButton.onClick.AddListener(OnConfirmCreateButtonClicked);
        cancelCreateButton.onClick.AddListener(OnCancelCreateButtonClicked);
        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        string nickname = nicknameInput.text;

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.Log("닉네임을 입력해주세요!");
            return;
        }

        PhotonNetwork.NickName = nickname;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("서버 연결 중...");

        // 화면 전환 (빈 목록으로 시작)
        loginPanel.SetActive(false);
        roomListPanel.SetActive(true);
    }

    void OnCreateRoomButtonClicked()
    {
        Debug.Log("방 만들기 버튼 클릭됨");

        // 방 제목 입력 팝업 표시
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    void OnJoinRoomButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedRoomName))
        {
            Debug.Log("방을 선택해주세요!");
            return;
        }

        Debug.Log(selectedRoomName + " 방에 참가합니다.");

        // 방 입장 (일반 플레이어)
        EnterRoom(selectedRoomName, false);
    }

    void OnConfirmCreateButtonClicked()
    {
        string roomName = roomNameInput.text;

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.Log("방 제목을 입력해주세요!");
            return;
        }

        Debug.Log(roomName + " 방을 생성합니다.");

        // 방 목록에 추가
        CreateRoomItem(roomName);

        // 방 입장 (방장)
        EnterRoom(roomName, true);

        // 입력창 초기화
        roomNameInput.text = "";
    }

    void OnCancelCreateButtonClicked()
    {
        Debug.Log("방 만들기 취소");

        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(true);
        roomNameInput.text = "";
    }

    void OnStartGameButtonClicked()
    {
        Debug.Log("게임 시작!");
        // 게임 씬으로 전환 (추후 구현)
    }

    void OnLeaveRoomButtonClicked()
    {
        Debug.Log("방에서 나가기");

        roomPanel.SetActive(false);
        roomListPanel.SetActive(true);
    }

    // 방 아이템 생성
    void CreateRoomItem(string roomName)
    {
        GameObject newRoomItem = Instantiate(roomItemButton, roomListContent);
        Text roomText = newRoomItem.GetComponentInChildren<Text>();
        roomText.text = roomName + " (1/4)"; // 임시 인원 표시

        // 방 선택 이벤트 연결
        Button roomButton = newRoomItem.GetComponent<Button>();
        roomButton.onClick.AddListener(() =>
        {
            selectedRoomName = roomName;
            Debug.Log(roomName + " 방 선택됨");

            // 선택된 방 하이라이트 (추후 구현)
        });

        roomList.Add(roomName);
    }

    // 방 입장
    void EnterRoom(string roomName, bool isMaster)
    {
        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);

        roomNameText.text = roomName;
        playersText.text = "플레이어: " + PhotonNetwork.NickName;

        // 방장만 게임 시작 버튼 표시
        startGameButton.gameObject.SetActive(isMaster);

        if (isMaster)
        {
            Debug.Log("방장으로 " + roomName + " 방에 입장했습니다.");
        }
        else
        {
            Debug.Log("일반 플레이어로 " + roomName + " 방에 입장했습니다.");
        }
    }
}