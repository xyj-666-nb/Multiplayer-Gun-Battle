using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// Relay专用Android网络权限管理
/// 严格遵循高版本安卓规则，只检查/引导网络相关权限
/// </summary>
public class AndroidNetworkPermissionManager : MonoBehaviour
{
    [Header("配置")]
    public bool autoCheckOnStart = true;

    // 仅检查、不申请的权限（安装时自动授予/需手动开启）
    private Dictionary<string, string> _networkPermissions = new Dictionary<string, string>()
    {
        { "android.permission.INTERNET", "基础互联网权限（Relay必需）" },
        { "android.permission.ACCESS_NETWORK_STATE", "网络状态访问权限（Relay必需）" },
        { "android.permission.ACCESS_WIFI_STATE", "WiFi状态访问权限" },
        { "android.permission.CHANGE_WIFI_MULTICAST_STATE", "WiFi组播权限（UDP联机优化）" },
        { "android.permission.USE_BACKGROUND_NETWORK", "后台网络权限（后台联机必需）" },
        { "android.permission.FOREGROUND_SERVICE", "前台服务权限（后台联机必需）" }
    };

    private AndroidJavaObject _androidActivity;
    private const int PERMISSION_GRANTED = 0;

    void Start()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogWarning("[权限管理] 非Android平台，跳过权限处理");
            return;
        }

        InitAndroidObjects();
        if (autoCheckOnStart)
        {
            StartCoroutine(NetworkPermissionCheckCoroutine());
        }
    }

    #region 初始化
    private void InitAndroidObjects()
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _androidActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[权限管理] 初始化失败：{e.Message}");
        }
    }
    #endregion

    #region 核心网络权限检查流程
    private IEnumerator NetworkPermissionCheckCoroutine()
    {
        int sdkVersion = GetAndroidSDKVersion();
        CheckAllNetworkPermissions();
        yield return new WaitForSeconds(0.5f);
        FinalNetworkPermissionGuide();
        CheckNetworkConfig();

    }
    #endregion

    #region 网络权限检查
    private void CheckAllNetworkPermissions()
    {
        Debug.Log("\n---------------- 核心网络权限检查 ----------------");
        foreach (var perm in _networkPermissions)
        {
            bool isGranted = CheckSinglePermission(perm.Key);
            Debug.Log($"【{perm.Value}】：{(isGranted ? " 已授予" : " 未授予/需手动开启")}");
        }
        Debug.Log("---------------------------------------------\n");
    }

    /// <summary>
    /// 检查单个权限状态
    /// </summary>
    private bool CheckSinglePermission(string permissionName)
    {
        if (_androidActivity == null) return false;

        try
        {
            int sdkVersion = GetAndroidSDKVersion();
            // Android 6.0（23）以下权限自动授予，无需检查
            if (sdkVersion < 23) return true;

            int status = _androidActivity.Call<int>("checkSelfPermission", permissionName);
            return status == PERMISSION_GRANTED;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[权限管理] 检查权限 {permissionName} 失败：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 最终网络权限引导（只提示，不申请）
    /// </summary>
    private void FinalNetworkPermissionGuide()
    {
        bool hasCriticalProblem = false;
        if (!CheckSinglePermission("android.permission.INTERNET"))
        {
            hasCriticalProblem = true;
            Debug.LogWarning("【严重】基础互联网权限缺失！请重新安装应用");
        }

        if (!CheckSinglePermission("android.permission.USE_BACKGROUND_NETWORK"))
        {
            hasCriticalProblem = true;
            Debug.LogWarning("【重要】后台网络权限未开启！请手动设置：\n设置 → 应用 → 你的游戏 → 流量管理 → 开启「后台数据/无限制数据访问」");
        }

        if (!CheckSinglePermission("android.permission.FOREGROUND_SERVICE"))
        {
            Debug.LogWarning("【建议】前台服务权限未开启！请手动设置：\n设置 → 应用 → 你的游戏 → 权限 → 开启「前台服务」");
        }

        if (!hasCriticalProblem)
        {
            Debug.Log(" 核心网络权限均正常，Relay联机环境就绪");
        }
        Debug.Log("-------------------------------------------------\n");
    }
    #endregion

    #region 网络配置检查（关键）
    private void CheckNetworkConfig()
    {

        bool runInBackground = Application.runInBackground;
        Debug.Log($"【Unity后台运行】：{(runInBackground ? " 开启（正常）" : " 关闭（必须开启）")}");

        CheckNetworkType();

        Debug.Log("---------------------------------------------\n");
    }

    /// <summary>
    /// 检查当前网络类型（WiFi/移动数据）
    /// </summary>
    private void CheckNetworkType()
    {
        try
        {
            AndroidJavaClass connectivityManagerClass = new AndroidJavaClass("android.net.ConnectivityManager");
            AndroidJavaObject connectivityManager = _androidActivity.Call<AndroidJavaObject>("getSystemService", "connectivity");
            AndroidJavaObject networkInfo = connectivityManager.Call<AndroidJavaObject>("getActiveNetworkInfo");

            if (networkInfo == null || !networkInfo.Call<bool>("isConnected"))
            {
                Debug.LogWarning("【网络状态】 无可用网络！请检查网络连接");
                return;
            }

            int type = networkInfo.Call<int>("getType");
            string typeName = type == 1 ? "WiFi（推荐）" : (type == 0 ? "移动数据（可能有NAT限制）" : "其他网络");
            Debug.Log($"【网络状态】 当前连接：{typeName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【网络状态】检查失败：{e.Message}");
        }
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 获取Android SDK版本
    /// </summary>
    private int GetAndroidSDKVersion()
    {
        try
        {
            using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return versionClass.GetStatic<int>("SDK_INT");
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 手动触发网络权限检查（供外部调用）
    /// </summary>
    public void ManualTriggerNetworkCheck()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        StartCoroutine(NetworkPermissionCheckCoroutine());
    }
    #endregion
}