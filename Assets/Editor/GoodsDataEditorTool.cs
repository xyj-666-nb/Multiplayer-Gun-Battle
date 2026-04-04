using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class GoodsDataEditorTool : EditorWindow
{
    // 配置
    private const string GOODS_GUID_FILE_NAME = "GoodsGuidMap.bytes"; // 加密文件后缀
    private const string SECRET_KEY = "MyProject_Multiplayer-Gun-Battle_2026_5people_2months_Sophomore"; // 和你加密管理器一致

    // 窗口菜单
    [MenuItem("Tools/商品GUID管理器")]
    public static void ShowWindow()
    {
        GetWindow<GoodsDataEditorTool>("商品GUID管理器");
    }

    private void OnGUI()
    {
        GUILayout.Label("商品 GUID 自动管理工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("手动扫描所有商品并生成GUID"))
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
        GUILayout.Label("2. 也可以手动点击上面按钮强制刷新", EditorStyles.wordWrappedLabel);
    }

    // 扫描所有 GoodsData 并生成 GUID
    public static void ScanAllGoodsAndGenerateGuids()
    {
        string[] guids = AssetDatabase.FindAssets("t:GoodsData");
        int generatedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);

            if (goods != null && string.IsNullOrEmpty(goods.goodsGuid))
            {
                goods.goodsGuid = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(goods);
                generatedCount++;
                Debug.Log($"[GUID工具] 自动生成GUID：{goods.goodsName} -> {goods.goodsGuid}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 自动重建加密文件
        RebuildEncryptedGuidFile();

        if (generatedCount > 0)
            EditorUtility.DisplayDialog("成功", $"已为 {generatedCount} 个商品生成 GUID 并更新加密文件！", "确定");
        else
            EditorUtility.DisplayDialog("提示", "所有商品都已有 GUID，已更新加密文件", "确定");
    }

    // 重建加密的 GUID 映射文件
    public static void RebuildEncryptedGuidFile()
    {
        // 1. 收集所有商品的 GUID
        Dictionary<string, string> guidMap = new Dictionary<string, string>();
        string[] guids = AssetDatabase.FindAssets("t:GoodsData");

        foreach (string assetGuid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);

            if (goods != null && !string.IsNullOrEmpty(goods.goodsGuid))
            {
                // Key: 资源路径，Value: 商品GUID
                guidMap[path] = goods.goodsGuid;
            }
        }

        // 2. 序列化并加密
        GuidMapData data = new GuidMapData { guidDictionary = guidMap };
        string encryptedString = DataEncryptionManger.EditorEncryptionTools.GenerateEncryptedSaveString(data, SECRET_KEY);

        // 3. 保存到 StreamingAssets 文件夹
        string folderPath = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, GOODS_GUID_FILE_NAME);
        File.WriteAllText(filePath, encryptedString);

        AssetDatabase.Refresh();
        Debug.Log($"[GUID工具] 加密GUID映射文件已更新：{filePath}");
    }

    // 运行时加载解密的 GUID 映射（给 GoodsManager 用）
    public static Dictionary<string, string> LoadDecryptedGuidMap()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, GOODS_GUID_FILE_NAME);
        if (!File.Exists(filePath))
        {
            Debug.LogError("[GUID工具] 未找到加密GUID映射文件！请先在编辑器里生成！");
            return new Dictionary<string, string>();
        }

        string encryptedString = File.ReadAllText(filePath);
        var data = DataEncryptionManger.EditorEncryptionTools.DecryptSaveString<GuidMapData>(encryptedString, SECRET_KEY);

        return data?.guidDictionary ?? new Dictionary<string, string>();
    }
}

// 用于序列化的 GUID 映射数据类
[System.Serializable]
public class GuidMapData
{
    public Dictionary<string, string> guidDictionary = new Dictionary<string, string>();
}