using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExpressionSystem))]
public class ExpressionSystemEditor : Editor
{
    private ExpressionSystem _targetSystem;

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
        EditorGUILayout.LabelField("【表情ID自动生成工具】", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("点击按钮自动为表情列表生成唯一ID（从1开始递增）", MessageType.Info);
        EditorGUILayout.Space(5);

        // 核心按钮：一键自动生成ID
        if (GUILayout.Button(" 一键自动生成表情ID", GUILayout.Height(30)))
        {
            AutoGenerateExpressionIDs();
        }
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
            }
        }

        // 标记对象脏数据，确保Unity保存修改
        EditorUtility.SetDirty(_targetSystem);
        serializedObject.ApplyModifiedProperties();

        // 控制台反馈生成结果
        Debug.Log($" 表情ID生成完成！共生成 {_targetSystem.ExpressionPackList.Count} 个ID（从1开始）");
    }
}