using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家重生管理器,服务端全局单例
/// </summary>
public class PlayerRespawnManager : NetworkBehaviour
{
    #region 单例与基础配置
    // 服务端单例
    public static PlayerRespawnManager Instance { get; private set; }
    private static bool _isManagerCreated = false;

    [Header("重生配置")]
    public float respawnDelay = 3f;
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("当前选择的地图信息")]
    [SyncVar(hook = nameof(OnMapIndexChanged))]
    public int CurrentMapIndex = -1; // -1 表示未选择

    public MapInfo CurrentMapInfo
    {
        get
        {
            if (CurrentMapIndex >= 0 && CurrentMapIndex < PlayerAndGameInfoManger.Instance.AllMapInfoList.Count)
            {
                return PlayerAndGameInfoManger.Instance.AllMapInfoList[CurrentMapIndex];
            }
            return null;
        }
    }

    [Header("地图选择数据")]
    [SyncVar(hook = nameof(OnMap1CountChanged))]
    public int Map1ChooseCount = 0; // 选择地图1的人数

    [SyncVar(hook = nameof(OnMap2CountChanged))]
    public int Map2ChooseCount = 0; // 选择地图2的人数

    // 用于记录每个玩家选择了哪个地图
    private Dictionary<int, int> _playerChooseMapDict = new Dictionary<int, int>();
    #endregion

    #region 玩家数据管理
    // 维护所有玩家的详细数据列表
    private List<PlayerInfo> _playerInfoList = new List<PlayerInfo>();

    /// <summary>
    /// 游戏正式开始时调用，记录队伍、重置击杀/死亡数、重置比分
    /// </summary>
    [Server]

    public void InitGameData()
    {
        Debug.Log("[数据管理] 开始初始化游戏数据...");

        _playerInfoList.Clear();

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady || conn.identity == null)
                continue;

            if (conn.identity.TryGetComponent<Player>(out Player playerScript))
            {
                PlayerInfo newInfo = new PlayerInfo
                {
                    PlayerConnectionToClient = conn,
                    Team = playerScript.CurrentTeam,
                    Monster = playerScript,
                    KillCount = 0,
                    DeathCount = 0
                };
                _playerInfoList.Add(newInfo);
                Debug.Log($"[数据管理] 初始化玩家: ID={conn.connectionId}, 队伍={newInfo.Team}");
            }
        }

        RedTeamScoreCount = 0;
        BlueTeamScoreCount = 0;
        IsGameStart = true;

        //启动协程进行倒计时
        StartCoroutine(ServerGameCountdownCoroutine());

        // 初始化完成后同步一次空数据给客户端，确保UI干净
        SyncWarRecordToAllClients();
    }

    #region 游戏倒计时与结束逻辑
    /// <summary>
    /// 服务器端游戏倒计时协程
    /// </summary>
    [Server]
    private IEnumerator ServerGameCountdownCoroutine()
    {
        // 初始化剩余时间 (GameTime 单位是分钟，转换为秒)
        RemainGameTime = GameTime * 60;

        Debug.Log($"[倒计时] 游戏开始！总时长: {GameTime}分钟 ({RemainGameTime}秒)");

        // 循环：只要游戏没结束且时间大于0，就继续
        while (IsGameStart && !_isGameEnded && RemainGameTime > 0)
        {
            yield return null; // 等待一帧

            // 扣除时间
            RemainGameTime -= Time.deltaTime;

            // 限制最小值为0
            if (RemainGameTime < 0) RemainGameTime = 0;

        }

        if (IsGameStart && !_isGameEnded)
        {
            Debug.Log("[倒计时] 时间耗尽！强制结算游戏。");
            ForceEndGameByTime();
        }
    }

    /// <summary>
    /// 时间耗尽时强制结算
    /// </summary>
    [Server]
    private void ForceEndGameByTime()
    {
        _isGameEnded = true;
        IsGameStart = false;

        Team? winningTeam = null;

        // 比较比分
        if (RedTeamScoreCount > BlueTeamScoreCount)
        {
            winningTeam = Team.Red;
            Debug.Log($"[游戏结束] 时间耗尽，红队 {RedTeamScoreCount}:{BlueTeamScoreCount} 获胜！");
        }
        else if (BlueTeamScoreCount > RedTeamScoreCount)
        {
            winningTeam = Team.Blue;
            Debug.Log($"[游戏结束] 时间耗尽，蓝队 {BlueTeamScoreCount}:{RedTeamScoreCount} 获胜！");
        }
        else
        {

            Debug.LogWarning($"[游戏结束] 时间耗尽且比分 {RedTeamScoreCount}:{BlueTeamScoreCount} 平！");
            winningTeam = Random.Range(0, 2) == 0 ? Team.Red : Team.Blue;
        }

        if (winningTeam.HasValue)
        {
            RpcGameSettlement(winningTeam.Value);
        }
    }
    #endregion

    /// <summary>
    /// 获取或创建玩家数据 (服务端调用)
    /// </summary>
    [Server]
    private PlayerInfo GetOrCreatePlayerInfo(NetworkConnectionToClient conn)
    {
        if (conn == null || conn.identity == null) return null;

        // 尝试查找现有数据
        PlayerInfo existingInfo = _playerInfoList.Find(p => p.PlayerConnectionToClient == conn);
        if (existingInfo != null)
        {
            return existingInfo;
        }

        // 创建新数据
        if (conn.identity.TryGetComponent<Player>(out Player playerScript))
        {
            PlayerInfo newInfo = new PlayerInfo
            {
                PlayerConnectionToClient = conn,
                Team = playerScript.CurrentTeam,
                Monster = playerScript,
                KillCount = 0,
                DeathCount = 0
            };
            _playerInfoList.Add(newInfo);
            Debug.Log($"[数据管理] 新玩家数据已添加: ConnectionId={conn.connectionId}, 队伍={newInfo.Team}");
            return newInfo;
        }
        return null;
    }

    /// <summary>
    /// 增加玩家击杀数 (服务端调用)
    /// </summary>
    [Server]
    public void AddPlayerKill(NetworkConnectionToClient killerConn)
    {
        if (!IsGameStart) return; // 游戏开始标识没打开就不处理逻辑

        PlayerInfo info = GetOrCreatePlayerInfo(killerConn);
        if (info != null)
        {
            info.KillCount++;
            Debug.Log($"[数据管理] 玩家 {killerConn.connectionId} 击杀数 +1, 当前: {info.KillCount}");

            // 通知该玩家的客户端更新自己的小KD UI
            TargetUpdatePlayerKD(killerConn, info.KillCount, info.DeathCount);

            // 同步战绩面板给所有人
            SyncWarRecordToAllClients();
        }
    }

    /// <summary>
    /// 增加玩家死亡数 (服务端调用)
    /// </summary>
    [Server]
    public void AddPlayerDeath(NetworkConnectionToClient deadConn)
    {
        if (!IsGameStart) return; // 游戏开始标识没打开就不处理逻辑

        PlayerInfo info = GetOrCreatePlayerInfo(deadConn);
        if (info != null)
        {
            info.DeathCount++;
            Debug.Log($"[数据管理] 玩家 {deadConn.connectionId} 死亡数 +1, 当前: {info.DeathCount}");

            // 通知该玩家的客户端更新自己的小KD UI
            TargetUpdatePlayerKD(deadConn, info.KillCount, info.DeathCount);

            // 同步战绩面板给所有人
            SyncWarRecordToAllClients();
        }
    }

    /// <summary>
    /// TargetRpc：专门给特定客户端发送最新的 KD 数据
    /// </summary>
    [TargetRpc]
    private void TargetUpdatePlayerKD(NetworkConnectionToClient target, int killCount, int deathCount)
    {
        if (UImanager.Instance != null)
        {
            GameScorePanel panel = UImanager.Instance.GetPanel<GameScorePanel>();
            if (panel != null)
            {
                panel.UpdateKDUI(killCount, deathCount);
            }
        }
    }

    /// <summary>
    /// 获取玩家数据 (服务端调用)
    /// </summary>
    [Server]
    public PlayerInfo GetPlayerInfo(NetworkConnectionToClient conn)
    {
        return _playerInfoList.Find(p => p.PlayerConnectionToClient == conn);
    }

    /// <summary>
    /// 清理玩家数据 (服务端调用)
    /// </summary>
    [Server]
    private void RemovePlayerInfo(NetworkConnectionToClient conn)
    {
        PlayerInfo infoToRemove = _playerInfoList.Find(p => p.PlayerConnectionToClient == conn);
        if (infoToRemove != null)
        {
            _playerInfoList.Remove(infoToRemove);
            Debug.Log($"[数据管理] 玩家数据已移除: ConnectionId={conn.connectionId}");
            // 玩家离开后也同步一下战绩面板
            SyncWarRecordToAllClients();
        }
    }
    #endregion

    #region 战绩面板数据同步
    /// <summary>
    /// 服务端构建数据并同步给所有客户端
    /// </summary>
    [Server]
    public void SyncWarRecordToAllClients()
    {
        List<NetworkPlayerInfo> dataList = new List<NetworkPlayerInfo>();

        foreach (var info in _playerInfoList)
        {
            if (info == null || info.Monster == null) continue;

            NetworkPlayerInfo data = new NetworkPlayerInfo
            {
                PlayerName = info.Monster.PlayerName,
                Team = info.Team,
                KillCount = info.KillCount,
                DeathCount = info.DeathCount
            };
            dataList.Add(data);
        }

        RpcReceiveWarRecordData(dataList.ToArray());
    }

    #endregion

    #region 玩家人数与队伍同步
    // 同步玩家人数 
    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    public int CurrentPlayerCount = 0;

    [SyncVar(hook = nameof(OnPreparaCountChanged))]
    public int CurrentpreparaCount;

    [SyncVar(hook = nameof(OnTeamCountChanged))]
    public int RedPlayerCount;

    [SyncVar(hook = nameof(OnTeamCountChanged))]
    public int BluePlayerCount;

    // Hook: 队伍人数变化
    private void OnTeamCountChanged(int oldVal, int newVal)
    {
        if (!isClient) return;
        var panel = UImanager.Instance?.GetPanel<PlayerPreparaPanel>();
        panel?.UpdateteamCompareText(RedPlayerCount, BluePlayerCount);
    }

    private void OnMapIndexChanged(int oldVal, int newVal)
    {
        Debug.Log($"[地图选择] 本地收到最终地图索引: {newVal}");
    }

    private void OnMap1CountChanged(int oldVal, int newVal)
    {
        if (!isClient)
            return;
        //调用本地的更新

        MapChooseWall.Instance.UpdatePlayerCount(newVal, Map2ChooseCount);
    }

    private void OnMap2CountChanged(int oldVal, int newVal)
    {
        if (!isClient) return;
        MapChooseWall.Instance.UpdatePlayerCount(Map1ChooseCount, newVal);
    }

    public bool IsStart = false;

    [Server]
    public void CheckGameAllowStar()
    {
        // 核心修改：游戏已经开始，直接跳过所有准备状态判断
        if (IsGameStart)
        {
            Debug.Log("[Room] 游戏已开始，跳过准备状态检查");
            return;
        }

        // 条件1：总人数为双数
        // 条件2：红队人数 == 蓝队人数 (1:1比例)
        // 条件3：总人数 > 0 (防止0人时误判)
        // 条件4：准备人数 == 总人数 (所有人都准备了)
        bool isEvenCount = CurrentPlayerCount % 2 == 0;
        bool isTeamBalanced = RedPlayerCount == BluePlayerCount;
        bool hasEnoughPlayers = CurrentPlayerCount > 0;
        bool isAllPrepared = CurrentpreparaCount == CurrentPlayerCount;

        if (isEvenCount && isTeamBalanced && hasEnoughPlayers && isAllPrepared)
        {
            Debug.Log($"[Room] 满足开始条件！总人数:{CurrentPlayerCount} (红:{RedPlayerCount} vs 蓝:{BluePlayerCount}) | 准备人数:{CurrentpreparaCount}/{CurrentPlayerCount}");
            // 新增空值检查
            if (UImanager.Instance != null)
            {
                var panel = UImanager.Instance.GetPanel<PlayerPreparaPanel>();
                panel?.IsActiveGameStartButton(true);
            }
            IsStart = true;
        }
        else
        {
            if (IsStart)
            {
                // 新增空值检查
                if (UImanager.Instance != null)
                {
                    var panel = UImanager.Instance.GetPanel<PlayerPreparaPanel>();
                    panel?.IsActiveGameStartButton(false);
                }
                IsStart = false;
            }
        }
    }

    // 兼容无参调用
    private void OnTeamCountChanged()
    {
        OnTeamCountChanged(0, 0);
    }

    // Hook: 总人数变化
    private void OnPlayerCountChanged(int oldVal, int newVal)
    {
        if (!isClient) return;
        var panel = UImanager.Instance?.GetPanel<PlayerPreparaPanel>();
        panel?.UpdateRoomPlayerCount(newVal);
    }

    // Hook: 准备人数变化
    private void OnPreparaCountChanged(int oldVal, int newVal)
    {
        if (!isClient) return;
        var panel = UImanager.Instance?.GetPanel<PlayerPreparaPanel>();
        panel?.UpdateRoomPlayerPreparaCount(newVal);
    }
    #endregion

    #region 玩家准备状态管理
    private HashSet<NetworkConnectionToClient> _preparedConnections = new HashSet<NetworkConnectionToClient>();

    // 兼容旧代码的方法
    public void ChangePreparaCount(int Count)
    {
        if (isServer)
        {
            CurrentpreparaCount += Count;
        }
        else if (isClient)
        {
            CmdChangePreparaCount(Count);
        }
    }

    [Command]
    private void CmdChangePreparaCount(int Count)
    {
        CurrentpreparaCount += Count;
    }

    // 由 Player 调用的准备逻辑
    [Server]
    public void ServerHandlePlayerPrepareChange(NetworkConnectionToClient conn, bool isPrepared)
    {
        // 核心修改：游戏已开始，跳过准备状态修改
        if (IsGameStart)
        {
            Debug.Log($"[Room] 游戏已开始，忽略玩家{conn.connectionId}的准备状态变更请求");
            return;
        }

        if (conn == null) return;

        if (isPrepared)
        {
            if (!_preparedConnections.Contains(conn))
            {
                _preparedConnections.Add(conn);
                CurrentpreparaCount = _preparedConnections.Count;
                Debug.Log($"[Room] 玩家准备，当前准备人数: {CurrentpreparaCount}");
            }
        }
        else
        {
            if (_preparedConnections.Contains(conn))
            {
                _preparedConnections.Remove(conn);
                CurrentpreparaCount = _preparedConnections.Count;
                Debug.Log($"[Room] 玩家取消准备，当前准备人数: {CurrentpreparaCount}");
            }
        }

        ServerUpdateTeamInfo();
        GetOrCreatePlayerInfo(conn);
    }
    #endregion

    #region 对局计数管理
    [SyncVar]
    public bool IsGameStart = false; // 游戏是否开始

    public bool _isGameEnded = false;

    [SyncVar]
    public int RedTeamScoreCount = 0;
    [SyncVar]
    public int BlueTeamScoreCount = 0;
    [SyncVar]
    public int GoalScoreCount = 3;//3分胜利
    [SyncVar]
    public int GameTime;
    [SyncVar(hook = nameof(OnRemainGameTimeUpdated))]
    public float RemainGameTime;

    /// <summary>
    /// 【客户端】剩余时间变化时的回调
    /// </summary>
    private void OnRemainGameTimeUpdated(float oldTime, float newTime)
    {

        if (UImanager.Instance != null)
        {
            GameScorePanel panel = UImanager.Instance.GetPanel<GameScorePanel>();
            if (panel != null)
            {
                panel.UpdateTime(newTime);
            }
        }
    }

    [Server]
    public void InitGoalScoreCount(int GoalScoreCount, int GameTime)
    {
        this.GoalScoreCount = GoalScoreCount;
        this.GameTime = GameTime;
    }

    [Command(requiresAuthority = false)]
    public void AddScore(Team AddScoreTeam)
    {
        if (!IsGameStart || _isGameEnded)
            return;

        if (AddScoreTeam == Team.Red)
            RedTeamScoreCount++;
        else if (AddScoreTeam == Team.Blue)
            BlueTeamScoreCount++;

        NoticeUIUpdate();
        CheckGameWin();
    }

    [ClientRpc]
    public void NoticeUIUpdate()
    {
        if (UImanager.Instance != null)
        {
            GameScorePanel panel = UImanager.Instance.GetPanel<GameScorePanel>();
            if (panel != null)
            {
                panel.UpdateScoreInfo();
            }
        }
    }

    public void CheckGameWin()
    {
        if (_isGameEnded)
            return;

        Team? winningTeam = null;

        if (RedTeamScoreCount >= GoalScoreCount)
        {
            winningTeam = Team.Red;
        }
        else if (BlueTeamScoreCount >= GoalScoreCount)
        {
            winningTeam = Team.Blue;
        }

        if (winningTeam.HasValue)
        {
            _isGameEnded = true;
            IsGameStart = false; // 也把开始标记关了

            Debug.Log($"[游戏结束] {winningTeam.Value} 获胜！");

            RpcGameSettlement(winningTeam.Value);
        }
    }

    /// <summary>
    /// RPC 增加参数，直接告诉客户端谁赢了
    /// </summary>
    [ClientRpc]
    public void RpcGameSettlement(Team winTeam)
    {
        if (UImanager.Instance != null)
        {
            UImanager.Instance.HidePanel<DeathPanel>();
            UImanager.Instance.HidePanel<GameScorePanel>();
            UImanager.Instance.HidePanel<PlayerPanel>();

            // 打开结算面板
            var settlementPanel = UImanager.Instance.ShowPanel<GameSettlementPanel>();
            if (settlementPanel != null)
            {
                settlementPanel.WinTeam = winTeam;
            }
        }
    }
    #endregion

    #region 地图选择核心逻辑
    [Command(requiresAuthority = false)]
    public void CmdPlayerChooseMap(int mapIndex, NetworkConnectionToClient senderConnection = null)
    {
        if (mapIndex != 1 && mapIndex != 2)
            return;

        if (senderConnection == null)
        {
            Debug.LogWarning("[地图选择] 无法获取发送者连接，投票失败！");
            return;
        }

        int connId = senderConnection.connectionId;
        Debug.Log($"[地图选择] 收到玩家{connId}的投票，选择地图{mapIndex}");

        if (_playerChooseMapDict == null)
        {
            _playerChooseMapDict = new Dictionary<int, int>();
        }

        int oldMap1Count = Map1ChooseCount;
        int oldMap2Count = Map2ChooseCount;

        if (_playerChooseMapDict.TryGetValue(connId, out int oldMapIndex))
        {
            if (oldMapIndex == 1)
                Map1ChooseCount = Mathf.Max(0, Map1ChooseCount - 1);
            else if (oldMapIndex == 2)
                Map2ChooseCount = Mathf.Max(0, Map2ChooseCount - 1);

            Debug.Log($"[地图选择] 玩家{connId}更换投票，移除地图{oldMapIndex}的票数");
        }

        _playerChooseMapDict[connId] = mapIndex;
        if (mapIndex == 1)
            Map1ChooseCount++;
        else if (mapIndex == 2)
            Map2ChooseCount++;

        Debug.Log($"[地图选择] [服务器] 投票完成! 地图1: {oldMap1Count}->{Map1ChooseCount}, 地图2: {oldMap2Count}->{Map2ChooseCount} | 投票玩家ID:{connId}");
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestDecideFinalMap()
    {
        DecideFinalMap();
    }

    [Server]
    public void DecideFinalMap()
    {
        if (PlayerAndGameInfoManger.Instance == null ||
            PlayerAndGameInfoManger.Instance.AllMapInfoList == null ||
            PlayerAndGameInfoManger.Instance.AllMapInfoList.Count < 2)
        {
            Debug.LogError("[地图判定] PlayerAndGameInfoManger 或地图列表未准备好！");
            return;
        }

        int finalMapIndex = 0;

        if (Map1ChooseCount > Map2ChooseCount)
        {
            finalMapIndex = 0;
            Debug.Log($"[地图判定] 地图1以 {Map1ChooseCount}:{Map2ChooseCount} 胜出！");
        }
        else if (Map2ChooseCount > Map1ChooseCount)
        {
            finalMapIndex = 1;
            Debug.Log($"[地图判定] 地图2以 {Map2ChooseCount}:{Map1ChooseCount} 胜出！");
        }
        else
        {
            finalMapIndex = Random.Range(0, 2);
            Debug.Log($"[地图判定] 平票 ({Map1ChooseCount}:{Map2ChooseCount})，随机选择地图{finalMapIndex + 1}");
        }

        CurrentMapIndex = finalMapIndex;
    }

    [Server]
    public void OnPlayerDisconnectCleanup(int connId)
    {
        if (_playerChooseMapDict == null) return;

        if (_playerChooseMapDict.TryGetValue(connId, out int mapIndex))
        {
            if (mapIndex == 1)
            {
                Map1ChooseCount = Mathf.Max(0, Map1ChooseCount - 1);
                Debug.Log($"[地图选择] 玩家{connId}断开，移除地图1票数");
            }
            else if (mapIndex == 2)
            {
                Map2ChooseCount = Mathf.Max(0, Map2ChooseCount - 1);
                Debug.Log($"[地图选择] 玩家{connId}断开，移除地图2票数");
            }

            _playerChooseMapDict.Remove(connId);
        }
    }
    #endregion

    #region 队伍信息更新逻辑
    public void UpdateTeamInfo()
    {
        if (isServer)
        {
            ServerUpdateTeamInfo();
        }
        else
        {
            CmdRequestUpdateTeamInfo();
        }
    }

    [Command]
    private void CmdRequestUpdateTeamInfo()
    {
        ServerUpdateTeamInfo();
    }

    [Server]
    public void ServerUpdateTeamInfo()
    {
        int redCount = 0;
        int blueCount = 0;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null || !conn.isReady || conn.identity == null)
                continue;

            if (conn.identity.TryGetComponent<Player>(out Player playerScript))
            {
                if (playerScript.CurrentTeam == Team.Red)
                    redCount++;
                else
                    blueCount++;

                GetOrCreatePlayerInfo(conn);
            }
        }

        RedPlayerCount = redCount;
        BluePlayerCount = blueCount;

        Debug.Log($"[Room] 队伍统计更新 - 红队:{RedPlayerCount}, 蓝队:{BluePlayerCount}");

        // 核心修改：仅游戏未开始时，才检查开始条件
        if (!IsGameStart)
        {
            CheckGameAllowStar();
        }
    }

    [Server]
    public void UpdatePlayerCount()
    {
        CurrentPlayerCount = NetworkManager.singleton.numPlayers;

        // 核心修改：仅游戏未开始时，才检查开始条件
        if (!IsGameStart)
        {
            CheckGameAllowStar();
        }
    }
    #endregion

    #region 网络生命周期与单例管理
    [Server]
    public static void SpawnRespawnManager()
    {
        if (_isManagerCreated || Instance != null) return;

        GameObject managerPrefab = Resources.Load<GameObject>("Prefabs/PlayerRespawnManager");
        if (managerPrefab == null)
        {
            Debug.LogError("[RespawnManager] 未在Resources中找到PlayerRespawnManager预制体！");
            return;
        }

        GameObject managerObj = Instantiate(managerPrefab);
        NetworkServer.Spawn(managerObj);
        _isManagerCreated = true;

        Debug.Log("[RespawnManager] 服务端全局重生管理器生成成功！");
    }

    [Server]
    public static void DestroyRespawnManager()
    {
        if (Instance != null)
        {
            NetworkServer.Destroy(Instance.gameObject);
            Instance = null;
            _isManagerCreated = false;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance != null && Instance != this)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }
        Instance = this;

        _playerChooseMapDict = new Dictionary<int, int>();
        _playerInfoList = new List<PlayerInfo>();
        Map1ChooseCount = 0;
        Map2ChooseCount = 0;
        CurrentMapIndex = -1;
        IsGameStart = false;

        UpdatePlayerCount();
        ServerUpdateTeamInfo();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (Instance == null)
        {
            Instance = this;
        }

        // 【新增】订阅客户端断开连接事件，监听房主是否跑路
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        // 【新增】取消订阅，防止内存泄漏
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
    }
    private void OnClientDisconnected()
    {
        Debug.LogWarning("[网络事件] 检测到与服务器断开连接（房主可能已离开），执行强制清理。");
        // 直接调用UI清理，不碰网络代码
        ForceCleanupUI();
    }

    private void OnDestroy()
    {
        if (isServer && Instance == this)
        {
            Instance = null;
            _isManagerCreated = false;
        }
    }
    #endregion

    #region 核心重生逻辑
    [Server]
    public void RespawnPlayer(NetworkConnectionToClient conn)
    {
        if (conn == null || !conn.isReady)
        {
            Debug.LogError($"[RespawnManager] 玩家连接无效，无法重生！");
            return;
        }
        StartCoroutine(ServerDelayedRespawnCoroutine(conn, respawnDelay));
    }

    [Server]
    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[RespawnManager] 未配置出生点，使用NetworkManager默认出生点！");
            return NetworkManager.singleton.GetStartPosition();
        }
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
    #endregion

    #region 客户端UI通知
    [TargetRpc]
    public void TargetShowDeathPanel(NetworkConnection target, float delay, string killerName, string killerGunName)
    {
        string finalKillerName = string.IsNullOrEmpty(killerName) ? "未知" : killerName;
        string finalKillerGunName = string.IsNullOrEmpty(killerGunName) ? "未知" : killerGunName;

        if (UImanager.Instance == null)
        {
            Debug.LogWarning("[RespawnManager] UImanager为空，无法显示死亡面板！");
            return;
        }

        DeathPanel deathPanel = UImanager.Instance.ShowPanel<DeathPanel>();
        if (deathPanel != null)
        {
            deathPanel.StartCountDown(delay, finalKillerGunName, finalKillerName);
        }
    }

    [TargetRpc]
    public void TargetHideDeathPanel(NetworkConnection target)
    {
        if (UImanager.Instance != null)
        {
            UImanager.Instance.HidePanel<DeathPanel>();
        }
        Debug.Log("[RespawnManager] 玩家重生完成，关闭死亡面板");
    }
    #endregion

    #region 消息分发与游戏控制
    public void SendGlobalMessage(string content, float duration)
    {
        if (string.IsNullOrEmpty(content) || duration <= 0)
        {
            Debug.LogWarning($"[RespawnManager] 全局消息参数无效");
            return;
        }

        if (isServer)
        {
            ServerHandleLogic(content, duration);
            return;
        }

        if (isClient)
        {
            CmdSendMessage_overallSituation(content, duration);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSendMessage_overallSituation(string content, float duration)
    {
        ServerHandleLogic(content, duration);
    }

    [Server]
    private void ServerHandleLogic(string content, float duration)
    {
        if (SendMessageManger.Instance != null)
        {
            SendMessageManger.Instance.SendMessage(content, duration);
        }
        RpcSendMessage(content, duration);
    }

    [ClientRpc]
    public void RpcSendMessage(string content, float duration)
    {
        if (isServer) return;

        if (SendMessageManger.Instance == null) return;

        SendMessageManger.Instance.SendMessage(content, duration);
    }

    [Server]
    public void HandlePlayerDisconnected(NetworkConnectionToClient conn)
    {
        UpdatePlayerCount();

        if (!IsGameStart)
        {
            if (_preparedConnections.Contains(conn))
            {
                _preparedConnections.Remove(conn);
                CurrentpreparaCount = _preparedConnections.Count;
                Debug.Log($"[Room] 退出玩家在准备列表中，已移除，当前准备人数: {CurrentpreparaCount}");
            }
        }

        OnPlayerDisconnectCleanup(conn.connectionId);
        RemovePlayerInfo(conn);
        ServerUpdateTeamInfo();
        CheckGameEndByPlayerQuit();
    }

    /// <summary>
    /// 检查是否因为有人退游戏导致某一方没人了
    /// </summary>
    [Server]
    private void CheckGameEndByPlayerQuit()
    {
        // 如果游戏还没开始，或者已经结束了，就不处理
        if (!IsGameStart || _isGameEnded)
            return;

        // 反之亦然
        Team? winningTeam = null;

        if (RedPlayerCount <= 0 && BluePlayerCount > 0)
        {
            Debug.Log("[游戏结束] 红队已无玩家，蓝队获胜！");
            winningTeam = Team.Blue;
        }
        else if (BluePlayerCount <= 0 && RedPlayerCount > 0)
        {
            Debug.Log("[游戏结束] 蓝队已无玩家，红队获胜！");
            winningTeam = Team.Red;
        }

        // 如果判定出了获胜方
        if (winningTeam.HasValue)
        {
            _isGameEnded = true;
            IsGameStart = false;

            // 调用通用的结算 RPC
            RpcGameSettlement(winningTeam.Value);
        }
    }

    public void NoticePlayerGameStart()
    {
        if (isServer)
        {
            Debug.Log("[Respawn] 服务器开始广播游戏开始通知...");
            RpcNoticePlayerGameStart();
        }
    }

    [ClientRpc]
    public void RpcNoticePlayerGameStart()
    {
        if (UImanager.Instance == null)
        {
            Debug.LogError("[Respawn] UImanager 未找到！");
            return;
        }

        UImanager.Instance.ShowPanel<CountDownPanel>().InitPanel(
            "游戏即将开始请做好准备！",
            5,
            () => {
                MapChooseWall.Instance.EnterMapChooseSystem();//通知所有人进入地图选择面板
            }
        );
    }
    #endregion

    #region 地图传送与队伍出生点核心逻辑 (已修复)

    [Server]
    private MapManager GetCurrentMapManager()
    {

        if (PlayerAndGameInfoManger.Instance == null)
        {
            return null;
        }

        if (CurrentMapIndex < 0 || CurrentMapIndex >= PlayerAndGameInfoManger.Instance.AllMapManagerList.Count)
        {
            return null;
        }

        return PlayerAndGameInfoManger.Instance.AllMapManagerList[CurrentMapIndex];
    }

    [Server]
    public Transform GetTeamSpawnPoint(Team playerTeam)
    {
        if (!IsGameStart || CurrentMapIndex < 0)
        {
            return GetRandomSpawnPoint();
        }

        MapManager mapManager = GetCurrentMapManager();
        if (mapManager == null)
        {
            Debug.LogWarning("[传送] 未找到当前地图管理器，使用默认出生点！");
            return GetRandomSpawnPoint();
        }

        List<Transform> targetBornList = null;

        if (playerTeam == Team.Red)
        {
            targetBornList = mapManager.teamBornPosList.RedTeamBornPosList;
        }
        else if (playerTeam == Team.Blue)
        {
            targetBornList = mapManager.teamBornPosList.BlueTeamBornPosList;
        }

        if (targetBornList == null || targetBornList.Count == 0)
        {
            Debug.LogWarning($"[传送] {playerTeam}队在当前地图未配置出生点，使用默认出生点！");
            return GetRandomSpawnPoint();
        }

        return targetBornList[Random.Range(0, targetBornList.Count)];
    }

    [Server]
    public void TeleportAllPlayersToMap()
    {
        if (CurrentMapIndex < 0)
        {
            Debug.LogError("[传送] 地图索引无效，无法传送！");
            return;
        }

        Debug.Log($"[传送] 服务端开始批量通知所有客户端传送...");

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.isReady && conn.identity != null)
            {
                if (conn.identity.TryGetComponent<Player>(out Player playerScript))
                {
                    Transform spawnPoint = GetTeamSpawnPoint(playerScript.CurrentTeam);
                    if (spawnPoint != null)
                    {
                        TargetTeleportPlayer(conn, spawnPoint.position, spawnPoint.rotation);
                        Debug.Log($"[传送] 已通知玩家 {conn.connectionId} 传送到：{spawnPoint.position}");
                    }
                }
            }
        }

        Debug.Log("[传送] 所有客户端传送通知发送完成！");
    }

    [TargetRpc]
    private void TargetTeleportPlayer(NetworkConnectionToClient target, Vector3 spawnPos, Quaternion spawnRot)
    {
        if (NetworkClient.localPlayer != null)
        {
            NetworkClient.localPlayer.transform.SetPositionAndRotation(spawnPos, spawnRot);
            Debug.Log($"[客户端] 本地传送完成，新位置：{spawnPos}");
            if (UImanager.Instance != null)
            {
                var playerPanel = UImanager.Instance.GetPanel<PlayerPanel>();
                playerPanel?.SimpleHidePanel();
            }
        }
        else
        {
            Debug.LogError("[客户端] 未找到自己的本地玩家对象，传送失败！");
        }
    }
    #endregion

    #region 修改原有重生逻辑，兼容新出生点
    [Server]
    private IEnumerator ServerDelayedRespawnCoroutine(NetworkConnectionToClient conn, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (conn.identity != null)
        {
            Team oldPlayerTeam = Team.Red;
            PlayerInfo savedInfo = GetPlayerInfo(conn);

            if (savedInfo != null)
            {
                oldPlayerTeam = savedInfo.Team;
                Debug.Log($"[RespawnManager] 从数据中恢复玩家队伍: {conn.connectionId} -> {oldPlayerTeam}");
            }
            else
            {
                if (conn.identity.TryGetComponent<Player>(out Player oldPlayer))
                {
                    oldPlayerTeam = oldPlayer.CurrentTeam;
                }
                Debug.LogWarning($"[RespawnManager] 未找到玩家数据，使用旧物体队伍: {oldPlayerTeam}");
            }

            NetworkServer.DestroyPlayerForConnection(conn);

            Transform spawnPoint = GetTeamSpawnPoint(oldPlayerTeam);
            Vector3 spawnPos = spawnPoint ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

            GameObject playerPrefab = NetworkManager.singleton.playerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError($"[RespawnManager] NetworkManager未配置PlayerPrefab！");
                yield break;
            }

            GameObject newPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);
            Player newPlayerScript = newPlayer.GetComponent<Player>();
            if (newPlayerScript != null)
            {
                newPlayerScript.CurrentTeam = oldPlayerTeam;
                Debug.Log($"[RespawnManager] 已直接赋值新玩家队伍: {oldPlayerTeam}");
            }
            if (!NetworkServer.AddPlayerForConnection(conn, newPlayer))
            {
                Debug.LogError($"[RespawnManager] 重生失败：玩家{conn}已有绑定的玩家对象");
                NetworkServer.Destroy(newPlayer);
                yield break;
            }

            if (savedInfo != null)
            {
                savedInfo.Monster = newPlayerScript;
            }

            TargetHideDeathPanel(conn);
            Debug.Log($"[RespawnManager] 玩家{conn} ({oldPlayerTeam}队) 重生成功，新玩家生成于：{spawnPos}");
        }
    }
    #endregion

    private static NetworkPlayerInfo[] _cachedClientData;

    [ClientRpc]
    private void RpcReceiveWarRecordData(NetworkPlayerInfo[] allPlayerData)
    {

        _cachedClientData = allPlayerData;

        if (UImanager.Instance != null)
        {
            WarRecordPanel panel = UImanager.Instance.GetPanel<WarRecordPanel>();
            if (panel != null)
            {
                panel.RefreshWarRecordData(allPlayerData);
            }
        }
    }

    // 新增一个公共方法，供面板打开时主动获取缓存
    public static NetworkPlayerInfo[] GetCachedData()
    {
        return _cachedClientData;
    }

    #region 简化版：客户端退出+UI操作
    /// <summary>
    /// 全局清理方法（对外暴露）
    /// </summary>
    // 找到PlayerRespawnManager中的CleanupAndExitGame方法，修改为：
    public void CleanupAndExitGame()
    {
        IsGameStart = false;
        _isGameEnded = true;

        StopAllCoroutines();

        bool isNetworkActive = NetworkServer.active || NetworkClient.active;

        if (!isNetworkActive)
        {
            ForceCleanupUI();
            return;
        }

        if (NetworkServer.active)
        {
            RedTeamScoreCount = 0;
            BlueTeamScoreCount = 0;

            // 销毁重生管理器
            DestroyRespawnManager();
        }

        if (CustomNetworkManager.Instance != null)
        {
            CustomNetworkManager.Instance.ForceStopCurrentPort();
        }

        // StopHost 会自动判断并停止 Client/Server/Host
        NetworkManager.singleton.StopHost();

        ForceCleanupUI();
    }

    /// <summary>
    /// 【优化】独立的UI强制清理方法，不依赖网络状态
    /// </summary>
    private void ForceCleanupUI()
    {
        try
        {
            Debug.Log("[UI清理] 强制关闭所有面板并返回房间...");
            //手动清理所有可能出现的面板
            UImanager.Instance.HidePanel<PlayerPanel>();
            UImanager.Instance.HidePanel<PlayerPreparaPanel>();
            UImanager.Instance.HidePanel<GameScorePanel>();
            UImanager.Instance.ShowPanel<RoomPanel>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UI清理过程中发生异常: {e.Message}");
        }
    }

    /// <summary>
    /// RPC：通知其他客户端退出
    /// </summary>
    [ClientRpc]
    private void RpcNotifyAllClientsExit()
    {
        // 跳过房主
        if (NetworkServer.active && NetworkClient.active)
            return;

        if (!NetworkClient.active)
            return;

        Debug.Log("[简化清理] RPC通知：客户端执行退出");

        // 断开连接
        NetworkManager networkManager = NetworkManager.singleton;
        if (networkManager != null)
        {
            networkManager.StopClient();
        }

        // 调用独立的 UI 清理方法
        ForceCleanupUI();
    }

    // 保留原有服务端数据重置（无修改）
    [Server]
    private void ResetAllGameData()
    {
        if (!NetworkServer.active) return;
        _playerInfoList?.Clear();
        _playerChooseMapDict?.Clear();
        Map1ChooseCount = 0;
        Map2ChooseCount = 0;
        CurrentMapIndex = -1;
        _preparedConnections?.Clear();
        CurrentpreparaCount = 0;
        CurrentPlayerCount = 0;
        RedPlayerCount = 0;
        BluePlayerCount = 0;
        IsGameStart = false;
        _isGameEnded = false;
        RedTeamScoreCount = 0;
        BlueTeamScoreCount = 0;
        IsStart = false;
    }
    #endregion
}

// 玩家当前的数据包
public class PlayerInfo
{
    public NetworkConnectionToClient PlayerConnectionToClient;
    public Team Team;
    public Player Monster;
    public int KillCount;
    public int DeathCount;
}

/// <summary>
/// 用于网络传输的玩家战绩数据
/// </summary>
[System.Serializable]
public struct NetworkPlayerInfo
{
    public string PlayerName;
    public Team Team;
    public int KillCount;
    public int DeathCount;
}