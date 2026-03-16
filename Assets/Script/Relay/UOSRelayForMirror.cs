using UnityEngine;
using Mirror;
using System;
using System.Collections;
using Unity.Sync.Relay.Transport.Mirror;
using Unity.Sync.Relay;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Model;

public class UOSRelaySimple : MonoBehaviour
{
    // 单例
    public static UOSRelaySimple Instance { get; private set; }

    [Header("引用")]
    public CustomNetworkManager customManager;
    public RelayTransportMirror relayTransport;

    [Header("设置")]
    public int maxPlayers = 4;
    public string currentRoomCode; // 保存给客户端用的 RoomCode

    // 内部玩家ID
    private string playerUuid;
    public string playerName;

    // 事件系统
    public static event Action OnRelayConnecting;
    public static event Action<string> OnRelaySuccess;
    public static event Action<string> OnRelayFailed;

    public static event Action<string> OnQuerySuccess; // 仅查询成功
    public static event Action<string> OnQueryFailed;  // 仅查询失败

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (customManager == null)
            customManager = GetComponent<CustomNetworkManager>();
        if (relayTransport == null)
            relayTransport = GetComponent<RelayTransportMirror>();
    }

    private void Start()
    {
        InitializePlayerData();
    }

    /// <summary>
    /// 初始化玩家数据
    /// </summary>
    private void InitializePlayerData()
    {
        playerUuid = Guid.NewGuid().ToString();
        playerName = "Player-" + playerUuid.Substring(0, 8);
        relayTransport.SetPlayerData(playerUuid, playerName);
        Debug.Log($"【UOS】已初始化玩家数据：UUID={playerUuid}, Name={playerName}");
    }

    /// <summary>
    /// 【房主】创建房间
    /// </summary>
    public void StartRelayHost()
    {
        if (relayTransport == null)
        {
            OnRelayFailed?.Invoke("未找到RelayTransportMirror组件！");
            return;
        }

        OnRelayConnecting?.Invoke();
        Debug.Log("【UOS】开始创建房间...");

        StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
        {
            Name = "游戏房间",
            MaxPlayers = maxPlayers,
            OwnerId = playerUuid,
            Visibility = LobbyRoomVisibility.Public,
        }, OnCreateRoomComplete));
    }

    /// <summary>
    /// 房间创建完成回调
    /// </summary>
    private void OnCreateRoomComplete(CreateRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK)
        {
            Debug.Log("【UOS】Create Room succeed.");

            if (resp.Status == LobbyRoomStatus.ServerAllocated)
            {
                relayTransport.SetRoomData(resp);
                currentRoomCode = resp.RoomCode;
                Debug.Log($"【UOS】【重要】客户端加入用的房间码：{currentRoomCode}");

                Debug.Log("【UOS】正在启动 Host...");
                customManager.StartHost();
                OnRelaySuccess?.Invoke(currentRoomCode);
            }
            else
            {
                string error = "Room Status Exception : " + resp.Status.ToString();
                Debug.LogError($"【UOS】{error}");
                OnRelayFailed?.Invoke(error);
            }
        }
        else
        {
            string error = "Create Room Fail By Lobby Service, Code: " + resp.Code;
            Debug.LogError($"【UOS】{error}");
            OnRelayFailed?.Invoke(error);
        }
    }

    /// <summary>
    /// 仅查询房间是否存在
    /// </summary>
    public void QueryRoomOnly(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            OnQueryFailed?.Invoke("请输入有效的房间码！");
            return;
        }
        if (relayTransport == null)
        {
            OnQueryFailed?.Invoke("未找到RelayTransportMirror组件！");
            return;
        }

        Debug.Log($"【UOS】正在查询房间，房间码：{roomCode}");
        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, OnQueryRoomComplete));
    }

    /// <summary>
    /// 通过RoomCode加入
    /// </summary>
    public void StartRelayClient(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            OnRelayFailed?.Invoke("请输入有效的房间码！");
            return;
        }
        if (relayTransport == null)
        {
            OnRelayFailed?.Invoke("未找到RelayTransportMirror组件！");
            return;
        }

        OnRelayConnecting?.Invoke();
        Debug.Log($"【UOS】正在连接房间，房间码：{roomCode}");
        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, (resp) => {
            if (resp.Code == (uint)RelayCode.OK)
            {
                Debug.Log("【UOS】Query Room succeed, starting client...");
                relayTransport.SetRoomData(resp);
                customManager.StartClient();
                OnRelaySuccess?.Invoke(resp.RoomCode);
            }
            else
            {
                string error = $"连接失败，错误码：{resp.Code}";
                Debug.LogError($"【UOS】{error}");
                OnRelayFailed?.Invoke(error);
            }
        }));
    }

    /// <summary>
    ///房间查询完成回调
    /// </summary>
    private void OnQueryRoomComplete(QueryRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK)
        {
            Debug.Log("【UOS】Query Room succeed.");
            // 仅触发查询成功事件，让 UI 层决定是否连接
            OnQuerySuccess?.Invoke(resp.RoomCode);
        }
        else
        {
            string finalUIMsg; // 给玩家看的提示
            string logMsg;      // 给控制台看的日志

            if (resp.Code == 10032)
            {
                finalUIMsg = "未找到房间";
                logMsg = $"【UOS】{finalUIMsg}，错误码：{resp.Code}（这是正常业务提示，非报错）";

                Debug.LogWarning(logMsg);
            }
            else
            {
                finalUIMsg = $"查询失败，请检查网络";
                logMsg = $"【UOS】查询房间异常失败，错误码：{resp.Code}";

                Debug.LogError(logMsg);
            }

            OnQueryFailed?.Invoke(finalUIMsg);
        }
    }

    /// <summary>
    /// 停止Relay连接并清理资源
    /// </summary>
    public void StopRelay()
    {
        Debug.Log("【UOS】正在停止Relay连接...");

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            if (customManager != null)
                customManager.StopHost();
            else
                NetworkManager.singleton.StopHost();
        }

        currentRoomCode = "";
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}