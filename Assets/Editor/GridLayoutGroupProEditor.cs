using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(GridLayoutGroup))]
[CanEditMultipleObjects]
public class GridLayoutGroupProEditor : Editor
{
    // 核心属性缓存（仅保留必要的）
    private SerializedProperty _cellSize;
    private SerializedProperty _spacing;
    private SerializedProperty _padding; // 实际是RectOffset类型
    private SerializedProperty _startCorner;
    private SerializedProperty _startAxis;
    private SerializedProperty _constraint;
    private SerializedProperty _constraintCount;

    private void OnEnable()
    {
        // 绑定序列化属性（仅核心）
        _cellSize = serializedObject.FindProperty("m_CellSize");
        _spacing = serializedObject.FindProperty("m_Spacing");
        _padding = serializedObject.FindProperty("m_Padding");
        _startCorner = serializedObject.FindProperty("m_StartCorner");
        _startAxis = serializedObject.FindProperty("m_StartAxis");
        _constraint = serializedObject.FindProperty("m_Constraint");
        _constraintCount = serializedObject.FindProperty("m_ConstraintCount");
    }

    // 场景视图绘制（简化+修复padding访问）
    private void OnSceneGUI()
    {
        var grid = target as GridLayoutGroup;
        if (grid == null || !Selection.activeGameObject) return;

        var rectTrans = grid.GetComponent<RectTransform>();
        if (rectTrans == null || Selection.activeGameObject != grid.gameObject) return;

        // 修复：正确获取RectOffset（替代原错误的rectValue）
        RectOffset padding = grid.padding;
        Vector2 cellSize = _cellSize.vector2Value;
        Vector2 spacing = _spacing.vector2Value;

        // 简化绘制：只绘制总网格边界（避免循环绘制子网格，提升性能）
        Handles.color = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        Vector3 bottomLeft = rectTrans.TransformPoint(new Vector2(padding.left, padding.bottom));
        Vector3 topRight = rectTrans.TransformPoint(new Vector2(rectTrans.rect.width - padding.right, rectTrans.rect.height - padding.top));
        Handles.DrawWireCube((bottomLeft + topRight) / 2, topRight - bottomLeft);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var grid = target as GridLayoutGroup;
        if (grid == null) return;

        // 核心基础设置（极简）
        EditorGUILayout.LabelField("网格布局核心设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_cellSize, new GUIContent("单元格尺寸"));
        EditorGUILayout.PropertyField(_spacing, new GUIContent("单元格间距"));
        EditorGUILayout.PropertyField(_padding, new GUIContent("内边距")); // 正确显示RectOffset

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("排列设置", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(_startCorner, new GUIContent("起始角"));
        EditorGUILayout.PropertyField(_startAxis, new GUIContent("起始轴"));
        EditorGUILayout.PropertyField(_constraint, new GUIContent("约束模式"));

        // 约束数量（仅非Flexible时显示）
        if (_constraint.enumValueIndex != (int)GridLayoutGroup.Constraint.Flexible)
        {
            EditorGUILayout.PropertyField(_constraintCount, new GUIContent("约束数量"));
        }

        // 快捷刷新按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("刷新布局"))
        {
            var rectTrans = grid.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
                Canvas.ForceUpdateCanvases();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}