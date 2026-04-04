using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GoodsData))]
public class GoodsDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GoodsData goods = (GoodsData)target;

        // 显示 GUID（只读）
        GUI.enabled = false;
        EditorGUILayout.TextField("商品唯一GUID", goods.goodsGuid);
        GUI.enabled = true;

        GUILayout.Space(10);

        // 绘制原有字段
        DrawDefaultInspector();
    }
}