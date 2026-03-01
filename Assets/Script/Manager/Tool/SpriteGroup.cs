using DG.Tweening;
using UnityEngine;

/// <summary>
/// 对标CanvasGroup的SpriteRenderer统一控制组件（仅透明度控制）
/// 支持单个/多个SpriteRenderer的透明度、显隐统一管理，兼容DOFade动画
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteGroup : MonoBehaviour
{
    [Header("核心配置（对标CanvasGroup）")]
    [Range(0f, 1f)]
    [Tooltip("整体透明度（0=完全透明，1=完全不透明）")]
    public float alpha = 1f;

    [Tooltip("是否可交互（仅标记，需自己在业务逻辑中判断）")]
    public bool interactable = true;

    [Header("需要控制的SpriteRenderer（自动/手动配置）")]
    public SpriteRenderer[] targetRenderers;

    [Tooltip("是否自动获取自身的SpriteRenderer")]
    public bool autoGetSelfRenderer = true;

    [Tooltip("是否自动获取所有子对象（含嵌套）的SpriteRenderer")]
    public bool autoGetAllChildRenderers = false;

    // 缓存原始颜色（避免透明度叠加错误）
    private Color[] _originalColors;

    private void Awake()
    {
        // 初始化：先清空原有数组，避免重复
        targetRenderers = new SpriteRenderer[0];

        // 1. 自动获取自身的SpriteRenderer
        if (autoGetSelfRenderer)
        {
            var selfRenderer = GetComponent<SpriteRenderer>();
            if (selfRenderer != null)
            {
                targetRenderers = new[] { selfRenderer };
            }
        }

        // 2. 自动获取所有子对象（含嵌套）的SpriteRenderer
        if (autoGetAllChildRenderers)
        {
            // 获取当前物体及所有子物体的SpriteRenderer（包括孙级、曾孙级）
            var allRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            if (allRenderers != null && allRenderers.Length > 0)
            {
                targetRenderers = allRenderers;
            }
        }

        // 缓存所有目标Renderer的原始颜色（只保留RGB，Alpha由组件统一控制）
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            _originalColors = new Color[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null)
                {
                    _originalColors[i] = targetRenderers[i].color;
                }
            }
        }

        // 初始化透明度
        UpdateAlpha();
    }

    /// <summary>
    /// 统一更新所有SpriteRenderer的透明度（核心方法）
    /// </summary>
    public void UpdateAlpha()
    {
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;

            // 保留原始RGB，仅修改Alpha通道
            Color newColor = _originalColors[i];
            newColor.a = alpha;
            targetRenderers[i].color = newColor;

            // 透明度低于0.01时隐藏Renderer（优化性能，可选）
            targetRenderers[i].enabled = alpha > 0.01f;
        }
    }

    // 编辑器模式下修改alpha值时，实时更新透明度
    private void OnValidate()
    {
        if (Application.isPlaying && _originalColors != null)
        {
            UpdateAlpha();
        }
    }

    #region 快捷动画方法（兼容DG.Tweening）
    /// <summary>
    /// 淡入淡出动画（和CanvasGroup的DOFade用法完全一致）
    /// </summary>
    /// <param name="targetAlpha">目标透明度</param>
    /// <param name="duration">动画时长</param>
    /// <returns>动画对象（可用于控制暂停/取消）</returns>
    public Tweener DOFade(float targetAlpha, float duration)
    {
        return DOTween.To(() => alpha, x =>
        {
            alpha = x;
            UpdateAlpha();
        }, targetAlpha, duration)
        .SetEase(Ease.Linear);
    }

    /// <summary>
    /// 快速淡入（alpha=1）
    /// </summary>
    public Tweener FadeIn(float duration = 0.3f)
    {
        return DOFade(1f, duration);
    }

    /// <summary>
    /// 快速淡出（alpha=0）
    /// </summary>
    public Tweener FadeOut(float duration = 0.3f)
    {
        return DOFade(0f, duration);
    }
    #endregion

    #region 快捷控制方法
    /// <summary>
    /// 显示（alpha=1，开启交互标记）
    /// </summary>
    public void Show()
    {
        alpha = 1f;
        interactable = true;
        UpdateAlpha();
    }

    /// <summary>
    /// 隐藏（alpha=0，关闭交互标记）
    /// </summary>
    public void Hide()
    {
        alpha = 0f;
        interactable = false;
        UpdateAlpha();
    }
    #endregion
}