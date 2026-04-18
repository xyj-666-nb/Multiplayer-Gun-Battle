using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoodDataManager : SingleMonoAutoBehavior<GoodDataManager>
{
    public List<GoodsData> AllGoodsDataList;
    public List<GoodsData> UserObtainGoodsList;
    private List<string> UserObtainGoodIDsList;
    private string PlayerGoodsDataFileName = "PlayerGoodsData";

    [Header("商店信息关联")]
    public int EverydayRefreshGoodsAmount = 6;
    public List<GoodsData> ToDayRefreshGoodsList = new List<GoodsData>();

    [Header("===== 每日商店配置 =====")]
    [Tooltip("自动刷新日期：存储最后一次刷新的日期")]
    [SerializeField] private string lastRefreshDate;
    [Tooltip("每日商店本地存档文件名")]
    [SerializeField] private string dailyShopSaveName = "DailyShopData";
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
        new DiscountLevel(){ discountRate = 0.9f, weight = 50 }, // 9折，权重最高，概率最大
        new DiscountLevel(){ discountRate = 0.8f, weight = 30 }, // 8折，次高概率
        new DiscountLevel(){ discountRate = 0.7f, weight = 10 }, // 7折，中等概率
        new DiscountLevel(){ discountRate = 0.6f, weight = 7 },  // 6折，低概率
        new DiscountLevel(){ discountRate = 0.5f, weight = 3 },  // 5折，最低概率
    };

    private Dictionary<string, float> currentGoodsDiscountDict = new Dictionary<string, float>();

    [Header("显示图片")]
    public RenderTexture displayRenderTexture;

    [Header("默认初始商品")]
    [Tooltip("用户默认拥有的商品，每次启动游戏都会验证并补全，防止被误删")]
    public List<GoodsData> DefaultInitGoodsData = new List<GoodsData>();

    [Header("今日是否给与过每日奖励")]
    public bool hasGivenDailyReward = false;

    [Serializable]
    private class DailyShopSaveData
    {
        public string date;
        public List<string> goodsGuids = new List<string>();
        public int dailyRefreshCount;
        // 存储折扣数据
        public List<DiscountData> discounts = new List<DiscountData>();
        // 存储今日是否已领取每日奖励
        public bool hasGivenDailyReward;
    }

    [Serializable]
    private class DiscountData
    {
        public string goodsGuid;
        public float discount;
    }

    /// <summary>
    /// 折扣档位配置类
    /// </summary>
    [Serializable]
    public class DiscountLevel
    {
        [Tooltip("折扣率（0.9=9折，0.5=5折）")]
        public float discountRate;
        [Tooltip("抽取权重，数值越大概率越高")]
        public int weight;
    }

    public bool IsReachUpperLimit()
    {
        return todayUsedRefreshCount >= maxDailyRefreshCount;
    }

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

    #region 折扣公共方法
    /// <summary>
    /// 获取某商品的当前折扣
    /// </summary>
    public float GetGoodsDiscount(string guid)
    {
        if (currentGoodsDiscountDict.TryGetValue(guid, out float discount))
        {
            return discount;
        }
        return 1.0f; // 默认不打折
    }

    /// <summary>
    /// 获取某商品的折后价 
    /// </summary>
    public int GetGoodsDiscountedPrice(GoodsData goods)
    {
        float discount = GetGoodsDiscount(goods.goodsGuid);
        return Mathf.FloorToInt(goods.goodsPrice * discount);
    }
    #endregion

    #region 每日奖励公共方法
    /// <summary>
    /// 检查今日是否可以领取每日奖励
    /// </summary>
    public bool CanReceiveDailyReward()
    {
        return !hasGivenDailyReward;
    }

    /// <summary>
    /// 标记今日已领取每日奖励
    /// </summary>
    public void MarkDailyRewardGiven()
    {
        if (hasGivenDailyReward)
        {
            Debug.LogWarning("[每日奖励] 今日已领取过奖励，请勿重复领取！");
            return;
        }

        hasGivenDailyReward = true;
        SaveDailyShopData(); // 立即保存到本地
        Debug.Log("[每日奖励] 今日奖励已领取，状态已保存");
    }

    /// <summary>
    /// GM功能：重置今日每日奖励状态（用于测试）
    /// </summary>
    [ContextMenu("GM_重置每日奖励状态")]
    private void GM_ResetDailyReward()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在游戏运行（Play Mode）下使用此功能！");
            return;
        }

        hasGivenDailyReward = false;
        SaveDailyShopData();
        Debug.Log("[GM] 每日奖励状态已重置为可领取");
    }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        LoadPlayerGood();
        // 启动时自动生成今日商店数据
        CreateToDayData();
    }

    #region 每日商店生成逻辑 (修复版)
    public void CreateToDayData()
    {
        string today = DateTime.Now.Date.ToString("yyyyMMdd");

        //  先尝试加载完整数据（包括折扣）
        // 这里我们不再分开加载元数据，而是直接尝试加载完整列表，确保折扣不丢失
        bool hasValidLoadedData = TryLoadCompleteDailyShopData();

        // 判断今天是否已经生成过，且数据有效
        if (lastRefreshDate == today && hasValidLoadedData)
        {
            //如果商品列表有了，但折扣字典是空的（旧存档），补全折扣
            if (ToDayRefreshGoodsList.Count > 0 && currentGoodsDiscountDict.Count == 0)
            {
                Debug.LogWarning("[商店] 检测到旧存档数据，正在补全折扣信息...");
                foreach (var goods in ToDayRefreshGoodsList)
                {
                    if (!string.IsNullOrEmpty(goods.goodsGuid))
                    {
                        GenerateDiscountForGoods(goods.goodsGuid);
                    }
                }
                SaveDailyShopData(); // 补全后立即保存
            }

            Debug.Log($"[商店] 今日商品已加载，数量：{ToDayRefreshGoodsList.Count}，剩余刷新次数：{maxDailyRefreshCount - todayUsedRefreshCount}，今日奖励状态：{(hasGivenDailyReward ? "已领取" : "未领取")}");
            return;
        }

        //新的一天 或 数据无效，重新生成商品
        GenerateNewDailyShopGoods(isNewDay: true);
    }

    /// <summary>
    /// 尝试加载完整的每日商店数据
    /// 返回是否加载成功
    /// </summary>
    private bool TryLoadCompleteDailyShopData()
    {
        if (!Application.isPlaying) return false;

        var saveData = DataEncryptionManger.Instance.LoadEncryptedComplexData<DailyShopSaveData>(dailyShopSaveName);

        if (saveData == null || string.IsNullOrEmpty(saveData.date))
        {
            return false; // 没有存档或存档损坏
        }

        // 恢复数据
        lastRefreshDate = saveData.date;
        todayUsedRefreshCount = saveData.dailyRefreshCount;
        // 【新增】恢复每日奖励标记
        hasGivenDailyReward = saveData.hasGivenDailyReward;

        // 恢复商品列表
        ToDayRefreshGoodsList.Clear();
        if (saveData.goodsGuids != null)
        {
            foreach (var guid in saveData.goodsGuids)
            {
                var goods = AllGoodsDataList.FirstOrDefault(g => g.goodsGuid == guid);
                if (goods != null) ToDayRefreshGoodsList.Add(goods);
            }
        }

        // 恢复折扣字典
        currentGoodsDiscountDict.Clear();
        if (saveData.discounts != null)
        {
            foreach (var d in saveData.discounts)
            {
                currentGoodsDiscountDict[d.goodsGuid] = d.discount;
            }
        }

        return true;
    }

    /// <summary>
    /// 手动强制刷新今日商品
    /// </summary>
    public void RefRefreshToDay()
    {
        if (todayUsedRefreshCount >= maxDailyRefreshCount)
        {
            Debug.LogWarning("[商店] 今日刷新次数已用完！请明天再试。");
            return;
        }

        GenerateNewDailyShopGoods(isNewDay: false);

        todayUsedRefreshCount++;
        SaveDailyShopData();

        Debug.Log($"[商店] 手动刷新完成！今日已用 {todayUsedRefreshCount}/{maxDailyRefreshCount} 次");
    }

    /// <summary>
    /// 生成新的每日商品
    /// </summary>
    private void GenerateNewDailyShopGoods(bool isNewDay)
    {
        ToDayRefreshGoodsList.Clear();
        currentGoodsDiscountDict.Clear(); // 清空旧折扣
        string today = DateTime.Now.Date.ToString("yyyyMMdd");

        if (isNewDay)
        {
            todayUsedRefreshCount = 0;
            // 【新增】新的一天，重置每日奖励标记为未领取
            hasGivenDailyReward = false;
            Debug.Log($"[每日奖励] 新的一天到来，每日奖励已重置为可领取状态");
        }

        // 筛选玩家未拥有的商品
        List<GoodsData> availableGoods = AllGoodsDataList
            .Where(goods => !string.IsNullOrEmpty(goods.goodsGuid)
                 && !UserObtainGoodsList.Contains(goods))
            .ToList();

        if (availableGoods.Count == 0)
        {
            Debug.LogWarning("[商店] 玩家已拥有所有商品，无商品可刷新！");
            lastRefreshDate = today;
            SaveDailyShopData();
            return;
        }

        // 加权随机抽取商品
        while (ToDayRefreshGoodsList.Count < EverydayRefreshGoodsAmount && availableGoods.Count > 0)
        {
            GoodsData selectedGoods = GetWeightedRandomGoods(availableGoods);
            if (selectedGoods != null)
            {
                ToDayRefreshGoodsList.Add(selectedGoods);
                availableGoods.Remove(selectedGoods);

                // 为选中的商品生成权重化折扣
                GenerateDiscountForGoods(selectedGoods.goodsGuid);
            }
        }

        lastRefreshDate = today;
        SaveDailyShopData();

        Debug.Log($"[商店] 已重新生成今日商品，打折商品数量：{currentGoodsDiscountDict.Count(kvp => kvp.Value < 1.0f)}");
    }

    /// <summary>
    /// 权重化随机折扣生成
    /// </summary>
    private void GenerateDiscountForGoods(string guid)
    {
        // 先判断是否命中打折概率
        if (UnityEngine.Random.value <= discountChance)
        {
            // 轮盘赌算法，按权重抽取折扣档位
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

            // 兜底：默认9折
            currentGoodsDiscountDict[guid] = 0.9f;
        }
        else
        {
            // 不打折
            currentGoodsDiscountDict[guid] = 1.0f;
        }
    }
    #endregion

    #region 商品加权随机核心算法
    private GoodsData GetWeightedRandomGoods(List<GoodsData> pool)
    {
        int totalWeight = 0;
        List<int> weightList = new List<int>();

        foreach (var goods in pool)
        {
            int typeW = skinTypeWeights.FirstOrDefault(t => t.skinType == goods.skinType)?.weight ?? 5;
            int qualityW = qualityWeights.FirstOrDefault(q => q.quality == goods.quality)?.weight ?? 5;
            int finalW = typeW * qualityW;

            weightList.Add(finalW);
            totalWeight += finalW;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight + 1);
        int current = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            current += weightList[i];
            if (randomValue <= current)
            {
                return pool[i];
            }
        }

        return pool.FirstOrDefault();
    }
    #endregion

    #region 数据持久化
    private void SaveDailyShopData()
    {
        // 只有在播放模式下才执行保存
        if (!Application.isPlaying) return;

        DailyShopSaveData saveData = new DailyShopSaveData();
        saveData.date = lastRefreshDate;
        saveData.goodsGuids = ToDayRefreshGoodsList.Select(g => g.goodsGuid).ToList();
        saveData.dailyRefreshCount = todayUsedRefreshCount;
        // 【新增】保存每日奖励标记
        saveData.hasGivenDailyReward = hasGivenDailyReward;

        // 保存折扣数据
        saveData.discounts = new List<DiscountData>();
        foreach (var kvp in currentGoodsDiscountDict)
        {
            saveData.discounts.Add(new DiscountData { goodsGuid = kvp.Key, discount = kvp.Value });
        }

        DataEncryptionManger.Instance.SaveEncryptedComplexData(dailyShopSaveName, saveData);
    }
    #endregion

    #region 原有商品功能
    public GoodsData GetData()
    {
        if (AllGoodsDataList != null && AllGoodsDataList.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, AllGoodsDataList.Count);
            return AllGoodsDataList[randomIndex];
        }
        else
        {
            Debug.LogWarning("AllGoodsDataList 为空");
            return null;
        }
    }

    public void PurchaseGoodToUser(GoodsData Data)
    {
        if (AllGoodsDataList.Contains(Data))
        {
            if (UserObtainGoodsList.Contains(Data))
            {
                Debug.LogWarning("商品已经被购买");
                return;
            }

            // 使用折后价计算
            int finalPrice = GetGoodsDiscountedPrice(Data);

            if (GoldSystem.Instance.GetGold() >= finalPrice)
            {
                GoldSystem.Instance.CostGold(finalPrice, $"成功购买商品: {Data.goodsName} 原价:{Data.goodsPrice} 折后价:{finalPrice}");
                UserObtainGoodsList.Add(Data);
                SavePlayerGood();
                // 开始对数据进行设置
                LoadGoodsData(Data);
            }
            else
            {
                Debug.LogWarning($"金币不足！当前：{GoldSystem.Instance.GetGold()} 折后价：{finalPrice}");
            }
        }
        else
        {
            Debug.LogWarning($"商品 {Data.goodsName} 不存在");
        }
    }

    /// <summary>
    /// 购买成功后，将数据加载到 GameSkinManager
    /// </summary>
    public void LoadGoodsData(GoodsData Data)
    {
        if (GameSkinManager.Instance == null)
        {
            Debug.LogError("GameSkinManager 实例不存在，无法加载数据！");
            return;
        }

        switch (Data.skinType)
        {
            case SkinType.PlayerCharacter:
                if (Data.playerSkinPack != null && !GameSkinManager.Instance.PlayerOwnerSkinPackList.Contains(Data.playerSkinPack))
                {
                    GameSkinManager.Instance.PlayerOwnerSkinPackList.Add(Data.playerSkinPack);
                }
                break;

            case SkinType.GunHitEffect:
                if (Data.gunHitData != null && !GameSkinManager.Instance.CurrentGunHitDataList.Contains(Data.gunHitData))
                {
                    GameSkinManager.Instance.CurrentGunHitDataList.Add(Data.gunHitData);
                }
                break;

            case SkinType.SpecialBullet:
                if (Data.bulletPack != null && !GameSkinManager.Instance.CurrentBulletBundleList.Contains(Data.bulletPack.BulletBindID))
                {
                    GameSkinManager.Instance.CurrentBulletBundleList.Add(Data.bulletPack.BulletBindID);
                }
                break;
            case SkinType.GunAppearance:
                //加载枪械外观
                GameSkinManager.Instance.AddGunSkinPack(Data.gunSkinPack);
                break;

        }
    }

    public bool JudgeUserHasGood(GoodsData Data) => UserObtainGoodsList.Contains(Data);

    public void SavePlayerGood()
    {
        // 只有在播放模式下才执行保存
        if (!Application.isPlaying) return;

        UserObtainGoodIDsList = new List<string>();
        foreach (GoodsData data in UserObtainGoodsList) UserObtainGoodIDsList.Add(data.goodsGuid);
        DataEncryptionManger.Instance.SaveEncryptedComplexData<List<string>>(PlayerGoodsDataFileName, UserObtainGoodIDsList);
    }

    /// <summary>
    /// 加载玩家商品数据，并验证/补全默认商品
    /// </summary>
    public void LoadPlayerGood()
    {
        // 只有在播放模式下才执行加载
        if (!Application.isPlaying)
            return;

        // 从存档加载已购买的商品
        UserObtainGoodIDsList = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<string>>(PlayerGoodsDataFileName);
        UserObtainGoodsList = new List<GoodsData>();

        if (UserObtainGoodIDsList != null)
        {
            foreach (string goodID in UserObtainGoodIDsList)
            {
                GoodsData data = AllGoodsDataList.Find(g => g.goodsGuid == goodID);
                if (data != null) UserObtainGoodsList.Add(data);
            }
        }

        //验证并补全默认商品
        ValidateAndCompleteDefaultGoods();

        //同步数据到 GameSkinManager
        SyncAllOwnedGoodsToSkinManager();

        // 保存
        SavePlayerGood();
    }

    /// <summary>
    /// 验证默认商品是否都在已购买列表中，不在则补加
    /// </summary>
    private void ValidateAndCompleteDefaultGoods()
    {
        if (DefaultInitGoodsData == null || DefaultInitGoodsData.Count == 0) return;

        int addCount = 0;
        foreach (var defaultGoods in DefaultInitGoodsData)
        {
            if (defaultGoods == null || string.IsNullOrEmpty(defaultGoods.goodsGuid)) continue;

            // 检查默认商品是否已在已购买列表中
            if (!UserObtainGoodsList.Contains(defaultGoods))
            {
                // 同时也检查一下 AllGoodsDataList 里有没有，防止引用丢失
                var goodsInAll = AllGoodsDataList.FirstOrDefault(g => g.goodsGuid == defaultGoods.goodsGuid);
                if (goodsInAll != null)
                {
                    UserObtainGoodsList.Add(goodsInAll);
                    addCount++;
                    Debug.Log($"[默认商品补全] 发现缺失的默认商品，已补回：{goodsInAll.goodsName}");
                }
                else
                {
                    Debug.LogError($"[默认商品错误] 默认商品 {defaultGoods.name} 不在 AllGoodsDataList 中，无法补全！请检查配置。");
                }
            }
        }

        if (addCount > 0)
        {
            Debug.Log($"[默认商品验证] 本次共补全 {addCount} 个默认商品");
        }
    }

    /// <summary>
    /// 将所有已购买商品强制同步到 GameSkinManager
    /// </summary>
    private void SyncAllOwnedGoodsToSkinManager()
    {
        if (GameSkinManager.Instance == null)
            return;

        foreach (var goods in UserObtainGoodsList)
        {
            if (goods == null)
                continue;
            LoadGoodsData(goods);
        }
        //触发本地已经保存的数据加载
        GameSkinManager.Instance.ReturnLastGameEquipment();
    }

    public void ClearLocalData()
    {
        // 只有在播放模式下才执行
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在游戏运行（Play Mode）下使用此功能！");
            return;
        }

        if (UserObtainGoodIDsList != null) UserObtainGoodIDsList.Clear();
        else UserObtainGoodsList = new List<GoodsData>();
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(PlayerGoodsDataFileName);
        // 同时也清空商店数据
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(dailyShopSaveName);
        LoadPlayerGood();
        Debug.Log("玩家商品数据 & 商店数据已清空");
    }

    [ContextMenu("GM_清空本地数据")]
    private void GM_ClearLocalData() => ClearLocalData();

    [ContextMenu("GM_手动刷新今日商店")]
    private void GM_RefreshDailyShop()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在游戏运行（Play Mode）下使用此功能！");
            return;
        }
        RefRefreshToDay();
    }

    /// <summary>
    /// GM功能：重置今日刷新次数
    /// </summary>
    [ContextMenu("GM_重置今日刷新次数")]
    private void GM_ResetRefreshCount()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在游戏运行（Play Mode）下使用此功能！");
            return;
        }

        todayUsedRefreshCount = 0;
        SaveDailyShopData(); // 保存到本地
        Debug.Log($"[GM] 今日刷新次数已重置！剩余次数：{maxDailyRefreshCount - todayUsedRefreshCount}/{maxDailyRefreshCount}");
    }

    public int GetRemainingRefreshCount() => maxDailyRefreshCount - todayUsedRefreshCount;
    #endregion
}