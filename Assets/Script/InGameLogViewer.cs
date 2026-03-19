using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class InGameLogViewer : MonoBehaviour
{
    // 单例
    public static InGameLogViewer Instance { get; private set; }

    [Header("悬浮窗设置")]
    [Tooltip("悬浮窗宽度占屏幕宽度的比例（已调大）")]
    public float WindowWidthRatio = 0.95f; // 【修改】默认宽度95%
    [Tooltip("悬浮窗高度占屏幕高度的比例（已调大）")]
    public float WindowHeightRatio = 0.6f; // 【修改】默认高度60%
    [Tooltip("日志字体大小")]
    public int LogFontSize = 32;
    [Tooltip("最多显示多少条日志")]
    public int MaxLogCount = 50; // 【修改】默认最多50条

    // 内部变量
    private string _logText = "";
    private Queue<string> _logQueue = new Queue<string>();
    private Vector2 _scrollPosition;
    private Rect _windowRect;
    private bool _isWindowVisible = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化悬浮窗位置和大小（更大）
        float windowWidth = Screen.width * WindowWidthRatio;
        float windowHeight = Screen.height * WindowHeightRatio;
        _windowRect = new Rect(
            (Screen.width - windowWidth) / 2, // 居中
            100, // 距离顶部100像素（避开顶部控制按钮）
            windowWidth,
            windowHeight
        );
    }

    private void OnEnable()
    {
        // 监听Unity所有日志输出
        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    // 收到Unity日志时触发
    private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
    {
        // 给不同类型的日志标不同颜色
        string logColor = type switch
        {
            LogType.Error => "#FF4444", // 红色报错
            LogType.Warning => "#FFFF00", // 黄色警告
            LogType.Exception => "#FF0000", // 深红色异常
            _ => "#FFFFFF" // 白色普通日志
        };

        // 格式化日志：时间 + 类型 + 内容
        string formattedLog = $"<color={logColor}>[{System.DateTime.Now:HH:mm:ss}] [{type}]\n{logString}</color>";

        // 控制日志数量，防止内存溢出
        _logQueue.Enqueue(formattedLog);
        if (_logQueue.Count > MaxLogCount)
        {
            _logQueue.Dequeue();
        }

        // 更新日志文本
        _logText = string.Join("\n\n", _logQueue.ToArray());
    }

    // 绘制GUI
    private void OnGUI()
    {
        // 1. 绘制顶部控制按钮（显示/隐藏、清空、复制）
        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, 100));
        GUILayout.BeginHorizontal();

        // 显示/隐藏按钮
        if (GUILayout.Button(_isWindowVisible ? "隐藏日志" : "显示日志", GUILayout.Height(90), GUILayout.Width(220)))
        {
            _isWindowVisible = !_isWindowVisible;
        }

        // 清空日志按钮
        if (GUILayout.Button("清空日志", GUILayout.Height(90), GUILayout.Width(220)))
        {
            _logQueue.Clear();
            _logText = "";
        }

        // 【新增】一键复制日志按钮
        if (GUILayout.Button("复制日志", GUILayout.Height(90), GUILayout.Width(220)))
        {
            CopyAllLogsToClipboard();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        // 2. 如果隐藏了，就不绘制日志窗口
        if (!_isWindowVisible) return;

        // 3. 绘制日志窗口
        _windowRect = GUI.Window(0, _windowRect, DrawLogWindow, "===== 游戏日志查看器 =====");
    }

    // 绘制日志窗口内容
    private void DrawLogWindow(int windowID)
    {
        // 绘制滚动视图
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        // 设置日志样式
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = LogFontSize;
        textStyle.wordWrap = true;
        textStyle.richText = true; // 支持富文本（颜色）

        // 显示日志
        GUILayout.Label(_logText, textStyle);

        GUILayout.EndScrollView();

        // 允许拖动窗口
        GUI.DragWindow();
    }

    // 【新增】一键复制所有日志到剪贴板（安卓/PC通用）
    private void CopyAllLogsToClipboard()
    {
        if (string.IsNullOrEmpty(_logText))
        {
            Debug.Log("【日志查看器】没有日志可复制");
            return;
        }

        // 去除富文本颜色标签（<color=...> 和 </color>），方便粘贴阅读
        string cleanLog = RemoveRichTextTags(_logText);

        // 复制到系统剪贴板（安卓/PC通用）
        GUIUtility.systemCopyBuffer = cleanLog;

        // 手动加一条日志，提示复制成功
        AddCustomLog("已复制所有日志到剪贴板！", Color.green);
        Debug.Log("【日志查看器】已复制所有日志到剪贴板");
    }

    // 【新增】辅助方法：去除富文本标签
    private string RemoveRichTextTags(string input)
    {
        // 用正则表达式去除所有 <color=...> 和 </color> 标签
        string output = Regex.Replace(input, @"<color=[^>]*>", "");
        output = Regex.Replace(output, @"</color>", "");
        return output;
    }

    // 公共方法：外部可以手动添加日志
    public void AddCustomLog(string message, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string formattedLog = $"<color=#{colorHex}>[{System.DateTime.Now:HH:mm:ss}] [自定义]\n{message}</color>";

        _logQueue.Enqueue(formattedLog);
        if (_logQueue.Count > MaxLogCount)
        {
            _logQueue.Dequeue();
        }

        _logText = string.Join("\n\n", _logQueue.ToArray());
    }
}