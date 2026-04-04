using UnityEngine;
using System.Threading.Tasks;
using TapSDK.Login;
using TapSDK.Compliance;
using TapSDK.Core;
using System.Collections.Generic;
using System;

public class TapTapGameLogin : SingleMonoAutoBehavior<TapTapGameLogin>
{
    // 你的TapTap配置
    private const string TAP_CLIENT_ID = "q39of4x3wbsf2rfynv";
    private const string TAP_CLIENT_TOKEN = "9ewCuZRSc1buY8ae4RwAvuh544fGc9T8IwF59s26";

    // 实例成员
    private bool isSdkInited = false;
    public bool hasCheckedCompliance { get; private set; } = false;

    // 仅声明委托，不初始化
    private Action<int, string> ComplianceCallback;

    #region 登录核心逻辑
    public async void OnTapLoginClick()
    {
        if (!InitSDK())
        {
            Debug.LogError("SDK 初始化失败，无法登录");
            return;
        }

        try
        {
            List<string> scopes = new List<string>
            {
                TapTapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE
            };

            var userInfo = await TapTapLogin.Instance.LoginWithScopes(scopes.ToArray());

            if (userInfo != null)
            {
                Debug.Log("===== TapTap登录成功 =====");
                Debug.Log($"用户 openId: {userInfo.openId}");
                Debug.Log($"用户 unionId: {userInfo.unionId}");
                Debug.Log($"用户昵称: {userInfo.name}");

                // 登录成功后启动实名/防沉迷检查
                StartCheckCompliance();
            }
        }
        catch (TaskCanceledException)
        {
            Debug.Log("用户取消登录");
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "您已取消登录!");
        }
        catch (Exception e)
        {
            Debug.LogError($"登录失败：{e.Message}\n{e.StackTrace}");
        }
    }

    public void OnTapLogoutClick()
    {
        try
        {
            TapTapLogin.Instance.Logout();
            hasCheckedCompliance = false;
            isSdkInited = false;
            Debug.Log("===== TapTap登出成功 =====");
        }
        catch (Exception e)
        {
            Debug.LogError($"登出失败：{e.Message}");
        }
    }
    #endregion

    #region SDK初始化（修复核心：在这里初始化委托）
    private bool InitSDK()
    {
        if (isSdkInited) return true;

        try
        {
            TapTapSdkOptions coreOptions = new TapTapSdkOptions
            {
                clientId = TAP_CLIENT_ID,
                clientToken = TAP_CLIENT_TOKEN,
                region = TapTapRegionType.CN,
                screenOrientation = 1, // 横屏
                enableLog = false
            };
            TapTapSDK.Init(coreOptions);

            InitComplianceCallback();

            TapTapCompliance.RegisterComplianceCallback(ComplianceCallback);

            isSdkInited = true;
            Debug.Log("===== TapSDK初始化成功 =====");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SDK初始化失败：{e.Message}");
            isSdkInited = false;
            return false;
        }
    }

    /// <summary>
    /// 延迟初始化合规回调
    /// </summary>
    private void InitComplianceCallback()
    {
        ComplianceCallback = (code, errorMsg) =>
        {
            switch (code)
            {
                // 500：实名通过，正常进入游戏
                case 500:
                    hasCheckedCompliance = true;
                    Debug.Log("===== 实名认证通过 =====");
                    // 保留你的业务逻辑
                    WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "登录成功！祝您游戏愉快!");
                    CountDownManager.Instance.CreateTimer(false, 1000, () =>
                    {
                        UImanager.Instance.HidePanel<TapTapLoginPanel>();
                        Main.Instance.StartCG();
                    });
                    break;

                // 1000/1001/9002：实名失败/关闭窗口，强制登出
                case 1000:
                case 1001:
                case 9002:
                    Debug.Log($"合规失败({code})：{errorMsg}");
                    TapTapLogin.Instance.Logout();
                    hasCheckedCompliance = false;
                    WarnTriggerManager.Instance.TriggerNoInteractionWarn(2f, "请重新登录并完成实名认证！");
                    break;

                // 1100：年龄限制
                case 1100:
                    WarnTriggerManager.Instance.TriggerSingleInteractionWarn(
                        "年龄限制",
                        "您的年龄不符合游戏准入要求，无法进入游戏",
                        () => { }
                    );
                    break;

                // 1200：网络/配置错误，重新触发认证
                case 1200:
                    WarnTriggerManager.Instance.TriggerSingleInteractionWarn(
                        "认证失败",
                        "网络异常或应用信息错误，请检查网络后重试",
                        () => { StartCheckCompliance(); } // 此时可正常调用实例方法
                    );
                    break;

                default:
                    Debug.Log($"其他合规状态({code})：{errorMsg}");
                    break;
            }
        };
    }
    #endregion

    #region 启动实名认证检查
    public async void StartCheckCompliance()
    {
        hasCheckedCompliance = false;
        TapTapAccount account = null;

        try
        {
            // 获取当前登录账号
            account = await TapTapLogin.Instance.GetCurrentTapAccount();
        }
        catch (Exception e)
        {
            Debug.Log($"获取用户信息失败：{e.Message}");
        }

        if (account == null)
        {
            // 无有效账号，强制登出
            TapTapLogin.Instance.Logout();
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "请先完成TapTap登录！");
            return;
        }

        // 调用官方公开API启动实名认证
        TapTapCompliance.Startup(account.unionId);
        Debug.Log($"===== 启动实名认证检查，用户标识：{account.unionId} =====");
    }
    #endregion

    #region 登录状态检查+生命周期
    public async void CheckLoginState()
    {
        if (!InitSDK()) return;

        try
        {
            var account = await TapTapLogin.Instance.GetCurrentTapAccount();
            if (account == null)
            {
                Debug.Log("===== 当前无登录账号 =====");
                UImanager.Instance.ShowPanel<TapTapLoginPanel>();
            }
            else
            {
                Debug.Log("===== 检测到已登录账号 =====");
                if (!hasCheckedCompliance)
                {
                    StartCheckCompliance(); // 此时可正常调用
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"检查登录状态失败：{e.Message}");
        }
    }

    private void Start()
    {
        Debug.Log("===== TapTap登录管理器启动 =====");
        CheckLoginState();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        isSdkInited = false;
        hasCheckedCompliance = false;
        ComplianceCallback = null; // 清空委托，避免内存泄漏
    }
    #endregion
}