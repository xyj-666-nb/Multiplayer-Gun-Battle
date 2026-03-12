using UnityEngine;
using Mirror;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System;
using Utp;

public class RelayForCustomManager : MonoBehaviour
{
    [Header("引用")]
    public CustomNetworkManager customManager;
    public UtpTransport utpTransport;

    [Header("设置")]
    public int maxRelayPlayers = 4;

    [Header("调试信息")]
    public string currentJoinCode;

    // ========== 事件系统 ==========
    public static event Action OnRelayConnecting;
    public static event Action<string> OnRelaySuccess; // 参数: JoinCode
    public static event Action<string> OnRelayFailed;  // 参数: 错误信息

    private bool isTryingToConnect;

    void Awake()
    {
        if (customManager == null) customManager = GetComponent<CustomNetworkManager>();
        if (utpTransport == null) utpTransport = GetComponent<UtpTransport>();
    }

    async void Start()
    {
        await LoginToUnity();
    }

    // ==========================================
    // 【公共接口】
    // ==========================================

    public void StartRelayHost()
    {
        if (utpTransport == null)
        {
            Debug.LogError("请在 Inspector 赋值 UtpTransport！");
            return;
        }

        isTryingToConnect = true;
        OnRelayConnecting?.Invoke();
        Debug.Log("正在申请 Relay 服务器...");

        utpTransport.useRelay = true;

        utpTransport.AllocateRelayServer(
            maxPlayers: maxRelayPlayers,
            regionId: null,
            onSuccess: (joinCode) =>
            {
                if (!isTryingToConnect) return;

                currentJoinCode = joinCode;
                Debug.Log("--------------------------------");
                Debug.Log($"房间创建成功！Join Code: {joinCode}");
                Debug.Log("--------------------------------");
                Main.Instance.JoinRoomInfo= joinCode;//赋值加入码
                OnRelaySuccess?.Invoke(joinCode);
                customManager.StartHost();
            },
            onFailure: () => // 【关键】这里是 () => 不带参数
            {
                if (!isTryingToConnect) return;

                string errorMsg = "创建房间失败，请检查网络连接";
                Debug.LogError(errorMsg);
                utpTransport.useRelay = false;
                isTryingToConnect = false;

                // 手动传错误信息
                OnRelayFailed?.Invoke(errorMsg);
            }
        );
    }

    public void StartRelayClient(string joinCode)
    {
        if (utpTransport == null)
        {
            Debug.LogError("请在 Inspector 赋值 UtpTransport！");
            return;
        }

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("请输入 Join Code！");
            return;
        }

        isTryingToConnect = true;
        OnRelayConnecting?.Invoke();
        Debug.Log($"正在加入房间: {joinCode}");

        utpTransport.useRelay = true;

        utpTransport.ConfigureClientWithJoinCode(
            joinCode: joinCode,
            onSuccess: () =>
            {
                if (!isTryingToConnect)
                    return;

                Debug.Log("配置成功，正在连接...");
                customManager.StartClient();
            },
            onFailure: () => 
            {
                if (!isTryingToConnect)
                    return;

                string errorMsg = "加入房间失败，请检查 Join Code 是否正确";
                Debug.LogError(errorMsg);
                utpTransport.useRelay = false;
                isTryingToConnect = false;

                // 手动传错误信息
                OnRelayFailed?.Invoke(errorMsg);
            }
        );
    }

    public void CancelRelayConnection()
    {
        isTryingToConnect = false;
        Debug.Log("已取消 Relay 连接");
    }

    // ==========================================
    // 内部
    // ==========================================

    private async Task LoginToUnity()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log("Unity 云服务就绪");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"云服务登录失败: {e.Message}");
        }
    }

    /// <summary>
    /// 停止 Relay 并清理状态
    /// </summary>
    public void StopRelay()
    {
        Debug.Log("[Relay] 正在停止 Relay 连接...");

        // 1. 停止 Mirror
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }

        // 2. 关闭 Relay 开关
        if (utpTransport != null)
        {
            utpTransport.useRelay = false;
        }

        // 3. 重置状态
        isTryingToConnect = false;
        currentJoinCode = "";

        Debug.Log("[Relay] Relay 连接已清理完毕");
    }
}