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

    [Header("💬 채팅 시스템")]
    public GameObject chatPanel;            // 채팅 전체 패널
    public ScrollRect chatScrollRect;       // 채팅 스크롤뷰
    public Transform chatContent;           // 채팅 메시지들이 들어갈 Content
    public InputField chatInputField;       // 채팅 입력창
    public Button chatSendButton;           // 채팅 전송 버튼
    public GameObject chatMessagePrefab;    // 채팅 메시지 프리펩
    public Button[] quickMessageButtons;    // 빠른 메시지 버튼들 (선택사항)

    [Header("📢 Notification System")]
    public GameObject notificationPanel;
    public Text notificationText;
    public Image notificationBackground;

    [Header("🎨 UI Colors")]
    public Color primaryColor = new Color(1f, 0.39f, 0.28f); // #FF6347
    public Color secondaryColor = new Color(0.25f, 0.41f, 0.88f); // #4169E1
    public Color successColor = new Color(0.2f, 0.8f, 0.2f); // #32CD32
    public Color dangerColor = new Color(0.86f, 0.08f, 0.24f); // #DC143C
    public Color chatMasterColor = new Color(1f, 0.8f, 0.2f); // 방장 채팅 색상
    public Color chatSystemColor = new Color(0.7f, 0.7f, 0.7f); // 시스템 메시지 색상

    [Header("🎨 플레이어 색상 선택")]
    public GameObject colorSelectionPanel;          // 색상 선택 패널
    public Button[] colorButtons;                   // 색상 버튼들 (8개)
    public Button openColorPanelButton;             // 색상 선택 패널 열기 버튼
    public Button closeColorPanelButton;            // 색상 선택 패널 닫기 버튼
    public Image myColorPreview;                    // 내 색상 미리보기
    public Text myColorText;                        // 내 색상 텍스트

    // 웜즈 스타일 색상 팔레트 (8색)
    private Color[] playerColors = new Color[]
    {
    new Color(1f, 0.2f, 0.2f),      // 빨강 (Red)
    new Color(0.2f, 0.4f, 1f),      // 파랑 (Blue) 
    new Color(0.2f, 0.8f, 0.2f),    // 초록 (Green)
    new Color(1f, 0.8f, 0.2f),      // 노랑 (Yellow)
    new Color(0.8f, 0.2f, 0.8f),    // 보라 (Purple)
    new Color(1f, 0.5f, 0.2f),      // 주황 (Orange)
    new Color(0.2f, 0.8f, 0.8f),    // 하늘 (Cyan)
    new Color(0.8f, 0.8f, 0.8f)     // 회색 (Gray)
    };

    private string[] colorNames = new string[]
    {
    "빨강", "파랑", "초록", "노랑", "보라", "주황", "하늘", "회색"
    };

    private int mySelectedColor = -1;               // 내가 선택한 색상 인덱스
    private Dictionary<string, int> playerColorMap = new Dictionary<string, int>(); // 플레이어별 색상
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

    // 채팅 시스템 변수들
    private List<GameObject> chatMessages = new List<GameObject>();
    private float lastMessageTime = 0f;
    private const float MESSAGE_COOLDOWN = 1f; // 도배 방지: 1초마다 1개 메시지
    private const int MAX_MESSAGE_LENGTH = 100; // 최대 메시지 길이
    private const int MAX_CHAT_MESSAGES = 50; // 최대 채팅 메시지 개수

    // 빠른 메시지들
    private string[] quickMessages = {
        "준비됐어요! 👍",
        "잠깐만요 ⏰",
        "시작하자! 🚀",
        "ㅋㅋㅋㅋ 😂",
        "좋네요! 👌"
    };

    void Start()
    {
        InitializeUI();
        SetupButtonEvents();
        ApplyUIColors();
        SetupAdvancedLobbySystem();
        SetupChatSystem(); // 채팅 시스템 초기화
        SetupColorSystem();
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

        // 채팅 패널 초기화
        if (chatPanel) chatPanel.SetActive(false);
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

        // 채팅 전송 버튼 색상
        if (chatSendButton != null)
            ApplyButtonColors(chatSendButton, primaryColor);

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

    // 💬 채팅 시스템 초기화
    void SetupChatSystem()
    {
        // 채팅 전송 버튼 이벤트
        if (chatSendButton)
            chatSendButton.onClick.AddListener(OnChatSendButtonClicked);

        // 채팅 입력창 Enter 키 이벤트
        if (chatInputField)
        {
            chatInputField.onEndEdit.AddListener(delegate
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnChatSendButtonClicked();
                }
            });
        }

        // 빠른 메시지 버튼들 설정
        if (quickMessageButtons != null)
        {
            for (int i = 0; i < quickMessageButtons.Length && i < quickMessages.Length; i++)
            {
                if (quickMessageButtons[i] != null)
                {
                    int index = i; // 클로저 문제 해결
                    quickMessageButtons[i].onClick.AddListener(() => SendQuickMessage(index));

                    // 버튼 텍스트 설정
                    Text btnText = quickMessageButtons[i].GetComponentInChildren<Text>();
                    if (btnText != null)
                        btnText.text = quickMessages[index];
                }
            }
        }

        Debug.Log("채팅 시스템 초기화 완료");
    }

    // 💬 채팅 메시지 전송 버튼 클릭
    void OnChatSendButtonClicked()
    {
        Debug.Log("🔥 채팅 전송 버튼 클릭됨!");

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("❌ 방에 있지 않음!");
            return;
        }
        Debug.Log("✅ 방에 있음!");

        string message = chatInputField.text.Trim();
        Debug.Log($"📝 입력된 메시지: '{message}'");

        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("❌ 메시지가 비어있음!");
            return;
        }
        Debug.Log("✅ 메시지가 유효함!");

        // 메시지 길이 제한
        if (message.Length > MAX_MESSAGE_LENGTH)
        {
            Debug.LogError($"❌ 메시지가 너무 김! ({message.Length}자)");
            ShowNotification($"메시지는 {MAX_MESSAGE_LENGTH}자 이하로 입력해주세요!", NotificationType.Warning);
            return;
        }
        Debug.Log("✅ 메시지 길이 OK!");

        // 도배 방지
        if (Time.time - lastMessageTime < MESSAGE_COOLDOWN)
        {
            Debug.LogError("❌ 도배 방지 작동!");
            ShowNotification("너무 빠르게 메시지를 보내고 있습니다!", NotificationType.Warning);
            return;
        }
        Debug.Log("✅ 도배 방지 통과!");

        // 메시지 전송
        Debug.Log("🚀 SendChatMessage 호출!");
        SendChatMessage(message);

        // 입력창 초기화
        chatInputField.text = "";
        chatInputField.Select();
        chatInputField.ActivateInputField();

        lastMessageTime = Time.time;
        Debug.Log("✅ 채팅 전송 완료!");
    }

    // 💬 빠른 메시지 전송
    void SendQuickMessage(int index)
    {
        if (!PhotonNetwork.InRoom) return;
        if (index < 0 || index >= quickMessages.Length) return;

        // 도배 방지
        if (Time.time - lastMessageTime < MESSAGE_COOLDOWN)
        {
            ShowNotification("너무 빠르게 메시지를 보내고 있습니다!", NotificationType.Warning);
            return;
        }

        SendChatMessage(quickMessages[index]);
        lastMessageTime = Time.time;
    }

    // 💬 채팅 메시지 전송 (RPC)
    void SendChatMessage(string message)
    {
        if (!PhotonNetwork.InRoom) return;

        string playerName = PhotonNetwork.LocalPlayer.NickName;
        bool isMaster = PhotonNetwork.IsMasterClient;

        // RPC로 모든 플레이어에게 메시지 전송
        photonView.RPC("ReceiveChatMessage", RpcTarget.All, playerName, message, isMaster);
    }

    // 💬 채팅 메시지 수신 (RPC)
    [PunRPC]
    void ReceiveChatMessage(string playerName, string message, bool isMaster)
    {
        DisplayChatMessage(playerName, message, isMaster, false);
    }

    // 💬 시스템 메시지 표시
    void DisplaySystemMessage(string message)
    {
        DisplayChatMessage("시스템", message, false, true);
    }

    // 💬 채팅 메시지 UI에 표시
    void DisplayChatMessage(string playerName, string message, bool isMaster, bool isSystem)
    {
        if (chatContent == null || chatMessagePrefab == null) return;

        // 최대 메시지 개수 초과 시 오래된 메시지 제거
        while (chatMessages.Count >= MAX_CHAT_MESSAGES)
        {
            GameObject oldMessage = chatMessages[0];
            chatMessages.RemoveAt(0);
            if (oldMessage != null)
                Destroy(oldMessage);
        }

        // 새 메시지 생성
        GameObject newMessage = Instantiate(chatMessagePrefab, chatContent);
        chatMessages.Add(newMessage);

        // 메시지 텍스트 설정
        Text messageText = newMessage.GetComponentInChildren<Text>();
        if (messageText != null)
        {
            string displayMessage;
            Color textColor;

            if (isSystem)
            {
                displayMessage = $"[시스템] {message}";
                textColor = chatSystemColor;
            }
            else if (isMaster)
            {
                displayMessage = $"[방장] {playerName}: {message}";
                textColor = chatMasterColor;
            }
            else
            {
                displayMessage = $"{playerName}: {message}";
                textColor = Color.white;
            }

            messageText.text = displayMessage;
            messageText.color = textColor;
        }

        // 시간 텍스트 설정 (선택사항)
        Text[] allTexts = newMessage.GetComponentsInChildren<Text>();
        if (allTexts.Length > 1)
        {
            Text timeText = allTexts[1];
            timeText.text = System.DateTime.Now.ToString("HH:mm");
            timeText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        // 자동 스크롤
        StartCoroutine(ScrollToBottom());
    }

    // 💬 채팅창 맨 아래로 스크롤
    // 기존 ScrollToBottom 함수를 찾아서 이렇게 수정
    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
            Debug.Log("✅ 스크롤 완료!");
        }
    }

    // 💬 채팅창 초기화
    void ClearChatMessages()
    {
        foreach (GameObject message in chatMessages)
        {
            if (message != null)
                Destroy(message);
        }
        chatMessages.Clear();
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
        bool currentReady = GetPlayerReadyStateFromProps(myNickname);
        bool newReady = !currentReady;

        Debug.Log($"🔥 준비 버튼 클릭: {myNickname}, 현재: {currentReady} → 새로운: {newReady}");

        // 서버에 준비 상태 전송
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["ready"] = newReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // 로컬 저장도 업데이트
        playerReadyStates[myNickname] = newReady;

        string message = newReady ? "준비 완료!" : "준비 취소";
        ShowNotification(message, newReady ? NotificationType.Success : NotificationType.Info);

        Debug.Log($"✅ {myNickname} 준비 상태 변경 완료: {newReady}");
    }

    bool GetPlayerReadyStateFromProps(string playerName)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == playerName)
            {
                if (player.CustomProperties.ContainsKey("ready"))
                {
                    bool readyState = (bool)player.CustomProperties["ready"];
                    Debug.Log($"플레이어 {playerName} 서버 준비 상태: {readyState}");
                    return readyState;
                }
                break;
            }
        }

        Debug.Log($"플레이어 {playerName} 준비 상태 없음 → false");
        return false;
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

    // B2: 수정된 플레이어 준비 상태 가져오기 (방장 자동 준비)
    bool GetPlayerReadyState(string playerName)
    {
        // 1차: 서버의 실제 속성에서 가져오기
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == playerName)
            {
                if (player.CustomProperties.ContainsKey("ready"))
                {
                    bool serverReady = (bool)player.CustomProperties["ready"];

                    // 방장인 경우: 서버 상태가 false여도 true로 강제 (방장은 항상 준비)
                    if (player.IsMasterClient && !serverReady)
                    {
                        Debug.Log($"방장 {playerName} 강제 준비 완료 처리");
                        return true;
                    }

                    return serverReady;
                }

                // 방장이면서 속성이 없는 경우 → 자동 준비
                if (player.IsMasterClient)
                {
                    Debug.Log($"방장 {playerName} 자동 준비 완료");
                    return true;
                }

                break;
            }
        }

        // 2차: 로컬 저장소에서 확인
        if (playerReadyStates.ContainsKey(playerName))
        {
            return playerReadyStates[playerName];
        }

        Debug.Log($"플레이어 {playerName} 준비 상태 기본값: false");
        return false;
    }

    // B2: 모든 플레이어 준비 상태 체크
    bool AreAllPlayersReady()
    {
        if (!PhotonNetwork.InRoom) return false;

        Debug.Log("=== 모든 플레이어 준비 상태 체크 ===");

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            bool isReady = GetPlayerReadyState(player.NickName);
            Debug.Log($"플레이어 {player.NickName}: {(isReady ? "✅준비완료" : "❌준비안됨")} (방장: {player.IsMasterClient})");

            if (!isReady)
            {
                Debug.Log($"❌ {player.NickName}이 준비되지 않아서 게임 시작 불가");
                return false;
            }
        }

        bool canStart = PhotonNetwork.CurrentRoom.PlayerCount >= 2;
        Debug.Log($"최종 결과: {(canStart ? "✅모든 조건 만족" : "❌인원 부족")}");
        return canStart;
    }

    // B2: 수정된 게임 시작 조건 업데이트 (방장 자동 준비 반영)
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

        readyButton.gameObject.SetActive(!isMaster);

        if (!isMaster)
        {
            // 실제 서버 속성에서 상태 확인
            bool isReady = GetPlayerReadyStateFromProps(PhotonNetwork.LocalPlayer.NickName);

            Debug.Log($"준비 버튼 업데이트: {PhotonNetwork.LocalPlayer.NickName} → {(isReady ? "준비완료" : "준비안됨")}");

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

        roomOptions.IsVisible = true;
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
        if (chatPanel) chatPanel.SetActive(false);
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

    // B2: 수정된 방 입장 함수 (비밀번호 체크 + 준비 상태 초기화 + 방장 자동 준비 + 채팅 활성화)
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

        // 채팅 패널 활성화
        if (chatPanel) chatPanel.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        // 모든 플레이어 준비 상태 초기화
        playerReadyStates.Clear();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            // 방장은 자동으로 준비 완료, 다른 플레이어는 준비 안됨
            bool isReady = player.IsMasterClient;
            playerReadyStates[player.NickName] = isReady;

            // 자신이 방장이면 준비 상태를 서버에도 전송
            if (player.IsMasterClient && PhotonNetwork.LocalPlayer == player)
            {
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["ready"] = true;
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        }

        // 채팅 초기화 및 입장 메시지
        ClearChatMessages();
        DisplaySystemMessage($"{PhotonNetwork.LocalPlayer.NickName}님이 입장했습니다!");

        // 채팅 입력창에 포커스
        if (chatInputField)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField();
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

        // 채팅 패널 비활성화
        if (chatPanel) chatPanel.SetActive(false);
        ClearChatMessages();
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"=== 플레이어 입장: {newPlayer.NickName} ===");
        UpdatePlayerList();
        ShowNotification($"{newPlayer.NickName}님이 입장했습니다!", NotificationType.Success);

        // 채팅에 입장 메시지 표시
        DisplaySystemMessage($"{newPlayer.NickName}님이 입장했습니다!");
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"=== 플레이어 퇴장: {otherPlayer.NickName} ===");
        UpdatePlayerList();
        ShowNotification($"{otherPlayer.NickName}님이 퇴장했습니다.", NotificationType.Info);

        // 채팅에 퇴장 메시지 표시
        DisplaySystemMessage($"{otherPlayer.NickName}님이 퇴장했습니다.");
    }

    // 방장 변경 시에도 새 방장을 자동 준비 상태로 만들기
    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"=== 방장 변경: {newMasterClient.NickName} ===");

        // 새 방장을 자동으로 준비 상태로 설정
        playerReadyStates[newMasterClient.NickName] = true;

        // 자신이 새 방장이 된 경우 서버에도 준비 상태 전송
        if (PhotonNetwork.LocalPlayer == newMasterClient)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["ready"] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        UpdatePlayerList();

        // 채팅에 방장 변경 메시지
        DisplaySystemMessage($"{newMasterClient.NickName}님이 새로운 방장이 되었습니다!");

        if (PhotonNetwork.IsMasterClient)
            ShowNotification("당신이 새로운 방장이 되었습니다! 게임을 시작할 수 있습니다.", NotificationType.Success);
        else
            ShowNotification($"{newMasterClient.NickName}님이 새로운 방장이 되었습니다.", NotificationType.Info);
    }

    // B2: 플레이어 속성 업데이트 (준비 상태 동기화)
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 준비 상태 변경 처리
        if (changedProps.ContainsKey("ready"))
        {
            bool isReady = (bool)changedProps["ready"];
            playerReadyStates[targetPlayer.NickName] = isReady;

            Debug.Log($"🔄 서버에서 준비 상태 업데이트: {targetPlayer.NickName} → {(isReady ? "✅준비완료" : "❌준비안됨")}");

            UpdatePlayerList();
            UpdateReadyButton();

            // 상태 변경 알림
            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                string message = isReady ? $"{targetPlayer.NickName}님이 준비 완료했습니다!" : $"{targetPlayer.NickName}님이 준비를 취소했습니다.";
                ShowNotification(message, isReady ? NotificationType.Success : NotificationType.Info);

                // 채팅에도 표시
                string chatMessage = isReady ? "준비 완료!" : "준비 취소";
                DisplaySystemMessage($"{targetPlayer.NickName}님이 {chatMessage}");
            }
        }

        // 🔥 색상 변경 처리 (독립적인 블록으로 분리!)
        if (changedProps.ContainsKey("playerColor"))
        {
            int colorIndex = (int)changedProps["playerColor"];
            playerColorMap[targetPlayer.NickName] = colorIndex;

            UpdatePlayerList();

            // 색상 변경 알림 (다른 플레이어에게만)
            if (targetPlayer != PhotonNetwork.LocalPlayer)
            {
                string colorName = (colorIndex >= 0 && colorIndex < colorNames.Length) ? colorNames[colorIndex] : "알 수 없음";
                ShowNotification($"{targetPlayer.NickName}님이 {colorName} 색상을 선택했습니다!", NotificationType.Info);

                // 채팅에도 표시
                DisplaySystemMessage($"{targetPlayer.NickName}님이 {colorName} 색상을 선택했습니다!");
            }

            Debug.Log($"플레이어 {targetPlayer.NickName} 색상 변경: {colorIndex}");
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

    // 깔끔한 플레이어 목록 업데이트 (추방 버튼 제거)
    void UpdatePlayerList()
    {
        if (PhotonNetwork.CurrentRoom == null || !PhotonNetwork.InRoom || playerListContent == null)
            return;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"=== 플레이어 목록 업데이트 시작 ===");

        int playerIndex = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Debug.Log($"플레이어 {playerIndex + 1}: {player.NickName} (방장: {player.IsMasterClient})");

            GameObject playerItem = Instantiate(playerItemPrefab, playerListContent);

            if (playerItem == null)
            {
                Debug.LogError("❌ PlayerItem 생성 실패!");
                continue;
            }

            RectTransform rectTransform = playerItem.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, -playerIndex * 60);
            }

            // 플레이어 텍스트 설정
            Text[] allTexts = playerItem.GetComponentsInChildren<Text>(true);
            Text playerNameText = null;

            // 플레이어 이름 표시용 Text 찾기
            foreach (Text text in allTexts)
            {
                if (text.transform.parent == playerItem.transform)
                {
                    playerNameText = text;
                    break;
                }
            }

            if (playerNameText != null)
            {
                bool isReady = GetPlayerReadyState(player.NickName);
                string readyIcon = isReady ? "✅" : "❌";

                int playerColorIndex = -1;
                if (player.CustomProperties.ContainsKey("playerColor"))
                    playerColorIndex = (int)player.CustomProperties["playerColor"];

                string colorName = (playerColorIndex >= 0) ? colorNames[playerColorIndex] : "미선택";
                string playerText = $"[{colorName}] {player.NickName}";

                if (player.IsMasterClient)
                {
                    playerText += " [방장]";
                }

                playerText += $" {readyIcon}";
                playerNameText.text = playerText;

                if (player.IsMasterClient)
                {
                    Image bg = playerItem.GetComponent<Image>();
                    if (bg) bg.color = new Color(1f, 0.95f, 0.7f);

                    playerNameText.color = new Color(0.8f, 0.5f, 0f);
                    playerNameText.fontStyle = FontStyle.Bold;
                }
                else
                {
                    playerNameText.color = Color.black;
                    playerNameText.fontStyle = FontStyle.Normal;

                    Image bg = playerItem.GetComponent<Image>();
                    if (bg) bg.color = Color.white;
                }
            }

            // 모든 버튼 비활성화 (추방 버튼 제거)
            Button[] allButtons = playerItem.GetComponentsInChildren<Button>(true);
            foreach (Button btn in allButtons)
            {
                btn.gameObject.SetActive(false);
            }

            playerIndex++;
        }

        if (playersText != null)
            playersText.text = $"현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}/4명";

        UpdateReadyButton();
        UpdateGameStartCondition();

        Debug.Log($"=== 플레이어 목록 업데이트 완료 ===");
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

    // 색상 시스템 초기화
    void SetupColorSystem()
    {
        if (colorSelectionPanel) colorSelectionPanel.SetActive(false);
        if (openColorPanelButton) openColorPanelButton.onClick.AddListener(OpenColorSelection);
        if (closeColorPanelButton) closeColorPanelButton.onClick.AddListener(CloseColorSelection);

        for (int i = 0; i < colorButtons.Length && i < playerColors.Length; i++)
        {
            if (colorButtons[i] != null)
            {
                int colorIndex = i;
                ColorBlock colors = colorButtons[i].colors;
                colors.normalColor = playerColors[i];
                colors.highlightedColor = LightenColor(playerColors[i], 0.2f);
                colors.pressedColor = DarkenColor(playerColors[i], 0.2f);
                colorButtons[i].colors = colors;
                colorButtons[i].onClick.AddListener(() => SelectColor(colorIndex));

                Text btnText = colorButtons[i].GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = colorNames[i];
            }
        }

        if (myColorPreview) myColorPreview.color = Color.white;
        if (myColorText) myColorText.text = "색상 선택";

        Debug.Log("플레이어 색상 시스템 초기화 완료");
    }

    void OpenColorSelection()
    {
        if (!PhotonNetwork.InRoom)
        {
            ShowNotification("방에 입장한 후 색상을 선택할 수 있습니다!", NotificationType.Warning);
            return;
        }
        if (colorSelectionPanel) colorSelectionPanel.SetActive(true);
    }

    void CloseColorSelection()
    {
        if (colorSelectionPanel) colorSelectionPanel.SetActive(false);
    }

    void SelectColor(int colorIndex)
    {
        if (!PhotonNetwork.InRoom) return;

        if (IsColorTaken(colorIndex))
        {
            ShowNotification($"{colorNames[colorIndex]} 색상은 다른 플레이어가 사용 중입니다!", NotificationType.Warning);
            return;
        }

        mySelectedColor = colorIndex;
        playerColorMap[PhotonNetwork.LocalPlayer.NickName] = colorIndex;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["playerColor"] = colorIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (myColorPreview) myColorPreview.color = playerColors[mySelectedColor];
        if (myColorText) myColorText.text = $"내 색상: {colorNames[mySelectedColor]}";

        UpdatePlayerList();
        ShowNotification($"{colorNames[colorIndex]} 색상을 선택했습니다!", NotificationType.Success);
    }

    bool IsColorTaken(int colorIndex)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("playerColor"))
            {
                int playerColorIndex = (int)player.CustomProperties["playerColor"];
                if (playerColorIndex == colorIndex && player != PhotonNetwork.LocalPlayer)
                    return true;
            }
        }
        return false;
    }

    public int GetMyColorIndex()
    {
        return mySelectedColor;
    }

    public Color GetPlayerColorAsColor(string playerName)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.NickName == playerName && player.CustomProperties.ContainsKey("playerColor"))
            {
                int colorIndex = (int)player.CustomProperties["playerColor"];
                if (colorIndex >= 0 && colorIndex < playerColors.Length)
                    return playerColors[colorIndex];
            }
        }
        return Color.white;
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