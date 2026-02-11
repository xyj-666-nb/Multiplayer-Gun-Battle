using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// 发起房间搜索、缓存已发现的房间、处理房间加入逻辑、停止搜索
/// </summary>
public class LanRoomClientBrowser : MonoBehaviour
{
    public CustomNetworkDiscovery discovery;//获取网络发现组件引用，这里的主要目的是接收房间信息回调，发起搜索和停止搜索也可以通过这个组件调用

    /// <summary>
    /// 已发现的房间缓存字典
    /// Key：服务器唯一ID，避免重复显示同一个房间
    /// Value：房间完整信息，用于UI展示和连接（ServerResponse房主传入过来的房间信息结构体用于显示信息）
    /// </summary>
    private readonly Dictionary<long, ServerResponse> discovered = new();

    private void Awake()
    {
        discovery= CustomNetworkDiscovery.Instance;//获取单例(获取当前的网络发现组件的单例)
    }

    /// <summary>
    /// 开始扫描局域网内的房间
    /// 点击“刷新/搜索房间”按钮时调用
    /// </summary>
    public void StartScan()
    {
        // 清空历史缓存，避免旧房间信息干扰
        discovered.Clear();
        // 调用自定义发现组件，开始向局域网发送广播请求
        discovery.StartDiscovery();//打开搜索
    }

    /// <summary>
    /// 房间信息接收回调
    /// 每当客户端收到一个房主的房间广播，就会触发该方法
    /// </summary>
    /// <param name="info">房主返回的房间完整信息</param>
    public void OnServerFound(ServerResponse info)
    {

        discovered[info.serverId] = info;//注册房间信息到本地缓存字典，key是服务器唯一ID，value是房间信息结构体
        Debug.Log($"发现房间: {info.roomName}  {info.playerCount}/{info.maxPlayers}  @ {info.uri}");
    }

    /// <summary>
    /// 【房间列表项按钮回调】通过房间Uri加入指定房间
    /// 点击“加入房间”按钮时，传入该房间的Uri调用此方法
    /// </summary>
    /// <param name="uri">目标房间的连接地址（由房主广播返回）</param>
    public void JoinByUri(System.Uri uri)//加入到专门的房间地址上，参数是房主广播返回的Uri
    {
        // 获取Mirror全局唯一的NetworkManager
        var nm = NetworkManager.singleton;
        // Mirror 1.x/2.x 通用写法：通过Uri直接连接到目标服务器(传入Ip坐标就能开始链接)
        nm.StartClient(uri);
    }

    /// <summary>
    /// 停止扫描局域网房间
    /// 点击“退出搜索面板”“取消搜索”按钮时调用
    /// </summary>
    public void StopScan()
    {
        // 调用自定义发现组件，停止发送广播请求
        discovery.StopDiscovery();
    }
}