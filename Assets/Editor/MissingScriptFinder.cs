using UnityEditor;
using UnityEngine;

public class MissingScriptFinder
{
    [MenuItem("Tools/清理 Missing Script (仅在编辑器使用)")]
    private static void FindAndCleanMissingScripts()
    {
        // 1. 扫描当前打开的场景
        Debug.Log("开始扫描当前场景...");
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(includeInactive: true);
        int cleanedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            // 获取该物体的所有组件
            Component[] components = obj.GetComponents<Component>();
            SerializedObject serializedObject = new SerializedObject(obj);
            SerializedProperty prop = serializedObject.FindProperty("m_Component");

            int r = 0;
            for (int j = 0; j < components.Length; j++)
            {
                if (components[j] == null)
                {
                    Debug.LogWarning($"发现 Missing Script！物体名：{obj.name}，路径：{GetGameObjectPath(obj)}");
                    prop.DeleteArrayElementAtIndex(j - r);
                    r++;
                    cleanedCount++;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        Debug.Log($"场景扫描完成！共清理了 {cleanedCount} 个 Missing Script。记得保存场景！");
    }

    // 获取物体在 Hierarchy 中的完整路径
    private static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }
}