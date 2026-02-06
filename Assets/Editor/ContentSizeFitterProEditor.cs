using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ContentSizeFitter))]
[CanEditMultipleObjects]
public class ContentSizeFitterProEditor : Editor
{
    // 核心属性缓存
    private SerializedProperty _horizontalFit;
    private SerializedProperty _verticalFit;

    // 折叠组状态（持久化保存）
    private bool _basicFoldout = true;
    private bool _previewFoldout = true;
    private bool _conflictFoldout = true;
    private bool _advancedFoldout = false;
    private bool _helpFoldout = false;

    // 尺寸缓存（用于实时计算）
    private Vector2 _lastMinSize;
    private Vector2 _lastPreferredSize;
    private DateTime _lastUpdateTime;

    private void OnEnable()
    {
        // 绑定序列化属性
        _horizontalFit = serializedObject.FindProperty("m_HorizontalFit");
        _verticalFit = serializedObject.FindProperty("m_VerticalFit");

        // 加载持久化折叠状态
        string targetId = target.GetInstanceID().ToString();
        _basicFoldout = EditorPrefs.GetBool($"CSF_Editor_{targetId}_Basic", true);
        _previewFoldout = EditorPrefs.GetBool($"CSF_Editor_{targetId}_Preview", true);
        _conflictFoldout = EditorPrefs.GetBool($"CSF_Editor_{targetId}_Conflict", true);
        _advancedFoldout = EditorPrefs.GetBool($"CSF_Editor_{targetId}_Advanced", false);
        _helpFoldout = EditorPrefs.GetBool($"CSF_Editor_{targetId}_Help", false);
    }

    private void OnDisable()
    {
        // 保存折叠状态
        string targetId = target.GetInstanceID().ToString();
        EditorPrefs.SetBool($"CSF_Editor_{targetId}_Basic", _basicFoldout);
        EditorPrefs.SetBool($"CSF_Editor_{targetId}_Preview", _previewFoldout);
        EditorPrefs.SetBool($"CSF_Editor_{targetId}_Conflict", _conflictFoldout);
        EditorPrefs.SetBool($"CSF_Editor_{targetId}_Advanced", _advancedFoldout);
        EditorPrefs.SetBool($"CSF_Editor_{targetId}_Help", _helpFoldout);
    }

    // 场景视图绘制适配边界
    private void OnSceneGUI()
    {
        var csf = target as ContentSizeFitter;
        if (csf == null) return;

        var rectTrans = csf.GetComponent<RectTransform>();
        if (rectTrans == null) return;

        // 仅在选中时绘制
        if (Selection.activeGameObject != csf.gameObject) return;

        // 修复：指定axis参数（0=水平，1=垂直）
        float minWidth = LayoutUtility.GetMinSize(rectTrans, 0);
        float minHeight = LayoutUtility.GetMinSize(rectTrans, 1);
        float preferredWidth = LayoutUtility.GetPreferredSize(rectTrans, 0);
        float preferredHeight = LayoutUtility.GetPreferredSize(rectTrans, 1);

        Vector2 minSize = new Vector2(minWidth, minHeight);
        Vector2 preferredSize = new Vector2(preferredWidth, preferredHeight);

        // 绘制MinSize边界（红色虚线）
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        Handles.DrawDottedLine(rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin, rectTrans.rect.yMin)),
                               rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin + minSize.x, rectTrans.rect.yMin)), 5f);
        Handles.DrawDottedLine(rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin, rectTrans.rect.yMin)),
                               rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin, rectTrans.rect.yMin + minSize.y)), 5f);

        // 绘制PreferredSize边界（蓝色虚线）
        Handles.color = new Color(0.3f, 0.3f, 1f, 0.5f);
        Handles.DrawDottedLine(rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin, rectTrans.rect.yMin)),
                               rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin + preferredSize.x, rectTrans.rect.yMin)), 5f);
        Handles.DrawDottedLine(rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin, rectTrans.rect.yMin)),
                               rectTrans.TransformPoint(new Vector2(rectTrans.rect.xMin, rectTrans.rect.yMin + preferredSize.y)), 5f);

        // 绘制当前实际尺寸（绿色实线）
        Handles.color = new Color(0.3f, 1f, 0.3f, 0.7f);
        Vector3[] corners = new Vector3[4];
        rectTrans.GetWorldCorners(corners);
        Handles.DrawLine(corners[0], corners[1]);
        Handles.DrawLine(corners[1], corners[2]);
        Handles.DrawLine(corners[2], corners[3]);
        Handles.DrawLine(corners[3], corners[0]);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var csf = target as ContentSizeFitter;
        if (csf == null) return;
        var rectTrans = csf.GetComponent<RectTransform>();

        // -------------------------- 顶部标题栏 --------------------------
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("自动调整RectTransform尺寸以适配内容", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);

        // -------------------------- 基础设置区 --------------------------
        _basicFoldout = EditorGUILayout.Foldout(_basicFoldout, " 基础设置", true);
        if (_basicFoldout)
        {
            EditorGUI.indentLevel++;

            // 适配模式选择（带详细说明）
            EditorGUILayout.PropertyField(_horizontalFit, new GUIContent(
                "水平适配模式",
                "Unconstrained=不限制（使用RectTransform原始尺寸）\n" +
                "MinSize=最小尺寸（取子对象的最小宽度）\n" +
                "PreferredSize=首选尺寸（取子对象的首选宽度）"));

            EditorGUILayout.PropertyField(_verticalFit, new GUIContent(
                "垂直适配模式",
                "Unconstrained=不限制（使用RectTransform原始尺寸）\n" +
                "MinSize=最小尺寸（取子对象的最小高度）\n" +
                "PreferredSize=首选尺寸（取子对象的首选高度）"));

            // 快捷操作按钮组（改为3行分布：2、2、1）
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("快捷切换", EditorStyles.miniBoldLabel);

            // 第1行：仅水平适配 | 仅垂直适配（2个按钮）
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("仅水平适配", GUILayout.Width(100)))
            {
                _horizontalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.PreferredSize;
                _verticalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.Unconstrained;
            }
            if (GUILayout.Button("仅垂直适配", GUILayout.Width(100)))
            {
                _horizontalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.Unconstrained;
                _verticalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.PreferredSize;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2); // 行间距

            // 第2行：全适配 | 重置水平（2个按钮）
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全适配", GUILayout.Width(100)))
            {
                _horizontalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.PreferredSize;
                _verticalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.PreferredSize;
            }
            if (GUILayout.Button("重置水平", GUILayout.Width(100)))
            {
                _horizontalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.Unconstrained;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2); // 行间距

            // 第3行：重置垂直（1个按钮）
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重置垂直", GUILayout.Width(100)))
            {
                _verticalFit.enumValueIndex = (int)ContentSizeFitter.FitMode.Unconstrained;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 实时预览区 --------------------------
        _previewFoldout = EditorGUILayout.Foldout(_previewFoldout, "实时尺寸预览", true);
        if (_previewFoldout && rectTrans != null)
        {
            EditorGUI.indentLevel++;

            // 强制刷新尺寸计算
            if (GUILayout.Button("刷新尺寸计算", GUILayout.Width(120)))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
                _lastUpdateTime = DateTime.Now;
            }

            // 修复：指定axis参数分别获取宽高
            float minWidth = LayoutUtility.GetMinSize(rectTrans, 0);
            float minHeight = LayoutUtility.GetMinSize(rectTrans, 1);
            float preferredWidth = LayoutUtility.GetPreferredSize(rectTrans, 0);
            float preferredHeight = LayoutUtility.GetPreferredSize(rectTrans, 1);

            Vector2 minSize = new Vector2(minWidth, minHeight);
            Vector2 preferredSize = new Vector2(preferredWidth, preferredHeight);
            Vector2 currentSize = rectTrans.rect.size;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("当前实际尺寸", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"宽度：{currentSize.x:F1}px | 高度：{currentSize.y:F1}px");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("计算出的尺寸", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"最小尺寸：{minSize.x:F1}px × {minSize.y:F1}px");
            EditorGUILayout.LabelField($"首选尺寸：{preferredSize.x:F1}px × {preferredSize.y:F1}px");

            // 显示尺寸变化提示
            if (minSize != _lastMinSize || preferredSize != _lastPreferredSize)
            {
                EditorGUILayout.HelpBox($" 尺寸已更新（{_lastUpdateTime:HH:mm:ss}）", MessageType.Info);
                _lastMinSize = minSize;
                _lastPreferredSize = preferredSize;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 冲突检测区 --------------------------
        _conflictFoldout = EditorGUILayout.Foldout(_conflictFoldout, "布局冲突检测", true);
        if (_conflictFoldout && rectTrans != null)
        {
            EditorGUI.indentLevel++;
            List<string> conflicts = new List<string>();

            // 检测1：固定宽高与适配模式冲突
            if (_horizontalFit.enumValueIndex != (int)ContentSizeFitter.FitMode.Unconstrained &&
                rectTrans.anchorMin.x == rectTrans.anchorMax.x)
            {
                conflicts.Add("水平适配与固定宽度冲突（锚点左右重合）");
            }
            if (_verticalFit.enumValueIndex != (int)ContentSizeFitter.FitMode.Unconstrained &&
                rectTrans.anchorMin.y == rectTrans.anchorMax.y)
            {
                conflicts.Add("垂直适配与固定高度冲突（锚点上下重合）");
            }

            // 检测2：父对象存在LayoutGroup导致冲突
            var parentLayout = rectTrans.parent.GetComponent<LayoutGroup>();
            if (parentLayout != null)
            {
                conflicts.Add($"父对象存在 {parentLayout.GetType().Name}，可能导致尺寸计算异常");
            }

            // 检测3：子对象缺少LayoutElement
            bool hasLayoutElement = false;
            for (int i = 0; i < rectTrans.childCount; i++)
            {
                if (rectTrans.GetChild(i).GetComponent<LayoutElement>() != null)
                {
                    hasLayoutElement = true;
                    break;
                }
            }
            if (!hasLayoutElement && rectTrans.childCount > 0)
            {
                conflicts.Add("子对象缺少LayoutElement，可能导致尺寸计算不准确");
            }

            // 显示冲突结果
            if (conflicts.Count > 0)
            {
                EditorGUILayout.HelpBox($"检测到 {conflicts.Count} 个冲突：\n" + string.Join("\n", conflicts), MessageType.Warning);

                // 修复建议
                if (GUILayout.Button("尝试自动修复", GUILayout.Width(120)))
                {
                    AutoFixConflicts(csf, rectTrans);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未检测到布局冲突", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 高级选项区 --------------------------
        _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "高级选项", true);
        if (_advancedFoldout)
        {
            EditorGUI.indentLevel++;

            // 批量操作
            if (targets.Length > 1)
            {
                EditorGUILayout.LabelField("批量操作", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("同步所有选中对象的适配模式", GUILayout.Width(200)))
                {
                    foreach (var t in targets)
                    {
                        var targetCsf = t as ContentSizeFitter;
                        if (targetCsf == null) continue;

                        var so = new SerializedObject(targetCsf);
                        so.FindProperty("m_HorizontalFit").enumValueIndex = _horizontalFit.enumValueIndex;
                        so.FindProperty("m_VerticalFit").enumValueIndex = _verticalFit.enumValueIndex;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // 调试模式
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("调试工具", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("强制重建整个UI布局", GUILayout.Width(200)))
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
                SceneView.RepaintAll();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 帮助信息区 --------------------------
        _helpFoldout = EditorGUILayout.Foldout(_helpFoldout, "帮助与常见问题", true);
        if (_helpFoldout)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox(
                "使用提示：\n" +
                "1. ContentSizeFitter 仅对直接子对象的尺寸进行计算\n" +
                "2. 若子对象是 Text，会自动计算文本的首选宽度和高度\n" +
                "3. 建议为动态尺寸的子对象添加 LayoutElement，明确 MinSize 和 PreferredSize\n" +
                "4. 与 LayoutGroup 一起使用时，需确保父对象的 LayoutGroup 不会覆盖当前适配",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("常见问题", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("为什么ContentSizeFitter不生效？", GUILayout.Width(200)))
            {
                EditorUtility.DisplayDialog(
                    "排查步骤",
                    "1. 检查RectTransform的锚点是否为拉伸（左右/上下锚点不重合）\n" +
                    "2. 确保子对象有有效的LayoutElement或可计算尺寸的组件（如Text、Image）\n" +
                    "3. 检查父对象是否有LayoutGroup，可能导致尺寸被覆盖\n" +
                    "4. 点击「强制重建整个UI布局」按钮刷新计算",
                    "确定");
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 自动修复常见冲突
    private void AutoFixConflicts(ContentSizeFitter csf, RectTransform rectTrans)
    {
        Undo.RecordObject(rectTrans, "Auto Fix ContentSizeFitter Conflicts");

        // 修复1：固定宽高冲突 → 调整锚点为拉伸
        if (_horizontalFit.enumValueIndex != (int)ContentSizeFitter.FitMode.Unconstrained &&
            rectTrans.anchorMin.x == rectTrans.anchorMax.x)
        {
            rectTrans.anchorMin = new Vector2(rectTrans.anchorMin.x, rectTrans.anchorMin.y);
            rectTrans.anchorMax = new Vector2(rectTrans.anchorMin.x + 0.1f, rectTrans.anchorMax.y);
            rectTrans.offsetMin = new Vector2(0, rectTrans.offsetMin.y);
            rectTrans.offsetMax = new Vector2(0, rectTrans.offsetMax.y);
        }
        if (_verticalFit.enumValueIndex != (int)ContentSizeFitter.FitMode.Unconstrained &&
            rectTrans.anchorMin.y == rectTrans.anchorMax.y)
        {
            rectTrans.anchorMin = new Vector2(rectTrans.anchorMin.x, rectTrans.anchorMin.y);
            rectTrans.anchorMax = new Vector2(rectTrans.anchorMax.x, rectTrans.anchorMin.y + 0.1f);
            rectTrans.offsetMin = new Vector2(rectTrans.offsetMin.x, 0);
            rectTrans.offsetMax = new Vector2(rectTrans.offsetMax.x, 0);
        }

        // 修复2：为子对象添加LayoutElement
        for (int i = 0; i < rectTrans.childCount; i++)
        {
            var child = rectTrans.GetChild(i);
            if (child.GetComponent<LayoutElement>() == null)
            {
                Undo.AddComponent<LayoutElement>(child.gameObject);
            }
        }

        // 刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
        SceneView.RepaintAll();
    }
}