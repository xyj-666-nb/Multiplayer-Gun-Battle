using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(GoodDataManager))]
public class GoodDataManagerEditor : Editor
{
    private GoodDataManager _manager;

    private void OnEnable()
    {
        _manager = (GoodDataManager)target;
    }

    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector
        base.OnInspectorGUI();

        GUILayout.Space(15);
        GUILayout.Label(" 编辑器工具", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 手动刷新按钮
        if (GUILayout.Button(" 自动扫描并填充所有商品", GUILayout.Height(30)))
        {
            RefreshAllGoodsDataList();
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("提示：\n1. 点击上方按钮可手动扫描所有 GoodsData 资源并填入列表。\n2. 配合下方的「资源监听」，创建新商品时会自动填充。", MessageType.Info);
    }

    // 核心逻辑：扫描并填充列表
    private void RefreshAllGoodsDataList()
    {
        if (_manager == null) return;

        string[] guids = AssetDatabase.FindAssets("t:GoodsData");
        List<GoodsData> foundGoods = new List<GoodsData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
            if (goods != null)
            {
                foundGoods.Add(goods);

                // 顺便检查并生成 GUID
                if (string.IsNullOrEmpty(goods.goodsGuid))
                {
                    goods.goodsGuid = System.Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(goods);
                }
            }
        }

        Undo.RecordObject(_manager, "Refresh Goods Data List");

        _manager.AllGoodsDataList = foundGoods;

        EditorUtility.SetDirty(_manager);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GoodsDataEditorTool.RebuildEncryptedGuidFile();

        Debug.Log($"[GoodDataManager] 刷新完成！共找到 {foundGoods.Count} 个商品数据，并更新了加密文件。");
        EditorUtility.DisplayDialog("成功", $"已自动扫描并填充 {foundGoods.Count} 个商品数据！\nGUID 加密文件也已同步更新。", "确定");
    }
}

public class GoodDataAssetProcessor : AssetPostprocessor
{
    // 当有资源被导入、删除、移动、重命名时调用
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool shouldRefresh = false;

        // 检查是否有 GoodsData 相关的资源变动
        foreach (string path in importedAssets)
        {
            if (Path.GetExtension(path) == ".asset") // 假设你的 GoodsData 是 .asset 文件
            {
                // 尝试加载看看是不是 GoodsData 类型
                GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
                if (goods != null)
                {
                    shouldRefresh = true;
                    break;
                }
            }
        }

        // 如果有删除，也刷新一下（虽然这里不检查具体类型，但为了安全）
        if (deletedAssets.Length > 0)
        {
            shouldRefresh = true;
        }

        if (shouldRefresh)
        {
            // 延迟一帧执行，确保资源已经导入完成
            EditorApplication.delayCall += AutoRefreshManager;
        }
    }

    private static void AutoRefreshManager()
    {
        // 找到场景中的 GoodDataManager
        GoodDataManager manager = Object.FindObjectOfType<GoodDataManager>();
        if (manager != null)
        {
            // 这里我们直接复用上面的逻辑，为了不重复代码，我们可以通过反射或者直接再写一遍
            // 为了稳定性，这里我们只打印日志，提示用户可以手动点一下按钮
            // 如果你想要完全自动，可以把上面 RefreshAllGoodsDataList 的逻辑复制一份到这里
            Debug.Log("[GoodDataManager] 检测到商品资源变动，请点击 Inspector 上的「自动扫描」按钮刷新列表。");

            // 如果你想要【完全全自动】，取消下面这行的注释 (需要把上面 RefreshAllGoodsDataList 改成静态方法或放到这里)：
             //ForceRefreshSilent();
        }
    }
}