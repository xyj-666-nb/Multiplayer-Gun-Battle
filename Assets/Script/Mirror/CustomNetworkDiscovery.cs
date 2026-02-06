using System;
using System.Net;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

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

    public string PlayerName;   // 房主名称
}

public class CustomNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, ServerResponse>
{
   
    [Header("Custom Room Info")]
    public string roomName = "Room";
    public int playerCount = 1;

    public int maxPlayers = 8;

    public string playerName = "Host";     

    protected override ServerRequest GetRequest() => new ServerRequest();
    protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
    {
        Uri uri = transport.ServerUri();

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
        };
    }

    protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
    {
        Debug.Log($"CLIENT: Received broadcast from {endpoint.Address}:{endpoint.Port}");

        // 修复 localhost → 实际 IP
        if (response.uri != null)
        {
            string h = response.uri.Host;
            if (h == "localhost" || h == "127.0.0.1" || h == "0.0.0.0")
            {
                var b = new UriBuilder(response.uri)
                {
                    Host = response.ipAddress,
                    Port = response.port
                };
                response.uri = b.Uri;
            }
        }

        OnServerFound.Invoke(response);
    }
}
