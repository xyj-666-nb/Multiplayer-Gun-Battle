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
    [SyncVar]
    public MapInfo CurrentMapInfo;
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
    // 服务器专用：记录准备的玩家连接
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

    // 【核心】由 Player 调用的准备逻辑
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

    #region 队伍信息更新逻辑
    public void UpdateTeamInfo()
    {
        if (isServer)
        {
            // 如果是服务器（房主），直接调用
            ServerUpdateTeamInfo();
        }
        else
        {
            // 如果是客户端，发 Command 给服务器
            CmdRequestUpdateTeamInfo();
        }
    }

    [Command]
    private void CmdRequestUpdateTeamInfo()
    {
        // Command 运行在服务器上，直接调用核心逻辑
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
    }

    // 刷新总人数
    [Server]
    public void UpdatePlayerCount()
    {
        CurrentPlayerCount = NetworkManager.singleton.numPlayers;
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
        // 服务器启动时，先统计一遍已有的玩家
        UpdatePlayerCount();
        ServerUpdateTeamInfo(); // 顺便初始化一下队伍信息
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
            () => { }
        );
    }
    #endregion
}