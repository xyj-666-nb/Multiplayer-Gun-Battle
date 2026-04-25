using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 综合商品数据一键生成工具
/// 支持：角色皮肤、枪械皮肤
/// </summary>
public class GoodsGeneratorWindow : EditorWindow
{
    // ====================== 路径配置 ======================
    // 角色皮肤配置
    private const string PLAYER_SKIN_FOLDER_PATH = "Assets/Resources/GameInfo/playerSkipInfo";
    // 枪械皮肤配置 (请根据你实际存放GunSkinPack的文件夹修改路径)
    private const string GUN_SKIN_FOLDER_PATH = "Assets/Resources/GameInfo/GunSkinInfo";
    // 商品输出路径
    private const string GOODS_OUTPUT_FOLDER_PATH = "Assets/Resources/GameInfo/GoodInfo";

    // ====================== 默认价格配置 ======================
    private const int PRICE_NORMAL = 100;
    private const int PRICE_RARE = 500;
    private const int PRICE_EPIC = 2000;

    [MenuItem("GameTools/综合商品数据生成器")]
    public static void OpenWindow()
    {
        var window = GetWindow<GoodsGeneratorWindow>("综合商品生成器");
        window.minSize = new Vector2(450, 300);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(20);
        EditorGUILayout.HelpBox("一键生成/刷新所有商品数据，会自动删除对应类型的旧数据，重新生成全量", MessageType.Info);
        GUILayout.Space(20);

        // ====== 角色皮肤生成区域 ======
        GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
        if (GUILayout.Button(" 一键生成【角色皮肤】商品数据", GUILayout.Height(35)))
        {
            GenerateAllPlayerSkinGoods();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // ====== 枪械皮肤生成区域 ======
        GUI.backgroundColor = new Color(1f, 0.9f, 0.8f);
        if (GUILayout.Button(" 一键生成【枪械皮肤】商品数据", GUILayout.Height(35)))
        {
            GenerateAllGunSkinGoods();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);
        EditorGUILayout.LabelField("路径配置 (如需修改请直接改代码)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("角色皮肤源路径:", PLAYER_SKIN_FOLDER_PATH);
        EditorGUILayout.LabelField("枪械皮肤源路径:", GUN_SKIN_FOLDER_PATH);
        EditorGUILayout.LabelField("商品数据输出路径:", GOODS_OUTPUT_FOLDER_PATH);
    }

    #region 【核心逻辑：枪械皮肤生成】

    private void GenerateAllGunSkinGoods()
    {
        // 1. 校验文件夹
        if (!Directory.Exists(GOODS_OUTPUT_FOLDER_PATH))
        {
            Directory.CreateDirectory(GOODS_OUTPUT_FOLDER_PATH);
        }
        if (!Directory.Exists(GUN_SKIN_FOLDER_PATH))
        {
            EditorUtility.DisplayDialog("错误", $"未找到枪械皮肤源文件夹: {GUN_SKIN_FOLDER_PATH}\n请在代码中修改路径。", "确定");
            return;
        }

        // 2. 清理旧的枪械皮肤商品数据
        int deleteCount = ClearOldGoodsByType(SkinType.GunAppearance);
        /* Debug.Log($"[枪械] 清理完成，删除了 {deleteCount} 个旧数据"); */

        // 3. 读取所有枪械皮肤
        List<GunSkinPack> allGunSkins = GetAllGunSkinPacks();
        if (allGunSkins.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有找到任何 GunSkinPack 数据", "确定");
            return;
        }

        // 4. 遍历生成
        int successCount = 0;
        foreach (var gunSkin in allGunSkins)
        {
            if (CreateGunSkinGoods(gunSkin))
            {
                successCount++;
            }
        }

        // 5. 完成
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("枪械皮肤生成完成", $"成功生成 {successCount} 个枪械商品数据", "确定");
    }

    private List<GunSkinPack> GetAllGunSkinPacks()
    {
        List<GunSkinPack> result = new List<GunSkinPack>();
        string[] guids = AssetDatabase.FindAssets("t:GunSkinPack", new[] { GUN_SKIN_FOLDER_PATH });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var pack = AssetDatabase.LoadAssetAtPath<GunSkinPack>(path);
            if (pack != null) result.Add(pack);
        }
        return result;
    }

    private bool CreateGunSkinGoods(GunSkinPack gunSkin)
    {
        // 简单校验
        if (string.IsNullOrEmpty(gunSkin.skinName))
        {
            /* Debug.LogWarning($"[枪械] 跳过生成：{gunSkin.name} 未设置 skinName"); */
            return false;
        }

        // 1. 创建实例
        GoodsData newGoods = ScriptableObject.CreateInstance<GoodsData>();

        // 2. 字段映射赋值
        // 基础信息
        newGoods.goodsName = gunSkin.skinName;
        newGoods.goodsDescription = gunSkin.description;
        newGoods.goodsIcon = gunSkin.skinIcon;
        newGoods.skinType = SkinType.GunAppearance;

        // 数据关联
        newGoods.gunSkinPack = gunSkin;

        // 自动生成 (这里默认给个普通品质，你也可以在GunSkinPack里加个Quality字段来读取)
        newGoods.quality = GoodsQuality.Rare;
        newGoods.goodsPrice = PRICE_RARE; // 按品质定价
        newGoods.goodsGuid = System.Guid.NewGuid().ToString();

        // 3. 保存文件 (命名规则：GunSkinGoodsData_皮肤名称)
        string fileName = $"GunSkinGoodsData_{gunSkin.name}";
        string savePath = Path.Combine(GOODS_OUTPUT_FOLDER_PATH, $"{fileName}.asset");

        AssetDatabase.CreateAsset(newGoods, savePath);
        EditorUtility.SetDirty(newGoods);

        /* Debug.Log($"[枪械] 生成成功：{fileName}"); */
        return true;
    }

    #endregion

    #region 【核心逻辑：角色皮肤生成 (保持原样)】

    private void GenerateAllPlayerSkinGoods()
    {
        if (!Directory.Exists(GOODS_OUTPUT_FOLDER_PATH)) Directory.CreateDirectory(GOODS_OUTPUT_FOLDER_PATH);

        int deleteCount = ClearOldGoodsByType(SkinType.PlayerCharacter);
        var allSkinPacks = GetAllPlayerSkinPacks();

        if (allSkinPacks.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "没有找到任何PlayerSkinPack数据", "确定");
            return;
        }

        int successCount = 0;
        foreach (var skinPack in allSkinPacks)
        {
            if (CreatePlayerSkinGoods(skinPack)) successCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("角色皮肤生成完成", $"成功生成 {successCount} 个角色商品数据", "确定");
    }

    private List<PlayerSkinPack> GetAllPlayerSkinPacks()
    {
        List<PlayerSkinPack> result = new List<PlayerSkinPack>();
        string[] guids = AssetDatabase.FindAssets("t:PlayerSkinPack", new[] { PLAYER_SKIN_FOLDER_PATH });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var pack = AssetDatabase.LoadAssetAtPath<PlayerSkinPack>(path);
            if (pack != null) result.Add(pack);
        }
        return result;
    }

    private bool CreatePlayerSkinGoods(PlayerSkinPack skinPack)
    {
        if (skinPack == null)
            return false;

        GoodsData newGoods = ScriptableObject.CreateInstance<GoodsData>();

        newGoods.goodsName = string.IsNullOrEmpty(skinPack.PlayerSkinName) ? skinPack.name : skinPack.PlayerSkinName;
        newGoods.goodsDescription = skinPack.PlayerSkinDescription;
        newGoods.goodsIcon = skinPack.IdleSprite;
        newGoods.skinType = SkinType.PlayerCharacter;
        newGoods.playerSkinPack = skinPack;
        newGoods.goodsGuid = System.Guid.NewGuid().ToString();
        newGoods.quality = skinPack.SkinQuality;
        newGoods.goodsPrice = GetPriceByQuality(newGoods.quality);

        string fileName = $"PlayerSkinPack_{skinPack.name}";
        string savePath = Path.Combine(GOODS_OUTPUT_FOLDER_PATH, $"{fileName}.asset");

        AssetDatabase.CreateAsset(newGoods, savePath);
        EditorUtility.SetDirty(newGoods);
        return true;
    }

    #endregion

    #region 【通用工具方法】

    /// <summary>
    /// 通用清理方法：根据类型删除旧的GoodsData
    /// </summary>
    private int ClearOldGoodsByType(SkinType typeToDelete)
    {
        int count = 0;
        string[] allGoodsGuids = AssetDatabase.FindAssets("t:GoodsData", new[] { GOODS_OUTPUT_FOLDER_PATH });

        foreach (var guid in allGoodsGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var goodsData = AssetDatabase.LoadAssetAtPath<GoodsData>(assetPath);

            if (goodsData != null && goodsData.skinType == typeToDelete)
            {
                AssetDatabase.DeleteAsset(assetPath);
                count++;
            }
        }
        return count;
    }

    private int GetPriceByQuality(GoodsQuality quality)
    {
        switch (quality)
        {
            case GoodsQuality.Rare:
                return PRICE_RARE;
            case GoodsQuality.Epic:
                return PRICE_EPIC;
            default:
                return PRICE_NORMAL;
        }
    }

    #endregion
}
