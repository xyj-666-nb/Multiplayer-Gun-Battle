using System.Collections.Generic;
using UnityEngine;

public class GameSkinManager : SingleMonoAutoBehavior<GameSkinManager>
{
    [Header("玩家枪械配置")]
    public List<GunSkinConfig> GunSkinConfigList;

    [Header("子弹数据预载管理")]
    public List<BulletVisualConfig> AllBulletVisualConfigList;
    [Header("枪口火光数据预载管理")]
    public List<MuzzleFlashConfig> AllMuzzleFlashConfigList;

    private Dictionary<int, BulletVisualConfig> _bulletConfigDict;
    private Dictionary<int, MuzzleFlashConfig> _muzzleFlashConfigDict;
    protected override void Awake()
    {
        base.Awake();
        // 启动时自动把List转成字典
        InitRuntimeDictionaries();
    }

    // 初始化运行时字典
    private void InitRuntimeDictionaries()
    {
        _bulletConfigDict = new Dictionary<int, BulletVisualConfig>();
        foreach (var config in AllBulletVisualConfigList)
        {
            if (!_bulletConfigDict.ContainsKey(config.BulletID))
                _bulletConfigDict.Add(config.BulletID, config);
            else
                Debug.LogWarning($"重复子弹ID：{config.BulletID}，已跳过");
        }

        _muzzleFlashConfigDict = new Dictionary<int, MuzzleFlashConfig>();
        foreach (var config in AllMuzzleFlashConfigList)
        {
            if (!_muzzleFlashConfigDict.ContainsKey(config.MuzzleFlashID))
                _muzzleFlashConfigDict.Add(config.MuzzleFlashID, config);
            else
                Debug.LogWarning($"重复火光ID：{config.MuzzleFlashID}，已跳过");
        }

        //清空内存

        AllBulletVisualConfigList.Clear();
        AllBulletVisualConfigList = null;

        AllMuzzleFlashConfigList.Clear();
        AllMuzzleFlashConfigList = null;
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
    public void EquipGunSki(GunSkinConfigType type,int DataID)
    {
        switch (type)
        {
            case GunSkinConfigType.Bullet:
                EquipBulletVisualConfig(FindListData<BulletVisualConfig>(DataID));
                break;
            case GunSkinConfigType.MuzzleFlash:
                EquipMuzzleFlashConfig(FindListData<MuzzleFlashConfig>(DataID));
                break;
            default:
                Debug.LogError($"不支持的配置类型：{type}");
                return;
        }

    }


    public void EquipBulletVisualConfig(BulletVisualConfig BulletConfig)
    {
        if(BulletConfig==null)
        {
            Debug.LogError("无法装备子弹配置，传入的BulletConfig为Null");
            return;
        }
        foreach (var config in GunSkinConfigList)
        {
            //装备该数据
            if(config.CurrentType == BulletConfig.gunType)
            {
                config.bulletConfig = BulletConfig;
                Debug.Log("装备成功");
            }
        }
       // Debug.LogError($"未找到枪械类型 {BulletConfig.gunType} 的皮肤配置，装备失败");
    }

    public void EquipMuzzleFlashConfig(MuzzleFlashConfig MuzzleFlashConfig)
    {
        if (MuzzleFlashConfig == null)
        {
            Debug.LogError("无法装备火光配置，传入的MuzzleFlashConfig为Null");
            return;
        }
        foreach (var config in GunSkinConfigList)
        {
            //装备该数据
            if (config.CurrentType == MuzzleFlashConfig.gunType)
            {
                config.muzzleFlashConfig = MuzzleFlashConfig;
                Debug.Log("装备成功");
            }
        }
        //Debug.LogError($"未找到枪械类型 {MuzzleFlashConfig.gunType} 的皮肤配置，装备失败");
    }


    #endregion

    #region 查询数据
    public T FindListData<T>(int dataID) where T : ScriptableObject
    {
        if (typeof(T) == typeof(BulletVisualConfig))
        {
            if (_bulletConfigDict.TryGetValue(dataID, out var config))
                return config as T;
            Debug.LogError($"未找到ID为 {dataID} 的子弹配置");
            return null;
        }
        else if (typeof(T) == typeof(MuzzleFlashConfig))
        {
            if (_muzzleFlashConfigDict.TryGetValue(dataID, out var config))
                return config as T;
            Debug.LogError($"未找到ID为 {dataID} 的火光配置");
            return null;
        }

        Debug.LogError($"不支持的配置类型：{typeof(T).Name}");
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

public enum GunSkinConfigType
{
    Bullet,
    MuzzleFlash
}