using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GoodsData))]
public class GoodsDataEditor : Editor
{
    // 基础字段
    private SerializedProperty goodsGuid;
    private SerializedProperty goodsPrice;
    private SerializedProperty skinType;

    // UI字段
    private SerializedProperty goodsIcon;
    private SerializedProperty goodsName;
    private SerializedProperty goodsDescription;
    private SerializedProperty quality;

    // 关联数据字段
    private SerializedProperty bulletPack;
    private SerializedProperty expressionPacks;
    private SerializedProperty playerSkinPack;
    private SerializedProperty gunHitData;
    // 声明枪械皮肤包的属性
    private SerializedProperty gunSkinPack;

    private void OnEnable()
    {
        // 绑定所有序列化属性，确保数据正确保存
        goodsGuid = serializedObject.FindProperty("goodsGuid");
        goodsPrice = serializedObject.FindProperty("goodsPrice");
        skinType = serializedObject.FindProperty("skinType");

        goodsIcon = serializedObject.FindProperty("goodsIcon");
        goodsName = serializedObject.FindProperty("goodsName");
        goodsDescription = serializedObject.FindProperty("goodsDescription");
        quality = serializedObject.FindProperty("quality");

        bulletPack = serializedObject.FindProperty("bulletPack");
        expressionPacks = serializedObject.FindProperty("expressionPacks");
        playerSkinPack = serializedObject.FindProperty("playerSkinPack");
        gunHitData = serializedObject.FindProperty("gunHitData");
        // 绑定枪械皮肤包
        gunSkinPack = serializedObject.FindProperty("gunSkinPack");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ======================  商品唯一标识 ======================
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("【商品唯一标识】", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(goodsGuid);
        GUI.enabled = true;
        EditorGUILayout.Space(10);

        // ======================  基础配置 ======================
        EditorGUILayout.LabelField("【基础配置】", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(goodsPrice);
        EditorGUILayout.PropertyField(skinType);
        EditorGUILayout.Space(10);

        // ======================  UI展示 ======================
        EditorGUILayout.LabelField("【UI展示】", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(goodsIcon);
        EditorGUILayout.PropertyField(goodsName);
        EditorGUILayout.PropertyField(goodsDescription);
        EditorGUILayout.PropertyField(quality);
        EditorGUILayout.Space(15);

        // ======================  关联数据 ======================
        EditorGUILayout.LabelField("【关联数据配置】", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 获取当前选择的皮肤类型
        SkinType currentType = (SkinType)skinType.enumValueIndex;

        // 根据类型动态显示对应字段，其他完全隐藏
        switch (currentType)
        {
            case SkinType.PlayerCharacter:
                EditorGUILayout.PropertyField(playerSkinPack, new GUIContent("角色皮肤捆绑包"));
                break;

            case SkinType.SpecialBullet:
                EditorGUILayout.PropertyField(bulletPack, new GUIContent("特殊子弹捆绑包"), true);
                break;

            case SkinType.GunHitEffect:
                EditorGUILayout.PropertyField(gunHitData, new GUIContent("命中特效数据"));
                break;

            case SkinType.Expression:
                EditorGUILayout.PropertyField(expressionPacks, new GUIContent("表情捆绑包列表"), true);
                break;

            // 枪械皮肤类型的处理分支
            case SkinType.GunAppearance:
                EditorGUILayout.PropertyField(gunSkinPack, new GUIContent("枪械皮肤数据包"));
                break;

            default:
                EditorGUILayout.HelpBox("当前商品类型无需配置关联数据", MessageType.Info);
                break;
        }

        // 应用所有修改，确保数据保存
        serializedObject.ApplyModifiedProperties();
    }
}