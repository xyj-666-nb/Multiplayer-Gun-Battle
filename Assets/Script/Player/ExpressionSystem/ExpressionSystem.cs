using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 表情系统（运行时管理单例）
public class ExpressionSystem : SingleMonoAutoBehavior<ExpressionSystem>
{
    [Header("表情配置")]
    public List<ExpressionPack> ExpressionPackList; // 所有表情配置表
    public List<int> PlayerOwnExpressionIDList;     // 玩家拥有的表情ID（运行时数据，不保存）
    public List<int> EquipmentExpressionList;//玩家已经装备的表情列表（保存到本地）

    // 【修改】装备列表存档文件名
    private string EquipmentExpressionDataFileName = "EquipmentExpressionData";
    private Dictionary<int, ExpressionPack> ExpressionIDToPackDictionary;

    protected override void Awake()
    {
        base.Awake();
        // 系统初始化（包含加载装备数据）
        SystemInit();
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
    /// 系统初始化（包含加载装备数据+校验）
    /// </summary>
    public void SystemInit()
    {
        // 初始化ID映射字典
        ExpressionIDToPackDictionary = new Dictionary<int, ExpressionPack>();
        foreach (var pack in ExpressionPackList)
        {
            if (pack == null) continue;

            if (!ExpressionIDToPackDictionary.ContainsKey(pack.ExpressionID))
                ExpressionIDToPackDictionary.Add(pack.ExpressionID, pack);
            else
                Debug.LogWarning($"重复表情ID：{pack.ExpressionID}");
        }

        // 加载并校验装备列表
        LoadAndValidateEquipmentList();
    }

    /// <summary>
    /// 加载并校验装备列表
    /// 校验逻辑：如果装备的表情不在玩家拥有列表中，则自动移除
    /// </summary>
    private void LoadAndValidateEquipmentList()
    {
        // 初始化装备列表
        if (EquipmentExpressionList == null)
        {
            EquipmentExpressionList = new List<int>();
        }
        else
        {
            EquipmentExpressionList.Clear();
        }

        // 从本地加载装备数据
        List<int> savedEquipmentList = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<int>>(EquipmentExpressionDataFileName);

        if (savedEquipmentList == null || savedEquipmentList.Count == 0)
        {
            Debug.Log("[表情系统] 未找到保存的装备数据，使用空列表");
            return;
        }

        // 校验并加载：只保留玩家拥有的表情
        int validCount = 0;
        int invalidCount = 0;

        foreach (int equipId in savedEquipmentList)
        {
            // 校验1：ID是否在拥有列表中
            // 校验2：ID是否在总配置表中存在
            if (PlayerOwnExpressionIDList.Contains(equipId) && ExpressionIDToPackDictionary.ContainsKey(equipId))
            {
                EquipmentExpressionList.Add(equipId);
                validCount++;
            }
            else
            {
                invalidCount++;
                Debug.LogWarning($"[表情系统] 移除无效装备表情：ID={equipId}（未拥有或不存在）");
            }
        }

        Debug.Log($"[表情系统] 装备数据加载完成！有效:{validCount} | 无效已移除:{invalidCount}");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 只保存装备列表
        SaveEquipmentList();
    }

    /// <summary>
    ///保存装备列表到本地
    /// </summary>
    private void SaveEquipmentList()
    {
        if (EquipmentExpressionList == null)
        {
            EquipmentExpressionList = new List<int>();
        }

        DataEncryptionManger.Instance.SaveEncryptedComplexData(EquipmentExpressionDataFileName, EquipmentExpressionList);
        Debug.Log($"[表情系统] 装备列表已保存，共 {EquipmentExpressionList.Count} 个装备");
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
        Debug.Log("[表情系统] 已清空玩家所有表情数据（运行时）");
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
        Debug.Log("[表情系统] 已添加测试表情(ID=1)");
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
        Debug.Log("[表情系统] 已重置玩家表情为默认(ID=1)");
    }

    [ContextMenu("GM_给玩家添加所有表情")]
    private void GM_GiveAllExpressions()
    {
        foreach (var pack in ExpressionPackList)
        {
            if (pack != null)
                PlayerObtainExpression(pack.ExpressionID);
        }
        Debug.Log($"已添加所有 {ExpressionPackList.Count} 个表情（运行时）");
    }

    [ContextMenu("GM_强制保存装备列表")]
    private void GM_SaveEquipmentList()
    {
        SaveEquipmentList();
    }

    [ContextMenu("GM_清空装备列表")]
    private void GM_ClearEquipmentList()
    {
        if (EquipmentExpressionList != null)
        {
            EquipmentExpressionList.Clear();
        }
        SaveEquipmentList();
        Debug.Log("[表情系统] 已清空并保存装备列表");
    }
    #endregion
}