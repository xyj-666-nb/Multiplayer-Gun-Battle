using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(GameSkinManager))]
public class GameSkinManagerEditor : Editor
{
    // 统一管理资源路径，后续改路径仅需修改这里
    private const string BulletConfigRootPath = "GameInfo/BulletInfo";
    private const string MuzzleFlashConfigRootPath = "GameInfo/MuzzleFlashInfo";

    private GameSkinManager _targetManager;

    private void OnEnable()
    {
        // 绑定目标管理器对象
        _targetManager = (GameSkinManager)target;
    }

    public override void OnInspectorGUI()
    {
        // 保留原有的Inspector面板所有内容，不破坏原有字段显示
        base.OnInspectorGUI();

        // 分隔美化
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("========================================", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("【自动配置加载工具】", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("点击按钮自动扫描Resources路径，全量同步配置到对应List，无需手动拖拽", MessageType.Info);
        EditorGUILayout.Space();

        // 主按钮：全量加载所有配置
        if (GUILayout.Button("一键加载所有配置", GUILayout.Height(35)))
        {
            LoadAllConfigAssets();
        }

        EditorGUILayout.Space();

        // 辅助按钮：单独加载某一类配置，方便局部更新
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("仅加载子弹配置"))
        {
            LoadBulletConfigAssets();
        }
        if (GUILayout.Button("仅加载火光配置"))
        {
            LoadMuzzleFlashConfigAssets();
        }
        EditorGUILayout.EndHorizontal();
    }

    #region 核心加载逻辑
    /// <summary>
    /// 全量加载所有配置（主入口）
    /// </summary>
    private void LoadAllConfigAssets()
    {
        // 注册撤销操作，支持Ctrl+Z回退
        Undo.RecordObject(_targetManager, "一键加载所有枪械配置");

        // 执行加载
        LoadBulletConfigAssets(false);
        LoadMuzzleFlashConfigAssets(false);

        // 标记对象脏数据，确保Unity保存修改
        EditorUtility.SetDirty(_targetManager);
        serializedObject.ApplyModifiedProperties();

        // 控制台反馈加载结果
        Debug.Log($"【配置加载完成】\n子弹配置：{_targetManager.AllBulletVisualConfigList.Count} 个\n 枪口火光配置：{_targetManager.AllMuzzleFlashConfigList.Count} 个");
    }

    /// <summary>
    /// 加载子弹视觉配置
    /// </summary>
    /// <param name="isSingleCall">是否单独调用，单独调用会自动处理脏数据和撤销</param>
    private void LoadBulletConfigAssets(bool isSingleCall = true)
    {
        if (isSingleCall) Undo.RecordObject(_targetManager, "加载子弹配置");

        // 从Resources路径加载对应类型的所有资源
        BulletVisualConfig[] allBulletConfigs = Resources.LoadAll<BulletVisualConfig>(BulletConfigRootPath);

        // 清空原有列表，全量覆盖，确保和资源目录完全同步（无重复、无遗漏）
        _targetManager.AllBulletVisualConfigList = new List<BulletVisualConfig>(allBulletConfigs);

        if (isSingleCall)
        {
            EditorUtility.SetDirty(_targetManager);
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"【子弹配置加载完成】共加载 {allBulletConfigs.Length} 个配置文件");
        }
    }

    /// <summary>
    /// 加载枪口火光配置
    /// </summary>
    /// <param name="isSingleCall">是否单独调用，单独调用会自动处理脏数据和撤销</param>
    private void LoadMuzzleFlashConfigAssets(bool isSingleCall = true)
    {
        if (isSingleCall) Undo.RecordObject(_targetManager, "加载枪口火光配置");

        // 从Resources路径加载对应类型的所有资源
        MuzzleFlashConfig[] allFlashConfigs = Resources.LoadAll<MuzzleFlashConfig>(MuzzleFlashConfigRootPath);

        // 清空原有列表，全量覆盖，确保和资源目录完全同步
        _targetManager.AllMuzzleFlashConfigList = new List<MuzzleFlashConfig>(allFlashConfigs);

        if (isSingleCall)
        {
            EditorUtility.SetDirty(_targetManager);
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"【枪口火光配置加载完成】共加载 {allFlashConfigs.Length} 个配置文件");
        }
    }
    #endregion
}