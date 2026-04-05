using System.Collections.Generic;
using UnityEngine;

public class GoodDataManager : SingleMonoAutoBehavior<GoodDataManager>
{
    public List<GoodsData> AllGoodsDataList; // 存储所有商品数据的列表(面板进行赋值)
    public List<GoodsData> UserObtainGoodsList; // 玩家已获得的商品数据列表(数据需要保密存储)
    private List<string> UserObtainGoodIDsList;//玩家已经获取的物品ID
    private string PlayerGoodsDataFileName = "PlayerGoodsData"; // 存储玩家商品数据的文件名

    protected override void Awake()
    {
        base.Awake();
        //自动加载分配数据
        LoadPlayerGood();//加载玩家已获得的商品数据
    }

    public GoodsData GetData()
    {
        //这个方法可以根据需要进行扩展，比如根据ID获取数据，或者随机获取数据等
        //目前我们先简单返回一个随机商品数据，后续可以根据实际需求进行调整
        if (AllGoodsDataList != null && AllGoodsDataList.Count > 0)
        {
            int randomIndex = Random.Range(0, AllGoodsDataList.Count);
            return AllGoodsDataList[randomIndex];
        }
        else
        {
            Debug.LogWarning("AllGoodsDataList 为空，请在面板中添加商品数据");
            return null;
        }
    }

    //购买商品给玩家
    public void PurchaseGoodToUser( GoodsData Data)
    {
        //先查询是否存在这个商品
        if(AllGoodsDataList.Contains(Data))
        {
            if (UserObtainGoodsList.Contains(Data))
            {
                Debug.LogWarning("商品已经被购买");
                return;
            }

            //判断价格
            if (GoldSystem.Instance.GetGold() >= Data.goodsPrice)
            {
                //扣除金币
                GoldSystem.Instance.CostGold(Data.goodsPrice, $"成功购买商品: {Data.goodsName}商品价格 {Data.goodsPrice}");//添加购买日志
                //添加到玩家已获得的商品列表
                UserObtainGoodsList.Add(Data);
                //金币是重要资源，购买商品后需要保存玩家的商品数据
                SavePlayerGood();//提前保存一次 
            }
            else
            {
                Debug.LogWarning($"购买失败，金币不足，当前金币 {GoldSystem.Instance.GetGold()}商品价格 {Data.goodsPrice}");
                //弹出提示UI，显示金币不足
            }

        }
        else
        {
            Debug.LogWarning($"[GoodDataManager] 购买失败，商品 {Data.goodsName} 不存在于 AllGoodsDataList 中");
            return;
        }

    }

    //保存玩家的商品数据
    public void SavePlayerGood()
    {
        //为了安全起见，我们不直接保存整个商品数据列表，而是只保存玩家已获得商品的ID列表
        UserObtainGoodIDsList = new List<string>();
        foreach (GoodsData data in UserObtainGoodsList)
        {
            UserObtainGoodIDsList.Add(data.goodsGuid);
        }
        DataEncryptionManger.Instance.SaveEncryptedComplexData<List<string>>(PlayerGoodsDataFileName, UserObtainGoodIDsList);//进行加密保存
    }

    public void LoadPlayerGood()
    {
        UserObtainGoodIDsList = DataEncryptionManger.Instance.LoadEncryptedComplexData<List<string>>(PlayerGoodsDataFileName);//进行加密加载
        //然后通过列表去找到对应的商品数据，填充玩家已获得的商品列表
        UserObtainGoodsList = new List<GoodsData>();
        if (UserObtainGoodIDsList != null)
        {
            foreach (string goodID in UserObtainGoodIDsList)
            {
                GoodsData data = AllGoodsDataList.Find(g => g.goodsGuid == goodID);
                if (data != null)
                {
                    UserObtainGoodsList.Add(data);
                }
                else
                {
                    Debug.LogWarning($"[GoodDataManager] 加载玩家商品数据时，未找到对应的商品ID: {goodID}，可能数据有误");
                }
            }
            Debug.Log($"[GoodDataManager] 玩家商品数据加载完成，已获得商品数量: {UserObtainGoodsList.Count}");
        }
        else
        {
            Debug.Log("[GoodDataManager] 玩家商品数据加载完成，但没有找到任何已获得的商品");
        }
    }

    //清除本地数据（测试用）
    public void ClearLocalData()
    {
        // 清空内存中的列表
        if (UserObtainGoodIDsList != null)
        {
            UserObtainGoodIDsList.Clear();
            Debug.Log("[GoodDataManager] 内存中的玩家商品数据已清空");
        }
        else
        {
            UserObtainGoodsList = new List<GoodsData>();
        }

        // 删除本地加密存档文件
        DataEncryptionManger.Instance.DeleteEncryptedComplexData(PlayerGoodsDataFileName);

         LoadPlayerGood(); 
        Debug.Log("[GoodDataManager] 玩家商品数据本地清除完成！");
    }

    [ContextMenu(" GM_ClearLocalData")]
    private void GM_ClearLocalData()
    {
        ClearLocalData();
    }
    [ContextMenu(" 自动扫描所有商品到列表")]
    private void AutoScanGoodsInEditor()
    {
        // 这个方法只是为了在编辑器里右键调用，
        // 具体逻辑我们在 Editor 脚本里写，避免污染运行时代码
#if UNITY_EDITOR
        // 这里直接调用 Editor 脚本的逻辑 
        // 简单起见，这里我们直接手动触发一下资源刷新
        UnityEditor.EditorUtility.DisplayDialog("提示", "请在 Inspector 面板点击「自动扫描并填充所有商品」按钮，\n或者使用顶部菜单 Tools/商品GUID管理器。", "好的");
#endif
    }
}
