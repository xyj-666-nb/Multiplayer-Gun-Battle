using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局配置管理器（单例）
/// 仅存储子弹/火光配置的引用，无内存压力
/// </summary>
public class ConfigManager : SingleMonoAutoBehavior<ConfigManager>
{

    private Dictionary<int, BulletVisualConfig> _bulletDict = new Dictionary<int, BulletVisualConfig>();
    private Dictionary<int, MuzzleFlashConfig> _muzzleDict = new Dictionary<int, MuzzleFlashConfig>();
    protected override void Awake()
    {
        base.Awake();
        LoadAllConfigs();
    }

    /// <summary>
    /// 加载Resources下所有子弹/火光配置
    /// </summary>
    private void LoadAllConfigs()
    {
        // 加载所有子弹视觉配置
        BulletVisualConfig[] allBullets = Resources.LoadAll<BulletVisualConfig>("GameInfo/BulletInfo");
        foreach (var config in allBullets)
        {
            if (!_bulletDict.ContainsKey(config.BulletID))
                _bulletDict.Add(config.BulletID, config);
        }

        // 加载所有枪口火光配置
        MuzzleFlashConfig[] allMuzzles = Resources.LoadAll<MuzzleFlashConfig>("GameInfo/MuzzleFlashInfo");
        foreach (var config in allMuzzles)
        {
            if (!_muzzleDict.ContainsKey(config.MuzzleFlashID))
                _muzzleDict.Add(config.MuzzleFlashID, config);
        }

        Debug.Log($"配置加载完成：子弹{_bulletDict.Count}个 | 火光{_muzzleDict.Count}个");
    }

    #region 外部调用接口
    /// <summary> 根据BulletID获取子弹配置 </summary>
    public BulletVisualConfig GetBulletConfig(int id)
    {
        _bulletDict.TryGetValue(id, out var config);
        return config;
    }

    /// <summary> 根据MuzzleID获取火光配置 </summary>
    public MuzzleFlashConfig GetMuzzleConfig(int id)
    {
        _muzzleDict.TryGetValue(id, out var config);
        return config;
    }


    /// <summary>手动释放内存 </summary>
    public void ClearUnusedConfigs()
    {
        _bulletDict.Clear();
        _muzzleDict.Clear();
        Resources.UnloadUnusedAssets();
    }

    #endregion
}