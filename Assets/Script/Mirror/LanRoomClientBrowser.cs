using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LanRoomClientBrowser : MonoBehaviour
{
    public CustomNetworkDiscovery discovery;

    // 用来去重/填 UI
    private readonly Dictionary<long, ServerResponse> discovered = new();
    private void Awake()
    {
        if (discovery == null)
            discovery = FindObjectOfType<CustomNetworkDiscovery>();
    }
       
    // “开始搜索”按钮
    public void StartScan()
    {
        discovered.Clear();
        discovery.StartDiscovery();     // 开始发送广播请求
    }

    // 在 Inspector 的 OnServerFound 里把这个方法拖进去
    public void OnServerFound(ServerResponse info)
    {
        discovered[info.serverId] = info;

        // TODO：刷新你的 UI 列表（显示 info.roomName、info.playerCount/maxPlayers、info.uri.Host）
        Debug.Log($"发现房间: {info.roomName}  {info.playerCount}/{info.maxPlayers}  @ {info.uri}");
    }

    // 列表项“加入”按钮回调：把该项的 uri 传进来
    public void JoinByUri(System.Uri uri)
    {
        var nm = NetworkManager.singleton;
        // Mirror 1.x/2.x 都支持通过 Uri 直接连接
        nm.StartClient(uri);
    }

    public void StopScan()
    {
        discovery.StopDiscovery();  // 停止继续发送请求（可选）
    }
}
