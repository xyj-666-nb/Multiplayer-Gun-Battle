using System.Collections.Generic;
using UnityEngine;

//表情系统
public class ExpressionSystem : SingleMonoAutoBehavior<ExpressionSystem>
{
    public List<ExpressionPack> ExpressionPackList;//所有的表情包列表
    public List<int> PlayerOwnExpressionIDList;//玩家拥有的表情ID;
    private string PlayerOwnExpressionIDDataFileName = "PlayerOwnExpressionIDData";//玩家拥有的表情ID数据文件名
    private Dictionary<int, ExpressionPack> ExpressionIDToPackDictionary;//表情ID到表情包的字典映射

    protected override void Awake()
    {
        base.Awake();
        //加载数据
        PlayerOwnExpressionIDList = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<int>>(PlayerOwnExpressionIDDataFileName);//加载数据
        AddDatInDictionary();//转换一下数据 
    }

    public ExpressionPack GetExpressionPack(int ExpressionID)
    {
        if(ExpressionIDToPackDictionary.ContainsKey(ExpressionID))
            return ExpressionIDToPackDictionary[ExpressionID];
        else
        {
            Debug.LogError($"未找到{ExpressionID}的表情");
            return null;
        }

    }

    public void obtainAllExpression()
    {
        foreach (var Pack in ExpressionPackList)
        {
            if (Pack != null)
            {
                PlayerObtainExpression(Pack.ExpressionID);//将这个表情添加到玩家的表情列表中
            }
        }
    }

    private void AddDatInDictionary()
    {
        // 初始化字典
        ExpressionIDToPackDictionary = new Dictionary<int, ExpressionPack>();

        foreach (var Pack in ExpressionPackList)
        {
            if (Pack != null)
            {
                if (!ExpressionIDToPackDictionary.ContainsKey(Pack.ExpressionID))
                {
                    ExpressionIDToPackDictionary.Add(Pack.ExpressionID, Pack);
                }
                else
                {
                    Debug.LogWarning($"表情ID {Pack.ExpressionID} 已经存在于字典中，无法添加重复的表情ID");
                }
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        //对数据进行保存
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);//数据加密保存
    }

    void Start()
    {

    }

    public void PlayerObtainExpression(int expressionID)
    {
        if (!PlayerOwnExpressionIDList.Contains(expressionID))//如果玩家没有这个表情
        {
            PlayerOwnExpressionIDList.Add(expressionID);//将这个表情添加到玩家的表情列表中
        }
    }

    private List<ExpressionPack> TransferExpressionList;//缓存传输表情包列表
    public List<ExpressionPack> GetAllPlayerExpression()//外部拿去自己用
    {
        // 初始化缓存列表
        if (TransferExpressionList == null)
            TransferExpressionList = new List<ExpressionPack>();

        TransferExpressionList.Clear();//清除缓存

        // 遍历玩家拥有的ID，从字典里取对应的表情
        foreach (var id in PlayerOwnExpressionIDList)
        {
            if (ExpressionIDToPackDictionary.TryGetValue(id, out var pack))
            {
                TransferExpressionList.Add(pack);//放入缓存列表
            }
        }

        return TransferExpressionList;//返还缓存列表
    }

    //清除玩家的所有表情数据
    public void ClearAllPlayerExpressionData()
    {
        PlayerOwnExpressionIDList.Clear();
    }

    void Update()
    {

    }

    #region GM 调试
    [ContextMenu("GM_清空玩家所有表情数据")]
    private void GM_ClearAllPlayerExpression()
    {
        ClearAllPlayerExpressionData();
        // 清空后立即保存，确保数据同步
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
        Debug.Log("[表情系统] GM已清空玩家所有表情数据并保存");
    }

    [ContextMenu("GM_给玩家添加测试表情(ID=1)")]
    private void GM_GiveTestExpression()
    {
        // 先检查表情是否存在
        if (ExpressionIDToPackDictionary == null || !ExpressionIDToPackDictionary.ContainsKey(1))
        {
            Debug.LogError("[表情系统] 未找到ID=1的表情，请先在ExpressionPackList里添加表情并生成ID");
            return;
        }

        PlayerObtainExpression(1);
        // 保存数据
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
        Debug.Log("[表情系统] GM已给玩家添加表情ID=1并保存");
    }

    [ContextMenu("GM_查看玩家当前拥有的表情ID")]
    private void GM_ListPlayerExpressions()
    {
        if (PlayerOwnExpressionIDList == null || PlayerOwnExpressionIDList.Count == 0)
        {
            Debug.Log("[表情系统] 玩家当前没有拥有任何表情");
            return;
        }
        Debug.Log($"[表情系统] 玩家当前拥有的表情ID：{string.Join(", ", PlayerOwnExpressionIDList)}（共 {PlayerOwnExpressionIDList.Count} 个）");
    }

    [ContextMenu("GM_重置玩家表情为默认(仅ID=1)")]
    private void GM_ResetPlayerExpressions()
    {
        // 先检查表情是否存在
        if (ExpressionIDToPackDictionary == null || !ExpressionIDToPackDictionary.ContainsKey(1))
        {
            Debug.LogError("[表情系统] 未找到ID=1的表情，请先在ExpressionPackList里添加表情并生成ID");
            return;
        }

        PlayerOwnExpressionIDList.Clear();
        PlayerObtainExpression(1);
        // 保存数据
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
        Debug.Log("[表情系统] GM已重置玩家表情为默认（仅ID=1）并保存");
    }

    [ContextMenu("GM_给玩家添加所有表情")]
    private void GM_GiveAllExpressions()
    {
        if (ExpressionPackList == null || ExpressionPackList.Count == 0)
        {
            Debug.LogError("[表情系统] ExpressionPackList为空，请先添加表情");
            return;
        }

        foreach (var pack in ExpressionPackList)
        {
            if (pack != null)
            {
                PlayerObtainExpression(pack.ExpressionID);
            }
        }

        // 保存数据
        DataEncryptionManger.Instance.SaveEncryptedComplexData(PlayerOwnExpressionIDDataFileName, PlayerOwnExpressionIDList);
        Debug.Log($"[表情系统] GM已给玩家添加所有表情（共 {ExpressionPackList.Count} 个）并保存");
    }
    #endregion
}

//表情数据包
[System.Serializable]
public class ExpressionPack
{
    public Sprite ExpressionSprite;
    public int ExpressionID;//表情ID
}