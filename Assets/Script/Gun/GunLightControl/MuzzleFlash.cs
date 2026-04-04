using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MuzzleFlash : MonoBehaviour
{
    [Header("火光配置（核心）")]
    public MuzzleFlashConfig config;

    [Header("组件引用（优先自身获取）")]
    public Light2D flashLight;

    // 内部状态
    private enum FlashState { Idle, Playing }
    private FlashState _currentState = FlashState.Idle;
    private float _flashTimer;

    void Awake()
    {
        if (config == null)
        {
            //默认自动获取
            GameSkinManager.Instance.ReturnMuzzleFlashConfig(GetComponentInParent<BaseGun>().gunInfo.type );
        }

        // 优先自身获取2D光源，没有则添加
        if (flashLight == null)
        {
            flashLight = GetComponent<Light2D>();
            if (flashLight == null)
                flashLight = gameObject.AddComponent<Light2D>();
        }

        // 初始化光源
        flashLight.lightType = Light2D.LightType.Point;
        flashLight.enabled = false;

        ApplyBaseConfig();
    }

    void Update()
    {
        if (_currentState != FlashState.Playing || config == null) return;

        // 2D Z轴锁定
        if (config.lock2DZAxis)
        {
            Vector3 localPos = transform.localPosition;
            localPos.z = 0;
            transform.localPosition = localPos;
        }

        // 火光计时与总进度
        _flashTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_flashTimer / config.flashDuration);

        // 【核心新增】颜色渐变：从起始色 平滑过渡到 结束色
        Color currentColor = Color.Lerp(config.lightStartColor, config.lightEndColor, progress);

        // 三段式强度与范围
        float currentIntensity = CalculateThreeStageIntensity(progress);
        float currentRadius = CalculateThreeStageRadius(progress);

        // 全部应用到光源
        flashLight.color = currentColor;
        flashLight.intensity = currentIntensity;
        flashLight.pointLightOuterRadius = currentRadius;

        // 结束
        if (progress >= 1f)
            EndFlash();
    }

    #region 对外接口
    [ContextMenu("测试播放2D火光")]
    public void PlayFlash()
    {
        if (config == null) return;

        // 连射优化：直接重置计时
        if (_currentState == FlashState.Playing)
        {
            _flashTimer = 0f;
            return;
        }

        _currentState = FlashState.Playing;
        _flashTimer = 0f;

        // 激活光源
        flashLight.enabled = true;
        ApplyBaseConfig();
    }

    public void SetConfig(MuzzleFlashConfig newConfig)
    {
        config = newConfig;
        if (config != null)
            ApplyBaseConfig();
    }
    #endregion

    #region 核心渐变逻辑
    /// <summary>
    /// 三段式强度：快速点亮 -> 保持峰值 -> 缓慢熄灭
    /// </summary>
    private float CalculateThreeStageIntensity(float progress)
    {
        if (progress < 0.3f)
            return Mathf.Lerp(0, config.lightMaxIntensity, progress / 0.3f);
        else if (progress < 0.6f)
            return config.lightMaxIntensity;
        else
            return Mathf.Lerp(config.lightMaxIntensity, 0, (progress - 0.6f) / 0.4f);
    }

    /// <summary>
    /// 三段式范围：和强度同步
    /// </summary>
    private float CalculateThreeStageRadius(float progress)
    {
        if (progress < 0.3f)
            return Mathf.Lerp(0, config.lightRadius, progress / 0.3f);
        else if (progress < 0.6f)
            return config.lightRadius;
        else
            return Mathf.Lerp(config.lightRadius, 0, (progress - 0.6f) / 0.4f);
    }
    #endregion

    #region 内部工具
    /// <summary>
    /// 应用基础配置（初始化为起始颜色）
    /// </summary>
    private void ApplyBaseConfig()
    {
        if (config == null || flashLight == null) return;
        flashLight.color = config.lightStartColor;
    }

    /// <summary>
    /// 结束火光
    /// </summary>
    private void EndFlash()
    {
        _currentState = FlashState.Idle;
        flashLight.enabled = false;
    }
    #endregion
}