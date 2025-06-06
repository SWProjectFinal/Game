using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class GameUIManager : MonoBehaviourPun
{
    public static GameUIManager Instance { get; private set; }

    [Header("=== 캐릭터 상태 패널 ===")]
    public GameObject playerStatusPanel;
    public Text playerStatusTitle;
    public Transform playerListParent;
    public GameObject playerItemPrefab;

    [Header("=== 타이머 패널 ===")]
    public GameObject timerPanel;
    public Text timerTitle;
    public Text timerText;
    public Image timerProgressBar;
    public Color normalTimerColor = Color.green;
    public Color urgentTimerColor = Color.red;

    [Header("=== 채팅 패널 ===")]
    public GameObject chatPanel;
    public Text chatTitle;
    public ScrollRect chatScrollRect;
    public Transform chatContent;
    public InputField chatInputField;
    public Button chatSendButton;
    public GameObject chatMessagePrefab;

    [Header("=== 게임 상태 ===")]
    public GameObject gameOverPanel;
    public Text winnerText;
    public Button backToLobbyButton;

    [Header("=== UI 스타일 ===")]
    public Color activePlayerColor = Color.green;
    public Color inactivePlayerColor = Color.gray;
    public Color panelBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    // 개선된 플레이어 상태 아이템 클래스
    [System.Serializable]
    public class PlayerStatusItem
    {
        public GameObject itemObject;
        public Text nameText;                   // 플레이어 이름 (색상 적용됨)
        public Image indicatorDot;              // 왼쪽 차례 표시 점
        public Image backgroundImage;           // 배경 이미지
        public Slider healthBar;                // 체력바 (추가)
        public string playerName;
        public bool isBot;
        public bool isActive;
        public Color playerColor;               // 플레이어 색상 저장

        public void SetActive(bool active)
        {
            // 차례 표시 점
            if (indicatorDot != null)
            {
                indicatorDot.color = active ? GameUIManager.Instance.activePlayerColor : GameUIManager.Instance.inactivePlayerColor;
                indicatorDot.gameObject.SetActive(active);
            }

            // 텍스트 Bold 처리 및 색상 강화
            if (nameText != null)
            {
                nameText.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                nameText.fontSize = active ? 18 : 16;

                // 현재 차례일 때 색상 더 밝게, 아닐 때 어둡게
                if (active)
                {
                    nameText.color = playerColor; // 선택한 색상 그대로
                }
                else
                {
                    // 비활성일 때 색상을 어둡게
                    nameText.color = new Color(playerColor.r * 0.6f, playerColor.g * 0.6f, playerColor.b * 0.6f, 1f);
                }
            }

            // 배경 강조
            if (backgroundImage != null)
            {
                if (active)
                {
                    // 현재 차례일 때 플레이어 색상으로 배경 약간 변경
                    Color bgColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.3f);
                    backgroundImage.color = bgColor;
                }
                else
                {
                    backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.4f);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (itemObject != null)
            {
                itemObject.SetActive(visible);
            }
            isActive = visible;
        }

        public void UpdateHealth(float healthPercentage)
        {
            if (healthBar != null)
            {
                healthBar.value = healthPercentage;

                // 체력에 따른 색상 변경
                Image fillImage = healthBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (healthPercentage > 70f)
                        fillImage.color = Color.green;
                    else if (healthPercentage > 30f)
                        fillImage.color = Color.yellow;
                    else
                        fillImage.color = Color.red;
                }
            }
        }
    }

    private PlayerStatusItem[] playerStatusItems = new PlayerStatusItem[4];
    private List<GameObject> chatMessages = new List<GameObject>();
    private float currentTurnTime;
    private float maxTurnTime;
    private bool isTimerActive = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeUI();
        ConnectToTurnManager();
    }

    void InitializeUI()
    {
        SetupPanels();
        SetupChatSystem();

        if (backToLobbyButton)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);

        Debug.Log("GameUIManager 초기화 완료!");
    }

    void SetupPanels()
    {
        if (playerStatusPanel)
        {
            playerStatusPanel.SetActive(true);
            Image panelBg = playerStatusPanel.GetComponent<Image>();
            if (panelBg) panelBg.color = panelBackgroundColor;
        }

        if (timerPanel)
        {
            timerPanel.SetActive(true);
            Image timerBg = timerPanel.GetComponent<Image>();
            if (timerBg) timerBg.color = panelBackgroundColor;
        }

        if (chatPanel)
        {
            chatPanel.SetActive(true);
            Image chatBg = chatPanel.GetComponent<Image>();
            if (chatBg) chatBg.color = panelBackgroundColor;
        }

        if (playerStatusTitle) playerStatusTitle.text = "캐릭터 상태";
        if (timerTitle) timerTitle.text = "타이머";
        if (chatTitle) chatTitle.text = "채팅";

        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    void SetupChatSystem()
    {
        if (chatSendButton)
            chatSendButton.onClick.AddListener(OnChatSendClicked);

        if (chatInputField)
        {
            chatInputField.onEndEdit.AddListener(delegate
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnChatSendClicked();
            });
        }
    }

    void ConnectToTurnManager()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart += OnTurnStarted;
            TurnManager.Instance.OnTurnEnd += OnTurnEnded;
            TurnManager.Instance.OnTurnTimeUpdate += OnTurnTimeUpdated;

            StartCoroutine(WaitAndInitializePlayerList());
        }
        else
        {
            StartCoroutine(WaitForTurnManager());
        }
    }

    IEnumerator WaitForTurnManager()
    {
        while (TurnManager.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        ConnectToTurnManager();
    }

    IEnumerator WaitAndInitializePlayerList()
    {
        yield return new WaitForSeconds(1f);
        InitializePlayerList();
    }

    public void InitializePlayerList()
    {
        if (TurnManager.Instance == null || playerListParent == null || playerItemPrefab == null)
        {
            Debug.LogError("플레이어 목록 초기화 실패!");
            return;
        }

        HideAllPlayerItems();

        var allPlayers = TurnManager.Instance.allPlayers;
        Debug.Log($"플레이어 목록 초기화: {allPlayers.Count}명");

        int playerCount = Mathf.Min(allPlayers.Count, 4);

        for (int i = 0; i < playerCount; i++)
        {
            string playerName = allPlayers[i];
            bool isBot = TurnManager.Instance.botPlayers.Contains(playerName);

            CreateOrUpdatePlayerItem(i, playerName, isBot);
        }
    }

    void CreateOrUpdatePlayerItem(int slotIndex, string playerName, bool isBot)
    {
        PlayerStatusItem statusItem = playerStatusItems[slotIndex];

        if (statusItem == null)
        {
            GameObject itemObj = Instantiate(playerItemPrefab, playerListParent);

            statusItem = new PlayerStatusItem();
            statusItem.itemObject = itemObj;

            // ✅ 개선된 UI 컴포넌트 찾기
            statusItem.nameText = FindInChildren<Text>(itemObj, "PlayerNameText");
            statusItem.indicatorDot = FindInChildren<Image>(itemObj, "IndicatorDot");
            statusItem.backgroundImage = itemObj.GetComponent<Image>();
            statusItem.healthBar = FindInChildren<Slider>(itemObj, "HealthBar");

            // null 체크 및 대안
            if (statusItem.nameText == null)
                statusItem.nameText = itemObj.GetComponentInChildren<Text>();

            playerStatusItems[slotIndex] = statusItem;
        }

        // 플레이어 정보 업데이트
        statusItem.playerName = playerName;
        statusItem.isBot = isBot;
        statusItem.playerColor = GetPlayerColor(playerName, isBot);

        // UI 업데이트
        if (statusItem.nameText != null)
        {
            string displayName = isBot ? $"{playerName} (AI)" : playerName;
            statusItem.nameText.text = displayName;
            statusItem.nameText.color = statusItem.playerColor; // ✅ 이름 색상 적용
        }

        if (statusItem.indicatorDot != null)
        {
            statusItem.indicatorDot.gameObject.SetActive(false);
        }

        if (statusItem.healthBar != null)
        {
            statusItem.UpdateHealth(100f); // 초기 체력 100%
        }

        statusItem.SetVisible(true);
        statusItem.SetActive(false);

        Debug.Log($"플레이어 슬롯 {slotIndex}: {playerName} ({(isBot ? "AI" : "Player")}) - 색상: {statusItem.playerColor}");
    }

    // ✅ 자식 오브젝트에서 컴포넌트 찾는 헬퍼 함수
    T FindInChildren<T>(GameObject parent, string childName) where T : Component
    {
        Transform child = parent.transform.Find(childName);
        if (child == null)
        {
            // 직접 찾기 실패 시 재귀적으로 찾기
            child = FindChildRecursive(parent.transform, childName);
        }

        return child?.GetComponent<T>();
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    void HideAllPlayerItems()
    {
        for (int i = 0; i < playerStatusItems.Length; i++)
        {
            if (playerStatusItems[i] != null)
            {
                playerStatusItems[i].SetVisible(false);
                playerStatusItems[i].SetActive(false);
            }
        }
    }

    Color GetPlayerColor(string playerName, bool isBot)
    {
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            Color color = isBot ? lobbyManager.GetBotColorAsColor(playerName) : lobbyManager.GetPlayerColorAsColor(playerName);

            // 색상이 너무 어두우면 밝게 조정
            if (color.r + color.g + color.b < 1.5f)
            {
                color = new Color(
                    Mathf.Max(color.r, 0.5f),
                    Mathf.Max(color.g, 0.5f),
                    Mathf.Max(color.b, 0.5f),
                    1f
                );
            }

            return color;
        }
        return isBot ? Color.gray : Color.white;
    }

    // === 턴 이벤트 처리 ===

    void OnTurnStarted(Photon.Realtime.Player currentPlayer)
    {
        string currentPlayerName = currentPlayer?.NickName ??
                                 TurnManager.Instance.allPlayers[TurnManager.Instance.currentPlayerIndex];

        UpdateCurrentPlayerDisplay(currentPlayerName);

        maxTurnTime = TurnManager.Instance.turnDuration;
        currentTurnTime = maxTurnTime;
        isTimerActive = true;
    }

    void OnTurnEnded(Photon.Realtime.Player currentPlayer)
    {
        isTimerActive = false;
    }

    void Update()
    {
        // TurnManager의 시간을 직접 체크해서 UI 업데이트
        if (TurnManager.Instance != null && TurnManager.Instance.isGameActive)
        {
            UpdateTimerDisplay();
        }
    }

    void OnTurnTimeUpdated(float timeRemaining)
    {
        currentTurnTime = timeRemaining;
        // UpdateTimerDisplay(); // ← 이 줄 제거 (Update에서 처리)
    }

    void UpdateCurrentPlayerDisplay(string currentPlayerName)
    {
        for (int i = 0; i < playerStatusItems.Length; i++)
        {
            if (playerStatusItems[i] != null && playerStatusItems[i].isActive)
            {
                bool isCurrentPlayer = playerStatusItems[i].playerName == currentPlayerName;
                playerStatusItems[i].SetActive(isCurrentPlayer);
            }
        }
    }

    void UpdateTimerDisplay()
    {
        // TurnManager에서 직접 시간 가져오기
        if (TurnManager.Instance != null)
        {
            currentTurnTime = TurnManager.Instance.currentTurnTime;
        }

        if (!isTimerActive && !TurnManager.Instance.isGameActive) return;

        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTurnTime);
            timerText.text = $"{seconds}초";
            timerText.color = seconds <= 5 ? urgentTimerColor : normalTimerColor;
        }

        if (timerProgressBar != null && maxTurnTime > 0)
        {
            float fillAmount = currentTurnTime / maxTurnTime;
            timerProgressBar.fillAmount = fillAmount;
            timerProgressBar.color = currentTurnTime <= 5f ? urgentTimerColor : normalTimerColor;
        }
    }

    // === 체력 업데이트 (친구1과 연결용) ===
    public void UpdatePlayerHealth(string playerName, float healthPercentage)
    {
        for (int i = 0; i < playerStatusItems.Length; i++)
        {
            if (playerStatusItems[i] != null &&
                playerStatusItems[i].isActive &&
                playerStatusItems[i].playerName == playerName)
            {
                playerStatusItems[i].UpdateHealth(healthPercentage);
                Debug.Log($"{playerName} 체력 업데이트: {healthPercentage}%");
                break;
            }
        }
    }

    // === 채팅 시스템 ===

    void OnChatSendClicked()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text.Trim())) return;

        string message = chatInputField.text.Trim();
        string playerName = PhotonNetwork.LocalPlayer.NickName;

        photonView.RPC("ReceiveChatMessage", RpcTarget.All, playerName, message);

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    [PunRPC]
    void ReceiveChatMessage(string playerName, string message)
    {
        DisplayChatMessage($"{playerName}: {message}");
    }

    void DisplayChatMessage(string message)
    {
        if (chatContent == null) return;

        while (chatMessages.Count >= 20)
        {
            GameObject oldMsg = chatMessages[0];
            chatMessages.RemoveAt(0);
            if (oldMsg != null) Destroy(oldMsg);
        }

        GameObject newMsg;
        if (chatMessagePrefab != null)
        {
            newMsg = Instantiate(chatMessagePrefab, chatContent);
        }
        else
        {
            newMsg = new GameObject("ChatMessage");
            newMsg.transform.SetParent(chatContent);
            Text msgText = newMsg.AddComponent<Text>();
            msgText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            msgText.fontSize = 14;
            msgText.color = Color.white;
        }

        Text textComponent = newMsg.GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = message;
        }

        chatMessages.Add(newMsg);

        if (chatScrollRect != null)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // === 게임 종료 ===

    public void ShowGameOver(string winnerName)
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (winnerText) winnerText.text = $"🏆 {winnerName} 승리!";
        isTimerActive = false;
    }

    void OnBackToLobbyClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("LobbyScene");
        }
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnd -= OnTurnEnded;
            TurnManager.Instance.OnTurnTimeUpdate -= OnTurnTimeUpdated;
        }
    }
}