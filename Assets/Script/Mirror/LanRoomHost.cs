using Mirror;
using UnityEngine;

public class LanRoomHost : MonoBehaviour
{
    private CustomNetworkDiscovery discovery;
    [Header("更新房间信息的频率，单位毫秒")]
    public int UpdateAdvertiseServerTime = 200;
    private int CurrentTimerIndex;

    private void Awake()
    {
        discovery = CustomNetworkDiscovery.Instance;
        if (discovery == null)
        {
            discovery = FindObjectOfType<CustomNetworkDiscovery>();
            if (discovery == null)
            {
                Debug.LogError("[LanRoomHost] 未找到CustomNetworkDiscovery组件！");
                return;
            }
        }
    }

    public void CreateRoom(string roomName, string playerName, int GameTime, int GoalScore, int maxPlayers = 8)
    {
        Debug.Log("HOST: StartHost + AdvertiseServer()");

        CustomNetworkManager nm = null;
        if (CustomNetworkManager.Instance != null)
        {
            nm = CustomNetworkManager.Instance;
        }
        else
        {
            nm = FindObjectOfType<CustomNetworkManager>();
        }
        if (nm == null)
        {
            nm = NetworkManager.singleton as CustomNetworkManager;
        }

        try
        {
            int port = nm.PrepareForCreateRoom();
            if (port == -1)
            {
                Debug.LogError("[LanRoomHost] 端口分配失败，无法创建房间！");
                return;
            }

            discovery.roomName = string.IsNullOrWhiteSpace(roomName) ? "默认房间" : roomName;
            discovery.playerName = string.IsNullOrWhiteSpace(playerName) ? "房主" : playerName;

            discovery.SetPort(port);

            discovery.maxPlayers = maxPlayers;
            discovery.playerCount = 1;
            discovery.gameTime = GameTime;
            discovery.GoldScore = GoalScore;

            nm.maxConnections = maxPlayers;
            nm.StartHost();

            discovery.AdvertiseServer();

            Debug.Log($"成功创建房间（端口：{port}），开始广播");

            if (CountDownManager.Instance == null)
            {
                Debug.LogError("[LanRoomHost] CountDownManager.Instance 为空！");
                return;
            }
            CurrentTimerIndex = CountDownManager.Instance.CreateTimer_Permanent(false, 200, UpdateAdvertiseServer);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LanRoomHost] 创建房间异常：{e.Message}\n{e.StackTrace}");
        }
    }

    public void UpdateAdvertiseServer()
    {
        if (!NetworkServer.active || discovery == null)
        {
            Debug.LogWarning("[LanRoomHost] 服务器未激活或discovery为空，跳过房间信息更新");
            return;
        }

        int players = 0;
        foreach (var kv in NetworkServer.connections)
        {
            if (kv.Value != null && kv.Value.isAuthenticated)
                players++;
        }
        discovery.playerCount = Mathf.Max(1, players);
    }

    public void StopRoom()
    {
        if (CountDownManager.Instance != null)
        {
            CountDownManager.Instance.StopTimer(CurrentTimerIndex);
        }
        if (discovery != null)
        {
            discovery.StopDiscovery();
        }
        if (CustomNetworkManager.Instance != null)
        {
            CustomNetworkManager.Instance.ForceStopCurrentPort();
        }
    }

    private void OnDestroy()
    {
        StopRoom();
    }
}