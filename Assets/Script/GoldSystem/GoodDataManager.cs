using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 保留 Inspector 面板配置所需要的类
[Serializable]
public class DiscountLevel
{
    [Tooltip("折扣率（0.9=9折，0.5=5折）")]
    public float discountRate;
    [Tooltip("抽取权重，数值越大概率越高")]
    public int weight;
}

public class GoodDataManager : SingleMonoAutoBehavior<GoodDataManager>
{
    [Header("商品总库与玩家拥有")]
    public List<GoodsData> AllGoodsDataList;
    public List<GoodsData> UserObtainGoodsList;

    [Header("商店信息关联")]
    public int EverydayRefreshGoodsAmount = 6;
    public List<GoodsData> ToDayRefreshGoodsList = new List<GoodsData>();

    [Header("===== 每日商店配置 =====")]
    [Tooltip("自动刷新日期：存储最后一次刷新的日期")]
    [SerializeField] private string lastRefreshDate;
    [Tooltip("皮肤类型抽取权重")]
    public SkinTypeWeight[] skinTypeWeights;
    [Tooltip("商品品质抽取权重")]
    public GoodsQualityWeight[] qualityWeights;

    [Header("===== 每日刷新次数配置 =====")]
    [Tooltip("每日最大手动刷新次数")]
    public int maxDailyRefreshCount = 3;
    [Tooltip("今日已使用的刷新次数")]
    [SerializeField] private int todayUsedRefreshCount = 0;

    [Header("===== 折扣概率配置 =====")]
    [Tooltip("商品触发打折的总概率")]
    public float discountChance = 0.5f;
    [Tooltip("折扣档位配置：权重越高，抽到该折扣的概率越大")]
    public DiscountLevel[] discountLevels = new DiscountLevel[]
    {
        new DiscountLevel(){ discountRate = 0.9f, weight = 50 },
        new DiscountLevel(){ discountRate = 0.8f, weight = 30 },
        new DiscountLevel(){ discountRate = 0.7f, weight = 10 },
        new DiscountLevel(){ discountRate = 0.6f, weight = 7 },
        new DiscountLevel(){ discountRate = 0.5f, weight = 3 },
    };

    // 内存中的折扣字典
    private Dictionary<string, float> currentGoodsDiscountDict = new Dictionary<string, float>();
    private readonly Dictionary<string, GoodsData> _goodsByGuid = new Dictionary<string, GoodsData>();
    private readonly HashSet<string> _ownedGoodsGuidSet = new HashSet<string>();
    private readonly Dictionary<SkinType, int> _skinTypeWeightDict = new Dictionary<SkinType, int>();
    private readonly Dictionary<GoodsQuality, int> _qualityWeightDict = new Dictionary<GoodsQuality, int>();

    [Header("显示图片")]
    public RenderTexture displayRenderTexture;

    [Header("默认初始商品")]
    [Tooltip("用户默认拥有的商品，每次启动游戏都会验证并补全，防止被误删")]
    public List<GoodsData> DefaultInitGoodsData = new List<GoodsData>();

    [Header("今日是否给与过每日奖励")]
    public bool hasGivenDailyReward = false;

    private const string PREF_OWNED_GOODS = "PlayerOwnedGoods_Guids";
    private const string PREF_DAILY_DATE = "DailyShop_LastDate";
    private const string PREF_DAILY_REWARD = "DailyShop_HasReward";
    private const string PREF_DAILY_REFRESH = "DailyShop_RefreshCount";
    private const string PREF_DAILY_GOODS = "DailyShop_GoodsList";
    private const string PREF_DAILY_DISCOUNTS = "DailyShop_Discounts";

    #region 权重配置类
    [Serializable]
    public class SkinTypeWeight
    {
        public SkinType skinType;
        public int weight = 10;
    }
    [Serializable]
    public class GoodsQualityWeight
    {
        public GoodsQuality quality;
        public int weight = 10;
    }
    #endregion
    protected override void Awake()
    {
        base.Awake();
        CompleteGoodsDataListFromResources();
        RebuildGoodsLookupCache();
        RebuildWeightCaches();
        ValidateAllGoodsData();
    }

    private void CompleteGoodsDataListFromResources()
    {
        GoodsData[] resourceGoods = Resources.LoadAll<GoodsData>("GameInfo/GoodInfo");
        if (resourceGoods == null || resourceGoods.Length == 0)
            return;

        if (AllGoodsDataList == null)
            AllGoodsDataList = new List<GoodsData>();

        foreach (GoodsData goods in resourceGoods)
        {
            if (goods != null && !AllGoodsDataList.Contains(goods))
                AllGoodsDataList.Add(goods);
        }
    }

    private void Start()
    {
        //  先加载玩家已拥有的永久商品
        LoadPlayerGood();
        //  执行严格的每日状态校验
        CheckAndInitDailyState();
    }

    #region 
    /// <summary>
    /// 唯一入口：检查并初始化每日状态
    /// </summary>
    public void CheckAndInitDailyState()
    {
        if (!Application.isPlaying) 
            return;

        string todayString = DateTime.Now.Date.ToString("yyyyMMdd");
        string savedDate = PlayerPrefs.GetString(PREF_DAILY_DATE, "");

        if (!string.IsNullOrEmpty(savedDate) && savedDate == todayString)
        {
            /* Debug.Log($"[系统校验] 校验通过，读取今日({todayString})数据..."); */
            RestoreStateFromPrefs();

            if (ToDayRefreshGoodsList.Count == 0)
            {
                GenerateShopGoodsListAndDiscount();
                SaveDailyStateToDisk();
            }
        }
        else
        {
            /* Debug.Log($"[系统校验] 检测到新日期或无存档(旧:{savedDate} 新:{todayString})，强制重置所有每日状态！"); */

            lastRefreshDate = todayString;
            todayUsedRefreshCount = 0;
            hasGivenDailyReward = false; // 核心：新的一天绝对重置为未领取！

            GenerateShopGoodsListAndDiscount();

            SaveDailyStateToDisk();
        }
    }

    /// <summary>
    /// 从 PlayerPrefs 恢复数据
    /// </summary>
    private void RestoreStateFromPrefs()
    {
        lastRefreshDate = PlayerPrefs.GetString(PREF_DAILY_DATE, "");
        hasGivenDailyReward = PlayerPrefs.GetInt(PREF_DAILY_REWARD, 0) == 1; // 1代表true，0代表false
        todayUsedRefreshCount = PlayerPrefs.GetInt(PREF_DAILY_REFRESH, 0);

        // 恢复商品列表 (格式: guid1|guid2|guid3)
        ToDayRefreshGoodsList.Clear();
        string goodsStr = PlayerPrefs.GetString(PREF_DAILY_GOODS, "");
        if (!string.IsNullOrEmpty(goodsStr))
        {
            string[] guids = goodsStr.Split('|');
            foreach (var guid in guids)
            {
                var goods = TryGetGoodsByGuid(guid);
                if (goods != null) ToDayRefreshGoodsList.Add(goods);
            }
        }

        // 恢复折扣 (格式: guid1:0.9|guid2:0.8)
        currentGoodsDiscountDict.Clear();
        string discountStr = PlayerPrefs.GetString(PREF_DAILY_DISCOUNTS, "");
        if (!string.IsNullOrEmpty(discountStr))
        {
            string[] pairs = discountStr.Split('|');
            foreach (var pair in pairs)
            {
                string[] kv = pair.Split(':');
                if (kv.Length == 2 && float.TryParse(kv[1], out float val))
                {
                    currentGoodsDiscountDict[kv[0]] = val;
                }
            }
        }

        /* Debug.Log($"[读取完毕] 奖励状态：{(hasGivenDailyReward ? "已领取" : "未领取")}"); */
    }

    /// <summary>
    /// 将当前内存中的所有每日状态保存到 PlayerPrefs
    /// </summary>
    private void SaveDailyStateToDisk()
    {
        if (!Application.isPlaying) return;

        PlayerPrefs.SetString(PREF_DAILY_DATE, lastRefreshDate);
        PlayerPrefs.SetInt(PREF_DAILY_REWARD, hasGivenDailyReward ? 1 : 0); // 保存奖励状态
        PlayerPrefs.SetInt(PREF_DAILY_REFRESH, todayUsedRefreshCount);

        // 拼接商品列表
        string goodsStr = string.Join("|", ToDayRefreshGoodsList.Select(g => g.goodsGuid));
        PlayerPrefs.SetString(PREF_DAILY_GOODS, goodsStr);

        // 拼接折扣字典
        List<string> discList = new List<string>();
        foreach (var kvp in currentGoodsDiscountDict)
        {
            discList.Add($"{kvp.Key}:{kvp.Value}");
        }
        PlayerPrefs.SetString(PREF_DAILY_DISCOUNTS, string.Join("|", discList));

        PlayerPrefs.Save(); // 强制立即写入硬盘
    }
    #endregion

    #region 每日奖励公共方法
    public bool CanReceiveDailyReward()
    {
        return !hasGivenDailyReward;
    }

    public void MarkDailyRewardGiven()
    {
        if (hasGivenDailyReward)
        {
            /* Debug.LogWarning("[每日奖励] 今日已领取过奖励，无法重复领取！"); */
            return;
        }

        hasGivenDailyReward = true;
        SaveDailyStateToDisk(); // 标记为 true 后立即落盘
        /* Debug.Log("[每日奖励] 奖励领取成功！状态已永久写入 PlayerPrefs。"); */
    }
    #endregion

    #region 商店刷新与商品生成逻辑
    public bool IsReachUpperLimit()
    {
        return todayUsedRefreshCount >= maxDailyRefreshCount;
    }

    public int GetRemainingRefreshCount()
    {
        return maxDailyRefreshCount - todayUsedRefreshCount;
    }

    /// <summary>
    /// 玩家手动点击刷新商店
    /// </summary>
    public void RefRefreshToDay()
    {
        if (IsReachUpperLimit())
        {
            /* Debug.LogWarning("[商店] 今日刷新次数已用完！"); */
            return;
        }

        // 仅刷新商品，不修改日期和奖励状态
        GenerateShopGoodsListAndDiscount();
        todayUsedRefreshCount++;
        SaveDailyStateToDisk();
        /* Debug.Log($"[商店] 刷新成功！今日已用刷新次数: {todayUsedRefreshCount}/{maxDailyRefreshCount}"); */
    }

    /// <summary>
    /// 仅负责从总库中抽取商品并生成折扣
    /// </summary>
    private void GenerateShopGoodsListAndDiscount()
    {
        ToDayRefreshGoodsList.Clear();
        currentGoodsDiscountDict.Clear();

        if (AllGoodsDataList == null || AllGoodsDataList.Count == 0)
            return;

        List<GoodsData> availableGoods = AllGoodsDataList
            .Where(goods => goods != null && !string.IsNullOrEmpty(goods.goodsGuid) && !_ownedGoodsGuidSet.Contains(goods.goodsGuid))
            .ToList();

        if (availableGoods.Count == 0) return;

        TryAddGuaranteedShopGoods(SkinType.GunAppearance, availableGoods);
        TryAddGuaranteedShopGoods(SkinType.PlayerCharacter, availableGoods);

        while (ToDayRefreshGoodsList.Count < EverydayRefreshGoodsAmount && availableGoods.Count > 0)
        {
            GoodsData selectedGoods = GetWeightedRandomGoods(availableGoods);
            if (selectedGoods != null)
            {
                ToDayRefreshGoodsList.Add(selectedGoods);
                availableGoods.Remove(selectedGoods);
                GenerateDiscountForSingleGoods(selectedGoods.goodsGuid);
            }
        }
    }

    private void TryAddGuaranteedShopGoods(SkinType targetType, List<GoodsData> availableGoods)
    {
        if (ToDayRefreshGoodsList.Count >= EverydayRefreshGoodsAmount || availableGoods == null || availableGoods.Count == 0)
            return;

        List<GoodsData> targetPool = availableGoods
            .Where(goods => goods.skinType == targetType)
            .ToList();

        if (targetPool.Count == 0)
            return;

        GoodsData selectedGoods = GetWeightedRandomGoods(targetPool);
        if (selectedGoods == null)
            return;

        ToDayRefreshGoodsList.Add(selectedGoods);
        availableGoods.Remove(selectedGoods);
        GenerateDiscountForSingleGoods(selectedGoods.goodsGuid);
    }

    private void GenerateDiscountForSingleGoods(string guid)
    {
        if (UnityEngine.Random.value <= discountChance)
        {
            int totalWeight = discountLevels.Sum(level => level.weight);
            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var level in discountLevels)
            {
                currentWeight += level.weight;
                if (randomValue < currentWeight)
                {
                    currentGoodsDiscountDict[guid] = level.discountRate;
                    return;
                }
            }
            currentGoodsDiscountDict[guid] = 0.9f;
        }
        else
        {
            currentGoodsDiscountDict[guid] = 1.0f;
        }
    }

    private GoodsData GetWeightedRandomGoods(List<GoodsData> pool)
    {
        int totalWeight = 0;
        List<int> weightList = new List<int>();

        foreach (var goods in pool)
        {
            int typeW = _skinTypeWeightDict.TryGetValue(goods.skinType, out int cachedTypeWeight) ? cachedTypeWeight : 5;
            int qualityW = _qualityWeightDict.TryGetValue(goods.quality, out int cachedQualityWeight) ? cachedQualityWeight : 5;
            int finalW = typeW * qualityW;

            weightList.Add(finalW);
            totalWeight += finalW;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight + 1);
        int current = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            current += weightList[i];
            if (randomValue <= current) return pool[i];
        }

        return pool.FirstOrDefault();
    }
    #endregion

    #region 获取折扣与价格
    public float GetGoodsDiscount(string guid)
    {
        if (currentGoodsDiscountDict.TryGetValue(guid, out float discount)) return discount;
        return 1.0f;
    }

    public int GetGoodsDiscountedPrice(GoodsData goods)
    {
        float discount = GetGoodsDiscount(goods.goodsGuid);
        return Mathf.FloorToInt(goods.goodsPrice * discount);
    }
    #endregion

    #region 玩家拥有商品管理
    public void PurchaseGoodToUser(GoodsData Data)
    {
        if (Data == null || string.IsNullOrEmpty(Data.goodsGuid))
            return;

        if (_goodsByGuid.ContainsKey(Data.goodsGuid))
        {
            if (_ownedGoodsGuidSet.Contains(Data.goodsGuid))
                return;

            int finalPrice = GetGoodsDiscountedPrice(Data);

            if (GoldSystem.Instance.GetGold() >= finalPrice)
            {
                GoldSystem.Instance.CostGold(finalPrice, $"购买商品: {Data.goodsName}");
                UserObtainGoodsList.Add(Data);
                _ownedGoodsGuidSet.Add(Data.goodsGuid);
                SavePlayerGood();
                LoadGoodsDataIntoSkinManager(Data);
            }
            else
            {
                /* Debug.LogWarning("金币不足！"); */
            }
        }
    }

    public void LoadGoodsDataIntoSkinManager(GoodsData Data)
    {
        if (GameSkinManager.Instance == null) return;

        switch (Data.skinType)
        {
            case SkinType.PlayerCharacter:
                if (Data.playerSkinPack != null && !GameSkinManager.Instance.PlayerOwnerSkinPackList.Contains(Data.playerSkinPack))
                    GameSkinManager.Instance.PlayerOwnerSkinPackList.Add(Data.playerSkinPack);
                break;
            case SkinType.GunHitEffect:
                if (Data.gunHitData != null && !GameSkinManager.Instance.CurrentGunHitDataList.Contains(Data.gunHitData))
                    GameSkinManager.Instance.CurrentGunHitDataList.Add(Data.gunHitData);
                break;
            case SkinType.SpecialBullet:
                if (Data.bulletPack != null && !GameSkinManager.Instance.CurrentBulletBundleList.Contains(Data.bulletPack.BulletBindID))
                    GameSkinManager.Instance.CurrentBulletBundleList.Add(Data.bulletPack.BulletBindID);
                break;
            case SkinType.GunAppearance:
                GameSkinManager.Instance.AddGunSkinPack(Data.gunSkinPack);
                break;
            case SkinType.Expression:
                if (Data.expressionPacks != null && Data.expressionPacks.Count > 0)
                {
                    foreach (var pack in Data.expressionPacks)
                        ExpressionSystem.Instance.PlayerOwnExpressionIDList.Add(pack.ExpressionID);
                }
                break;
        }
    }

    public bool JudgeUserHasGood(GoodsData Data) => Data != null && !string.IsNullOrEmpty(Data.goodsGuid) && _ownedGoodsGuidSet.Contains(Data.goodsGuid);

    public void SavePlayerGood()
    {
        if (!Application.isPlaying) return;

        RebuildOwnedGoodsCache();

        // 改用 PlayerPrefs 存储已拥有商品
        string goodsStr = string.Join("|", UserObtainGoodsList.Select(g => g.goodsGuid));
        PlayerPrefs.SetString(PREF_OWNED_GOODS, goodsStr);
        PlayerPrefs.Save();
    }

    public void LoadPlayerGood()
    {
        if (!Application.isPlaying) return;

        UserObtainGoodsList = new List<GoodsData>();

        string goodsStr = PlayerPrefs.GetString(PREF_OWNED_GOODS, "");
        if (!string.IsNullOrEmpty(goodsStr))
        {
            string[] guids = goodsStr.Split('|');
            foreach (string goodID in guids)
            {
                GoodsData data = TryGetGoodsByGuid(goodID);
                if (data != null) UserObtainGoodsList.Add(data);
            }
        }

        ValidateAndCompleteDefaultGoods();
        RebuildOwnedGoodsCache();
        SyncAllOwnedGoodsToSkinManager();
        SavePlayerGood(); // 重新保存确保存档干净
    }

    private void ValidateAndCompleteDefaultGoods()
    {
        if (DefaultInitGoodsData == null || DefaultInitGoodsData.Count == 0) return;

        foreach (var defaultGoods in DefaultInitGoodsData)
        {
            if (defaultGoods == null || string.IsNullOrEmpty(defaultGoods.goodsGuid)) continue;

            if (!_ownedGoodsGuidSet.Contains(defaultGoods.goodsGuid))
            {
                var goodsInAll = TryGetGoodsByGuid(defaultGoods.goodsGuid);
                if (goodsInAll != null)
                {
                    UserObtainGoodsList.Add(goodsInAll);
                    _ownedGoodsGuidSet.Add(goodsInAll.goodsGuid);
                }
            }
        }
    }

    private void SyncAllOwnedGoodsToSkinManager()
    {
        if (GameSkinManager.Instance == null) return;

        foreach (var goods in UserObtainGoodsList)
        {
            if (goods == null) continue;
            LoadGoodsDataIntoSkinManager(goods);
        }
        GameSkinManager.Instance.ReturnLastGameEquipment();
        ExpressionSystem.Instance.SystemInit();
    }

    public void ClearLocalData()
    {
        if (!Application.isPlaying) return;

        UserObtainGoodsList.Clear();
        _ownedGoodsGuidSet.Clear();

        // 删除所有 PlayerPrefs 对应的 Key
        PlayerPrefs.DeleteKey(PREF_OWNED_GOODS);
        PlayerPrefs.DeleteKey(PREF_DAILY_DATE);
        PlayerPrefs.DeleteKey(PREF_DAILY_REWARD);
        PlayerPrefs.DeleteKey(PREF_DAILY_REFRESH);
        PlayerPrefs.DeleteKey(PREF_DAILY_GOODS);
        PlayerPrefs.DeleteKey(PREF_DAILY_DISCOUNTS);
        PlayerPrefs.Save();

        LoadPlayerGood();
        CheckAndInitDailyState(); // 清空后彻底重置每日状态
        /* Debug.Log("玩家商品数据 & 商店数据已全部清空！"); */
    }
    #endregion


    private void RebuildGoodsLookupCache()
    {
        _goodsByGuid.Clear();

        if (AllGoodsDataList == null)
            return;

        foreach (var goods in AllGoodsDataList)
        {
            if (goods == null || string.IsNullOrEmpty(goods.goodsGuid))
                continue;

            if (_goodsByGuid.ContainsKey(goods.goodsGuid))
            {
                /* Debug.LogWarning($"[GoodDataManager] 检测到重复商品 Guid: {goods.goodsGuid}", goods); */
                continue;
            }

            _goodsByGuid.Add(goods.goodsGuid, goods);
        }
    }

    private void RebuildOwnedGoodsCache()
    {
        _ownedGoodsGuidSet.Clear();

        if (UserObtainGoodsList == null)
            return;

        UserObtainGoodsList = UserObtainGoodsList
            .Where(goods => goods != null && !string.IsNullOrEmpty(goods.goodsGuid))
            .Distinct()
            .ToList();

        foreach (var goods in UserObtainGoodsList)
        {
            _ownedGoodsGuidSet.Add(goods.goodsGuid);
        }
    }

    private void RebuildWeightCaches()
    {
        _skinTypeWeightDict.Clear();
        _qualityWeightDict.Clear();

        if (skinTypeWeights != null)
        {
            foreach (var weight in skinTypeWeights)
            {
                _skinTypeWeightDict[weight.skinType] = weight.weight;
            }
        }

        if (qualityWeights != null)
        {
            foreach (var weight in qualityWeights)
            {
                _qualityWeightDict[weight.quality] = weight.weight;
            }
        }
    }

    private GoodsData TryGetGoodsByGuid(string goodsGuid)
    {
        if (string.IsNullOrEmpty(goodsGuid))
            return null;

        return _goodsByGuid.TryGetValue(goodsGuid, out GoodsData goods) ? goods : null;
    }

    private void ValidateAllGoodsData()
    {
        if (AllGoodsDataList == null)
            return;

        foreach (var goods in AllGoodsDataList)
        {
            if (goods == null)
                continue;

            if (goods.ValidateData(out string errorMessage))
                continue;

            /* Debug.LogWarning($"[GoodDataManager] 商品配置异常: {errorMessage}", goods); */
        }
    }
    #region GM 测试指令
    // [ContextMenu("GM_清空本地所有数据")]
    private void GM_ClearLocalData() => ClearLocalData();

    // [ContextMenu("GM_手动刷新今日商店")]
    private void GM_RefreshDailyShop()
    {
        if (!Application.isPlaying) return;
        RefRefreshToDay();
    }

    // [ContextMenu("GM_重置今日刷新次数")]
    private void GM_ResetRefreshCount()
    {
        if (!Application.isPlaying) return;
        todayUsedRefreshCount = 0;
        SaveDailyStateToDisk();
        /* Debug.Log("[GM] 今日刷新次数已重置为0"); */
    }

    // [ContextMenu("GM_重置每日奖励状态为未领取")]
    private void GM_ResetDailyReward()
    {
        if (!Application.isPlaying) return;
        hasGivenDailyReward = false;
        SaveDailyStateToDisk();
        /* Debug.Log("[GM] 每日奖励状态已重置为可领取"); */
    }
    #endregion
}