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
    public int maxPlayers = CustomNetworkManager.DefaultRoomPlayerLimit;
    public string currentRoomCode;
    public string currentRoomUuid;

    private const string MATCH_ROOM_NAME_PREFIX = "PUBLIC_MATCH_";
    private const string ROOM_FULL_MESSAGE = "房间人数已满";
    private const string ROOM_STARTED_MESSAGE = "游戏已开始，无法加入";
    private const float MATCH_QUERY_TIMEOUT = 3f;

    private LobbyRoomStatus _lastPublishedStatus = LobbyRoomStatus.Unknown;

    private string playerUuid;
    public string playerName;

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
            /* Debug.LogError("【UOS】relayTransport 为 null"); */
            OnRelayFailed?.Invoke("Relay 组件未找到");
        }
    }

    public void GetPlayerName(string PlayerName)
    {
        playerName = PlayerName;
    }

    public void StartRelayHost()
    {
        if (CheckPrerequisite(out string error))
        {
            OnRelayFailed?.Invoke(error);
            return;
        }

        customManager.transport = relayTransport;
        Transport.active = relayTransport;
        int roomMaxPlayers = GetEffectiveMaxPlayers();
        customManager.ApplyRoomPlayerLimit(roomMaxPlayers);
        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
        {
            Name = "游戏房间",
            MaxPlayers = roomMaxPlayers,
            OwnerId = playerUuid,
            Visibility = LobbyRoomVisibility.Public
        }, OnCreateRoomComplete));
    }

    public void StartMatchHost()
    {
        if (CheckPrerequisite(out string error))
        {
            OnRelayFailed?.Invoke(error);
            return;
        }

        string roomName = GenerateMatchRoomName();

        customManager.transport = relayTransport;
        Transport.active = relayTransport;
        int roomMaxPlayers = GetEffectiveMaxPlayers();
        customManager.ApplyRoomPlayerLimit(roomMaxPlayers);
        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncCreateRoom(new CreateRoomRequest()
        {
            Name = roomName,
            MaxPlayers = roomMaxPlayers,
            OwnerId = playerUuid,
            Visibility = LobbyRoomVisibility.Public
        }, OnCreateRoomComplete));
    }

    public void StartMatchHost(int matchIndex)
    {
        StartMatchHost();
    }

    public void QueryBestMatchRoom(Action<bool, LobbyRoom, string> callback)
    {
        if (CheckPrerequisite(out string error))
        {
            callback?.Invoke(false, null, error);
            return;
        }

        StartCoroutine(QueryBestMatchRoomCoroutine(callback));
    }

    public void QueryMatchRoom(int matchIndex, Action<bool, LobbyRoom> callback)
    {
        QueryBestMatchRoom((success, room, message) =>
        {
            callback?.Invoke(success, room);
        });
    }

    public void JoinMatchRoom(LobbyRoom room)
    {
        if (room == null || string.IsNullOrEmpty(room.RoomUuid))
        {
            OnRelayFailed?.Invoke("房间无效");
            return;
        }
        QueryRoomAndConnect(room.RoomUuid);
    }

    private IEnumerator QueryBestMatchRoomCoroutine(Action<bool, LobbyRoom, string> callback)
    {
        bool finished = false;
        string failureMessage = null;
        List<LobbyRoom> listedRooms = null;

        void OnListed(List<LobbyRoom> rooms)
        {
            Unsubscribe();
            listedRooms = rooms;
            finished = true;
        }

        void OnFailed(string msg)
        {
            Unsubscribe();
            failureMessage = msg;
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

        float timer = 0f;
        while (!finished && timer < MATCH_QUERY_TIMEOUT)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Unsubscribe();

        if (!finished)
        {
            callback?.Invoke(false, null, "匹配请求超时");
            yield break;
        }

        if (!string.IsNullOrEmpty(failureMessage))
        {
            callback?.Invoke(false, null, failureMessage);
            yield break;
        }

        LobbyRoom bestRoom = SelectBestMatchRoom(listedRooms);
        if (bestRoom != null)
        {
            callback?.Invoke(true, bestRoom, null);
        }
        else
        {
            callback?.Invoke(false, null, GetMatchRoomFailureMessage(listedRooms));
        }
    }

    private string GetMatchRoomFailureMessage(List<LobbyRoom> rooms)
    {
        if (rooms == null || rooms.Count == 0)
        {
            return "未找到可加入的公共房间";
        }

        bool hasMatchRoom = false;
        bool hasFullRoom = false;
        bool hasStartedRoom = false;

        foreach (var room in rooms)
        {
            if (room == null || string.IsNullOrEmpty(room.Name) || !room.Name.StartsWith(MATCH_ROOM_NAME_PREFIX, StringComparison.Ordinal))
            {
                continue;
            }

            hasMatchRoom = true;
            if (room.MaxPlayers > 0 && room.PlayerCount >= (uint)room.MaxPlayers)
            {
                hasFullRoom = true;
            }

            if (room.Status != LobbyRoomStatus.Ready && room.Status != LobbyRoomStatus.ServerAllocated)
            {
                hasStartedRoom = true;
            }
        }

        if (hasFullRoom)
        {
            return ROOM_FULL_MESSAGE;
        }

        if (hasStartedRoom)
        {
            return ROOM_STARTED_MESSAGE;
        }

        return hasMatchRoom ? "当前没有可加入房间" : "未找到可加入的公共房间";
    }

    private LobbyRoom SelectBestMatchRoom(List<LobbyRoom> rooms)
    {
        if (rooms == null || rooms.Count == 0)
        {
            return null;
        }

        return rooms
            .Where(IsAvailableMatchRoom)
            .OrderByDescending(room => room.PlayerCount)
            .ThenBy(room => room.MaxPlayers)
            .FirstOrDefault();
    }

    private bool IsAvailableMatchRoom(LobbyRoom room)
    {
        if (room == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(room.RoomUuid) || string.IsNullOrEmpty(room.Name))
        {
            return false;
        }

        if (!room.Name.StartsWith(MATCH_ROOM_NAME_PREFIX, StringComparison.Ordinal))
        {
            return false;
        }

        if (room.Status != LobbyRoomStatus.Ready)
        {
            return false;
        }

        if (room.MaxPlayers > 0 && room.PlayerCount >= (uint)room.MaxPlayers)
        {
            return false;
        }

        return true;
    }

    private string GenerateMatchRoomName()
    {
        return MATCH_ROOM_NAME_PREFIX + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
    }

    private void OnCreateRoomComplete(CreateRoomResponse resp)
    {
        if (resp != null && resp.Code == (uint)RelayCode.OK && resp.Status == LobbyRoomStatus.ServerAllocated)
        {
            currentRoomCode = resp.RoomCode;
            currentRoomUuid = resp.RoomUuid;
            _lastPublishedStatus = LobbyRoomStatus.Unknown;

            int roomMaxPlayers = resp.MaxPlayers > 0 ? resp.MaxPlayers : GetEffectiveMaxPlayers();
            customManager.ApplyRoomPlayerLimit(roomMaxPlayers);
            relayTransport.SetRoomData(resp);
            customManager.StartHost();
            RefreshRoomJoinState();
            OnRelaySuccess?.Invoke(currentRoomCode);
        }
        else
        {
            string err = $"创建房间失败：{(resp != null ? resp.Code : 0)}";
            /* Debug.LogError(err); */
            OnRelayFailed?.Invoke(err);
        }
    }

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

    private void QueryRoomAndConnect(string roomUuid)
    {
        StartCoroutine(LobbyService.AsyncQueryRoom(roomUuid, resp =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                if (!IsRelayRoomJoinable(resp, out string failureReason))
                {
                    OnRelayFailed?.Invoke(failureReason);
                    return;
                }

                customManager.ApplyRoomPlayerLimit(resp.MaxPlayers);
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
                if (!IsRelayRoomJoinable(resp, out string failureReason))
                {
                    OnRelayFailed?.Invoke(failureReason);
                    return;
                }

                customManager.ApplyRoomPlayerLimit(resp.MaxPlayers);
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
            {
                if (!IsRelayRoomJoinable(resp, out string failureReason))
                {
                    OnQueryFailed?.Invoke(failureReason);
                    return;
                }

                OnQuerySuccess?.Invoke(resp.RoomCode);
            }
            else
                OnQueryFailed?.Invoke("未找到房间");
        }));
    }

    private int GetEffectiveMaxPlayers()
    {
        int hardLimit = customManager != null ? customManager.RoomPlayerLimit : CustomNetworkManager.DefaultRoomPlayerLimit;
        int requested = maxPlayers > 0 ? maxPlayers : hardLimit;
        return Mathf.Clamp(requested, 1, hardLimit);
    }

    private bool IsRelayRoomJoinable(QueryRoomResponse room, out string reason)
    {
        reason = null;
        if (room == null)
        {
            reason = "房间不存在或已关闭";
            return false;
        }

        if (room.MaxPlayers > 0 && room.PlayerCount >= (uint)room.MaxPlayers)
        {
            reason = ROOM_FULL_MESSAGE;
            return false;
        }

        if (room.Status != LobbyRoomStatus.Ready && room.Status != LobbyRoomStatus.ServerAllocated)
        {
            reason = ROOM_STARTED_MESSAGE;
            return false;
        }

        return true;
    }

    public void RefreshRoomJoinState()
    {
        if (!isActiveAndEnabled || customManager == null || !NetworkServer.active || string.IsNullOrEmpty(currentRoomUuid))
        {
            return;
        }

        bool isOpen = !customManager.IsRoomClosedToNewPlayers() && customManager.GetCurrentPlayerCount() < GetEffectiveMaxPlayers();
        PublishRoomStatus(isOpen ? LobbyRoomStatus.Ready : LobbyRoomStatus.Running);
    }

    private void PublishRoomStatus(LobbyRoomStatus status)
    {
        if (string.IsNullOrEmpty(currentRoomUuid) || _lastPublishedStatus == status)
        {
            return;
        }

        _lastPublishedStatus = status;
        StartCoroutine(LobbyService.ChangeRoomStatus(currentRoomUuid, status, resp =>
        {
            if (resp == null || resp.Code != (uint)RelayCode.OK)
            {
                _lastPublishedStatus = LobbyRoomStatus.Unknown;
            }
        }));
    }

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
        currentRoomUuid = "";
        _lastPublishedStatus = LobbyRoomStatus.Unknown;
        StopAllCoroutines();
    }

    public void TriggerRelaySuccess(string roomCode) => OnRelaySuccess?.Invoke(roomCode);
    public void TriggerRelayFailed(string msg) => OnRelayFailed?.Invoke(msg);

    private void OnDestroy() => Instance = null;
}