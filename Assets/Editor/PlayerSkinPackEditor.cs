using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerSkinPack))]
public class PlayerSkinPackEditor : Editor
{
    private SerializedProperty playerSkinID;
    private SerializedProperty playerSkinName;
    private SerializedProperty playerSkinDescription;
    private SerializedProperty idleSprite;
    private SerializedProperty skinQuality;
    private SerializedProperty isHaveAnima;
    private SerializedProperty animaSpriteList;


    private void OnEnable()
    {
        playerSkinID = serializedObject.FindProperty("PlayerSkinID");
        playerSkinName = serializedObject.FindProperty("PlayerSkinName");
        playerSkinDescription = serializedObject.FindProperty("PlayerSkinDescription");
        idleSprite = serializedObject.FindProperty("IdleSprite");
        skinQuality = serializedObject.FindProperty("SkinQuality");
        isHaveAnima = serializedObject.FindProperty("IsHaveAnima");
        animaSpriteList = serializedObject.FindProperty("AnimaSpriteList");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 基础信息
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playerSkinID);
        EditorGUILayout.PropertyField(playerSkinName);
        EditorGUILayout.PropertyField(playerSkinDescription);
        EditorGUILayout.PropertyField(idleSprite);
        EditorGUILayout.PropertyField(skinQuality);

        EditorGUILayout.Space();

        // 动画设置
        EditorGUILayout.LabelField("动画设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isHaveAnima);

        if (isHaveAnima.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(animaSpriteList, new GUIContent("主动画序列"));

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}