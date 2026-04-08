using System.Collections.Generic;
using UnityEngine;

// 表情系统（运行时管理单例）
public class ExpressionSystem : SingleMonoAutoBehavior<ExpressionSystem>
{
    [Header("表情配置")]
    public List<ExpressionPack> ExpressionPackList; // 所有表情配置表
    public List<int> PlayerOwnExpressionIDList;     // 玩家拥有的表情ID

    private string PlayerOwnExpressionIDDataFileName = "PlayerOwnExpressionIDData";
    private Dictionary<int, ExpressionPack> ExpressionIDToPackDictionary;

    protected override void Awake()
    {
        base.Awake();
        // 加载本地数据
        PlayerOwnExpressionIDList = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<int>>(PlayerOwnExpressionIDDataFileName);
        // 初始化字典
        AddDatInDictionary();
    }

    /// <summary>
    /// 根据ID获取表情
    /// </summary>
    public ExpressionPack GetExpressionPack(int ExpressionID)
    {
        if (ExpressionIDToPackDictionary.TryGetValue(ExpressionID, out var pack))
            return pack;

        Debug.LogError($"未找到ID为 {ExpressionID} 的表情");
        return null;
    }

    /// <summary>
    /// 玩家获得全部表情
    /// </summary>
    public void obtainAllExpression()
    {
        foreach (var pack in ExpressionPackList)
        {
            if (pack != null)
                PlayerObtainExpression(pack.ExpressionID);
        }
    }

    /// <summary>
    /// 初始化ID映射字典
    /// </summary>
    private void AddDatInDictionary()
    {
        ExpressionIDToPackDictionary = new Dictionary<int, ExpressionPack>();
        foreach (var pack in ExpressionPackList)
        {
            if (pack == null) continue;

            if (!ExpressionIDToPackDictionary.ContainsKey(pack.ExpressionID))
                ExpressionIDToPackDictionary.Add(pack.ExpressionID, pack);
            else
                Debug.LogWarning($"重复表情ID：{pack.ExpressionID}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 保存数据
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
    }

    /// <summary>
    /// 玩家获得单个表情
    /// </summary>
    public void PlayerObtainExpression(int expressionID)
    {
        if (!PlayerOwnExpressionIDList.Contains(expressionID))
            PlayerOwnExpressionIDList.Add(expressionID);
    }

    private List<ExpressionPack> TransferExpressionList;
    /// <summary>
    /// 获取玩家所有拥有的表情
    /// </summary>
    public List<ExpressionPack> GetAllPlayerExpression()
    {
        TransferExpressionList ??= new List<ExpressionPack>();
        TransferExpressionList.Clear();

        foreach (var id in PlayerOwnExpressionIDList)
        {
            if (ExpressionIDToPackDictionary.TryGetValue(id, out var pack))
                TransferExpressionList.Add(pack);
        }
        return TransferExpressionList;
    }

    /// <summary>
    /// 清空玩家表情数据
    /// </summary>
    public void ClearAllPlayerExpressionData()
    {
        PlayerOwnExpressionIDList.Clear();
    }

    #region GM 调试
    [ContextMenu("GM_清空玩家所有表情数据")]
    private void GM_ClearAllPlayerExpression()
    {
        ClearAllPlayerExpressionData();
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
        Debug.Log("[表情系统] 已清空玩家所有表情数据");
    }

    [ContextMenu("GM_给玩家添加测试表情(ID=1)")]
    private void GM_GiveTestExpression()
    {
        if (!ExpressionIDToPackDictionary.ContainsKey(1))
        {
            Debug.LogError("未找到ID=1的表情");
            return;
        }
        PlayerObtainExpression(1);
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
    }

    [ContextMenu("GM_查看玩家当前拥有的表情ID")]
    private void GM_ListPlayerExpressions()
    {
        if (PlayerOwnExpressionIDList.Count == 0)
        {
            Debug.Log("玩家无任何表情");
            return;
        }
        Debug.Log($"玩家拥有表情ID：{string.Join(", ", PlayerOwnExpressionIDList)}");
    }

    [ContextMenu("GM_重置玩家表情为默认(仅ID=1)")]
    private void GM_ResetPlayerExpressions()
    {
        if (!ExpressionIDToPackDictionary.ContainsKey(1))
        {
            Debug.LogError("未找到ID=1的表情");
            return;
        }
        PlayerOwnExpressionIDList.Clear();
        PlayerObtainExpression(1);
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
    }

    [ContextMenu("GM_给玩家添加所有表情")]
    private void GM_GiveAllExpressions()
    {
        foreach (var pack in ExpressionPackList)
        {
            if (pack != null)
                PlayerObtainExpression(pack.ExpressionID);
        }
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
        Debug.Log($"已添加所有 {ExpressionPackList.Count} 个表情");
    }
    #endregion
}