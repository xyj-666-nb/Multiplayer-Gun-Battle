using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Sync.Relay.Transport.Mirror;
using Unity.Sync.Relay;
using Unity.Sync.Relay.Lobby;
using Unity.Sync.Relay.Model;

public class UOSRelaySimple : MonoBehaviour
{
    public bool forceAndroidMode = true;
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

        // 优先从场景找CustomNetworkManager，再GetComponent
        if (customManager == null)
        {
            customManager = FindObjectOfType<CustomNetworkManager>();
            if (customManager == null)
            {
                customManager = GetComponent<CustomNetworkManager>();
            }
        }

        // 优先从场景找RelayTransportMirror，再GetComponent
        if (relayTransport == null)
        {
            relayTransport = FindObjectOfType<RelayTransportMirror>();
            if (relayTransport == null)
            {
                relayTransport = GetComponent<RelayTransportMirror>();
            }
        }
    }

    private void Start()
    {
        string platformName = Application.platform.ToString();
        bool isAndroid = platformName.Contains("Android") || SystemInfo.operatingSystem.Contains("Android");

        if (isAndroid || forceAndroidMode)
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
        }

        InitializePlayerData();
    }

    /// <summary>
    /// 初始化玩家数据
    /// </summary>
    private void InitializePlayerData()
    {
        playerUuid = Guid.NewGuid().ToString();
        playerName = "DefaultPlayer";

        if (relayTransport != null)
        {
            relayTransport.SetPlayerData(playerUuid, playerName);
        }
        else
        {
            Debug.LogError("【UOS】严重错误：relayTransport为null，无法设置玩家数据");
            OnRelayFailed?.Invoke("RelayTransport组件未找到");
        }
    }

    //重新获取姓名
    public void GetPlayerName(string PlayerName)
    {
        playerName = PlayerName;
    }

    /// <summary>
    ///创建房间
    /// </summary>
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

    /// <summary>
    /// 房间创建完成回调
    /// </summary>
    private void OnCreateRoomComplete(CreateRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK)
        {
            if (resp.Status == LobbyRoomStatus.ServerAllocated)
            {
                currentRoomCode = resp.RoomCode;

                relayTransport.SetRoomData(resp);

                customManager.StartHost();

                OnRelaySuccess?.Invoke(currentRoomCode);
            }
            else
            {
                string error = $"房间状态异常：{resp.Status}（仅ServerAllocated状态可启动）";
                Debug.LogError($"【UOS房主】错误：{error}");
                OnRelayFailed?.Invoke(error);
            }
        }
        else
        {
            string error = $"创建房间失败，错误码：{resp.Code}";
            Debug.LogError($"【UOS房主】错误：{error}");
            OnRelayFailed?.Invoke(error);
        }
    }

    /// <summary>
    /// 列出房间列表
    /// </summary>
    public void ListRelayRooms()
    {
        if (CheckPrerequisite(out string error))
        {
            OnRelayFailed?.Invoke(error);
            return;
        }

        customManager.transport = relayTransport;
        Transport.active = relayTransport;

        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncListRoom(new ListRoomRequest()
        {
            Start = 0,
            Count = 10,
            Statuses = new List<LobbyRoomStatus>() { LobbyRoomStatus.Ready, LobbyRoomStatus.Running }
        }, (resp) =>
        {
            if (resp.Code == (uint)RelayCode.OK)
            {
                if (resp.Items.Count > 0)
                {
                    OnRoomListSuccess?.Invoke(resp.Items);

                    foreach (LobbyRoom item in resp.Items)
                    {
                        if (item.Status == LobbyRoomStatus.Ready)
                        {
                            QueryRoomAndConnect(item.RoomUuid);
                            break;
                        }
                    }
                }
                else
                {
                    OnRelayFailed?.Invoke("未找到可用房间");
                }
            }
            else
            {
                string error = $"获取房间列表失败，错误码：{resp.Code}";
                Debug.LogError($"【UOS客户端】错误：{error}");
                OnRelayFailed?.Invoke(error);
            }
        }));
    }

    /// <summary>
    ///通过RoomUuid查询房间并连接
    /// </summary>
    private void QueryRoomAndConnect(string roomUuid)
    {
        StartCoroutine(LobbyService.AsyncQueryRoom(roomUuid, (_resp) =>
        {
            if (_resp.Code == (uint)RelayCode.OK)
            {
                // 核心修复：校验房间状态
                if (_resp.Status != LobbyRoomStatus.ServerAllocated && _resp.Status != LobbyRoomStatus.Ready)
                {
                    OnRelayFailed?.Invoke($"房间状态不可连接：{_resp.Status}");
                    return;
                }

                relayTransport.SetRoomData(_resp);

                // 对齐官方：直接启动Client
                customManager.StartClient();

                OnRelaySuccess?.Invoke(_resp.RoomCode);
            }
            else
            {
                string error = $"查询房间详情失败，错误码：{_resp.Code}";
                Debug.LogError($"【UOS客户端】错误：{error}");
                OnRelayFailed?.Invoke(error);
            }
        }));
    }

    /// <summary>
    /// 仅查询房间是否存在
    /// </summary>
    public void QueryRoomOnly(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            string error = "请输入有效的房间码！";
            OnQueryFailed?.Invoke(error);
            return;
        }
        if (relayTransport == null)
        {
            string error = "未找到RelayTransportMirror组件！";
            Debug.LogError($"【UOS】严重错误：{error}");
            OnQueryFailed?.Invoke(error);
            return;
        }

        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, OnQueryRoomComplete));
    }

    /// <summary>
    ///通过RoomCode加入
    /// </summary>
    public void StartRelayClient(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            string error = "请输入有效的房间码！";
            Debug.LogError($"【UOS客户端】错误：{error}");
            OnRelayFailed?.Invoke(error);
            return;
        }

        customManager.transport = relayTransport;
        Transport.active = relayTransport;

        OnRelayConnecting?.Invoke();

        StartCoroutine(LobbyService.AsyncQueryRoomByRoomCode(roomCode, (resp) => {
            if (resp.Code == (uint)RelayCode.OK)
            {
                if (resp.Status != LobbyRoomStatus.ServerAllocated && resp.Status != LobbyRoomStatus.Ready)
                {
                    string statusError = $"房间状态不可连接：{resp.Status}（仅ServerAllocated/Ready可连接）";
                    Debug.LogError($"【UOS客户端】错误：{statusError}");
                    OnRelayFailed?.Invoke(statusError);
                    return;
                }

                currentRoomCode = resp.RoomCode;
                relayTransport.SetRoomData(resp);
                customManager.StartClient();

                OnRelaySuccess?.Invoke(currentRoomCode);
            }
            else
            {
                string errorMsg = $"连接失败，错误码：{resp.Code}";
                Debug.LogError($"【UOS客户端】错误：{errorMsg}");
                OnRelayFailed?.Invoke(errorMsg);
            }
        }));
    }

    /// <summary>
    /// 房间查询完成回调
    /// </summary>
    private void OnQueryRoomComplete(QueryRoomResponse resp)
    {
        if (resp.Code == (uint)RelayCode.OK)
        {
            OnQuerySuccess?.Invoke(resp.RoomCode);
        }
        else
        {
            string finalUIMsg;

            if (resp.Code == 10032)
            {
                finalUIMsg = "未找到房间";
            }
            else
            {
                finalUIMsg = $"查询失败，请检查网络";
                Debug.LogError($"【UOS】查询房间异常失败，错误码：{resp.Code}");
            }

            OnQueryFailed?.Invoke(finalUIMsg);
        }
    }

    /// <summary>
    /// 前置条件检查
    /// </summary>
    private bool CheckPrerequisite(out string error)
    {
        error = string.Empty;
        if (customManager == null)
        {
            error = "CustomNetworkManager未找到";
            Debug.LogError($"【UOS】严重错误：{error}");
            return true;
        }
        if (relayTransport == null)
        {
            error = "RelayTransportMirror组件未找到";
            Debug.LogError($"【UOS】严重错误：{error}");
            return true;
        }
        if (string.IsNullOrEmpty(playerUuid))
        {
            error = "玩家UUID未初始化";
            Debug.LogError($"【UOS】严重错误：{error}");
            return true;
        }
        // 确保Transport赋值正确（仅Relay模式下生效）
        if (customManager.IsRelayModeActive() && customManager.transport != relayTransport)
        {
            customManager.transport = relayTransport;
            Transport.active = relayTransport;
        }
        return false;
    }

    // 供 CustomNetworkManager 外部调用
    public void TriggerRelaySuccess(string roomCode)
    {
        OnRelaySuccess?.Invoke(roomCode);
    }

    public void TriggerRelayFailed(string errorMsg)
    {
        OnRelayFailed?.Invoke(errorMsg);
    }

    /// <summary>
    /// 停止Relay连接并清理资源
    /// </summary>
    public void StopRelay()
    {
        try
        {
            if (NetworkServer.active || NetworkClient.isConnected)
            {
                if (customManager != null)
                {
                    customManager.StopHost();
                }
                else
                {
                    NetworkManager.singleton.StopHost();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"【UOS】停止Relay时发生异常：{e.Message}");
        }

        currentRoomCode = "";
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}