using System.Collections.Generic;
using UnityEngine;

public class GameSkinManager : SingleMonoAutoBehavior<GameSkinManager>
{
    [Header("玩家数据")]
    [Header("玩家枪械配置")]
    public List<GunSkinConfig> GunSkinConfigList;
    [Header("玩家皮肤数据")]
    public List<PlayerSkinPack> PlayerOwnerSkinPackList;
    public PlayerSkinPack CurrentPlayerSkinPack;

    [Header("角色皮肤数据配置预载管理")]
    public List<PlayerSkinPack> AllPlayerSkinPackList;
    [Header("打击粒子特效数据预载管理")]
    public List<GunHitData> AllGunHitDataList;
    [Header("子弹捆绑包数据预载管理")]
    public List<SpecialBulletBindPack> AllBulletBundleList = new List<SpecialBulletBindPack>();

    // 运行时字典
    private Dictionary<int, SpecialBulletBindPack> _bulletBundleDict;

    // 设置玩家皮肤包
    public void SetPlayerSkinPack(PlayerSkinPack SkinPack)
    {
        CurrentPlayerSkinPack = SkinPack;
    }

    protected override void Awake()
    {
        base.Awake();
        InitRuntimeDictionaries();
    }

    public List<SpecialBulletBindPack> GetSpecialBulletBindPackList(GunType Type)
    {
        // 优化：使用局部变量，避免全局列表冲突
        List<SpecialBulletBindPack> resultList = new List<SpecialBulletBindPack>();
        foreach (SpecialBulletBindPack pack in AllBulletBundleList)
        {
            if (pack.gunType == Type)
            {
                resultList.Add(pack);
            }
        }
        return resultList;
    }

    // 初始化运行时字典
    private void InitRuntimeDictionaries()
    {
        // 初始化子弹捆绑包字典
        _bulletBundleDict = new Dictionary<int, SpecialBulletBindPack>();
        foreach (var pack in AllBulletBundleList)
        {
            if (pack == null) continue;

            if (!_bulletBundleDict.ContainsKey(pack.BulletBindID))
            {
                _bulletBundleDict.Add(pack.BulletBindID, pack);
            }
            else
            {
                Debug.LogWarning($"重复子弹捆绑包ID：{pack.BulletBindID}，已跳过");
            }
        }

    }

    public GunSkinConfig ReturnGunSkinConfig(GunType Type)
    {
        foreach (var config in GunSkinConfigList)
        {
            if (config.CurrentType == Type)
                return config;
        }
        Debug.LogError($"未找到枪械类型 {Type} 的皮肤配置");
        return null;
    }

    public BulletVisualConfig ReturnBulletVisualConfig(GunType Type)
      => ReturnGunSkinConfig(Type)?.bulletConfig;

    public MuzzleFlashConfig ReturnMuzzleFlashConfig(GunType Type)
      => ReturnGunSkinConfig(Type)?.muzzleFlashConfig;

    #region 装备数据

    /// <summary>
    /// 通过ID装备子弹捆绑包
    /// </summary>
    public void EquipBulletBindPack(int bundleID)
    {
        var pack = FindBulletBindPack(bundleID);
        if (pack != null)
        {
            EquipBulletBindPack(pack);
        }
    }

    /// <summary>
    /// 直接装备子弹捆绑包
    /// </summary>
    public void EquipBulletBindPack(SpecialBulletBindPack bundlePack)
    {
        if (bundlePack == null)
        {
            Debug.LogError("无法装备，传入的子弹捆绑包为Null");
            return;
        }

        bool equipped = false;
        foreach (var config in GunSkinConfigList)
        {
            if (bundlePack.bulletVisualConfig != null &&
                config.CurrentType == bundlePack.bulletVisualConfig.gunType)
            {
                // 同时装备子弹和火光
                config.bulletConfig = bundlePack.bulletVisualConfig;
                config.muzzleFlashConfig = bundlePack.muzzleFlashConfig;
                Debug.Log($"[装备成功] 枪械 {config.CurrentType} 已装备捆绑包：{bundlePack.BulletBindName}");
                equipped = true;
            }
        }

        if (!equipped)
        {
            Debug.LogWarning($"未找到匹配的枪械来装备捆绑包：{bundlePack.BulletBindName}");
        }
    }

    #endregion

    #region 查询数据

    /// <summary>
    /// 通过ID查找子弹捆绑包
    /// </summary>
    public SpecialBulletBindPack FindBulletBindPack(int bundleID)
    {
        if (_bulletBundleDict.TryGetValue(bundleID, out var pack))
        {
            return pack;
        }
        Debug.LogError($"未找到ID为 {bundleID} 的子弹捆绑包");
        return null;
    }

    #endregion
}

[System.Serializable]
public class GunSkinConfig
{
    public GunType CurrentType;
    public BulletVisualConfig bulletConfig;
    public MuzzleFlashConfig muzzleFlashConfig;
}