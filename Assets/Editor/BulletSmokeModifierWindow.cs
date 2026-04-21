using UnityEngine;
using UnityEditor;

public class BatchConfigEditor : EditorWindow
{
    // 公共选择
    private GunType targetGunType;

    // 子弹烟雾参数
    private Color smokeColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    private float smokeSizeMin = 0.5f;
    private float smokeSizeMax = 1.2f;
    private float smokeDuration = 0.3f;
    private float smokeDecaySpeed = 10f;

    // 路径
    private readonly string bulletPath = "GameInfo/BulletInfo";
    private readonly string gunPath = "GameInfo/GunInfo";

    [MenuItem("Tools/配置批量修改工具")]
    public static void ShowWindow()
    {
        GetWindow<BatchConfigEditor>("批量修改工具");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("按枪械类型批量修改配置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 选择枪械类型
        targetGunType = (GunType)EditorGUILayout.EnumPopup("目标枪械类型", targetGunType);
        GUILayout.Space(15);

        // ===================== 子弹烟雾修改 =====================
        GUILayout.Label("【子弹蛋壳烟雾配置】", EditorStyles.boldLabel);
        smokeColor = EditorGUILayout.ColorField("烟雾颜色", smokeColor);
        smokeSizeMin = EditorGUILayout.FloatField("烟雾最小大小", smokeSizeMin);
        smokeSizeMax = EditorGUILayout.FloatField("烟雾最大大小", smokeSizeMax);
        smokeDuration = EditorGUILayout.FloatField("烟雾持续时间", smokeDuration);
        smokeDecaySpeed = EditorGUILayout.FloatField("烟雾衰减速度", smokeDecaySpeed);

        if (GUILayout.Button("批量修改子弹配置", GUILayout.Height(30)))
        {
            ModifyBulletConfig();
        }

        GUILayout.Space(20);

        // ===================== 枪械配置修改 =====================
        GUILayout.Label("【枪械视觉配置】", EditorStyles.boldLabel);
        GUILayout.Label("批量修改同类型所有枪械配置", EditorStyles.miniLabel);

        if (GUILayout.Button("批量修改枪械配置", GUILayout.Height(30)))
        {
            ModifyGunConfig();
        }
    }

    // 批量修改子弹烟雾（原逻辑）
    private void ModifyBulletConfig()
    {
        BulletVisualConfig[] configs = Resources.LoadAll<BulletVisualConfig>(bulletPath);
        if (configs == null || configs.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到子弹配置文件！", "确定");
            return;
        }

        int count = 0;
        foreach (var item in configs)
        {
            if (item.gunType == targetGunType)
            {
                item.smokeColor = smokeColor;
                item.smokeSizeMin = smokeSizeMin;
                item.smokeSizeMax = smokeSizeMax;
                item.smokeDuration = smokeDuration;
                item.smokeDecaySpeed = smokeDecaySpeed;

                EditorUtility.SetDirty(item);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"成功修改 {count} 个子弹配置！", "确定");
    }

    // 批量修改枪械（和子弹完全同理）
    private void ModifyGunConfig()
    {
        // 加载GunInfo下所有枪械ScriptableObject
        ScriptableObject[] gunConfigs = Resources.LoadAll<ScriptableObject>(gunPath);

        if (gunConfigs == null || gunConfigs.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "未找到枪械配置文件！", "确定");
            return;
        }

        int count = 0;
        foreach (var obj in gunConfigs)
        {
            // 通过反射获取gunType字段（兼容所有枪械配置类）
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty typeProp = so.FindProperty("gunType");

            if (typeProp != null && (GunType)typeProp.enumValueIndex == targetGunType)
            {
                // 这里可以添加你要批量修改的枪械字段
                // 示例：so.FindProperty("gunName").stringValue = "新名称";
                EditorUtility.SetDirty(obj);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"成功修改 {count} 个枪械配置！", "确定");
    }
}