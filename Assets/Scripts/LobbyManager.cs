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
    public GameObject loginLoadingUI;
    public Text loadingText;

    [Header("🏠 Room List UI")]
    public GameObject roomListPanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button refreshButton;
    public Transform roomListContent;
    public GameObject roomItemButton;
    public GameObject roomListLoadingUI;

    [Header("🏗️ Create Room UI")]
    public GameObject createRoomPanel;
    public InputField roomNameInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public GameObject createLoadingUI;

    [Header("🔐 비공개 방 시스템")]
    public Toggle privateRoomToggle;        // 비공개 방 체크박스
    public GameObject passwordInputGroup;   // 비밀번호 입력 그룹
    public InputField roomPasswordInput;    // 비밀번호 입력 필드
    public GameObject passwordPromptPanel;  // 비밀번호 입력 팝업
    public InputField joinPasswordInput;    // 방 참가용 비밀번호 입력
    public Button confirmPasswordButton;    // 비밀번호 확인 버튼
    public Button cancelPasswordButton;     // 비밀번호 취소 버튼

    [Header("👥 방 관리 시스템")]
    public Dropdown maxPlayersDropdown;     // 최대 인원 선택

    [Header("🏠 Room UI")]
    public GameObject roomPanel;
    public Text roomNameText;
    public Text playersText;
    public Button startGameButton;
    public Button leaveRoomButton;
    public Transform playerListContent;
    public GameObject playerItemPrefab;

    [Header("✅ 플레이어 준비 상태")]
    public Button readyButton;              // 준비 완료/취소 버튼 (일반 플레이어용)
    public Text gameStatusText;             // 게임 상태 메시지

    [Header("👢 플레이어 추방 시스템")]
    public Button kickButtonPrefab;         // 추방 버튼 프리팹
    public GameObject kickConfirmPanel;     // 추방 확인 팝업
    public Text kickConfirmText;            // 추방 확인 메시지
    public Button confirmKickButton;        // 추방 확인 버튼
    public Button cancelKickButton;         // 추방 취소 버튼

    [Header("📢 Notification System")]
    public GameObject notificationPanel;
    public Text notificationText;
    public Image notificationBackground;

    [Header("🎨 UI Colors")]
    public Color primaryColor = new Color(1f, 0.39f, 0.28f); // #FF6347
    public Color secondaryColor = new Color(0.25f, 0.41f, 0.88f); // #4169E1
    public Color successColor = new Color(0.2f, 0.8f, 0.2f); // #32CD32
    public Color dangerColor = new Color(0.86f, 0.08f, 0.24f); // #DC143C

    // 기존 변수들
    private string selectedRoomName = "";
    private Dictionary<string, RoomInfo> roomListDictionary = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomUIItems = new Dictionary<string, GameObject>();
    private Dictionary<string, System.DateTime> roomCreationTimes = new Dictionary<string, System.DateTime>();
    private Coroutine autoRefreshCoroutine;
    private Coroutine notificationCoroutine;

    // B2 새 변수들
    private string selectedRoomPassword = "";
    private string attemptingRoomName = "";
    private Dictionary<string, bool> playerReadyStates = new Dictionary<string, bool>();

    // 추방 시스템 변수
    private string targetKickPlayerName = "";

    void Start()
    {
        InitializeUI();
        SetupButtonEvents();
        ApplyUIColors();
        SetupAdvancedLobbySystem(); // B2 기능 초기화
        PhotonNetwork.AddCallbackTarget(this);
    }

    void InitializeUI()
    {
        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);

        if (loginLoadingUI) loginLoadingUI.SetActive(false);
        if (roomListLoadingUI) roomListLoadingUI.SetActive(false);
        if (createLoadingUI) createLoadingUI.SetActive(false);
        if (notificationPanel) notificationPanel.SetActive(false);
        if (passwordPromptPanel) passwordPromptPanel.SetActive(false);
        if (kickConfirmPanel) kickConfirmPanel.SetActive(false);
    }

    void ApplyUIColors()
    {
        ApplyButtonColors(playButton, primaryColor);
        ApplyButtonColors(confirmCreateButton, successColor);
        ApplyButtonColors(startGameButton, successColor);

        ApplyButtonColors(createRoomButton, secondaryColor);
        ApplyButtonColors(joinRoomButton, secondaryColor);
        if (refreshButton != null)
            ApplyButtonColors(refreshButton, secondaryColor);

        ApplyButtonColors(leaveRoomButton, dangerColor);
        ApplyButtonColors(cancelCreateButton, dangerColor);

        if (notificationBackground != null)
            notificationBackground.color = secondaryColor;

        Debug.Log("UI 색상 적용 완료!");
    }

    void ApplyButtonColors(Button button, Color baseColor)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = LightenColor(baseColor, 0.1f);
        colors.pressedColor = DarkenColor(baseColor, 0.1f);
        colors.selectedColor = baseColor;
        colors.disabledColor = Color.gray;

        button.colors = colors;
    }

    Color LightenColor(Color color, float amount)
    {
        return Color.Lerp(color, Color.white, amount);
    }

    Color DarkenColor(Color color, float amount)
    {
        return Color.Lerp(color, Color.black, amount);
    }

    void SetupButtonEvents()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        confirmCreateButton.onClick.AddListener(OnConfirmCreateButtonClicked);
        cancelCreateButton.onClick.AddListener(OnCancelCreateButtonClicked);
        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);

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

    // B2: 고급 로비 시스템 초기화
    void SetupAdvancedLobbySystem()
    {
        SetupPrivateRoomSystem();
        SetupReadySystem();
        SetupRoomManagement();
        SetupKickSystem(); // 추방 시스템 추가
    }

    void SetupPrivateRoomSystem()
    {
        if (privateRoomToggle)
        {
            privateRoomToggle.onValueChanged.AddListener(OnPrivateRoomToggleChanged);
            privateRoomToggle.isOn = false;
        }

        if (passwordInputGroup) passwordInputGroup.SetActive(false);
        if (passwordPromptPanel) passwordPromptPanel.SetActive(false);

        if (confirmPasswordButton) confirmPasswordButton.onClick.AddListener(OnConfirmPasswordClicked);
        if (cancelPasswordButton) cancelPasswordButton.onClick.AddListener(OnCancelPasswordClicked);

        Debug.Log("비공개 방 시스템 초기화 완료");
    }

    void SetupReadySystem()
    {
        if (readyButton)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
            readyButton.gameObject.SetActive(false);
        }

        Debug.Log("준비 상태 시스템 초기화 완료");
    }

    void SetupRoomManagement()
    {
        if (maxPlayersDropdown)
        {
            maxPlayersDropdown.ClearOptions();
            maxPlayersDropdown.AddOptions(new List<string> { "2명", "3명", "4명" });
            maxPlayersDropdown.value = 2;
        }

        Debug.Log("방 관리 시스템 초기화 완료");
    }

    // 추방 시스템 초기화
    void SetupKickSystem()
    {
        if (confirmKickButton) confirmKickButton.onClick.AddListener(OnConfirmKickClicked);
        if (cancelKickButton) cancelKickButton.onClick.AddListener(OnCancelKickClicked);

        Debug.Log("플레이어 추방 시스템 초기화 완료");
    }

    // B2: 비공개 방 토글 변경
    void OnPrivateRoomToggleChanged(bool isPrivate)
    {
        if (passwordInputGroup)
        {
            passwordInputGroup.SetActive(isPrivate);
            if (isPrivate && roomPasswordInput)
                roomPasswordInput.Select();
            else if (roomPasswordInput)
                roomPasswordInput.text = "";
        }
    }

    // B2: 준비 상태 버튼 클릭
    void OnReadyButtonClicked()
    {
        if (!PhotonNetwork.InRoom) return;

        string myNickname = PhotonNetwork.LocalPlayer.NickName;
        bool currentReady = GetPlayerReadyState(myNickname);
        bool newReady = !currentReady;

        SetPlayerReadyState(myNickname, newReady);

        string message = newReady ? "준비 완료!" : "준비 취소";
        ShowNotification(message, newReady ? NotificationType.Success : NotificationType.Info);

        Debug.Log($"{myNickname} 준비 상태: {newReady}");
    }

    // B2: 플레이어 준비 상태 설정
    void SetPlayerReadyState(string playerName, bool isReady)
    {
        playerReadyStates[playerName] = isReady;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["ready"] = isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdatePlayerList();
        UpdateGameStartCondition();
    }

    // B2: 플레이어 준비 상태 가져오기
    bool GetPlayerReadyState(string playerName)
    {
        if (playerReadyStates.ContainsKey(playerName))
            return playerReadyStates[playerName];

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == playerName)
            {
                if (player.CustomProperties.ContainsKey("ready"))
                    return (bool)player.CustomProperties["ready"];
                break;
            }
        }

        return false;
    }

    // B2: 모든 플레이어 준비 상태 체크
    bool AreAllPlayersReady()
    {
        if (!PhotonNetwork.InRoom) return false;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!GetPlayerReadyState(player.NickName))
            {
                return false;
            }
        }

        return PhotonNetwork.CurrentRoom.PlayerCount >= 2;
    }

    // B2: 게임 시작 조건 업데이트
    void UpdateGameStartCondition()
    {
        if (!PhotonNetwork.InRoom) return;

        bool allReady = AreAllPlayersReady();
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyPlayers = 0;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (GetPlayerReadyState(player.NickName))
                readyPlayers++;
        }

        string statusMessage;
        if (currentPlayers < 2)
        {
            statusMessage = $"최소 2명 필요 (현재 {currentPlayers}명)";
        }
        else if (allReady)
        {
            statusMessage = "모든 플레이어 준비 완료! 게임 시작 가능";
        }
        else
        {
            statusMessage = $"{readyPlayers}/{currentPlayers}명 준비 완료 (대기중)";
        }

        if (gameStatusText)
        {
            gameStatusText.text = statusMessage;
            gameStatusText.color = allReady ? successColor : dangerColor;
        }

        if (PhotonNetwork.IsMasterClient && startGameButton)
        {
            startGameButton.interactable = allReady;

            ColorBlock colors = startGameButton.colors;
            if (allReady)
            {
                colors.normalColor = successColor;
                colors.highlightedColor = LightenColor(successColor, 0.1f);
            }
            else
            {
                colors.normalColor = Color.gray;
                colors.highlightedColor = Color.gray;
            }
            startGameButton.colors = colors;
        }

        Debug.Log($"게임 시작 조건: {(allReady ? "만족" : "불만족")} - {statusMessage}");
    }

    // B2: 준비 버튼 UI 업데이트
    void UpdateReadyButton()
    {
        if (!PhotonNetwork.InRoom || readyButton == null) return;

        bool isMaster = PhotonNetwork.IsMasterClient;
        bool isReady = GetPlayerReadyState(PhotonNetwork.LocalPlayer.NickName);

        readyButton.gameObject.SetActive(!isMaster);

        if (!isMaster)
        {
            Text buttonText = readyButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isReady ? "준비 취소" : "준비 완료";
            }

            ColorBlock colors = readyButton.colors;
            if (isReady)
            {
                colors.normalColor = dangerColor;
                colors.highlightedColor = LightenColor(dangerColor, 0.1f);
            }
            else
            {
                colors.normalColor = successColor;
                colors.highlightedColor = LightenColor(successColor, 0.1f);
            }
            readyButton.colors = colors;
        }
    }

    // 플레이어 추방 시스템
    void ShowKickConfirmation(string playerName)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        targetKickPlayerName = playerName;

        if (kickConfirmPanel && kickConfirmText)
        {
            kickConfirmPanel.SetActive(true);
            kickConfirmText.text = $"'{playerName}'님을 추방하시겠습니까?";
        }

        Debug.Log($"추방 확인 창 표시: {playerName}");
    }

    void OnConfirmKickClicked()
    {
        if (string.IsNullOrEmpty(targetKickPlayerName)) return;

        // 추방 대상 플레이어 찾기
        Player targetPlayer = null;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == targetKickPlayerName)
            {
                targetPlayer = player;
                break;
            }
        }

        if (targetPlayer != null)
        {
            // RPC로 추방 알림 전송
            photonView.RPC("NotifyPlayerKicked", targetPlayer, targetKickPlayerName);

            // 잠시 후 실제 추방 (알림을 받을 시간을 줌)
            StartCoroutine(KickPlayerAfterDelay(targetPlayer));

            ShowNotification($"{targetKickPlayerName}님을 추방했습니다.", NotificationType.Warning);
        }

        if (kickConfirmPanel) kickConfirmPanel.SetActive(false);
        targetKickPlayerName = "";
    }

    void OnCancelKickClicked()
    {
        if (kickConfirmPanel) kickConfirmPanel.SetActive(false);
        targetKickPlayerName = "";
    }

    IEnumerator KickPlayerAfterDelay(Player targetPlayer)
    {
        yield return new WaitForSeconds(1f);

        if (PhotonNetwork.IsMasterClient && targetPlayer != null)
        {
            // 방에서 추방
            PhotonNetwork.CloseConnection(targetPlayer);
        }
    }

    [PunRPC]
    void NotifyPlayerKicked(string kickedPlayerName)
    {
        ShowNotification($"방장에 의해 추방되었습니다.", NotificationType.Error);

        // 3초 후 자동으로 방 나가기
        StartCoroutine(LeaveRoomAfterKick());
    }

    IEnumerator LeaveRoomAfterKick()
    {
        yield return new WaitForSeconds(3f);
        PhotonNetwork.LeaveRoom();
    }

    #region Notification System
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

        switch (type)
        {
            case NotificationType.Success:
                notificationBackground.color = successColor;
                break;
            case NotificationType.Error:
                notificationBackground.color = dangerColor;
                break;
            case NotificationType.Warning:
                notificationBackground.color = new Color(1f, 0.65f, 0f);
                break;
            default:
                notificationBackground.color = secondaryColor;
                break;
        }

        notificationText.text = message;
        notificationText.color = Color.white;

        RectTransform rectTransform = notificationPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float textWidth = message.Length * 12f;
            float panelWidth = Mathf.Clamp(textWidth, 300f, 600f);
            rectTransform.sizeDelta = new Vector2(panelWidth, 80f);
        }

        notificationPanel.SetActive(true);

        notificationPanel.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        float timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(0.8f, 1f, timer / 0.3f);
            notificationPanel.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        yield return new WaitForSeconds(3f);

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

    #region Loading System
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

    #region Button Events
    void OnPlayButtonClicked()
    {
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            ShowNotification("플레이어 이름을 입력해주세요!", NotificationType.Error);
            return;
        }

        if (nickname.Length > 12)
        {
            ShowNotification("이름은 12자 이하로 입력해주세요!", NotificationType.Error);
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
        roomNameInput.Select();
    }

    void OnJoinRoomButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedRoomName))
        {
            ShowNotification("참가할 방을 선택해주세요!", NotificationType.Warning);
            return;
        }

        ShowLoading(roomListLoadingUI, "방에 참가하는 중...");
        PhotonNetwork.JoinRoom(selectedRoomName);
    }

    void OnRefreshButtonClicked()
    {
        ShowLoading(roomListLoadingUI, "방 목록을 새로고침하는 중...");

        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }
        roomUIItems.Clear();
        roomListDictionary.Clear();
        selectedRoomName = "";

        PhotonNetwork.LeaveLobby();
        StartCoroutine(RejoinLobbyAfterDelay());
    }

    IEnumerator RejoinLobbyAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.JoinLobby();
    }

    // B2: 수정된 방 생성 함수 (비공개 방 + 최대 인원 지원)
    void OnConfirmCreateButtonClicked()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            ShowNotification("방 이름을 입력해주세요!", NotificationType.Error);
            return;
        }

        if (roomName.Length > 20)
        {
            ShowNotification("방 이름은 20자 이하로 입력해주세요!", NotificationType.Error);
            return;
        }

        bool isPrivate = privateRoomToggle != null && privateRoomToggle.isOn;
        string password = "";

        if (isPrivate)
        {
            password = roomPasswordInput.text.Trim();
            if (string.IsNullOrEmpty(password))
            {
                ShowNotification("비공개 방은 비밀번호가 필요합니다!", NotificationType.Error);
                return;
            }
            if (password.Length < 4)
            {
                ShowNotification("비밀번호는 4자 이상 입력해주세요!", NotificationType.Error);
                return;
            }
        }

        RoomOptions roomOptions = new RoomOptions();

        int maxPlayers = 4;
        if (maxPlayersDropdown != null)
        {
            maxPlayers = maxPlayersDropdown.value + 2;
        }
        roomOptions.MaxPlayers = (byte)maxPlayers;

        roomOptions.IsVisible = !isPrivate;
        roomOptions.IsOpen = true;

        if (isPrivate)
        {
            roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
            roomOptions.CustomRoomProperties["password"] = password;
            roomOptions.CustomRoomProperties["isPrivate"] = true;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "isPrivate" };
        }

        ShowLoading(createLoadingUI, "방을 생성하는 중...");
        PhotonNetwork.CreateRoom(roomName, roomOptions);

        Debug.Log($"{roomName} 방 생성 시도: {(isPrivate ? "비공개" : "공개")}, 최대 {maxPlayers}명");
    }

    void OnCancelCreateButtonClicked()
    {
        createRoomPanel.SetActive(false);
        roomNameInput.text = "";
    }

    // B2: 수정된 게임 시작 함수 (준비 상태 체크)
    void OnStartGameButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            ShowNotification("방장만 게임을 시작할 수 있습니다!", NotificationType.Error);
            return;
        }

        if (!AreAllPlayersReady())
        {
            ShowNotification("모든 플레이어가 준비 완료해야 게임을 시작할 수 있습니다!", NotificationType.Warning);
            return;
        }

        ShowNotification("게임을 시작합니다!", NotificationType.Success);
        PhotonNetwork.LoadLevel("GameScene");
    }

    void OnLeaveRoomButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
        ShowNotification("방에서 나왔습니다.", NotificationType.Info);
    }

    // B2: 비밀번호 관련 함수들
    void ShowPasswordPrompt(string roomName)
    {
        attemptingRoomName = roomName;

        if (passwordPromptPanel)
        {
            passwordPromptPanel.SetActive(true);

            if (joinPasswordInput)
            {
                joinPasswordInput.text = "";
                joinPasswordInput.Select();
            }
        }

        Debug.Log($"비공개 방 비밀번호 입력 요청: {roomName}");
    }

    void OnConfirmPasswordClicked()
    {
        string inputPassword = joinPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(inputPassword))
        {
            ShowNotification("비밀번호를 입력해주세요!", NotificationType.Error);
            return;
        }

        selectedRoomPassword = inputPassword;
        selectedRoomName = attemptingRoomName;

        if (passwordPromptPanel)
            passwordPromptPanel.SetActive(false);

        OnJoinRoomButtonClicked();
    }

    void OnCancelPasswordClicked()
    {
        if (passwordPromptPanel)
            passwordPromptPanel.SetActive(false);

        attemptingRoomName = "";
        selectedRoomPassword = "";
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
        HideLoading(loginLoadingUI);
        PhotonNetwork.JoinLobby();
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("연결 끊김: " + cause);
        HideLoading(loginLoadingUI);
        ShowNotification("서버 연결이 끊어졌습니다: " + cause, NotificationType.Error);

        loginPanel.SetActive(true);
        roomListPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
    }

    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    #endregion

    #region ILobbyCallbacks
    public void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료!");
        HideLoading(roomListLoadingUI);

        loginPanel.SetActive(false);
        roomListPanel.SetActive(true);

        ShowNotification("로비에 입장했습니다!", NotificationType.Success);
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

    #region IMatchmakingCallbacks
    public void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공!");
        HideLoading(createLoadingUI);
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 생성 실패: " + message);
        HideLoading(createLoadingUI);
        ShowNotification("방 생성에 실패했습니다: " + message, NotificationType.Error);
    }

    // B2: 수정된 방 입장 함수 (비밀번호 체크 + 준비 상태 초기화)
    public void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공!");
        HideLoading(roomListLoadingUI);

        if (!string.IsNullOrEmpty(selectedRoomPassword))
        {
            var roomPassword = PhotonNetwork.CurrentRoom.CustomProperties["password"].ToString();
            if (selectedRoomPassword != roomPassword)
            {
                ShowNotification("비밀번호가 틀렸습니다!", NotificationType.Error);
                PhotonNetwork.LeaveRoom();
                selectedRoomPassword = "";
                return;
            }
        }

        createRoomPanel.SetActive(false);
        roomListPanel.SetActive(false);
        roomPanel.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        playerReadyStates.Clear();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerReadyStates[player.NickName] = false;
        }

        UpdatePlayerList();
        roomNameInput.text = "";

        if (!string.IsNullOrEmpty(selectedRoomPassword))
        {
            ShowNotification("비공개 방에 입장했습니다!", NotificationType.Success);
            selectedRoomPassword = "";
        }
        else
        {
            ShowNotification("방에 입장했습니다!", NotificationType.Success);
        }

        if (autoRefreshCoroutine != null)
            StopCoroutine(autoRefreshCoroutine);
        autoRefreshCoroutine = StartCoroutine(AutoRefreshPlayerList());
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 입장 실패: " + message);
        HideLoading(roomListLoadingUI);

        if (!string.IsNullOrEmpty(selectedRoomPassword))
        {
            ShowNotification("비밀번호가 틀렸습니다!", NotificationType.Error);
            ShowPasswordPrompt(selectedRoomName);
        }
        else
        {
            ShowNotification($"방 참가에 실패했습니다: {message}", NotificationType.Error);
        }

        selectedRoomPassword = "";
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
        ShowNotification($"{newPlayer.NickName}님이 입장했습니다!", NotificationType.Success);
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"=== 플레이어 퇴장: {otherPlayer.NickName} ===");
        UpdatePlayerList();
        ShowNotification($"{otherPlayer.NickName}님이 퇴장했습니다.", NotificationType.Info);
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"=== 방장 변경: {newMasterClient.NickName} ===");
        UpdatePlayerList();

        if (PhotonNetwork.IsMasterClient)
            ShowNotification("당신이 새로운 방장이 되었습니다! 게임을 시작할 수 있습니다.", NotificationType.Success);
        else
            ShowNotification($"{newMasterClient.NickName}님이 새로운 방장이 되었습니다.", NotificationType.Info);
    }

    // B2: 플레이어 속성 업데이트 (준비 상태 동기화)
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("ready"))
        {
            bool isReady = (bool)changedProps["ready"];
            playerReadyStates[targetPlayer.NickName] = isReady;

            UpdatePlayerList();

            Debug.Log($"{targetPlayer.NickName} 준비 상태 변경: {isReady}");
        }
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    #endregion

    #region UI Helper Methods
    void UpdateRoomListUI(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                if (roomUIItems.ContainsKey(room.Name))
                {
                    Destroy(roomUIItems[room.Name]);
                    roomUIItems.Remove(room.Name);
                }
                roomListDictionary.Remove(room.Name);

                if (roomCreationTimes.ContainsKey(room.Name))
                {
                    roomCreationTimes.Remove(room.Name);
                }
            }
            else
            {
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
            roomItem = roomUIItems[room.Name];
        }
        else
        {
            roomItem = Instantiate(roomItemButton, roomListContent);
            roomUIItems[room.Name] = roomItem;

            if (!roomCreationTimes.ContainsKey(room.Name))
            {
                roomCreationTimes[room.Name] = System.DateTime.Now;
            }

            Button roomButton = roomItem.GetComponent<Button>();
            string roomName = room.Name;

            roomButton.onClick.RemoveAllListeners();
            roomButton.onClick.AddListener(() => SelectRoom(roomName));
        }

        Text[] texts = roomItem.GetComponentsInChildren<Text>();
        if (texts.Length >= 2)
        {
            string roomStatus = GetRoomStatusIcon(room);
            texts[0].text = $"{roomStatus} {room.Name}";
            texts[0].fontSize = 20;
            texts[0].fontStyle = FontStyle.Bold;
            texts[0].color = new Color(0.1f, 0.1f, 0.1f);

            string timeAgo = GetRoomAge(room.Name);
            string detailInfo = $"인원: {room.PlayerCount}/{room.MaxPlayers}명";

            if (!string.IsNullOrEmpty(timeAgo))
            {
                detailInfo += $"  {timeAgo}";
            }

            texts[1].text = detailInfo;
            texts[1].fontSize = 16;
            texts[1].fontStyle = FontStyle.Normal;
            texts[1].color = new Color(0.4f, 0.4f, 0.4f);
        }
        else if (texts.Length >= 1)
        {
            string roomStatus = GetRoomStatusIcon(room);
            string timeAgo = GetRoomAge(room.Name);

            string fullInfo = $"{roomStatus} {room.Name} (인원: {room.PlayerCount}/{room.MaxPlayers}명)";
            if (!string.IsNullOrEmpty(timeAgo))
            {
                fullInfo += $"  {timeAgo}";
            }

            texts[0].text = fullInfo;
            texts[0].fontSize = 18;
            texts[0].fontStyle = FontStyle.Bold;
            texts[0].color = new Color(0.1f, 0.1f, 0.1f);
        }
    }

    // B2: 수정된 방 상태 아이콘 (비공개 방 지원)
    string GetRoomStatusIcon(RoomInfo room)
    {
        if (room.CustomProperties != null && room.CustomProperties.ContainsKey("isPrivate"))
        {
            return "[비공개]";
        }

        if (!room.IsOpen)
            return "[잠김]";
        else if (room.PlayerCount >= room.MaxPlayers)
            return "[만원]";
        else if (room.PlayerCount == 0)
            return "[빈방]";
        else
            return "[입장가능]";
    }

    string GetRoomAge(string roomName)
    {
        if (!roomCreationTimes.ContainsKey(roomName))
            return "";

        var creationTime = roomCreationTimes[roomName];
        var timeSpan = System.DateTime.Now - creationTime;

        if (timeSpan.TotalMinutes < 1)
            return "방금 전";
        else if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}분 전";
        else if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}시간 전";
        else
            return $"{(int)timeSpan.TotalDays}일 전";
    }

    // B2: 수정된 방 선택 함수 (비공개 방 처리)
    void SelectRoom(string roomName)
    {
        if (roomListDictionary.ContainsKey(roomName))
        {
            RoomInfo room = roomListDictionary[roomName];
            bool isPrivateRoom = room.CustomProperties != null &&
                               room.CustomProperties.ContainsKey("isPrivate");

            if (isPrivateRoom)
            {
                ShowPasswordPrompt(roomName);
                return;
            }
        }

        SelectPublicRoom(roomName);
    }

    void SelectPublicRoom(string roomName)
    {
        foreach (var item in roomUIItems.Values)
        {
            if (item != null && item.gameObject != null)
            {
                Button button = item.GetComponent<Button>();
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
                    button.colors = colors;
                }
            }
        }

        if (roomUIItems.ContainsKey(roomName) && roomUIItems[roomName] != null)
        {
            GameObject selectedRoom = roomUIItems[roomName];
            Button button = selectedRoom.GetComponent<Button>();

            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = new Color(0.9f, 0.95f, 1f);
                colors.highlightedColor = new Color(0.8f, 0.9f, 1f);
                button.colors = colors;
            }
        }

        selectedRoomName = roomName;
        Debug.Log($"공개 방 선택: {roomName}");
    }

    // B2: 수정된 플레이어 목록 업데이트 (준비 상태 + 추방 버튼)
    void UpdatePlayerList()
    {
        if (PhotonNetwork.CurrentRoom == null || !PhotonNetwork.InRoom || playerListContent == null)
            return;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        int playerIndex = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerItem = Instantiate(playerItemPrefab, playerListContent);

            RectTransform rectTransform = playerItem.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, -playerIndex * 60);
            }

            Text[] texts = playerItem.GetComponentsInChildren<Text>();
            if (texts.Length > 0)
            {
                bool isReady = GetPlayerReadyState(player.NickName);
                string readyIcon = isReady ? "✅" : "❌";

                string playerText = $"플레이어: {player.NickName}";

                if (player.IsMasterClient)
                {
                    playerText += " [방장]";
                }

                playerText += $" {readyIcon}";

                texts[0].text = playerText;

                if (player.IsMasterClient)
                {
                    Image bg = playerItem.GetComponent<Image>();
                    if (bg) bg.color = new Color(1f, 0.95f, 0.7f);

                    texts[0].color = new Color(0.8f, 0.5f, 0f);
                    texts[0].fontStyle = FontStyle.Bold;
                }
                else
                {
                    texts[0].color = Color.black;
                    texts[0].fontStyle = FontStyle.Normal;

                    Image bg = playerItem.GetComponent<Image>();
                    if (bg) bg.color = Color.white;
                }
            }

            // 추방 버튼 추가 (방장만, 자신 제외)
            if (PhotonNetwork.IsMasterClient && !player.IsMasterClient && kickButtonPrefab != null)
            {
                Button kickButton = Instantiate(kickButtonPrefab, playerItem.transform);

                RectTransform kickRect = kickButton.GetComponent<RectTransform>();
                if (kickRect != null)
                {
                    kickRect.anchorMin = new Vector2(1, 0.5f);
                    kickRect.anchorMax = new Vector2(1, 0.5f);
                    kickRect.anchoredPosition = new Vector2(-30, 0);
                    kickRect.sizeDelta = new Vector2(50, 30);
                }

                Text kickText = kickButton.GetComponentInChildren<Text>();
                if (kickText != null)
                {
                    kickText.text = "👢";
                    kickText.fontSize = 18;
                }

                ApplyButtonColors(kickButton, dangerColor);

                string playerName = player.NickName;
                kickButton.onClick.AddListener(() => ShowKickConfirmation(playerName));
            }

            playerIndex++;
        }

        if (playersText != null)
            playersText.text = $"현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/4명";

        UpdateReadyButton();
        UpdateGameStartCondition();

        Debug.Log($"플레이어 목록 업데이트 완료: {PhotonNetwork.CurrentRoom.PlayerCount}명");
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