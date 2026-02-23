using System;
using System.Net;
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
    public GameMode GameMode;
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
    public ushort port;

    [NonSerialized] 
    public UnityEvent<ServerResponse> OnServerFound;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 初始化事件（防止空引用）
            if (OnServerFound == null)
            {
                OnServerFound = new UnityEvent<ServerResponse>();
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    protected override ServerRequest GetRequest() => new ServerRequest();

    protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
    {
        Uri uri = transport.ServerUri();

        if (transport is KcpTransport kcpTransport)
        {
            var uriBuilder = new UriBuilder(uri)
            {
                Port = kcpTransport.Port
            };
            uri = uriBuilder.Uri;
        }

        return new ServerResponse
        {
            serverId = ServerId,
            ipAddress = uri.Host,
            port = uri.Port,
            uri = uri,
            roomName = roomName,
            playerCount = playerCount,
            maxPlayers = maxPlayers,
            PlayerName = playerName,
            gameTime = gameTime,
            GoldScore = GoldScore
        };
    }

    protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
    {
        Debug.Log($"CLIENT: Received broadcast from {endpoint.Address}:{endpoint.Port} | 房间端口：{response.port}");

        if (response.uri != null)
        {
            string host = response.uri.Host;
            if (host == "localhost" || host == "127.0.0.1" || host == "0.0.0.0")
            {
                var uriBuilder = new UriBuilder(response.uri)
                {
                    Host = response.ipAddress,
                    Port = response.port
                };
                response.uri = uriBuilder.Uri;
            }
        }

        // 安全调用：先判空再触发事件
        if (OnServerFound != null)
        {
            OnServerFound.Invoke(response);
        }
    }

    public new void StopDiscovery()
    {
        base.StopDiscovery();
        Debug.Log("[CustomNetworkDiscovery] 已停止广播/发现");
    }

    public void SetPort(int port)
    {
        if (port < 0 || port > 65535)
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
        if (instance == this)
        {
            instance = null;
        }
        StopDiscovery();
        // 清空事件订阅，防止内存泄漏
        if (OnServerFound != null)
        {
            OnServerFound.RemoveAllListeners();
        }
    }
}