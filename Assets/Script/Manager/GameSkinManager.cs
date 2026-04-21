using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameSkinManager : SingleMonoAutoBehavior<GameSkinManager>
{
    [Header("开发者模式")]
    [Tooltip("开启后：自动解锁所有物品")]
    public bool IsDeveloperMode = false;

    [Header("玩家数据")]
    [Header("玩家枪械基础配置")]
    public List<GunSkinConfig> GunSkinConfigList;
    [Header("玩家角色皮肤数据")]
    public List<PlayerSkinPack> PlayerOwnerSkinPackList;
    public PlayerSkinPack CurrentPlayerSkinPack;
    [Header("玩家打击特效数据")]
    public GunHitData CurrentOwnerHitObj;
    public List<GunHitData> CurrentGunHitDataList;
    [Header("玩家子弹包数据")]
    public List<int> CurrentBulletBundleList;

    [Header("全局预载 - 角色皮肤")]
    public List<PlayerSkinPack> AllPlayerSkinPackList;
    [Header("全局预载 - 打击特效")]
    public List<GunHitData> AllGunHitDataList;
    [Header("全局预载 - 子弹包")]
    public List<SpecialBulletBindPack> AllBulletBundleList = new List<SpecialBulletBindPack>();

    [Header("全局预载 - 枪械皮肤总池")]
    public List<GunSkinPack> AllGunSkinPackList;
    [Header("玩家已拥有 - 枪械皮肤（默认基础皮肤）")]
    public List<GunSkinPack> CurrentGunSkinPackList;
    [Header("枪械装备配置 - 每把枪的已装备皮肤")]
    public List<GunEquipmentConfig> GunEquipmentConfigList;

    // 运行时字典
    private Dictionary<int, SpecialBulletBindPack> _bulletBundleDict;
    private Dictionary<int, PlayerSkinPack> _playerSkinDict;
    private Dictionary<int, GunHitData> _gunHitDict;
    private Dictionary<int, GunSkinPack> _gunSkinDict;

    // 单独记录当前装备的子弹包ID
    private int _currentEquippedBulletBundleID = -1;

    // 存档文件名
    private const string _skinEquipSaveFileName = "PlayerSkinEquipData";

    // 历史存档文件名
    private const string _oldSkinSaveFileName = "PlayerSkinData";
    private const string _oldSkinEquipSaveFileName = "PlayerSkinEquipData";

    public UnityAction DataLoadCallBack;

    #region 存档数据结构
    [Serializable]
    private class SkinEquipSaveData
    {
        public int currentPlayerSkinID = -1;
        public int currentHitEffectID = -1;
        public List<GunEquipmentSaveData> gunEquipmentSaveDatas = new List<GunEquipmentSaveData>();
        public List<GunSkinConfigSaveData> gunSkinConfigSaveDatas = new List<GunSkinConfigSaveData>();
    }

    [Serializable]
    private class GunEquipmentSaveData
    {
        public string gunName;
        public int? equippedSkinID;
    }

    [Serializable]
    private class GunSkinConfigSaveData
    {
        public GunType gunType;
        public int? bulletBindID;
    }
    #endregion

    #region 保存与加载核心逻辑
    /// <summary>
    /// 保存当前装备配置数据
    /// </summary>
    public void SavePlayerData()
    {
        if (!Application.isPlaying)
            return;
        if (IsDeveloperMode)
            return;

        SkinEquipSaveData saveData = new SkinEquipSaveData();

        // 保存当前角色皮肤ID
        saveData.currentPlayerSkinID = CurrentPlayerSkinPack != null ? CurrentPlayerSkinPack.PlayerSkinID : -1;

        // 保存当前打击特效ID
        saveData.currentHitEffectID = CurrentOwnerHitObj != null ? CurrentOwnerHitObj.HitID : -1;

        // 保存枪械装备配置
        foreach (var config in GunEquipmentConfigList)
        {
            if (config == null) continue;
            saveData.gunEquipmentSaveDatas.Add(new GunEquipmentSaveData
            {
                gunName = config.GunName,
                equippedSkinID = config.EquippedSkin?.skinGuid
            });
        }

        // 保存枪械基础配置（子弹包）
        foreach (var config in GunSkinConfigList)
        {
            if (config == null) continue;
            saveData.gunSkinConfigSaveDatas.Add(new GunSkinConfigSaveData
            {
                gunType = config.CurrentType,
                bulletBindID = config.BulletBindPack?.BulletBindID
            });
        }

        DataEncryptionManger.Instance.SaveEncryptedComplexData(_skinEquipSaveFileName, saveData);
    }

    /// <summary>
    /// 仅加载存档数据，不做赋值
    /// </summary>
    private SkinEquipSaveData LoadPlayerData()
    {
        if (!Application.isPlaying) return null;
        if (IsDeveloperMode) return null;

        return DataEncryptionManger.Instance.LoadEncryptedComplexData<SkinEquipSaveData>(_skinEquipSaveFileName);
    }
    #endregion

    #region 装备还原与校验
    /// <summary>
    /// 归还上次游戏的装备数据，进行本地数据校验和加载（由外部调用）
    /// </summary>
    public void ReturnLastGameEquipment()
    {
        var saveData = LoadPlayerData();
        if (saveData == null)
        {
            Debug.Log("无存档装备数据，使用默认配置");
            DataLoadCallBack?.Invoke();
            return;
        }

        if (saveData.currentPlayerSkinID >= 0 && _playerSkinDict.TryGetValue(saveData.currentPlayerSkinID, out var playerSkin))
        {
            if (PlayerOwnerSkinPackList.Contains(playerSkin))
            {
                CurrentPlayerSkinPack = playerSkin;
            }
        }

        if (saveData.currentHitEffectID >= 0 && _gunHitDict.TryGetValue(saveData.currentHitEffectID, out var hitData))
        {
            if (CurrentGunHitDataList.Contains(hitData))
            {
                CurrentOwnerHitObj = hitData;
            }
        }

        foreach (var saveItem in saveData.gunEquipmentSaveDatas)
        {
            foreach (var config in GunEquipmentConfigList)
            {
                if (config.GunName == saveItem.gunName)
                {
                    if (saveItem.equippedSkinID.HasValue && _gunSkinDict.TryGetValue(saveItem.equippedSkinID.Value, out var skinPack))
                    {
                        if (CurrentGunSkinPackList.Contains(skinPack))
                        {
                            config.EquippedSkin = skinPack;
                        }
                    }
                    break;
                }
            }
        }

        // 4. 还原枪械基础配置列表（子弹包）
        foreach (var saveItem in saveData.gunSkinConfigSaveDatas)
        {
            foreach (var config in GunSkinConfigList)
            {
                if (config.CurrentType == saveItem.gunType)
                {
                    if (saveItem.bulletBindID.HasValue && _bulletBundleDict.TryGetValue(saveItem.bulletBindID.Value, out var bulletPack))
                    {
                        if (CurrentBulletBundleList.Contains(bulletPack.BulletBindID))
                        {
                            config.BulletBindPack = bulletPack;
                            _currentEquippedBulletBundleID = bulletPack.BulletBindID;
                        }
                    }
                    break;
                }
            }
        }

        Debug.Log("装备数据还原完成");
        DataLoadCallBack?.Invoke();
    }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        InitRuntimeDictionaries();
        InitDefaultGunEquipmentData();

        // 数据同步：开发者模式解锁全物品，否则从商品系统同步拥有数据
        if (IsDeveloperMode)
        {
            UnlockAllDefaultData();
        }
        else
        {
            CalibrateDataWithGoodsManager();
        }

        // 自动补全默认皮肤
        AutoSetDefaultGunSkin();
    }

    /// <summary>
    /// 初始化枪械配置
    /// </summary>
    private void InitDefaultGunEquipmentData()
    {
        if (GunEquipmentConfigList != null && GunEquipmentConfigList.Count > 0)
            return;

        GunEquipmentConfigList = new List<GunEquipmentConfig>();
        var gunNames = AllGunSkinPackList.Select(x => x.GunRealName).Distinct().ToList();

        foreach (var gunName in gunNames)
        {
            GunEquipmentConfig config = new GunEquipmentConfig
            {
                GunName = gunName,
                EquippedSkin = null
            };
            GunEquipmentConfigList.Add(config);
        }
    }

    /// <summary>
    /// 自动为所有枪械挂载默认基础皮肤
    /// </summary>
    private void AutoSetDefaultGunSkin()
    {
        foreach (var gunConfig in GunEquipmentConfigList)
        {
            if (gunConfig.EquippedSkin != null) continue;

            // 从玩家默认拥有的皮肤中匹配对应枪械
            var defaultSkin = CurrentGunSkinPackList.FirstOrDefault(skin => skin.GunRealName == gunConfig.GunName);
            if (defaultSkin != null)
            {
                gunConfig.EquippedSkin = defaultSkin;
            }
        }
    }

    #region 枪械皮肤 - 装备
    public void EquipmentGunSkin(int skinID)
    {
        if (!_gunSkinDict.ContainsKey(skinID))
            return;
        EquipmentGunSkin(_gunSkinDict[skinID]);
    }

    public void EquipmentGunSkin(GunSkinPack skinPack)
    {
        if (skinPack == null || !CurrentGunSkinPackList.Contains(skinPack)) return;

        foreach (var config in GunEquipmentConfigList)
        {
            if (config.GunName == skinPack.GunRealName)
            {
                config.EquippedSkin = skinPack;
                SavePlayerData();
                Debug.Log($"装备枪械皮肤成功：{skinPack.name}");
                return;
            }
        }
    }
    #endregion

    #region 枪械皮肤 - 查询
    public GunSkinPack GetCurrentGunEquipmentSkinPack(string gunName)
    {
        if (string.IsNullOrEmpty(gunName)) return null;
        foreach (var config in GunEquipmentConfigList)
        {
            if (config.GunName == gunName)
                return config.EquippedSkin;
        }
        return null;
    }

    public bool HasGunSkin(GunSkinPack skinPack)
    {
        return skinPack != null && CurrentGunSkinPackList.Contains(skinPack);
    }

    public GunSkinPack GetGunSkinPack(int ID)
    {
        foreach (var skin in AllGunSkinPackList)
        {
            if (skin.skinGuid == ID)
            {
                return skin;
            }
        }

        return null;
    }
    #endregion

    #region 枪械皮肤 - 解锁
    public void AddGunSkinPack(GunSkinPack skinPack)
    {
        if (skinPack == null) return;
        if (!AllGunSkinPackList.Contains(skinPack)) return;
        if (CurrentGunSkinPackList.Contains(skinPack)) return;

        CurrentGunSkinPackList.Add(skinPack);
    }

    public void UnlockAllGunSkins()
    {
        foreach (var skin in AllGunSkinPackList)
        {
            if (!CurrentGunSkinPackList.Contains(skin))
                CurrentGunSkinPackList.Add(skin);
        }
    }
    #endregion

    #region 字典初始化
    private void InitRuntimeDictionaries()
    {
        // 子弹包
        _bulletBundleDict = new Dictionary<int, SpecialBulletBindPack>();
        foreach (var pack in AllBulletBundleList)
        {
            if (pack == null)
                continue;
            if (!_bulletBundleDict.ContainsKey(pack.BulletBindID))
                _bulletBundleDict.Add(pack.BulletBindID, pack);
        }

        // 角色皮肤
        _playerSkinDict = new Dictionary<int, PlayerSkinPack>();
        foreach (var pack in AllPlayerSkinPackList)
        {
            if (pack == null) continue;
            if (!_playerSkinDict.ContainsKey(pack.PlayerSkinID))
                _playerSkinDict.Add(pack.PlayerSkinID, pack);
        }

        // 打击特效
        _gunHitDict = new Dictionary<int, GunHitData>();
        foreach (var data in AllGunHitDataList)
        {
            if (data == null) continue;
            if (!_gunHitDict.ContainsKey(data.HitID))
                _gunHitDict.Add(data.HitID, data);
        }

        // 枪械皮肤
        _gunSkinDict = new Dictionary<int, GunSkinPack>();
        foreach (var skin in AllGunSkinPackList)
        {
            if (skin == null) continue;
            int id = skin.skinGuid;
            if (!_gunSkinDict.ContainsKey(id))
                _gunSkinDict.Add(id, skin);
        }
    }
    #endregion

    #region 清除数据
    /// <summary>
    /// 清除所有运行时拥有数据+历史本地存档，不修改GunSkinConfigList和GunEquipmentConfigList
    /// </summary>
    public void ClearAllSkinData()
    {
        if (!Application.isPlaying)
        { Debug.LogWarning("请在运行时使用"); return; }

        //仅清空玩家拥有的皮肤列表，完全不碰GunSkinConfigList和GunEquipmentConfigList
        PlayerOwnerSkinPackList.Clear();
        CurrentGunHitDataList.Clear();
        CurrentBulletBundleList.Clear();
        CurrentGunSkinPackList.Clear();

        // 重置当前装备引用
        CurrentPlayerSkinPack = null;
        CurrentOwnerHitObj = null;
        _currentEquippedBulletBundleID = -1;

        //  彻底清除历史本地存档数据
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(_oldSkinSaveFileName);
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(_oldSkinEquipSaveFileName);
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(_skinEquipSaveFileName);

        Debug.Log("已清空所有皮肤运行时数据+历史本地存档，枪械配置与装备列表已完整保留");
    }
    #endregion

    #region 通用功能
    /// <summary>
    /// 开发者模式：解锁所有物品
    /// </summary>
    private void UnlockAllDefaultData()
    {
        // 解锁所有默认资源
        foreach (var skin in AllPlayerSkinPackList)
            if (!PlayerOwnerSkinPackList.Contains(skin)) PlayerOwnerSkinPackList.Add(skin);
        foreach (var hit in AllGunHitDataList)
            if (!CurrentGunHitDataList.Contains(hit)) CurrentGunHitDataList.Add(hit);
        foreach (var pack in AllBulletBundleList)
            if (!CurrentBulletBundleList.Contains(pack.BulletBindID)) CurrentBulletBundleList.Add(pack.BulletBindID);

        UnlockAllGunSkins();
    }

    /// <summary>
    /// 从商品系统同步玩家已拥有的皮肤数据
    /// </summary>
    private void CalibrateDataWithGoodsManager()
    {
        if (IsDeveloperMode || GoodDataManager.Instance == null) return;
        var list = GoodDataManager.Instance.UserObtainGoodsList ?? new List<GoodsData>();

        foreach (var goods in list)
        {
            if (goods == null) continue;
            switch (goods.skinType)
            {
                case SkinType.PlayerCharacter:
                    if (goods.playerSkinPack && !PlayerOwnerSkinPackList.Contains(goods.playerSkinPack))
                        PlayerOwnerSkinPackList.Add(goods.playerSkinPack);
                    break;
                case SkinType.GunHitEffect:
                    if (goods.gunHitData && !CurrentGunHitDataList.Contains(goods.gunHitData))
                        CurrentGunHitDataList.Add(goods.gunHitData);
                    break;
                case SkinType.SpecialBullet:
                    if (goods.bulletPack && !CurrentBulletBundleList.Contains(goods.bulletPack.BulletBindID))
                        CurrentBulletBundleList.Add(goods.bulletPack.BulletBindID);
                    break;
                case SkinType.GunAppearance:
                    if (goods.gunSkinPack && !CurrentGunSkinPackList.Contains(goods.gunSkinPack))
                        CurrentGunSkinPackList.Add(goods.gunSkinPack);
                    break;
            }
        }
    }

    public void SetPlayerSkinPack(PlayerSkinPack SkinPack)
    {
        CurrentPlayerSkinPack = SkinPack;

        SavePlayerData();
    }

    public void SetPlayerSkinPack(int PackID)
    {
        if (_playerSkinDict.TryGetValue(PackID, out var pack)) 
            SetPlayerSkinPack(pack);
    }

    public void SetCurrentHitEffect(GunHitData hitData)
    {
        CurrentOwnerHitObj = hitData;
        SavePlayerData();
    }

    public void SetCurrentHitEffect(int hitID)
    {
        if (_gunHitDict.TryGetValue(hitID, out var data)) SetCurrentHitEffect(data);
    }

    public void EquipBulletBindPack(int bundleID)
    {
        if (bundleID < 0 || !CurrentBulletBundleList.Contains(bundleID)) return;
        var pack = FindBulletBindPack(bundleID);
        if (pack != null)
        {
            _currentEquippedBulletBundleID = bundleID;
            // 更新所有对应枪械类型的配置
            foreach (var config in GunSkinConfigList)
            {
                if (config.CurrentType == pack.gunType)
                {
                    config.BulletBindPack = pack;
                }
            }
            SavePlayerData();
        }
    }

    public void EquipBulletBindPack(SpecialBulletBindPack bundlePack)
    {
        if (bundlePack == null || !CurrentBulletBundleList.Contains(bundlePack.BulletBindID)) return;
        _currentEquippedBulletBundleID = bundlePack.BulletBindID;
        // 更新所有对应枪械类型的配置
        foreach (var config in GunSkinConfigList)
        {
            if (config.CurrentType == bundlePack.gunType)
            {
                config.BulletBindPack = bundlePack;
            }
        }
        SavePlayerData();
    }

    public List<SpecialBulletBindPack> GetSpecialBulletBindPackList(GunType Type)
    {
        List<SpecialBulletBindPack> list = new List<SpecialBulletBindPack>();
        foreach (var id in CurrentBulletBundleList)
            if (_bulletBundleDict.TryGetValue(id, out var pack) && pack.gunType == Type)
                list.Add(pack);
        return list;
    }

    public GunHitData GetHitData(int ID)
    {
        _gunHitDict.TryGetValue(ID, out var data); return data;
    }

    public PlayerSkinPack GetPlayerSkipPack(int PackID)
    {
        _playerSkinDict.TryGetValue(PackID, out var pack); return pack;
    }

    public GunSkinConfig ReturnGunSkinConfig(GunType Type)
    {
        return GunSkinConfigList.FirstOrDefault(c => c.CurrentType == Type);
    }

    public BulletVisualConfig ReturnBulletVisualConfig(GunType Type)
    {
        var config = ReturnGunSkinConfig(Type);
        return config?.BulletBindPack?.bulletVisualConfig;
    }

    public MuzzleFlashConfig ReturnMuzzleFlashConfig(GunType Type)
    {
        var config = ReturnGunSkinConfig(Type);
        return config?.BulletBindPack?.muzzleFlashConfig;
    }

    public SpecialBulletBindPack FindBulletBindPack(int bundleID)
    {
        _bulletBundleDict.TryGetValue(bundleID, out var pack); return pack;
    }
    #endregion

    #region GM工具
    [ContextMenu("GM_清空所有皮肤数据+历史存档")]
    private void GM_ClearAllSkinData()
    {
        ClearAllSkinData();
    }

    [ContextMenu("GM_解锁所有枪械皮肤")]
    private void GM_UnlockAllGunSkins()
    {
        if (!Application.isPlaying) return;
        UnlockAllGunSkins();
        AutoSetDefaultGunSkin();
    }
    #endregion
}

[System.Serializable]
public class GunSkinConfig
{
    public GunType CurrentType;
    public SpecialBulletBindPack BulletBindPack; // 子弹捆绑包
}

[System.Serializable]
public class GunEquipmentConfig
{
    public string GunName;
    public GunSkinPack EquippedSkin;
}