using UnityEngine;

/// <summary>
/// 单例版GUI FPS显示面板（纯GUI绘制，平滑颜色渐变，样式隔离）
/// 无需UGUI组件，挂载即生效，Inspector可配置显隐/样式/采样/颜色
/// </summary>
public class FPSDisplayPanel : SingleMonoAutoBehavior<FPSDisplayPanel>
{
    #region 全局配置
    [Header("=== 全局控制 ===")]
    [Tooltip("是否显示FPS（一键开关）")]
    public bool isShowFPS = true;

    [Header("=== GUI绘制样式（直接可视化调整）===")]
    [Tooltip("FPS显示位置X（屏幕左上角为原点）")]
    public int guiPosX = 10;
    [Tooltip("FPS显示位置Y（屏幕左上角为原点）")]
    public int guiPosY = 10;
    [Tooltip("GUI字体大小")]
    public int guiFontSize = 20;
    [Tooltip("绘制区域高度（适配字体大小）")]
    public int guiLineHeight = 30;
    [Tooltip("绘制区域宽度（足够显示FPS数值）")]
    public int guiContentWidth = 100;

    [Header("=== FPS采样配置 ===")]
    [Tooltip("采样间隔（秒），越小更新越频繁，建议0.2-1.0")]
    public float sampleInterval = 0.5f;
    #endregion

    #region 颜色渐变配置（Inspector自定义）
    [Header("=== FPS颜色渐变配置 ===")]
    [Tooltip("低帧率阈值（低于此值显示纯红）")]
    public int minFPS = 30;
    [Tooltip("中帧率阈值（此区间平滑渐变黄-绿）")]
    public int midFPS = 60;
    [Tooltip("高帧率阈值（高于此值显示纯绿）")]
    public int maxFPS = 120;
    [Tooltip("低帧率颜色（默认红）")]
    public Color lowColor = Color.red;
    [Tooltip("中帧率颜色（默认黄）")]
    public Color midColor = Color.yellow;
    [Tooltip("高帧率颜色（默认绿）")]
    public Color highColor = Color.green;
    [Tooltip("初始未计算时的颜色（默认白）")]
    public Color initColor = Color.white;
    #endregion

    #region 内部核心变量（私有化，避免外部修改）
    private int _countDownTaskID = -1; // 定时器ID（下划线命名规范）
    private float _accumulatedTime;    // 累计采样时间
    private int _frameCount;           // 累计采样帧数
    private int _currentFPS;           // 最新计算的FPS整数值
    private bool _isFirstCalculate = true; // 首次计算标记（优化初始显示）
    private Color _targetGUIColor;     // GUI目标颜色（用于平滑渐变）
    #endregion

    #region 单例生命周期（完善初始化/销毁，严格空值校验）
    protected override void Awake()
    {
        base.Awake();
        // 初始化变量，防止空引用
        _accumulatedTime = 0;
        _frameCount = 0;
        _currentFPS = 0;
        _targetGUIColor = initColor;
        Debug.Log("[FPSDisplay] GUI版FPS面板初始化完成，单例已就绪");
    }

    protected void Start()
    {
        // 严格空值校验：CountDownManager不存在则禁用脚本
        if (CountDownManager.Instance == null)
        {
            Debug.LogError("[FPSDisplay] 启动失败：CountDownManager.Instance 为Null！");
            enabled = false;
            return;
        }

        // 创建永久定时器，执行FPS计算（毫秒转换）
        int delayMs = Mathf.RoundToInt(sampleInterval * 1000);
        _countDownTaskID = CountDownManager.Instance.CreateTimer_Permanent(true, delayMs, CalculateFPS);
        Debug.Log($"[FPSDisplay] 定时器创建成功，采样间隔：{sampleInterval}s（{delayMs}ms），TaskID：{_countDownTaskID}");
    }

    protected void Update()
    {
        // 仅在显示状态下累加数据，减少性能消耗
        if (!isShowFPS) return;

        _accumulatedTime += Time.deltaTime;
        _frameCount++;

        // GUI颜色平滑渐变（每帧插值，让颜色过渡更丝滑）
        if (_targetGUIColor != GUI.color)
        {
            GUI.color = Color.Lerp(GUI.color, _targetGUIColor, Time.deltaTime / 0.1f);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 安全移除定时器：判空+有效ID校验
        if (CountDownManager.Instance != null && _countDownTaskID != -1)
        {
            CountDownManager.Instance.RemoveTimer(_countDownTaskID);
            Debug.Log($"[FPSDisplay] 定时器已移除，TaskID：{_countDownTaskID}");
        }
        _countDownTaskID = -1;
    }
    #endregion

    #region 核心GUI绘制（样式全局隔离，不影响其他GUI，极简显示）
    private void OnGUI()
    {
        // 总开关关闭，直接不绘制
        if (!isShowFPS) 
            return;

        // 【关键】保存原始GUI样式/颜色，绘制后恢复，避免污染全局GUI
        int originalFontSize = GUI.skin.label.fontSize;
        Color originalColor = GUI.color;
        TextAnchor originalAlign = GUI.skin.label.alignment;

        try
        {
            // 配置独立的GUI样式（仅作用于当前FPS显示）
            GUI.skin.label.fontSize = guiFontSize;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            // 拼接显示文本：首次计算前显示"-- FPS"，更友好
            string fpsText = _isFirstCalculate ? "-- FPS" : $"{_currentFPS} FPS";
            // 核心一句绘制：指定位置+区域+文本
            GUI.Label(new Rect(guiPosX, guiPosY, guiContentWidth, guiLineHeight), fpsText);
        }
        finally
        {
            // 强制恢复原始样式，确保不影响场景中其他GUI组件
            GUI.skin.label.fontSize = originalFontSize;
            GUI.color = originalColor;
            GUI.skin.label.alignment = originalAlign;
        }
    }
    #endregion

    #region FPS计算 + GUI颜色平滑渐变（核心逻辑）
    /// <summary>
    /// 定时器回调：计算FPS，更新目标颜色
    /// </summary>
    private void CalculateFPS()
    {
        // 空值校验：累计时间为0则重置，避免除零错误
        if (_accumulatedTime <= 0.001f)
        {
            ResetSampleData();
            return;
        }

        // 计算FPS并取整
        float fpsValue = _frameCount / _accumulatedTime;
        _currentFPS = Mathf.RoundToInt(fpsValue);
        // 首次计算完成，取消标记
        if (_isFirstCalculate) _isFirstCalculate = false;

        // 计算并更新GUI目标渐变颜色
        UpdateGUITargetColor(_currentFPS);
        // 重置采样数据，准备下一次计算
        ResetSampleData();

        // 调试日志（可选删除）
        // Debug.Log($"[FPSDisplay] FPS计算完成：{_currentFPS}，目标颜色：{_targetGUIColor}");
    }

    /// <summary>
    /// 根据当前FPS值，计算GUI平滑渐变的目标颜色
    /// 分三段：低帧纯红 → 中帧红-黄渐变 → 高帧黄-绿渐变 → 满帧纯绿
    /// </summary>
    private void UpdateGUITargetColor(int fps)
    {
        if (fps <= minFPS)
        {
            // 低于低帧率：纯低色
            _targetGUIColor = lowColor;
        }
        else if (fps >= maxFPS)
        {
            // 高于高帧率：纯高色
            _targetGUIColor = highColor;
        }
        else if (fps <= midFPS)
        {
            // 低-中区间：低色到中色的平滑渐变
            float t = Mathf.InverseLerp(minFPS, midFPS, fps);
            _targetGUIColor = Color.Lerp(lowColor, midColor, t);
        }
        else
        {
            // 中-高区间：中色到高色的平滑渐变
            float t = Mathf.InverseLerp(midFPS, maxFPS, fps);
            _targetGUIColor = Color.Lerp(midColor, highColor, t);
        }
        // 强制不透明，避免GUI显示半透明
        _targetGUIColor.a = 1f;
    }

    /// <summary>
    /// 重置采样数据（抽离方法，简化代码）
    /// </summary>
    private void ResetSampleData()
    {
        _accumulatedTime = 0;
        _frameCount = 0;
    }
    #endregion

    #region 编辑器参数校验（OnValidate）：防止非法配置，实时修正
    private void OnValidate()
    {
        // 采样间隔限制：最小0.1s，避免定时器触发过频
        sampleInterval = Mathf.Max(sampleInterval, 0.1f);
        // FPS阈值校验：保证数值递增，避免逻辑错误
        minFPS = Mathf.Max(minFPS, 1);
        midFPS = Mathf.Max(midFPS, minFPS + 1);
        maxFPS = Mathf.Max(maxFPS, midFPS + 1);
        // GUI样式校验：避免负数位置/大小
        guiPosX = Mathf.Max(guiPosX, 0);
        guiPosY = Mathf.Max(guiPosY, 0);
        guiFontSize = Mathf.Clamp(guiFontSize, 12, 40);
        guiLineHeight = Mathf.Clamp(guiLineHeight, 20, 60);
        guiContentWidth = Mathf.Clamp(guiContentWidth, 80, 200);
        // 颜色强制不透明
        lowColor.a = 1f;
        midColor.a = 1f;
        highColor.a = 1f;
        initColor.a = 1f;
    }
    #endregion

    #region 对外公开接口（单例调用，控制显隐）
    /// <summary>
    /// 外部控制：显示FPS
    /// </summary>
    public void ShowFPS()
    {
        isShowFPS = true;
    }

    /// <summary>
    /// 外部控制：隐藏FPS
    /// </summary>
    public void HideFPS()
    {
        isShowFPS = false;
    }

    /// <summary>
    /// 外部控制：切换FPS显隐状态
    /// </summary>
    public void ToggleFPSShow()
    {
        isShowFPS = !isShowFPS;
    }
    #endregion
}