using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 控制Light2D强度在指定范围（0.3~0.7）内平滑闪烁
/// 新增：低概率触发短时间连续快闪（模拟真实电压不稳/接触不良）
/// </summary>
[RequireComponent(typeof(Light2D))]
public class Light2DFlicker : MonoBehaviour
{
    [Header("基础闪烁配置")]
    [Tooltip("最小强度（默认0.3）")]
    [Range(0f, 2f)]
    public float minIntensity = 0.3f;

    [Tooltip("最大强度（默认0.7）")]
    [Range(0f, 2f)]
    public float maxIntensity = 0.7f;

    [Tooltip("基础闪烁速度（值越大越快，建议0.5~3）")]
    public float baseFlickerSpeed = 1f;

    [Tooltip("是否启用基础随机间隔（模拟轻微不规则）")]
    public bool useBaseRandomInterval = false;
    public Vector2 baseRandomIntervalRange = new Vector2(0.1f, 0.5f);

    [Header("概率快闪配置（核心真实感）")]
    [Tooltip("每帧触发快闪的概率（0~1，建议0.005~0.02，即0.5%~2%）")]
    [Range(0f, 0.1f)]
    public float fastFlickerChance = 0.01f; // 1%概率每帧触发

    [Tooltip("快闪持续时间（建议0.3~1秒）")]
    public float fastFlickerDuration = 0.5f;

    [Tooltip("快闪速度（远大于基础速度，建议5~15）")]
    public float fastFlickerSpeed = 8f;

    [Tooltip("快闪时强度波动范围（可略大于基础范围，更夸张）")]
    public float fastFlickerIntensityOffset = 0.1f;

    [Header("全局控制")]
    public bool isFlickerEnabled = true;

    private Light2D _light2D;
    // 基础闪烁变量
    private float _baseRandomDelay;
    private float _baseTimer;
    // 快闪状态变量
    private bool _isFastFlickering; // 是否正在快闪
    private float _fastFlickerTimer; // 快闪剩余时长

    private void Awake()
    {
        _light2D = GetComponent<Light2D>();
        if (_light2D == null)
        {
            Debug.LogError("未找到Light2D组件！", this);
            enabled = false;
            return;
        }

        // 安全校验
        if (minIntensity > maxIntensity)
        {
            (minIntensity, maxIntensity) = (maxIntensity, minIntensity);
            Debug.LogWarning("修正最小强度大于最大强度的问题", this);
        }

        // 初始化基础随机间隔
        if (useBaseRandomInterval)
        {
            _baseRandomDelay = Random.Range(baseRandomIntervalRange.x, baseRandomIntervalRange.y);
        }
    }

    private void Update()
    {
        if (!isFlickerEnabled || _light2D == null) return;

        // 优先级：快闪 > 基础闪烁
        if (_isFastFlickering)
        {
            UpdateFastFlicker();
            return;
        }

        // 先判断是否触发快闪（低概率）
        TryTriggerFastFlicker();

        // 未触发快闪则执行基础闪烁
        UpdateBaseFlicker();
    }

    /// <summary>
    /// 尝试触发快闪（低概率）
    /// </summary>
    private void TryTriggerFastFlicker()
    {
        if (Random.value < fastFlickerChance)
        {
            _isFastFlickering = true;
            _fastFlickerTimer = fastFlickerDuration;
            Debug.Log("触发灯光快闪！");
        }
    }

    /// <summary>
    /// 执行快闪逻辑（连续快速闪动）
    /// </summary>
    private void UpdateFastFlicker()
    {
        _fastFlickerTimer -= Time.deltaTime;

        // 快闪结束，恢复基础状态
        if (_fastFlickerTimer <= 0)
        {
            _isFastFlickering = false;
            _fastFlickerTimer = 0;
            return;
        }

        // 快闪时的强度计算：更快的PingPong + 随机偏移（更不规则）
        float fastPingPong = Mathf.PingPong(Time.time * fastFlickerSpeed, 1f);
        // 快闪强度范围 = 基础范围 ± 偏移（比如0.2~0.8）
        float fastMin = minIntensity - fastFlickerIntensityOffset;
        float fastMax = maxIntensity + fastFlickerIntensityOffset;
        // 加微小随机值，避免绝对规律
        float randomOffset = Random.Range(-0.05f, 0.05f);
        float targetIntensity = Mathf.Lerp(fastMin, fastMax, fastPingPong) + randomOffset;
        // 限制强度≥0，避免负强度
        _light2D.intensity = Mathf.Max(0, targetIntensity);
    }

    /// <summary>
    /// 执行基础平滑闪烁逻辑
    /// </summary>
    private void UpdateBaseFlicker()
    {
        // 基础随机间隔逻辑
        if (useBaseRandomInterval)
        {
            _baseTimer += Time.deltaTime;
            if (_baseTimer < _baseRandomDelay) return;

            _baseTimer = 0;
            _baseRandomDelay = Random.Range(baseRandomIntervalRange.x, baseRandomIntervalRange.y);
        }

        // 基础平滑闪烁（PingPong实现往复）
        float basePingPong = Mathf.PingPong(Time.time * baseFlickerSpeed, 1f);
        float targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, basePingPong);
        _light2D.intensity = targetIntensity;
    }

    #region 外部控制方法
    public void StartFlicker() => isFlickerEnabled = true;

    public void StopFlicker()
    {
        isFlickerEnabled = false;
        if (_light2D != null) _light2D.intensity = maxIntensity;
    }

    public void StopFlicker(float targetIntensity)
    {
        isFlickerEnabled = false;
        if (_light2D != null)
        {
            targetIntensity = Mathf.Clamp(targetIntensity, minIntensity, maxIntensity);
            _light2D.intensity = targetIntensity;
        }
    }
    #endregion
}