using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[CustomEditor(typeof(ExpressionSystem))]
public class ExpressionSystemEditor : Editor
{
    private ExpressionSystem _targetSystem;

    // 表情数据文件夹路径
    private const string ExpressionDataFolderPath = "Assets/Resources/GameInfo/ExpressionDataInfo";

    private void OnEnable()
    {
        // 绑定目标对象
        _targetSystem = (ExpressionSystem)target;
    }

    public override void OnInspectorGUI()
    {
        // 保留原有的Inspector面板所有内容，不破坏原有字段显示
        base.OnInspectorGUI();

        // 分隔美化
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("========================================", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("【表情系统工具集】", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 1. 一键加载所有表情数据
        EditorGUILayout.HelpBox($"从文件夹加载所有表情：\n{ExpressionDataFolderPath}", MessageType.Info);
        if (GUILayout.Button(" 1. 一键加载所有表情数据", GUILayout.Height(25)))
        {
            LoadAllExpressionPacksFromFolder();
        }

        EditorGUILayout.Space(5);

        // 2. 一键自动生成ID
        EditorGUILayout.HelpBox("为表情列表生成唯一ID（从1开始递增）", MessageType.Info);
        if (GUILayout.Button(" 2. 一键自动生成表情ID", GUILayout.Height(25)))
        {
            AutoGenerateExpressionIDs();
        }

        EditorGUILayout.Space(5);

        // 3. 一键清空列表
        EditorGUILayout.HelpBox("清空当前表情列表（不会删除源文件）", MessageType.Warning);
        if (GUILayout.Button(" 清空当前列表", GUILayout.Height(25)))
        {
            ClearExpressionList();
        }
    }

    /// <summary>
    /// 【新增】从文件夹加载所有表情数据
    /// </summary>
    private void LoadAllExpressionPacksFromFolder()
    {
        // 检查文件夹是否存在
        if (!Directory.Exists(ExpressionDataFolderPath))
        {
            Debug.LogError($"表情数据文件夹不存在：{ExpressionDataFolderPath}");
            return;
        }

        // 初始化列表（如果为空）
        if (_targetSystem.ExpressionPackList == null)
        {
            _targetSystem.ExpressionPackList = new System.Collections.Generic.List<ExpressionPack>();
        }

        // 注册撤销操作
        Undo.RecordObject(_targetSystem, "加载所有表情数据");

        // 搜索文件夹下所有 ExpressionPack
        string[] allGuids = AssetDatabase.FindAssets("t:ExpressionPack", new[] { ExpressionDataFolderPath });

        if (allGuids.Length == 0)
        {
            Debug.LogWarning($"在文件夹中未找到任何表情数据：{ExpressionDataFolderPath}");
            return;
        }

        int addCount = 0;
        int skipCount = 0;

        foreach (string guid in allGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ExpressionPack pack = AssetDatabase.LoadAssetAtPath<ExpressionPack>(assetPath);

            if (pack == null) continue;

            // 去重：检查是否已在列表中
            if (!_targetSystem.ExpressionPackList.Contains(pack))
            {
                _targetSystem.ExpressionPackList.Add(pack);
                addCount++;
                Debug.Log($"添加表情：{pack.name} (ID: {pack.ExpressionID})");
            }
            else
            {
                skipCount++;
            }
        }

        // 标记脏数据
        EditorUtility.SetDirty(_targetSystem);
        serializedObject.ApplyModifiedProperties();

        // 反馈结果
        Debug.Log($"========== 表情加载完成 ==========\n新增: {addCount} | 跳过(重复): {skipCount} | 总计: {_targetSystem.ExpressionPackList.Count}");
    }

    /// <summary>
    /// 自动生成表情ID的核心逻辑
    /// </summary>
    private void AutoGenerateExpressionIDs()
    {
        // 空列表保护
        if (_targetSystem.ExpressionPackList == null || _targetSystem.ExpressionPackList.Count == 0)
        {
            Debug.LogWarning("表情列表为空，无法生成ID，请先添加表情");
            return;
        }

        // 注册撤销操作，支持Ctrl+Z一键回退
        Undo.RecordObject(_targetSystem, "自动生成表情ID");

        // 从1开始，逐个赋值ID
        for (int i = 0; i < _targetSystem.ExpressionPackList.Count; i++)
        {
            var pack = _targetSystem.ExpressionPackList[i];
            if (pack != null)
            {
                pack.ExpressionID = i + 1; // ID从1开始递增
                // 标记Pack本身也脏了，确保ID保存到源文件
                EditorUtility.SetDirty(pack);
            }
        }

        // 标记对象脏数据，确保Unity保存修改
        EditorUtility.SetDirty(_targetSystem);
        serializedObject.ApplyModifiedProperties();

        // 控制台反馈生成结果
        Debug.Log($" 表情ID生成完成！共生成 {_targetSystem.ExpressionPackList.Count} 个ID（从1开始）");
    }

    /// <summary>
    /// 【新增】清空表情列表
    /// </summary>
    private void ClearExpressionList()
    {
        if (_targetSystem.ExpressionPackList == null || _targetSystem.ExpressionPackList.Count == 0)
        {
            Debug.Log("表情列表已经是空的");
            return;
        }

        // 注册撤销
        Undo.RecordObject(_targetSystem, "清空表情列表");

        _targetSystem.ExpressionPackList.Clear();

        // 标记脏数据
        EditorUtility.SetDirty(_targetSystem);
        serializedObject.ApplyModifiedProperties();

        Debug.Log("表情列表已清空");
    }
}