using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(VerticalLayoutGroup))]
[CanEditMultipleObjects]
public class VerticalLayoutGroupProEditor : Editor
{
    // 仅保留核心布局属性
    private SerializedProperty _childAlignment;
    private SerializedProperty _spacing;
    private SerializedProperty _padding;
    private SerializedProperty _childControlWidth;
    private SerializedProperty _childControlHeight;
    private SerializedProperty _childForceExpandWidth;
    private SerializedProperty _childForceExpandHeight;

    private void OnEnable()
    {
        // 安全获取属性，避免空引用
        serializedObject.UpdateIfRequiredOrScript();
        _childAlignment = TryGetProperty("m_ChildAlignment");
        _spacing = TryGetProperty("m_Spacing");
        _padding = TryGetProperty("m_Padding");
        _childControlWidth = TryGetProperty("m_ChildControlWidth");
        _childControlHeight = TryGetProperty("m_ChildControlHeight");
        _childForceExpandWidth = TryGetProperty("m_ChildForceExpandWidth");
        _childForceExpandHeight = TryGetProperty("m_ChildForceExpandHeight");
    }

    // 场景视图绘制（兼容所有Unity版本：手动画四条边）
    private void OnSceneGUI()
    {
        var layout = target as VerticalLayoutGroup;
        if (layout == null || Selection.activeGameObject != layout.gameObject) return;

        var rectTrans = layout.GetComponent<RectTransform>();
        if (rectTrans == null) return;

        // 获取矩形四个角的世界坐标（所有版本通用）
        Vector3[] corners = new Vector3[4];
        rectTrans.GetWorldCorners(corners);

        // 手动绘制矩形的四条边（替代DrawWireRect）
        Handles.color = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        Handles.DrawLine(corners[0], corners[1]); // 左下 → 右下
        Handles.DrawLine(corners[1], corners[2]); // 右下 → 右上
        Handles.DrawLine(corners[2], corners[3]); // 右上 → 左上
        Handles.DrawLine(corners[3], corners[0]); // 左上 → 左下
    }

    public override void OnInspectorGUI()
    {
        // 基础空值防护
        var layout = target as VerticalLayoutGroup;
        if (layout == null) return;

        serializedObject.Update();

        // 仅保留核心布局设置
        EditorGUILayout.LabelField("垂直布局核心设置", EditorStyles.boldLabel);
        DrawPropertyIfNotNull(_childAlignment, "子对象对齐");
        DrawPropertyIfNotNull(_spacing, "子对象间距");
        DrawPropertyIfNotNull(_padding, "内边距");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("尺寸控制", EditorStyles.miniBoldLabel);
        DrawPropertyIfNotNull(_childControlWidth, "控制子对象宽度");
        DrawPropertyIfNotNull(_childControlHeight, "控制子对象高度");
        DrawPropertyIfNotNull(_childForceExpandWidth, "强制扩展宽度");
        DrawPropertyIfNotNull(_childForceExpandHeight, "强制扩展高度");

        // 快捷刷新布局按钮
        EditorGUILayout.Space();
        var rectTrans = layout.GetComponent<RectTransform>();
        if (GUILayout.Button("刷新布局") && rectTrans != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
            Canvas.ForceUpdateCanvases();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 工具方法：安全获取SerializedProperty，避免null
    private SerializedProperty TryGetProperty(string propertyName)
    {
        var prop = serializedObject.FindProperty(propertyName);
        return prop == null ? null : prop;
    }

    // 工具方法：仅当属性非空时绘制，避免空引用报错
    private void DrawPropertyIfNotNull(SerializedProperty prop, string label)
    {
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }
    }
}