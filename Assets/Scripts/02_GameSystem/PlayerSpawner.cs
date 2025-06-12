using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class PlayerSpawner : MonoBehaviourPun, IConnectionCallbacks, IPunObservable
{
  public static PlayerSpawner Instance { get; private set; }

  [Header("스폰 설정")]
  public GameObject catPrefab; // Cat Prefab 할당
  public Transform[] spawnPoints; // 스폰 위치들
  public float spawnHeight = -10f; // 스폰 높이
  public Vector2 mapBounds = new Vector2(8.8f, 5f); // 맵 크기 (랜덤 스폰용)

  [Header("스폰된 오브젝트 관리")]
  public List<GameObject> spawnedPlayers = new List<GameObject>();
  public List<GameObject> spawnedBots = new List<GameObject>();

  // 이벤트
  public System.Action OnAllPlayersSpawned;

  private bool hasSpawned = false;

  [System.Serializable]
  public class BotInfo
  {
    public string name;
    public int colorIndex;
    public Vector3 spawnPosition; // ✅ 스폰 위치 추가
  }

  private List<BotInfo> lobbyBots = new List<BotInfo>();

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
    // 방에 이미 연결되어 있다면 바로 스폰
    if (PhotonNetwork.InRoom && !hasSpawned)
    {
      StartCoroutine(DelayedSpawn());
    }

    // 로비에서 저장한 봇 정보 로드
    LoadBotDataFromLobby();
  }

  void OnEnable()
  {
    PhotonNetwork.AddCallbackTarget(this);
  }

  void OnDisable()
  {
    PhotonNetwork.RemoveCallbackTarget(this);
  }

  // PUN 콜백: 방에 성공적으로 들어갔을 때
  public void OnJoinedRoom()
  {
    Debug.Log("방 연결 완료! 플레이어 스폰 준비");

    if (!hasSpawned)
    {
      StartCoroutine(DelayedSpawn());
    }
  }

  IEnumerator DelayedSpawn()
  {
    // 네트워크가 완전히 준비될 때까지 잠시 대기
    yield return new WaitForSeconds(1f);

    if (!hasSpawned)
    {
      SpawnAllPlayers();

      // ✅ 마스터 클라이언트만 봇 위치 계산 및 전송
      if (PhotonNetwork.IsMasterClient)
      {
        if (lobbyBots.Count > 0)
        {
          // ✅ 봇 위치를 다시 계산 (플레이어 스폰 후)
          for (int i = 0; i < lobbyBots.Count; i++)
          {
            lobbyBots[i].spawnPosition = GetSpawnPosition(PhotonNetwork.PlayerList.Length + i);
          }

          // 모든 클라이언트에 봇 정보 전송 (위치 포함)
          photonView.RPC("RPC_SyncBotsData", RpcTarget.Others, SerializeBotData());

          // 마스터에서 봇 스폰
          SpawnAllBots();
        }
        else
        {
          // 봇이 없으면 바로 게임 시작
          if (TurnManager.Instance != null)
          {
            TurnManager.Instance.StartGame();
            Debug.Log("봇 없이 게임 시작");
          }
        }
      }

      hasSpawned = true;

      // 모든 플레이어 색깔 적용
      StartCoroutine(ApplyColorsToAllPlayers());
    }
  }

  // ✅ 봇 데이터 직렬화 (RPC 전송용)
  string SerializeBotData()
  {
    List<string> serializedData = new List<string>();

    foreach (var bot in lobbyBots)
    {
      // "BotName|ColorIndex|X|Y|Z" 형식으로 저장 (위치 포함)
      Vector3 pos = bot.spawnPosition;
      serializedData.Add($"{bot.name}|{bot.colorIndex}|{pos.x}|{pos.y}|{pos.z}");
    }

    // 배열을 하나의 문자열로 합치기 (구분자: ;)
    return string.Join(";", serializedData);
  }

  // ✅ 봇 데이터 역직렬화 (RPC 수신용)
  List<BotInfo> DeserializeBotData(string serializedData)
  {
    List<BotInfo> bots = new List<BotInfo>();

    if (string.IsNullOrEmpty(serializedData))
    {
      return bots;
    }

    // ";" 구분자로 분리
    string[] botEntries = serializedData.Split(';');

    foreach (string entry in botEntries)
    {
      if (string.IsNullOrEmpty(entry)) continue;

      string[] parts = entry.Split('|');
      if (parts.Length == 5) // name|colorIndex|x|y|z
      {
        var botInfo = new BotInfo
        {
          name = parts[0],
          colorIndex = int.Parse(parts[1]),
          spawnPosition = new Vector3(
                float.Parse(parts[2]),
                float.Parse(parts[3]),
                float.Parse(parts[4])
            )
        };
        bots.Add(botInfo);
      }
    }

    return bots;
  }

  // ✅ RPC: 모든 클라이언트에서 봇 데이터 수신 및 스폰
  [PunRPC]
  void RPC_SyncBotsData(string serializedBotData)
  {
    Debug.Log($"🤖 [Client] 봇 데이터 수신: {serializedBotData}");

    // 봇 데이터 역직렬화
    lobbyBots = DeserializeBotData(serializedBotData);

    // 모든 클라이언트에서 봇 스폰
    SpawnAllBots();

    Debug.Log($"🤖 [Client] 봇 스폰 완료: {lobbyBots.Count}개");
  }

  void SpawnAllPlayers()
  {
    Debug.Log("모든 플레이어 스폰 시작!");

    // 각 클라이언트가 자신의 캐릭터만 생성 (이게 정상)
    Vector3 spawnPos = GetSpawnPosition(GetPlayerIndex());
    SpawnMyPlayer(spawnPos);

    Debug.Log("내 플레이어 스폰 완료");

    // 모든 플레이어 스폰 완료 이벤트
    OnAllPlayersSpawned?.Invoke();
  }

  int GetPlayerIndex()
  {
    var players = PhotonNetwork.PlayerList;
    for (int i = 0; i < players.Length; i++)
    {
      if (players[i].Equals(PhotonNetwork.LocalPlayer))
        return i;
    }
    return 0;
  }

  void SpawnMyPlayer(Vector3 position)
  {
    if (catPrefab == null)
    {
      Debug.LogError("Cat Prefab이 할당되지 않았습니다!");
      return;
    }

    // 각 클라이언트가 자신의 캐릭터만 생성
    GameObject playerObj = PhotonNetwork.Instantiate(catPrefab.name, position, Quaternion.identity);
    spawnedPlayers.Add(playerObj);

    // 로비에서 설정한 색깔 적용
    ApplyPlayerColorFromLobby(playerObj);

    // ===== 충돌 무시 설정 추가 ===== ← 새로 추가
    StartCoroutine(SetupPlayerCollisions(playerObj));

    Debug.Log($"내 플레이어 스폰: {PhotonNetwork.LocalPlayer.NickName} at {position}");
  }

  // ===== 플레이어 충돌 설정 코루틴 ===== ← 새로 추가
  IEnumerator SetupPlayerCollisions(GameObject newPlayer)
  {
    // 다른 플레이어들이 모두 스폰될 때까지 잠시 대기
    yield return new WaitForSeconds(1f);

    // 모든 기존 플레이어들의 충돌 설정 새로고침
    GameObject[] allCats = GameObject.FindGameObjectsWithTag("Player");
    if (allCats.Length == 0)
    {
      allCats = FindObjectsOfType<GameObject>().Where(obj => obj.name.Contains("Cat")).ToArray();
    }

    foreach (GameObject cat in allCats)
    {
      CatController catController = cat.GetComponent<CatController>();
      if (catController != null)
      {
        catController.RefreshPlayerCollisions();
      }
    }

    Debug.Log("모든 플레이어 충돌 설정 새로고침 완료");
  }

  // ✅ public으로 변경! (오류 수정)
  public void ApplyPlayerColorFromLobby(GameObject playerObj)
  {
    if (playerObj == null) return;

    // PhotonView에서 플레이어 정보 가져오기
    PhotonView pv = playerObj.GetComponent<PhotonView>();
    if (pv == null || pv.Owner == null) return;

    // 로비에서 설정한 색상 정보 가져오기
    Color playerColor = GetPlayerColorFromCustomProperties(pv.Owner);

    // 캐릭터에 색상 적용
    ApplyColorToSprite(playerObj, playerColor);

    Debug.Log($"플레이어 색깔 적용: {pv.Owner.NickName} → {playerColor}");
  }

  Color GetPlayerColorFromCustomProperties(Photon.Realtime.Player player)
  {
    // CustomProperties에서 색상 인덱스 가져오기
    if (player.CustomProperties.TryGetValue("playerColor", out object colorData))
    {
      int colorIndex = (int)colorData;
      return GetLobbyColor(colorIndex);
    }

    // 색상이 설정되지 않은 경우 ActorNumber 기반 기본 색상
    int defaultColorIndex = (player.ActorNumber - 1) % 8;
    return GetLobbyColor(defaultColorIndex);
  }

  Color GetLobbyColor(int colorIndex)
  {
    // LobbyManager의 색상 배열과 동일하게 설정
    Color[] lobbyColors = new Color[]
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

    if (colorIndex >= 0 && colorIndex < lobbyColors.Length)
    {
      return lobbyColors[colorIndex];
    }

    return Color.white; // 기본값
  }

  void ApplyColorToSprite(GameObject playerObj, Color color)
  {
    // 메인 SpriteRenderer 색깔 변경
    SpriteRenderer spriteRenderer = playerObj.GetComponent<SpriteRenderer>();
    if (spriteRenderer != null)
    {
      spriteRenderer.color = color;
    }

    // 자식 오브젝트들의 SpriteRenderer도 변경 (필요시)
    SpriteRenderer[] childRenderers = playerObj.GetComponentsInChildren<SpriteRenderer>();
    foreach (var renderer in childRenderers)
    {
      if (renderer != spriteRenderer) // 메인 스프라이트 제외
      {
        renderer.color = color;
      }
    }

    Debug.Log($"캐릭터 색상 적용 완료: {color}");
  }

  // 로비에서 저장한 봇 정보 로드
  void LoadBotDataFromLobby()
  {
    int botCount = PlayerPrefs.GetInt("BotCount", 0);
    Debug.Log($"🤖 로비에서 저장된 봇 정보 로드: {botCount}개");

    lobbyBots.Clear(); // 기존 데이터 클리어

    for (int i = 0; i < botCount; i++)
    {
      string botName = PlayerPrefs.GetString($"BotName{i}", $"Bot{i + 1}");
      int botColorIndex = PlayerPrefs.GetInt($"BotColor{i}", i);

      // ✅ 마스터 클라이언트에서만 위치 계산 (일관성 보장)
      Vector3 botSpawnPos = Vector3.zero;
      if (PhotonNetwork.IsMasterClient)
      {
        botSpawnPos = GetSpawnPosition(PhotonNetwork.PlayerList.Length + i);
      }

      // 봇 정보를 로컬 리스트에 저장
      var botInfo = new BotInfo
      {
        name = botName,
        colorIndex = botColorIndex,
        spawnPosition = botSpawnPos // ✅ 위치 저장
      };

      lobbyBots.Add(botInfo);
      Debug.Log($"🤖 봇 정보 로드: {botName} (색상: {botColorIndex}, 위치: {botSpawnPos})");
    }
  }

  // ✅ 수정: 모든 클라이언트에서 봇 스폰 (로컬 오브젝트로)
  void SpawnAllBots()
  {
    Debug.Log($"🤖 모든 클라이언트에서 봇 스폰 시작: {lobbyBots.Count}개");

    for (int i = 0; i < lobbyBots.Count; i++)
    {
      var botInfo = lobbyBots[i];

      // ✅ 미리 계산된 위치 사용 (랜덤 계산 안 함)
      Vector3 spawnPos = botInfo.spawnPosition;

      SpawnBot(botInfo, spawnPos);
    }

    Debug.Log($"🤖 모든 봇 스폰 완료: {lobbyBots.Count}개");

    // ✅ 마스터 클라이언트만 TurnManager에 봇 정보 전달
    if (PhotonNetwork.IsMasterClient)
    {
      NotifyBotsToTurnManager();
    }
  }

  void NotifyBotsToTurnManager()
  {
    if (TurnManager.Instance != null)
    {
      // 봇 이름 리스트를 TurnManager에 전달
      List<string> botNames = new List<string>();
      foreach (var bot in lobbyBots)
      {
        botNames.Add(bot.name);
      }

      TurnManager.Instance.AddBots(botNames);
      Debug.Log($"🤖 TurnManager에 봇 {botNames.Count}개 정보 전달");

      // 봇 추가 완료 후 게임 시작
      TurnManager.Instance.StartGame();
    }
    else
    {
      Debug.LogError("TurnManager.Instance가 null입니다!");
    }
  }

  void SpawnBot(BotInfo botInfo, Vector3 position)
  {
    if (catPrefab == null)
    {
      Debug.LogError("Cat Prefab이 할당되지 않았습니다!");
      return;
    }

    // ✅ 모든 클라이언트에서 봇을 로컬 오브젝트로 생성
    GameObject botObj = Instantiate(catPrefab, position, Quaternion.identity);

    // 봇 이름 설정
    botObj.name = botInfo.name;

    // 봇 색상 적용
    Color botColor = GetLobbyColor(botInfo.colorIndex);
    ApplyColorToSprite(botObj, botColor);

    // 네트워크 관련 컴포넌트들 모두 제거 (봇은 로컬 오브젝트)
    RemoveNetworkComponents(botObj);

    // 봇 설정
    var catController = botObj.GetComponent<CatController>();
    if (catController != null)
    {
      // 봇은 입력을 받지 않도록 설정 (나중에 AI 추가)
      catController.enabled = false;
    }

    spawnedBots.Add(botObj);

    Debug.Log($"🤖 봇 스폰 완료: {botInfo.name} (색상: {botColor}) at {position}");
  }

  void RemoveNetworkComponents(GameObject botObj)
  {
    // PhotonView 제거
    PhotonView photonView = botObj.GetComponent<PhotonView>();
    if (photonView != null)
    {
      DestroyImmediate(photonView);
      Debug.Log("🤖 봇에서 PhotonView 제거");
    }

    // PhotonTransformView 제거 (더 안전한 방법)
    Component[] allComponents = botObj.GetComponents<Component>();
    foreach (var component in allComponents)
    {
      if (component != null && component.GetType().Name.Contains("PhotonTransformView"))
      {
        DestroyImmediate(component);
        Debug.Log("🤖 봇에서 PhotonTransformView 제거");
      }
    }

    // 자식 오브젝트들의 네트워크 컴포넌트도 제거
    Component[] allChildComponents = botObj.GetComponentsInChildren<Component>();
    foreach (var component in allChildComponents)
    {
      if (component != null &&
          (component.GetType().Name.Contains("PhotonView") ||
           component.GetType().Name.Contains("PhotonTransformView")))
      {
        DestroyImmediate(component);
        Debug.Log($"🤖 봇 자식에서 {component.GetType().Name} 제거");
      }
    }
  }

  Vector3 GetSpawnPosition(int playerIndex)
  {
    // 스폰 포인트가 설정되어 있으면 사용
    if (spawnPoints != null && spawnPoints.Length > 0)
    {
      int spawnIndex = playerIndex % spawnPoints.Length;
      return spawnPoints[spawnIndex].position;
    }
    // 없으면 랜덤 위치
    else
    {
      return GetRandomSpawnPosition();
    }
  }

  Vector3 GetRandomSpawnPosition()
  {
    float x = Random.Range(-mapBounds.x, mapBounds.x);
    float z = Random.Range(-mapBounds.y, mapBounds.y);
    return new Vector3(x, spawnHeight, z);
  }

  // 모든 플레이어 캐릭터에 색깔 적용
  IEnumerator ApplyColorsToAllPlayers()
  {
    yield return new WaitForSeconds(0.5f); // 캐릭터 생성 대기

    // 씬에 있는 모든 Cat(Clone) 오브젝트 찾기
    GameObject[] allCats = GameObject.FindGameObjectsWithTag("Player");
    if (allCats.Length == 0)
    {
      // Tag가 없으면 이름으로 찾기
      allCats = FindObjectsOfType<GameObject>().Where(obj => obj.name.Contains("Cat")).ToArray();
    }

    foreach (GameObject catObj in allCats)
    {
      PhotonView pv = catObj.GetComponent<PhotonView>();
      if (pv != null && pv.Owner != null)
      {
        ApplyPlayerColorFromLobby(catObj);
      }
    }

    Debug.Log($"모든 플레이어 색깔 적용 완료: {allCats.Length}개");
  }

  // 색깔 정보 동기화용 RPC (업데이트됨)
  [PunRPC]
  void SyncPlayerColor(int actorNumber, int colorIndex)
  {
    // 해당 플레이어의 캐릭터 찾아서 색깔 적용
    GameObject[] allCats = FindObjectsOfType<GameObject>().Where(obj => obj.name.Contains("Cat")).ToArray();

    foreach (GameObject catObj in allCats)
    {
      PhotonView pv = catObj.GetComponent<PhotonView>();
      if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
      {
        Color playerColor = GetLobbyColor(colorIndex);
        ApplyColorToSprite(catObj, playerColor);
        Debug.Log($"RPC로 색깔 동기화: {pv.Owner.NickName} → {playerColor}");
        break;
      }
    }
  }

  // 모든 스폰된 오브젝트 정리
  public void ClearAllSpawned()
  {
    // 플레이어 오브젝트들 (네트워크 오브젝트)
    foreach (var player in spawnedPlayers)
    {
      if (player != null && player.GetComponent<PhotonView>() != null && player.GetComponent<PhotonView>().IsMine)
      {
        PhotonNetwork.Destroy(player);
      }
    }
    spawnedPlayers.Clear();

    // 봇 오브젝트들 (로컬 오브젝트)
    foreach (var bot in spawnedBots)
    {
      if (bot != null)
      {
        Destroy(bot);
      }
    }
    spawnedBots.Clear();

    Debug.Log("모든 스폰된 오브젝트 정리 완료");
  }

  // 새로운 플레이어가 방에 들어왔을 때 (IConnectionCallbacks)
  public void OnPlayerEnteredRoom(Player newPlayer)
  {
    Debug.Log($"새로운 플레이어가 방에 들어왔습니다: {newPlayer.NickName}");

    // ✅ 마스터 클라이언트라면 새로 들어온 플레이어에게 봇 정보 전송
    if (PhotonNetwork.IsMasterClient && lobbyBots.Count > 0)
    {
      // 현재 스폰된 봇들의 실제 위치를 업데이트
      for (int i = 0; i < spawnedBots.Count && i < lobbyBots.Count; i++)
      {
        if (spawnedBots[i] != null)
        {
          lobbyBots[i].spawnPosition = spawnedBots[i].transform.position;
        }
      }

      photonView.RPC("RPC_SyncBotsData", newPlayer, SerializeBotData());
      Debug.Log($"🤖 새 플레이어 {newPlayer.NickName}에게 봇 정보 전송");
    }

    // 새로 들어온 플레이어의 캐릭터에도 색깔 적용
    StartCoroutine(ApplyColorsToAllPlayers());
  }

  // IConnectionCallbacks 인터페이스의 다른 메서드들 (비워둠)
  public void OnConnected() { }
  public void OnConnectedToMaster() { }
  public void OnDisconnected(DisconnectCause cause) { }
  public void OnRegionListReceived(RegionHandler regionHandler) { }
  public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
  public void OnCustomAuthenticationFailed(string debugMessage) { }

  // IPunObservable 구현
  public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
  {
    if (stream.IsWriting)
    {
      // 데이터 전송 (필요시)
      stream.SendNext(hasSpawned);
    }
    else
    {
      // 데이터 수신 (필요시)
      hasSpawned = (bool)stream.ReceiveNext();
    }
  }

  void OnDrawGizmosSelected()
  {
    // 스폰 포인트들 시각화
    if (spawnPoints != null)
    {
      Gizmos.color = Color.green;
      foreach (var point in spawnPoints)
      {
        if (point != null)
        {
          Gizmos.DrawWireSphere(point.position, 0.5f);
          Gizmos.DrawWireCube(point.position, Vector3.one);
        }
      }
    }

    // 맵 경계 시각화
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapBounds.x * 2, 1, mapBounds.y * 2));
  }

  public GameObject GetPlayerObject(string nickname)
  {
    // 실제 플레이어 찾기
    foreach (GameObject playerObj in spawnedPlayers)
    {
      PhotonView pv = playerObj.GetComponent<PhotonView>();
      if (pv != null && pv.Owner != null && pv.Owner.NickName == nickname)
        return playerObj;
    }

    // ✅ 봇 찾기 추가
    foreach (GameObject botObj in spawnedBots)
    {
      if (botObj != null && botObj.name == nickname)
        return botObj;
    }

    return null;
  }
}