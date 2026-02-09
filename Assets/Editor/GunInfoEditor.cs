using UnityEditor;
using UnityEngine;

// 标记该编辑器用于GunInfo类
[CustomEditor(typeof(GunInfo))]
public class GunInfoEditor : Editor
{
    // 序列化属性（对应GunInfo的字段，保证数据序列化安全）
    private SerializedProperty _name;
    private SerializedProperty _type;
    private SerializedProperty _description;
    private SerializedProperty _bulletCapacity;
    private SerializedProperty _allBulletAmount;
    private SerializedProperty _rateOfFires;
    private SerializedProperty _accuracy;
    private SerializedProperty _damage;
    private SerializedProperty _range;
    private SerializedProperty _reloadTime;
    private SerializedProperty _bulletSpeed;
    private SerializedProperty _recoil;
    private SerializedProperty _recoilEnemy;
    private SerializedProperty _isCanContinuousShoot;
    private SerializedProperty _gunBodySprite;
    private SerializedProperty _shootAudio;
    private SerializedProperty _bulletFill;
    private SerializedProperty _gunSprite;
    private SerializedProperty _shackStrength;
    private SerializedProperty _shackTime;

    // 散弹枪专属属性
    private SerializedProperty _shotgunBulletAmount;
    private SerializedProperty _shotgunBatchInterval;
    private SerializedProperty _shotgunScatterAngle;

    // 烟雾效果属性
    private SerializedProperty _smokeColor;
    private SerializedProperty _smokeSizeMin;
    private SerializedProperty _smokeSizeMax;
    private SerializedProperty _smokeDuration;
    private SerializedProperty _smokeDecaySpeed;

    // 初始化：获取所有序列化属性（只执行一次）
    private void OnEnable()
    {
        // 基础信息
        _name = serializedObject.FindProperty("Name");
        _type = serializedObject.FindProperty("type");
        _description = serializedObject.FindProperty("description");
        _bulletCapacity = serializedObject.FindProperty("Bullet_capacity");
        _allBulletAmount = serializedObject.FindProperty("AllBulletAmount");
        _rateOfFires = serializedObject.FindProperty("RateOfFires");

        // 精度/伤害/射程
        _accuracy = serializedObject.FindProperty("Accuracy");
        _damage = serializedObject.FindProperty("Damage");
        _range = serializedObject.FindProperty("Range");

        // 换弹/子弹速度
        _reloadTime = serializedObject.FindProperty("ReloadTime");
        _bulletSpeed = serializedObject.FindProperty("BulletSpeed");

        // 后坐力
        _recoil = serializedObject.FindProperty("Recoil");
        _recoilEnemy = serializedObject.FindProperty("Recoil_Enemy");

        // 连射
        _isCanContinuousShoot = serializedObject.FindProperty("IsCanContinuousShoot");

        // 贴图/音效
        _gunBodySprite = serializedObject.FindProperty("GunBodySprite");
        _shootAudio = serializedObject.FindProperty("ShootAudio");
        _bulletFill = serializedObject.FindProperty("BulletFill");
        _gunSprite = serializedObject.FindProperty("GunSprite");

        // 屏幕震动
        _shackStrength = serializedObject.FindProperty("ShackStrength");
        _shackTime = serializedObject.FindProperty("ShackTime");

        // 散弹枪专属
        _shotgunBulletAmount = serializedObject.FindProperty("ShotgunBulletAmount");
        _shotgunBatchInterval = serializedObject.FindProperty("shotgunBatchInterval");
        _shotgunScatterAngle = serializedObject.FindProperty("shotgunScatterAngle");

        // 烟雾效果
        _smokeColor = serializedObject.FindProperty("smokeColor");
        _smokeSizeMin = serializedObject.FindProperty("smokeSizeMin");
        _smokeSizeMax = serializedObject.FindProperty("smokeSizeMax");
        _smokeDuration = serializedObject.FindProperty("smokeDuration");
        _smokeDecaySpeed = serializedObject.FindProperty("smokeDecaySpeed");
    }

    // 重写Inspector绘制逻辑
    public override void OnInspectorGUI()
    {
        // 更新序列化对象（获取最新数据）
        serializedObject.Update();

        // ========== 1. 基础信息分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_name, new GUIContent("枪械名称"));
        EditorGUILayout.PropertyField(_type, new GUIContent("枪械类型"));

        // 描述文本域（保持原有的TextArea样式）
        EditorGUILayout.LabelField("枪械描述");
        _description.stringValue = EditorGUILayout.TextArea(
            _description.stringValue,
            GUILayout.Height(80) // 固定高度，对应原TextArea(3,5)
        );

        EditorGUILayout.PropertyField(_bulletCapacity, new GUIContent("弹匣容量"));
        EditorGUILayout.PropertyField(_allBulletAmount, new GUIContent("总子弹上限"));
        EditorGUILayout.PropertyField(_rateOfFires, new GUIContent("射速"));

        // ========== 2. 枪械性能分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("枪械性能", EditorStyles.boldLabel);

        // 精度（Range滑动条）
        EditorGUILayout.LabelField($"精度: {_accuracy.floatValue:F1}");
        _accuracy.floatValue = EditorGUILayout.Slider(_accuracy.floatValue, 0, 100);

        // 伤害（Range滑动条）
        EditorGUILayout.LabelField($"伤害: {_damage.floatValue:F1}");
        _damage.floatValue = EditorGUILayout.Slider(_damage.floatValue, 0, 100);

        // 射程（Range滑动条）
        EditorGUILayout.LabelField($"射程: {_range.floatValue:F1}");
        _range.floatValue = EditorGUILayout.Slider(_range.floatValue, 100, 500);

        // ========== 3. 换弹/子弹参数分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("换弹/子弹参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_reloadTime, new GUIContent("换弹时间"));
        EditorGUILayout.PropertyField(_bulletSpeed, new GUIContent("子弹初速"));

        // ========== 4. 后坐力分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("后坐力参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_recoil, new GUIContent("自身后坐力"));
        EditorGUILayout.PropertyField(_recoilEnemy, new GUIContent("敌人受击后坐力"));

        // ========== 5. 射击规则分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("射击规则", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_isCanContinuousShoot, new GUIContent("是否连射"));

        // ========== 6. 资源分组（贴图/音效） ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("资源引用", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_gunBodySprite, new GUIContent("枪械本体贴图"));
        EditorGUILayout.PropertyField(_gunSprite, new GUIContent("枪械UI贴图"));
        EditorGUILayout.PropertyField(_shootAudio, new GUIContent("射击音效"));
        EditorGUILayout.PropertyField(_bulletFill, new GUIContent("子弹掉落音效"));

        // ========== 7. 屏幕震动分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("屏幕震动参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_shackStrength, new GUIContent("震动强度"));
        EditorGUILayout.PropertyField(_shackTime, new GUIContent("震动时长"));

        // ========== 8. 烟雾效果分组 ==========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("烟雾效果参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_smokeColor, new GUIContent("烟雾颜色"));
        EditorGUILayout.PropertyField(_smokeSizeMin, new GUIContent("烟雾最小尺寸"));
        EditorGUILayout.PropertyField(_smokeSizeMax, new GUIContent("烟雾最大尺寸"));
        EditorGUILayout.PropertyField(_smokeDuration, new GUIContent("烟雾持续时间"));
        EditorGUILayout.PropertyField(_smokeDecaySpeed, new GUIContent("烟雾衰减速度"));

        // ========== 9. 散弹枪专属分组（仅Shotgun显示） ==========
        if ((GunType)_type.enumValueIndex == GunType.Shotgun)
        {
            EditorGUILayout.Space();
            // 加红色边框突出散弹枪专属
            GUIStyle shotgunStyle = new GUIStyle(EditorStyles.helpBox);
            shotgunStyle.normal.background = MakeTex(2, 2, new Color(1f, 0.8f, 0.8f, 0.5f));
            EditorGUILayout.BeginVertical(shotgunStyle);

            EditorGUILayout.LabelField("散弹枪专属参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_shotgunBulletAmount, new GUIContent("弹丸数量"));
            EditorGUILayout.PropertyField(_shotgunBatchInterval, new GUIContent("批次发射间隔"));
            EditorGUILayout.PropertyField(_shotgunScatterAngle, new GUIContent("散射角度"));

            EditorGUILayout.EndVertical();
        }

        // ========== 保存修改 ==========
        serializedObject.ApplyModifiedProperties();
    }

    // 辅助方法：创建纯色纹理（用于散弹枪分组背景）
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}