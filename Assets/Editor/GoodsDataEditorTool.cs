using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class GoodsDataEditorTool : EditorWindow
{
    private const string GOODS_GUID_FILE_NAME = "GoodsGuidMap.bytes";
    private const string SECRET_KEY = "MyProject_Multiplayer-Gun-Battle_2026_5people_2months_Sophomore";

    [MenuItem("Tools/商品GUID管理器")]
    public static void ShowWindow()
    {
        GetWindow<GoodsDataEditorTool>("商品GUID管理器");
    }

    private void OnGUI()
    {
        GUILayout.Label("商品 GUID 自动管理工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("手动扫描所有商品并生成/修复GUID"))
        {
            ScanAllGoodsAndGenerateGuids();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("手动重建加密GUID映射文件"))
        {
            RebuildEncryptedGuidFile();
        }

        GUILayout.Space(20);
        GUILayout.Label("说明：", EditorStyles.wordWrappedLabel);
        GUILayout.Label("1. 创建/修改/删除 GoodsData 时，会自动更新 GUID 和加密文件", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2. 手动扫描会同时修复空 GUID 和重复 GUID", EditorStyles.wordWrappedLabel);
    }

    public static void ScanAllGoodsAndGenerateGuids()
    {
        int fixedCount = EnsureAllGoodsHaveUniqueGuids(false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RebuildEncryptedGuidFile();

        if (fixedCount > 0)
            EditorUtility.DisplayDialog("成功", $"已为 {fixedCount} 个商品生成或修复 GUID，并更新加密文件！", "确定");
        else
            EditorUtility.DisplayDialog("提示", "所有商品 GUID 均有效且唯一，已更新加密文件", "确定");
    }

    public static int EnsureAllGoodsHaveUniqueGuids(bool saveAssets = true)
    {
        string[] assetGuids = AssetDatabase.FindAssets("t:GoodsData");
        HashSet<string> usedGoodsGuids = new HashSet<string>();
        int fixedCount = 0;

        foreach (string assetGuid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
            if (goods == null)
                continue;

            bool needGenerate = string.IsNullOrWhiteSpace(goods.goodsGuid) || usedGoodsGuids.Contains(goods.goodsGuid);
            if (needGenerate)
            {
                goods.goodsGuid = CreateUniqueGoodsGuid(usedGoodsGuids);
                EditorUtility.SetDirty(goods);
                fixedCount++;
            }

            usedGoodsGuids.Add(goods.goodsGuid);
        }

        if (saveAssets && fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        return fixedCount;
    }

    private static string CreateUniqueGoodsGuid(HashSet<string> usedGoodsGuids)
    {
        string newGuid;
        do
        {
            newGuid = System.Guid.NewGuid().ToString();
        }
        while (usedGoodsGuids.Contains(newGuid));

        return newGuid;
    }

    public static void RebuildEncryptedGuidFile()
    {
        Dictionary<string, string> guidMap = new Dictionary<string, string>();
        string[] assetGuids = AssetDatabase.FindAssets("t:GoodsData");

        foreach (string assetGuid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);

            if (goods != null && !string.IsNullOrEmpty(goods.goodsGuid))
            {
                guidMap[path] = goods.goodsGuid;
            }
        }

        GuidMapData data = new GuidMapData { guidDictionary = guidMap };
        string encryptedString = DataEncryptionManger.EditorEncryptionTools.GenerateEncryptedSaveString(data, SECRET_KEY);

        string folderPath = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, GOODS_GUID_FILE_NAME);
        File.WriteAllText(filePath, encryptedString);

        AssetDatabase.Refresh();
    }

    public static Dictionary<string, string> LoadDecryptedGuidMap()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, GOODS_GUID_FILE_NAME);
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, string>();
        }

        string encryptedString = File.ReadAllText(filePath);
        var data = DataEncryptionManger.EditorEncryptionTools.DecryptSaveString<GuidMapData>(encryptedString, SECRET_KEY);

        return data?.guidDictionary ?? new Dictionary<string, string>();
    }
}

[System.Serializable]
public class GuidMapData
{
    public Dictionary<string, string> guidDictionary = new Dictionary<string, string>();
}
