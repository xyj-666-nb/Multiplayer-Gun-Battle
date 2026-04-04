using UnityEditor;
using UnityEngine;

/// <summary>
/// 仅用于给 子弹视觉配置 / 枪口火光配置 分配唯一ID
/// 不修改任何其他数据！
/// </summary>
public class ConfigUniqueIDGenerator : EditorWindow
{
    private int _currentBulletID = 1;
    private int _currentMuzzleID = 1;

    [MenuItem("Tools/配置工具/分配唯一ID(子弹/枪口火光)", false, 110)]
    public static void ShowIDWindow()
    {
        GetWindow<ConfigUniqueIDGenerator>("唯一ID分配工具");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label(" 仅分配唯一ID，不修改任何数据", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button(" 为选中的配置分配ID", GUILayout.Height(30)))
        {
            GenerateIDForSelected();
        }

        if (GUILayout.Button(" 全项目自动分配唯一ID", GUILayout.Height(30)))
        {
            GenerateIDForAll();
        }

        GUILayout.Space(20);
        EditorGUILayout.HelpBox("规则：\n1. 子弹配置 = BulletID 自增\n2. 火光配置 = MuzzleFlashID 自增\n3. 互不干扰，全局唯一", MessageType.Info);
    }

    #region 核心分配逻辑
    private void GenerateIDForSelected()
    {
        _currentBulletID = 1;
        _currentMuzzleID = 1;

        var selection = Selection.objects;
        foreach (var obj in selection)
        {
            AssignIDToConfig(obj);
        }

        SaveAndRefresh();
        EditorUtility.DisplayDialog("完成", "选中配置ID分配完毕！", "确定");
    }

    private void GenerateIDForAll()
    {
        _currentBulletID = 1;
        _currentMuzzleID = 1;

        // 查找所有子弹视觉配置
        AssignAllByType<BulletVisualConfig>();
        // 查找所有枪口火光配置
        AssignAllByType<MuzzleFlashConfig>();

        SaveAndRefresh();
        EditorUtility.DisplayDialog("完成", "全项目ID分配完毕！", "确定");
    }

    private void AssignAllByType<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var config = AssetDatabase.LoadAssetAtPath<T>(path);
            AssignIDToConfig(config);
        }
    }

    private void AssignIDToConfig(Object obj)
    {
        // 分配子弹配置ID
        if (obj is BulletVisualConfig bulletConfig)
        {
            Undo.RecordObject(bulletConfig, "分配BulletID");
            bulletConfig.BulletID = _currentBulletID++;
            EditorUtility.SetDirty(bulletConfig);
            Debug.Log($" 分配子弹ID: {bulletConfig.name} = {bulletConfig.BulletID}", bulletConfig);
        }

        // 分配火光配置ID
        if (obj is MuzzleFlashConfig flashConfig)
        {
            Undo.RecordObject(flashConfig, "分配MuzzleFlashID");
            flashConfig.MuzzleFlashID = _currentMuzzleID++;
            EditorUtility.SetDirty(flashConfig);
            Debug.Log($" 分配火光ID: {flashConfig.name} = {flashConfig.MuzzleFlashID}", flashConfig);
        }
    }
    #endregion

    private void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}