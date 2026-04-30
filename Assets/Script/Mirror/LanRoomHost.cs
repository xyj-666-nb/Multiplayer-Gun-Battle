using Mirror;
using UnityEngine;

public class LanRoomHost : MonoBehaviour
{
    private CustomNetworkDiscovery discovery;
    [Header("更新房间信息的频率，单位毫秒")]
    public int UpdateAdvertiseServerTime = 200;
    private int CurrentTimerIndex = -1;
    private bool _isAdvertising;

    private void Awake()
    {
        discovery = CustomNetworkDiscovery.Instance;
        if (discovery == null)
        {
            discovery = FindObjectOfType<CustomNetworkDiscovery>();
            if (discovery == null)
            {
                /* Debug.LogError("[LanRoomHost] 未找到CustomNetworkDiscovery组件！"); */
                return;
            }
        }
    }

    public void CreateRoom(string roomName, string playerName, int GameTime, int GoalScore, int maxPlayers = CustomNetworkManager.DefaultRoomPlayerLimit)
    {
        /* Debug.Log("HOST: StartHost + AdvertiseServer()"); */

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
                /* Debug.LogError("[LanRoomHost] 端口分配失败，无法创建房间！"); */
                return;
            }

            discovery.roomName = string.IsNullOrWhiteSpace(roomName) ? "默认房间" : roomName;
            discovery.playerName = string.IsNullOrWhiteSpace(playerName) ? "房主" : playerName;

            discovery.SetPort(port);
            maxPlayers = nm.ApplyRoomPlayerLimit(maxPlayers);

            discovery.maxPlayers = maxPlayers;
            discovery.playerCount = 1;
            discovery.gameTime = GameTime;
            discovery.GoldScore = GoalScore;

            nm.maxConnections = maxPlayers;
            nm.StartHost();

            StartAdvertisingIfNeeded();

            /* Debug.Log($"成功创建房间（端口：{port}），开始广播"); */

            if (CountDownManager.Instance == null)
            {
                /* Debug.LogError("[LanRoomHost] CountDownManager.Instance 为空！"); */
                return;
            }
            CurrentTimerIndex = CountDownManager.Instance.CreateTimer_Permanent(false, UpdateAdvertiseServerTime, UpdateAdvertiseServer);
        }
        catch (System.Exception e)
        {
            /* Debug.LogError($"[LanRoomHost] 创建房间异常：{e.Message}\n{e.StackTrace}"); */
        }
    }

    public void UpdateAdvertiseServer()
    {
        if (!NetworkServer.active || discovery == null)
        {
            return;
        }

        CustomNetworkManager nm = CustomNetworkManager.Instance != null
            ? CustomNetworkManager.Instance
            : NetworkManager.singleton as CustomNetworkManager;

        int players = nm != null ? nm.GetCurrentPlayerCount() : CountAuthenticatedConnections();
        discovery.playerCount = Mathf.Max(1, players);

        bool canJoin = nm == null || !nm.IsRoomClosedToNewPlayers();
        bool hasSpace = discovery.maxPlayers <= 0 || discovery.playerCount < discovery.maxPlayers;

        if (canJoin && hasSpace)
            StartAdvertisingIfNeeded();
        else
            StopAdvertisingIfNeeded();
    }

    private int CountAuthenticatedConnections()
    {
        int players = 0;
        foreach (var kv in NetworkServer.connections)
        {
            if (kv.Value != null && kv.Value.isAuthenticated)
                players++;
        }
        return players;
    }

    private void StartAdvertisingIfNeeded()
    {
        if (_isAdvertising || discovery == null)
            return;

        discovery.AdvertiseServer();
        _isAdvertising = true;
    }

    private void StopAdvertisingIfNeeded()
    {
        if (!_isAdvertising || discovery == null)
            return;

        discovery.StopDiscovery();
        _isAdvertising = false;
    }

    public void StopRoom()
    {
        if (CountDownManager.Instance != null && CurrentTimerIndex != -1)
        {
            CountDownManager.Instance.StopTimer(CurrentTimerIndex);
            CurrentTimerIndex = -1;
        }
        if (discovery != null)
        {
            discovery.StopDiscovery();
            _isAdvertising = false;
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