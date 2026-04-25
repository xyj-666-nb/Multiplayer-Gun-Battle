using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(GameSkinManager))]
public class GameSkinManagerEditor : Editor
{
    // 资源路径配置
    private const string BulletBundleRootPath = "GameInfo/BulletBindPack";
    private const string PlayerSkinRootPath = "GameInfo/playerSkipInfo";
    private const string GunHitDataRootPath = "GameInfo/HitobjInfo";
    private const string GunSkinRootPath = "GameInfo/GunSkinInfo";

    private GameSkinManager _targetManager;
    private readonly Color _moduleBgColor = new Color(0.95f, 0.97f, 1f); // 模块背景色
    private readonly Color _quickBtnColor = new Color(0.9f, 1f, 0.9f);   // 快捷按钮颜色

    private void OnEnable()
    {
        _targetManager = (GameSkinManager)target;
    }

    public override void OnInspectorGUI()
    {
        // 绘制默认脚本面板
        base.OnInspectorGUI();

        // ====================== 【工具总标题】 ======================
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(" 皮肤配置自动化工具", EditorStyles.boldLabel);
        DrawLine(Color.gray);
        EditorGUILayout.Space(5);

        // ====================== 【模块1：全局一键加载】 ======================
        DrawModuleHeader(" 全局加载操作");
        GUI.backgroundColor = _moduleBgColor;
        EditorGUILayout.BeginVertical("Box");
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button(" 一键加载所有配置（全类型）", GUILayout.Height(35)))
        {
            LoadAllConfigAssets();
        }
        EditorGUILayout.HelpBox("自动扫描Resources，加载：子弹包/角色皮肤/枪械皮肤/打击特效", MessageType.Info);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);

        // ====================== 【模块2：独立单项加载】 ======================
        DrawModuleHeader(" 独立配置加载");
        GUI.backgroundColor = _moduleBgColor;
        EditorGUILayout.BeginVertical("Box");
        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(" 子弹捆绑包", GUILayout.Height(28))) LoadBulletBundleAssets();
        if (GUILayout.Button(" 角色皮肤", GUILayout.Height(28))) LoadPlayerSkinAssets();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(" 枪械皮肤", GUILayout.Height(28))) LoadGunSkinAssets();
        if (GUILayout.Button(" 打击特效", GUILayout.Height(28))) LoadGunHitDataAssets();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);

        // ====================== 【模块3：快捷解锁功能】 ======================
        DrawModuleHeader(" 快捷解锁功能");
        GUI.backgroundColor = _quickBtnColor;
        EditorGUILayout.BeginVertical("Box");
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button(" 解锁所有角色皮肤 → 玩家拥有列表", GUILayout.Height(30)))
        {
            AutoSetAllPlayerSkinToOwned();
        }
        if (GUILayout.Button(" 解锁所有枪械皮肤 → 玩家拥有列表", GUILayout.Height(30)))
        {
            AutoSetAllGunSkinToOwned();
        }
        EditorGUILayout.HelpBox("一键将所有预载皮肤添加到玩家拥有列表，用于测试", MessageType.None);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    // 绘制分隔线
    private void DrawLine(Color color, int height = 1)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height);
        EditorGUI.DrawRect(rect, color);
    }

    // 绘制模块标题
    private void DrawModuleHeader(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    #region 核心加载逻辑（功能完全不变）
    private void LoadAllConfigAssets()
    {
        Undo.RecordObject(_targetManager, "一键加载所有配置");

        LoadBulletBundleAssets(false);
        LoadPlayerSkinAssets(false);
        LoadGunHitDataAssets(false);
        LoadGunSkinAssets(false);

        EditorUtility.SetDirty(_targetManager);
        serializedObject.ApplyModifiedProperties();

        int bundleCount = _targetManager.AllBulletBundleList?.Count ?? 0;
        int skinCount = _targetManager.AllPlayerSkinPackList?.Count ?? 0;
        int hitCount = _targetManager.AllGunHitDataList?.Count ?? 0;
        int gunSkinCount = _targetManager.AllGunSkinPackList?.Count ?? 0;

        Debug.Log($"<color=cyan>【全配置加载完成】</color>\n" +
                  $"子弹捆绑包：{bundleCount}\n" +
                  $"角色皮肤：{skinCount}\n" +
                  $"打击特效：{hitCount}\n" +
                  $"枪械皮肤：{gunSkinCount}");
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

    private void LoadGunSkinAssets(bool isSingleCall = true)
    {
        if (isSingleCall) Undo.RecordObject(_targetManager, "加载枪械皮肤");
        var configs = Resources.LoadAll<GunSkinPack>(GunSkinRootPath);
        _targetManager.AllGunSkinPackList = new List<GunSkinPack>(configs);
        if (isSingleCall) { EditorUtility.SetDirty(_targetManager); serializedObject.ApplyModifiedProperties(); }
    }

    private void AutoSetAllPlayerSkinToOwned()
    {
        Undo.RecordObject(_targetManager, "一键获取所有角色皮肤");
        _targetManager.PlayerOwnerSkinPackList = new List<PlayerSkinPack>(_targetManager.AllPlayerSkinPackList);
        EditorUtility.SetDirty(_targetManager);
        serializedObject.ApplyModifiedProperties();
        /* Debug.Log($"<color=green>【快捷赋值成功】</color> 已将所有角色皮肤赋值给玩家拥有列表，数量：{_targetManager.PlayerOwnerSkinPackList.Count}"); */
    }

    private void AutoSetAllGunSkinToOwned()
    {
        Undo.RecordObject(_targetManager, "一键获取所有枪械皮肤");
        _targetManager.CurrentGunSkinPackList = new List<GunSkinPack>(_targetManager.AllGunSkinPackList);
        EditorUtility.SetDirty(_targetManager);
        serializedObject.ApplyModifiedProperties();
        /* Debug.Log($"<color=green>【快捷赋值成功】</color> 已将所有枪械皮肤赋值给玩家拥有列表，数量：{_targetManager.CurrentGunSkinPackList.Count}"); */
    }
    #endregion
}
