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
    private const string GoodSoFolderPath = "Assets/Resources/GameInfo/GoodInfo";

    // 配置常量
    private const int ExpressionsPerBundle = 4; // 每个捆绑包包含4个表情

    // 统计信息
    private int _newCreatedCount = 0;
    private int _updatedCount = 0;
    private int _skippedCount = 0;
    private int _goodsCreatedCount = 0;
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

        // 显示选中的对象信息
        Object selectedObject = Selection.activeObject;
        GUILayout.Label($"当前选中: {(selectedObject != null ? selectedObject.name : "未选中")}");
        GUILayout.Space(10);

        GUILayout.Label($"表情图片源路径: {SpriteFolderPath}");
        GUILayout.Label($"ExpressionPack生成路径: {SoFolderPath}");
        GUILayout.Label($"GoodsData生成路径: {GoodSoFolderPath}");
        GUILayout.Label($"每个捆绑包包含: {ExpressionsPerBundle} 个表情");
        GUILayout.Space(10);

        // 处理选中图片按钮
        if (selectedObject != null && (selectedObject is Texture2D || selectedObject is Sprite))
        {
            if (GUILayout.Button("注册选中图片的所有切片并生成商品", GUILayout.Height(40)))
            {
                ProcessSelectedImage(selectedObject);
            }
        }
        else
        {
            GUILayout.Label("请先在Project窗口选中一张图片或图集！", EditorStyles.helpBox);
        }

        GUILayout.Space(15);

        // 原有批量扫描按钮
        if (GUILayout.Button("扫描整个文件夹生成表情", GUILayout.Height(30)))
        {
            GenerateExpressionPacks();
        }

        // 新增：单独生成商品按钮
        if (GUILayout.Button("将所有已有表情转为商品(4个一组)", GUILayout.Height(30)))
        {
            GenerateGoodsFromAllExistingExpressions();
        }

        GUILayout.Space(20);
        GUILayout.Label("统计信息:", EditorStyles.boldLabel);
        GUILayout.Label($"表情: 新增{_newCreatedCount} | 更新{_updatedCount} | 跳过{_skippedCount}");
        GUILayout.Label($"商品: 新增{_goodsCreatedCount} 个");

        GUILayout.Space(10);
        GUILayout.Label("详细日志:", EditorStyles.boldLabel);

        // 滚动日志
        EditorGUILayout.TextArea(_logText, GUILayout.ExpandHeight(true));
    }

    /// <summary>
    /// 处理选中的图片：注册所有切片 + 生成商品
    /// </summary>
    private void ProcessSelectedImage(Object selectedObject)
    {
        // 重置统计
        ResetStats();

        // 1. 确保所有文件夹存在
        EnsureAllFoldersExist();

        // 2. 扫描现有数据
        var existingPacks = ScanExistingPacks();
        var idToPackMap = existingPacks.Values.ToDictionary(p => p.ExpressionID);
        var usedExpressionIds = GetAllUsedExpressionIds();

        // 3. 计算下一个可用ID
        int nextId = GetNextAvailableExpressionId(idToPackMap);
        AddLog($"初始表情ID计数器: {nextId}");

        // 4. 获取选中对象的路径
        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
        AddLog($"处理选中资源: {assetPath}");

        // 5. 加载该资源下的所有Sprite（包括切片）
        Sprite[] allSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
            .OfType<Sprite>()
            .ToArray();

        if (allSprites.Length == 0)
        {
            AddLog("错误: 未找到任何Sprite切片！请检查图片导入设置。");
            return;
        }

        AddLog($"找到 {allSprites.Length} 个Sprite切片，开始处理...");

        // 6. 处理每个Sprite，生成ExpressionPack
        List<ExpressionPack> newGeneratedPacks = new List<ExpressionPack>();
        foreach (Sprite sprite in allSprites)
        {
            string spriteName = sprite.name;

            if (existingPacks.TryGetValue(spriteName, out ExpressionPack existingPack))
            {
                // 已存在，检查是否需要更新
                if (existingPack.ExpressionSprite != sprite)
                {
                    existingPack.ExpressionSprite = sprite;
                    EditorUtility.SetDirty(existingPack);
                    AddLog($"更新: {spriteName} (ID: {existingPack.ExpressionID})");
                    _updatedCount++;
                }
                else
                {
                    AddLog($"跳过: {spriteName} (ID: {existingPack.ExpressionID}) - 已存在");
                    _skippedCount++;
                }

                // 如果这个表情还没被任何商品使用，加入待生成列表
                if (!usedExpressionIds.Contains(existingPack.ExpressionID))
                {
                    newGeneratedPacks.Add(existingPack);
                }
            }
            else
            {
                // 创建新的
                int newId = GetNextAvailableId(ref nextId, idToPackMap);
                ExpressionPack newPack = CreateNewExpressionPack(spriteName, sprite, newId, idToPackMap);
                newGeneratedPacks.Add(newPack);
                _newCreatedCount++;
                AddLog($"创建: {spriteName} (ID: {newId})");
            }
        }

        // 7. 按4个一组生成商品
        if (newGeneratedPacks.Count > 0)
        {
            CreateGoodsDataInBundles(newGeneratedPacks);
        }

        // 8. 保存所有修改
        SaveAndRefresh();

        AddLog("");
        AddLog("========== 处理完成 ==========");
        AddLog($"表情切片总计: {allSprites.Length}");
        AddLog($"表情: 新增{_newCreatedCount} | 更新{_updatedCount} | 跳过{_skippedCount}");
        AddLog($"商品: 新增{_goodsCreatedCount} 个");
    }

    /// <summary>
    /// 新增功能：将所有已有且未被使用的表情批量转为商品
    /// </summary>
    private void GenerateGoodsFromAllExistingExpressions()
    {
        ResetStats();
        EnsureAllFoldersExist();

        // 1. 扫描所有表情和已使用的ID
        var allPacks = ScanExistingPacks().Values.OrderBy(p => p.ExpressionID).ToList();
        var usedIds = GetAllUsedExpressionIds();

        // 2. 筛选出未被使用的表情
        var unusedPacks = allPacks.Where(p => !usedIds.Contains(p.ExpressionID)).ToList();

        AddLog($"扫描到总计 {allPacks.Count} 个表情");
        AddLog($"其中 {usedIds.Count} 个已被商品使用");
        AddLog($"剩余 {unusedPacks.Count} 个未使用，开始按{ExpressionsPerBundle}个一组生成商品...");

        if (unusedPacks.Count == 0)
        {
            AddLog("没有找到未被使用的表情，无需生成新商品。");
            return;
        }

        // 3. 按4个一组生成商品
        CreateGoodsDataInBundles(unusedPacks);

        SaveAndRefresh();

        AddLog("");
        AddLog("========== 处理完成 ==========");
        AddLog($"共生成 {_goodsCreatedCount} 个新商品");
    }

    /// <summary>
    /// 按4个一组生成GoodsData商品
    /// </summary>
    private void CreateGoodsDataInBundles(List<ExpressionPack> expressionPacks)
    {
        if (expressionPacks.Count == 0) return;

        // 按ID排序，保证顺序一致
        var sortedPacks = expressionPacks.OrderBy(p => p.ExpressionID).ToList();

        // 计算需要生成多少个商品
        int totalBundles = Mathf.CeilToInt((float)sortedPacks.Count / ExpressionsPerBundle);
        int nextBundleIndex = GetNextExpressionBundleIndex();

        AddLog($"将 {sortedPacks.Count} 个表情分为 {totalBundles} 个捆绑包");

        for (int i = 0; i < totalBundles; i++)
        {
            // 截取当前组的表情
            int startIndex = i * ExpressionsPerBundle;
            int count = Mathf.Min(ExpressionsPerBundle, sortedPacks.Count - startIndex);
            var bundlePacks = sortedPacks.GetRange(startIndex, count);

            // 创建商品
            string goodsName = $"表情捆绑包{nextBundleIndex}";
            CreateSingleGoodsData(goodsName, bundlePacks);

            nextBundleIndex++;
            _goodsCreatedCount++;
        }
    }

    /// <summary>
    /// 创建单个GoodsData商品
    /// </summary>
    private void CreateSingleGoodsData(string goodsName, List<ExpressionPack> expressionPacks)
    {
        GoodsData newGoods = ScriptableObject.CreateInstance<GoodsData>();

        // 基础属性
        newGoods.goodsName = goodsName;
        newGoods.goodsPrice = 0; // 默认价格，可自行修改
        newGoods.skinType = SkinType.Expression;
        newGoods.quality = GoodsQuality.Normal; // 固定普通品质

        // UI展示：用第一个表情作为图标
        if (expressionPacks.Count > 0 && expressionPacks[0].ExpressionSprite != null)
        {
            newGoods.goodsIcon = expressionPacks[0].ExpressionSprite;
        }

        // 描述
        newGoods.goodsDescription = $"包含{expressionPacks.Count}个精选表情";

        // 关联数据
        newGoods.expressionPacks = new List<ExpressionPack>(expressionPacks);

        // 生成GUID
        newGoods.goodsGuid = System.Guid.NewGuid().ToString();

        // 保存
        string soPath = Path.Combine(GoodSoFolderPath, $"{goodsName}.asset");
        AssetDatabase.CreateAsset(newGoods, soPath);

        AddLog($"创建商品: {goodsName} (包含表情ID: {string.Join(", ", expressionPacks.Select(p => p.ExpressionID))})");
    }

    /// <summary>
    /// 获取所有已经被商品使用的表情ID
    /// </summary>
    private HashSet<int> GetAllUsedExpressionIds()
    {
        HashSet<int> usedIds = new HashSet<int>();
        string[] allGoodGuids = AssetDatabase.FindAssets("t:GoodsData", new[] { GoodSoFolderPath });

        foreach (string guid in allGoodGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);

            if (goods != null && goods.skinType == SkinType.Expression && goods.expressionPacks != null)
            {
                foreach (var pack in goods.expressionPacks)
                {
                    if (pack != null)
                    {
                        usedIds.Add(pack.ExpressionID);
                    }
                }
            }
        }

        return usedIds;
    }

    // --- 以下是原有方法，保持不变 ---

    private void GenerateExpressionPacks()
    {
        ResetStats();
        EnsureAllFoldersExist();

        var existingPacks = ScanExistingPacks();
        var idToPackMap = existingPacks.Values.ToDictionary(p => p.ExpressionID);
        int nextId = GetNextAvailableExpressionId(idToPackMap);

        AddLog($"初始表情ID计数器: {nextId}");

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

            if (existingPacks.TryGetValue(fileNameWithoutExt, out ExpressionPack existingPack))
            {
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
                int newId = GetNextAvailableId(ref nextId, idToPackMap);
                CreateNewExpressionPack(fileNameWithoutExt, sprite, newId, idToPackMap);
                _newCreatedCount++;
                AddLog($"创建: {fileNameWithoutExt} (ID: {newId})");
            }
        }

        SaveAndRefresh();

        AddLog("");
        AddLog("========== 处理完成 ==========");
        AddLog($"总计: {allSpriteGuids.Length} 张图片");
        AddLog($"表情: 新增{_newCreatedCount} | 更新{_updatedCount} | 跳过{_skippedCount}");
    }

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

    private ExpressionPack CreateNewExpressionPack(string fileName, Sprite sprite, int id, Dictionary<int, ExpressionPack> idMap)
    {
        ExpressionPack newPack = ScriptableObject.CreateInstance<ExpressionPack>();
        newPack.ExpressionSprite = sprite;
        newPack.ExpressionID = id;

        string soPath = Path.Combine(SoFolderPath, $"{fileName}.asset");
        AssetDatabase.CreateAsset(newPack, soPath);

        idMap[id] = newPack;
        return newPack;
    }

    private int GetNextAvailableExpressionId(Dictionary<int, ExpressionPack> idMap)
    {
        return idMap.Count > 0 ? idMap.Keys.Max() + 1 : 1;
    }

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

    private int GetNextExpressionBundleIndex()
    {
        int maxIndex = 0;
        string[] allGoodGuids = AssetDatabase.FindAssets("t:GoodsData", new[] { GoodSoFolderPath });

        foreach (string guid in allGoodGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);

            if (goods != null && goods.skinType == SkinType.Expression)
            {
                if (goods.goodsName.StartsWith("表情捆绑包"))
                {
                    string indexStr = goods.goodsName.Replace("表情捆绑包", "");
                    if (int.TryParse(indexStr, out int index) && index > maxIndex)
                    {
                        maxIndex = index;
                    }
                }
            }
        }

        return maxIndex + 1;
    }

    private void EnsureAllFoldersExist()
    {
        EnsureFolderExists(SpriteFolderPath);
        EnsureFolderExists(SoFolderPath);
        EnsureFolderExists(GoodSoFolderPath);
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AddLog($"创建文件夹: {folderPath}");
        }
    }

    private void ResetStats()
    {
        _newCreatedCount = 0;
        _updatedCount = 0;
        _skippedCount = 0;
        _goodsCreatedCount = 0;
        _logText = "";
    }

    private void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void AddLog(string message)
    {
        _logText += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
    }
}