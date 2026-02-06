using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(LoopScrollRectBase), true)]
    public class LoopScrollRectInspector : Editor
    {
        // 基础滚动矩形属性
        SerializedProperty m_Content;
        SerializedProperty m_Horizontal;
        SerializedProperty m_Vertical;
        SerializedProperty m_MovementType;
        SerializedProperty m_Elasticity;
        SerializedProperty m_Inertia;
        SerializedProperty m_DecelerationRate;
        SerializedProperty m_ScrollSensitivity;
        SerializedProperty m_Viewport;
        SerializedProperty m_HorizontalScrollbar;
        SerializedProperty m_VerticalScrollbar;
        SerializedProperty m_HorizontalScrollbarVisibility;
        SerializedProperty m_VerticalScrollbarVisibility;
        SerializedProperty m_HorizontalScrollbarSpacing;
        SerializedProperty m_VerticalScrollbarSpacing;
        SerializedProperty m_OnValueChanged;
        AnimBool m_ShowElasticity;
        AnimBool m_ShowDecelerationRate;
        bool m_ViewportIsNotChild, m_HScrollbarIsNotChild, m_VScrollbarIsNotChild;
        static string s_HError = "此可见性模式要求：Viewport和水平滚动条必须是ScrollRect的子物体。";
        static string s_VError = "此可见性模式要求：Viewport和垂直滚动条必须是ScrollRect的子物体。";

        // 循环滚动扩展属性
        SerializedProperty totalCount;
        SerializedProperty reverseDirection;

        int firstItem = 0, lastItem = 0, scrollIndex = 0;
        float firstOffset = 0.0f, lastOffset = 0.0f, scrollOffset = 0;
        LoopScrollRectBase.ScrollMode scrollMode = LoopScrollRectBase.ScrollMode.ToStart;
        float scrollSpeed = 1000, scrollTime = 1;

        protected virtual void OnEnable()
        {
            // 初始化基础滚动矩形属性
            m_Content = serializedObject.FindProperty("m_Content");
            m_Horizontal = serializedObject.FindProperty("m_Horizontal");
            m_Vertical = serializedObject.FindProperty("m_Vertical");
            m_MovementType = serializedObject.FindProperty("m_MovementType");
            m_Elasticity = serializedObject.FindProperty("m_Elasticity");
            m_Inertia = serializedObject.FindProperty("m_Inertia");
            m_DecelerationRate = serializedObject.FindProperty("m_DecelerationRate");
            m_ScrollSensitivity = serializedObject.FindProperty("m_ScrollSensitivity");
            m_Viewport = serializedObject.FindProperty("m_Viewport");
            m_HorizontalScrollbar = serializedObject.FindProperty("m_HorizontalScrollbar");
            m_VerticalScrollbar = serializedObject.FindProperty("m_VerticalScrollbar");
            m_HorizontalScrollbarVisibility = serializedObject.FindProperty("m_HorizontalScrollbarVisibility");
            m_VerticalScrollbarVisibility = serializedObject.FindProperty("m_VerticalScrollbarVisibility");
            m_HorizontalScrollbarSpacing = serializedObject.FindProperty("m_HorizontalScrollbarSpacing");
            m_VerticalScrollbarSpacing = serializedObject.FindProperty("m_VerticalScrollbarSpacing");
            m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");

            m_ShowElasticity = new AnimBool(Repaint);
            m_ShowDecelerationRate = new AnimBool(Repaint);
            SetAnimBools(true);

            // 初始化循环滚动扩展属性
            totalCount = serializedObject.FindProperty("totalCount");
            reverseDirection = serializedObject.FindProperty("reverseDirection");
        }

        protected virtual void OnDisable()
        {
            m_ShowElasticity.valueChanged.RemoveListener(Repaint);
            m_ShowDecelerationRate.valueChanged.RemoveListener(Repaint);
        }

        void SetAnimBools(bool instant)
        {
            SetAnimBool(m_ShowElasticity, !m_MovementType.hasMultipleDifferentValues && m_MovementType.enumValueIndex == (int)ScrollRect.MovementType.Elastic, instant);
            SetAnimBool(m_ShowDecelerationRate, !m_Inertia.hasMultipleDifferentValues && m_Inertia.boolValue == true, instant);
        }

        void SetAnimBool(AnimBool a, bool value, bool instant)
        {
            if (instant)
                a.value = value;
            else
                a.target = value;
        }

        void CalculateCachedValues()
        {
            m_ViewportIsNotChild = false;
            m_HScrollbarIsNotChild = false;
            m_VScrollbarIsNotChild = false;
            if (targets.Length == 1)
            {
                Transform transform = ((LoopScrollRectBase)target).transform;
                if (m_Viewport.objectReferenceValue == null || ((RectTransform)m_Viewport.objectReferenceValue).transform.parent != transform)
                    m_ViewportIsNotChild = true;
                if (m_HorizontalScrollbar.objectReferenceValue == null || ((Scrollbar)m_HorizontalScrollbar.objectReferenceValue).transform.parent != transform)
                    m_HScrollbarIsNotChild = true;
                if (m_VerticalScrollbar.objectReferenceValue == null || ((Scrollbar)m_VerticalScrollbar.objectReferenceValue).transform.parent != transform)
                    m_VScrollbarIsNotChild = true;
            }
        }

        public override void OnInspectorGUI()
        {
            SetAnimBools(false);
            serializedObject.Update();
            CalculateCachedValues();

            // -------------------------- 基础滚动矩形设置 --------------------------
            EditorGUILayout.LabelField("基础滚动设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Content, new GUIContent("内容容器"));

            EditorGUILayout.PropertyField(m_Horizontal, new GUIContent("允许水平滚动"));
            EditorGUILayout.PropertyField(m_Vertical, new GUIContent("允许垂直滚动"));

            EditorGUILayout.PropertyField(m_MovementType, new GUIContent("滚动边界模式"));
            if (EditorGUILayout.BeginFadeGroup(m_ShowElasticity.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Elasticity, new GUIContent("弹性系数"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_Inertia, new GUIContent("启用惯性"));
            if (EditorGUILayout.BeginFadeGroup(m_ShowDecelerationRate.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DecelerationRate, new GUIContent("减速速率"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_ScrollSensitivity, new GUIContent("滚轮灵敏度"));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Viewport, new GUIContent("视口"));

            EditorGUILayout.PropertyField(m_HorizontalScrollbar, new GUIContent("水平滚动条"));
            if (m_HorizontalScrollbar.objectReferenceValue && !m_HorizontalScrollbar.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_HorizontalScrollbarVisibility, new GUIContent("可见性"));

                if ((ScrollRect.ScrollbarVisibility)m_HorizontalScrollbarVisibility.enumValueIndex == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport
                    && !m_HorizontalScrollbarVisibility.hasMultipleDifferentValues)
                {
                    if (m_ViewportIsNotChild || m_HScrollbarIsNotChild)
                        EditorGUILayout.HelpBox(s_HError, MessageType.Error);
                    EditorGUILayout.PropertyField(m_HorizontalScrollbarSpacing, new GUIContent("与视口间距"));
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_VerticalScrollbar, new GUIContent("垂直滚动条"));
            if (m_VerticalScrollbar.objectReferenceValue && !m_VerticalScrollbar.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_VerticalScrollbarVisibility, new GUIContent("可见性"));

                if ((ScrollRect.ScrollbarVisibility)m_VerticalScrollbarVisibility.enumValueIndex == ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport
                    && !m_VerticalScrollbarVisibility.hasMultipleDifferentValues)
                {
                    if (m_ViewportIsNotChild || m_VScrollbarIsNotChild)
                        EditorGUILayout.HelpBox(s_VError, MessageType.Error);
                    EditorGUILayout.PropertyField(m_VerticalScrollbarSpacing, new GUIContent("与视口间距"));
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_OnValueChanged, new GUIContent("位置变化事件"));

            // -------------------------- 循环滚动扩展设置 --------------------------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("循环滚动扩展", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(totalCount, new GUIContent("总项数（负数=无限）"));
            EditorGUILayout.PropertyField(reverseDirection, new GUIContent("反向滚动"));

            serializedObject.ApplyModifiedProperties();

            LoopScrollRectBase scroll = (LoopScrollRectBase)target;
            GUI.enabled = Application.isPlaying;
            const float buttonWidth = 100f;

            // -------------------------- 基础操作区 --------------------------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("基础操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("清空所有现有项");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空", GUILayout.Width(buttonWidth)))
            {
                scroll.ClearCells();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("刷新现有项（仅更新数据）");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(buttonWidth)))
            {
                scroll.RefreshCells();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("从开头重新填充");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("填充", GUILayout.Width(buttonWidth)))
            {
                scroll.RefillCells();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("从结尾重新填充");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("反向填充", GUILayout.Width(buttonWidth)))
            {
                scroll.RefillCellsFromEnd();
            }
            EditorGUILayout.EndHorizontal();

            // -------------------------- 填充测试区 --------------------------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("填充测试", EditorStyles.boldLabel);

            firstItem = EditorGUILayout.IntField("起始项索引", firstItem);
            firstOffset = EditorGUILayout.FloatField("起始偏移量", firstOffset);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("获取当前可见第一项");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("获取第一项", GUILayout.Width(buttonWidth)))
            {
                firstItem = scroll.GetFirstItem(out firstOffset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("从指定项开始填充");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("填充", GUILayout.Width(buttonWidth)))
            {
                scroll.RefillCells(scroll.reverseDirection ? (scroll.totalCount - firstItem) : firstItem, firstOffset);
            }
            EditorGUILayout.EndHorizontal();

            // -------------------------- 反向填充测试区 --------------------------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("反向填充测试", EditorStyles.boldLabel);

            lastItem = EditorGUILayout.IntField("结束项索引", lastItem);
            lastOffset = EditorGUILayout.FloatField("结束偏移量", lastOffset);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("获取当前可见最后一项");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("获取最后一项", GUILayout.Width(buttonWidth)))
            {
                lastItem = scroll.GetLastItem(out lastOffset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("从指定项反向填充");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("反向填充", GUILayout.Width(buttonWidth)))
            {
                scroll.RefillCellsFromEnd(scroll.reverseDirection ? lastItem : (scroll.totalCount - lastItem), lastOffset);
            }
            EditorGUILayout.EndHorizontal();

            // -------------------------- 滚动测试区 --------------------------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("滚动测试", EditorStyles.boldLabel);
            scrollIndex = EditorGUILayout.IntField("目标项索引", scrollIndex);
            scrollOffset = EditorGUILayout.FloatField("额外偏移量", scrollOffset);
            scrollMode = (LoopScrollRectBase.ScrollMode)EditorGUILayout.EnumPopup("滚动模式", scrollMode);

            scrollSpeed = EditorGUILayout.FloatField("滚动速度", scrollSpeed);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("按速度滚动到目标项");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("滚动到项", GUILayout.Width(buttonWidth)))
            {
                scroll.ScrollToCell(scrollIndex, scrollSpeed, scrollOffset, scrollMode);
            }
            EditorGUILayout.EndHorizontal();

            scrollTime = EditorGUILayout.FloatField("滚动时长", scrollTime);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("按时长滚动到目标项");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("限时滚动", GUILayout.Width(buttonWidth)))
            {
                scroll.ScrollToCellWithinTime(scrollIndex, scrollTime, scrollOffset, scrollMode);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}