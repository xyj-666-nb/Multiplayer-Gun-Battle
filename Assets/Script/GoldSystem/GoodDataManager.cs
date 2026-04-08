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
    [Tooltip("皮肤类型抽取权重（数值越大概率越高）")]
    public SkinTypeWeight[] skinTypeWeights;
    [Tooltip("商品品质抽取权重（数值越大概率越高）")]
    public GoodsQualityWeight[] qualityWeights;

    // 本地存储结构
    [Serializable]
    private class DailyShopSaveData
    {
        public string date;
        public List<string> goodsGuids = new List<string>();
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

    protected override void Awake()
    {
        base.Awake();
        LoadPlayerGood();
        // 启动时自动生成今日商店数据
        CreateToDayData();
    }

    #region 每日商店生成逻辑
    public void CreateToDayData()
    {
        // 获取今天日期
        string today = DateTime.Now.Date.ToString("yyyyMMdd");

        // 如果今天已经生成过，直接加载本地数据
        if (lastRefreshDate == today)
        {
            LoadDailyShopData();
            Debug.Log($"[商店] 今日商品已加载，数量：{ToDayRefreshGoodsList.Count}");
            return;
        }

        // 新的一天 → 自动生成新商品
        GenerateNewDailyShopGoods();
    }

    /// <summary>
    /// 手动强制刷新今日商品
    /// </summary>
    public void RefRefreshToDay()
    {
        GenerateNewDailyShopGoods();
        Debug.Log($"[商店] 手动刷新完成，今日商品已更新！");
    }

    /// <summary>
    /// 生成新的每日商品
    /// </summary>
    private void GenerateNewDailyShopGoods()
    {
        ToDayRefreshGoodsList.Clear();
        string today = DateTime.Now.Date.ToString("yyyyMMdd");

        // ============== 筛选的商品 ==============
        List<GoodsData> availableGoods = AllGoodsDataList
            .Where(goods => !string.IsNullOrEmpty(goods.goodsGuid)
                 && !UserObtainGoodsList.Contains(goods))
            .ToList();

        if (availableGoods.Count == 0)
        {
            Debug.LogWarning("[商店] 玩家已拥有所有商品，无商品可刷新！");
            SaveDailyShopData();
            return;
        }

        // ============== 加权随机抽取商品 ==============
        while (ToDayRefreshGoodsList.Count < EverydayRefreshGoodsAmount && availableGoods.Count > 0)
        {
            GoodsData selectedGoods = GetWeightedRandomGoods(availableGoods);
            if (selectedGoods != null)
            {
                ToDayRefreshGoodsList.Add(selectedGoods);
                availableGoods.Remove(selectedGoods); // 防止重复
            }
        }

        // ============== 保存日期与数据 ==============
        lastRefreshDate = today;
        SaveDailyShopData();
    }
    #endregion

    #region 加权随机核心算法
    /// <summary>
    /// 根据双权重随机抽取商品
    /// </summary>
    private GoodsData GetWeightedRandomGoods(List<GoodsData> pool)
    {
        int totalWeight = 0;
        List<int> weightList = new List<int>();

        foreach (var goods in pool)
        {
            // 获取类型权重
            int typeW = skinTypeWeights.FirstOrDefault(t => t.skinType == goods.skinType)?.weight ?? 5;
            // 获取品质权重
            int qualityW = qualityWeights.FirstOrDefault(q => q.quality == goods.quality)?.weight ?? 5;
            // 总权重 = 类型权重 * 品质权重
            int finalW = typeW * qualityW;

            weightList.Add(finalW);
            totalWeight += finalW;
        }

        // 轮盘赌随机
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

    #region 每日商店数据本地持久化
    private void SaveDailyShopData()
    {
        DailyShopSaveData saveData = new DailyShopSaveData();
        saveData.date = lastRefreshDate;
        saveData.goodsGuids = ToDayRefreshGoodsList.Select(g => g.goodsGuid).ToList();

        DataEncryptionManger.Instance.SaveEncryptedComplexData(dailyShopSaveName, saveData);
    }

    private void LoadDailyShopData()
    {
        ToDayRefreshGoodsList.Clear();
        var saveData = DataEncryptionManger.Instance.LoadEncryptedComplexData<DailyShopSaveData>(dailyShopSaveName);

        if (saveData == null || saveData.goodsGuids.Count == 0)
        {
            GenerateNewDailyShopGoods();
            return;
        }

        // 根据GUID还原商品
        foreach (var guid in saveData.goodsGuids)
        {
            var goods = AllGoodsDataList.FirstOrDefault(g => g.goodsGuid == guid);
            if (goods != null) ToDayRefreshGoodsList.Add(goods);
        }
    }
    #endregion

    #region 原有功能（完全保留）
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

            if (GoldSystem.Instance.GetGold() >= Data.goodsPrice)
            {
                GoldSystem.Instance.CostGold(Data.goodsPrice, $"成功购买商品: {Data.goodsName}价格:{Data.goodsPrice}");
                UserObtainGoodsList.Add(Data);
                SavePlayerGood();
            }
            else
            {
                Debug.LogWarning($"金币不足！当前：{GoldSystem.Instance.GetGold()} 价格：{Data.goodsPrice}");
            }
        }
        else
        {
            Debug.LogWarning($"商品 {Data.goodsName} 不存在");
        }
    }

    public bool JudgeUserHasGood(GoodsData Data) => UserObtainGoodsList.Contains(Data);

    public void SavePlayerGood()
    {
        UserObtainGoodIDsList = new List<string>();
        foreach (GoodsData data in UserObtainGoodsList) UserObtainGoodIDsList.Add(data.goodsGuid);
        DataEncryptionManger.Instance.SaveEncryptedComplexData<List<string>>(PlayerGoodsDataFileName, UserObtainGoodIDsList);
    }

    public void LoadPlayerGood()
    {
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
    }

    public void ClearLocalData()
    {
        if (UserObtainGoodIDsList != null) UserObtainGoodIDsList.Clear();
        else UserObtainGoodsList = new List<GoodsData>();
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(PlayerGoodsDataFileName);
        LoadPlayerGood();
        Debug.Log("玩家商品数据已清空");
    }

    [ContextMenu("GM_清空本地数据")]
    private void GM_ClearLocalData() => ClearLocalData();

    [ContextMenu("GM_手动刷新今日商店")]
    private void GM_RefreshDailyShop() => RefRefreshToDay();
    #endregion
}