using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(LayoutElement))]
[CanEditMultipleObjects]
public class LayoutElementProEditor : Editor
{
    // 核心属性缓存
    private SerializedProperty _minWidth;
    private SerializedProperty _minHeight;
    private SerializedProperty _preferredWidth;
    private SerializedProperty _preferredHeight;
    private SerializedProperty _flexibleWidth;
    private SerializedProperty _flexibleHeight;
    private SerializedProperty _ignoreLayout;

    // 折叠组状态（持久化保存）
    private bool _basicFoldout = true;
    private bool _presetFoldout = true;
    private bool _conflictFoldout = true;
    private bool _advancedFoldout = false;
    private bool _helpFoldout = false;

    // 尺寸缓存
    private Vector2 _lastMinSize;
    private Vector2 _lastPreferredSize;
    private DateTime _lastUpdateTime;

    private void OnEnable()
    {
        // 绑定序列化属性
        _minWidth = serializedObject.FindProperty("m_MinWidth");
        _minHeight = serializedObject.FindProperty("m_MinHeight");
        _preferredWidth = serializedObject.FindProperty("m_PreferredWidth");
        _preferredHeight = serializedObject.FindProperty("m_PreferredHeight");
        _flexibleWidth = serializedObject.FindProperty("m_FlexibleWidth");
        _flexibleHeight = serializedObject.FindProperty("m_FlexibleHeight");
        _ignoreLayout = serializedObject.FindProperty("m_IgnoreLayout");

        // 加载持久化折叠状态
        string targetId = target.GetInstanceID().ToString();
        _basicFoldout = EditorPrefs.GetBool($"LE_Editor_{targetId}_Basic", true);
        _presetFoldout = EditorPrefs.GetBool($"LE_Editor_{targetId}_Preset", true);
        _conflictFoldout = EditorPrefs.GetBool($"LE_Editor_{targetId}_Conflict", true);
        _advancedFoldout = EditorPrefs.GetBool($"LE_Editor_{targetId}_Advanced", false);
        _helpFoldout = EditorPrefs.GetBool($"LE_Editor_{targetId}_Help", false);
    }

    private void OnDisable()
    {
        // 保存折叠状态
        string targetId = target.GetInstanceID().ToString();
        EditorPrefs.SetBool($"LE_Editor_{targetId}_Basic", _basicFoldout);
        EditorPrefs.SetBool($"LE_Editor_{targetId}_Preset", _presetFoldout);
        EditorPrefs.SetBool($"LE_Editor_{targetId}_Conflict", _conflictFoldout);
        EditorPrefs.SetBool($"LE_Editor_{targetId}_Advanced", _advancedFoldout);
        EditorPrefs.SetBool($"LE_Editor_{targetId}_Help", _helpFoldout);
    }

    // 场景视图绘制尺寸边界（兼容所有Unity版本）
    private void OnSceneGUI()
    {
        var element = target as LayoutElement;
        if (element == null) return;

        var rectTrans = element.GetComponent<RectTransform>();
        if (rectTrans == null) return;

        // 仅在选中时绘制
        if (Selection.activeGameObject != element.gameObject) return;

        // 计算尺寸
        float minWidth = Mathf.Max(_minWidth.floatValue, rectTrans.rect.width);
        float minHeight = Mathf.Max(_minHeight.floatValue, rectTrans.rect.height);
        float preferredWidth = Mathf.Max(_preferredWidth.floatValue, minWidth);
        float preferredHeight = Mathf.Max(_preferredHeight.floatValue, minHeight);

        // 绘制最小尺寸边界（红色虚线）
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        Vector3 minBottomLeft = rectTrans.TransformPoint(new Vector2(0, 0));
        Vector3 minTopRight = rectTrans.TransformPoint(new Vector2(minWidth, minHeight));
        Handles.DrawDottedLine(minBottomLeft, new Vector3(minTopRight.x, minBottomLeft.y), 5f);
        Handles.DrawDottedLine(minBottomLeft, new Vector3(minBottomLeft.x, minTopRight.y), 5f);

        // 绘制首选尺寸边界（蓝色虚线）
        Handles.color = new Color(0.3f, 0.3f, 1f, 0.5f);
        Vector3 prefBottomLeft = rectTrans.TransformPoint(new Vector2(0, 0));
        Vector3 prefTopRight = rectTrans.TransformPoint(new Vector2(preferredWidth, preferredHeight));
        Handles.DrawDottedLine(prefBottomLeft, new Vector3(prefTopRight.x, prefBottomLeft.y), 5f);
        Handles.DrawDottedLine(prefBottomLeft, new Vector3(prefBottomLeft.x, prefTopRight.y), 5f);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var element = target as LayoutElement;
        if (element == null) return;
        var rectTrans = element.GetComponent<RectTransform>();

        // -------------------------- 顶部标题栏 --------------------------
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("布局元素", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("为布局系统提供尺寸约束", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);

        // -------------------------- 基础设置区 --------------------------
        _basicFoldout = EditorGUILayout.Foldout(_basicFoldout, "基础设置", true);
        if (_basicFoldout)
        {
            EditorGUI.indentLevel++;

            // 尺寸约束（全中文标签）
            EditorGUILayout.LabelField("尺寸约束", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_minWidth, new GUIContent("最小宽度"));
            EditorGUILayout.PropertyField(_minHeight, new GUIContent("最小高度"));
            EditorGUILayout.PropertyField(_preferredWidth, new GUIContent("首选宽度"));
            EditorGUILayout.PropertyField(_preferredHeight, new GUIContent("首选高度"));
            EditorGUILayout.PropertyField(_flexibleWidth, new GUIContent("弹性宽度"));
            EditorGUILayout.PropertyField(_flexibleHeight, new GUIContent("弹性高度"));

            // 忽略布局
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_ignoreLayout, new GUIContent("忽略布局"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 快捷预设区 --------------------------
        _presetFoldout = EditorGUILayout.Foldout(_presetFoldout, "快捷预设", true);
        if (_presetFoldout)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("常用尺寸预设", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("按钮 (120x40)", GUILayout.Width(100)))
            {
                _minWidth.floatValue = 120;
                _minHeight.floatValue = 40;
                _preferredWidth.floatValue = 120;
                _preferredHeight.floatValue = 40;
                _flexibleWidth.floatValue = 0;
                _flexibleHeight.floatValue = 0;
                _ignoreLayout.boolValue = false;
            }

            if (GUILayout.Button("文本 (200x30)", GUILayout.Width(100)))
            {
                _minWidth.floatValue = 100;
                _minHeight.floatValue = 30;
                _preferredWidth.floatValue = 200;
                _preferredHeight.floatValue = 30;
                _flexibleWidth.floatValue = 1;
                _flexibleHeight.floatValue = 0;
                _ignoreLayout.boolValue = false;
            }

            if (GUILayout.Button("图标 (80x80)", GUILayout.Width(100)))
            {
                _minWidth.floatValue = 80;
                _minHeight.floatValue = 80;
                _preferredWidth.floatValue = 80;
                _preferredHeight.floatValue = 80;
                _flexibleWidth.floatValue = 0;
                _flexibleHeight.floatValue = 0;
                _ignoreLayout.boolValue = false;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            // 快捷尺寸调整
            EditorGUILayout.LabelField("快捷尺寸调整", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("宽度：", GUILayout.Width(60));
            if (GUILayout.Button("+10", GUILayout.Width(40)))
            {
                _minWidth.floatValue += 10;
                _preferredWidth.floatValue += 10;
            }
            if (GUILayout.Button("-10", GUILayout.Width(40)))
            {
                _minWidth.floatValue = Mathf.Max(0, _minWidth.floatValue - 10);
                _preferredWidth.floatValue = Mathf.Max(_minWidth.floatValue, _preferredWidth.floatValue - 10);
            }
            EditorGUILayout.LabelField("高度：", GUILayout.Width(40));
            if (GUILayout.Button("+10", GUILayout.Width(40)))
            {
                _minHeight.floatValue += 10;
                _preferredHeight.floatValue += 10;
            }
            if (GUILayout.Button("-10", GUILayout.Width(40)))
            {
                _minHeight.floatValue = Mathf.Max(0, _minHeight.floatValue - 10);
                _preferredHeight.floatValue = Mathf.Max(_minHeight.floatValue, _preferredHeight.floatValue - 10);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 冲突检测区 --------------------------
        _conflictFoldout = EditorGUILayout.Foldout(_conflictFoldout, "冲突检测", true);
        if (_conflictFoldout && rectTrans != null)
        {
            EditorGUI.indentLevel++;
            List<string> conflicts = new List<string>();

            // 检测尺寸设置异常
            if (_preferredWidth.floatValue < _minWidth.floatValue)
            {
                conflicts.Add("首选宽度小于最小宽度，会自动取最小宽度");
            }
            if (_preferredHeight.floatValue < _minHeight.floatValue)
            {
                conflicts.Add("首选高度小于最小高度，会自动取最小高度");
            }
            if (_flexibleWidth.floatValue < 0 || _flexibleHeight.floatValue < 0)
            {
                conflicts.Add("弹性宽度/高度不能为负数");
            }

            // 检测父布局冲突
            var parentLayout = rectTrans.parent.GetComponent<LayoutGroup>();
            if (parentLayout != null && _ignoreLayout.boolValue)
            {
                conflicts.Add("父对象存在布局组，但当前元素已忽略布局，可能导致位置异常");
            }

            // 显示冲突结果
            if (conflicts.Count > 0)
            {
                EditorGUILayout.HelpBox($"检测到 {conflicts.Count} 个冲突：\n" + string.Join("\n", conflicts), MessageType.Warning);
                if (GUILayout.Button("尝试自动修复", GUILayout.Width(120)))
                {
                    AutoFixConflicts(element);
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
                if (GUILayout.Button("同步所有选中对象的尺寸参数", GUILayout.Width(200)))
                {
                    foreach (var t in targets)
                    {
                        var targetElement = t as LayoutElement;
                        if (targetElement == null) continue;

                        var so = new SerializedObject(targetElement);
                        so.FindProperty("m_MinWidth").floatValue = _minWidth.floatValue;
                        so.FindProperty("m_MinHeight").floatValue = _minHeight.floatValue;
                        so.FindProperty("m_PreferredWidth").floatValue = _preferredWidth.floatValue;
                        so.FindProperty("m_PreferredHeight").floatValue = _preferredHeight.floatValue;
                        so.FindProperty("m_FlexibleWidth").floatValue = _flexibleWidth.floatValue;
                        so.FindProperty("m_FlexibleHeight").floatValue = _flexibleHeight.floatValue;
                        so.FindProperty("m_IgnoreLayout").boolValue = _ignoreLayout.boolValue;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // 调试工具
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("调试工具", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("强制重建父对象布局", GUILayout.Width(200)))
            {
                if (rectTrans.parent != null)
                {
                    var parentRect = rectTrans.parent.GetComponent<RectTransform>();
                    if (parentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }
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
                "1. 最小尺寸是布局系统能分配的最小空间\n" +
                "2. 首选尺寸是布局系统优先分配的空间\n" +
                "3. 弹性尺寸是空间充足时额外分配的比例\n" +
                "4. 忽略布局会让该对象不参与父布局组的排列",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("常见问题", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("为什么尺寸设置不生效？", GUILayout.Width(200)))
            {
                EditorUtility.DisplayDialog(
                    "排查步骤",
                    "1. 检查父对象是否有布局组（如HorizontalLayoutGroup）\n" +
                    "2. 确保首选尺寸不小于最小尺寸\n" +
                    "3. 检查是否勾选了「忽略布局」\n" +
                    "4. 点击「强制重建父对象布局」按钮刷新计算",
                    "确定");
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 自动修复常见冲突
    private void AutoFixConflicts(LayoutElement element)
    {
        Undo.RecordObject(element, "Auto Fix LayoutElement Conflicts");

        if (_preferredWidth.floatValue < _minWidth.floatValue)
        {
            _preferredWidth.floatValue = _minWidth.floatValue;
        }
        if (_preferredHeight.floatValue < _minHeight.floatValue)
        {
            _preferredHeight.floatValue = _minHeight.floatValue;
        }

        if (_flexibleWidth.floatValue < 0) _flexibleWidth.floatValue = 0;
        if (_flexibleHeight.floatValue < 0) _flexibleHeight.floatValue = 0;

        var rectTrans = element.GetComponent<RectTransform>();
        if (rectTrans != null && rectTrans.parent != null && rectTrans.parent.GetComponent<LayoutGroup>() != null)
        {
            _ignoreLayout.boolValue = false;
        }

        // 刷新布局
        if (rectTrans != null && rectTrans.parent != null)
        {
            var parentRect = rectTrans.parent.GetComponent<RectTransform>();
            if (parentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
        SceneView.RepaintAll();
    }
}