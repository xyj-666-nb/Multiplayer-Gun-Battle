using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(GameSkinManager))]
public class GameSkinManagerEditor : Editor
{
    // 自动匹配你 Resources 下的文件夹结构
    private const string BulletBundleRootPath = "GameInfo/BulletBindPack";
    private const string PlayerSkinRootPath = "GameInfo/playerSkipInfo";
    private const string GunHitDataRootPath = "GameInfo/HitobjInfo";

    private GameSkinManager _targetManager;

    private void OnEnable()
    {
        _targetManager = (GameSkinManager)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("========================================", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("【全自动配置加载工具】", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("点击按钮自动扫描 Resources 目录，一键加载所有配置", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button(" 一键加载所有配置（捆绑包/皮肤/打击特效）", GUILayout.Height(35)))
        {
            LoadAllConfigAssets();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("仅加载子弹捆绑包")) LoadBulletBundleAssets();
        if (GUILayout.Button("仅加载角色皮肤")) LoadPlayerSkinAssets();
        EditorGUILayout.EndHorizontal();

        // ====================== 【新增：一键获取所有角色皮肤】 ======================
        if (GUILayout.Button("【快捷】一键获取所有角色皮肤 → 赋值给拥有列表", GUILayout.Height(30)))
        {
            AutoSetAllPlayerSkinToOwned();
        }
        // ==========================================================================

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("仅加载打击特效")) LoadGunHitDataAssets();
        EditorGUILayout.EndHorizontal();
    }

    #region 核心加载逻辑
    private void LoadAllConfigAssets()
    {
        Undo.RecordObject(_targetManager, "一键加载所有配置");

        LoadBulletBundleAssets(false);
        LoadPlayerSkinAssets(false);
        LoadGunHitDataAssets(false);

        EditorUtility.SetDirty(_targetManager);
        serializedObject.ApplyModifiedProperties();

        int bundleCount = _targetManager.AllBulletBundleList?.Count ?? 0;
        int skinCount = _targetManager.AllPlayerSkinPackList?.Count ?? 0;
        int hitCount = _targetManager.AllGunHitDataList?.Count ?? 0;

        Debug.Log($"<color=cyan>【全配置加载完成】</color>\n" +
                  $"子弹捆绑包：{bundleCount}\n" +
                  $"皮肤：{skinCount}\n" +
                  $"打击特效：{hitCount}");
    }

    private void LoadBulletBundleAssets(bool isSingleCall = true)
    {
        if (isSingleCall) Undo.RecordObject(_targetManager, "加载子弹捆绑包");
        var configs = Resources.LoadAll<SpecialBulletBindPack>(BulletBundleRootPath);
        _targetManager.AllBulletBundleList = new List<SpecialBulletBindPack>(configs);
        if (isSingleCall) { EditorUtility.SetDirty(_targetManager); serializedObject.ApplyModifiedProperties(); }
    }

    private void LoadPlayerSkinAssets(bool isSingleCall = true)
    {
        if (isSingleCall) Undo.RecordObject(_targetManager, "加载角色皮肤");
        var configs = Resources.LoadAll<PlayerSkinPack>(PlayerSkinRootPath);
        _targetManager.AllPlayerSkinPackList = new List<PlayerSkinPack>(configs);
        if (isSingleCall) { EditorUtility.SetDirty(_targetManager); serializedObject.ApplyModifiedProperties(); }
    }

    private void LoadGunHitDataAssets(bool isSingleCall = true)
    {
        if (isSingleCall) Undo.RecordObject(_targetManager, "加载打击特效");
        var configs = Resources.LoadAll<GunHitData>(GunHitDataRootPath);
        _targetManager.AllGunHitDataList = new List<GunHitData>(configs);
        if (isSingleCall) { EditorUtility.SetDirty(_targetManager); serializedObject.ApplyModifiedProperties(); }
    }

    // ====================== 【新增：一键赋值所有皮肤给玩家拥有列表】 ======================
    private void AutoSetAllPlayerSkinToOwned()
    {
        Undo.RecordObject(_targetManager, "一键获取所有角色皮肤");

        // 把 AllPlayerSkinPackList 全部赋值给 PlayerOwnerSkinPackList
        _targetManager.PlayerOwnerSkinPackList = new List<PlayerSkinPack>(_targetManager.AllPlayerSkinPackList);

        EditorUtility.SetDirty(_targetManager);
        serializedObject.ApplyModifiedProperties();

        Debug.Log($"<color=green>【快捷赋值成功】</color> 已将所有角色皮肤赋值给玩家拥有列表，数量：{_targetManager.PlayerOwnerSkinPackList.Count}");
    }
    // ==================================================================================
    #endregion
}