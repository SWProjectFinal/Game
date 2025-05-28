using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;

public class LobbyManager : MonoBehaviourPun, IConnectionCallbacks, IMatchmakingCallbacks, ILobbyCallbacks
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
    private Dictionary<string, RoomInfo> roomListDictionary = new Dictionary<string, RoomInfo>();
    private Coroutine autoRefreshCoroutine;
    void Start()
    {
        // 초기 UI 설정
        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);

        // Photon 콜백 강제 등록
        PhotonNetwork.AddCallbackTarget(this);

        // 버튼 이벤트 연결
        playButton.onClick.AddListener(OnPlayButtonClicked);
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        confirmCreateButton.onClick.AddListener(OnConfirmCreateButtonClicked);
        cancelCreateButton.onClick.AddListener(OnCancelCreateButtonClicked);
        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
    }

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    #region Button Events
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
    }

    void OnCreateRoomButtonClicked()
    {
        // roomListPanel.SetActive(false); // 이 줄 주석처리!
        createRoomPanel.SetActive(true);
    }

    void OnJoinRoomButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedRoomName))
        {
            Debug.Log("방을 선택해주세요!");
            return;
        }

        PhotonNetwork.JoinRoom(selectedRoomName);
        Debug.Log(selectedRoomName + " 방 참가 시도 중...");
    }

    void OnConfirmCreateButtonClicked()
    {
        string roomName = roomNameInput.text;

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.Log("방 제목을 입력해주세요!");
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log(roomName + " 방 생성 시도 중...");
    }

    void OnCancelCreateButtonClicked()
    {
        createRoomPanel.SetActive(false);
        // roomListPanel.SetActive(true); // 이 줄도 주석처리!
        roomNameInput.text = "";
    }

    void OnStartGameButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    void OnLeaveRoomButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }
    #endregion

    #region IConnectionCallbacks
    public void OnConnected()
    {
        Debug.Log("Photon 서버에 연결됨");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결 완료!");
        PhotonNetwork.JoinLobby();
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("연결 끊김: " + cause);
        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log("지역 목록 수신됨");
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("커스텀 인증 응답");
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log("커스텀 인증 실패: " + debugMessage);
    }
    #endregion

    #region ILobbyCallbacks
    public void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료!");
        loginPanel.SetActive(false);
        roomListPanel.SetActive(true);
    }

    public void OnLeftLobby()
    {
        Debug.Log("로비에서 나감");
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("방 목록 업데이트: " + roomList.Count + "개");
        UpdateRoomListUI(roomList);
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        // 로비 통계 업데이트
    }
    #endregion

    #region IMatchmakingCallbacks
    public void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공!");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 생성 실패: " + message);
        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(true);
    }

    public void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공!");

        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        UpdatePlayerList();
        roomNameInput.text = "";

        // 자동 새로고침 시작
        if (autoRefreshCoroutine != null)
            StopCoroutine(autoRefreshCoroutine);
        autoRefreshCoroutine = StartCoroutine(AutoRefreshPlayerList());
    }

    // 방 입장 후 지연 업데이트
    IEnumerator DelayedUpdateAfterJoin()
    {
        yield return new WaitForSeconds(0.5f);
        UpdatePlayerList();
        Debug.Log("방 입장 후 지연 업데이트 완료");
    }
    void Update()
    {
        // 개발 중에만: R키를 누르면 강제 새로고침
        if (Input.GetKeyDown(KeyCode.R) && PhotonNetwork.InRoom)
        {
            Debug.Log("강제 플레이어 목록 새로고침");
            UpdatePlayerList();
        }
    }
    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 입장 실패: " + message);
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("랜덤 방 입장 실패: " + message);
    }

    public void OnLeftRoom()
    {
        Debug.Log("방에서 나감");

        // 자동 새로고침 중지
        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
            autoRefreshCoroutine = null;
        }

        roomPanel.SetActive(false);
        roomListPanel.SetActive(true);
    }
    // 자동 새고침 코루틴
    IEnumerator AutoRefreshPlayerList()
    {
        while (PhotonNetwork.InRoom)
        {
            yield return new WaitForSeconds(1f); // 1초마다

            if (PhotonNetwork.InRoom && roomPanel.activeInHierarchy)
            {
                UpdatePlayerList();
            }
        }
    }
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("=== OnPlayerEnteredRoom 호출됨! ===");
        Debug.Log("입장 플레이어: " + newPlayer.NickName);
        Debug.Log("내가 받은 콜백임. 내 닉네임: " + PhotonNetwork.LocalPlayer.NickName);
        Debug.Log("현재 총 인원: " + PhotonNetwork.CurrentRoom.PlayerCount + "명");

        UpdatePlayerList();
        StartCoroutine(DelayedUpdatePlayerList());
    }
    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("=== 플레이어 퇴장 ===");
        Debug.Log(otherPlayer.NickName + "님이 퇴장!");
        Debug.Log("현재 총 인원: " + PhotonNetwork.CurrentRoom.PlayerCount + "명");

        // UI 강제 업데이트
        StartCoroutine(DelayedUpdatePlayerList());

        // 방장 권한 업데이트
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
    // 지연된 UI 업데이트 (네트워크 동기화 대기)
    IEnumerator DelayedUpdatePlayerList()
    {
        yield return new WaitForSeconds(0.1f);
        UpdatePlayerList();
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("=== 방장 변경 ===");
        Debug.Log("새 방장: " + newMasterClient.NickName);
        Debug.Log("내가 새 방장인가? " + PhotonNetwork.IsMasterClient);

        // UI 강제 업데이트
        UpdatePlayerList();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 플레이어 속성 업데이트
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // 방 속성 업데이트
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        Debug.Log("친구 목록 업데이트");
    }
    #endregion

    #region UI Helper Methods
    void UpdateRoomListUI(List<RoomInfo> roomList)
    {
        // 기존 방 목록 정보 업데이트
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                roomListDictionary.Remove(room.Name);
            }
            else
            {
                roomListDictionary[room.Name] = room;
            }
        }

        // UI 방 목록 새로고침
        RefreshRoomListUI();
    }

    void RefreshRoomListUI()
    {
        // 기존 UI 삭제
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // 새 방 목록 UI 생성
        foreach (RoomInfo room in roomListDictionary.Values)
        {
            GameObject newRoomItem = Instantiate(roomItemButton, roomListContent);
            Text roomText = newRoomItem.GetComponentInChildren<Text>();
            roomText.text = room.Name + " (" + room.PlayerCount + "/" + room.MaxPlayers + ")";

            Button roomButton = newRoomItem.GetComponent<Button>();
            string roomName = room.Name; // 클로저를 위한 로컬 변수

            roomButton.onClick.AddListener(() =>
            {
                selectedRoomName = roomName;
                Debug.Log(roomName + " 방 선택됨");
            });
        }
    }

    void UpdatePlayerList()
    {
        if (PhotonNetwork.CurrentRoom == null || !PhotonNetwork.InRoom)
        {
            Debug.Log("방에 없음 - 업데이트 건너뜀");
            return;
        }

        Debug.Log("=== MasterClient 디버깅 ===");
        Debug.Log("내가 방장인가? " + PhotonNetwork.IsMasterClient);
        Debug.Log("실제 방장: " + PhotonNetwork.MasterClient.NickName);
        Debug.Log("내 닉네임: " + PhotonNetwork.LocalPlayer.NickName);

        string playerListText = "플레이어 목록 (" + PhotonNetwork.CurrentRoom.PlayerCount + "명):\n";

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Debug.Log("플레이어: " + player.NickName +
                     " | IsMasterClient: " + player.IsMasterClient +
                     " | ActorNumber: " + player.ActorNumber);

            playerListText += "• " + player.NickName;

            // 실제 MasterClient와 비교해서 방장 표시
            if (player.ActorNumber == PhotonNetwork.MasterClient.ActorNumber)
                playerListText += " (방장)";

            playerListText += "\n";
        }

        playersText.text = playerListText;
        Debug.Log("UI 업데이트 완료!");
    }
    #endregion
}