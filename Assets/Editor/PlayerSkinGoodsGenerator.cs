using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 角色皮肤商品数据一键生成工具
/// 适配你的GoodsData、PlayerSkinPack结构，自动生成/刷新全量数据
/// </summary>
public class PlayerSkinGoodsGenerator : EditorWindow
{
    // 配置路径
    private const string PLAYER_SKIN_FOLDER_PATH = "Assets/Resources/GameInfo/playerSkipInfo";
    private const string GOODS_OUTPUT_FOLDER_PATH = "Assets/Resources/GameInfo/GoodInfo";

    // 商品默认价格配置（按品质自动赋值，可自行修改）
    private const int PRICE_NORMAL = 100;
    private const int PRICE_RARE = 500;
    private const int PRICE_EPIC = 2000;

    [MenuItem("GameTools/角色商品数据生成器")]
    public static void OpenWindow()
    {
        var window = GetWindow<PlayerSkinGoodsGenerator>("角色商品生成器");
        window.minSize = new Vector2(400, 200);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("一键生成/刷新所有角色皮肤对应的商品数据，会自动删除旧数据，重新生成全量，防止遗漏", MessageType.Info);
        GUILayout.Space(20);

        if (GUILayout.Button(" 一键生成所有角色商品数据", GUILayout.Height(40)))
        {
            GenerateAllPlayerSkinGoods();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("路径配置", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("角色皮肤读取路径:", PLAYER_SKIN_FOLDER_PATH);
        EditorGUILayout.LabelField("商品数据输出路径:", GOODS_OUTPUT_FOLDER_PATH);
    }

    /// <summary>
    /// 核心生成逻辑
    /// </summary>
    private void GenerateAllPlayerSkinGoods()
    {
        // 1. 校验并创建输出文件夹
        if (!Directory.Exists(GOODS_OUTPUT_FOLDER_PATH))
        {
            Directory.CreateDirectory(GOODS_OUTPUT_FOLDER_PATH);
            AssetDatabase.Refresh();
            Debug.Log($"自动创建输出文件夹: {GOODS_OUTPUT_FOLDER_PATH}");
        }

        // 2. 先清理旧的角色商品数据（防止遗漏、重复）
        int deleteCount = ClearOldPlayerSkinGoods();
        Debug.Log($"清理完成，删除了 {deleteCount} 个旧的角色商品数据");

        // 3. 读取所有角色皮肤数据
        var allSkinPacks = GetAllPlayerSkinPacks();
        if (allSkinPacks.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "没有找到任何PlayerSkinPack数据，请检查路径是否正确", "确定");
            return;
        }

        // 4. 遍历生成每个皮肤对应的商品数据
        int successCount = 0;
        foreach (var skinPack in allSkinPacks)
        {
            if (CreateGoodsDataFromSkinPack(skinPack))
            {
                successCount++;
            }
        }

        // 5. 刷新资产，完成提示
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("生成完成", $"成功生成 {successCount} 个角色商品数据，清理了 {deleteCount} 个旧数据", "确定");
        Debug.Log($" 生成完成！成功生成 {successCount} 个角色商品数据");
    }

    /// <summary>
    /// 清理旧的角色商品数据（只删除SkinType=PlayerCharacter的，不影响其他类型商品）
    /// </summary>
    private int ClearOldPlayerSkinGoods()
    {
        int count = 0;
        // 找到输出文件夹里所有的GoodsData
        string[] allGoodsGuids = AssetDatabase.FindAssets("t:GoodsData", new[] { GOODS_OUTPUT_FOLDER_PATH });

        foreach (var guid in allGoodsGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var goodsData = AssetDatabase.LoadAssetAtPath<GoodsData>(assetPath);

            // 只删除角色类型的商品数据
            if (goodsData != null && goodsData.skinType == SkinType.PlayerCharacter)
            {
                AssetDatabase.DeleteAsset(assetPath);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 读取所有PlayerSkinPack数据
    /// </summary>
    private List<PlayerSkinPack> GetAllPlayerSkinPacks()
    {
        List<PlayerSkinPack> result = new List<PlayerSkinPack>();

        // 查找指定文件夹里所有的PlayerSkinPack
        string[] allSkinGuids = AssetDatabase.FindAssets("t:PlayerSkinPack", new[] { PLAYER_SKIN_FOLDER_PATH });
        foreach (var guid in allSkinGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var skinPack = AssetDatabase.LoadAssetAtPath<PlayerSkinPack>(assetPath);
            if (skinPack != null)
            {
                result.Add(skinPack);
            }
        }

        return result;
    }

    /// <summary>
    /// 从PlayerSkinPack生成对应的GoodsData
    /// </summary>
    private bool CreateGoodsDataFromSkinPack(PlayerSkinPack skinPack)
    {
        // 校验必填字段
        if (string.IsNullOrEmpty(skinPack.PlayerSkinName))
        {
            Debug.LogWarning($"跳过生成：{skinPack.name} 没有设置PlayerSkinName");
            return false;
        }

        // 1. 创建GoodsData实例
        GoodsData newGoods = ScriptableObject.CreateInstance<GoodsData>();

        // 2. 自动赋值所有字段（完全从皮肤数据里读取）
        newGoods.goodsName = skinPack.PlayerSkinName;
        newGoods.goodsDescription = skinPack.PlayerSkinDescription;
        newGoods.goodsIcon = skinPack.IdleSprite;
        newGoods.quality = skinPack.SkinQuality;
        newGoods.skinType = SkinType.PlayerCharacter;
        newGoods.playerSkinPack = skinPack;

        // 自动生成唯一GUID
        newGoods.goodsGuid = System.Guid.NewGuid().ToString();

        // 按品质自动赋值价格（可自行修改上面的常量）
        newGoods.goodsPrice = newGoods.quality switch
        {
            GoodsQuality.Normal => PRICE_NORMAL,
            GoodsQuality.Rare => PRICE_RARE,
            GoodsQuality.Epic => PRICE_EPIC,
            _ => PRICE_NORMAL
        };

        // 3. 生成文件路径和名称（命名规则：PlayerSkinPack_皮肤名称，可自行修改）
        string fileName = $"PlayerSkinPack_{skinPack.name}";
        string savePath = Path.Combine(GOODS_OUTPUT_FOLDER_PATH, $"{fileName}.asset");

        // 4. 保存文件
        AssetDatabase.CreateAsset(newGoods, savePath);
        EditorUtility.SetDirty(newGoods);

        Debug.Log($" 生成成功：{fileName} -> {savePath}");
        return true;
    }
}