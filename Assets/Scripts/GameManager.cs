using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public class GameManager : MonoBehaviourPun
{
  public static GameManager Instance { get; private set; }

  [Header("게임 상태")]
  public bool isGameActive = false;
  public bool isGameEnded = false;

  [Header("플레이어 관리")]
  public List<string> alivePlayers = new List<string>();
  public List<string> deadPlayers = new List<string>();
  public string winner = "";

  [Header("게임 종료 설정")]
  public float gameEndDelay = 3f; // 게임 종료 후 결과 화면까지 딜레이

  // 이벤트
  public static System.Action<string> OnGameEnded; // 게임 종료 시 발생 (승자 이름)
  public static System.Action<List<string>> OnPlayersUpdated; // 생존자 업데이트 시 발생

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
    // PlayerHealth 이벤트 구독
    PlayerHealth.OnPlayerDied += OnPlayerDied;
    PlayerHealth.OnPlayerHealthChanged += OnPlayerHealthChanged;

    // TurnManager 연동
    if (TurnManager.Instance != null)
    {
      // 게임 시작 대기
      StartCoroutine(WaitForGameStart());
    }

    Debug.Log("GameManager 초기화 완료!");
  }

  // 게임 시작 대기
  IEnumerator WaitForGameStart()
  {
    // TurnManager가 게임을 시작할 때까지 대기
    while (TurnManager.Instance != null && !TurnManager.Instance.isGameActive)
    {
      yield return new WaitForSeconds(0.5f);
    }

    // 게임 시작!
    StartGame();
  }

  // 게임 시작
  public void StartGame()
  {
    if (isGameActive) return;

    isGameActive = true;
    isGameEnded = false;
    winner = "";

    // 생존 플레이어 목록 초기화
    InitializePlayerList();

    Debug.Log($"🎮 게임 시작! 참가자: {alivePlayers.Count}명");
    foreach (string player in alivePlayers)
    {
      Debug.Log($"  - {player}");
    }
  }

  // 플레이어 목록 초기화
  void InitializePlayerList()
  {
    alivePlayers.Clear();
    deadPlayers.Clear();

    if (TurnManager.Instance != null)
    {
      // TurnManager의 allPlayers에서 가져오기
      alivePlayers.AddRange(TurnManager.Instance.allPlayers);
    }
    else
    {
      // 백업: PhotonNetwork에서 가져오기
      foreach (var player in PhotonNetwork.PlayerList)
      {
        alivePlayers.Add(player.NickName);
      }
    }

    // 이벤트 발생
    OnPlayersUpdated?.Invoke(new List<string>(alivePlayers));
  }

  // 플레이어 사망 이벤트 처리
  void OnPlayerDied(string playerName)
  {
    if (!isGameActive || isGameEnded) return;

    Debug.Log($"💀 {playerName} 사망! 생존자 체크 시작...");

    // 생존자 목록에서 제거
    if (alivePlayers.Contains(playerName))
    {
      alivePlayers.Remove(playerName);
      deadPlayers.Add(playerName);

      Debug.Log($"현재 생존자: {alivePlayers.Count}명");
      foreach (string alive in alivePlayers)
      {
        Debug.Log($"  - {alive}");
      }

      // 이벤트 발생
      OnPlayersUpdated?.Invoke(new List<string>(alivePlayers));

      // 승부 판정
      CheckWinCondition();
    }
  }

  // 플레이어 체력 변경 이벤트 처리
  void OnPlayerHealthChanged(string playerName, float healthPercentage)
  {
    // GameUIManager에 전달
    if (GameUIManager.Instance != null)
    {
      GameUIManager.Instance.UpdatePlayerHealth(playerName, healthPercentage);
    }
  }

  // 승리 조건 체크
  void CheckWinCondition()
  {
    if (!isGameActive || isGameEnded) return;

    // 생존자가 1명 이하면 게임 종료
    if (alivePlayers.Count <= 1)
    {
      if (alivePlayers.Count == 1)
      {
        winner = alivePlayers[0];
        Debug.Log($"🏆 승자 결정: {winner}");  // ✅ 수정: Debug.Log
      }
      else
      {
        winner = "무승부";
        Debug.Log($"🤝 무승부! (모든 플레이어 사망)");
      }

      // 게임 종료
      EndGame();
    }
    else
    {
      Debug.Log($"⏳ 게임 계속 진행 - 생존자 {alivePlayers.Count}명");
    }
  }

  // 게임 종료
  void EndGame()
  {
    if (isGameEnded) return;

    isGameEnded = true;
    isGameActive = false;

    Debug.Log($"🎯 게임 종료! 승자: {winner}");

    // TurnManager 정지
    if (TurnManager.Instance != null)
    {
      TurnManager.Instance.StopGame();
    }

    // 딜레이 후 결과 화면 표시
    StartCoroutine(ShowGameResultsDelayed());

    // 이벤트 발생
    OnGameEnded?.Invoke(winner);
  }

  // 딜레이 후 결과 화면 표시
  IEnumerator ShowGameResultsDelayed()
  {
    yield return new WaitForSeconds(gameEndDelay);

    // GameUIManager에 결과 화면 표시 요청
    if (GameUIManager.Instance != null)
    {
      GameUIManager.Instance.ShowGameOver(winner);
    }

    Debug.Log($"📊 게임 결과 화면 표시: {winner} 승리!");
  }

  // 방으로 돌아가기 (롤/배그 스타일) - 안전한 버전
  public void ReturnToRoom()
  {
    Debug.Log("🚪 방으로 돌아가기 요청...");

    // ✅ 안전한 체크
    if (!PhotonNetwork.IsConnected)
    {
      Debug.LogError("❌ 네트워크에 연결되지 않음!");
      return;
    }

    if (!PhotonNetwork.InRoom)
    {
      Debug.LogError("❌ 방에 있지 않음!");
      return;
    }

    if (!PhotonNetwork.IsMasterClient)
    {
      Debug.Log("⚠️ 방장이 아니므로 대기...");
      return;
    }

    Debug.Log("✅ 방으로 돌아가기 시작...");

    // RPC로 모든 클라이언트에 방 복귀 알림
    if (photonView != null)
    {
      photonView.RPC("PrepareReturnToRoom", RpcTarget.All);
    }

    // 씬 전환
    StartCoroutine(ReturnToRoomProcess());
  }

  IEnumerator ReturnToRoomProcess()
  {
    yield return new WaitForSeconds(1f);

    // 게임 오브젝트들 정리
    CleanupGameObjects();

    // ✅ 씬 이름 자동 감지 및 안전한 전환
    Debug.Log("🔄 로비 씬으로 전환...");

    try
    {
      // 여러 가능한 씬 이름 시도
      string[] possibleSceneNames = { "LobbyScene", "MainScene", "Main", "Lobby" };
      string targetScene = "LobbyScene"; // 기본값

      // 현재 씬이 TestGameScene이라면 원래 씬으로 돌아가기
      string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
      if (currentScene == "TestGameScene")
      {
        targetScene = "LobbyScene"; // 또는 실제 로비 씬 이름
      }

      Debug.Log($"🎯 목표 씬: {targetScene}");
      PhotonNetwork.LoadLevel(targetScene);
    }
    catch (System.Exception e)
    {
      Debug.LogError($"❌ 씬 전환 실패: {e.Message}");

      // 백업: 직접 씬 전환
      try
      {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
      }
      catch (System.Exception e2)
      {
        Debug.LogError($"❌ 백업 씬 전환도 실패: {e2.Message}");

        // 최후의 수단: 로비 나가기
        PhotonNetwork.LeaveRoom();
      }
    }
  }

  [PunRPC]
  void PrepareReturnToRoom()
  {
    Debug.Log("🔄 방 복귀 준비 중...");

    // 모든 클라이언트에서 게임 정리
    isGameActive = false;
    isGameEnded = true;

    // UI 정리
    if (GameUIManager.Instance != null)
    {
      // 게임 UI 숨기기
      Debug.Log("UI 정리 중...");
    }
  }

  // 게임 오브젝트 정리 (안전한 버전)
  void CleanupGameObjects()
  {
    Debug.Log("🧹 게임 오브젝트 정리 시작...");

    // 스폰된 플레이어들 정리
    if (PlayerSpawner.Instance != null)
    {
      try
      {
        PlayerSpawner.Instance.ClearAllSpawned();
        Debug.Log("✅ PlayerSpawner 정리 완료");
      }
      catch (System.Exception e)
      {
        Debug.LogError($"❌ PlayerSpawner 정리 실패: {e.Message}");
      }
    }

    // 아이템 박스들 정리
    if (ItemSpawner.Instance != null)
    {
      try
      {
        ItemSpawner.Instance.ClearAllBoxes();
        Debug.Log("✅ ItemSpawner 정리 완료");
      }
      catch (System.Exception e)
      {
        Debug.LogError($"❌ ItemSpawner 정리 실패: {e.Message}");
      }
    }

    // ✅ 안전한 게임 오브젝트 정리 (태그 의존성 제거)
    try
    {
      // 무기/발사체 정리 (태그 없이 이름으로 찾기)
      GameObject[] allObjects = FindObjectsOfType<GameObject>();
      int cleanedCount = 0;

      foreach (GameObject obj in allObjects)
      {
        if (obj != null && obj.name != null)
        {
          string objName = obj.name.ToLower();

          // 무기/발사체로 보이는 오브젝트들 정리
          if (objName.Contains("projectile") ||
              objName.Contains("bullet") ||
              objName.Contains("rocket") ||
              objName.Contains("explosion") ||
              objName.Contains("weapon") ||
              objName.Contains("missile") ||
              objName.Contains("(clone)") && (objName.Contains("shot") || objName.Contains("fire")))
          {
            Destroy(obj);
            cleanedCount++;
          }
        }
      }

      if (cleanedCount > 0)
      {
        Debug.Log($"✅ 무기/발사체 {cleanedCount}개 정리 완료");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError($"❌ 게임 오브젝트 정리 중 오류: {e.Message}");
    }

    // 이펙트 오브젝트 정리
    try
    {
      // 파티클 시스템들 정리
      ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
      foreach (ParticleSystem ps in particles)
      {
        if (ps != null && ps.gameObject.name.ToLower().Contains("effect"))
        {
          Destroy(ps.gameObject);
        }
      }

      if (particles.Length > 0)
      {
        Debug.Log($"✅ 파티클 시스템 {particles.Length}개 확인/정리");
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError($"❌ 이펙트 정리 중 오류: {e.Message}");
    }

    Debug.Log("🧹 게임 오브젝트 정리 완료!");
  }

  // 현재 게임 상태 정보
  public GameStatus GetGameStatus()
  {
    return new GameStatus
    {
      isActive = isGameActive,
      isEnded = isGameEnded,
      aliveCount = alivePlayers.Count,
      alivePlayers = new List<string>(alivePlayers),
      deadPlayers = new List<string>(deadPlayers),
      winner = winner
    };
  }

  // 게임 상태 구조체
  [System.Serializable]
  public struct GameStatus
  {
    public bool isActive;
    public bool isEnded;
    public int aliveCount;
    public List<string> alivePlayers;
    public List<string> deadPlayers;
    public string winner;
  }

  // 강제 게임 종료 (테스트용)
  [ContextMenu("강제 게임 종료 (테스트)")]
  public void ForceEndGame()
  {
    if (alivePlayers.Count > 0)
    {
      winner = alivePlayers[0];
    }
    else
    {
      winner = "테스트 승자";
    }

    EndGame();
  }

  // 디버그 정보 출력
  [ContextMenu("게임 상태 출력")]
  public void PrintGameStatus()
  {
    var status = GetGameStatus();
    Debug.Log($"=== 게임 상태 ===");
    Debug.Log($"활성: {status.isActive}, 종료: {status.isEnded}");
    Debug.Log($"생존자: {status.aliveCount}명");
    Debug.Log($"승자: {status.winner}");
  }

  void OnDestroy()
  {
    // 이벤트 구독 해제
    PlayerHealth.OnPlayerDied -= OnPlayerDied;
    PlayerHealth.OnPlayerHealthChanged -= OnPlayerHealthChanged;
  }
}