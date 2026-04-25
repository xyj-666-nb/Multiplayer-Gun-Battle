using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// 自动创建打击特效商品数据的编辑器工具
/// 对应文件夹结构：Assets/Resources/GameInfo/HitobjInfo → 生成到 Assets/Resources/GameInfo/GoodInfo
/// </summary>
public static class HitGoodsAutoCreator
{
    // ===================== 可自定义配置 =====================
    private const int DEFAULT_PRICE = 100;          // 默认商品价格
    private const bool SKIP_EXISTING = true;        // 是否跳过已存在的商品（避免覆盖）
    private const bool AUTO_SET_QUALITY = false;    // 是否自动根据ID设置品质
    // ======================================================

    /// <summary>
    /// Unity顶部菜单入口
    /// </summary>
    [MenuItem("Tools/自动创建打击商品数据")]
    public static void CreateAllHitGoodsData()
    {
        // 定义路径（严格对应你的文件夹结构）
        string hitObjRootPath = "Assets/Resources/GameInfo/HitobjInfo";
        string goodInfoRootPath = "Assets/Resources/GameInfo/GoodInfo";

        // 路径合法性检查
        if (!Directory.Exists(hitObjRootPath))
        {
            EditorUtility.DisplayDialog("错误", $"打击特效文件夹不存在！\n路径：{hitObjRootPath}", "确定");
            return;
        }
        // 自动创建GoodInfo文件夹（如果不存在）
        if (!Directory.Exists(goodInfoRootPath))
        {
            Directory.CreateDirectory(goodInfoRootPath);
            AssetDatabase.Refresh();
        }

        // 获取HitobjInfo下所有GunHitData类型的资产
        string[] hitGuids = AssetDatabase.FindAssets("t:GunHitData", new[] { hitObjRootPath });
        if (hitGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "HitobjInfo文件夹下未找到任何打击特效数据！", "确定");
            return;
        }

        int createdCount = 0;
        int skippedCount = 0;

        // 4. 遍历每个打击特效数据，自动创建商品
        foreach (string guid in hitGuids)
        {
            string hitAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            GunHitData hitData = AssetDatabase.LoadAssetAtPath<GunHitData>(hitAssetPath);
            if (hitData == null || string.IsNullOrEmpty(hitData.HitName))
            {
                /* Debug.LogWarning($"跳过无效数据：{hitAssetPath}"); */
                continue;
            }

            //  生成商品数据的保存路径（GoodInfo文件夹下，命名规则：{特效名}_GoodInfo.asset）
            string goodsAssetName = $"{hitData.HitName}_GoodInfo.asset";
            string goodsSavePath = Path.Combine(goodInfoRootPath, goodsAssetName);

            //  检查是否已存在，跳过/覆盖
            if (File.Exists(goodsSavePath) && SKIP_EXISTING)
            {
                skippedCount++;
                continue;
            }

            // 创建GoodsData实例，自动赋值所有字段
            GoodsData goodsData = ScriptableObject.CreateInstance<GoodsData>();

            //  基础核心信息自动赋值
            goodsData.goodsPrice = DEFAULT_PRICE;
            goodsData.skinType = SkinType.GunHitEffect; // 严格对应打击特效类型

            // UI展示信息自动赋值（完全复用打击特效数据）
            goodsData.goodsIcon = hitData.HitIcon;
            goodsData.goodsName = hitData.HitName;
            goodsData.goodsDescription = hitData.HitDescription;
            // 自动设置品质（按ID分级，可自定义）
            goodsData.quality = AUTO_SET_QUALITY
                ? GetQualityByHitID(hitData.HitID)
                : GoodsQuality.Normal;

            // 数据关联自动绑定（直接关联对应打击特效数据）
            goodsData.gunHitData = hitData;

            //  保存商品数据资产
            AssetDatabase.CreateAsset(goodsData, goodsSavePath);
            AssetDatabase.Refresh();

            // 9. 自动生成唯一goodsGuid（用商品资产的全局唯一ID）
            string goodsGuid = AssetDatabase.AssetPathToGUID(goodsSavePath);
            goodsData.goodsGuid = goodsGuid;

            // 10. 保存修改，刷新数据库
            EditorUtility.SetDirty(goodsData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            createdCount++;
            /* Debug.Log($" 成功创建商品：{goodsSavePath}"); */
        }

        // 11. 完成提示
        EditorUtility.DisplayDialog(
            "自动创建完成",
            $"打击商品数据生成完毕！\n新建：{createdCount} 个\n 跳过（已存在）：{skippedCount} 个",
            "确定"
        );
    }

    /// <summary>
    /// 根据打击ID自动设置品质（1-3普通/4-6稀有/7-8史诗，可自定义）
    /// </summary>
    private static GoodsQuality GetQualityByHitID(int hitID)
    {
        return hitID switch
        {
            <= 3 => GoodsQuality.Normal,
            <= 6 => GoodsQuality.Rare,
            _ => GoodsQuality.Epic
        };
    }
}
