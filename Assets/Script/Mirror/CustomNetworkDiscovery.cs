using System;
using System.Net;
using System.Net.Sockets;
using kcp2k;
using Mirror;
using Mirror.Discovery;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct ServerRequest : NetworkMessage { }

[Serializable]
public struct ServerResponse : NetworkMessage
{
    public long serverId;
    public string ipAddress;
    public int port;
    public Uri uri;
    public string roomName;
    public int playerCount;
    public int maxPlayers;
    public GameMode GameMode; // 若未定义GameMode，需补充枚举：public enum GameMode { Default }
    public string PlayerName;
    public int gameTime;
    public int GoldScore;
}

public class CustomNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, ServerResponse>
{
    private static CustomNetworkDiscovery instance;
    public static CustomNetworkDiscovery Instance => instance;

    [Header("Custom Room Info")]
    public string roomName = "Room";
    public int playerCount = 1;
    public int maxPlayers = 8;
    public string playerName = "Host";
    public int gameTime = 0;
    public int GoldScore = 0;

    [Header("Network Config")]
    public ushort port = 7777; // 默认KCP端口

    [NonSerialized]
    public new UnityEvent<ServerResponse> OnServerFound;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            OnServerFound ??= new UnityEvent<ServerResponse>(); // 简化判空
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    ///获取本机真实局域网IP
    /// </summary>
    private string GetLocalLanIp()
    {
        foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork &&
                !ip.ToString().StartsWith("127.") &&
                !ip.ToString().StartsWith("169."))
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1"; // 兜底
    }

    protected override ServerRequest GetRequest() => new ServerRequest();

    protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
    {
        string realLanIp = GetLocalLanIp();
        int realPort = port;

        // 从KcpTransport获取实际端口
        if (transport is KcpTransport kcpTransport)
        {
            realPort = kcpTransport.Port;
        }

        Uri realUri = new UriBuilder("kcp", realLanIp, realPort).Uri;

        return new ServerResponse
        {
            serverId = ServerId,
            ipAddress = realLanIp, // 存储真实IP
            port = realPort,       // 存储真实端口
            uri = realUri,         // 传递完整Uri（kcp://真实IP:端口）
            roomName = roomName,
            playerCount = playerCount,
            maxPlayers = maxPlayers,
            PlayerName = playerName,
            gameTime = gameTime,
            GoldScore = GoldScore,
            GameMode = GameMode.Team_Battle // 补充默认值，避免空引用
        };
    }

    protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
    {
        string realSenderIp = endpoint.Address.ToString();
        int realSenderPort = response.port > 0 ? response.port : port;

        Uri fixedUri = new UriBuilder(response.uri)
        {
            Host = realSenderIp,
            Port = realSenderPort
        }.Uri;

  
        response.uri = fixedUri;
        response.ipAddress = realSenderIp;
        response.port = realSenderPort;

        Debug.Log($"CLIENT: Received broadcast from {realSenderIp}:{endpoint.Port} | 房间端口：{realSenderPort}");

        // 安全触发事件
        OnServerFound?.Invoke(response);
    }

    public new void StopDiscovery()
    {
        base.StopDiscovery();
        Debug.Log("[CustomNetworkDiscovery] 已停止广播/发现");
    }

    public void SetPort(int port)
    {
        if (port is < 0 or > 65535)
        {
            Debug.LogError($"[CustomNetworkDiscovery] 端口{port}超出范围（0-65535），使用默认端口7777");
            this.port = 7777;
        }
        else
        {
            this.port = (ushort)port;
        }
        Debug.Log($"[CustomNetworkDiscovery] 已设置房间端口：{this.port}");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        StopDiscovery();
        OnServerFound?.RemoveAllListeners(); // 简化判空
    }
}