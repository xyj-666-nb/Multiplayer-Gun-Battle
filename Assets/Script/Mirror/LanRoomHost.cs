using Mirror;
using System.Collections;
using UnityEngine;

public class LanRoomHost : MonoBehaviour
{
    public CustomNetworkDiscovery discovery;
    private void Awake()
    {
        if (discovery == null)
            discovery = FindObjectOfType<CustomNetworkDiscovery>();
    }

    // 供创建房间按钮调用
    public void CreateRoom(string roomName, string playerName,  int maxPlayers = 8)
    {
        Debug.Log("HOST: StartHost + AdvertiseServer()");
        var nm = NetworkManager.singleton;

        // 优先设置房间信息
        discovery.roomName = string.IsNullOrWhiteSpace(roomName) ? "Room" : roomName;
        discovery.playerName = string.IsNullOrWhiteSpace(playerName) ? "Host" : playerName;

        discovery.maxPlayers = maxPlayers;
        discovery.playerCount = 1; // 初始房主

        // 启动 Host
        nm.maxConnections = maxPlayers;
        nm.StartHost();

        // 开始广播房间信息
        discovery.AdvertiseServer();

        StartCoroutine(WaitTime());
    }
    IEnumerator WaitTime()
    {
        yield return new WaitForSeconds(0.1f);
        Debug.Log("进行了广播，成功创建房间");
    }

    private void Update()
    {
        if (NetworkServer.active && discovery != null)
        {
            int players = 0;
            foreach (var kv in NetworkServer.connections)
            {
                if (kv.Value != null && kv.Value.isAuthenticated)
                    players++;
            }
            discovery.playerCount = Mathf.Max(1, players);
        }
    }

    public void StopRoom()
    {
        if (NetworkServer.active || NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
    }
}
