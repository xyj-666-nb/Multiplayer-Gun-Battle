using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SpriteMergerEditor : EditorWindow
{
    // 编辑器窗口单例
    private static SpriteMergerEditor window;

    // 合并配置
    private bool mergeCollider = true; // 是否合并碰撞体
    private bool markStatic = true;    // 是否标记为静态
    private bool deleteOriginal = true;// 是否删除原物体
    private Transform targetParent;    // 目标父物体

    // 顶部菜单入口（Assets/右键/顶部菜单都能调）
    [MenuItem("Tools/2D工具/合并背景植物 ")]
    public static void OpenWindow()
    {
        window = GetWindow<SpriteMergerEditor>("合并背景植物");
        window.minSize = new Vector2(300, 200);
        window.maxSize = new Vector2(300, 200);
    }

    private void OnGUI()
    {
        GUILayout.Label("选择要合并的父物体（包含所有背景植物）", EditorStyles.boldLabel);
        targetParent = (Transform)EditorGUILayout.ObjectField("目标父物体", targetParent, typeof(Transform), true);

        GUILayout.Space(10);
        GUILayout.Label("合并配置", EditorStyles.boldLabel);
        mergeCollider = EditorGUILayout.Toggle("合并碰撞体", mergeCollider);
        markStatic = EditorGUILayout.Toggle("标记为静态", markStatic);
        deleteOriginal = EditorGUILayout.Toggle("删除原零散物体", deleteOriginal);

        GUILayout.Space(20);
        EditorGUI.BeginDisabledGroup(targetParent == null);
        if (GUILayout.Button(" 开始合并", GUILayout.Height(40)))
        {
            MergeSprites();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);
        GUILayout.Label(" 提示：合并前建议备份场景！", EditorStyles.miniLabel);
    }

    /// <summary>
    /// 核心合并逻辑
    /// </summary>
    private void MergeSprites()
    {
        // 安全检查
        if (targetParent == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择包含背景植物的父物体！", "确定");
            return;
        }

        // 获取所有子物体的SpriteRenderer
        List<Transform> childPlants = targetParent.GetComponentsInChildren<Transform>(true)
            .Where(t => t != targetParent && t.GetComponent<SpriteRenderer>() != null)
            .ToList();

        if (childPlants.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "父物体下没有带SpriteRenderer的子物体！", "确定");
            return;
        }

        // 确认合并
        bool confirm = EditorUtility.DisplayDialog(
            "确认合并",
            $"即将合并 {childPlants.Count} 个背景植物物体，是否继续？",
            "确定",
            "取消"
        );
        if (!confirm) return;

        // 开始合并
        Undo.RecordObject(targetParent, "合并背景植物");

        GameObject mergedRoot = new GameObject($"{targetParent.name}_Merged");
        mergedRoot.transform.SetParent(targetParent.parent);
        mergedRoot.transform.position = targetParent.position;
        mergedRoot.transform.rotation = targetParent.rotation;
        mergedRoot.transform.localScale = targetParent.localScale;

        if (mergeCollider)
        {
            // 给根物体添加静态刚体
            Rigidbody2D rootRb = mergedRoot.AddComponent<Rigidbody2D>();
            rootRb.bodyType = RigidbodyType2D.Static;

            // 添加复合碰撞体
            CompositeCollider2D compositeCol = mergedRoot.AddComponent<CompositeCollider2D>();
            // 兼容不同Unity版本的GeometryType枚举
#if UNITY_2021_1_OR_NEWER
            compositeCol.geometryType = CompositeCollider2D.GeometryType.Outlines; // 新版本用Outlines
#else
            compositeCol.geometryType = CompositeCollider2D.GeometryType.Polygons; // 旧版本用Polygons（注意复数）
#endif

            // 收集所有子物体的碰撞体
            foreach (var plant in childPlants)
            {
                Collider2D col = plant.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.usedByComposite = true; // 标记为用于复合碰撞体
                    Rigidbody2D childRb = plant.GetComponent<Rigidbody2D>();
                    if (childRb != null)
                    {
                        Undo.DestroyObjectImmediate(childRb); // 安全删除子物体刚体
                    }
                }
            }
        }

        if (markStatic)
        {
            mergedRoot.gameObject.isStatic = true;
        }

        foreach (var plant in childPlants)
        {
            if (deleteOriginal)
            {
                Undo.DestroyObjectImmediate(plant.gameObject); // 支持撤销的删除
            }
            else
            {
                plant.SetParent(mergedRoot.transform); // 仅移动层级，保留物体
            }
        }

        if (deleteOriginal)
        {
            Undo.DestroyObjectImmediate(targetParent.gameObject);
        }

        Selection.activeGameObject = mergedRoot;

        EditorUtility.DisplayDialog(
            "成功",
            $"已合并 {childPlants.Count} 个背景植物物体！\n合并后的物体：{mergedRoot.name}",
            "确定"
        );
    }

    /// <summary>
    /// 右键菜单快速合并
    /// </summary>
    [MenuItem("GameObject/2D工具/快速合并背景植物 ", false, 10)]
    public static void QuickMerge()
    {
        if (Selection.activeTransform == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选中包含背景植物的父物体！", "确定");
            return;
        }

        window = GetWindow<SpriteMergerEditor>("合并背景植物");
        window.targetParent = Selection.activeTransform;
        window.mergeCollider = true;
        window.markStatic = true;
        window.deleteOriginal = true;
        window.MergeSprites();
    }
}