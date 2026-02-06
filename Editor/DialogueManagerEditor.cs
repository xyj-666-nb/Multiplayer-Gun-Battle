using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueManager))]
public class DialogueManagerEditor : Editor
{
    private SerializedProperty dialogueDataPacksListProperty;
    private DialogueManager dialogueManager;

    // 用于跟踪哪些元素是新添加的
    private List<bool> isNewElement = new List<bool>();

    // 用于展开/折叠每个对话包
    private List<bool> foldoutStates = new List<bool>();

    // 用于展开/折叠每个对话包内的对话内容
    private List<List<bool>> dialogueFoldoutStates = new List<List<bool>>();

    private void OnEnable()
    {
        dialogueManager = (DialogueManager)target;
        dialogueDataPacksListProperty = serializedObject.FindProperty("dialogueDataPacksList");

        // 初始化时，假设所有现有元素都不是新的
        if (dialogueDataPacksListProperty != null)
        {
            isNewElement.Clear();
            foldoutStates.Clear();
            dialogueFoldoutStates.Clear();

            for (int i = 0; i < dialogueDataPacksListProperty.arraySize; i++)
            {
                isNewElement.Add(false);
                foldoutStates.Add(false);
                dialogueFoldoutStates.Add(new List<bool>());

                // 初始化每个对话包内的对话内容展开状态
                SerializedProperty packProperty = dialogueDataPacksListProperty.GetArrayElementAtIndex(i);
                SerializedProperty dialogueListProperty = packProperty.FindPropertyRelative("dialogueInfoPacks");
                if (dialogueListProperty != null)
                {
                    for (int j = 0; j < dialogueListProperty.arraySize; j++)
                    {
                        dialogueFoldoutStates[i].Add(false);
                    }
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制默认属性
        DrawPropertiesExcluding(serializedObject, "dialogueDataPacksList");

        EditorGUILayout.Space(10);

        // 显示对话包数量信息
        EditorGUILayout.LabelField($"对话包数量: {dialogueDataPacksListProperty.arraySize}", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // 显示列表
        for (int i = 0; i < dialogueDataPacksListProperty.arraySize; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            SerializedProperty packProperty = dialogueDataPacksListProperty.GetArrayElementAtIndex(i);

            // 确保foldoutStates列表有足够的元素
            while (i >= foldoutStates.Count)
            {
                foldoutStates.Add(false);
            }

            // 确保dialogueFoldoutStates列表有足够的元素
            while (i >= dialogueFoldoutStates.Count)
            {
                dialogueFoldoutStates.Add(new List<bool>());
            }

            // 检查这个元素是否是新创建的（需要扩展isNewElement列表）
            while (i >= isNewElement.Count)
            {
                isNewElement.Add(true); // 超过现有范围的是新元素
            }

            // 如果是新元素且ID为0，分配ID
            if (isNewElement[i])
            {
                SerializedProperty idProperty = packProperty.FindPropertyRelative("dialoguePackID");
                if (idProperty.intValue == 0)
                {
                    idProperty.intValue = dialogueManager.GetNextAvailableID();
                }
                isNewElement[i] = false; // 标记为已处理
            }

            // 获取对话包信息用于显示
            SerializedProperty nameProperty = packProperty.FindPropertyRelative("dialoguePackName");
            SerializedProperty idPropertyDisplay = packProperty.FindPropertyRelative("dialoguePackID");

            // 显示对话包头信息（可折叠）
            string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ?
                $"对话包ID:{idPropertyDisplay.intValue}" : nameProperty.stringValue;

            foldoutStates[i] = EditorGUILayout.Foldout(foldoutStates[i], $"{displayName} (ID: {idPropertyDisplay.intValue})", true);

            if (foldoutStates[i])
            {
                EditorGUI.indentLevel++;

                // 绘制对话包基础信息
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("对话包名称"));
                EditorGUILayout.PropertyField(idPropertyDisplay, new GUIContent("对话包ID"));
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);

                // 绘制对话内容列表标题
                EditorGUILayout.LabelField("对话内容列表", EditorStyles.boldLabel);

                // 获取对话内容列表
                SerializedProperty dialogueListProperty = packProperty.FindPropertyRelative("dialogueInfoPacks");

                // 确保对话内容展开状态列表有足够元素
                while (dialogueFoldoutStates[i].Count < dialogueListProperty.arraySize)
                {
                    dialogueFoldoutStates[i].Add(false);
                }

                // 显示对话内容列表
                for (int j = 0; j < dialogueListProperty.arraySize; j++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    SerializedProperty dialogueProperty = dialogueListProperty.GetArrayElementAtIndex(j);

                    // 获取对话内容信息用于显示
                    SerializedProperty speakerNameProperty = dialogueProperty.FindPropertyRelative("speakerName");
                    SerializedProperty contentProperty = dialogueProperty.FindPropertyRelative("dialogueContent");

                    // 显示对话内容头信息（可折叠）
                    string dialogueDisplayName = string.IsNullOrEmpty(speakerNameProperty.stringValue) ?
                        "未命名对话" : speakerNameProperty.stringValue;

                    // 截取部分内容作为预览
                    string previewContent = contentProperty.stringValue;
                    if (previewContent.Length > 30)
                    {
                        previewContent = previewContent.Substring(0, 30) + "...";
                    }

                    dialogueFoldoutStates[i][j] = EditorGUILayout.Foldout(
                        dialogueFoldoutStates[i][j],
                        $"{dialogueDisplayName}: {previewContent}",
                        true
                    );

                    if (dialogueFoldoutStates[i][j])
                    {
                        EditorGUI.indentLevel++;

                        // 绘制对话内容详细信息
                        EditorGUILayout.PropertyField(speakerNameProperty, new GUIContent("说话人"));
                        EditorGUILayout.PropertyField(contentProperty, new GUIContent("对话内容"));

                        SerializedProperty avatarProperty = dialogueProperty.FindPropertyRelative("speakerAvatar");
                        EditorGUILayout.PropertyField(avatarProperty, new GUIContent("说话人头像"));

                        SerializedProperty waitTimerProperty = dialogueProperty.FindPropertyRelative("WaitTimer");
                        EditorGUILayout.PropertyField(waitTimerProperty, new GUIContent("自动播放等待时间(秒)"));

                        EditorGUI.indentLevel--;
                    }

                    // 添加删除按钮
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("删除对话", GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("确认删除",
                            $"确定要删除这条对话吗？", "删除", "取消"))
                        {
                            dialogueListProperty.DeleteArrayElementAtIndex(j);

                            // 同步删除展开状态
                            if (j < dialogueFoldoutStates[i].Count)
                            {
                                dialogueFoldoutStates[i].RemoveAt(j);
                            }

                            serializedObject.ApplyModifiedProperties();
                            return; // 立即返回避免索引越界
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }

                // 添加对话按钮
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("添加对话", GUILayout.Width(100)))
                {
                    dialogueListProperty.arraySize++;

                    // 为新对话项设置默认值
                    SerializedProperty newDialogue = dialogueListProperty.GetArrayElementAtIndex(dialogueListProperty.arraySize - 1);

                    SerializedProperty newSpeakerName = newDialogue.FindPropertyRelative("speakerName");
                    newSpeakerName.stringValue = "新说话人";

                    SerializedProperty newContent = newDialogue.FindPropertyRelative("dialogueContent");
                    newContent.stringValue = "新对话内容";

                    SerializedProperty newWaitTimer = newDialogue.FindPropertyRelative("WaitTimer");
                    newWaitTimer.floatValue = 1.0f;

                    // 添加展开状态
                    dialogueFoldoutStates[i].Add(true);

                    serializedObject.ApplyModifiedProperties();
                }

                // 清空对话列表按钮
                if (GUILayout.Button("清空对话", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("确认清空",
                        $"确定要清空这个对话包中的所有对话吗？", "清空", "取消"))
                    {
                        dialogueListProperty.ClearArray();
                        dialogueFoldoutStates[i].Clear();
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            // 添加删除按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("删除对话包", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                    $"确定要删除对话包 '{displayName}' 吗？", "删除", "取消"))
                {
                    // 记录删除的索引，用于后续处理
                    int deletedIndex = i;

                    // 从列表中删除
                    dialogueDataPacksListProperty.DeleteArrayElementAtIndex(deletedIndex);

                    // 同步删除标记列表中的对应元素
                    if (deletedIndex < isNewElement.Count) isNewElement.RemoveAt(deletedIndex);
                    if (deletedIndex < foldoutStates.Count) foldoutStates.RemoveAt(deletedIndex);
                    if (deletedIndex < dialogueFoldoutStates.Count) dialogueFoldoutStates.RemoveAt(deletedIndex);

                    serializedObject.ApplyModifiedProperties();

                    // 强制重新分配所有ID以确保连续性
                    ReassignAllIDsImmediately();

                    // 设置对象为脏，确保保存
                    EditorUtility.SetDirty(dialogueManager);

                    // 重新获取序列化对象
                    serializedObject.Update();

                    // 立即返回，避免访问已删除的元素
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        // 添加/删除按钮区域
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("添加对话包", GUILayout.Width(120)))
        {
            dialogueDataPacksListProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

            // 标记最后一个元素为新元素
            int newIndex = dialogueDataPacksListProperty.arraySize - 1;

            // 扩展列表
            while (newIndex >= isNewElement.Count)
            {
                isNewElement.Add(true);
            }
            while (newIndex >= foldoutStates.Count)
            {
                foldoutStates.Add(true); // 默认展开新添加的对话包
            }
            while (newIndex >= dialogueFoldoutStates.Count)
            {
                dialogueFoldoutStates.Add(new List<bool>());
            }

            // 标记为新元素
            isNewElement[newIndex] = true;
            foldoutStates[newIndex] = true;

            // 自动为新对话包分配ID
            SerializedProperty newPack = dialogueDataPacksListProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newIDProperty = newPack.FindPropertyRelative("dialoguePackID");
            newIDProperty.intValue = dialogueManager.GetNextAvailableID();

            // 设置默认对话包名称
            SerializedProperty newNameProperty = newPack.FindPropertyRelative("dialoguePackName");
            newNameProperty.stringValue = $"新对话包 {newIDProperty.intValue}";

            // 初始化对话内容列表
            SerializedProperty infoPacksProperty = newPack.FindPropertyRelative("dialogueInfoPacks");
            if (infoPacksProperty != null && !infoPacksProperty.hasMultipleDifferentValues)
            {
                infoPacksProperty.ClearArray();
                infoPacksProperty.arraySize = 0;

                // 添加一个默认的对话
                infoPacksProperty.arraySize = 1;
                SerializedProperty defaultDialogue = infoPacksProperty.GetArrayElementAtIndex(0);

                SerializedProperty defaultSpeakerName = defaultDialogue.FindPropertyRelative("speakerName");
                defaultSpeakerName.stringValue = "默认说话人";

                SerializedProperty defaultContent = defaultDialogue.FindPropertyRelative("dialogueContent");
                defaultContent.stringValue = "这是一个示例对话内容。";

                SerializedProperty defaultWaitTimer = defaultDialogue.FindPropertyRelative("WaitTimer");
                defaultWaitTimer.floatValue = 1.0f;

                // 添加展开状态
                dialogueFoldoutStates[newIndex].Add(true);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogueManager);
        }

        if (GUILayout.Button("重新分配所有ID", GUILayout.Width(120)))
        {
            ReassignAllIDsImmediately();
        }

        // 添加一个清空所有对话包的按钮
        if (GUILayout.Button("清空所有", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("确认清空",
                "确定要清空所有对话包吗？此操作不可撤销。", "清空", "取消"))
            {
                dialogueDataPacksListProperty.ClearArray();
                isNewElement.Clear();
                foldoutStates.Clear();
                dialogueFoldoutStates.Clear();
                serializedObject.ApplyModifiedProperties();

                // 清空后也需要重置ID计数器
                EditorUtility.SetDirty(dialogueManager);
            }
        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        // 应用修改到实际对象
        if (GUI.changed)
        {
            EditorUtility.SetDirty(dialogueManager);
        }
    }

    // 立即重新分配所有ID
    private void ReassignAllIDsImmediately()
    {
        // 确保序列化对象是最新的
        serializedObject.Update();

        // 遍历所有对话包，按顺序分配ID
        for (int i = 0; i < dialogueDataPacksListProperty.arraySize; i++)
        {
            SerializedProperty packProperty = dialogueDataPacksListProperty.GetArrayElementAtIndex(i);
            SerializedProperty idProperty = packProperty.FindPropertyRelative("dialoguePackID");

            // 分配连续ID（从1开始）
            idProperty.intValue = i + 1;
        }

        // 应用修改
        serializedObject.ApplyModifiedProperties();

        // 设置对象为脏
        EditorUtility.SetDirty(dialogueManager);

        // 强制重新绘制Inspector
        Repaint();
    }
}