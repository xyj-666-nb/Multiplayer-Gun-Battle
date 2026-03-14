//using UnityEngine;
//using Mirror;
//using Unity.Services.Core;
//using Unity.Services.Authentication;
//using System.Threading.Tasks;
//using System;
//using Utp;

//public class RelayForCustomManager : MonoBehaviour
//{
//    [Header("引用")]
//    public CustomNetworkManager customManager;
//    public UtpTransport utpTransport;

//    [Header("设置")]
//    public int maxRelayPlayers = 4;
//    public string currentJoinCode;

//    // 事件系统
//    public static event Action OnRelayConnecting;
//    public static event Action<string> OnRelaySuccess;
//    public static event Action<string> OnRelayFailed;

//    private bool isTryingToConnect;

//    void Awake()
//    {
//        if (customManager == null) customManager = GetComponent<CustomNetworkManager>();
//        if (utpTransport == null) utpTransport = GetComponent<UtpTransport>();
//    }

//    async void Start()
//    {
//        await LoginToUnityChina();
//    }

//    /// <summary>
//    /// 【中国区 UOS 简化版】初始化 Unity 中国云服务
//    /// </summary>
//    private async Task LoginToUnityChina()
//    {
//        try
//        {
//            // 【修改点】去掉了 SetEnvironment，UOS Launcher 已自动配置环境
//            await UnityServices.InitializeAsync();

//            // 匿名登录
//            if (!AuthenticationService.Instance.IsSignedIn)
//            {
//                await AuthenticationService.Instance.SignInAnonymouslyAsync();
//            }
//            Debug.Log("【中国区】Unity 云服务初始化成功，玩家 ID：" + AuthenticationService.Instance.PlayerId);
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("【中国区】云服务初始化失败：" + e.Message);
//        }
//    }

//    /// <summary>
//    /// 【国内上海节点】启动 Relay 房主
//    /// </summary>
//    public async void StartRelayHost()
//    {
//        // 1. 切换到 Relay 模式
//        if (customManager != null)
//        {
//            customManager.SwitchToRelayMode();
//        }

//        // 2. 校验 UTP 组件
//        utpTransport = customManager.GetComponent<UtpTransport>();
//        if (utpTransport == null)
//        {
//            string error = "未找到 UtpTransport 组件！";
//            Debug.LogError(error);
//            OnRelayFailed?.Invoke(error);
//            return;
//        }

//        // 3. 校验服务状态
//        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
//        {
//            try { await LoginToUnityChina(); }
//            catch (Exception e)
//            {
//                string error = "云服务初始化失败：" + e.Message;
//                Debug.LogError(error);
//                OnRelayFailed?.Invoke(error);
//                return;
//            }
//        }

//        if (!AuthenticationService.Instance.IsSignedIn)
//        {
//            try { await AuthenticationService.Instance.SignInAnonymouslyAsync(); }
//            catch (Exception e)
//            {
//                string error = "登录失败：" + e.Message;
//                Debug.LogError(error);
//                OnRelayFailed?.Invoke(error);
//                return;
//            }
//        }

//        isTryingToConnect = true;
//        OnRelayConnecting?.Invoke();
//        Debug.Log("正在申请国内上海 Relay 低延迟服务器...");

//        // 4. 开启 Relay 模式
//        utpTransport.useRelay = true;
//        utpTransport.enabled = true;

//        // 5. 【核心】固定国内上海节点 cn-east，国内延迟最低
//        utpTransport.AllocateRelayServer(
//            maxPlayers: maxRelayPlayers,
//            regionId: "cn-east", // 中国上海节点，北方用户可以换成 cn-north（北京）
//            onSuccess: (joinCode) =>
//            {
//                if (!isTryingToConnect) return;

//                if (string.IsNullOrEmpty(joinCode))
//                {
//                    OnRelayFailed?.Invoke("房间创建失败：云服务器返回空房间码");
//                    isTryingToConnect = false;
//                    return;
//                }

//                currentJoinCode = joinCode;
//                Debug.Log("========================================");
//                Debug.Log("【国内节点】房间创建成功！");
//                Debug.Log($"房间码: {joinCode}");
//                Debug.Log("========================================");

//                OnRelaySuccess?.Invoke(joinCode);
//                if (Main.Instance != null)
//                {
//                    Main.Instance.JoinRoomInfo = joinCode;
//                }

//                // 启动 Mirror Host
//                if (customManager != null) customManager.StartHost();
//                else NetworkManager.singleton.StartHost();
//            },
//            onFailure: () =>
//            {
//                if (!isTryingToConnect) return;
//                string errorMsg = "房间创建失败！请检查网络和云服务配置";
//                Debug.LogError(errorMsg);
//                isTryingToConnect = false;
//                OnRelayFailed?.Invoke(errorMsg);
//            }
//        );
//    }

//    /// <summary>
//    /// 【国内节点版】启动 Relay 客户端
//    /// </summary>
//    public async void StartRelayClient(string joinCode)
//    {
//        // 1. 切换到 Relay 模式
//        if (customManager != null)
//        {
//            customManager.SwitchToRelayMode();
//        }

//        // 2. 校验 UTP 组件
//        utpTransport = customManager.GetComponent<UtpTransport>();
//        if (utpTransport == null)
//        {
//            string error = "未找到 UtpTransport 组件！";
//            Debug.LogError(error);
//            OnRelayFailed?.Invoke(error);
//            return;
//        }

//        if (string.IsNullOrEmpty(joinCode))
//        {
//            string error = "请输入有效的房间码！";
//            Debug.LogError(error);
//            OnRelayFailed?.Invoke(error);
//            return;
//        }

//        // 3. 格式化房间码
//        joinCode = joinCode.Trim().ToUpper().Replace(" ", "").Replace("\n", "").Replace("\r", "");
//        Debug.Log($"正在加入房间，处理后的房间码: [{joinCode}]");

//        // 4. 校验服务状态
//        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
//        {
//            try { await LoginToUnityChina(); }
//            catch (Exception e)
//            {
//                string error = "云服务初始化失败：" + e.Message;
//                Debug.LogError(error);
//                OnRelayFailed?.Invoke(error);
//                return;
//            }
//        }

//        if (!AuthenticationService.Instance.IsSignedIn)
//        {
//            try { await AuthenticationService.Instance.SignInAnonymouslyAsync(); }
//            catch (Exception e)
//            {
//                string error = "登录失败：" + e.Message;
//                Debug.LogError(error);
//                OnRelayFailed?.Invoke(error);
//                return;
//            }
//        }

//        isTryingToConnect = true;
//        OnRelayConnecting?.Invoke();

//        // 5. 开启 Relay 模式
//        utpTransport.useRelay = true;
//        utpTransport.enabled = true;

//        // 6. 配置客户端并加入
//        utpTransport.ConfigureClientWithJoinCode(
//            joinCode: joinCode,
//            onSuccess: () =>
//            {
//                if (!isTryingToConnect) return;
//                Debug.Log("房间码验证通过，正在连接房主...");

//                if (customManager != null) customManager.StartClient();
//                else NetworkManager.singleton.StartClient();

//                OnRelaySuccess?.Invoke(joinCode);
//            },
//            onFailure: () =>
//            {
//                if (!isTryingToConnect) return;
//                string errorMsg = "加入失败！请检查房间码、房主在线状态和网络";
//                Debug.LogError(errorMsg);
//                isTryingToConnect = false;
//                OnRelayFailed?.Invoke(errorMsg);
//            }
//        );
//    }

//    /// <summary>
//    /// 取消连接
//    /// </summary>
//    public void CancelRelayConnection()
//    {
//        isTryingToConnect = false;
//        Debug.Log("已取消 Relay 连接");
//    }

//    /// <summary>
//    /// 停止 Relay 并清理资源
//    /// </summary>
//    public void StopRelay()
//    {
//        Debug.Log("[Relay] 正在停止 Relay 连接...");

//        if (NetworkServer.active || NetworkClient.isConnected)
//        {
//            if (customManager != null) customManager.StopHost();
//            else NetworkManager.singleton.StopHost();
//        }

//        if (utpTransport != null)
//        {
//            utpTransport.ServerStop();
//            utpTransport.useRelay = false;
//        }

//        isTryingToConnect = false;
//        currentJoinCode = "";
//        Debug.Log("[Relay] Relay 连接已清理完毕");
//    }
//}