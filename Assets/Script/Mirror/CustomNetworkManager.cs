using kcp2k;
using Mirror;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public static CustomNetworkManager Instance;

    // 自定义事件：供Main类监听
    public static event System.Action OnServerStartedEvent;//接入服务器
    public static event System.Action OnServerStoppedEvent;//停止服务器
    public static event System.Action OnClientConnectedSuccess;//链接成功

    public GameObject BroadcasterObj;//广播器（断开房间和游戏开始的时候自动销毁）

    private KcpTransport _kcpTransport;
    private bool isStoppingPort = false;

    // 新增：动态端口配置
    [Header("动态端口配置")]
    public int minPort = 7777; // 端口起始范围
    public int maxPort = 8888; // 端口结束范围
    private int _currentUsedPort; // 记录当前使用的端口

    /// <summary>
    /// 服务端启动时触发
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        OnServerStartedEvent?.Invoke();
        Debug.Log($"[CustomNetworkManager] 服务端启动（端口：{_currentUsedPort}），触发重生管理器生成事件");
    }

    /// <summary>
    /// 服务端停止时触发（核心修复：移除递归调用）
    /// </summary>
    public override void OnStopServer()
    {
        base.OnStopServer();

        OnServerStoppedEvent?.Invoke();
        Debug.Log("[CustomNetworkManager] 服务端已停止，执行收尾清理");

        // 清理广播器（保留原有逻辑）
        if (BroadcasterObj != null)
        {
            Destroy(BroadcasterObj);
            BroadcasterObj = null; // 置空防止重复销毁
        }

        // 移除原有的 ForceStopCurrentPort() 调用！这是递归的根源
    }

    /// <summary>
    /// 重写玩家生成（仅服务端执行）
    /// </summary>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform spawnPoint = GetStartPosition(); // 先取Mirror默认出生点

        // 仅当重生管理器已生成时，才使用其出生点
        if (PlayerRespawnManager.Instance != null)
        {
            Transform managerSpawnPoint = PlayerRespawnManager.Instance.GetRandomSpawnPoint();
            if (managerSpawnPoint != null)
            {
                spawnPoint = managerSpawnPoint;
            }
        }
        if (playerPrefab == null)
        {
            Debug.LogError("[CustomNetworkManager] 严重错误：Player Prefab未赋值！请在Inspector面板中选择玩家预制体");
            conn.Disconnect();
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // 生成玩家
        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"[CustomNetworkManager] 玩家{conn.connectionId}生成成功，出生点：{spawnPos}");

        // 仅保留服务端的全局消息播报（延迟执行）
        CountDownManager.Instance.CreateTimer(false, 500, () =>
        {
            string playerName = player.GetComponent<Player>().GetDisplayName(); // 用兜底方法
            PlayerRespawnManager.Instance.SendGlobalMessage("玩家：" + playerName + "加入进房间", 1);
        });

        PlayerRespawnManager.Instance.UpdatePlayerCount();//更新一下人数
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // 尝试获取玩家名字用于广播
        string exitPlayerName = "未知玩家";
        if (conn.identity != null && conn.identity.TryGetComponent<Player>(out var player))
        {
            exitPlayerName = player.GetDisplayName();
        }

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.HandlePlayerDisconnected(conn);
            PlayerRespawnManager.Instance.SendGlobalMessage("玩家：" + exitPlayerName + "离开了房间", 1);
        }

        base.OnServerDisconnect(conn);
        PlayerRespawnManager.Instance?.UpdatePlayerCount();
        Debug.Log($"[CustomNetworkManager] 玩家 {exitPlayerName} 断开连接");
    }

    /// <summary>
    /// 客户端连接服务器成功时触发
    /// </summary>
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnClientConnectedSuccess?.Invoke();
        Debug.Log("[CustomNetworkManager] 客户端连接服务器成功，触发UI初始化事件");
    }

    #region 强化版：强制停止端口+动态端口分配
    /// <summary>
    /// 强制停止当前占用的网络端口，清理所有网络连接
    /// </summary>
    public void ForceStopCurrentPort()
    {
        if (isStoppingPort) return;
        isStoppingPort = true;

        try
        {
            // 1. 通用方式判断并停止Host/Server/Client
            bool isHost = NetworkServer.active && NetworkClient.isConnected;
            bool isServerOnly = NetworkServer.active && !NetworkClient.isConnected;
            bool isClientOnly = !NetworkServer.active && NetworkClient.isConnected;

            if (isHost)
            {
                StopHost();
                Debug.Log("[CustomNetworkManager] 强制停止：已关闭Host模式");
            }
            else if (isServerOnly)
            {
                NetworkServer.DisconnectAll();
                StopServer();
                Debug.Log("[CustomNetworkManager] 强制停止：已关闭Server并断开所有客户端");
            }

            if (isClientOnly)
            {
                StopClient();
                Debug.Log("[CustomNetworkManager] 强制停止：已断开Client连接");
            }

            if (_kcpTransport != null)
            {
                // 反射清理Kcp底层套接字（核心！彻底释放端口）
                CleanupKcpSocket();

                // 禁用组件释放端口
                _kcpTransport.enabled = false;
                Invoke(nameof(ReEnableKcpTransport), 0.1f);

                Debug.Log($"[CustomNetworkManager] 强制停止：已释放Kcp端口 {_kcpTransport.Port}");
            }
            else
            {
                _kcpTransport = FindObjectOfType<KcpTransport>();
                if (_kcpTransport != null)
                {
                    CleanupKcpSocket();
                    _kcpTransport.enabled = false;
                    Invoke(nameof(ReEnableKcpTransport), 0.1f);
                }
            }

            if (BroadcasterObj != null)
            {
                Destroy(BroadcasterObj);
                BroadcasterObj = null;
            }
            PlayerRespawnManager.Instance?.UpdatePlayerCount();

            // 重置当前端口
            _currentUsedPort = 0;
            Debug.Log("[CustomNetworkManager] 强制停止端口完成：所有网络资源已清理");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CustomNetworkManager] 强制停止端口时出错：{ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            isStoppingPort = false;
        }
    }

    /// <summary>
    /// 反射清理KcpTransport底层套接字（彻底释放端口）
    /// </summary>
    private void CleanupKcpSocket()
    {
        if (_kcpTransport == null) return;

        try
        {
            // 查找KcpTransport的私有socket字段
            var socketField = _kcpTransport.GetType().GetField("socket",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var serverSocketField = _kcpTransport.GetType().GetField("serverSocket",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 关闭socket
            if (socketField != null)
            {
                var socket = socketField.GetValue(_kcpTransport) as Socket;
                if (socket != null)
                {
                    if (socket.Connected) socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket.Dispose();
                    socketField.SetValue(_kcpTransport, null);
                }
            }

            // 关闭serverSocket
            if (serverSocketField != null)
            {
                var serverSocket = serverSocketField.GetValue(_kcpTransport) as Socket;
                if (serverSocket != null)
                {
                    serverSocket.Close();
                    serverSocket.Dispose();
                    serverSocketField.SetValue(_kcpTransport, null);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CustomNetworkManager] 清理Kcp套接字警告：{ex.Message}");
        }
    }

    /// <summary>
    /// 重新启用KcpTransport
    /// </summary>
    private void ReEnableKcpTransport()
    {
        if (_kcpTransport != null && !_kcpTransport.enabled)
        {
            _kcpTransport.enabled = true;
            Debug.Log("[CustomNetworkManager] KcpTransport已重新启用，端口已释放");
        }
    }

    /// <summary>
    /// 检测端口是否可用
    /// </summary>
    /// <param name="port">要检测的端口</param>
    /// <returns>是否可用</returns>
    public bool IsPortAvailable(int port)
    {
        try
        {
            // 创建临时Socket检测端口是否被占用
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

    /// <summary>
    /// 自动分配可用端口（从minPort到maxPort找第一个可用的）
    /// </summary>
    /// <returns>可用端口，失败返回-1</returns>
    public int AllocateAvailablePort()
    {
        // 先检查端口范围是否合法（ushort最大是65535）
        if (maxPort > 65535)
        {
            Debug.LogError($"[CustomNetworkManager] 最大端口{maxPort}超出ushort范围（0-65535），自动修正为65535");
            maxPort = 65535;
        }
        if (minPort < 0)
        {
            Debug.LogError($"[CustomNetworkManager] 最小端口{minPort}小于0，自动修正为0");
            minPort = 0;
        }

        for (int port = minPort; port <= maxPort; port++)
        {
            if (IsPortAvailable(port))
            {
                _currentUsedPort = port;
                if (_kcpTransport != null)
                {
                    // ========== 核心修复：显式转换为ushort + 范围兜底 ==========
                    // 1. 显式转换（解决编译错误）
                    // 2. 二次范围检查，防止极端情况溢出
                    ushort kcpPort = port > 65535 ? (ushort)65535 : (ushort)port;
                    _kcpTransport.Port = kcpPort;

                    Debug.Log($"[CustomNetworkManager] 已分配可用端口：{port}（转换为ushort：{kcpPort}）");
                    return port;
                }
            }
        }
        Debug.LogError("[CustomNetworkManager] 端口范围[" + minPort + "-" + maxPort + "]内无可用端口！");
        return -1;
    }

    /// <summary>
    /// 创建房间前的预处理（清理旧端口+分配新端口）
    /// </summary>
    /// <returns>分配的端口号，失败返回-1</returns>
    public int PrepareForCreateRoom()
    {
        // 1. 先停止旧端口
        ForceStopCurrentPort();

        // 2. 等待端口释放
        System.Threading.Thread.Sleep(200);

        // 3. 分配新端口
        int port = AllocateAvailablePort();

        if (port == -1)
        {
            Debug.LogError("[CustomNetworkManager] 无法分配可用端口，创建房间失败！");
            return -1;
        }

        Debug.Log("[CustomNetworkManager] 已完成创建房间前的端口清理，分配端口：" + port);
        return port;
    }
    #endregion

    public override void Awake()
    {
        base.Awake();
        Instance = this;
        // 通用方式获取KcpTransport（兼容所有版本）
        _kcpTransport = FindObjectOfType<KcpTransport>();
        if (_kcpTransport == null)
        {
            Debug.LogWarning("[CustomNetworkManager] 未找到KcpTransport组件，将在运行时再次尝试获取");
        }
        else
        {
            // 初始化当前端口
            _currentUsedPort = _kcpTransport.Port;
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
    }
}