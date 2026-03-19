using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Android网络权限检查工具
/// 打印所有关键网络权限的授予状态到控制台
/// </summary>
public class AndroidNetworkPermissionChecker : MonoBehaviour
{
    // 需要检查的关键网络权限列表（对应之前配置的权限）
    private Dictionary<string, string> _networkPermissions = new Dictionary<string, string>()
    {
        { "android.permission.INTERNET", "基础互联网权限（Relay/UDP必需）" },
        { "android.permission.ACCESS_NETWORK_STATE", "网络状态访问权限" },
        { "android.permission.ACCESS_WIFI_STATE", "WiFi状态访问权限" },
        { "android.permission.CHANGE_WIFI_MULTICAST_STATE", "WiFi组播权限（局域网联机）" },
        { "android.permission.USE_BACKGROUND_NETWORK", "后台网络权限（Android 12+ UDP长连接）" },
        { "android.permission.FOREGROUND_SERVICE", "前台服务权限（防止UDP连接被杀死）" },
        { "android.permission.ACCESS_BACKGROUND_LOCATION", "后台定位（部分网络权限依赖）" }
    };

    private AndroidJavaObject _androidActivity; // Android当前Activity
    private AndroidJavaClass _permissionChecker; // Android权限检查类

    void Start()
    {
        // 只在Android平台执行检查
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogWarning("[权限检查] 当前不是Android平台，跳过权限检查");
            return;
        }

        InitAndroidObjects();
        CheckAllNetworkPermissions();
    }

    /// <summary>
    /// 初始化Android相关对象
    /// </summary>
    private void InitAndroidObjects()
    {
        try
        {
            // 获取Unity的当前Activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _androidActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // 获取Android的权限检查类
            _permissionChecker = new AndroidJavaClass("android.content.pm.PackageManager");
            Debug.Log("[权限检查] Android对象初始化成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[权限检查] Android对象初始化失败：{e.Message}");
        }
    }

    /// <summary>
    /// 检查单个权限的授予状态
    /// </summary>
    /// <param name="permissionName">权限名称（如android.permission.INTERNET）</param>
    /// <returns>true=已授予，false=未授予</returns>
    private bool CheckSinglePermission(string permissionName)
    {
        if (_androidActivity == null || _permissionChecker == null)
        {
            Debug.LogError($"[权限检查] 未初始化Android对象，无法检查权限：{permissionName}");
            return false;
        }

        try
        {
            // Android权限检查常量：PERMISSION_GRANTED = 0，PERMISSION_DENIED = -1
            int PERMISSION_GRANTED = _permissionChecker.GetStatic<int>("PERMISSION_GRANTED");

            // Android 6.0（API 23）以上需要用checkSelfPermission检查运行时权限
            int androidSDKVersion = GetAndroidSDKVersion();
            if (androidSDKVersion >= 23)
            {
                int permissionStatus = _androidActivity.Call<int>("checkSelfPermission", permissionName);
                return permissionStatus == PERMISSION_GRANTED;
            }
            else
            {
                // Android 6.0以下：只要Manifest声明就默认授予
                Debug.Log($"[权限检查] Android {androidSDKVersion}（<6.0），Manifest声明即授予权限：{permissionName}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[权限检查] 检查权限{permissionName}失败：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 批量检查所有网络权限并打印详细日志
    /// </summary>
    private void CheckAllNetworkPermissions()
    {
        Debug.Log("\n==================== 网络权限检查结果 ====================");
        int androidSDKVersion = GetAndroidSDKVersion();
        Debug.Log($"当前Android SDK版本：{androidSDKVersion}（{GetAndroidVersionName(androidSDKVersion)}）");

        foreach (var permission in _networkPermissions)
        {
            string permName = permission.Key;
            string permDesc = permission.Value;
            bool isGranted = CheckSinglePermission(permName);

            // 特殊说明：INTERNET权限在所有Android版本中，Manifest声明即自动授予（无需运行时授权）
            string specialNote = permName == "android.permission.INTERNET" ? "【特殊】Manifest声明即自动授予，无需运行时授权" : "";

            Debug.Log($"【{permName}】\n描述：{permDesc}\n状态：{(isGranted ? " 已授予" : " 未授予")}\n备注：{specialNote}\n");
        }

        // 额外检查关键配置
        CheckNetworkConfig();

        Debug.Log("===========================================================\n");
    }

    /// <summary>
    /// 检查额外网络配置（如后台运行、明文流量等）
    /// </summary>
    private void CheckNetworkConfig()
    {
        // 检查Unity的后台运行设置
        bool runInBackground = Application.runInBackground;
        Debug.Log($"【Unity配置】RunInBackground：{(runInBackground ? " 开启" : " 关闭")}（后台联网必需）");

        // 检查当前网络类型（WiFi/移动数据）
        CheckNetworkType();
    }

    /// <summary>
    /// 检查当前设备的网络类型（WiFi/移动数据/无网络）
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
                Debug.Log($"【网络状态】 当前无可用网络连接");
                return;
            }

            int type = networkInfo.Call<int>("getType");
            string networkTypeName = type == 1 ? "WiFi" : (type == 0 ? "移动数据（蜂窝网络）" : "其他网络");
            Debug.Log($"【网络状态】 当前连接类型：{networkTypeName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【网络状态】检查失败：{e.Message}");
        }
    }

    /// <summary>
    /// 获取当前Android设备的SDK版本（如23=Android 6.0）
    /// </summary>
    private int GetAndroidSDKVersion()
    {
        using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return versionClass.GetStatic<int>("SDK_INT");
        }
    }

    /// <summary>
    /// 将SDK版本转换为Android版本名称（便于阅读）
    /// </summary>
    private string GetAndroidVersionName(int sdkVersion)
    {
        Dictionary<int, string> versionMap = new Dictionary<int, string>()
        {
            {23, "6.0 (Marshmallow)"},
            {24, "7.0 (Nougat)"},
            {28, "9.0 (Pie)"},
            {31, "12.0 (Snow Cone)"},
            {33, "13.0 (Tiramisu)"}
        };
        return versionMap.ContainsKey(sdkVersion) ? versionMap[sdkVersion] : $"未知版本（SDK {sdkVersion}）";
    }

    /// <summary>
    /// 手动触发权限检查（可绑定到UI按钮）
    /// </summary>
    public void ManualCheckPermissions()
    {
        Debug.Log("\n========== 手动触发权限检查 ==========");
        CheckAllNetworkPermissions();
    }
}