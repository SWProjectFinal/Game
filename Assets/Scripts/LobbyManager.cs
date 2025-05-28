using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;

public class LobbyManager : MonoBehaviourPun, IConnectionCallbacks, IMatchmakingCallbacks, ILobbyCallbacks
{
    [Header("🎮 Login UI")]
    public GameObject loginPanel;
    public InputField nicknameInput;
    public Button playButton;
    public GameObject loginLoadingUI; // 로딩 스피너
    public Text loadingText;

    [Header("🏠 Room List UI")]
    public GameObject roomListPanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button refreshButton; // 새로고침 버튼 추가
    public Transform roomListContent;
    public GameObject roomItemButton;
    public GameObject roomListLoadingUI; // 방 목록 로딩

    [Header("🏗️ Create Room UI")]
    public GameObject createRoomPanel;
    public InputField roomNameInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public GameObject createLoadingUI; // 방 생성 로딩

    [Header("🏠 Room UI")]
    public GameObject roomPanel;
    public Text roomNameText;
    public Text playersText;
    public Button startGameButton;
    public Button leaveRoomButton;
    public Transform playerListContent; // 플레이어 목록용 Transform
    public GameObject playerItemPrefab; // 플레이어 아이템 프리팹

    [Header("📢 Notification System")]
    public GameObject notificationPanel;
    public Text notificationText; // Text로 변경!
    public Image notificationBackground;

    [Header("🎨 UI Colors")]
    public Color primaryColor = new Color(1f, 0.39f, 0.28f); // #FF6347
    public Color secondaryColor = new Color(0.25f, 0.41f, 0.88f); // #4169E1
    public Color successColor = new Color(0.2f, 0.8f, 0.2f); // #32CD32
    public Color dangerColor = new Color(0.86f, 0.08f, 0.24f); // #DC143C

    private string selectedRoomName = "";
    private Dictionary<string, RoomInfo> roomListDictionary = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomUIItems = new Dictionary<string, GameObject>();
    private Coroutine autoRefreshCoroutine;
    private Coroutine notificationCoroutine;

    void Start()
    {
        InitializeUI();
        SetupButtonEvents();
        ApplyUIColors();
        PhotonNetwork.AddCallbackTarget(this);
    }

    void InitializeUI()
    {
        // 초기 UI 설정
        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);

        // 로딩 UI 숨김
        if (loginLoadingUI) loginLoadingUI.SetActive(false);
        if (roomListLoadingUI) roomListLoadingUI.SetActive(false);
        if (createLoadingUI) createLoadingUI.SetActive(false);
        if (notificationPanel) notificationPanel.SetActive(false);
    }

    void SetupButtonEvents()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);

        // refreshButton 안전성 체크 추가!
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        confirmCreateButton.onClick.AddListener(OnConfirmCreateButtonClicked);
        cancelCreateButton.onClick.AddListener(OnCancelCreateButtonClicked);
        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);

        // 엔터키 이벤트 추가
        if (nicknameInput)
        {
            nicknameInput.onEndEdit.AddListener(delegate
            {
                if (Input.GetKeyDown(KeyCode.Return)) OnPlayButtonClicked();
            });
        }

        if (roomNameInput)
        {
            roomNameInput.onEndEdit.AddListener(delegate
            {
                if (Input.GetKeyDown(KeyCode.Return)) OnConfirmCreateButtonClicked();
            });
        }
    }

    #region 🔔 Notification System
    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);

        notificationCoroutine = StartCoroutine(DisplayNotification(message, type));
    }

    IEnumerator DisplayNotification(string message, NotificationType type)
    {
        if (!notificationPanel || !notificationText || !notificationBackground)
        {
            Debug.Log("알림 UI가 연결되지 않음: " + message);
            yield break;
        }

        // 색상 설정
        switch (type)
        {
            case NotificationType.Success:
                notificationBackground.color = successColor;
                break;
            case NotificationType.Error:
                notificationBackground.color = dangerColor;
                break;
            case NotificationType.Warning:
                notificationBackground.color = new Color(1f, 0.65f, 0f); // Orange
                break;
            default:
                notificationBackground.color = secondaryColor;
                break;
        }

        // 텍스트 설정 강화
        notificationText.text = message;
        notificationText.color = Color.white; // 흰색 강제 설정

        // 패널 크기 자동 조정
        RectTransform rectTransform = notificationPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 텍스트 길이에 따라 패널 크기 조정
            float textWidth = message.Length * 12f; // 대략적인 계산
            float panelWidth = Mathf.Clamp(textWidth, 300f, 600f);
            rectTransform.sizeDelta = new Vector2(panelWidth, 80f);
        }

        notificationPanel.SetActive(true);

        // 슬라이드 인 애니메이션
        notificationPanel.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        float timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(0.8f, 1f, timer / 0.3f);
            notificationPanel.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // 3초 대기
        yield return new WaitForSeconds(3f);

        // 슬라이드 아웃 애니메이션
        timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0.8f, timer / 0.3f);
            notificationPanel.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        notificationPanel.SetActive(false);
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
    #endregion

    #region 🔄 Loading System
    void ShowLoading(GameObject loadingUI, string message = "")
    {
        if (loadingUI)
        {
            loadingUI.SetActive(true);
            if (!string.IsNullOrEmpty(message) && loadingText)
                loadingText.text = message;
        }
    }

    void HideLoading(GameObject loadingUI)
    {
        if (loadingUI)
            loadingUI.SetActive(false);
    }
    #endregion

    #region 🎮 Button Events
    void OnPlayButtonClicked()
    {
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            ShowNotification("🐛 플레이어 이름을 입력해주세요!", NotificationType.Error);
            return;
        }

        if (nickname.Length > 12)
        {
            ShowNotification("🐛 이름은 12자 이하로 입력해주세요!", NotificationType.Error);
            return;
        }

        PhotonNetwork.NickName = nickname;
        ShowLoading(loginLoadingUI, "서버에 연결 중...");
        PhotonNetwork.ConnectUsingSettings();

        Debug.Log("서버 연결 시도: " + nickname);
    }

    void OnCreateRoomButtonClicked()
    {
        createRoomPanel.SetActive(true);
        roomNameInput.text = "";
        roomNameInput.Select(); // 자동 포커스
    }

    void OnJoinRoomButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedRoomName))
        {
            ShowNotification("🏠 참가할 방을 선택해주세요!", NotificationType.Warning);
            return;
        }

        ShowLoading(roomListLoadingUI, "방에 참가하는 중...");
        PhotonNetwork.JoinRoom(selectedRoomName);
        Debug.Log(selectedRoomName + " 방 참가 시도 중...");
    }

    void OnRefreshButtonClicked()
    {
        if (roomListLoadingUI != null)
            ShowLoading(roomListLoadingUI, "방 목록을 새로고침하는 중...");

        // 기존 방 목록 초기화
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }
        roomUIItems.Clear();
        roomListDictionary.Clear();
        selectedRoomName = "";

        // 로비 재입장으로 방 목록 갱신
        PhotonNetwork.LeaveLobby();
        StartCoroutine(RejoinLobbyAfterDelay());
    }

    IEnumerator RejoinLobbyAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.JoinLobby();
    }

    void OnConfirmCreateButtonClicked()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            ShowNotification("🏗️ 방 이름을 입력해주세요!", NotificationType.Error);
            return;
        }

        if (roomName.Length > 20)
        {
            ShowNotification("🏗️ 방 이름은 20자 이하로 입력해주세요!", NotificationType.Error);
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        ShowLoading(createLoadingUI, "방을 생성하는 중...");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log(roomName + " 방 생성 시도 중...");
    }

    void OnCancelCreateButtonClicked()
    {
        createRoomPanel.SetActive(false);
        roomNameInput.text = "";
    }

    void OnStartGameButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                ShowNotification("⚔️ 최소 2명 이상의 플레이어가 필요합니다!", NotificationType.Warning);
                return;
            }

            ShowNotification("🚀 게임을 시작합니다!", NotificationType.Success);
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    void OnLeaveRoomButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
        ShowNotification("🚪 방에서 나왔습니다.", NotificationType.Info);
    }
    #endregion

    #region 🌐 IConnectionCallbacks
    public void OnConnected()
    {
        Debug.Log("Photon 서버에 연결됨");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결 완료!");
        HideLoading(loginLoadingUI);
        PhotonNetwork.JoinLobby();
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("연결 끊김: " + cause);
        HideLoading(loginLoadingUI);
        ShowNotification("❌ 서버 연결이 끊어졌습니다: " + cause, NotificationType.Error);

        // UI 초기화
        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    #endregion

    #region 🏠 ILobbyCallbacks
    public void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료!");
        HideLoading(roomListLoadingUI);

        loginPanel.SetActive(false);
        roomListPanel.SetActive(true);

        ShowNotification("🎮 로비에 입장했습니다!", NotificationType.Success);

        RefreshRoomListUI();
    }

    void RefreshRoomListUI()
    {
        // 기존 방 목록 UI 삭제
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // 딕셔너리에 있는 방들 다시 표시
        foreach (RoomInfo room in roomListDictionary.Values)
        {
            UpdateRoomItemUI(room);
        }
    }

    public void OnLeftLobby()
    {
        Debug.Log("로비에서 나감");
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"방 목록 업데이트: {roomList.Count}개");
        HideLoading(roomListLoadingUI);
        UpdateRoomListUI(roomList);
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }
    #endregion

    #region 🎯 IMatchmakingCallbacks
    public void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공!");
        HideLoading(createLoadingUI);
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 생성 실패: " + message);
        HideLoading(createLoadingUI);
        ShowNotification("❌ 방 생성에 실패했습니다: " + message, NotificationType.Error);
    }

    public void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공!");
        HideLoading(roomListLoadingUI);

        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        UpdatePlayerList();
        roomNameInput.text = "";

        ShowNotification("🏠 방에 입장했습니다!", NotificationType.Success);

        // 자동 새로고침 시작
        if (autoRefreshCoroutine != null)
            StopCoroutine(autoRefreshCoroutine);
        autoRefreshCoroutine = StartCoroutine(AutoRefreshPlayerList());
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 입장 실패: " + message);
        HideLoading(roomListLoadingUI);
        ShowNotification("❌ 방 참가에 실패했습니다: " + message, NotificationType.Error);
    }

    public void OnJoinRandomFailed(short returnCode, string message) { }

    public void OnLeftRoom()
    {
        Debug.Log("방에서 나감");

        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
            autoRefreshCoroutine = null;
        }

        roomPanel.SetActive(false);
        roomListPanel.SetActive(true);
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"=== 플레이어 입장: {newPlayer.NickName} ===");
        UpdatePlayerList();
        ShowNotification($"🐛 {newPlayer.NickName}님이 입장했습니다!", NotificationType.Success);
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"=== 플레이어 퇴장: {otherPlayer.NickName} ===");
        UpdatePlayerList();
        ShowNotification($"👋 {otherPlayer.NickName}님이 퇴장했습니다.", NotificationType.Info);

        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"=== 방장 변경: {newMasterClient.NickName} ===");
        UpdatePlayerList();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient)
            ShowNotification("👑 당신이 새로운 방장이 되었습니다!", NotificationType.Success);
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    #endregion

    #region 🎨 UI Helper Methods
    void UpdateRoomListUI(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                // 방 제거
                if (roomUIItems.ContainsKey(room.Name))
                {
                    Destroy(roomUIItems[room.Name]);
                    roomUIItems.Remove(room.Name);
                }
                roomListDictionary.Remove(room.Name);
            }
            else
            {
                // 방 추가/업데이트
                roomListDictionary[room.Name] = room;
                UpdateRoomItemUI(room);
            }
        }
    }

    void UpdateRoomItemUI(RoomInfo room)
    {
        GameObject roomItem;

        if (roomUIItems.ContainsKey(room.Name))
        {
            // 기존 방 UI 업데이트
            roomItem = roomUIItems[room.Name];
        }
        else
        {
            // 새 방 UI 생성
            roomItem = Instantiate(roomItemButton, roomListContent);
            roomUIItems[room.Name] = roomItem;

            // 클릭 이벤트 설정
            Button roomButton = roomItem.GetComponent<Button>();
            string roomName = room.Name;

            roomButton.onClick.RemoveAllListeners();
            roomButton.onClick.AddListener(() => SelectRoom(roomName));
        }

        // 방 정보 업데이트 (Text 사용!)
        Text[] texts = roomItem.GetComponentsInChildren<Text>();
        if (texts.Length > 0)
        {
            texts[0].text = $"🏠 {room.Name}";
        }
        if (texts.Length > 1)
        {
            texts[1].text = $"👥 {room.PlayerCount}/{room.MaxPlayers} 플레이어";
        }
    }

    void SelectRoom(string roomName)
    {
        // 기존 선택 해제
        foreach (var item in roomUIItems.Values)
        {
            var colors = item.GetComponent<Button>().colors;
            colors.normalColor = Color.white;
            item.GetComponent<Button>().colors = colors;
        }

        // 새 방 선택
        if (roomUIItems.ContainsKey(roomName))
        {
            var colors = roomUIItems[roomName].GetComponent<Button>().colors;
            colors.normalColor = new Color(1f, 0.9f, 0.9f); // 연한 빨간색
            roomUIItems[roomName].GetComponent<Button>().colors = colors;
        }

        selectedRoomName = roomName;
        Debug.Log("방 선택: " + roomName);
    }

    void UpdatePlayerList()
    {
        if (PhotonNetwork.CurrentRoom == null || !PhotonNetwork.InRoom)
            return;

        // 기존 플레이어 UI 삭제
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // 플레이어 목록 업데이트 (Text 사용!)
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerItem = Instantiate(playerItemPrefab, playerListContent);

            Text[] texts = playerItem.GetComponentsInChildren<Text>();
            if (texts.Length > 0)
            {
                string playerText = $"🐛 {player.NickName}";
                if (player.IsMasterClient)
                    playerText += " 👑";

                texts[0].text = playerText;
            }

            // 방장 표시 (배경색 변경)
            if (player.IsMasterClient)
            {
                Image bg = playerItem.GetComponent<Image>();
                if (bg) bg.color = new Color(1f, 1f, 0.8f); // 연한 노란색
            }
        }

        // 플레이어 수 업데이트
        playersText.text = $"플레이어 ({PhotonNetwork.CurrentRoom.PlayerCount}/4)";
    }

    IEnumerator AutoRefreshPlayerList()
    {
        while (PhotonNetwork.InRoom)
        {
            yield return new WaitForSeconds(2f);
            if (PhotonNetwork.InRoom && roomPanel.activeInHierarchy)
            {
                UpdatePlayerList();
            }
        }
    }
    // 새로운 함수 추가 (맨 아래쪽에)
    void ApplyUIColors()
    {
        // 주요 버튼들 색상 적용
        ApplyButtonColors(playButton, primaryColor);           // 빨간색 (게임시작)
        ApplyButtonColors(confirmCreateButton, successColor);   // 초록색 (방만들기 확인)
        ApplyButtonColors(startGameButton, successColor);      // 초록색 (게임시작)

        ApplyButtonColors(createRoomButton, secondaryColor);   // 파란색 (방만들기)
        ApplyButtonColors(joinRoomButton, secondaryColor);     // 파란색 (참가하기)
        if (refreshButton != null)
            ApplyButtonColors(refreshButton, secondaryColor);  // 파란색 (새로고침)

        ApplyButtonColors(leaveRoomButton, dangerColor);       // 빨간색 (나가기)
        ApplyButtonColors(cancelCreateButton, dangerColor);    // 빨간색 (취소)

        // 알림 패널 기본 색상
        if (notificationBackground != null)
            notificationBackground.color = secondaryColor;

        Debug.Log("UI 색상 적용 완료!");
    }

    // 버튼 색상 적용 헬퍼 함수
    void ApplyButtonColors(Button button, Color baseColor)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = LightenColor(baseColor, 0.1f);  // 살짝 밝게
        colors.pressedColor = DarkenColor(baseColor, 0.1f);       // 살짝 어둡게
        colors.selectedColor = baseColor;
        colors.disabledColor = Color.gray;

        button.colors = colors;
    }

    // 색상 밝게 만들기
    Color LightenColor(Color color, float amount)
    {
        return Color.Lerp(color, Color.white, amount);
    }

    // 색상 어둡게 만들기  
    Color DarkenColor(Color color, float amount)
    {
        return Color.Lerp(color, Color.black, amount);
    }
    #endregion

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}