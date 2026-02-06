//using UnityEngine;
//using UnityEditor;
//using Cinemachine;
//using System.Collections.Generic;

//[CustomEditor(typeof(MyCameraControl))]
//[CanEditMultipleObjects]
//public class CameraControlEditor : Editor
//{
//    #region 折叠组状态（纯文本命名）
//    private bool _折叠组_组件状态 = true;       // 组件状态检查
//    private bool _折叠组_基础设置 = true;         // 虚拟相机基础配置
//    private bool _折叠组_镜头设置 = true;         // 镜头参数配置
//    private bool _折叠组_过渡参数 = true;         // 相机切换过渡配置
//    private bool _折叠组_组件管道 = true;         // Cinemachine组件管道管理
//    private bool _折叠组_震动控制 = true;         // 震动调试
//    private bool _折叠组_缩放控制 = true;         // 缩放调试
//    private bool _折叠组_相机模式 = true;         // 相机模式切换
//    private bool _折叠组_调试工具 = true;         // 快捷调试工具
//    #endregion

//    #region 临时调试缓存（纯文本命名）
//    // 震动调试
//    private float _临时_震动强度 = 0.5f;
//    private float _临时_震动时长 = 1f;
//    private bool _临时_启用距离衰减 = false;
//    private Transform _临时_震动源位置;
//    private Transform _临时_目标位置;

//    // 缩放调试
//    private float _临时_缩放目标尺寸 = 8f;
//    private float _临时_缩放停留时间 = 2f;
//    private float _临时_缩放速度 = 2f;
//    private ZoomTaskType _临时_缩放类型 = ZoomTaskType.TemporaryWithTime;

//    // 调试工具
//    private Vector3 _临时_强制位置 = Vector3.zero;
//    private Quaternion _临时_强制旋转 = Quaternion.identity;
//    #endregion

//    #region 核心引用（添加null保护）
//    private SerializedProperty _prop_虚拟相机;
//    private SerializedProperty _prop_主相机;
//    private SerializedProperty _prop_震动衰减速度;
//    private SerializedProperty _prop_距离衰减强度;
//    private SerializedProperty _prop_当前相机模式;
//    private SerializedProperty _prop_偏移位置;
//    private SerializedProperty _prop_区域锁定包;

//    // CinemachineVirtualCamera核心属性（反射获取，兼容源码）
//    private SerializedProperty _prop_vcam_LookAt;
//    private SerializedProperty _prop_vcam_Follow;
//    private SerializedProperty _prop_vcam_Lens;
//    private SerializedProperty _prop_vcam_Transitions;
//    private SerializedProperty _prop_vcam_Priority;
//    private SerializedProperty _prop_vcam_Enabled;
//    #endregion

//    #region 编辑器样式缓存（关键修复：避免重复创建）
//    private GUIStyle _折叠组标题样式;
//    private GUIStyle _盒子样式;
//    private GUIStyle _按钮样式;

//    // 初始化样式（只执行一次）
//    private void InitStyles()
//    {
//        if (_折叠组标题样式 == null)
//        {
//            _折叠组标题样式 = new GUIStyle(EditorStyles.foldout)
//            {
//                fontStyle = FontStyle.Bold,
//                fontSize = 12,
//                normal = { textColor = new Color(0.2f, 0.5f, 0.8f) }
//            };
//        }

//        if (_盒子样式 == null)
//        {
//            _盒子样式 = new GUIStyle("box")
//            {
//                padding = new RectOffset(10, 10, 10, 10),
//                stretchWidth = true // 自适应宽度，防止溢出
//            };
//        }

//        if (_按钮样式 == null)
//        {
//            _按钮样式 = new GUIStyle(GUI.skin.button)
//            {
//                fontSize = 11,
//                padding = new RectOffset(5, 5, 3, 3),
//                fixedWidth = 150f // 固定按钮宽度，防止溢出
//            };
//        }
//    }
//    #endregion

//    #region 初始化（添加严格的null检查）
//    private void OnEnable()
//    {
//        // 初始化样式
//        InitStyles();

//        // 安全获取序列化属性（避免属性名错误导致null）
//        _prop_虚拟相机 = TryGetSerializedProperty("virtualCamera");
//        _prop_主相机 = TryGetSerializedProperty("MainCamera");
//        _prop_震动衰减速度 = TryGetSerializedProperty("_shakeFadeSpeed");
//        _prop_距离衰减强度 = TryGetSerializedProperty("DistanceDecay");
//        _prop_当前相机模式 = TryGetSerializedProperty("CurrentCameraMode");
//        _prop_偏移位置 = TryGetSerializedProperty("OffsetPos");
//        _prop_区域锁定包 = TryGetSerializedProperty("areaLockingPack");

//        // 获取目标脚本
//        MyCameraControl targetScript = target as MyCameraControl;
//        if (targetScript != null && targetScript.virtualCamera != null)
//        {
//            SerializedObject vcamSO = new SerializedObject(targetScript.virtualCamera);
//            // 安全获取虚拟相机属性
//            _prop_vcam_LookAt = TryGetSerializedProperty(vcamSO, "m_LookAt");
//            _prop_vcam_Follow = TryGetSerializedProperty(vcamSO, "m_Follow");
//            _prop_vcam_Lens = TryGetSerializedProperty(vcamSO, "m_Lens");
//            _prop_vcam_Transitions = TryGetSerializedProperty(vcamSO, "m_Transitions");
//            _prop_vcam_Priority = TryGetSerializedProperty(vcamSO, "m_Priority");
//            _prop_vcam_Enabled = TryGetSerializedProperty(vcamSO, "m_Enabled");
//            vcamSO.ApplyModifiedProperties();
//        }
//    }

//    /// <summary>
//    /// 安全获取序列化属性（避免null）
//    /// </summary>
//    private SerializedProperty TryGetSerializedProperty(string propertyName)
//    {
//        SerializedProperty prop = serializedObject.FindProperty(propertyName);
//        if (prop == null)
//        {
//            Debug.LogWarning($"未找到序列化属性：{propertyName}，请检查属性名是否正确");
//        }
//        return prop;
//    }

//    /// <summary>
//    /// 重载：从指定SerializedObject获取属性
//    /// </summary>
//    private SerializedProperty TryGetSerializedProperty(SerializedObject so, string propertyName)
//    {
//        if (so == null)
//        {
//            Debug.LogWarning("SerializedObject为null，无法获取属性");
//            return null;
//        }
//        SerializedProperty prop = so.FindProperty(propertyName);
//        if (prop == null)
//        {
//            Debug.LogWarning($"未找到序列化属性：{propertyName}，请检查属性名是否正确");
//        }
//        return prop;
//    }
//    #endregion

//    public override void OnInspectorGUI()
//    {
//        // 每次绘制前检查样式是否初始化（防止意外销毁）
//        InitStyles();

//        // 基础null检查：如果目标对象为null，直接返回（关键修复）
//        MyCameraControl targetScript = target as MyCameraControl;
//        if (targetScript == null)
//        {
//            EditorGUILayout.LabelField("错误：目标脚本为null");
//            return;
//        }

//        // 开始属性修改检查（规范属性修改，减少布局抖动）
//        EditorGUI.BeginChangeCheck();
//        serializedObject.Update();

//        // ==============================================
//        // 1. 组件状态检查（核心自检）
//        // ==============================================
//        _折叠组_组件状态 = EditorGUILayout.Foldout(_折叠组_组件状态, "【组件状态检查】", _折叠组标题样式);
//        if (_折叠组_组件状态)
//        {
//            // 限制最大宽度，防止溢出（关键修复）
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            // 虚拟相机状态
//            EditorGUILayout.LabelField("虚拟相机状态:", EditorStyles.boldLabel);
//            if (targetScript.virtualCamera != null)
//            {
//                EditorGUILayout.LabelField("绑定状态", "已绑定: " + targetScript.virtualCamera.name);

//                // 检查Noise组件（震动核心）
//                CinemachineBasicMultiChannelPerlin noise = targetScript.virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
//                if (noise != null)
//                {
//                    EditorGUILayout.LabelField("震动组件", "已添加: Basic Multi Channel Perlin");
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("震动组件", "缺失（震动功能不可用）", EditorStyles.whiteLabel);
//                    if (GUILayout.Button("一键添加震动组件", _按钮样式))
//                    {
//                        targetScript.virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
//                        EditorUtility.SetDirty(targetScript.virtualCamera);
//                        AssetDatabase.SaveAssets();
//                    }
//                }

//                // 检查CinemachineBrain（相机驱动核心）
//                CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
//                if (brain != null)
//                {
//                    EditorGUILayout.LabelField("相机大脑", "已存在: " + brain.gameObject.name);
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("相机大脑", "缺失（虚拟相机无法驱动主相机）", EditorStyles.whiteLabel);
//                    if (GUILayout.Button("一键添加相机大脑", _按钮样式))
//                    {
//                        Camera mainCam = Camera.main;
//                        if (mainCam != null)
//                        {
//                            mainCam.gameObject.AddComponent<CinemachineBrain>();
//                            EditorUtility.SetDirty(mainCam.gameObject);
//                            AssetDatabase.SaveAssets();
//                        }
//                        else
//                        {
//                            EditorUtility.DisplayDialog("错误", "未找到主相机！", "确定");
//                        }
//                    }
//                }
//            }
//            else
//            {
//                EditorGUILayout.LabelField("绑定状态", "未绑定虚拟相机", EditorStyles.whiteLabel);
//                if (GUILayout.Button("自动查找虚拟相机", _按钮样式))
//                {
//                    targetScript.virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
//                    EditorUtility.SetDirty(targetScript);
//                    serializedObject.ApplyModifiedProperties();
//                }
//                // 安全绘制属性：先检查是否为null
//                if (_prop_虚拟相机 != null)
//                {
//                    EditorGUILayout.PropertyField(_prop_虚拟相机, new GUIContent("手动绑定虚拟相机"));
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("手动绑定虚拟相机：属性不存在");
//                }
//            }

//            // 主相机状态
//            EditorGUILayout.Space();
//            EditorGUILayout.LabelField("主相机状态:", EditorStyles.boldLabel);
//            if (targetScript.MainCamera != null)
//            {
//                EditorGUILayout.LabelField("绑定状态", "已绑定: " + targetScript.MainCamera.name);
//            }
//            else
//            {
//                EditorGUILayout.LabelField("绑定状态", "未绑定主相机", EditorStyles.whiteLabel);
//                // 安全绘制属性
//                if (_prop_主相机 != null)
//                {
//                    EditorGUILayout.PropertyField(_prop_主相机, new GUIContent("手动绑定主相机"));
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("手动绑定主相机：属性不存在");
//                }
//            }

//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 2. 虚拟相机基础设置（源码核心属性）
//        // ==============================================
//        _折叠组_基础设置 = EditorGUILayout.Foldout(_折叠组_基础设置, "【虚拟相机基础设置】", _折叠组标题样式);
//        if (_折叠组_基础设置 && targetScript.virtualCamera != null)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            SerializedObject vcamSO = new SerializedObject(targetScript.virtualCamera);

//            // 跟随目标（Follow）- 安全绘制
//            if (_prop_vcam_Follow != null)
//            {
//                EditorGUILayout.PropertyField(_prop_vcam_Follow, new GUIContent(
//                    "跟随目标 (Body)", "相机要跟随移动的目标，Body组件会基于此定位相机位置"));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("跟随目标 (Body)：属性不存在");
//            }

//            // 看向目标（LookAt）- 安全绘制
//            if (_prop_vcam_LookAt != null)
//            {
//                EditorGUILayout.PropertyField(_prop_vcam_LookAt, new GUIContent(
//                    "看向目标 (Aim)", "相机要朝向的目标，Aim组件会基于此调整相机朝向"));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("看向目标 (Aim)：属性不存在");
//            }

//            // 优先级（影响CinemachineBrain选择活跃相机）- 安全绘制
//            if (_prop_vcam_Priority != null)
//            {
//                EditorGUILayout.PropertyField(_prop_vcam_Priority, new GUIContent(
//                    "优先级", "数值越高，越优先成为活跃相机（范围：0-100）"));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("优先级：属性不存在");
//            }

//            // 启用状态 - 安全绘制
//            if (_prop_vcam_Enabled != null)
//            {
//                EditorGUILayout.PropertyField(_prop_vcam_Enabled, new GUIContent(
//                    "启用状态", "是否启用该虚拟相机"));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("启用状态：属性不存在");
//            }

//            vcamSO.ApplyModifiedProperties();
//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 3. 镜头参数设置（正交/透视/物理相机）
//        // ==============================================
//        _折叠组_镜头设置 = EditorGUILayout.Foldout(_折叠组_镜头设置, "【镜头参数设置】", _折叠组标题样式);
//        if (_折叠组_镜头设置 && targetScript.virtualCamera != null)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            SerializedObject vcamSO = new SerializedObject(targetScript.virtualCamera);
//            // 先检查Lens属性是否存在
//            if (_prop_vcam_Lens != null)
//            {
//                // 镜头类型（正交/透视）
//                SerializedProperty orthoProp = _prop_vcam_Lens.FindPropertyRelative("m_Orthographic");
//                if (orthoProp != null)
//                {
//                    EditorGUILayout.PropertyField(orthoProp, new GUIContent(
//                        "正交相机", "2D游戏常用，无透视效果"));

//                    if (orthoProp.boolValue)
//                    {
//                        // 正交尺寸（2D核心参数）
//                        SerializedProperty orthoSizeProp = _prop_vcam_Lens.FindPropertyRelative("m_OrthographicSize");
//                        if (orthoSizeProp != null)
//                        {
//                            EditorGUILayout.PropertyField(orthoSizeProp, new GUIContent(
//                                "正交尺寸", "正交相机的视野大小，数值越小，画面放大"));
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("正交尺寸：属性不存在");
//                        }
//                    }
//                    else
//                    {
//                        // 透视相机参数
//                        SerializedProperty fovProp = _prop_vcam_Lens.FindPropertyRelative("m_FieldOfView");
//                        if (fovProp != null)
//                        {
//                            EditorGUILayout.PropertyField(fovProp, new GUIContent(
//                                "视野角度 (FOV)", "透视相机的视野范围，数值越大，视野越广（范围：10-170）"));
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("视野角度 (FOV)：属性不存在");
//                        }

//                        // 近裁剪面
//                        SerializedProperty nearClipProp = _prop_vcam_Lens.FindPropertyRelative("m_NearClipPlane");
//                        if (nearClipProp != null)
//                        {
//                            EditorGUILayout.PropertyField(nearClipProp, new GUIContent(
//                                "近裁剪面", "相机能看到的最近距离（建议：0.1-1）"));
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("近裁剪面：属性不存在");
//                        }

//                        // 远裁剪面
//                        SerializedProperty farClipProp = _prop_vcam_Lens.FindPropertyRelative("m_FarClipPlane");
//                        if (farClipProp != null)
//                        {
//                            EditorGUILayout.PropertyField(farClipProp, new GUIContent(
//                                "远裁剪面", "相机能看到的最远距离（建议：100-1000）"));
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("远裁剪面：属性不存在");
//                        }
//                    }

//                    // 物理相机参数（高级）
//                    SerializedProperty physicalCamProp = _prop_vcam_Lens.FindPropertyRelative("m_IsPhysicalCamera");
//                    if (physicalCamProp != null)
//                    {
//                        EditorGUILayout.PropertyField(physicalCamProp, new GUIContent(
//                            "物理相机", "模拟真实相机的光学参数"));
//                        if (physicalCamProp.boolValue)
//                        {
//                            SerializedProperty focalLengthProp = _prop_vcam_Lens.FindPropertyRelative("m_FocalLength");
//                            if (focalLengthProp != null)
//                            {
//                                EditorGUILayout.PropertyField(focalLengthProp, new GUIContent("焦距 (mm)"));
//                            }
//                            else
//                            {
//                                EditorGUILayout.LabelField("焦距 (mm)：属性不存在");
//                            }

//                            SerializedProperty sensorSizeProp = _prop_vcam_Lens.FindPropertyRelative("m_SensorSize");
//                            if (sensorSizeProp != null)
//                            {
//                                EditorGUILayout.PropertyField(sensorSizeProp, new GUIContent("传感器尺寸"));
//                            }
//                            else
//                            {
//                                EditorGUILayout.LabelField("传感器尺寸：属性不存在");
//                            }
//                        }
//                    }
//                    else
//                    {
//                        EditorGUILayout.LabelField("物理相机：属性不存在");
//                    }
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("正交相机：属性不存在");
//                }
//            }
//            else
//            {
//                EditorGUILayout.LabelField("镜头参数：属性不存在");
//            }

//            vcamSO.ApplyModifiedProperties();
//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 4. 过渡参数设置（兼容新旧版本Cinemachine）
//        // ==============================================
//        _折叠组_过渡参数 = EditorGUILayout.Foldout(_折叠组_过渡参数, "【过渡参数设置】", _折叠组标题样式);
//        if (_折叠组_过渡参数 && targetScript.virtualCamera != null)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            SerializedObject vcamSO = new SerializedObject(targetScript.virtualCamera);
//            // 检查过渡属性是否存在
//            if (_prop_vcam_Transitions != null)
//            {
//                // 优先尝试新版 BlendStyle（兼容新版）
//                SerializedProperty blendStyleProp = _prop_vcam_Transitions.FindPropertyRelative("m_BlendStyle");
//                if (blendStyleProp != null)
//                {
//                    // 获取当前的混合样式枚举值
//                    CinemachineBlendDefinition.Style currentStyle =
//                        (CinemachineBlendDefinition.Style)blendStyleProp.enumValueIndex;

//                    // 定义汉化后的选项
//                    string[] styleOptions = {
//                        "立即切换 (Cut)",
//                        "缓入缓出 (EaseInOut)",
//                        "缓入 (EaseIn)",
//                        "缓出 (EaseOut)",
//                        "线性 (Linear)",
//                        "硬缓入 (HardEaseIn)",
//                        "硬缓出 (HardEaseOut)",
//                        "硬缓入缓出 (HardEaseInOut)"
//                    };

//                    // 正确的Popup调用（带布局约束）
//                    GUIContent blendStyleLabel = new GUIContent(
//                        "混合样式",
//                        "相机切换时的过渡动画样式：\n" +
//                        "立即切换 - 无过渡，瞬间切换\n" +
//                        "缓入缓出 - 先慢后快再慢（最常用）\n" +
//                        "缓入 - 开始慢，之后匀速\n" +
//                        "缓出 - 匀速开始，结束慢\n" +
//                        "线性 - 匀速过渡\n" +
//                        "硬缓入/缓出/缓入缓出 - 更明显的缓动效果"
//                    );

//                    int selectedIndex = EditorGUILayout.Popup(
//                        blendStyleLabel,
//                        (int)currentStyle,
//                        styleOptions,
//                        GUILayout.MaxWidth(Screen.width - 40) // 限制宽度
//                    );

//                    blendStyleProp.enumValueIndex = selectedIndex;
//                }
//                // 兼容旧版 BlendHint
//                else
//                {
//                    SerializedProperty blendHintProp = _prop_vcam_Transitions.FindPropertyRelative("m_BlendHint");
//                    if (blendHintProp != null)
//                    {
//                        EditorGUILayout.PropertyField(blendHintProp, new GUIContent(
//                            "混合提示", "相机切换时的位置混合方式：\n" +
//                            "None - 直接混合\n" +
//                            "SphericalPosition - 球形插值（适合3D）\n" +
//                            "CartesianPosition - 笛卡尔插值（适合2D）"));
//                    }
//                    else
//                    {
//                        EditorGUILayout.LabelField("混合样式/提示：属性不存在");
//                    }
//                }

//                // 混合时间
//                SerializedProperty blendTimeProp = _prop_vcam_Transitions.FindPropertyRelative("m_BlendTime");
//                if (blendTimeProp != null)
//                {
//                    EditorGUILayout.PropertyField(blendTimeProp, new GUIContent(
//                        "混合时间 (秒)", "相机切换的平滑过渡时长（建议：0.1-1）"));
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("混合时间 (秒)：属性不存在");
//                }

//                // 继承位置
//                SerializedProperty inheritPosProp = _prop_vcam_Transitions.FindPropertyRelative("m_InheritPosition");
//                if (inheritPosProp != null)
//                {
//                    EditorGUILayout.PropertyField(inheritPosProp, new GUIContent(
//                        "继承位置", "切换时是否继承上一个相机的位置"));
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("继承位置：属性不存在");
//                }
//            }
//            else
//            {
//                EditorGUILayout.LabelField("过渡参数：属性不存在");
//            }

//            vcamSO.ApplyModifiedProperties();
//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 5. Cinemachine组件管道管理（Body/Aim/Noise）
//        // ==============================================
//        _折叠组_组件管道 = EditorGUILayout.Foldout(_折叠组_组件管道, "【组件管道管理】", _折叠组标题样式);
//        if (_折叠组_组件管道 && targetScript.virtualCamera != null)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            EditorGUILayout.LabelField("当前组件管道（按执行顺序）:", EditorStyles.boldLabel);

//            // 获取当前各阶段的组件
//            Dictionary<CinemachineCore.Stage, CinemachineComponentBase> stageComponents = new Dictionary<CinemachineCore.Stage, CinemachineComponentBase>()
//            {
//                { CinemachineCore.Stage.Body, targetScript.virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) },
//                { CinemachineCore.Stage.Aim, targetScript.virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Aim) },
//                { CinemachineCore.Stage.Noise, targetScript.virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise) },
//                { CinemachineCore.Stage.Finalize, targetScript.virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Finalize) }
//            };

//            // 显示各阶段组件
//            foreach (var kvp in stageComponents)
//            {
//                string stageName = GetStageChineseName(kvp.Key);
//                string compName = kvp.Value != null ? kvp.Value.GetType().Name : "未配置";
//                EditorGUILayout.LabelField(stageName + ": " + compName);
//            }

//            EditorGUILayout.Space();
//            EditorGUILayout.LabelField("一键添加常用组件:", EditorStyles.boldLabel);

//            // 按钮横向布局，防止纵向溢出
//            EditorGUILayout.BeginHorizontal();
//            // Body组件（2D常用）
//            if (GUILayout.Button("添加2D跟随组件 (Transposer)", _按钮样式))
//            {
//                targetScript.virtualCamera.AddCinemachineComponent<CinemachineTransposer>();
//                EditorUtility.SetDirty(targetScript.virtualCamera);
//            }

//            // Aim组件（2D常用）
//            if (GUILayout.Button("添加2D朝向组件 (Composer)", _按钮样式))
//            {
//                targetScript.virtualCamera.AddCinemachineComponent<CinemachineComposer>();
//                EditorUtility.SetDirty(targetScript.virtualCamera);
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.BeginHorizontal();
//            // Noise组件（震动核心）
//            if (GUILayout.Button("添加震动组件 (BasicMultiChannelPerlin)", _按钮样式))
//            {
//                targetScript.virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
//                EditorUtility.SetDirty(targetScript.virtualCamera);
//            }

//            // 清空组件
//            if (GUILayout.Button("清空所有组件", _按钮样式))
//            {
//                if (EditorUtility.DisplayDialog("确认", "是否清空所有组件管道？", "确定", "取消"))
//                {
//                    CinemachineComponentBase[] allComps = targetScript.virtualCamera.GetComponentsInChildren<CinemachineComponentBase>();
//                    foreach (var comp in allComps)
//                    {
//                        DestroyImmediate(comp);
//                    }
//                }
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 6. 震动控制（可视化调试）
//        // ==============================================
//        _折叠组_震动控制 = EditorGUILayout.Foldout(_折叠组_震动控制, "【震动控制（调试）】", _折叠组标题样式);
//        if (_折叠组_震动控制)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            // 震动基础配置 - 安全绘制
//            if (_prop_震动衰减速度 != null)
//            {
//                EditorGUILayout.PropertyField(_prop_震动衰减速度, new GUIContent(
//                    "震动衰减速度", "震动强度随时间衰减的速度，数值越大，震动消失越快"));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("震动衰减速度：属性不存在");
//            }

//            if (_prop_距离衰减强度 != null)
//            {
//                EditorGUILayout.PropertyField(_prop_距离衰减强度, new GUIContent(
//                    "距离衰减强度", "每米削减的震动强度百分比（0-1）"));
//            }
//            else
//            {
//                EditorGUILayout.LabelField("距离衰减强度：属性不存在");
//            }

//            EditorGUILayout.Space();

//            // 时间驱动震动
//            EditorGUILayout.LabelField("时间驱动震动（自动停止）", EditorStyles.boldLabel);
//            _临时_震动强度 = EditorGUILayout.FloatField("震动强度 (0-10)", _临时_震动强度);
//            _临时_震动时长 = EditorGUILayout.FloatField("持续时间 (秒)", _临时_震动时长);
//            _临时_启用距离衰减 = EditorGUILayout.Toggle("启用距离衰减", _临时_启用距离衰减);
//            if (_临时_启用距离衰减)
//            {
//                _临时_震动源位置 = (Transform)EditorGUILayout.ObjectField("震动源位置", _临时_震动源位置, typeof(Transform), true);
//                _临时_目标位置 = (Transform)EditorGUILayout.ObjectField("目标位置", _临时_目标位置, typeof(Transform), true);
//            }

//            if (GUILayout.Button("触发时间震动", _按钮样式))
//            {
//                if (_临时_启用距离衰减 && _临时_震动源位置 != null && _临时_目标位置 != null)
//                {
//                    targetScript.AddTimeBasedShake(_临时_震动强度, _临时_震动时长, _临时_目标位置, _临时_震动源位置);
//                }
//                else
//                {
//                    targetScript.AddTimeBasedShake(_临时_震动强度, _临时_震动时长);
//                }
//            }

//            EditorGUILayout.Space();

//            // 手动震动
//            EditorGUILayout.LabelField("手动震动（需手动停止）", EditorStyles.boldLabel);
//            float 手动震动强度 = EditorGUILayout.FloatField("震动强度 (0-10)", _临时_震动强度);
//            bool 手动衰减 = EditorGUILayout.Toggle("启用距离衰减", _临时_启用距离衰减);
//            if (手动衰减)
//            {
//                _临时_震动源位置 = (Transform)EditorGUILayout.ObjectField("震动源位置", _临时_震动源位置, typeof(Transform), true);
//                _临时_目标位置 = (Transform)EditorGUILayout.ObjectField("目标位置", _临时_目标位置, typeof(Transform), true);
//            }

//            if (GUILayout.Button("触发手动震动", _按钮样式))
//            {
//                if (手动衰减 && _临时_震动源位置 != null && _临时_目标位置 != null)
//                {
//                    targetScript.AddManualShake(手动震动强度, _临时_目标位置, _临时_震动源位置);
//                }
//                else
//                {
//                    targetScript.AddManualShake(手动震动强度);
//                }
//            }

//            EditorGUILayout.Space();

//            // 停止震动按钮
//            if (GUILayout.Button("停止所有震动", _按钮样式))
//            {
//                targetScript.ResetAllShake();
//            }

//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 7. 缩放控制（可视化调试）
//        // ==============================================
//        _折叠组_缩放控制 = EditorGUILayout.Foldout(_折叠组_缩放控制, "【缩放控制（调试）】", _折叠组标题样式);
//        if (_折叠组_缩放控制)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            _临时_缩放类型 = (ZoomTaskType)EditorGUILayout.EnumPopup("缩放类型", _临时_缩放类型);
//            _临时_缩放目标尺寸 = EditorGUILayout.FloatField("目标尺寸", _临时_缩放目标尺寸);
//            _临时_缩放速度 = EditorGUILayout.FloatField("缩放速度", _临时_缩放速度);

//            if (_临时_缩放类型 == ZoomTaskType.TemporaryWithTime)
//            {
//                _临时_缩放停留时间 = EditorGUILayout.FloatField("停留时间 (秒)", _临时_缩放停留时间);
//            }

//            if (GUILayout.Button("触发缩放", _按钮样式))
//            {
//                if (_临时_缩放类型 == ZoomTaskType.TemporaryWithTime)
//                {
//                    targetScript.AddZoomTask_TemporaryWithTime(_临时_缩放目标尺寸, _临时_缩放停留时间, _临时_缩放速度);
//                }
//                else
//                {
//                    targetScript.AddZoomTask_TemporaryManual(_临时_缩放目标尺寸, _临时_缩放速度);
//                }
//            }

//            EditorGUILayout.Space();

//            EditorGUILayout.BeginHorizontal();
//            if (GUILayout.Button("重置所有手动缩放", _按钮样式))
//            {
//                targetScript.ResetAllManualZoomTasks();
//            }

//            if (GUILayout.Button("清空所有缩放任务", _按钮样式))
//            {
//                targetScript.ClearAllZoomTasks();
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        // 8. 相机模式切换（可视化配置）
//        // ==============================================
//        _折叠组_相机模式 = EditorGUILayout.Foldout(_折叠组_相机模式, "【相机模式切换】", _折叠组标题样式);
//        if (_折叠组_相机模式)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            // 当前模式选择 - 安全绘制
//            if (_prop_当前相机模式 != null)
//            {
//                EditorGUILayout.PropertyField(_prop_当前相机模式, new GUIContent("当前相机模式"));
//                CameraMode 新模式 = (CameraMode)_prop_当前相机模式.enumValueIndex;

//                // 模式参数配置
//                switch (新模式)
//                {
//                    case CameraMode.FollowPlayerMode:
//                        GameObject 跟随目标 = (GameObject)EditorGUILayout.ObjectField("跟随目标", null, typeof(GameObject), true);
//                        if (GUILayout.Button("绑定跟随目标", _按钮样式))
//                        {
//                            if (跟随目标 != null)
//                            {
//                                targetScript.SetCameraMode_FollowPlayerMode(跟随目标);
//                            }
//                        }
//                        break;

//                    case CameraMode.XFollowTargetMode:
//                    case CameraMode.YFollowTargetMode:
//                        GameObject 轴跟随目标 = (GameObject)EditorGUILayout.ObjectField("跟随目标", null, typeof(GameObject), true);
//                        // 安全绘制偏移量
//                        if (_prop_偏移位置 != null)
//                        {
//                            EditorGUILayout.PropertyField(_prop_偏移位置, new GUIContent("偏移量"));
//                            if (GUILayout.Button("绑定跟随目标", _按钮样式))
//                            {
//                                if (轴跟随目标 != null)
//                                {
//                                    if (新模式 == CameraMode.XFollowTargetMode)
//                                    {
//                                        targetScript.SetCameraMode_XFollowTargetMode(轴跟随目标, _prop_偏移位置.vector2Value);
//                                    }
//                                    else
//                                    {
//                                        targetScript.SetCameraMode_YFollowTargetMode(轴跟随目标, _prop_偏移位置.vector2Value);
//                                    }
//                                }
//                            }
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("偏移量：属性不存在");
//                        }
//                        break;

//                    case CameraMode.AreaLockingMode:
//                        GameObject 区域目标 = (GameObject)EditorGUILayout.ObjectField("参考目标", null, typeof(GameObject), true);
//                        // 安全绘制偏移量
//                        if (_prop_偏移位置 != null)
//                        {
//                            EditorGUILayout.PropertyField(_prop_偏移位置, new GUIContent("偏移量"));
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("偏移量：属性不存在");
//                        }

//                        // 区域锁定位置配置
//                        if (targetScript.areaLockingPack == null)
//                        {
//                            targetScript.areaLockingPack = new AreaLockingPack();
//                        }
//                        // 安全绘制区域锁定属性
//                        if (_prop_区域锁定包 != null)
//                        {
//                            SerializedProperty leftUpPos = _prop_区域锁定包.FindPropertyRelative("LeftUpPos");
//                            if (leftUpPos != null)
//                            {
//                                EditorGUILayout.PropertyField(leftUpPos, new GUIContent("左上位置"));
//                            }
//                            else
//                            {
//                                EditorGUILayout.LabelField("左上位置：属性不存在");
//                            }

//                            SerializedProperty rightDownPos = _prop_区域锁定包.FindPropertyRelative("RightDownPos");
//                            if (rightDownPos != null)
//                            {
//                                EditorGUILayout.PropertyField(rightDownPos, new GUIContent("右下位置"));
//                            }
//                            else
//                            {
//                                EditorGUILayout.LabelField("右下位置：属性不存在");
//                            }

//                            SerializedProperty topUpPos = _prop_区域锁定包.FindPropertyRelative("TopUpPos");
//                            if (topUpPos != null)
//                            {
//                                EditorGUILayout.PropertyField(topUpPos, new GUIContent("上方位置"));
//                            }
//                            else
//                            {
//                                EditorGUILayout.LabelField("上方位置：属性不存在");
//                            }

//                            SerializedProperty bottomDownPos = _prop_区域锁定包.FindPropertyRelative("BottomDownPos");
//                            if (bottomDownPos != null)
//                            {
//                                EditorGUILayout.PropertyField(bottomDownPos, new GUIContent("下方位置"));
//                            }
//                            else
//                            {
//                                EditorGUILayout.LabelField("下方位置：属性不存在");
//                            }

//                            if (GUILayout.Button("绑定区域锁定", _按钮样式))
//                            {
//                                if (区域目标 != null && targetScript.areaLockingPack != null && _prop_偏移位置 != null)
//                                {
//                                    targetScript.SetCameraMode_AreaLockingMode(区域目标, targetScript.areaLockingPack, _prop_偏移位置.vector2Value);
//                                }
//                            }
//                        }
//                        else
//                        {
//                            EditorGUILayout.LabelField("区域锁定包：属性不存在");
//                        }
//                        break;
//                }
//            }
//            else
//            {
//                EditorGUILayout.LabelField("当前相机模式：属性不存在");
//            }

//            EditorGUILayout.EndVertical();
//        }

//        EditorGUILayout.Space();

//        // ==============================================
//        //  调试工具（快捷操作）- 兼容所有Cinemachine版本
//        // ==============================================
//        _折叠组_调试工具 = EditorGUILayout.Foldout(_折叠组_调试工具, "【调试工具】", _折叠组标题样式);
//        if (_折叠组_调试工具 && targetScript.virtualCamera != null)
//        {
//            EditorGUILayout.BeginVertical(_盒子样式, GUILayout.MaxWidth(Screen.width - 20));

//            // 强制设置相机位置/旋转
//            EditorGUILayout.LabelField("强制设置相机位置/旋转", EditorStyles.boldLabel);
//            _临时_强制位置 = EditorGUILayout.Vector3Field("目标位置", _临时_强制位置);
//            _临时_强制旋转 = Quaternion.Euler(EditorGUILayout.Vector3Field("目标旋转（欧拉角）", _临时_强制旋转.eulerAngles));
//            if (GUILayout.Button("强制设置相机位置", _按钮样式))
//            {
//                targetScript.virtualCamera.ForceCameraPosition(_临时_强制位置, _临时_强制旋转);
//            }

//            EditorGUILayout.Space();

//            EditorGUILayout.BeginHorizontal();
//            // 重置相机状态
//            if (GUILayout.Button("重置相机状态", _按钮样式))
//            {
//                targetScript.virtualCamera.ForceCameraPosition(
//                    targetScript.virtualCamera.transform.position,
//                    targetScript.virtualCamera.transform.rotation);
//                targetScript.ResetAllShake();
//                targetScript.ClearAllZoomTasks();
//            }

//            // 设为活跃相机
//            if (GUILayout.Button("设为活跃相机", _按钮样式))
//            {
//                CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
//                if (brain != null)
//                {
//                    brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);

//                    SerializedObject vcamSO = new SerializedObject(targetScript.virtualCamera);
//                    SerializedProperty priorityProp = vcamSO.FindProperty("m_Priority");
//                    if (priorityProp != null)
//                    {
//                        priorityProp.intValue = 100;
//                        vcamSO.ApplyModifiedProperties();
//                    }
//                    else
//                    {
//                        Debug.LogWarning("无法设置相机优先级：属性不存在");
//                    }

//                    targetScript.virtualCamera.gameObject.SetActive(true);
//                    targetScript.virtualCamera.enabled = true;

//                    CinemachineVirtualCamera[] allVcams = FindObjectsOfType<CinemachineVirtualCamera>();
//                    foreach (var vcam in allVcams)
//                    {
//                        if (vcam != targetScript.virtualCamera)
//                        {
//                            SerializedObject otherVcamSO = new SerializedObject(vcam);
//                            SerializedProperty otherPriority = otherVcamSO.FindProperty("m_Priority");
//                            if (otherPriority != null)
//                            {
//                                otherPriority.intValue = 0;
//                                otherVcamSO.ApplyModifiedProperties();
//                            }
//                        }
//                    }

//                    EditorUtility.DisplayDialog("提示", "已将目标相机设为最高优先级并激活！\n其他相机优先级已设为最低", "确定");
//                }
//                else
//                {
//                    EditorUtility.DisplayDialog("错误", "未找到CinemachineBrain组件！", "确定");
//                }
//            }
//            EditorGUILayout.EndHorizontal();

//            EditorGUILayout.EndVertical();
//        }

//        // 结束属性修改检查，仅在有修改时应用（减少布局刷新）
//        if (EditorGUI.EndChangeCheck())
//        {
//            serializedObject.ApplyModifiedProperties();
//        }
//    }

//    #region 辅助方法（阶段名称中文转换）
//    /// <summary>
//    /// 将Cinemachine阶段转换为中文名称
//    /// </summary>
//    private string GetStageChineseName(CinemachineCore.Stage stage)
//    {
//        switch (stage)
//        {
//            case CinemachineCore.Stage.Body: return "身体阶段 (Body)";
//            case CinemachineCore.Stage.Aim: return "瞄准阶段 (Aim)";
//            case CinemachineCore.Stage.Noise: return "噪声阶段 (Noise)";
//            case CinemachineCore.Stage.Finalize: return "最终调整 (Finalize)";
//            default: return stage.ToString();
//        }
//    }
//    #endregion
//}