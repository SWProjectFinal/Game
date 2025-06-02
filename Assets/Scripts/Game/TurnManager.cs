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
    public List<Player> players = new List<Player>();
    public int currentPlayerIndex = 0;

    [Header("턴 상태")]
    public bool isGameActive = false;
    public float currentTurnTime;
    public bool isItemUsed = false;

    // 이벤트
    public System.Action<Player> OnTurnStart;
    public System.Action<Player> OnTurnEnd;
    public System.Action<float> OnTurnTimeUpdate;

    // 플레이어 움직임 제어 이벤트 (친구1이 구독할 이벤트)
    public static System.Action<bool> OnPlayerMovementChanged;

    private Coroutine turnTimerCoroutine;

    void Awake()
    {
        // 싱글톤 패턴 (DontDestroyOnLoad 제거)
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
            StartGame();
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
        currentPlayerIndex = 0;

        Debug.Log($"플레이어 턴 순서 초기화 완료. 총 {players.Count}명");
        for (int i = 0; i < players.Count; i++)
        {
            Debug.Log($"{i + 1}번째: {players[i].NickName}");
        }
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isGameActive = true;
        StartTurn();
    }

    void StartTurn()
    {
        if (!isGameActive || players.Count == 0) return;

        var currentPlayer = GetCurrentPlayer();
        currentTurnTime = turnDuration;
        isItemUsed = false;

        Debug.Log($"{currentPlayer.NickName}의 턴 시작!");

        // 이벤트 발생
        OnTurnStart?.Invoke(currentPlayer);

        // 타이머 시작
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }
        turnTimerCoroutine = StartCoroutine(TurnTimer());

        // 플레이어 움직임 제어
        ControlPlayerMovement();
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
        var currentPlayer = GetCurrentPlayer();

        Debug.Log($"{currentPlayer.NickName}의 턴 종료!");

        // 이벤트 발생
        OnTurnEnd?.Invoke(currentPlayer);

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
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

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
        // 모든 플레이어의 움직임을 제어
        var currentPlayer = GetCurrentPlayer();

        // 현재 턴 플레이어만 움직일 수 있도록 설정
        photonView.RPC("SetPlayerMovementState", RpcTarget.All, currentPlayer.ActorNumber);
    }

    [PunRPC]
    void SetPlayerMovementState(int activePlayerActorNumber)
    {
        // 이 RPC는 모든 클라이언트에서 실행됨
        bool canMove = PhotonNetwork.LocalPlayer.ActorNumber == activePlayerActorNumber;

        Debug.Log($"플레이어 움직임 제어: {PhotonNetwork.LocalPlayer.NickName} - 움직임 가능: {canMove}");

        // 이벤트 발생 (친구1이 구독해서 사용)
        OnPlayerMovementChanged?.Invoke(canMove);
    }

    public Player GetCurrentPlayer()
    {
        if (players.Count == 0) return null;
        return players[currentPlayerIndex];
    }

    public bool IsMyTurn()
    {
        var currentPlayer = GetCurrentPlayer();
        return currentPlayer != null && currentPlayer.Equals(PhotonNetwork.LocalPlayer);
    }

    public void StopGame()
    {
        isGameActive = false;

        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
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