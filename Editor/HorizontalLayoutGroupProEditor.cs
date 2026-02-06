using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(HorizontalLayoutGroup))]
[CanEditMultipleObjects]
public class HorizontalLayoutGroupProEditor : Editor
{
    // 核心属性缓存
    private SerializedProperty _childAlignment;
    private SerializedProperty _spacing;
    private SerializedProperty _padding; // RectOffset类型，非Rect
    private SerializedProperty _childControlWidth;
    private SerializedProperty _childControlHeight;
    private SerializedProperty _childForceExpandWidth;
    private SerializedProperty _childForceExpandHeight;

    // 关键修复：拆分RectOffset的四个子属性
    private SerializedProperty _paddingLeft;
    private SerializedProperty _paddingRight;
    private SerializedProperty _paddingTop;
    private SerializedProperty _paddingBottom;

    // 折叠组状态（持久化保存）
    private bool _basicFoldout = true;
    private bool _presetFoldout = true;
    private bool _previewFoldout = true;
    private bool _conflictFoldout = true;
    private bool _advancedFoldout = false;
    private bool _helpFoldout = false;

    // 尺寸缓存（改用RectOffset的数值缓存）
    private float _lastSpacing;
    private int _lastPadLeft, _lastPadRight, _lastPadTop, _lastPadBottom;
    private DateTime _lastUpdateTime;

    private void OnEnable()
    {
        // 空值防护：先判断目标对象是否有效
        if (target == null) return;

        // 绑定核心序列化属性（加空值判断）
        _childAlignment = serializedObject?.FindProperty("m_ChildAlignment");
        _spacing = serializedObject?.FindProperty("m_Spacing");
        _padding = serializedObject?.FindProperty("m_Padding");
        _childControlWidth = serializedObject?.FindProperty("m_ChildControlWidth");
        _childControlHeight = serializedObject?.FindProperty("m_ChildControlHeight");
        _childForceExpandWidth = serializedObject?.FindProperty("m_ChildForceExpandWidth");
        _childForceExpandHeight = serializedObject?.FindProperty("m_ChildForceExpandHeight");

        // 修复：加空值判断，避免_padding为空时报错（对应第67行空引用）
        if (_padding != null)
        {
            _paddingLeft = _padding.FindPropertyRelative("left");
            _paddingRight = _padding.FindPropertyRelative("right");
            _paddingTop = _padding.FindPropertyRelative("top");
            _paddingBottom = _padding.FindPropertyRelative("bottom");
        }

        // 加载持久化折叠状态（加空值判断）
        string targetId = target.GetInstanceID().ToString();
        _basicFoldout = EditorPrefs.GetBool($"HLG_Editor_{targetId}_Basic", true);
        _presetFoldout = EditorPrefs.GetBool($"HLG_Editor_{targetId}_Preset", true);
        _previewFoldout = EditorPrefs.GetBool($"HLG_Editor_{targetId}_Preview", true);
        _conflictFoldout = EditorPrefs.GetBool($"HLG_Editor_{targetId}_Conflict", true);
        _advancedFoldout = EditorPrefs.GetBool($"HLG_Editor_{targetId}_Advanced", false);
        _helpFoldout = EditorPrefs.GetBool($"HLG_Editor_{targetId}_Help", false);

        // 初始化缓存值（加空值判断）
        _lastSpacing = _spacing?.floatValue ?? 0f;
        _lastPadLeft = _paddingLeft?.intValue ?? 0;
        _lastPadRight = _paddingRight?.intValue ?? 0;
        _lastPadTop = _paddingTop?.intValue ?? 0;
        _lastPadBottom = _paddingBottom?.intValue ?? 0;
    }

    private void OnDisable()
    {
        // 空值防护：目标对象无效时直接返回
        if (target == null) return;

        // 保存折叠状态
        string targetId = target.GetInstanceID().ToString();
        EditorPrefs.SetBool($"HLG_Editor_{targetId}_Basic", _basicFoldout);
        EditorPrefs.SetBool($"HLG_Editor_{targetId}_Preset", _presetFoldout);
        EditorPrefs.SetBool($"HLG_Editor_{targetId}_Preview", _previewFoldout);
        EditorPrefs.SetBool($"HLG_Editor_{targetId}_Conflict", _conflictFoldout);
        EditorPrefs.SetBool($"HLG_Editor_{targetId}_Advanced", _advancedFoldout);
        EditorPrefs.SetBool($"HLG_Editor_{targetId}_Help", _helpFoldout);
    }

    // 场景视图绘制布局边界（修复Padding计算+空值防护）
    private void OnSceneGUI()
    {
        // 多层空值防护
        if (target == null) return;
        var layout = target as HorizontalLayoutGroup;
        if (layout == null) return;

        var rectTrans = layout.GetComponent<RectTransform>();
        if (rectTrans == null) return;

        // 仅在选中时绘制
        if (Selection.activeGameObject != layout.gameObject) return;

        // 修复：加空值判断，避免_padding相关属性为空
        if (_paddingLeft == null || _paddingRight == null || _paddingTop == null || _paddingBottom == null)
            return;

        // 正确获取RectOffset的四个值
        int padLeft = _paddingLeft.intValue;
        int padRight = _paddingRight.intValue;
        int padTop = _paddingTop.intValue;
        int padBottom = _paddingBottom.intValue;

        // 计算布局总宽度
        float totalWidth = padLeft + padRight;
        float maxHeight = 0;

        for (int i = 0; i < rectTrans.childCount; i++)
        {
            var child = rectTrans.GetChild(i);
            var childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                totalWidth += childRect.rect.width + (_spacing?.floatValue ?? 0f);
                maxHeight = Mathf.Max(maxHeight, childRect.rect.height);
            }
        }
        if (rectTrans.childCount > 0) totalWidth -= (_spacing?.floatValue ?? 0f); // 减去最后一个间距
        totalWidth = Mathf.Max(totalWidth, rectTrans.rect.width);
        maxHeight += padTop + padBottom;

        // 绘制布局边界
        Handles.color = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        Vector3 bottomLeft = rectTrans.TransformPoint(new Vector2(padLeft, padBottom));
        Vector3 bottomRight = rectTrans.TransformPoint(new Vector2(totalWidth - padRight, padBottom));
        Vector3 topRight = rectTrans.TransformPoint(new Vector2(totalWidth - padRight, maxHeight - padTop));
        Vector3 topLeft = rectTrans.TransformPoint(new Vector2(padLeft, maxHeight - padTop));

        Handles.DrawLine(bottomLeft, bottomRight);
        Handles.DrawLine(bottomRight, topRight);
        Handles.DrawLine(topRight, topLeft);
        Handles.DrawLine(topLeft, bottomLeft);
    }

    public override void OnInspectorGUI()
    {
        // 空值防护：序列化对象无效时直接返回
        if (serializedObject == null) return;
        serializedObject.Update();

        // 多层空值防护（对应第262行空引用）
        if (target == null) return;
        var layout = target as HorizontalLayoutGroup;
        if (layout == null) return;
        var rectTrans = layout.GetComponent<RectTransform>();

        // -------------------------- 顶部标题栏 --------------------------
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("水平布局组", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("将子对象按水平方向排列", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);

        // -------------------------- 基础设置区 --------------------------
        _basicFoldout = EditorGUILayout.Foldout(_basicFoldout, "基础设置", true);
        if (_basicFoldout)
        {
            EditorGUI.indentLevel++;

            // 1. 子对象对齐 汉化（加空值判断）
            if (_childAlignment != null)
            {
                string[] anchorNames = {
                    "左上角", "中上", "右上角",
                    "左中", "居中", "右中",
                    "左下角", "中下", "右下角"
                };
                int currentAnchorIndex = _childAlignment.enumValueIndex;
                int newAnchorIndex = EditorGUILayout.Popup("子对象对齐", currentAnchorIndex, anchorNames);
                if (newAnchorIndex != currentAnchorIndex)
                {
                    _childAlignment.enumValueIndex = newAnchorIndex;
                }
            }

            // 加空值判断，避免属性为空时报错
            if (_spacing != null)
                EditorGUILayout.PropertyField(_spacing, new GUIContent("子对象间距"));
            if (_padding != null)
                EditorGUILayout.PropertyField(_padding, new GUIContent("内边距"));

            // 尺寸控制（加空值判断）
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("尺寸控制", EditorStyles.miniBoldLabel);
            if (_childControlWidth != null)
                EditorGUILayout.PropertyField(_childControlWidth, new GUIContent("控制子对象宽度"));
            if (_childControlHeight != null)
                EditorGUILayout.PropertyField(_childControlHeight, new GUIContent("控制子对象高度"));
            if (_childForceExpandWidth != null)
                EditorGUILayout.PropertyField(_childForceExpandWidth, new GUIContent("强制扩展宽度"));
            if (_childForceExpandHeight != null)
                EditorGUILayout.PropertyField(_childForceExpandHeight, new GUIContent("强制扩展高度"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 快捷预设区 --------------------------
        _presetFoldout = EditorGUILayout.Foldout(_presetFoldout, "快捷预设", true);
        if (_presetFoldout)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("常用布局预设", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("按钮栏 (120x40)", GUILayout.Width(120)))
            {
                if (_childAlignment != null)
                    _childAlignment.enumValueIndex = (int)TextAnchor.MiddleCenter;
                if (_spacing != null)
                    _spacing.floatValue = 15;
                // 加空值判断，避免_padding相关属性为空
                if (_paddingLeft != null) _paddingLeft.intValue = 20;
                if (_paddingRight != null) _paddingRight.intValue = 20;
                if (_paddingTop != null) _paddingTop.intValue = 20;
                if (_paddingBottom != null) _paddingBottom.intValue = 20;
                if (_childControlWidth != null) _childControlWidth.boolValue = true;
                if (_childControlHeight != null) _childControlHeight.boolValue = true;
                if (_childForceExpandWidth != null) _childForceExpandWidth.boolValue = false;
                if (_childForceExpandHeight != null) _childForceExpandHeight.boolValue = false;
            }

            if (GUILayout.Button("文本行 (200x30)", GUILayout.Width(120)))
            {
                if (_childAlignment != null)
                    _childAlignment.enumValueIndex = (int)TextAnchor.MiddleLeft;
                if (_spacing != null)
                    _spacing.floatValue = 10;
                if (_paddingLeft != null) _paddingLeft.intValue = 10;
                if (_paddingRight != null) _paddingRight.intValue = 10;
                if (_paddingTop != null) _paddingTop.intValue = 10;
                if (_paddingBottom != null) _paddingBottom.intValue = 10;
                if (_childControlWidth != null) _childControlWidth.boolValue = true;
                if (_childControlHeight != null) _childControlHeight.boolValue = true;
                if (_childForceExpandWidth != null) _childForceExpandWidth.boolValue = true;
                if (_childForceExpandHeight != null) _childForceExpandHeight.boolValue = false;
            }

            if (GUILayout.Button("图标行 (80x80)", GUILayout.Width(120)))
            {
                if (_childAlignment != null)
                    _childAlignment.enumValueIndex = (int)TextAnchor.MiddleCenter;
                if (_spacing != null)
                    _spacing.floatValue = 10;
                if (_paddingLeft != null) _paddingLeft.intValue = 15;
                if (_paddingRight != null) _paddingRight.intValue = 15;
                if (_paddingTop != null) _paddingTop.intValue = 15;
                if (_paddingBottom != null) _paddingBottom.intValue = 15;
                if (_childControlWidth != null) _childControlWidth.boolValue = true;
                if (_childControlHeight != null) _childControlHeight.boolValue = true;
                if (_childForceExpandWidth != null) _childForceExpandWidth.boolValue = false;
                if (_childForceExpandHeight != null) _childForceExpandHeight.boolValue = false;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            // 快捷间距调整（加空值判断）
            EditorGUILayout.LabelField("快捷间距调整", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("间距：", GUILayout.Width(60));
            if (GUILayout.Button("+5", GUILayout.Width(40)) && _spacing != null)
                _spacing.floatValue += 5;
            if (GUILayout.Button("-5", GUILayout.Width(40)) && _spacing != null)
                _spacing.floatValue = Mathf.Max(0, _spacing.floatValue - 5);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 实时预览区 --------------------------
        _previewFoldout = EditorGUILayout.Foldout(_previewFoldout, "实时预览", true);
        if (_previewFoldout && rectTrans != null)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("刷新布局计算", GUILayout.Width(120)))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
                _lastUpdateTime = DateTime.Now;
            }

            // 加空值判断，避免_padding相关属性为空
            if (_paddingLeft == null || _paddingRight == null || _paddingTop == null || _paddingBottom == null)
            {
                EditorGUILayout.HelpBox("缺少Padding属性，无法计算尺寸", MessageType.Warning);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(6);
                return;
            }

            // 用RectOffset的四个值计算总尺寸
            int padLeft = _paddingLeft.intValue;
            int padRight = _paddingRight.intValue;
            int padTop = _paddingTop.intValue;
            int padBottom = _paddingBottom.intValue;

            // 计算总尺寸
            float totalWidth = padLeft + padRight;
            float maxHeight = 0;
            int childCount = rectTrans.childCount;

            for (int i = 0; i < childCount; i++)
            {
                var child = rectTrans.GetChild(i);
                var childRect = child.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    totalWidth += childRect.rect.width + (_spacing?.floatValue ?? 0f);
                    maxHeight = Mathf.Max(maxHeight, childRect.rect.height);
                }
            }
            if (childCount > 0) totalWidth -= (_spacing?.floatValue ?? 0f); // 减去最后一个间距
            maxHeight += padTop + padBottom;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("预估布局信息", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"子对象数量：{childCount}");
            EditorGUILayout.LabelField($"总占用宽度：{totalWidth:F1}px");
            EditorGUILayout.LabelField($"最大高度：{maxHeight:F1}px");

            // 尺寸变化检测（改用RectOffset数值对比）
            bool isChanged = (_spacing?.floatValue ?? 0f) != _lastSpacing ||
                             (_paddingLeft?.intValue ?? 0) != _lastPadLeft ||
                             (_paddingRight?.intValue ?? 0) != _lastPadRight ||
                             (_paddingTop?.intValue ?? 0) != _lastPadTop ||
                             (_paddingBottom?.intValue ?? 0) != _lastPadBottom;

            if (isChanged)
            {
                EditorGUILayout.HelpBox($"尺寸已更新（{_lastUpdateTime:HH:mm:ss}）", MessageType.Info);
                _lastSpacing = _spacing?.floatValue ?? 0f;
                _lastPadLeft = _paddingLeft?.intValue ?? 0;
                _lastPadRight = _paddingRight?.intValue ?? 0;
                _lastPadTop = _paddingTop?.intValue ?? 0;
                _lastPadBottom = _paddingBottom?.intValue ?? 0;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        // -------------------------- 冲突检测区 --------------------------
        _conflictFoldout = EditorGUILayout.Foldout(_conflictFoldout, "冲突检测", true);
        if (_conflictFoldout && rectTrans != null)
        {
            EditorGUI.indentLevel++;
            List<string> conflicts = new List<string>();

            // 检测子对象尺寸异常
            bool hasInvalidChild = false;
            for (int i = 0; i < rectTrans.childCount; i++)
            {
                var child = rectTrans.GetChild(i);
                var childRect = child.GetComponent<RectTransform>();
                if (childRect != null && childRect.rect.size == Vector2.zero)
                {
                    hasInvalidChild = true;
                    break;
                }
            }
            if (hasInvalidChild)
            {
                conflicts.Add("部分子对象尺寸为0，可能导致布局异常");
            }

            // 检测父对象布局冲突
            var parentLayout = rectTrans.parent.GetComponent<LayoutGroup>();
            if (parentLayout != null && (parentLayout is VerticalLayoutGroup || parentLayout is GridLayoutGroup))
            {
                conflicts.Add("父对象存在垂直/网格布局组，可能导致水平布局被覆盖");
            }

            // 正确计算宽度溢出（加空值判断）
            if (_paddingLeft != null && _paddingRight != null && _spacing != null)
            {
                int padLeft = _paddingLeft.intValue;
                int padRight = _paddingRight.intValue;
                float totalWidth = padLeft + padRight;
                for (int i = 0; i < rectTrans.childCount; i++)
                {
                    var child = rectTrans.GetChild(i);
                    var childRect = child.GetComponent<RectTransform>();
                    if (childRect != null) totalWidth += childRect.rect.width + _spacing.floatValue;
                }
                if (rectTrans.childCount > 0) totalWidth -= _spacing.floatValue;
                if (totalWidth > rectTrans.rect.width * 1.5f)
                {
                    conflicts.Add("总宽度超出父对象尺寸1.5倍，可能导致内容溢出");
                }
            }

            // 显示冲突结果
            if (conflicts.Count > 0)
            {
                EditorGUILayout.HelpBox($"检测到 {conflicts.Count} 个冲突：\n" + string.Join("\n", conflicts), MessageType.Warning);
                if (GUILayout.Button("尝试自动修复", GUILayout.Width(120)))
                {
                    AutoFixConflicts(layout);
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

            // 批量操作（修复Padding赋值逻辑+空值判断）
            if (targets.Length > 1)
            {
                EditorGUILayout.LabelField("批量操作", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("同步所有选中对象的布局参数", GUILayout.Width(200)))
                {
                    foreach (var t in targets)
                    {
                        var targetLayout = t as HorizontalLayoutGroup;
                        if (targetLayout == null) continue;

                        var so = new SerializedObject(targetLayout);
                        var targetChildAlign = so.FindProperty("m_ChildAlignment");
                        if (targetChildAlign != null && _childAlignment != null)
                            targetChildAlign.enumValueIndex = _childAlignment.enumValueIndex;

                        var targetSpacing = so.FindProperty("m_Spacing");
                        if (targetSpacing != null && _spacing != null)
                            targetSpacing.floatValue = _spacing.floatValue;

                        // 批量同步RectOffset的四个子属性（加空值判断）
                        var targetPadding = so.FindProperty("m_Padding");
                        if (targetPadding != null)
                        {
                            if (_paddingLeft != null)
                                targetPadding.FindPropertyRelative("left").intValue = _paddingLeft.intValue;
                            if (_paddingRight != null)
                                targetPadding.FindPropertyRelative("right").intValue = _paddingRight.intValue;
                            if (_paddingTop != null)
                                targetPadding.FindPropertyRelative("top").intValue = _paddingTop.intValue;
                            if (_paddingBottom != null)
                                targetPadding.FindPropertyRelative("bottom").intValue = _paddingBottom.intValue;
                        }

                        var targetControlWidth = so.FindProperty("m_ChildControlWidth");
                        if (targetControlWidth != null && _childControlWidth != null)
                            targetControlWidth.boolValue = _childControlWidth.boolValue;

                        var targetControlHeight = so.FindProperty("m_ChildControlHeight");
                        if (targetControlHeight != null && _childControlHeight != null)
                            targetControlHeight.boolValue = _childControlHeight.boolValue;

                        var targetExpandWidth = so.FindProperty("m_ChildForceExpandWidth");
                        if (targetExpandWidth != null && _childForceExpandWidth != null)
                            targetExpandWidth.boolValue = _childForceExpandWidth.boolValue;

                        var targetExpandHeight = so.FindProperty("m_ChildForceExpandHeight");
                        if (targetExpandHeight != null && _childForceExpandHeight != null)
                            targetExpandHeight.boolValue = _childForceExpandHeight.boolValue;

                        so.ApplyModifiedProperties();
                    }
                }
            }

            // 调试工具（加空值判断）
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("调试工具", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("强制重建整个UI布局", GUILayout.Width(200)) && rectTrans != null)
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
                "1. 控制子对象尺寸时，需确保子对象有有效的RectTransform\n" +
                "2. 强制扩展宽度会让子对象自动填充剩余空间\n" +
                "3. 内边距和间距会影响整体布局的紧凑程度\n" +
                "4. 与ContentSizeFitter一起使用时，需确保父对象宽度足够",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("常见问题", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("为什么水平布局不生效？", GUILayout.Width(200)))
            {
                EditorUtility.DisplayDialog(
                    "排查步骤",
                    "1. 检查子对象是否有有效的RectTransform尺寸\n" +
                    "2. 确保父对象有足够的宽度容纳所有子对象\n" +
                    "3. 检查父对象是否有其他布局组件导致冲突\n" +
                    "4. 点击「强制重建整个UI布局」按钮刷新计算",
                    "确定");
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 自动修复常见冲突（修复Padding计算逻辑+空值防护）
    private void AutoFixConflicts(HorizontalLayoutGroup layout)
    {
        // 空值防护
        if (layout == null) return;
        Undo.RecordObject(layout, "Auto Fix HorizontalLayoutGroup Conflicts");

        var rectTrans = layout.GetComponent<RectTransform>();
        if (rectTrans == null) return;

        for (int i = 0; i < rectTrans.childCount; i++)
        {
            var child = rectTrans.GetChild(i);
            if (child.GetComponent<LayoutElement>() == null)
            {
                Undo.AddComponent<LayoutElement>(child.gameObject);
            }
        }

        // 用RectOffset的四个值计算总宽度（加空值判断）
        if (_paddingLeft == null || _paddingRight == null || _spacing == null)
            return;

        int padLeft = _paddingLeft.intValue;
        int padRight = _paddingRight.intValue;
        float totalWidth = padLeft + padRight;
        for (int i = 0; i < rectTrans.childCount; i++)
        {
            var child = rectTrans.GetChild(i);
            var childRect = child.GetComponent<RectTransform>();
            if (childRect != null) totalWidth += childRect.rect.width + _spacing.floatValue;
        }
        if (rectTrans.childCount > 0) totalWidth -= _spacing.floatValue;

        if (totalWidth > rectTrans.rect.width)
        {
            Undo.RecordObject(rectTrans, "Adjust Parent Width");
            rectTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
        }

        // 刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
        SceneView.RepaintAll();
    }
}