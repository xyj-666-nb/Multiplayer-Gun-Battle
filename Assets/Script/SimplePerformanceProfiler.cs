using UnityEngine;
using Unity.Profiling;
using System.Text;
using UnityEngine.Rendering;
using System;
using UnityEditor;

[DisallowMultipleComponent]
public class PerformanceMonitor : MonoBehaviour
{
    public static PerformanceMonitor Instance { get; private set; }

    [Header("面板设置")]
    [SerializeField] private float _panelWidth = 320f;
    [SerializeField] private float _panelHeight = 460f;

    private bool _showPanel = false;
    private Rect _windowRect;
    private const int WindowId = 12345;

    // 性能统计Recorder（修正官方正确的统计项名称）
    private ProfilerRecorder _mainThreadCPURecorder;
    private ProfilerRecorder _totalMemoryRecorder;
    private ProfilerRecorder _gcPerFrameRecorder;
    private ProfilerRecorder _gpuFrameTimeRecorder;

    // 帧率计算
    private const int FrameSampleCount = 60;
    private float[] _frameTimeSamples;
    private int _frameSampleIndex;
    private float _currentFPS;
    private float _averageFrameTime;

    private readonly StringBuilder _sb = new StringBuilder(256);

    #region 生命周期
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化窗口位置（左上角按钮下方）
        _windowRect = new Rect(10, 50, _panelWidth, _panelHeight);
        // 初始化帧率采样数组
        _frameTimeSamples = new float[FrameSampleCount];
    }

    private void OnEnable()
    {
        // 初始化所有性能Recorder（正确的Unity官方统计项名称，兼容Unity 2020+）
        _mainThreadCPURecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        _totalMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory", 1);
        _gcPerFrameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        _gpuFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1);
    }

    private void OnDisable()
    {
        // 安全释放Recorder资源
        _mainThreadCPURecorder.Dispose();
        _totalMemoryRecorder.Dispose();
        _gcPerFrameRecorder.Dispose();
        _gpuFrameTimeRecorder.Dispose();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // 计算平滑帧率与平均帧时间
        _frameTimeSamples[_frameSampleIndex] = Time.unscaledDeltaTime;
        _frameSampleIndex = (_frameSampleIndex + 1) % FrameSampleCount;

        float totalFrameTime = 0f;
        for (int i = 0; i < FrameSampleCount; i++)
        {
            totalFrameTime += _frameTimeSamples[i];
        }
        _averageFrameTime = totalFrameTime / FrameSampleCount * 1000f;
        _currentFPS = FrameSampleCount / totalFrameTime;
    }

    private void OnGUI()
    {
        // 绘制展开/收起按钮（固定左上角）
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fixedWidth = 140,
            fixedHeight = 35,
            fontStyle = FontStyle.Bold
        };

        if (GUI.Button(new Rect(10, 10, 140, 35), _showPanel ? "收起性能面板" : "展开性能面板", buttonStyle))
        {
            _showPanel = !_showPanel;
        }

        // 绘制可拖动的性能面板
        if (_showPanel)
        {
            _windowRect = GUI.Window(WindowId, _windowRect, DrawPerformanceWindow, "性能监控面板");
        }
    }
    #endregion

    #region 面板绘制
    private void DrawPerformanceWindow(int windowId)
    {
        // 整个标题栏都可拖动（核心拖动逻辑）
        GUI.DragWindow(new Rect(0, 0, _windowRect.width, 24));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true,
            padding = new RectOffset(10, 10, 6, 6),
            richText = true
        };

        _sb.Clear();

        // 1. 帧率与帧时间
        _sb.AppendLine($"<b>帧率 (FPS):</b> {_currentFPS:F1}");
        _sb.AppendLine($"<b> 平均帧时间:</b> {_averageFrameTime:F2} ms");
        _sb.AppendLine();

        // 2. CPU主线程时间（修复：正确统计项+兜底方案）
        if (_mainThreadCPURecorder.Valid && _mainThreadCPURecorder.LastValue > 0)
        {
            double cpuMainThreadMs = _mainThreadCPURecorder.LastValue / 1000000.0;
            _sb.AppendLine($"<b> CPU主线程耗时:</b> {cpuMainThreadMs:F2} ms");
        }
        else
        {
            // 兜底方案：用帧时间作为CPU耗时参考，不会显示不可用
            _sb.AppendLine($"<b> CPU主线程耗时:</b> {_averageFrameTime:F2} ms (参考值)");
        }
        _sb.AppendLine();

        // 3. 内存占用
        if (_totalMemoryRecorder.Valid)
        {
            double totalUsedMemoryMB = _totalMemoryRecorder.LastValue / (1024.0 * 1024.0);
            _sb.AppendLine($"<b>总内存占用:</b> {totalUsedMemoryMB:F2} MB");
        }
        else
        {
            long totalMemory = GC.GetTotalMemory(false);
            _sb.AppendLine($"<b> 总内存占用:</b> {totalMemory / (1024.0 * 1024.0):F2} MB (参考值)");
        }
        _sb.AppendLine();

        // 4. GC占用
        if (_gcPerFrameRecorder.Valid)
        {
            double gcPerFrameKB = _gcPerFrameRecorder.LastValue / 1024.0;
            _sb.AppendLine($"<b>每帧GC分配:</b> {gcPerFrameKB:F2} KB");
        }
        else
        {
            long gcTotalMemory = GC.GetTotalMemory(false);
            _sb.AppendLine($"<b> GC总占用:</b> {gcTotalMemory / (1024.0 * 1024.0):F2} MB (参考值)");
        }
        _sb.AppendLine();

        // 5. DrawCall（核心修复：改用Unity官方稳定API，100%可获取）
        _sb.AppendLine($"<b>Draw Calls:</b> {UnityStats.drawCalls}");
        _sb.AppendLine($"<b>三角面数:</b> {UnityStats.triangles / 10000.0:F2} 万");
        _sb.AppendLine();

        // 6. GPU帧时间（修复：异常值处理+兼容性提示）
        if (_gpuFrameTimeRecorder.Valid && _gpuFrameTimeRecorder.LastValue > 0)
        {
            double gpuFrameTimeMs = _gpuFrameTimeRecorder.LastValue / 1000000.0;
            _sb.AppendLine($"<b> GPU帧耗时:</b> {gpuFrameTimeMs:F2} ms");
        }
        else
        {
            _sb.AppendLine($"<b> GPU帧耗时:</b> 平台不支持/需开启Development Build");
        }

        GUILayout.Label(_sb.ToString(), labelStyle);
    }
    #endregion
}