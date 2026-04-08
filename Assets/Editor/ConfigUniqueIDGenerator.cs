using UnityEditor;
using UnityEngine;

/// <summary>
/// 仅用于给配置文件分配全局唯一ID
/// 不修改任何其他数据！
/// </summary>
public class ConfigUniqueIDGenerator : EditorWindow
{
    // ID计数器
    private int _currentBulletID = 1;
    private int _currentMuzzleID = 1;
    private int _currentHitID = 1;
    private int _currentSkinID = 1;
    private int _currentExpressionID = 1;
    private int _currentBulletBindID = 1; // 【新增】子弹捆绑包ID计数器

    [MenuItem("Tools/配置工具/分配唯一ID(全配置)", false, 110)]
    public static void ShowIDWindow()
    {
        GetWindow<ConfigUniqueIDGenerator>("唯一ID分配工具");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label(" 仅分配唯一ID，不修改任何配置数据", EditorStyles.boldLabel);
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
        // 【更新】提示文案
        EditorGUILayout.HelpBox("分配规则：\n1. 子弹配置 = BulletID 全局自增\n2. 火光配置 = MuzzleFlashID 全局自增\n3. 命中特效 = HitID 全局自增\n4. 角色皮肤 = PlayerSkinID 全局自增\n5. 表情配置 = ExpressionID 全局自增\n6. 子弹捆绑包 = BulletBindID 全局自增\n7. 各类型ID互不干扰，全局唯一", MessageType.Info);
    }

    #region 核心分配逻辑
    private void GenerateIDForSelected()
    {
        // 重置计数器
        _currentBulletID = 1;
        _currentMuzzleID = 1;
        _currentHitID = 1;
        _currentSkinID = 1;
        _currentExpressionID = 1;
        _currentBulletBindID = 1; // 【新增】重置子弹捆绑包ID

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
        // 重置计数器
        _currentBulletID = 1;
        _currentMuzzleID = 1;
        _currentHitID = 1;
        _currentSkinID = 1;
        _currentExpressionID = 1;
        _currentBulletBindID = 1; // 【新增】重置子弹捆绑包ID

        // 全类型扫描分配
        AssignAllByType<BulletVisualConfig>();
        AssignAllByType<MuzzleFlashConfig>();
        AssignAllByType<GunHitData>();
        AssignAllByType<PlayerSkinPack>();
        AssignAllByType<ExpressionPack>();
        AssignAllByType<SpecialBulletBindPack>(); // 【新增】子弹捆绑包

        SaveAndRefresh();
        EditorUtility.DisplayDialog("完成", "全项目所有配置ID分配完毕！", "确定");
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
        // 子弹配置ID分配
        if (obj is BulletVisualConfig bulletConfig)
        {
            Undo.RecordObject(bulletConfig, "分配BulletID");
            bulletConfig.BulletID = _currentBulletID++;
            EditorUtility.SetDirty(bulletConfig);
            Debug.Log($" 分配子弹ID: {bulletConfig.name} = {bulletConfig.BulletID}", bulletConfig);
        }

        // 枪口火光配置ID分配
        if (obj is MuzzleFlashConfig flashConfig)
        {
            Undo.RecordObject(flashConfig, "分配MuzzleFlashID");
            flashConfig.MuzzleFlashID = _currentMuzzleID++;
            EditorUtility.SetDirty(flashConfig);
            Debug.Log($" 分配火光ID: {flashConfig.name} = {flashConfig.MuzzleFlashID}", flashConfig);
        }

        // 命中特效配置ID分配
        if (obj is GunHitData hitData)
        {
            Undo.RecordObject(hitData, "分配HitID");
            hitData.HitID = _currentHitID++;
            EditorUtility.SetDirty(hitData);
            Debug.Log($" 分配命中特效ID: {hitData.name} = {hitData.HitID}", hitData);
        }

        // 角色皮肤配置ID分配
        if (obj is PlayerSkinPack skinPack)
        {
            Undo.RecordObject(skinPack, "分配PlayerSkinID");
            skinPack.PlayerSkinID = _currentSkinID++;
            EditorUtility.SetDirty(skinPack);
            Debug.Log($" 分配角色皮肤ID: {skinPack.name} = {skinPack.PlayerSkinID}", skinPack);
        }

        // 表情配置ID分配
        if (obj is ExpressionPack expressionPack)
        {
            Undo.RecordObject(expressionPack, "分配ExpressionID");
            expressionPack.ExpressionID = _currentExpressionID++;
            EditorUtility.SetDirty(expressionPack);
            Debug.Log($" 分配表情ID: {expressionPack.name} = {expressionPack.ExpressionID}", expressionPack);
        }

        if (obj is SpecialBulletBindPack bulletBindPack)
        {
            Undo.RecordObject(bulletBindPack, "分配BulletBindID");
            bulletBindPack.BulletBindID = _currentBulletBindID++;
            EditorUtility.SetDirty(bulletBindPack);
            Debug.Log($" 分配子弹捆绑包ID: {bulletBindPack.name} = {bulletBindPack.BulletBindID}", bulletBindPack);
        }
    }
    #endregion

    private void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}