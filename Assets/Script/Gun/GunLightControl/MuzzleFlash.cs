using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MuzzleFlash : MonoBehaviour
{
    [Header("火光配置（核心）")]
    public MuzzleFlashConfig config;

    [Header("双组件：图片火光 + 灯光")]
    public SpriteRenderer flashSprite;
    public Light2D flashLight;

    // 内部状态
    private enum FlashState { Idle, Playing }
    private FlashState _currentState = FlashState.Idle;
    private float _flashTimer;

    void Awake()
    {
        // 自动获取配置
        if (config == null)
        {
            GameSkinManager.Instance.ReturnMuzzleFlashConfig(GetComponentInParent<BaseGun>().gunInfo.type);
        }

        // 自动初始化图片
        if (flashSprite == null)
        {
            flashSprite = GetComponent<SpriteRenderer>();
            if (flashSprite == null)
                flashSprite = gameObject.AddComponent<SpriteRenderer>();
        }

        // 自动初始化灯光
        if (flashLight == null)
        {
            flashLight = GetComponent<Light2D>();
            if (flashLight == null)
                flashLight = gameObject.AddComponent<Light2D>();
        }

        // 初始完全关闭（0 DC 占用）
        flashSprite.enabled = false;
        flashLight.enabled = false;
        flashLight.intensity = 0;
    }

    void Update()
    {
        if (_currentState != FlashState.Playing || config == null)
            return;

        _flashTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_flashTimer / config.flashDuration);

        // ====================== 核心修改 ======================
        // 1. 灯光：恢复颜色渐变 + 强度渐变
        flashLight.color = Color.Lerp(config.lightStartColor, config.lightEndColor, progress);
        flashLight.intensity = Mathf.Lerp(config.lightMaxIntensity, 0f, progress);

        // 2. 图片：仅透明度渐变，无任何缩放/形变动画
        Color spriteColor = Color.Lerp(config.lightStartColor, config.lightEndColor, progress);
        spriteColor.a = Mathf.Lerp(1f, 0f, progress);
        flashSprite.color = spriteColor;

        // 结束动画
        if (progress >= 1f)
            EndFlash();
    }

    #region 对外接口
    [ContextMenu("测试播放火光")]
    public void PlayFlash()
    {
        if (config == null) return;

        // 连射优化
        if (_currentState == FlashState.Playing)
        {
            _flashTimer = 0f;
            flashSprite.color = config.lightStartColor;
            return;
        }

        // 启动
        _currentState = FlashState.Playing;
        _flashTimer = 0f;

        flashSprite.enabled = true;
        flashSprite.color = config.lightStartColor;

        // 灯光开启
        flashLight.enabled = true;
        flashLight.intensity = config.lightMaxIntensity;
        flashLight.color = config.lightStartColor;
    }

    public void SetConfig(MuzzleFlashConfig newConfig)
    {
        config = newConfig;
    }
    #endregion

    #region 结束逻辑
    private void EndFlash()
    {
        _currentState = FlashState.Idle;

        // 关闭图片
        flashSprite.enabled = false;

        // 彻底关闭灯光（释放DC）
        flashLight.enabled = false;
        flashLight.intensity = 0;
    }
    #endregion
}