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
    [Header("玩家枪械配置")]
    public List<GunSkinConfig> GunSkinConfigList;
    [Header("玩家皮肤数据")]
    public List<PlayerSkinPack> PlayerOwnerSkinPackList;
    public PlayerSkinPack CurrentPlayerSkinPack;
    [Header("玩家打击特效相关")]
    public GunHitData CurrentOwnerHitObj;//玩家当前装备的打击特效
    public List<GunHitData> CurrentGunHitDataList;//玩家拥有的打击特效
    [Header("玩家拥有的数据")]
    public List<int> CurrentBulletBundleList;//玩家拥有的子弹捆绑包 (存ID)

    [Header("角色皮肤数据配置预载管理")]
    public List<PlayerSkinPack> AllPlayerSkinPackList;
    [Header("打击粒子特效数据预载管理")]
    public List<GunHitData> AllGunHitDataList;
    [Header("子弹捆绑包数据预载管理")]
    public List<SpecialBulletBindPack> AllBulletBundleList = new List<SpecialBulletBindPack>();

    // 运行时字典
    private Dictionary<int, SpecialBulletBindPack> _bulletBundleDict;
    private Dictionary<int, PlayerSkinPack> _playerSkinDict;
    private Dictionary<int, GunHitData> _gunHitDict;

    // 单独记录当前装备的子弹包ID
    private int _currentEquippedBulletBundleID = -1;

    protected override void Awake()
    {
        base.Awake();
        InitRuntimeDictionaries();
        // 启动时加载数据
        LoadSkinData();
    }

    #region 初始化与字典
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
        }

        // 初始化玩家皮肤字典
        _playerSkinDict = new Dictionary<int, PlayerSkinPack>();
        foreach (var pack in AllPlayerSkinPackList)
        {
            if (pack == null) continue;
            if (!_playerSkinDict.ContainsKey(pack.PlayerSkinID))
            {
                _playerSkinDict.Add(pack.PlayerSkinID, pack);
            }
        }

        // 初始化打击特效字典
        _gunHitDict = new Dictionary<int, GunHitData>();
        foreach (var data in AllGunHitDataList)
        {
            if (data == null) continue;
            if (!_gunHitDict.ContainsKey(data.HitID))
            {
                _gunHitDict.Add(data.HitID, data);
            }
        }
    }
    #endregion

    #region 数据持久化 

    /// <summary>
    /// 用于加密存储的皮肤数据类
    /// </summary>
    [Serializable]
    private class SkinSaveData
    {
        public List<int> ownedSkinIDs = new List<int>();
        public List<int> ownedHitEffectIDs = new List<int>();
        public List<int> ownedBulletBundleIDs = new List<int>();
        public int equippedSkinID = -1;
        public int equippedHitEffectID = -1;
        public int equippedBulletBundleID = -1;
    }

    /// <summary>
    /// 保存玩家皮肤数据到本地 (加密)
    /// </summary>
    public void SaveSkinData()
    {
        if (!Application.isPlaying) return;

        if (IsDeveloperMode)
        {
            Debug.LogWarning("[开发者模式] 已拦截保存操作，不会修改本地存档");
            return;
        }

        SkinSaveData saveData = new SkinSaveData();

        // 保存拥有的皮肤ID
        foreach (var skin in PlayerOwnerSkinPackList)
        {
            if (skin != null) saveData.ownedSkinIDs.Add(skin.PlayerSkinID);
        }

        // 保存拥有的打击特效ID
        foreach (var hit in CurrentGunHitDataList)
        {
            if (hit != null) saveData.ownedHitEffectIDs.Add(hit.HitID);
        }

        // 保存拥有的子弹包ID
        saveData.ownedBulletBundleIDs = new List<int>(CurrentBulletBundleList);

        // 保存当前装备的ID
        saveData.equippedSkinID = CurrentPlayerSkinPack != null ? CurrentPlayerSkinPack.PlayerSkinID : -1;
        saveData.equippedHitEffectID = CurrentOwnerHitObj != null ? CurrentOwnerHitObj.HitID : -1;
        saveData.equippedBulletBundleID = _currentEquippedBulletBundleID;

        // 加密保存
        DataEncryptionManger.Instance.SaveEncryptedComplexData(SkinSaveFileName, saveData);
    }

    /// <summary>
    /// 加载玩家皮肤数据
    /// 【开发者模式】直接解锁所有物品，跳过存档
    /// </summary>
    public void LoadSkinData()
    {
        if (!Application.isPlaying) return;

        // 重置状态
        PlayerOwnerSkinPackList.Clear();
        CurrentGunHitDataList.Clear();
        CurrentBulletBundleList.Clear();
        CurrentPlayerSkinPack = null;
        CurrentOwnerHitObj = null;
        _currentEquippedBulletBundleID = -1;

        if (IsDeveloperMode)
        {
            Debug.LogWarning("========================================");
            Debug.LogWarning("[开发者模式] 已激活！正在解锁所有物品...");

            // 解锁所有皮肤
            foreach (var skin in AllPlayerSkinPackList)
            {
                if (skin != null && !PlayerOwnerSkinPackList.Contains(skin))
                {
                    PlayerOwnerSkinPackList.Add(skin);
                }
            }
            // 解锁所有打击特效
            foreach (var hit in AllGunHitDataList)
            {
                if (hit != null && !CurrentGunHitDataList.Contains(hit))
                {
                    CurrentGunHitDataList.Add(hit);
                }
            }
            // 解锁所有子弹包
            foreach (var pack in AllBulletBundleList)
            {
                if (pack != null && !CurrentBulletBundleList.Contains(pack.BulletBindID))
                {
                    CurrentBulletBundleList.Add(pack.BulletBindID);
                }
            }

            // 自动装备第一个物品（可选，方便测试）
            if (PlayerOwnerSkinPackList.Count > 0) CurrentPlayerSkinPack = PlayerOwnerSkinPackList[0];
            if (CurrentGunHitDataList.Count > 0) CurrentOwnerHitObj = CurrentGunHitDataList[0];
            if (CurrentBulletBundleList.Count > 0)
            {
                _currentEquippedBulletBundleID = CurrentBulletBundleList[0];
                var firstPack = FindBulletBindPack(_currentEquippedBulletBundleID);
                if (firstPack != null) ApplyBulletBundleConfig(firstPack);
            }

            Debug.LogWarning($"[开发者模式] 解锁完成！皮肤:{PlayerOwnerSkinPackList.Count} 特效:{CurrentGunHitDataList.Count} 子弹包:{CurrentBulletBundleList.Count}");
            Debug.LogWarning("========================================");
            return;
        }

        // 【正常模式】尝试加载存档
        var saveData = DataEncryptionManger.Instance.LoadEncryptedComplexData<SkinSaveData>(SkinSaveFileName);

        if (saveData != null)
        {
            // 从存档恢复基础数据
            foreach (var id in saveData.ownedSkinIDs)
            {
                if (_playerSkinDict.TryGetValue(id, out var skin) && !PlayerOwnerSkinPackList.Contains(skin))
                {
                    PlayerOwnerSkinPackList.Add(skin);
                }
            }
            foreach (var id in saveData.ownedHitEffectIDs)
            {
                if (_gunHitDict.TryGetValue(id, out var hit) && !CurrentGunHitDataList.Contains(hit))
                {
                    CurrentGunHitDataList.Add(hit);
                }
            }
            foreach (var id in saveData.ownedBulletBundleIDs)
            {
                if (_bulletBundleDict.ContainsKey(id) && !CurrentBulletBundleList.Contains(id))
                {
                    CurrentBulletBundleList.Add(id);
                }
            }
        }

        // 以GoodDataManager为唯一权威进行数据校准
        CalibrateDataWithGoodsManager();

        // 独立恢复各自的装备状态
        RestoreEquippedItems(saveData);

        // 保存校准后的数据
        SaveSkinData();

        Debug.Log("[GameSkinManager] 玩家皮肤数据加载完成 (正常模式)");
    }

    #endregion

    #region 数据校准
    /// <summary>
    /// 以GoodDataManager的已购买列表为唯一权威，补全合法数据
    /// </summary>
    private void CalibrateDataWithGoodsManager()
    {
        // 开发者模式跳过校准
        if (IsDeveloperMode) return;

        if (GoodDataManager.Instance == null)
        {
            return;
        }

        var authorityPurchasedList = GoodDataManager.Instance.UserObtainGoodsList;
        authorityPurchasedList ??= new List<GoodsData>();

        foreach (var goods in authorityPurchasedList)
        {
            if (goods == null || string.IsNullOrEmpty(goods.goodsGuid)) continue;

            switch (goods.skinType)
            {
                case SkinType.PlayerCharacter:
                    if (goods.playerSkinPack != null && !PlayerOwnerSkinPackList.Contains(goods.playerSkinPack))
                    {
                        PlayerOwnerSkinPackList.Add(goods.playerSkinPack);
                    }
                    break;

                case SkinType.GunHitEffect:
                    if (goods.gunHitData != null && !CurrentGunHitDataList.Contains(goods.gunHitData))
                    {
                        CurrentGunHitDataList.Add(goods.gunHitData);
                    }
                    break;

                case SkinType.SpecialBullet:
                    if (goods.bulletPack != null && !CurrentBulletBundleList.Contains(goods.bulletPack.BulletBindID))
                    {
                        CurrentBulletBundleList.Add(goods.bulletPack.BulletBindID);
                    }
                    break;
            }
        }
    }

    #endregion

    #region 装备恢复
    /// <summary>
    /// 独立恢复各自的装备状态，互不干扰
    /// </summary>
    private void RestoreEquippedItems(SkinSaveData saveData)
    {
        CurrentPlayerSkinPack = null;
        CurrentOwnerHitObj = null;
        _currentEquippedBulletBundleID = -1;

        if (saveData == null) return;

        // 独立恢复装备皮肤
        if (saveData.equippedSkinID >= 0
            && _playerSkinDict.TryGetValue(saveData.equippedSkinID, out var skinPack)
            && PlayerOwnerSkinPackList.Contains(skinPack))
        {
            CurrentPlayerSkinPack = skinPack;
        }

        // 独立恢复装备打击特效
        if (saveData.equippedHitEffectID >= 0
            && _gunHitDict.TryGetValue(saveData.equippedHitEffectID, out var hitData)
            && CurrentGunHitDataList.Contains(hitData))
        {
            CurrentOwnerHitObj = hitData;
        }

        // 独立恢复装备子弹包 (并重新应用配置)
        if (saveData.equippedBulletBundleID >= 0
            && _bulletBundleDict.TryGetValue(saveData.equippedBulletBundleID, out var bulletPack)
            && CurrentBulletBundleList.Contains(saveData.equippedBulletBundleID))
        {
            _currentEquippedBulletBundleID = saveData.equippedBulletBundleID;
            ApplyBulletBundleConfig(bulletPack);
        }
    }
    #endregion

    #region 公共功能

    // ====================== 玩家皮肤相关 ======================
    public void SetPlayerSkinPack(PlayerSkinPack SkinPack)
    {
        CurrentPlayerSkinPack = SkinPack;
        SaveSkinData();
    }

    public void SetPlayerSkinPack(int PackID)
    {
        if (_playerSkinDict.TryGetValue(PackID, out var pack))
        {
            SetPlayerSkinPack(pack);
        }
    }

    // ====================== 打击特效相关 ======================
    public void SetCurrentHitEffect(GunHitData hitData)
    {
        CurrentOwnerHitObj = hitData;
        SaveSkinData();
    }

    public void SetCurrentHitEffect(int hitID)
    {
        if (_gunHitDict.TryGetValue(hitID, out var hitData))
        {
            SetCurrentHitEffect(hitData);
        }
    }

    // ====================== 子弹捆绑包相关 ======================
    public void EquipBulletBindPack(int bundleID)
    {
        if (bundleID < 0) return;
        if (!CurrentBulletBundleList.Contains(bundleID)) return;

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
        if (bundlePack == null) return;
        if (!CurrentBulletBundleList.Contains(bundlePack.BulletBindID)) return;

        _currentEquippedBulletBundleID = bundlePack.BulletBindID;
        ApplyBulletBundleConfig(bundlePack);
        SaveSkinData();
    }

    /// <summary>
    /// 【内部】仅应用子弹包配置到枪械
    /// </summary>
    private void ApplyBulletBundleConfig(SpecialBulletBindPack bundlePack)
    {
        if (bundlePack == null) return;

        foreach (var config in GunSkinConfigList)
        {
            if (bundlePack.bulletVisualConfig != null &&
                config.CurrentType == bundlePack.bulletVisualConfig.gunType)
            {
                config.bulletConfig = bundlePack.bulletVisualConfig;
                config.muzzleFlashConfig = bundlePack.muzzleFlashConfig;
            }
        }
    }

    // ====================== 查询相关 ======================
    public List<SpecialBulletBindPack> GetSpecialBulletBindPackList(GunType Type)
    {
        List<SpecialBulletBindPack> resultList = new List<SpecialBulletBindPack>();
        foreach (var ownedID in CurrentBulletBundleList)
        {
            if (_bulletBundleDict.TryGetValue(ownedID, out var pack) && pack.gunType == Type)
            {
                resultList.Add(pack);
            }
        }
        return resultList;
    }

    public GunHitData GetHitData(int ID)
    {
        _gunHitDict.TryGetValue(ID, out var data);
        return data;
    }

    public PlayerSkinPack GetPlayerSkipPack(int PackID)
    {
        _playerSkinDict.TryGetValue(PackID, out var pack);
        return pack;
    }

    public GunSkinConfig ReturnGunSkinConfig(GunType Type)
    {
        return GunSkinConfigList.FirstOrDefault(config => config.CurrentType == Type);
    }

    public BulletVisualConfig ReturnBulletVisualConfig(GunType Type)
      => ReturnGunSkinConfig(Type)?.bulletConfig;

    public MuzzleFlashConfig ReturnMuzzleFlashConfig(GunType Type)
      => ReturnGunSkinConfig(Type)?.muzzleFlashConfig;

    public SpecialBulletBindPack FindBulletBindPack(int bundleID)
    {
        _bulletBundleDict.TryGetValue(bundleID, out var pack);
        return pack;
    }

    #endregion

    #region GM调试方法
    [ContextMenu("GM_清空所有皮肤数据")]
    private void GM_ClearAllSkinData()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在Play模式下执行此操作");
            return;
        }

        // 开发者模式下也允许清空（仅清空内存）
        PlayerOwnerSkinPackList.Clear();
        CurrentGunHitDataList.Clear();
        CurrentBulletBundleList.Clear();
        CurrentPlayerSkinPack = null;
        CurrentOwnerHitObj = null;
        _currentEquippedBulletBundleID = -1;

        // 清空枪械配置
        foreach (var config in GunSkinConfigList)
        {
            config.bulletConfig = null;
            config.muzzleFlashConfig = null;
        }

        // 非开发者模式才删除文件
        if (!IsDeveloperMode)
        {
            DataEncryptionManger.Instance.DeleteEncryptedComplexData(SkinSaveFileName);
            SaveSkinData();
        }

        Debug.Log("[GM] 已清空皮肤数据 (内存)");
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