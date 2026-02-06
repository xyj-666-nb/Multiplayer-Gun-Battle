//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UI;

//[CustomEditor(typeof(BasePanel), true)] // 第二个参数isFallback=true：让子类也使用此编辑器
//public class BasePanelEditor : Editor
//{
//    // 序列化属性（对应BasePanel中的字段）
//    private SerializedProperty _isUseSpecialCanvasProp;
//    private SerializedProperty _canvasRenderModeProp;
//    private SerializedProperty _canvasPixelPerfectProp;
//    private SerializedProperty _canvasSortingOrderProp;
//    private SerializedProperty _canvasSortingLayerProp;
//    private SerializedProperty _canvasReferenceResolutionProp;

//    // 其他原有属性（可选，如需控制其他字段显示）
//    private SerializedProperty _isCanDestroyProp;
//    private SerializedProperty _isUseRealTimeProp;
//    private SerializedProperty _alphaSpeedProp;


//    private void OnEnable()
//    {
//        // 初始化所有序列化属性（字段名要和BasePanel中完全一致）
//        _isUseSpecialCanvasProp = serializedObject.FindProperty("IsUseSpecialCanvas");
//        _canvasRenderModeProp = serializedObject.FindProperty("_canvasRenderMode");
//        _canvasPixelPerfectProp = serializedObject.FindProperty("_canvasPixelPerfect");
//        _canvasSortingOrderProp = serializedObject.FindProperty("_canvasSortingOrder");
//        _canvasSortingLayerProp = serializedObject.FindProperty("_canvasSortingLayer");
//        _canvasReferenceResolutionProp = serializedObject.FindProperty("_canvasReferenceResolution");

//        // 初始化其他属性（保持原有Inspector显示）
//        _isCanDestroyProp = serializedObject.FindProperty("IsCanDestroy");
//        _isUseRealTimeProp = serializedObject.FindProperty("IsUseRealTime");
//        _alphaSpeedProp = serializedObject.FindProperty("alhpaSpeed"); // 注意原字段拼写是alhpa（非alpha）
//    }


//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update(); // 更新序列化数据，同步面板和脚本字段

//        // 绘制基础设置
//        DrawBasicSettings();

//        // 绘制Canvas开关 + 动态显示Canvas设定
//        DrawCanvasSettings();

//        //绘制其他原有字段
//        DrawDefaultInspectorExceptHidden();

//        serializedObject.ApplyModifiedProperties(); // 应用修改，保存到脚本字段
//    }


//    /// <summary>
//    /// 绘制基础设置
//    /// </summary>
//    private void DrawBasicSettings()
//    {
//        EditorGUILayout.Space(10);
//        EditorGUILayout.LabelField("基础面板设置", EditorStyles.boldLabel);

//        EditorGUILayout.PropertyField(_isCanDestroyProp, new GUIContent("是否可销毁"));
//        EditorGUILayout.PropertyField(_isUseRealTimeProp, new GUIContent("是否用真实时间（不受暂停影响）"));
//        EditorGUILayout.PropertyField(_alphaSpeedProp, new GUIContent("淡入淡出速度（建议改名为alphaSpeed）"));

//        EditorGUILayout.Space(5);
//    }


//    /// <summary>
//    /// 绘制Canvas开关 + 动态显示Canvas设定
//    /// </summary>
//    private void DrawCanvasSettings()
//    {
//        // 绘制Canvas开关（IsUseSpecialCanvas）
//        EditorGUILayout.PropertyField(_isUseSpecialCanvasProp, new GUIContent("启用特殊Canvas"));

//        //如果开关为true，展开显示Canvas配置
//        if (_isUseSpecialCanvasProp.boolValue)
//        {
//            // 缩进 + 边框分组，让UI更整洁
//            EditorGUI.indentLevel++;
//            using (new EditorGUILayout.VerticalScope("box"))
//            {
//                EditorGUILayout.LabelField("Canvas配置", EditorStyles.boldLabel);

//                // 绘制Canvas核心设置
//                EditorGUILayout.PropertyField(_canvasRenderModeProp, new GUIContent("渲染模式"));
//                EditorGUILayout.PropertyField(_canvasPixelPerfectProp, new GUIContent("像素完美"));
//                EditorGUILayout.PropertyField(_canvasSortingOrderProp, new GUIContent("排序层级（值越大越靠上）"));

//                // 绘制排序层
//                DrawSortingLayerSelection();

//                EditorGUILayout.PropertyField(_canvasReferenceResolutionProp, new GUIContent("参考分辨率（适配用）"));

//                // 额外提示：检查是否已有Canvas组件
//                BasePanel targetPanel = (BasePanel)target;
//                if (targetPanel.GetComponent<Canvas>() == null)
//                {
//                    EditorGUILayout.HelpBox("当前面板没有Canvas组件，显示时会自动添加！", MessageType.Info);
//                }
//            }
//            EditorGUI.indentLevel--;
//        }
//    }


//    /// <summary>
//    /// 绘制排序层下拉选择
//    /// </summary>
//    private void DrawSortingLayerSelection()
//    {
//        // 获取项目中所有排序层名称
//        string[] sortingLayers = GetAllSortingLayerNames();
//        // 找到当前排序层在数组中的索引
//        int currentIndex = Mathf.Max(0, System.Array.IndexOf(sortingLayers, _canvasSortingLayerProp.stringValue));

//        // 绘制下拉菜单
//        int newIndex = EditorGUILayout.Popup("排序层", currentIndex, sortingLayers);
//        // 如果选择变化，更新字段值
//        if (newIndex != currentIndex)
//        {
//            _canvasSortingLayerProp.stringValue = sortingLayers[newIndex];
//        }
//    }


//    /// <summary>
//    /// 获取项目中所有排序层名称（工具方法）
//    /// </summary>
//    private string[] GetAllSortingLayerNames()
//    {
//        System.Reflection.FieldInfo field = typeof(SortingLayer).GetField("s_SortingLayers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
//        if (field == null) return new[] { "Default" };

//        SortingLayer[] layers = (SortingLayer[])field.GetValue(null);
//        string[] layerNames = new string[layers.Length];
//        for (int i = 0; i < layers.Length; i++)
//        {
//            layerNames[i] = layers[i].name;
//        }
//        return layerNames;
//    }


//    /// <summary>
//    /// 绘制默认Inspector（排除已手动绘制的字段，避免重复）
//    /// </summary>
//    private void DrawDefaultInspectorExceptHidden()
//    {
//        EditorGUILayout.Space(10);
//        EditorGUILayout.LabelField("其他系统字段", EditorStyles.boldLabel);

//        // 遍历所有序列化属性，绘制未手动处理的字段
//        SerializedProperty prop = serializedObject.GetIterator();
//        // 跳过第一个属性（脚本引用）
//        if (prop.NextVisible(true))
//        {
//            do
//            {
//                // 排除已手动绘制的字段，避免重复
//                if (prop.name == "IsUseSpecialCanvas" ||
//                    prop.name == "IsCanDestroy" ||
//                    prop.name == "IsUseRealTime" ||
//                    prop.name == "alhpaSpeed" ||
//                    prop.name == "_canvasRenderMode" ||
//                    prop.name == "_canvasPixelPerfect" ||
//                    prop.name == "_canvasSortingOrder" ||
//                    prop.name == "_canvasSortingLayer" ||
//                    prop.name == "_canvasReferenceResolution")
//                {
//                    continue;
//                }
//                // 绘制其他字段
//                EditorGUILayout.PropertyField(prop);
//            } while (prop.NextVisible(false));
//        }
//    }
//}