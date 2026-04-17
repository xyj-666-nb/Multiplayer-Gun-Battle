using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Sync.Relay.Transport.Mirror;
using Unity.Sync.Relay;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Model;

public class UOSRelaySimple : MonoBehaviour
{
    public bool forceAndroidMode = true;
    public static UOSRelaySimple Instance { get; private set; }

    [Header("引用")]
    public CustomNetworkManager customManager;
    public RelayTransportMirror relayTransport;

    [Header("设置")]
    public int maxPlayers = 4;
    public string currentRoomCode;

    // 匹配房间固定配置
    private const string MATCH_ROOM_NAME_PREFIX = "PUBLIC_MATCH_";

    private string playerUuid;
    public string playerName;

    // 事件
    public static event Action OnRelayConnecting;
    public static event Action<string> OnRelaySuccess;
    public static event Action<string> OnRelayFailed;
    public static event Action<string> OnQuerySuccess;
    public static event Action<string> OnQueryFailed;
    public static event Action<List<LobbyRoom>> OnRoomListSuccess;

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
            customManager = FindObjectOfType<CustomNetworkManager>() ?? GetComponent<CustomNetworkManager>();

        if (relayTransport == null)
            relayTransport = FindObjectOfType<RelayTransportMirror>() ?? GetComponent<RelayTransportMirror>();
    }

    private void Start()
    {
        bool isAndroid = Application.platform.ToString().Contains("Android") || forceAndroidMode;
        if (isAndroid)
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        InitializePlayerData();
    }

    private void InitializePlayerData()
    {
        playerUuid = Guid.NewGuid().ToString();
        playerName = "DefaultPlayer";

        if (relayTransport != null)
            relayTransport.SetPlayerData(playerUuid, playerName);
        else
        {
            Debug.LogError("【UOS】relayTransport 为 null");
            OnRelayFailed?.Invoke("Relay 组件未找到");
        }
    }

    public void GetPlayerName(string PlayerName)
    {
        playerName = PlayerName;
    }

    // ==============================================
    // 【1】手动创建房间（远程面板用）
    // ==============================================
    public void StartRelayHost()
    {
        if (CheckPrerequisite(out string error))
        {
            OnRelayFailed?.Invoke(error);
            return;
        }

        customManager.transport = relayTransport;
        Transport.active = relayTransport;
        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
        {
            Name = "游戏房间",
            MaxPlayers = maxPlayers,
            OwnerId = playerUuid,
            Visibility = LobbyRoomVisibility.Public
        }, OnCreateRoomComplete));
    }

    // ==============================================
    // 【2】匹配模式 —— 创建房间（规律房间名）
    // ==============================================
    public void StartMatchHost(int matchIndex)
    {
        if (CheckPrerequisite(out string error))
        {
            OnRelayFailed?.Invoke(error);
            return;
        }

        string roomName = $"{MATCH_ROOM_NAME_PREFIX}{matchIndex:000}";

        customManager.transport = relayTransport;
        Transport.active = relayTransport;
        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
        {
            Name = roomName,
            MaxPlayers = maxPlayers,
            OwnerId = playerUuid,
            Visibility = LobbyRoomVisibility.Public
        }, OnCreateRoomComplete));
    }

    // ==============================================
    // 【3】匹配模式 —— 查询房间（通过列表+本地过滤）
    // ==============================================
    public void QueryMatchRoom(int matchIndex, Action<bool, LobbyRoom> callback)
    {
        string targetName = $"{MATCH_ROOM_NAME_PREFIX}{matchIndex:000}";
        StartCoroutine(FindRoomByRoomName(targetName, callback));
    }

    // ==============================================
    // 【4】匹配模式 —— 加入房间（通过 LobbyRoom）
    // ==============================================
    public void JoinMatchRoom(LobbyRoom room)
    {
        if (room == null || string.IsNullOrEmpty(room.RoomUuid))
        {
            OnRelayFailed?.Invoke("房间无效");
            return;
        }
        QueryRoomAndConnect(room.RoomUuid);
    }

    // ==============================================
    // 内部：通过房间名查找（列表+过滤）
    // ==============================================
    private IEnumerator FindRoomByRoomName(string targetName, Action<bool, LobbyRoom> callback)
    {
        bool finished = false;
        bool found = false;
        LobbyRoom targetRoom = null;

        void OnListed(List<LobbyRoom> rooms)
        {
            Unsubscribe();
            targetRoom = rooms.FirstOrDefault(r =>
                r.Name == targetName &&
                (r.Status == LobbyRoomStatus.Ready || r.Status == LobbyRoomStatus.Running));
            found = targetRoom != null;
            finished = true;
        }

        void OnFailed(string msg)
        {
            Unsubscribe();
            finished = true;
        }

        void Unsubscribe()
        {
            OnRoomListSuccess -= OnListed;
            OnRelayFailed -= OnFailed;
        }

        OnRoomListSuccess += OnListed;
        OnRelayFailed += OnFailed;

        ListRelayRooms();
        yield return new WaitForSeconds(1.5f);
        callback?.Invoke(found, targetRoom);
    }

    // ==============================================
    // 房间创建完成回调
    // ==============================================
    private void OnCreateRoomComplete(CreateRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK && resp.Status == LobbyRoomStatus.ServerAllocated)
        {
            currentRoomCode = resp.RoomCode;
            relayTransport.SetRoomData(resp);
            customManager.StartHost();
            OnRelaySuccess?.Invoke(currentRoomCode);
        }
        else
        {
            string err = $"创建房间失败：{resp.Code}";
            Debug.LogError(err);
            OnRelayFailed?.Invoke(err);
        }
    }

    // ==============================================
    // 获取房间列表
    // ==============================================
    public void ListRelayRooms()
    {
        if (CheckPrerequisite(out string error))
        {
            OnRelayFailed?.Invoke(error);
            return;
        }

        customManager.transport = relayTransport;
        Transport.active = relayTransport;

        StartCoroutine(LobbyService.AsyncListRoom(new ListRoomRequest()
        {
            Start = 0,
            Count = 100,
            Statuses = new List<LobbyRoomStatus> { LobbyRoomStatus.Ready, LobbyRoomStatus.Running }
        }, resp =>
        {
            if (resp.Code == (uint)RelayCode.OK)
                OnRoomListSuccess?.Invoke(resp.Items);
            else
                OnRelayFailed?.Invoke($"列表获取失败：{resp.Code}");
        }));
    }

    // ==============================================
    // 通过 UUID 查询并连接（安全、无报错）
    // ==============================================
    private void QueryRoomAndConnect(string roomUuid)
    {
        StartCoroutine(LobbyService.AsyncQueryRoom(roomUuid, resp =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                if (resp.Status != LobbyRoomStatus.ServerAllocated && resp.Status != LobbyRoomStatus.Ready)
                {
                    OnRelayFailed?.Invoke($"房间状态不可用：{resp.Status}");
                    return;
                }

                relayTransport.SetRoomData(resp);
                customManager.StartClient();
                OnRelaySuccess?.Invoke(resp.RoomCode);
            }
            else
            {
                OnRelayFailed?.Invoke($"查询房间失败：{resp.Code}");
            }
        }));
    }

    // ==============================================
    // 手动输入房间码加入（保留原有功能）
    // ==============================================
    public void StartRelayClient(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            OnRelayFailed?.Invoke("房间码不能为空");
            return;
        }

        customManager.transport = relayTransport;
        Transport.active = relayTransport;
        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, resp =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                relayTransport.SetRoomData(resp);
                customManager.StartClient();
                OnRelaySuccess?.Invoke(resp.RoomCode);
            }
            else
            {
                OnRelayFailed?.Invoke("房间不存在或已关闭");
            }
        }));
    }

    // ==============================================
    // 仅查询房间（保留）
    // ==============================================
    public void QueryRoomOnly(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            OnQueryFailed?.Invoke("房间码不能为空");
            return;
        }

        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, resp =>
        {
            if (resp.Code == (uint)RelayCode.OK)
                OnQuerySuccess?.Invoke(resp.RoomCode);
            else
                OnQueryFailed?.Invoke("未找到房间");
        }));
    }

    // ==============================================
    // 工具函数
    // ==============================================
    private bool CheckPrerequisite(out string error)
    {
        error = default;
        if (customManager == null) { error = "网络管理器未找到"; return true; }
        if (relayTransport == null) { error = "RelayTransport 未找到"; return true; }
        if (string.IsNullOrEmpty(playerUuid)) { error = "玩家ID未初始化"; return true; }

        if (customManager.IsRelayModeActive() && customManager.transport != relayTransport)
        {
            customManager.transport = relayTransport;
            Transport.active = relayTransport;
        }
        return false;
    }

    public void StopRelay()
    {
        try
        {
            if (NetworkServer.active || NetworkClient.isConnected)
                customManager?.StopHost();
        }
        catch { }
        currentRoomCode = "";
        StopAllCoroutines();
    }

    public void TriggerRelaySuccess(string roomCode) => OnRelaySuccess?.Invoke(roomCode);
    public void TriggerRelayFailed(string msg) => OnRelayFailed?.Invoke(msg);

    private void OnDestroy() => Instance = null;
}