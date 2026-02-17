using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour//血量UI
{
    public static HealthUI Instance;

    public Image HPImage;//血量图片
    private int AnimaIndex = -1;//动画索引
    private bool _isValid = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 清理旧实例的动画和引用
            Instance.CleanAllAnimations();
            Destroy(Instance.gameObject);
        }

        Instance = this;
        _isValid = HPImage != null && HPImage.gameObject != null;
        if (!_isValid)
        {
            Debug.LogError("[HealthUI] HPImage未赋值或所属对象已销毁！", this);
        }
    }

    // 新增：对外暴露的重置方法（玩家重生时调用）
    public void ResetHealthUI()
    {
        if (!_isValid) return;

        // 重置所有状态
        CleanAllAnimations();
        HPImage.fillAmount = 1f; // 重置血量为满
        HPImage.color = ColorManager.Red; // 重置颜色
        AnimaIndex = -1;
    }

    public void CleanAllAnimations()
    {

        if (HPImage != null)
        {
            HPImage.DOKill(true); // true=立即完成并回调
        }

        if (SimpleAnimatorTool.Instance != null && AnimaIndex != -1)
        {
            SimpleAnimatorTool.Instance.StopFloatLerpById(AnimaIndex);
            AnimaIndex = -1;
        }
        DOTween.Kill(this);
    }

    public void SetValue(float Value)//设置数值
    {
        // 核心校验：任何一步无效都直接返回
        if (!_isValid || this == null || HPImage == null || HPImage.gameObject == null)
        {
            Debug.LogWarning("[HealthUI] 组件/HPImage已销毁，跳过血量更新");
            return;
        }

        CleanAllAnimations();

        // 血量增加时的颜色变化
        if (HPImage.fillAmount < Value)
        {
            HPImage.DOColor(ColorManager.DarkGreen, 0.1f)
                .OnKill(() => { if (HPImage != null) HPImage.color = ColorManager.DarkGreen; }); // 防止动画中断导致颜色异常
        }

        // 重构插值动画：回调内添加多层校验
        AnimaIndex = SimpleAnimatorTool.Instance.StartFloatLerp(
            HPImage.fillAmount,
            Value,
            0.5f,
            (float newValue) =>
            {
                // 回调内校验：防止中途销毁
                if (_isValid && HPImage != null && HPImage.gameObject != null)
                {
                    HPImage.fillAmount = newValue;
                }
            },
            () =>
            {
                // 回调内校验：防止中途销毁
                if (_isValid && HPImage != null && HPImage.gameObject != null)
                {
                    HPImage.DOColor(ColorManager.Red, 0.1f)
                        .OnKill(() => { if (HPImage != null) HPImage.color = ColorManager.Red; });
                }
            });
    }

    private void OnDestroy()
    {
        CleanAllAnimations();

        if (Instance == this)
        {
            Instance = null;
        }

        _isValid = false;
        AnimaIndex = -1;
    }

    private void Update()
    {
        if (_isValid && (HPImage == null || HPImage.gameObject == null))
        {
            _isValid = false;
            CleanAllAnimations();
            Debug.LogWarning("[HealthUI] HPImage运行中被销毁，已清理所有动画", this);
        }
    }
}