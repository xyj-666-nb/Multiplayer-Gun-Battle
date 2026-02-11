using Mirror;
using UnityEngine;

public class LanRoomHost : MonoBehaviour
{
    private CustomNetworkDiscovery discovery;//获取网络发现组件引用
    [Header("更新房间信息的频率，单位毫秒(1000毫秒等于1秒)")]
    public int UpdateAdvertiseServerTime = 200;//0.2秒一次
    private int CurrentTimerIndex;//当前计时器的唯一ID
    private void Awake()
    {
        discovery = CustomNetworkDiscovery.Instance;//获取单例
    }

    // 供创建房间按钮调用
    public void CreateRoom(string roomName, string playerName,  int maxPlayers = 8)//获取玩家输入的房间名称、玩家名称和最大人数
    {
        Debug.Log("HOST: StartHost + AdvertiseServer()");
        var nm = NetworkManager.singleton;

        // 优先设置房间信息(对信息的判空)
        discovery.roomName = string.IsNullOrWhiteSpace(roomName) ? "Room" : roomName;
        discovery.playerName = string.IsNullOrWhiteSpace(playerName) ? "Host" : playerName;

        discovery.maxPlayers = maxPlayers;
        discovery.playerCount = 1; // 初始房主

        // 启动 Host
        nm.maxConnections = maxPlayers;
        nm.StartHost();//启动服务器，等待客户端连接

        discovery.AdvertiseServer();// 开始广播房间信息

        Debug.Log("进行了广播，成功创建房间");
        //打开持续广播，设置广播频率
        CurrentTimerIndex= CountDownManager.Instance.CreateTimer_Permanent(false,200, UpdateAdvertiseServer);//创建一个永久的计时器，每200毫秒调用一次UpdateAdvertiseServer方法，确保房间信息持续更新并广播出去
    }

    public void UpdateAdvertiseServer()//更新房间信息，确保广播出去的房间信息是最新的
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

    private void Update()
    {
     
    }

    public void StopRoom()//关闭房间，当房主离开房间以及正式开始游戏后调用
    {
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
            //关闭永久计时器的调用
            CountDownManager.Instance.StopTimer(CurrentTimerIndex);//传入唯一ID
        }
    }
}
