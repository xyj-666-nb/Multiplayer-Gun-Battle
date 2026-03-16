using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class FindScriptEverywhere : EditorWindow
{
    private MonoScript _targetScript;

    [MenuItem("Tools/全方位查找脚本")]
    public static void ShowWindow()
    {
        GetWindow<FindScriptEverywhere>("全方位脚本查找器");
    }

    private void OnGUI()
    {
        GUILayout.Label("请拖入要查找的脚本：", EditorStyles.boldLabel);
        _targetScript = (MonoScript)EditorGUILayout.ObjectField("目标脚本", _targetScript, typeof(MonoScript), false);

        if (GUILayout.Button("开始全方位查找") && _targetScript != null)
        {
            FindEverything();
        }
    }

    private void FindEverything()
    {
        var type = _targetScript.GetClass();
        if (type == null)
        {
            Debug.LogError("无法获取脚本类型！");
            return;
        }

        Debug.Log($"========== 开始查找 [{type.Name}] ==========");

        // 1. 查找场景中的物体
        var sceneObjects = FindObjectsOfType(type, includeInactive: true);
        if (sceneObjects.Length > 0)
        {
            Debug.Log($"【场景】找到 {sceneObjects.Length} 个物体：");
            foreach (var obj in sceneObjects)
            {
                Debug.Log($"  - 场景物体：{obj.name}", obj);
            }
        }
        else
        {
            Debug.Log("【场景】未找到");
        }

        // 2. 查找 Assets 里的预制体
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        List<Object> prefabResults = new List<Object>();

        foreach (string guid in allPrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent(type) != null)
            {
                prefabResults.Add(prefab);
            }
        }

        if (prefabResults.Count > 0)
        {
            Debug.Log($"【Assets】找到 {prefabResults.Count} 个预制体：");
            foreach (var prefab in prefabResults)
            {
                Debug.Log($"  - 预制体：{prefab.name}", prefab);
            }
        }
        else
        {
            Debug.Log("【Assets】未找到预制体");
        }

        Debug.Log($"========== 查找完成 ==========");
    }
}