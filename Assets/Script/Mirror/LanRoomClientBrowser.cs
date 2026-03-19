using kcp2k;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sync.Relay.Transport.Mirror; // 新增：引入Relay传输层命名空间

public class LanRoomClientBrowser : MonoBehaviour
{
    public CustomNetworkDiscovery discovery;
    private readonly Dictionary<long, ServerResponse> discoveredRooms = new();
    private const int KCP_DEFAULT_PORT = 7777;

    private void Awake()
    {
        // 自动查找+单例容错
        discovery = CustomNetworkDiscovery.Instance;
        if (discovery == null)
        {
            Debug.LogError("[LanRoomClientBrowser] 未找到CustomNetworkDiscovery单例！");
        }
        else
        {
            Debug.Log("[LanRoomClientBrowser] 成功获取CustomNetworkDiscovery单例");
        }
    }

    /// <summary>
    /// 开始扫描房间（清空缓存+启动发现）
    /// </summary>
    public void StartScan()
    {
        if (discovery == null) return;

        discoveredRooms.Clear();
        discovery.StartDiscovery();
    }

    /// <summary>
    /// 停止扫描房间
    /// </summary>
    public void StopScan()
    {
        if (discovery == null) return;

        discovery.StopDiscovery();
        Debug.Log("[LanRoomClientBrowser] 已停止扫描局域网房间");
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public void JoinByUri(Uri roomUri)
    {
        if (roomUri == null)
        {
            Debug.LogError("[LanRoomClientBrowser] 房间Uri为空，无法连接");
            return;
        }

        string serverIp = roomUri.Host;
        int serverPort = roomUri.Port > 0 ? roomUri.Port : KCP_DEFAULT_PORT;

        if (string.IsNullOrEmpty(serverIp) || serverIp is "localhost" or "127.0.0.1")
        {
            Debug.LogError("[LanRoomClientBrowser] 房间IP无效，无法连接");
            return;
        }

        CustomNetworkManager customNetMgr = CustomNetworkManager.Instance;
        if (customNetMgr != null)
        {
            customNetMgr.SwitchToLanMode();

            KcpTransport kcp = customNetMgr.GetComponent<KcpTransport>();
            RelayTransportMirror relay = customNetMgr.GetComponent<RelayTransportMirror>();

            if (kcp != null)
                kcp.enabled = true;
            if (relay != null) 
                relay.enabled = false;

            customNetMgr.transport = kcp;
            Transport.active = kcp;

            Debug.Log($"[LanRoomClientBrowser] 强制切换到KCP模式，Transport={customNetMgr.transport.GetType().Name}");
        }
        // ========== 修复结束 ==========

        Debug.Log($"[LanRoomClientBrowser] 开始连接：IP={serverIp}, 端口={serverPort}, Uri={roomUri}");
        NetworkManager netMgr = FindObjectOfType<NetworkManager>();
        if (netMgr == null)
        {
            Debug.LogError("[LanRoomClientBrowser] 未找到NetworkManager！");
            return;
        }

        netMgr.networkAddress = serverIp;
        KcpTransport kcpTransport = netMgr.GetComponent<KcpTransport>();
        if (kcpTransport != null)
        {
            kcpTransport.Port = (ushort)serverPort;
            Debug.Log($"[LanRoomClientBrowser] 已设置KCP端口={serverPort}");
        }

        try
        {
            netMgr.StartClient();
            Debug.Log("[LanRoomClientBrowser] StartClient()调用成功，等待连接回调");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LanRoomClientBrowser] 连接失败：{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 外部获取已发现的房间列表（用于扩展）
    /// </summary>
    public List<ServerResponse> GetDiscoveredRooms()
    {
        return new List<ServerResponse>(discoveredRooms.Values);
    }
}