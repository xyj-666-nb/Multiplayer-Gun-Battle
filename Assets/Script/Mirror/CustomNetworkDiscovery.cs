using System;
using System.Net;
using Mirror;      
using Mirror.Discovery; // Mirror网络发现
using UnityEngine;

/// <summary>
/// 客户端向房主发送的「房间搜索请求」消息结构体
/// 继承NetworkMessage：标记为Mirror可网络传输的消息
/// 空结构体仅作为“触发搜索”的信号，无需携带数据
/// </summary>
[Serializable]
public struct ServerRequest : NetworkMessage { }//服务需求

/// <summary>
/// 主要的作用就是广播和接受信息
/// </summary>
[Serializable]
public struct ServerResponse : NetworkMessage//服务响应
{
    public long serverId;          // 服务器唯一ID
    public string ipAddress;       // 服务器实际IP地址
    public int port;               // 服务器端口号
    public Uri uri;                // 服务器完整连接地址
    public string roomName;        // 房间名称
    public int playerCount;        // 当前房间在线人数
    public int maxPlayers;         // 房间最大容纳人数
    public GameMode GameMode;      // 房间的游戏模式
    public string PlayerName;      // 房主的昵称
}

/// <summary>
/// 自定义网络发现组件
/// 继承NetworkDiscoveryBase：复用Mirror的局域网发现底层逻辑，仅自定义消息格式和业务逻辑
/// 泛型参数：<请求消息类型, 响应消息类型>
/// </summary>
public class CustomNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, ServerResponse>
{
    private static CustomNetworkDiscovery instance;
    public static CustomNetworkDiscovery Instance=> instance;
   

    [Header("Custom Room Info")] 
    public string roomName = "Room";   // 房间名称
    public int playerCount = 1;        // 当前房间人数
    public int maxPlayers = 8;         // 房间最大人数
    public string playerName = "Host"; // 房主昵称

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 【客户端调用】生成发送给房主的搜索请求
    /// </summary>
    /// <returns>空的搜索请求结构体</returns>
    protected override ServerRequest GetRequest() => new ServerRequest();//客户端进行搜索的时候就会调用这个方法，生成一个空的请求消息

    /// <summary>
    /// 【房主端调用】处理客户端的搜索请求，返回房间信息
    /// </summary>
    /// <param name="request">客户端发来的搜索请求（无数据）</param>
    /// <param name="endpoint">客户端的网络端点（IP+端口）</param>
    /// <returns>组装好的房间信息响应</returns>
    protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)//收到请求就会调用这个方法，生成一个包含房间信息的响应消息，然后发送出去
    {
        // 获取当前服务器的连接地址
        Uri uri = transport.ServerUri();//获取自己的具体地址数据，包括IP和端口

        // 组装响应数据，返回给客户端
        return new ServerResponse//建立一个响应消息，包含房间的所有核心信息，客户端收到后可以直接显示在UI上，并使用其中的连接地址进行连接
        {
            serverId = ServerId,          // Mirror内置的服务器唯一ID
            ipAddress = uri.Host,         // 服务器IP
            port = uri.Port,              // 服务器端口
            uri = uri,                    // 服务器原始连接地址

            roomName = roomName,          // 自定义房间名
            playerCount = playerCount,    // 当前在线人数
            maxPlayers = maxPlayers,      // 最大人数
            PlayerName = playerName       // 房主昵称
        };
    }

    /// <summary>
    /// 【客户端调用】处理房主返回的房间信息响应
    /// </summary>
    /// <param name="response">房主返回的房间信息</param>
    /// <param name="endpoint">房主的网络端点（IP+端口）</param>
    protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)//客户端收到房主的响应后会调用这个方法，处理房间信息并触发事件通知UI更新，这里是客户端方面调用的逻辑，接受到的数据会自己传入
    {
        Debug.Log($"CLIENT: Received broadcast from {endpoint.Address}:{endpoint.Port}");

        if (response.uri != null)
        {
            string host = response.uri.Host;
            // 判断是否是本地回环地址，需要替换为实际IP
            if (host == "localhost" || host == "127.0.0.1" || host == "0.0.0.0")
            {
                // 重建Uri，替换为房主实际IP
                var uriBuilder = new UriBuilder(response.uri)
                {
                    Host = response.ipAddress, // 使用修复后的IP
                    Port = response.port      // 保留端口
                };
                response.uri = uriBuilder.Uri; // 更新为可跨设备访问的Uri
            }
        }

        OnServerFound.Invoke(response);//触发事件，通知UI层有新的房间信息到达，可以更新房间列表显示了
    }
}