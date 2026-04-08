using UnityEngine;

[RequireComponent(typeof(RectTransform))] // 确保是UI物体
public class UIPlayerMove : MonoBehaviour
{
    [Header("目标物体")]
    [Tooltip("需要进行颠簸动画的UI物体")]
    public RectTransform TargetBody;

    [Header("动画配置")]
    [Tooltip("基础高度（Y轴缩放）")]
    public float BaseYScale = 1f;
    [Tooltip("颠簸速度")]
    public float BumpySpeed = 10f;
    [Tooltip("颠簸幅度（建议 0.05 ~ 0.2）")]
    public float BumpyRange = 0.1f;
    [Tooltip("平滑过渡速度")]
    public float LerpSpeed = 5f;

    [Header("状态")]
    [Tooltip("是否正在播放动画")]
    public bool IsPlaying = false;

    // 内部状态变量
    private float _currentYScale;
    private float _targetYScale;
    private float _cachedTime;
    private Vector3 _cachedScale;
    private bool _isMoving; // 是否有水平移动

    #region 外部接口

    /// <summary>
    /// 开始移动动画
    /// </summary>
    public void StartMove()
    {
        IsPlaying = true;
        _isMoving = true;
        if (TargetBody != null)
        {
            _currentYScale = TargetBody.localScale.y;
        }
    }

    /// <summary>
    /// 停止移动动画
    /// </summary>
    public void StopMove()
    {
        _isMoving = false;
        // 停止时平滑恢复原状
        _targetYScale = BaseYScale;
    }

    /// <summary>
    /// 设置当前速度（仅用于判断是否在移动）
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        // 只要有水平速度就认为在移动
        _isMoving = Mathf.Abs(velocity.x) > 0.1f;
    }

    /// <summary>
    /// 手动设置基础缩放
    /// </summary>
    public void SetBaseScale(float baseScale)
    {
        BaseYScale = baseScale;
        if (!IsPlaying)
        {
            _currentYScale = baseScale;
            ApplyScale();
        }
    }

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        // 如果没指定目标，就用自己
        if (TargetBody == null)
        {
            TargetBody = GetComponent<RectTransform>();
        }

        // 初始化基础缩放
        if (TargetBody != null)
        {
            BaseYScale = TargetBody.localScale.y;
            _currentYScale = BaseYScale;
        }
    }

    private void Update()
    {
        if (TargetBody == null) return;

        if (IsPlaying && _isMoving)
        {
            PlayBumpyAnimation();
        }
        else
        {
            // 停止状态下平滑恢复
            RestoreScale();
        }
    }

    #endregion

    #region 内部动画逻辑

    private void PlayBumpyAnimation()
    {
        _cachedTime = Time.time;

        // 纯正弦波上下颠簸
        _targetYScale = BaseYScale + Mathf.Sin(_cachedTime * BumpySpeed) * BumpyRange;

        // 平滑过渡
        _currentYScale = Mathf.Lerp(_currentYScale, _targetYScale, LerpSpeed * Time.deltaTime);

        ApplyScale();
    }

    private void RestoreScale()
    {
        _targetYScale = BaseYScale;
        _currentYScale = Mathf.Lerp(_currentYScale, _targetYScale, LerpSpeed * Time.deltaTime);
        ApplyScale();
    }

    private void ApplyScale()
    {
        _cachedScale = TargetBody.localScale;
        _cachedScale.y = _currentYScale;
        TargetBody.localScale = _cachedScale;
    }

    #endregion
}