using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // 用于记录每个玩家选择了哪个地图 (connectionId -> mapIndex, 1或2)
    private Dictionary<int, int> _playerChooseMapDict = new Dictionary<int, int>();
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

    // 【新增】地图索引变更的 Hook
    private void OnMapIndexChanged(int oldVal, int newVal)
    {
        Debug.Log($"[地图选择] 本地收到最终地图索引: {newVal}");
    }

    private void OnMap1CountChanged(int oldVal, int newVal)
    {
        if (!isClient) return;
        // 通知 MapChoosePanel 更新UI
        var panel = UImanager.Instance?.GetPanel<MapChoosePanel>();
        if (panel != null)
        {
            panel.MapButton_1.UpdatePlayerCount(newVal);
        }
    }

    private void OnMap2CountChanged(int oldVal, int newVal)
    {
        if (!isClient) return;
        // 通知 MapChoosePanel 更新UI
        var panel = UImanager.Instance?.GetPanel<MapChoosePanel>();
        if (panel != null)
        {
            panel.MapButton_2.UpdatePlayerCount(newVal);
        }
    }

    public bool IsStart = false;

    [Server]
    public void CheckGameAllowStar()
    {
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
            //在这里触发房主的开始房间按钮

            UImanager.Instance.GetPanel<PlayerPreparaPanel>().IsActiveGameStartButton(true);
            IsStart = true;
        }
        else
        {
            if (IsStart)
            {
                UImanager.Instance.GetPanel<PlayerPreparaPanel>().IsActiveGameStartButton(false);
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

        // 准备状态变更，自动更新队伍信息
        ServerUpdateTeamInfo();
    }
    #endregion

    #region 地图选择核心逻辑
    /// <summary>
    /// 玩家选择地图（由客户端调用）
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdPlayerChooseMap(int mapIndex, NetworkConnectionToClient senderConnection = null)
    {
        // 非法地图索引直接返回
        if (mapIndex != 1 && mapIndex != 2)
            return;

        // Mirror 自动注入发送者连接
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

        // 1. 如果玩家之前投过票，先减去之前的票数
        if (_playerChooseMapDict.TryGetValue(connId, out int oldMapIndex))
        {
            if (oldMapIndex == 1)
                Map1ChooseCount = Mathf.Max(0, Map1ChooseCount - 1);
            else if (oldMapIndex == 2)
                Map2ChooseCount = Mathf.Max(0, Map2ChooseCount - 1);

            Debug.Log($"[地图选择] 玩家{connId}更换投票，移除地图{oldMapIndex}的票数");
        }

        // 2. 记录新投票并增加对应票数
        _playerChooseMapDict[connId] = mapIndex;
        if (mapIndex == 1)
            Map1ChooseCount++;
        else if (mapIndex == 2)
            Map2ChooseCount++;

        Debug.Log($"[地图选择] [服务器] 投票完成! 地图1: {oldMap1Count}->{Map1ChooseCount}, 地图2: {oldMap2Count}->{Map2ChooseCount} | 投票玩家ID:{connId}");
    }

    /// <summary>
    /// 由客户端请求服务器判定最终地图
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdRequestDecideFinalMap()
    {
        DecideFinalMap();
    }

    /// <summary>
    /// 倒计时结束后判定最终地图
    /// </summary>
    [Server]
    public void DecideFinalMap()
    {
        // 防止未初始化报错
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

        // 【核心修改】只同步索引，不同步整个类
        CurrentMapIndex = finalMapIndex;
    }

    /// <summary>
    /// 玩家断开连接时，清理他的地图选择
    /// </summary>
    [Server]
    public void OnPlayerDisconnectCleanup(int connId)
    {
        if (_playerChooseMapDict == null) return;

        // 检查该玩家是否投过票
        if (_playerChooseMapDict.TryGetValue(connId, out int mapIndex))
        {
            // 减去他的票数
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

            // 从字典中移除记录
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

        // 遍历服务器上所有的连接，统计所有玩家
        foreach (var conn in NetworkServer.connections.Values)
        {
            // 过滤无效连接
            if (conn == null || !conn.isReady || conn.identity == null)
                continue;

            if (conn.identity.TryGetComponent<Player>(out Player playerScript))
            {
                if (playerScript.CurrentTeam == Team.Red)
                    redCount++;
                else
                    blueCount++;
            }
        }

        // 更新 SyncVar
        RedPlayerCount = redCount;
        BluePlayerCount = blueCount;

        Debug.Log($"[Room] 队伍统计更新 - 红队:{RedPlayerCount}, 蓝队:{BluePlayerCount}");

        CheckGameAllowStar();
    }

    // 刷新总人数
    [Server]
    public void UpdatePlayerCount()
    {
        CurrentPlayerCount = NetworkManager.singleton.numPlayers;
        CheckGameAllowStar();
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

        // 服务器启动时强制初始化字典
        _playerChooseMapDict = new Dictionary<int, int>();
        // 重置投票数据
        Map1ChooseCount = 0;
        Map2ChooseCount = 0;
        CurrentMapIndex = -1;

        // 服务器启动时，先统计一遍已有的玩家
        UpdatePlayerCount();
        ServerUpdateTeamInfo();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // 客户端也初始化单例
        if (Instance == null)
        {
            Instance = this;
        }
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
    private IEnumerator ServerDelayedRespawnCoroutine(NetworkConnectionToClient conn, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (conn.identity != null)
        {
            NetworkServer.DestroyPlayerForConnection(conn);
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 spawnPos = spawnPoint ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        GameObject playerPrefab = NetworkManager.singleton.playerPrefab;
        if (playerPrefab == null)
        {
            Debug.LogError($"[RespawnManager] NetworkManager未配置PlayerPrefab！");
            yield break;
        }

        GameObject newPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);
        if (!NetworkServer.AddPlayerForConnection(conn, newPlayer))
        {
            Debug.LogError($"[RespawnManager] 重生失败：玩家{conn}已有绑定的玩家对象");
            NetworkServer.Destroy(newPlayer);
            yield break;
        }

        TargetHideDeathPanel(conn);
        Debug.Log($"[RespawnManager] 玩家{conn}重生成功，新玩家生成于：{spawnPos}");
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
    /// <summary>
    /// 对外公开的全局消息发送方法（客户端/服务器都能调用）
    /// </summary>
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
        // 1. 更新总人数
        UpdatePlayerCount();

        // 2. 清理准备列表
        if (_preparedConnections.Contains(conn))
        {
            _preparedConnections.Remove(conn);
            CurrentpreparaCount = _preparedConnections.Count;
            Debug.Log($"[Room] 退出玩家在准备列表中，已移除，当前准备人数: {CurrentpreparaCount}");
        }
        else
        {
            Debug.Log($"[Room] 退出玩家未准备，无需处理准备人数");
        }

        // 3. 清理地图选择
        OnPlayerDisconnectCleanup(conn.connectionId);

        ServerUpdateTeamInfo();
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
        // 判空保护
        if (UImanager.Instance == null)
        {
            Debug.LogError("[Respawn] UImanager 未找到！");
            return;
        }

        UImanager.Instance.ShowPanel<CountDownPanel>().InitPanel(
            "游戏即将开始请做好准备！",
            40,
            () => {
                //所有人打开本地的地图选择面板
                UImanager.Instance.ShowPanel<MapChoosePanel>();
            }
        );
    }
    #endregion
}