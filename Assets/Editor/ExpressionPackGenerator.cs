using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ExpressionPackGenerator : EditorWindow
{
    // 路径配置
    private const string SpriteFolderPath = "Assets/Resources/Sprite/Expression";
    private const string SoFolderPath = "Assets/Resources/GameInfo/ExpressionDataInfo";

    // 统计信息
    private int _newCreatedCount = 0;
    private int _updatedCount = 0;
    private int _skippedCount = 0;
    private string _logText = "";

    [MenuItem("Tools/表情包批量生成器")]
    public static void ShowWindow()
    {
        GetWindow<ExpressionPackGenerator>("表情包生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("表情包批量生成工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label($"图片源路径: {SpriteFolderPath}");
        GUILayout.Label($"SO生成路径: {SoFolderPath}");
        GUILayout.Space(10);

        if (GUILayout.Button("开始扫描并生成", GUILayout.Height(40)))
        {
            GenerateExpressionPacks();
        }

        GUILayout.Space(20);
        GUILayout.Label("统计信息:", EditorStyles.boldLabel);
        GUILayout.Label($"新增: {_newCreatedCount} | 更新: {_updatedCount} | 跳过: {_skippedCount}");

        GUILayout.Space(10);
        GUILayout.Label("详细日志:", EditorStyles.boldLabel);

        // 滚动日志
        EditorGUILayout.TextArea(_logText, GUILayout.ExpandHeight(true));
    }

    private void GenerateExpressionPacks()
    {
        // 重置统计
        _newCreatedCount = 0;
        _updatedCount = 0;
        _skippedCount = 0;
        _logText = "";

        // 1. 确保文件夹存在
        EnsureFolderExists(SpriteFolderPath);
        EnsureFolderExists(SoFolderPath);

        // 2. 扫描现有的ExpressionPack，建立字典（去重用）
        Dictionary<string, ExpressionPack> existingPacks = ScanExistingPacks();
        Dictionary<int, ExpressionPack> idToPackMap = existingPacks.Values.ToDictionary(p => p.ExpressionID);

        // 3. 计算下一个可用的唯一ID（从现有最大ID+1开始）
        int nextId = 1;
        if (idToPackMap.Count > 0)
        {
            nextId = idToPackMap.Keys.Max() + 1;
        }
        AddLog($"初始ID计数器: {nextId}");

        // 4. 扫描源图片文件夹
        string[] allSpriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { SpriteFolderPath });

        if (allSpriteGuids.Length == 0)
        {
            AddLog("警告: 未找到任何表情图片！请检查路径是否正确。");
            return;
        }

        AddLog($"找到 {allSpriteGuids.Length} 张图片，开始处理...");

        foreach (string guid in allSpriteGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null)
            {
                AddLog($"跳过: {Path.GetFileName(assetPath)} 不是有效的Sprite");
                _skippedCount++;
                continue;
            }

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(assetPath);

            // 5. 检查是否已存在（通过文件名匹配）
            if (existingPacks.TryGetValue(fileNameWithoutExt, out ExpressionPack existingPack))
            {
                // 已存在，检查是否需要更新图片
                if (existingPack.ExpressionSprite != sprite)
                {
                    existingPack.ExpressionSprite = sprite;
                    EditorUtility.SetDirty(existingPack);
                    AddLog($"更新: {fileNameWithoutExt} (ID: {existingPack.ExpressionID}) - 图片已更新");
                    _updatedCount++;
                }
                else
                {
                    AddLog($"跳过: {fileNameWithoutExt} (ID: {existingPack.ExpressionID}) - 已存在且无需更新");
                    _skippedCount++;
                }
            }
            else
            {
                // 6. 不存在，创建新的
                int newId = GetNextAvailableId(ref nextId, idToPackMap);
                CreateNewExpressionPack(fileNameWithoutExt, sprite, newId, idToPackMap);
                _newCreatedCount++;
                AddLog($"创建: {fileNameWithoutExt} (ID: {newId})");
            }
        }

        // 7. 保存所有修改
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddLog("");
        AddLog("========== 处理完成 ==========");
        AddLog($"总计: {allSpriteGuids.Length} 张图片");
        AddLog($"新增: {_newCreatedCount} | 更新: {_updatedCount} | 跳过: {_skippedCount}");
    }

    /// <summary>
    /// 扫描现有的ExpressionPack
    /// </summary>
    private Dictionary<string, ExpressionPack> ScanExistingPacks()
    {
        Dictionary<string, ExpressionPack> dict = new Dictionary<string, ExpressionPack>();

        string[] allSoGuids = AssetDatabase.FindAssets("t:ExpressionPack", new[] { SoFolderPath });

        foreach (string guid in allSoGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ExpressionPack pack = AssetDatabase.LoadAssetAtPath<ExpressionPack>(path);

            if (pack != null && !string.IsNullOrEmpty(pack.name))
            {
                dict[pack.name] = pack;
            }
        }

        AddLog($"扫描到 {dict.Count} 个已存在的ExpressionPack");
        return dict;
    }

    /// <summary>
    /// 创建新的ExpressionPack
    /// </summary>
    private void CreateNewExpressionPack(string fileName, Sprite sprite, int id, Dictionary<int, ExpressionPack> idMap)
    {
        ExpressionPack newPack = ScriptableObject.CreateInstance<ExpressionPack>();
        newPack.ExpressionSprite = sprite;
        newPack.ExpressionID = id;

        string soPath = Path.Combine(SoFolderPath, $"{fileName}.asset");
        AssetDatabase.CreateAsset(newPack, soPath);

        idMap[id] = newPack;
    }

    /// <summary>
    /// 获取下一个可用的唯一ID（防止ID冲突）
    /// </summary>
    private int GetNextAvailableId(ref int nextId, Dictionary<int, ExpressionPack> idMap)
    {
        while (idMap.ContainsKey(nextId))
        {
            nextId++;
        }
        int result = nextId;
        nextId++;
        return result;
    }

    /// <summary>
    /// 确保文件夹存在
    /// </summary>
    private void EnsureFolderExists(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AddLog($"创建文件夹: {folderPath}");
        }
    }

    private void AddLog(string message)
    {
        _logText += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
        /* Debug.Log($"[表情生成器] {message}"); */
    }
}
