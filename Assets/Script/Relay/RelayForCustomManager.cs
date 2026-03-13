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
    public string currentJoinCode;

    // 事件系统
    public static event Action OnRelayConnecting;
    public static event Action<string> OnRelaySuccess;
    public static event Action<string> OnRelayFailed;

    private bool isTryingToConnect;

    void Awake()
    {
        if (customManager == null) customManager = GetComponent<CustomNetworkManager>();
    }

    async void Start()
    {
        await LoginToUnity();
    }

    /// <summary>
    /// 启动Relay房主
    /// </summary>
    public async void StartRelayHost()
    {
        // 1. 强制切换到Relay模式（彻底移除KCP）
        if (customManager != null)
        {
            customManager.SwitchToRelayMode();
        }

        // 2. 重新获取UTP引用（因为刚才可能重新添加了组件）
        utpTransport = customManager.GetComponent<UtpTransport>();

        if (utpTransport == null)
        {
            string error = "场景里没有找到 UtpTransport 组件！";
            Debug.LogError(error);
            OnRelayFailed?.Invoke(error);
            return;
        }

        // 3. 校验UGS登录
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                string error = string.Format("Unity云初始化失败：{0}", e.Message);
                Debug.LogError(error);
                OnRelayFailed?.Invoke(error);
                return;
            }
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log(string.Format("房主登录成功，玩家ID：{0}", AuthenticationService.Instance.PlayerId));
            }
            catch (Exception e)
            {
                string error = string.Format("房主登录失败：{0}", e.Message);
                Debug.LogError(error);
                OnRelayFailed?.Invoke(error);
                return;
            }
        }

        isTryingToConnect = true;
        OnRelayConnecting?.Invoke();
        Debug.Log("正在申请Relay服务器...");

        utpTransport.useRelay = true;
        utpTransport.enabled = true;

        // 4. 通过 UtpTransport 申请房间
        utpTransport.AllocateRelayServer(
            maxPlayers: maxRelayPlayers,
            regionId: "",
            onSuccess: (joinCode) =>
            {
                if (!isTryingToConnect) return;

                if (string.IsNullOrEmpty(joinCode))
                {
                    OnRelayFailed?.Invoke("房间创建失败：云服务器返回空房间码");
                    isTryingToConnect = false;
                    return;
                }

                currentJoinCode = joinCode;
                Debug.Log("========================================");
                Debug.Log("房间创建成功！");
                Debug.Log(string.Format("房间码: {0}", joinCode));
                Debug.Log("========================================");

                OnRelaySuccess?.Invoke(joinCode);
                if (Main.Instance != null)
                {
                    Main.Instance.JoinRoomInfo = joinCode;
                }

                // 5. 直接启动Mirror的Host（此时KCP已经被移除，Mirror只能用UTP）
                if (customManager != null)
                {
                    customManager.StartHost();
                }
                else
                {
                    NetworkManager.singleton.StartHost();
                }
            },
            onFailure: () =>
            {
                if (!isTryingToConnect) return;

                string errorMsg = "房间创建失败！请检查网络和Unity云服务配置";
                Debug.LogError(errorMsg);
                isTryingToConnect = false;
                OnRelayFailed?.Invoke(errorMsg);
            }
        );
    }

    /// <summary>
    /// 启动Relay客户端
    /// </summary>
    public async void StartRelayClient(string joinCode)
    {
        // 1. 切换到Relay模式
        if (customManager != null)
        {
            customManager.SwitchToRelayMode();
        }

        // 2. 获取UTP引用
        utpTransport = customManager.GetComponent<UtpTransport>();

        if (utpTransport == null)
        {
            string error = "场景里没有找到 UtpTransport 组件！";
            Debug.LogError(error);
            OnRelayFailed?.Invoke(error);
            return;
        }

        if (string.IsNullOrEmpty(joinCode))
        {
            string error = "请输入有效的房间码！";
            Debug.LogError(error);
            OnRelayFailed?.Invoke(error);
            return;
        }

        // 3. 格式化房间码
        joinCode = joinCode.Trim().ToUpper().Replace(" ", "").Replace("\n", "").Replace("\r", "");
        Debug.Log(string.Format("正在加入房间，处理后的房间码: [{0}]", joinCode));

        // 4. 校验UGS登录
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                string error = string.Format("Unity云初始化失败：{0}", e.Message);
                Debug.LogError(error);
                OnRelayFailed?.Invoke(error);
                return;
            }
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log(string.Format("客户端登录成功，玩家ID：{0}", AuthenticationService.Instance.PlayerId));
            }
            catch (Exception e)
            {
                string error = string.Format("客户端登录失败：{0}", e.Message);
                Debug.LogError(error);
                OnRelayFailed?.Invoke(error);
                return;
            }
        }

        isTryingToConnect = true;
        OnRelayConnecting?.Invoke();

        utpTransport.useRelay = true;
        utpTransport.enabled = true;

        // 5. 通过 UtpTransport 配置客户端
        utpTransport.ConfigureClientWithJoinCode(
            joinCode: joinCode,
            onSuccess: () =>
            {
                if (!isTryingToConnect) return;

                Debug.Log("房间码验证通过，正在连接房主...");

                // 启动客户端
                if (customManager != null)
                {
                    customManager.StartClient();
                }
                else
                {
                    NetworkManager.singleton.StartClient();
                }

                OnRelaySuccess?.Invoke(joinCode);
            },
            onFailure: () =>
            {
                if (!isTryingToConnect) return;

                string errorMsg = "加入失败！请检查：\n1. 房间码是否正确\n2. 房主是否在线\n3. 两台设备是否为同一个Unity项目\n4. 网络是否正常";
                Debug.LogError(errorMsg);
                isTryingToConnect = false;
                OnRelayFailed?.Invoke(errorMsg);
            }
        );
    }

    public void CancelRelayConnection()
    {
        isTryingToConnect = false;
        Debug.Log("已取消Relay连接");
    }

    private async Task LoginToUnity()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log("Unity云服务就绪");
        }
        catch (Exception e)
        {
            Debug.LogWarning(string.Format("云服务登录失败: {0}", e.Message));
        }
    }

    public void StopRelay()
    {
        Debug.Log("[Relay] 正在停止Relay连接...");

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

        if (utpTransport != null)
        {
            utpTransport.ServerStop();
            utpTransport.useRelay = false;
        }

        isTryingToConnect = false;
        currentJoinCode = "";

        Debug.Log("[Relay] Relay连接已清理完毕");
    }
}