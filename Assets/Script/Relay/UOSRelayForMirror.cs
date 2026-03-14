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

    // 内部玩家ID（按照官方示例，在Awake/Start里初始化）
    private string playerUuid;
    public string playerName;

    // 事件系统
    public static event Action OnRelayConnecting;
    public static event Action<string> OnRelaySuccess;
    public static event Action<string> OnRelayFailed;

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
        // 【完全照搬官方】在 Start() 里就初始化玩家数据
        InitializePlayerData();
    }

    /// <summary>
    /// 【照搬官方】初始化玩家数据
    /// </summary>
    private void InitializePlayerData()
    {
        playerUuid = Guid.NewGuid().ToString();
        playerName = "Player-" + playerUuid.Substring(0, 8);

        // 【关键】按照官方示例，尽早设置玩家数据
        relayTransport.SetPlayerData(playerUuid, playerName);
        Debug.Log($"【UOS】已初始化玩家数据：UUID={playerUuid}, Name={playerName}");
    }

    /// <summary>
    /// 【房主】创建房间（完全照搬官方 StartHost 流程）
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

        // 【完全照搬官方】异步创建房间
        StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
        {
            Name = "游戏房间",
            MaxPlayers = maxPlayers,
            OwnerId = playerUuid, // 【关键】OwnerId 必须和 SetPlayerData 里的一致
            // 【修改】先试 Public 房间，不设 JoinCode，避免密码验证问题
            Visibility = LobbyRoomVisibility.Public,
            // JoinCode = "U", // 【照搬官方】Private房间才需要，先注释掉
        }, OnCreateRoomComplete));
    }

    /// <summary>
    /// 【照搬官方】房间创建完成回调
    /// </summary>
    private void OnCreateRoomComplete(CreateRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK)
        {
            Debug.Log("【UOS】Create Room succeed.");

            if (resp.Status == LobbyRoomStatus.ServerAllocated)
            {
                relayTransport.SetRoomData(resp);

                // 保存 RoomCode 给客户端用
                currentRoomCode = resp.RoomCode;
                Debug.Log($"【UOS】【重要】客户端加入用的房间码：{currentRoomCode}");
                Debug.LogWarning("【UOS】请把上面的RoomCode分享给客户端！");

                // 【完全照搬官方】直接启动 Host，不做任何等待
                Debug.Log("【UOS】正在启动 Host...");
                customManager.StartHost();

                // 触发成功事件
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
    /// 【客户端】通过RoomCode加入（基于官方流程，改用RoomCode查询）
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
        Debug.Log($"【UOS】正在查询房间，房间码：{roomCode}");

        // 【修改】官方是用 ListRoom，我们用 QueryRoomByRoomCode 来满足"码加入"的需求
        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, OnQueryRoomComplete));
    }

    /// <summary>
    /// 【照搬官方】房间查询完成回调
    /// </summary>
    private void OnQueryRoomComplete(QueryRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK)
        {
            Debug.Log("【UOS】Query Room succeed.");

            // 【完全照搬官方】设置房间数据
            relayTransport.SetRoomData(resp);

            // 【完全照搬官方】不设置 JoinCode，直接启动 Client
            Debug.Log("【UOS】正在启动 Client...");
            customManager.StartClient();

            OnRelaySuccess?.Invoke(resp.RoomCode);
        }
        else
        {
            string error = $"Query Room Fail By Lobby Service, Code: {resp.Code}";
            Debug.LogError($"【UOS】{error}");
            OnRelayFailed?.Invoke(error);
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