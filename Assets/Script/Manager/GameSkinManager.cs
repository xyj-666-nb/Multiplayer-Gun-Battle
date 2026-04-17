using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameSkinManager : SingleMonoAutoBehavior<GameSkinManager>
{
    // 存档文件名
    private const string SkinSaveFileName = "PlayerSkinData";

    [Header("开发者模式")]
    [Tooltip("开启后：自动解锁所有物品，且禁止保存数据")]
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

    protected override void Awake()
    {
        base.Awake();
        InitRuntimeDictionaries();
        InitDefaultGunEquipmentData();
        LoadSkinData();
    }

    /// <summary>
    /// 初始化枪械配置（保留面板默认配置，不覆盖）
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
        if (!_gunSkinDict.ContainsKey(skinID)) return;
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
                SaveSkinData();
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
            if (config.GunName == gunName) return config.EquippedSkin;
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
        SaveSkinData();
    }

    public void UnlockAllGunSkins()
    {
        foreach (var skin in AllGunSkinPackList)
        {
            if (!CurrentGunSkinPackList.Contains(skin))
                CurrentGunSkinPackList.Add(skin);
        }
        SaveSkinData();
    }
    #endregion

    #region 字典初始化
    private void InitRuntimeDictionaries()
    {
        // 子弹包
        _bulletBundleDict = new Dictionary<int, SpecialBulletBindPack>();
        foreach (var pack in AllBulletBundleList)
        {
            if (pack == null) continue;
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

    #region 数据存储结构
    [Serializable]
    private class SkinSaveData
    {
        public List<int> ownedSkinIDs = new List<int>();
        public List<int> ownedHitEffectIDs = new List<int>();
        public List<int> ownedBulletBundleIDs = new List<int>();
        public int equippedSkinID = -1;
        public int equippedHitEffectID = -1;
        public int equippedBulletBundleID = -1;

        public List<int> ownedGunSkinIDs = new List<int>();
        public List<GunEquipmentSaveData> gunEquipmentSaveDatas = new List<GunEquipmentSaveData>();
    }

    [Serializable]
    private class GunEquipmentSaveData
    {
        public string gunName;
        public int? equippedSkinID;
    }
    #endregion

    #region 保存数据
    public void SaveSkinData()
    {
        if (!Application.isPlaying) return;
        if (IsDeveloperMode) return;

        SkinSaveData saveData = new SkinSaveData();

        // 角色皮肤
        foreach (var skin in PlayerOwnerSkinPackList)
            if (skin != null) saveData.ownedSkinIDs.Add(skin.PlayerSkinID);
        // 打击特效
        foreach (var hit in CurrentGunHitDataList)
            if (hit != null) saveData.ownedHitEffectIDs.Add(hit.HitID);
        // 子弹包
        saveData.ownedBulletBundleIDs = new List<int>(CurrentBulletBundleList);
        // 当前装备
        saveData.equippedSkinID = CurrentPlayerSkinPack != null ? CurrentPlayerSkinPack.PlayerSkinID : -1;
        saveData.equippedHitEffectID = CurrentOwnerHitObj != null ? CurrentOwnerHitObj.HitID : -1;
        saveData.equippedBulletBundleID = _currentEquippedBulletBundleID;

        // 枪械皮肤
        foreach (var skin in CurrentGunSkinPackList)
            if (skin != null) saveData.ownedGunSkinIDs.Add(skin.skinGuid);
        // 枪械装备配置
        foreach (var config in GunEquipmentConfigList)
        {
            if (config == null) continue;
            saveData.gunEquipmentSaveDatas.Add(new GunEquipmentSaveData
            {
                gunName = config.GunName,
                equippedSkinID = config.EquippedSkin?.skinGuid
            });
        }

        DataEncryptionManger.Instance.SaveEncryptedComplexData(SkinSaveFileName, saveData);
    }
    #endregion

    #region 加载数据
    public void LoadSkinData()
    {
        if (!Application.isPlaying) return;

        // 开发者模式
        if (IsDeveloperMode)
        {
            UnlockAllDefaultData();
            AutoSetDefaultGunSkin();
            return;
        }

        // 读取存档
        var saveData = DataEncryptionManger.Instance.LoadEncryptedComplexData<SkinSaveData>(SkinSaveFileName);
        if (saveData == null)
        {
            AutoSetDefaultGunSkin();
            Debug.Log("首次启动，应用默认皮肤配置");
            return;
        }

        // 加载角色皮肤
        foreach (var id in saveData.ownedSkinIDs)
            if (_playerSkinDict.TryGetValue(id, out var skin) && !PlayerOwnerSkinPackList.Contains(skin))
                PlayerOwnerSkinPackList.Add(skin);
        // 加载打击特效
        foreach (var id in saveData.ownedHitEffectIDs)
            if (_gunHitDict.TryGetValue(id, out var hit) && !CurrentGunHitDataList.Contains(hit))
                CurrentGunHitDataList.Add(hit);
        // 加载子弹包
        foreach (var id in saveData.ownedBulletBundleIDs)
            if (_bulletBundleDict.ContainsKey(id) && !CurrentBulletBundleList.Contains(id))
                CurrentBulletBundleList.Add(id);
        // 加载枪械皮肤
        foreach (var id in saveData.ownedGunSkinIDs)
            if (_gunSkinDict.TryGetValue(id, out var skin) && !CurrentGunSkinPackList.Contains(skin))
                CurrentGunSkinPackList.Add(skin);

        // 加载枪械装备
        foreach (var saveItem in saveData.gunEquipmentSaveDatas)
        {
            foreach (var config in GunEquipmentConfigList)
            {
                if (config.GunName == saveItem.gunName)
                {
                    if (saveItem.equippedSkinID.HasValue && _gunSkinDict.TryGetValue(saveItem.equippedSkinID.Value, out var skin))
                        config.EquippedSkin = skin;
                    break;
                }
            }
        }

        // 自动补全默认皮肤
        AutoSetDefaultGunSkin();
        RestoreEquippedItems(saveData);
    }

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
        AutoEquipFirstItem();
    }
    #endregion

    #region 通用功能
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
            }
        }
    }

    private void RestoreEquippedItems(SkinSaveData saveData)
    {
        if (saveData == null) return;

        if (saveData.equippedSkinID >= 0 && _playerSkinDict.TryGetValue(saveData.equippedSkinID, out var skin) && PlayerOwnerSkinPackList.Contains(skin))
            CurrentPlayerSkinPack = skin;
        if (saveData.equippedHitEffectID >= 0 && _gunHitDict.TryGetValue(saveData.equippedHitEffectID, out var hit) && CurrentGunHitDataList.Contains(hit))
            CurrentOwnerHitObj = hit;
        if (saveData.equippedBulletBundleID >= 0 && _bulletBundleDict.TryGetValue(saveData.equippedBulletBundleID, out var bullet) && CurrentBulletBundleList.Contains(saveData.equippedBulletBundleID))
        {
            _currentEquippedBulletBundleID = saveData.equippedBulletBundleID;
            ApplyBulletBundleConfig(bullet);
        }
    }

    private void AutoEquipFirstItem()
    {
        if (PlayerOwnerSkinPackList.Count > 0) CurrentPlayerSkinPack = PlayerOwnerSkinPackList[0];
        if (CurrentGunHitDataList.Count > 0) CurrentOwnerHitObj = CurrentGunHitDataList[0];
        if (CurrentBulletBundleList.Count > 0)
        {
            _currentEquippedBulletBundleID = CurrentBulletBundleList[0];
            var pack = FindBulletBindPack(_currentEquippedBulletBundleID);
            if (pack != null) ApplyBulletBundleConfig(pack);
        }
    }

    private void ApplyBulletBundleConfig(SpecialBulletBindPack bundlePack)
    {
        if (bundlePack == null) return;
        foreach (var config in GunSkinConfigList)
        {
            if (bundlePack.bulletVisualConfig != null && config.CurrentType == bundlePack.bulletVisualConfig.gunType)
            {
                config.bulletConfig = bundlePack.bulletVisualConfig;
                config.muzzleFlashConfig = bundlePack.muzzleFlashConfig;
            }
        }
    }

    public void SetPlayerSkinPack(PlayerSkinPack SkinPack)
    {
        CurrentPlayerSkinPack = SkinPack; SaveSkinData();
    }

    public void SetPlayerSkinPack(int PackID)
    {
        if (_playerSkinDict.TryGetValue(PackID, out var pack)) SetPlayerSkinPack(pack);
    }

    public void SetCurrentHitEffect(GunHitData hitData)
    {
        CurrentOwnerHitObj = hitData; SaveSkinData();
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
            ApplyBulletBundleConfig(pack);
            SaveSkinData();
        }
    }

    public void EquipBulletBindPack(SpecialBulletBindPack bundlePack)
    {
        if (bundlePack == null || !CurrentBulletBundleList.Contains(bundlePack.BulletBindID)) return;
        _currentEquippedBulletBundleID = bundlePack.BulletBindID;
        ApplyBulletBundleConfig(bundlePack);
        SaveSkinData();
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
      => ReturnGunSkinConfig(Type)?.bulletConfig;

    public MuzzleFlashConfig ReturnMuzzleFlashConfig(GunType Type)
      => ReturnGunSkinConfig(Type)?.muzzleFlashConfig;

    public SpecialBulletBindPack FindBulletBindPack(int bundleID)
    {
        _bulletBundleDict.TryGetValue(bundleID, out var pack); return pack;
    }
    #endregion

    #region GM工具
    [ContextMenu("GM_清空所有皮肤数据")]
    private void GM_ClearAllSkinData()
    {
        if (!Application.isPlaying) { Debug.LogWarning("请在运行时使用"); return; }

        PlayerOwnerSkinPackList.Clear();
        CurrentGunHitDataList.Clear();
        CurrentBulletBundleList.Clear();
        CurrentGunSkinPackList.Clear();
        CurrentPlayerSkinPack = null;
        CurrentOwnerHitObj = null;
        _currentEquippedBulletBundleID = -1;

        foreach (var c in GunEquipmentConfigList) c.EquippedSkin = null;

        if (!IsDeveloperMode) DataEncryptionManger.Instance.DeleteEncryptedComplexData(SkinSaveFileName);
        Debug.Log("已清空皮肤数据");
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
    public BulletVisualConfig bulletConfig;
    public MuzzleFlashConfig muzzleFlashConfig;
}

[System.Serializable]
public class GunEquipmentConfig
{
    public string GunName;
    public GunSkinPack EquippedSkin;
}