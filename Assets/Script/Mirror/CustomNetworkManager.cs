using kcp2k;
using Mirror;
using System.Net;
using System.Net.Sockets;
using Unity.Sync.Relay.Transport.Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;

    // 自定义事件
    public static event System.Action OnServerStartedEvent;
    public static event System.Action OnServerStoppedEvent;
    public static event System.Action OnClientConnectedSuccess;

    public GameObject BroadcasterObj;
    private bool _isRelayModeActive = false;

    [Header("动态端口配置")]
    public int minPort = 7777;
    public int maxPort = 8888;
    private int _currentUsedPort;

    #region 双模式切换
    /// <summary>
    /// 切换到局域网模式（KCP）
    /// </summary>
    public void SwitchToLanMode()
    {
        _isRelayModeActive = false;
        Debug.Log("[CustomNetworkManager] 正在切换到【局域网模式（KCP）】...");

        // 先停止所有网络活动
        StopAllNetworkActivity();

        // 禁用 UOS 相关的一切
        DisableUOSRelayEverything();

        // 确保有 KCP 组件，没有就添加
        KcpTransport kcp = GetComponent<KcpTransport>();
        if (kcp == null)
        {
            kcp = gameObject.AddComponent<KcpTransport>();
        }

        // 启用 KCP，禁用 Relay
        kcp.enabled = true;
        var uosRelay = GetComponent<RelayTransportMirror>();
        if (uosRelay != null)
        {
            uosRelay.enabled = false;
        }

        // 告诉 Mirror 使用 KCP
        transport = kcp;

        Debug.Log("[CustomNetworkManager] 已切换到【局域网模式（KCP）】");
    }

    /// <summary>
    /// 切换到 Relay 模式（UOS）
    /// </summary>
    public void SwitchToRelayMode()
    {
        _isRelayModeActive = true;
        Debug.Log("[CustomNetworkManager] 正在切换到【Relay模式（UOS）】...");

        // 先停止所有网络活动
        StopAllNetworkActivity();

        // 启用 UOS 相关的一切
        EnableUOSRelayEverything();

        // 确保有 Relay 组件，没有就添加
        RelayTransportMirror uosRelay = GetComponent<RelayTransportMirror>();
        if (uosRelay == null)
        {
            uosRelay = gameObject.AddComponent<RelayTransportMirror>();
        }

        // 启用 Relay，禁用 KCP
        uosRelay.enabled = true;
        var kcp = GetComponent<KcpTransport>();
        if (kcp != null)
        {
            kcp.enabled = false;
        }

        //  告诉 Mirror 使用 Relay
        transport = uosRelay;

        //  同步引用给 UOSRelaySimple
        if (UOSRelaySimple.Instance != null)
        {
            UOSRelaySimple.Instance.relayTransport = uosRelay;
        }

        Debug.Log("[CustomNetworkManager]  已切换到【Relay模式（UOS）】");
    }

    /// <summary>
    /// 停止所有网络活动
    /// </summary>
    private void StopAllNetworkActivity()
    {
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            StopHost();
        }
    }

    /// <summary>
    /// 禁用 UOS 相关的一切
    /// </summary>
    private void DisableUOSRelayEverything()
    {
        // 禁用 UOSRelaySimple 单例
        if (UOSRelaySimple.Instance != null)
        {
            UOSRelaySimple.Instance.enabled = false;
            Debug.Log("[CustomNetworkManager] 已禁用 UOSRelaySimple");
        }
    }

    /// <summary>
    ///启用 UOS 相关的一切
    /// </summary>
    private void EnableUOSRelayEverything()
    {
        // 启用 UOSRelaySimple 单例
        if (UOSRelaySimple.Instance != null)
        {
            UOSRelaySimple.Instance.enabled = true;
            Debug.Log("[CustomNetworkManager] 已启用 UOSRelaySimple");
        }
    }
    #endregion

    #region 生命周期
    public override void OnStartServer()
    {
        base.OnStartServer();
        OnServerStartedEvent?.Invoke();
        string transportName = transport != null ? transport.GetType().Name : "未知";
        Debug.Log($"[CustomNetworkManager] 服务端启动（模式：{(_isRelayModeActive ? "Relay" : "局域网")}，传输层：{transportName}）");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        OnServerStoppedEvent?.Invoke();

        if (BroadcasterObj != null)
        {
            Destroy(BroadcasterObj);
            BroadcasterObj = null;
        }

        // 切回局域网待命
        if (_isRelayModeActive)
        {
            SwitchToLanMode();
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawnPoint = GetStartPosition();
        if (PlayerRespawnManager.Instance != null)
        {
            Transform managerSpawn = PlayerRespawnManager.Instance.GetRandomSpawnPoint();
            if (managerSpawn != null) spawnPoint = managerSpawn;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[CustomNetworkManager] Player Prefab未赋值！");
            conn.Disconnect();
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkServer.AddPlayerForConnection(conn, player);

        if (CountDownManager.Instance != null)
        {
            CountDownManager.Instance.CreateTimer(false, 500, () =>
            {
                string playerName = "未知玩家";
                if (player != null && player.TryGetComponent<Player>(out var p))
                {
                    playerName = p.GetDisplayName();
                }
                PlayerRespawnManager.Instance?.SendGlobalMessage("玩家：" + playerName + "加入进房间", 1);
            });
        }

        PlayerRespawnManager.Instance?.UpdatePlayerCount();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        string exitPlayerName = "未知玩家";
        if (conn.identity != null && conn.identity.TryGetComponent<Player>(out var player))
        {
            exitPlayerName = player.GetDisplayName();
        }

        PlayerRespawnManager.Instance?.HandlePlayerDisconnected(conn);
        PlayerRespawnManager.Instance?.SendGlobalMessage("玩家：" + exitPlayerName + "离开了房间", 1);

        base.OnServerDisconnect(conn);
        PlayerRespawnManager.Instance?.UpdatePlayerCount();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnClientConnectedSuccess?.Invoke();
        Debug.Log("[CustomNetworkManager] 客户端连接成功");
        //处理其他信息
        UImanager.Instance.HidePanel<EnterRoomPanel>();
        ModeChooseSystem.instance.ExitSystem();//退出系统
        UImanager.Instance.HidePanel<Remote_EnterRoomPanel>();
    }
    #endregion

    #region 局域网端口管理
    public void ForceStopCurrentPort()
    {
        if (_isRelayModeActive) return;

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            StopHost();
        }

        var kcp = GetComponent<KcpTransport>();
        if (kcp != null)
        {
            CleanupKcpSocket(kcp);
            kcp.enabled = false;
            Invoke(nameof(ReEnableKcpTransport), 0.1f);
        }

        if (BroadcasterObj != null)
        {
            Destroy(BroadcasterObj);
            BroadcasterObj = null;
        }

        _currentUsedPort = 0;
    }

    private void CleanupKcpSocket(KcpTransport kcp)
    {
        try
        {
            var socketField = kcp.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var serverSocketField = kcp.GetType().GetField("serverSocket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (socketField != null)
            {
                var socket = socketField.GetValue(kcp) as Socket;
                if (socket != null)
                {
                    if (socket.Connected) socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket.Dispose();
                    socketField.SetValue(kcp, null);
                }
            }

            if (serverSocketField != null)
            {
                var serverSocket = serverSocketField.GetValue(kcp) as Socket;
                if (serverSocket != null)
                {
                    serverSocket.Close();
                    serverSocket.Dispose();
                    serverSocketField.SetValue(kcp, null);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"清理KCP套接字警告：{ex.Message}");
        }
    }

    private void ReEnableKcpTransport()
    {
        var kcp = GetComponent<KcpTransport>();
        if (kcp != null && !kcp.enabled && !_isRelayModeActive)
        {
            kcp.enabled = true;
        }
    }

    public bool IsPortAvailable(int port)
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Close();
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public int AllocateAvailablePort()
    {
        if (_isRelayModeActive) return 7777;

        if (maxPort > 65535) maxPort = 65535;
        if (minPort < 0) minPort = 0;

        for (int port = minPort; port <= maxPort; port++)
        {
            if (IsPortAvailable(port))
            {
                _currentUsedPort = port;
                var kcp = GetComponent<KcpTransport>();
                if (kcp != null)
                {
                    ushort kcpPort = (ushort)Mathf.Clamp(port, 0, 65535);
                    kcp.Port = kcpPort;
                }
                return port;
            }
        }
        Debug.LogError("无可用端口！");
        return -1;
    }

    public int PrepareForCreateRoom()
    {
        if (_isRelayModeActive) return 0;

        ForceStopCurrentPort();
        System.Threading.Thread.Sleep(200);
        return AllocateAvailablePort();
    }
    #endregion

    public override void Awake()
    {
        base.Awake();
        Instance = this;

        // 默认局域网模式
        if (!_isRelayModeActive)
        {
            // 确保有KCP
            if (GetComponent<KcpTransport>() == null)
            {
                gameObject.AddComponent<KcpTransport>();
            }
        }
    }

    public bool IsRelayModeActive() => _isRelayModeActive;
}