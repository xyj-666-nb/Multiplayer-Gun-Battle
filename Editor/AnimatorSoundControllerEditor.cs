using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(AnimatorSoundController))]
[CanEditMultipleObjects]
public class AnimatorSoundControllerEditor : Editor
{
    // 序列化属性缓存
    private SerializedProperty _prop_DefaultIsLoop;
    private SerializedProperty _prop_DefaultVolumeScale;
    private SerializedProperty _prop_Default3dMaxDistance;
    private SerializedProperty _prop_Default3dMinDistance;
    private SerializedProperty _prop_Is3dSoundFollowOwner;
    private SerializedProperty[] _prop_SoundClips = new SerializedProperty[10];

    // 折叠组状态（仅保留全局和轨道的折叠）
    private bool _foldout_GlobalConfig = true;
    private bool _foldout_SoundTracks = true;

    // 样式
    private GUIStyle _foldoutStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _buttonStyle; // 轨道小按钮
    private GUIStyle _batchButtonStyle; // 批量按钮（适度放大）

    private void OnEnable()
    {
        // 缓存属性
        _prop_DefaultIsLoop = serializedObject.FindProperty("DefaultIsLoop");
        _prop_DefaultVolumeScale = serializedObject.FindProperty("DefaultVolumeScale");
        _prop_Default3dMaxDistance = serializedObject.FindProperty("Default3dMaxDistance");
        _prop_Default3dMinDistance = serializedObject.FindProperty("Default3dMinDistance");
        _prop_Is3dSoundFollowOwner = serializedObject.FindProperty("Is3dSoundFollowOwner");

        for (int i = 0; i < 10; i++)
        {
            _prop_SoundClips[i] = serializedObject.FindProperty($"SoundClip{i + 1}");
        }
    }

    public override void OnInspectorGUI()
    {
        InitStyles();
        AnimatorSoundController targetScript = target as AnimatorSoundController;
        if (targetScript == null)
        {
            EditorGUILayout.LabelField(" 目标脚本为空", EditorStyles.boldLabel);
            return;
        }

        serializedObject.Update();

        DrawGlobalConfig();
        DrawSoundTracks(targetScript);
        DrawBatchControl(targetScript); // 仅显示按钮，无框

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 初始化样式（批量按钮适度放大）
    /// </summary>
    private void InitStyles()
    {
        // 折叠组样式
        if (_foldoutStyle == null)
        {
            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = new Color(0.15f, 0.45f, 0.8f) }
            };
        }

        // 容器样式（仅轨道/全局使用）
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }

        // 轨道小按钮样式
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                padding = new RectOffset(4, 4, 2, 2),
                fixedWidth = 80f
            };
        }

        // 批量按钮样式（适度放大：宽120→高25，易点击且不突兀）
        if (_batchButtonStyle == null)
        {
            _batchButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,                  // 字体保持不变，避免拥挤
                padding = new RectOffset(8, 8, 5, 5), // 内边距适配新尺寸
                fixedWidth = 120f,              // 从110→120（加宽10px）
                fixedHeight = 25f,              // 从22→25（加高3px）
                fontStyle = FontStyle.Normal
            };
        }
    }

    /// <summary>
    /// 绘制全局配置区域
    /// </summary>
    private void DrawGlobalConfig()
    {
        _foldout_GlobalConfig = EditorGUILayout.Foldout(_foldout_GlobalConfig, "全局播放配置", _foldoutStyle);
        if (_foldout_GlobalConfig)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            DrawProperty(_prop_DefaultIsLoop, "默认循环播放", "所有音效的默认循环状态");
            DrawProperty(_prop_DefaultVolumeScale, "默认音量缩放", "0-1，叠加MusicManager的全局音效音量");
            DrawProperty(_prop_Default3dMaxDistance, "3D音效最大衰减距离", "超过此距离听不到音效");
            DrawProperty(_prop_Default3dMinDistance, "3D音效最小无衰减距离", "此距离内音效音量无衰减");
            DrawProperty(_prop_Is3dSoundFollowOwner, "3D音效跟随物体", "3D音效的AudioSource是否跟随当前物体移动");
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(8);
    }

    /// <summary>
    /// 绘制10组音效轨道
    /// </summary>
    private void DrawSoundTracks(AnimatorSoundController targetScript)
    {
        _foldout_SoundTracks = EditorGUILayout.Foldout(_foldout_SoundTracks, " 音效轨道配置（10组）", _foldoutStyle);
        if (_foldout_SoundTracks)
        {
            EditorGUILayout.HelpBox("点击下方按钮可直接测试音效（需在PlayMode下生效）", MessageType.Info);

            for (int i = 0; i < 10; i++)
            {
                int trackNum = i + 1;
                EditorGUILayout.BeginVertical(_boxStyle);

                // 轨道标题
                EditorGUILayout.LabelField($"轨道 {trackNum}", EditorStyles.boldLabel);

                // 音效Clip选择框
                if (_prop_SoundClips[i] != null)
                {
                    EditorGUILayout.PropertyField(_prop_SoundClips[i], new GUIContent($"音效文件 (轨道{trackNum})", "2D/3D音效共用此文件"));
                }
                else
                {
                    EditorGUILayout.LabelField($"音效文件 (轨道{trackNum})：属性不存在", EditorStyles.miniLabel);
                }

                // 空值提示
                bool isClipNull = _prop_SoundClips[i] != null && _prop_SoundClips[i].objectReferenceValue == null;
                EditorGUILayout.LabelField(isClipNull ? " 未赋值音效文件" : "", EditorStyles.miniLabel);

                EditorGUILayout.Space(4);

                // 测试按钮组
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("播放2D", _buttonStyle)) TestPlaySound(targetScript, trackNum, false);
                if (GUILayout.Button("播放3D", _buttonStyle)) TestPlaySound(targetScript, trackNum, true);
                if (GUILayout.Button("暂停", _buttonStyle)) TestPauseSound(targetScript, trackNum);
                if (GUILayout.Button("停止", _buttonStyle)) TestStopSound(targetScript, trackNum);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.Space(8);
    }

    /// <summary>
    /// 绘制批量控制按钮（仅显示按钮，无任何容器框）
    /// </summary>
    private void DrawBatchControl(AnimatorSoundController targetScript)
    {
        // 仅水平布局显示3个按钮，无任何容器框、无标题
        EditorGUILayout.BeginHorizontal();

        // 按钮之间增加少量间距（通过空布局）
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("批量暂停所有音效", _batchButtonStyle))
        {
            targetScript.PauseAllTrackSounds();
            Debug.Log(" 已暂停所有音效轨道");
        }
        GUILayout.Space(10); // 按钮之间的间距
        if (GUILayout.Button("批量停止所有音效", _batchButtonStyle))
        {
            targetScript.StopAllTrackSounds();
            Debug.Log(" 已停止所有音效轨道");
        }
        GUILayout.Space(10); // 按钮之间的间距
        if (GUILayout.Button("恢复所有暂停音效", _batchButtonStyle))
        {
            targetScript.ResumeAllTrackSounds();
            Debug.Log(" 已恢复所有暂停的音效");
        }
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8); // 按钮下方留少量间距
    }

    /// <summary>
    /// 绘制单个属性
    /// </summary>
    private void DrawProperty(SerializedProperty prop, string label, string tooltip = "")
    {
        if (prop == null)
        {
            EditorGUILayout.LabelField($"{label}：属性不存在", EditorStyles.miniLabel);
            return;
        }
        EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
    }

    #region 测试方法
    private void TestPlaySound(AnimatorSoundController target, int trackNum, bool is3D)
    {
        if (targets.Length > 1)
        {
            EditorUtility.DisplayDialog("提示", "测试功能仅支持单对象编辑！", "确定");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("提示", "请先进入PlayMode再测试音效播放！", "确定");
            return;
        }

        AudioClip clip = GetTrackClip(target, trackNum);
        if (clip == null)
        {
            EditorUtility.DisplayDialog("警告", $"轨道{trackNum}未配置音效文件！", "确定");
            return;
        }

        try
        {
            if (is3D)
            {
                target.GetType().GetMethod($"PlaySound{trackNum}_3D", new Type[] { typeof(bool) })
                    ?.Invoke(target, new object[] { target.DefaultIsLoop });
            }
            else
            {
                target.GetType().GetMethod($"PlaySound{trackNum}", new Type[] { typeof(bool) })
                    ?.Invoke(target, new object[] { target.DefaultIsLoop });
            }
            Debug.Log($" 已播放 轨道{trackNum} {(is3D ? "3D" : "2D")} 音效：{clip.name}");
        }
        catch (Exception e)
        {
            Debug.LogError($" 播放轨道{trackNum}音效失败：{e.Message}", target);
        }
    }

    private void TestPauseSound(AnimatorSoundController target, int trackNum)
    {
        if (targets.Length > 1)
        {
            EditorUtility.DisplayDialog("提示", "测试功能仅支持单对象编辑！", "确定");
            return;
        }

        try
        {
            target.GetType().GetMethod($"PauseSound{trackNum}")?.Invoke(target, null);
            Debug.Log($" 已暂停 轨道{trackNum} 音效");
        }
        catch (Exception e)
        {
            Debug.LogError($" 暂停轨道{trackNum}音效失败：{e.Message}", target);
        }
    }

    private void TestStopSound(AnimatorSoundController target, int trackNum)
    {
        if (targets.Length > 1)
        {
            EditorUtility.DisplayDialog("提示", "测试功能仅支持单对象编辑！", "确定");
            return;
        }

        try
        {
            target.GetType().GetMethod($"StopSound{trackNum}")?.Invoke(target, null);
            Debug.Log($"已停止 轨道{trackNum} 音效");
        }
        catch (Exception e)
        {
            Debug.LogError($" 停止轨道{trackNum}音效失败：{e.Message}", target);
        }
    }

    private AudioClip GetTrackClip(AnimatorSoundController target, int trackNum)
    {
        if (trackNum < 1 || trackNum > 10 || target == null) return null;
        return (AudioClip)target.GetType().GetField($"SoundClip{trackNum}")?.GetValue(target);
    }
    #endregion
}