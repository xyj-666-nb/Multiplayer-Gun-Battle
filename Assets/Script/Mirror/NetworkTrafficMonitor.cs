using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class NetworkTrafficMonitor : MonoBehaviour
{
    public static NetworkTrafficMonitor Instance { get; private set; }

    private NetworkManager networkManager;

    private long totalSendBytes = 0;
    private long totalReceiveBytes = 0;
    private bool heartbeatVerified = false;
    private float heartbeatInterval = 2f;
    private float lastHeartbeatTime = 0f;

    // Mirror推荐自定义消息结构体
    public struct HeartbeatMsg : NetworkMessage
    {
        public string content;
    }

    private const string HEARTBEAT_CONTENT = "HB_CHECK";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("[通信检测]  未找到NetworkManager！");
            return;
        }

        // 推荐用泛型注册消息
        NetworkClient.RegisterHandler<HeartbeatMsg>(OnHeartbeatClient);
        NetworkServer.RegisterHandler<HeartbeatMsg>(OnHeartbeatServer);

        Debug.Log("[通信检测]  初始化完成（Mirror 4.x+ 推荐写法）");
    }

    void Update()
    {
        if (Time.time - lastHeartbeatTime > heartbeatInterval)
        {
            SendHeartbeat();
            lastHeartbeatTime = Time.time;
        }
    }

    void OnDestroy()
    {
        // Mirror 4.x+ 不再需要手动UnregisterHandler，自动清理
        if (Instance == this) Instance = null;
    }

    private void SendHeartbeat()
    {
        HeartbeatMsg msg = new HeartbeatMsg { content = HEARTBEAT_CONTENT };

        // 统计消息字节数
        NetworkWriter writer = new NetworkWriter();
        writer.WriteString(HEARTBEAT_CONTENT);
        int msgBytes = writer.ToArray().Length;

        if (NetworkServer.active)
        {
            foreach (var kvp in NetworkServer.connections)
            {
                var conn = kvp.Value;
                if (conn != null)
                {
                    conn.Send(msg);
                    totalSendBytes += msgBytes;
                    Debug.Log($"[通信检测-服务端]  发送心跳到 ConnId:{conn.connectionId} | 字节：{msgBytes} | 累计发送：{totalSendBytes}");
                }
            }
        }

        if (NetworkClient.active && NetworkClient.isConnected)
        {
            NetworkClient.Send(msg);
            totalSendBytes += msgBytes;
            Debug.Log($"[通信检测-客户端]  发送心跳到 {networkManager.networkAddress} | 字节：{msgBytes} | 累计发送：{totalSendBytes}");
        }
    }

    // 客户端收到心跳
    private void OnHeartbeatClient(HeartbeatMsg msg)
    {
        if (msg.content == HEARTBEAT_CONTENT)
        {
            heartbeatVerified = true;
            int size = msg.content.Length;
            totalReceiveBytes += size;
            Debug.Log($"[通信检测-客户端]  收到心跳 | 字节：{size} | 累计接收：{totalReceiveBytes} | ? 验证成功");
        }
    }

    // 服务端收到心跳
    private void OnHeartbeatServer(NetworkConnectionToClient conn, HeartbeatMsg msg)
    {
        if (msg.content == HEARTBEAT_CONTENT)
        {
            heartbeatVerified = true;
            int size = msg.content.Length;
            totalReceiveBytes += size;
            Debug.Log($"[通信检测-服务端]  收到心跳(ConnId:{conn.connectionId}) | 字节：{size} | 累计接收：{totalReceiveBytes} | ? 验证成功");
        }
    }

    // 辅助方法
    public void LogSendData(string targetInfo, int byteCount)
    {
        totalSendBytes += byteCount;
        Debug.Log($"[通信检测-业务]  发送到 {targetInfo} | 字节：{byteCount} | 累计发送：{totalSendBytes}");
    }

    public void LogReceiveData(string sourceInfo, int byteCount)
    {
        totalReceiveBytes += byteCount;
        Debug.Log($"[通信检测-业务]  收到({sourceInfo}) | 字节：{byteCount} | 累计接收：{totalReceiveBytes}");
    }

    [ContextMenu(" 重置通信统计数据")]
    public void ResetTrafficStats()
    {
        totalSendBytes = 0;
        totalReceiveBytes = 0;
        heartbeatVerified = false;
        lastHeartbeatTime = 0f;
        Debug.Log("[通信检测]  统计数据已重置");
    }
}
