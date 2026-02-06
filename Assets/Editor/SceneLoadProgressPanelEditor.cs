using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

[CustomEditor(typeof(SceneLoadProgressPanel))]
public class SceneLoadProgressPanelEditor : Editor
{
    private ReorderableList _promptTextReorderableList;
    private ReorderableList _promptTextEnglishReorderableList; // 新增：英文列表
    private SceneLoadProgressPanel _targetPanel;
    private SerializedProperty _promptTextListProp;
    private SerializedProperty _promptTextEnglishListProp; // 新增：英文列表序列化属性

    private void OnEnable()
    {
        _targetPanel = (SceneLoadProgressPanel)target;

        // 初始化空列表（防止空引用）
        if (_targetPanel.PromptTextList == null)
        {
            _targetPanel.PromptTextList = new List<string>();
            EditorUtility.SetDirty(_targetPanel);
        }
        // 新增：初始化英文列表空值
        if (_targetPanel.PromptTextList_English == null)
        {
            _targetPanel.PromptTextList_English = new List<string>();
            EditorUtility.SetDirty(_targetPanel);
        }

        serializedObject.Update();
        // 缓存序列化属性
        _promptTextListProp = serializedObject.FindProperty("PromptTextList");
        _promptTextEnglishListProp = serializedObject.FindProperty("PromptTextList_English"); // 新增：缓存英文列表属性

        if (_promptTextListProp == null)
        {
            Debug.LogError("未找到 PromptTextList 序列化属性！请检查字段名是否正确");
            return;
        }
        // 新增：英文列表属性校验
        if (_promptTextEnglishListProp == null)
        {
            Debug.LogError("未找到 PromptTextList_English 序列化属性！请检查字段名是否正确");
            return;
        }

        // 初始化中文提示文本可重排列表
        _promptTextReorderableList = CreateReorderableList(_promptTextListProp, "中文加载提示文本列表");
        // 新增：初始化英文提示文本可重排列表
        _promptTextEnglishReorderableList = CreateReorderableList(_promptTextEnglishListProp, "英文加载提示文本列表");
    }

    // 新增：通用创建可重排列表方法（复用逻辑，避免冗余）
    private ReorderableList CreateReorderableList(SerializedProperty prop, string headerName)
    {
        ReorderableList list = new ReorderableList(
            serializedObject,
            prop,
            true, // 显示拖拽手柄
            true, // 显示列表标题
            true, // 显示添加按钮
            true  // 显示删除按钮
        );

        // 列表标题
        list.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, headerName, EditorStyles.boldLabel);
        };

        // 加宽字符串输入区域
        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index < 0 || index >= prop.arraySize)
                return;

            SerializedProperty element = prop.GetArrayElementAtIndex(index);
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            float indexWidth = 25f;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, indexWidth, rect.height), $"{index + 1}.");

            Rect inputRect = new Rect(
                rect.x + indexWidth + 5f,
                rect.y,
                rect.width - indexWidth - 10f,
                rect.height
            );
            element.stringValue = EditorGUI.TextField(inputRect, "", element.stringValue);
        };

        // 修复添加逻辑
        list.onAddCallback = (l) =>
        {
            int newIndex = l.serializedProperty.arraySize;
            l.serializedProperty.arraySize++;
            SerializedProperty newElement = l.serializedProperty.GetArrayElementAtIndex(newIndex);
            // 区分中英文默认文本
            newElement.stringValue = prop == _promptTextEnglishListProp ? "New load prompt text" : "新的加载提示文本";
            serializedObject.ApplyModifiedProperties();
        };

        return list;
    }

    public override void OnInspectorGUI()
    {
        if (_targetPanel == null)
            return;

        serializedObject.Update();
        EditorGUI.BeginDisabledGroup(!_targetPanel.gameObject.activeSelf);

        // 核心组件区
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("=== 核心组件赋值 ===", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        // 进度条填充图
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(
            "进度条填充图",
            "需将Image组件的Image Type设为Filled（填充模式）"
        ));
        _targetPanel.ProgressImage = (UnityEngine.UI.Image)EditorGUILayout.ObjectField(
            _targetPanel.ProgressImage,
            typeof(UnityEngine.UI.Image),
            true
        );
        EditorGUILayout.EndHorizontal();

        // 进度百分比文本
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(
            "进度百分比文本",
            "显示0%~100%的数字文本（TMP组件）"
        ));
        _targetPanel.ProgressNumberText = (TMPro.TextMeshProUGUI)EditorGUILayout.ObjectField(
            _targetPanel.ProgressNumberText,
            typeof(TMPro.TextMeshProUGUI),
            true
        );
        EditorGUILayout.EndHorizontal();

        // 提示文本显示框
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(
            "提示文本显示框",
            "滚动播放提示文本的TMP组件"
        ));
        _targetPanel.PromptText = (TMPro.TextMeshProUGUI)EditorGUILayout.ObjectField(
            _targetPanel.PromptText,
            typeof(TMPro.TextMeshProUGUI),
            true
        );
        EditorGUILayout.EndHorizontal();

        // 场景名称文本
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(
            "场景名称文本",
            "显示当前加载场景名称的TMP组件"
        ));
        _targetPanel.SceneName = (TMPro.TextMeshProUGUI)EditorGUILayout.ObjectField(
            _targetPanel.SceneName,
            typeof(TMPro.TextMeshProUGUI),
            true
        );
        EditorGUILayout.EndHorizontal();

        // 背景图片
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(
            "背景图片",
            "加载面板的背景Image组件"
        ));
        _targetPanel.BackGroundImage = (UnityEngine.UI.Image)EditorGUILayout.ObjectField(
            _targetPanel.BackGroundImage,
            typeof(UnityEngine.UI.Image),
            true
        );
        EditorGUILayout.EndHorizontal();

        // 标题文本
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(
            "标题文本",
            "显示加载状态（如“加载完毕”）的TMP组件"
        ));
        _targetPanel.TopicText = (TMPro.TextMeshProUGUI)EditorGUILayout.ObjectField(
            _targetPanel.TopicText,
            typeof(TMPro.TextMeshProUGUI),
            true
        );
        EditorGUILayout.EndHorizontal();

        // 中文提示文本列表区
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== 中文加载提示文本配置 ===", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        if (_promptTextReorderableList != null && _promptTextListProp != null)
        {
            _promptTextReorderableList.DoLayoutList();
            if (_targetPanel.PromptTextList.Count == 0)
            {
                EditorGUILayout.HelpBox(" 中文提示文本列表为空！请添加至少一条提示文本", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("中文提示文本列表初始化失败！", MessageType.Error);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== 英文加载提示文本配置 ===", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        if (_promptTextEnglishReorderableList != null && _promptTextEnglishListProp != null)
        {
            _promptTextEnglishReorderableList.DoLayoutList();
            if (_targetPanel.PromptTextList_English.Count == 0)
            {
                EditorGUILayout.HelpBox(" 英文提示文本列表为空！请添加至少一条提示文本", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("英文提示文本列表初始化失败！", MessageType.Error);
        }

        // 保存修改
        EditorGUILayout.Space(10);
        serializedObject.ApplyModifiedProperties();
        EditorGUI.EndDisabledGroup();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_targetPanel);
        }
    }
}