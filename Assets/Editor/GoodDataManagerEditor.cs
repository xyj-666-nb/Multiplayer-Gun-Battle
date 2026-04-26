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
        base.OnInspectorGUI();

        GUILayout.Space(15);
        GUILayout.Label("编辑器工具", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("自动扫描并填充所有商品", GUILayout.Height(30)))
        {
            RefreshAllGoodsDataList();
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("提示：\n1. 点击上方按钮可手动扫描所有 GoodsData 资源并填入列表。\n2. 扫描时会同时修复空 GUID 和重复 GUID。", MessageType.Info);
    }

    private void RefreshAllGoodsDataList()
    {
        if (_manager == null) return;

        int fixedGuidCount = GoodsDataEditorTool.EnsureAllGoodsHaveUniqueGuids(false);

        string[] guids = AssetDatabase.FindAssets("t:GoodsData");
        List<GoodsData> foundGoods = new List<GoodsData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
            if (goods != null)
            {
                foundGoods.Add(goods);
            }
        }

        Undo.RecordObject(_manager, "Refresh Goods Data List");
        _manager.AllGoodsDataList = foundGoods;

        EditorUtility.SetDirty(_manager);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GoodsDataEditorTool.RebuildEncryptedGuidFile();

        EditorUtility.DisplayDialog(
            "成功",
            $"已自动扫描并填充 {foundGoods.Count} 个商品数据！\n已修复 {fixedGuidCount} 个空/重复商品 GUID。\nGUID 加密文件也已同步更新。",
            "确定");
    }
}

public class GoodDataAssetProcessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool shouldRefresh = false;

        foreach (string path in importedAssets)
        {
            if (Path.GetExtension(path) == ".asset")
            {
                GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
                if (goods != null)
                {
                    shouldRefresh = true;
                    break;
                }
            }
        }

        if (deletedAssets.Length > 0)
        {
            shouldRefresh = true;
        }

        if (shouldRefresh)
        {
            EditorApplication.delayCall += AutoRefreshManager;
        }
    }

    private static void AutoRefreshManager()
    {
        GoodDataManager manager = Object.FindObjectOfType<GoodDataManager>();
        if (manager != null)
        {
            GoodsDataEditorTool.EnsureAllGoodsHaveUniqueGuids();
            GoodsDataEditorTool.RebuildEncryptedGuidFile();
        }
    }
}
