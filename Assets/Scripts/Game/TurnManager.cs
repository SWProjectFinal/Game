using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class TurnManager : MonoBehaviourPun, IPunObservable
{
    public static TurnManager Instance { get; private set; }

    [Header("턴 설정")]
    public float turnDuration = 20f; // 기본 턴 시간
    public float itemUseTurnDuration = 5f; // 아이템 사용 후 턴 시간

    [Header("플레이어 관리")]
    public List<Photon.Realtime.Player> players = new List<Photon.Realtime.Player>();
    public List<string> botPlayers = new List<string>(); // 봇 플레이어 목록
    public List<string> allPlayers = new List<string>(); // 실제 플레이어 + 봇 통합 목록
    public int currentPlayerIndex = 0;

    [Header("턴 상태")]
    public bool isGameActive = false;
    public float currentTurnTime;
    public bool isItemUsed = false;

    // 이벤트
    public System.Action<Photon.Realtime.Player> OnTurnStart;
    public System.Action<Photon.Realtime.Player> OnTurnEnd;
    public System.Action<float> OnTurnTimeUpdate;

    // 플레이어 움직임 제어 이벤트 (친구1이 구독할 이벤트)
    public static System.Action<bool> OnPlayerMovementChanged;

    private Coroutine turnTimerCoroutine;

    void Awake()
    {
        // 싱글톤 패턴
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
        // 마스터 클라이언트만 게임 로직 실행
        if (PhotonNetwork.IsMasterClient)
        {
            InitializePlayers();
            // 봇이 추가될 때까지 대기 (PlayerSpawner에서 AddBots 호출할 예정)
            Debug.Log("플레이어 초기화 완료. 봇 추가 대기 중...");
        }
    }

    void InitializePlayers()
    {
        // 현재 방에 있는 모든 플레이어를 가져와서 랜덤 순서로 섞기
        var photonPlayers = PhotonNetwork.PlayerList.ToList();

        // 랜덤으로 섞기 (Fisher-Yates 셔플)
        for (int i = 0; i < photonPlayers.Count; i++)
        {
            var temp = photonPlayers[i];
            int randomIndex = Random.Range(i, photonPlayers.Count);
            photonPlayers[i] = photonPlayers[randomIndex];
            photonPlayers[randomIndex] = temp;
        }

        players = photonPlayers;

        // 통합 플레이어 목록 생성 (실제 플레이어들만 일단 추가)
        allPlayers.Clear();
        foreach (var player in players)
        {
            allPlayers.Add(player.NickName);
        }

        currentPlayerIndex = 0;

        Debug.Log($"플레이어 턴 순서 초기화 완료. 총 {players.Count}명 (봇은 나중에 추가됨)");
        for (int i = 0; i < players.Count; i++)
        {
            Debug.Log($"{i + 1}번째: {players[i].NickName}");
        }
    }

    // 봇들을 턴 시스템에 추가
    public void AddBots(List<string> botNames)
    {
        botPlayers = new List<string>(botNames);

        // 통합 플레이어 목록에 봇들 추가
        foreach (string botName in botNames)
        {
            allPlayers.Add(botName);
        }

        // 통합 목록을 랜덤으로 섞기
        for (int i = 0; i < allPlayers.Count; i++)
        {
            string temp = allPlayers[i];
            int randomIndex = Random.Range(i, allPlayers.Count);
            allPlayers[i] = allPlayers[randomIndex];
            allPlayers[randomIndex] = temp;
        }

        Debug.Log($"🤖 봇 {botNames.Count}개 추가 완료!");
        Debug.Log($"📋 최종 턴 순서 (총 {allPlayers.Count}명):");
        for (int i = 0; i < allPlayers.Count; i++)
        {
            string playerType = IsBot(allPlayers[i]) ? "[봇]" : "[플레이어]";
            Debug.Log($"{i + 1}번째: {allPlayers[i]} {playerType}");
        }

        // 턴 시스템 재시작
        currentPlayerIndex = 0;
    }

    // 해당 이름이 봇인지 확인
    bool IsBot(string playerName)
    {
        return botPlayers.Contains(playerName);
    }

    // 현재 턴 플레이어 이름 가져오기
    string GetCurrentPlayerName()
    {
        if (allPlayers.Count == 0) return "";
        return allPlayers[currentPlayerIndex];
    }

    // 현재 턴이 봇인지 확인
    bool IsCurrentTurnBot()
    {
        string currentPlayerName = GetCurrentPlayerName();
        return IsBot(currentPlayerName);
    }

    // 이름으로 플레이어 찾기
    Photon.Realtime.Player GetPlayerByName(string playerName)
    {
        foreach (var player in players)
        {
            if (player.NickName == playerName)
                return player;
        }
        return null;
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isGameActive = true;

        // 봇이 이미 추가되었다면 바로 시작, 아니면 플레이어만으로 시작
        if (allPlayers.Count > 0)
        {
            StartTurn();
        }
        else
        {
            Debug.LogWarning("플레이어 목록이 비어있습니다!");
        }
    }

    void StartTurn()
    {
        if (!isGameActive || allPlayers.Count == 0) return;

        string currentPlayerName = GetCurrentPlayerName();
        bool isBot = IsCurrentTurnBot();

        currentTurnTime = turnDuration;
        isItemUsed = false;

        Debug.Log($"{currentPlayerName}의 턴 시작! {(isBot ? "[봇]" : "[플레이어]")}");

        // 이벤트 발생 (봇인 경우 null 전달)
        if (!isBot)
        {
            var player = GetPlayerByName(currentPlayerName);
            OnTurnStart?.Invoke(player);
        }
        else
        {
            OnTurnStart?.Invoke(null); // 봇의 경우 null 전달
        }

        // 타이머 시작
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }
        turnTimerCoroutine = StartCoroutine(TurnTimer());

        // 플레이어 움직임 제어
        ControlPlayerMovement();

        // 봇인 경우 자동 턴 종료 (임시)
        if (isBot)
        {
            StartCoroutine(BotTurn());
        }
    }

    // 봇 턴 처리 (임시로 3초 후 자동 종료)
    IEnumerator BotTurn()
    {
        Debug.Log($"🤖 {GetCurrentPlayerName()} 봇 턴 진행 중...");
        yield return new WaitForSeconds(3f); // 봇은 3초 후 자동 턴 종료

        if (isGameActive && IsCurrentTurnBot())
        {
            Debug.Log($"🤖 {GetCurrentPlayerName()} 봇 턴 자동 종료");
            EndTurn();
        }
    }

    IEnumerator TurnTimer()
    {
        while (currentTurnTime > 0 && isGameActive)
        {
            OnTurnTimeUpdate?.Invoke(currentTurnTime);
            yield return new WaitForSeconds(0.1f);
            currentTurnTime -= 0.1f;
        }

        // 시간 종료
        if (isGameActive)
        {
            EndTurn();
        }
    }

    public void ForceEndTurn()
    {
        if (!isGameActive) return;

        Debug.Log("아이템 사용으로 인한 강제 턴 종료!");

        // 현재 시간이 5초보다 크면 5초로 변경
        if (currentTurnTime > itemUseTurnDuration)
        {
            currentTurnTime = itemUseTurnDuration;
            isItemUsed = true;
        }
    }

    void EndTurn()
    {
        string currentPlayerName = GetCurrentPlayerName();
        bool isBot = IsCurrentTurnBot();

        Debug.Log($"{currentPlayerName}의 턴 종료! {(isBot ? "[봇]" : "[플레이어]")}");

        // 이벤트 발생 (봇인 경우 null 전달)
        if (!isBot)
        {
            var player = GetPlayerByName(currentPlayerName);
            OnTurnEnd?.Invoke(player);
        }
        else
        {
            OnTurnEnd?.Invoke(null); // 봇의 경우 null 전달
        }

        // 타이머 정지
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        // 다음 플레이어로 넘어가기
        NextPlayer();
    }

    void NextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % allPlayers.Count;

        // 잠시 대기 후 다음 턴 시작
        StartCoroutine(WaitAndStartNextTurn());
    }

    IEnumerator WaitAndStartNextTurn()
    {
        yield return new WaitForSeconds(1f); // 1초 대기
        StartTurn();
    }

    void ControlPlayerMovement()
    {
        string currentPlayerName = GetCurrentPlayerName();
        bool isBot = IsCurrentTurnBot();

        if (isBot)
        {
            // 봇 턴일 때는 모든 플레이어 움직임 차단
            OnPlayerMovementChanged?.Invoke(false);
            Debug.Log($"🤖 봇 {currentPlayerName}의 턴 - 모든 플레이어 움직임 차단");
        }
        else
        {
            // 일반 플레이어 턴
            var currentPlayer = GetPlayerByName(currentPlayerName);
            if (currentPlayer != null)
            {
                // 현재 턴 플레이어만 움직일 수 있도록 설정
                photonView.RPC("SetPlayerMovementState", RpcTarget.All, currentPlayer.ActorNumber);
            }
        }
    }

    [PunRPC]
    void SetPlayerMovementState(int activePlayerActorNumber)
    {
        // 이 RPC는 모든 클라이언트에서 실행됨
        bool canMove = PhotonNetwork.LocalPlayer.ActorNumber == activePlayerActorNumber;

        Debug.Log($"플레이어 움직임 제어: {PhotonNetwork.LocalPlayer.NickName} - 움직임 가능: {canMove}");

        // 이벤트 발생 (친구1의 CatController가 구독)
        OnPlayerMovementChanged?.Invoke(canMove);
    }

    public Photon.Realtime.Player GetCurrentPlayer()
    {
        // 호환성을 위해 남겨둠 (봇이면 null 반환)
        string currentPlayerName = GetCurrentPlayerName();
        if (IsBot(currentPlayerName))
            return null;

        return GetPlayerByName(currentPlayerName);
    }

    public bool IsMyTurn()
    {
        string currentPlayerName = GetCurrentPlayerName();

        // 봇 턴이면 false
        if (IsBot(currentPlayerName))
            return false;

        // 내 턴인지 확인
        return currentPlayerName == PhotonNetwork.LocalPlayer.NickName;
    }

    public void StopGame()
    {
        isGameActive = false;

        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }

        Debug.Log("게임 종료!");
    }

    // 네트워크 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 마스터 클라이언트가 데이터를 전송
            stream.SendNext(currentPlayerIndex);
            stream.SendNext(currentTurnTime);
            stream.SendNext(isGameActive);
            stream.SendNext(isItemUsed);
        }
        else
        {
            // 다른 클라이언트들이 데이터를 받음
            currentPlayerIndex = (int)stream.ReceiveNext();
            currentTurnTime = (float)stream.ReceiveNext();
            isGameActive = (bool)stream.ReceiveNext();
            isItemUsed = (bool)stream.ReceiveNext();
        }
    }
}