//游戏金币系统
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GoldSystem : SingleMonoAutoBehavior<GoldSystem>
{
    private string DataLocalString = "GameGoldAmount";
    private string GoldCheckSumKey = "GameGoldCheckSum"; 
    private string GoldLogSaveFileName = "GameGoldLogHistory";

    [Header("玩家用户默认金币")]
    public int defaultGoldAmount = 100;
    [Header("安全限制")]
    public int maxGoldLimit = 999999;
    public int singleOperateLimit = 100000;
    [Header("日志设置")]
    public int maxLocalLogCount = 500;
    public int maxPreviewLogCount = 20;

    // 校验用固定魔数（防止简单哈希碰撞，可自行修改）
    private const int GOLD_VERIFY_MAGIC = 0x5F3759DF;
    private int MyGoldDataEncryptionPackID;
    public List<GoldLog> _currentSessionLogList;

    protected override void Awake()
    {
        base.Awake();
        _currentSessionLogList = new List<GoldLog>();

        // 加载加密的本地金币
        int loadedGold = DataEncryptionManger.Instance.LoadEncryptedPlayerPrefs<int>(DataLocalString, defaultGoldAmount);

        // 金币数据完整性校准
        int finalValidGold = VerifyAndFixGoldData(loadedGold);

        // 校验通过后，再写入内存加密
        MyGoldDataEncryptionPackID = DataEncryptionManger.Instance.EncryptData<int>(finalValidGold);

        VerifyAndCleanLocalLogs();

        Debug.Log($"[金币系统] 初始化完成，最终合法金币：{finalValidGold}，本次会话日志已就绪");
    }

    #region 金币防篡改校准逻辑
    /// <summary>
    /// 校验金币数据完整性，被篡改则自动修复并返回合法值
    /// </summary>
    /// <param name="loadedGold">从本地加载的原始金币</param>
    /// <returns>最终合法的金币数值</returns>
    private int VerifyAndFixGoldData(int loadedGold)
    {
        string errorMsg = string.Empty;
        bool isDataValid = true;
        int finalGold = loadedGold;

        if (finalGold < 0 || finalGold > maxGoldLimit)
        {
            errorMsg += $"金币数值非法！范围超出0~{maxGoldLimit}，加载值：{finalGold}；";
            isDataValid = false;
        }

        // 读取本地存储的校验码
        string localCheckSum = PlayerPrefs.GetString(GoldCheckSumKey, string.Empty);
        // 用当前加载的金币重新计算校验码
        string calculatedCheckSum = GenerateGoldCheckSum(finalGold);

        // 非首次启动，校验码比对失败 → 数据被篡改
        if (!string.IsNullOrEmpty(localCheckSum) && localCheckSum != calculatedCheckSum)
        {
            errorMsg += $"金币校验码不匹配！本地校验码：{localCheckSum}，计算校验码：{calculatedCheckSum}，疑似被篡改；";
            isDataValid = false;
        }

        // 读取本地历史日志，做交叉验证
        List<GoldLog> localHistoryLogs = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<GoldLog>>(GoldLogSaveFileName);
        if (localHistoryLogs != null && localHistoryLogs.Count > 0)
        {
            GoldLog lastLog = localHistoryLogs.First(); // 最新的一条日志在最前面
            if (lastLog.afterGold != finalGold)
            {
                errorMsg += $"日志交叉校验失败！最后一条日志剩余金币：{lastLog.afterGold}，加载金币：{finalGold}，疑似被篡改；";
                isDataValid = false;
            }
        }

        if (!isDataValid)
        {
            Debug.LogError($"[金币系统] 数据校验失败！{errorMsg}");

            // 兜底方案1：优先用日志里的最后一个合法值
            if (localHistoryLogs != null && localHistoryLogs.Count > 0)
            {
                finalGold = Mathf.Clamp(localHistoryLogs.First().afterGold, 0, maxGoldLimit);
                Debug.LogWarning($"[金币系统] 已回滚到日志最后记录的合法值：{finalGold}");
            }
            else
            {
                finalGold = defaultGoldAmount;
                Debug.LogWarning($"[金币系统] 无有效日志，已重置为默认金币：{defaultGoldAmount}");
            }

            // 重置后，重新生成合法的校验码并保存
            RefreshGoldCheckSum(finalGold);
            // 重新保存合法的金币数据
            DataEncryptionManger.Instance.SaveEncryptedPlayerPrefs<int>(DataLocalString, finalGold);

            // 新增一条篡改记录日志
            AddGoldLog(0, "系统检测到数据篡改，已自动修复", finalGold);
        }
        // 校验通过：首次启动自动生成校验码
        else
        {
            if (string.IsNullOrEmpty(localCheckSum))
            {
                RefreshGoldCheckSum(finalGold);
                Debug.Log("[金币系统] 首次启动，已生成金币校验码");
            }
        }

        return finalGold;
    }

    /// <summary>
    /// 生成金币专属加密校验码
    /// </summary>
    private string GenerateGoldCheckSum(int goldValue)
    {
        // 多层混淆：魔数异或 + 密钥绑定 + 数值绑定，防止暴力破解
        int mixedValue = goldValue ^ GOLD_VERIFY_MAGIC;
        string rawSignData = $"{mixedValue}_{goldValue}_{GOLD_VERIFY_MAGIC}";
        // 复用你现有的加密工具生成MD5签名
        return DataEncryptionManger.EditorEncryptionTools.CalculateMD5Hash(rawSignData, DataEncryptionManger.Instance.saveSecretKey);
    }

    /// <summary>
    /// 刷新并保存最新的金币校验码
    /// </summary>
    private void RefreshGoldCheckSum(int currentGold)
    {
        string newCheckSum = GenerateGoldCheckSum(currentGold);
        PlayerPrefs.SetString(GoldCheckSumKey, newCheckSum);
        PlayerPrefs.Save();
    }
    #endregion

    // 应用退出/切后台/游戏结束时 → 统一保存
    private void OnApplicationQuit()
    {
        SaveCurrentGoldToFile();
        MergeSessionLogsToLocal();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveCurrentGoldToFile();
            MergeSessionLogsToLocal();
        }
    }

    // 获取当前金币
    public int GetGold()
    {
        return DataEncryptionManger.Instance.GetDecryptedData<int>(MyGoldDataEncryptionPackID);
    }

    // 金币变动
    public void ChangeGold(int Value, string operateReason = "未知操作")
    {
        if (Mathf.Abs(Value) > singleOperateLimit)
        {
            Debug.LogError($"[金币系统] 单次操作超过上限！变动值：{Value}，原因：{operateReason}");
            return;
        }

        int currentGold = GetGold();
        int targetGold = currentGold + Value;

        // 扣钱校验
        if (Value < 0)
        {
            if (targetGold < 0)
            {
                Debug.LogWarning($"[金币系统] 余额不足！当前：{currentGold}，需扣：{Mathf.Abs(Value)}，原因：{operateReason}");
                return;
            }
        }
        // 加钱上限
        else
        {
            targetGold = Mathf.Min(targetGold, maxGoldLimit);
        }

        // 更新内存加密数据
        DataEncryptionManger.Instance.UpdateEncryptedData<int>(MyGoldDataEncryptionPackID, targetGold);
        // 添加本次日志
        AddGoldLog(Value, operateReason, targetGold);

        Debug.Log($"[金币系统] 变动成功！±{Value}，剩余：{targetGold}，原因：{operateReason}");
    }

    #region 提供给外部的数据变动方法
    public void AddGold(int addAmount, string reason = "未知来源")
    {
        if (addAmount <= 0) return;
        ChangeGold(addAmount, reason);
    }

    public bool CostGold(int costAmount, string reason = "未知消耗")
    {
        if (costAmount <= 0) return false;
        int currentGold = GetGold();
        if (currentGold < costAmount) return false;
        ChangeGold(-costAmount, reason);
        return true;
    }
    #endregion

    // 保存金币（同步刷新校验码）
    private void SaveCurrentGoldToFile()
    {
        int currentGold = GetGold();
        // 保存加密金币数据
        DataEncryptionManger.Instance.SaveEncryptedPlayerPrefs<int>(DataLocalString, currentGold);
        // 同步刷新校验码
        RefreshGoldCheckSum(currentGold);
    }

    #region 优化后的日志核心逻辑
    private void VerifyAndCleanLocalLogs()
    {
        var tempCheckList = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<GoldLog>>(GoldLogSaveFileName);
        if (tempCheckList == null)
        {
            Debug.Log("[金币系统] 本地日志文件不存在或已损坏，将创建新档案");
        }
        else
        {
            Debug.Log($"[金币系统] 本地日志校验通过，共 {tempCheckList.Count} 条历史记录");
        }
    }

    private void MergeSessionLogsToLocal()
    {
        if (_currentSessionLogList == null || _currentSessionLogList.Count == 0)
        {
            Debug.Log("[金币系统] 本次无新日志，无需合并");
            return;
        }

        List<GoldLog> allHistoryLogs = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<GoldLog>>(GoldLogSaveFileName);
        allHistoryLogs ??= new List<GoldLog>();

        allHistoryLogs.InsertRange(0, _currentSessionLogList);

        if (allHistoryLogs.Count > maxLocalLogCount)
        {
            allHistoryLogs.RemoveRange(maxLocalLogCount, allHistoryLogs.Count - maxLocalLogCount);
        }

        DataEncryptionManger.Instance.SaveEncryptedComplexData<List<GoldLog>>(GoldLogSaveFileName, allHistoryLogs);
        Debug.Log($"[金币系统] 日志合并成功！本次新增 {_currentSessionLogList.Count} 条，本地总计 {allHistoryLogs.Count} 条");
    }

    private void AddGoldLog(int changeValue, string reason, int afterGold)
    {
        GoldLog log = new GoldLog
        {
            operateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            operateType = changeValue > 0 ? "收入" : "支出",
            changeAmount = changeValue,
            reason = reason,
            afterGold = afterGold
        };
        _currentSessionLogList.Add(log);
    }

    public List<GoldLog> GetAllLogsForUI()
    {
        List<GoldLog> historyLogs = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<GoldLog>>(GoldLogSaveFileName);
        historyLogs ??= new List<GoldLog>();

        List<GoldLog> uiLogs = new List<GoldLog>(_currentSessionLogList);
        uiLogs.AddRange(historyLogs);

        if (uiLogs.Count > maxPreviewLogCount)
        {
            uiLogs.RemoveRange(maxPreviewLogCount, uiLogs.Count - maxPreviewLogCount);
        }

        return uiLogs;
    }
    #endregion

    #region GM 调试
    [ContextMenu("GM_增加10000金币")]
    private void GM_Add10000Gold() => AddGold(10000, "GM调试_增加金币");

    [ContextMenu("GM_重置金币为默认值")]
    private void GM_ResetToDefaultGold() => ForceResetGold(defaultGoldAmount);

    [ContextMenu("GM_清空所有日志(本地+内存)")]
    private void GM_ClearAllLogs()
    {
        _currentSessionLogList.Clear();
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(GoldLogSaveFileName);
        Debug.Log("[金币系统] GM已清空所有日志");
    }

    [ContextMenu("GM_消耗500金币")]
    private void GM_Cost500Gold() => CostGold(500, "GM调试_消耗金币");

    public void ForceResetGold(int targetGold)
    {
        targetGold = Mathf.Clamp(targetGold, 0, maxGoldLimit);
        MyGoldDataEncryptionPackID = DataEncryptionManger.Instance.EncryptData<int>(targetGold);
        SaveCurrentGoldToFile();
        AddGoldLog(-GetGold(), "GM强制重置金币", targetGold);
        Debug.Log($"[金币系统] 强制重置成功：{targetGold}");
    }
    #endregion

    [System.Serializable]
    public class GoldLog
    {
        public string operateTime;
        public string operateType;
        public int changeAmount;
        public string reason;
        public int afterGold;
    }
}