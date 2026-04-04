using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class MobileHorizontalLever : MonoBehaviour,
    IDragHandler,
    IEndDragHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("核心引用（必须赋值）")]
    public RectTransform bgRect;

    [Header("手感配置")]
    [Tooltip("自动回位的时间（秒）")]
    public float returnDuration = 0.2f;
    [Tooltip("触发方向的阈值比例，0.5=拉过半就触发全速")]
    [Range(0.1f, 0.9f)] public float triggerThreshold = 0.5f;

    [Header("手柄缩放手感")]
    [Tooltip("按下时的缩小比例，0.667=缩小到2/3")]
    public float pressedScale = 0.667f;
    [Tooltip("缩放动画的时长，越小反馈越快")]
    public float scaleDuration = 0.12f;
    [Tooltip("缩放动画的曲线，OutQuad手感最自然")]
    public Ease scaleEase = Ease.OutQuad;

    // 内部状态
    private RectTransform _handleRect;
    private Vector2 _initialAnchoredPos;
    private float _currentMaxOffsetX;
    private Vector3 _originalScale;
    private MyPlayerInput _localPlayerInput;

    // 【核心新增】状态机变量
    private bool _isInteracting = false; // 是否正在交互（手指是否在屏幕上）
    private int _currentDirection = 0;   // 当前记录的移动方向

    // 动画ID分离
    private const string SCALE_TWEEN_ID = "LeverScale";
    private const string POSITION_TWEEN_ID = "LeverPosition";

    private void Awake()
    {
        _handleRect = GetComponent<RectTransform>();
        _initialAnchoredPos = _handleRect.anchoredPosition;
        _originalScale = _handleRect.localScale;

        if (bgRect == null)
        {
            Debug.LogError("[MobileHorizontalLever] 请在Inspector赋值背景条BgRect！", this);
            return;
        }
        CalculateMaxOffset();
    }

    // 【核心修改】Update里每帧判断并持续调用
    private void Update()
    {
        // 只要正在交互、且获取到玩家、且方向不为0，就每帧持续调用
        if (_isInteracting && TryGetPlayerInput() && _currentDirection != 0)
        {
            _localPlayerInput.SetMoveDirection(_currentDirection);
        }
    }

    // 统一的动态获取方法
    private bool TryGetPlayerInput()
    {
        if (_localPlayerInput != null)
            return true;

        if (Player.LocalPlayer != null)
        {
            _localPlayerInput = Player.LocalPlayer.myInputSystem;
            return _localPlayerInput != null;
        }

        return false;
    }

    #region UI适配逻辑
    [ContextMenu("重新计算移动范围")]
    public void CalculateMaxOffset()
    {
        if (bgRect == null) return;
        float bgWidth = bgRect.rect.width;
        float handleWidth = _handleRect.rect.width;
        _currentMaxOffsetX = (bgWidth / 2f) - (handleWidth / 2f) - 5f;
        _currentMaxOffsetX = Mathf.Max(_currentMaxOffsetX, 10f);
    }
    #endregion

    #region 按下/抬起交互状态管理
    public void OnPointerDown(PointerEventData eventData)
    {
        // 【新增】标记开始交互
        _isInteracting = true;

        DOTween.Kill(SCALE_TWEEN_ID);
        _handleRect.DOScale(_originalScale * pressedScale, scaleDuration)
            .SetEase(scaleEase)
            .SetId(SCALE_TWEEN_ID);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 【新增】标记结束交互
        _isInteracting = false;
        RestoreScale();
    }

    private void RestoreScale()
    {
        DOTween.Kill(SCALE_TWEEN_ID);
        _handleRect.DOScale(_originalScale, scaleDuration)
            .SetEase(scaleEase)
            .SetId(SCALE_TWEEN_ID);
    }
    #endregion

    #region 核心拖拽逻辑（只负责更新位置和方向，不负责调用移动）
    public void OnDrag(PointerEventData eventData)
    {
        if (bgRect == null) return;

        DOTween.Kill(POSITION_TWEEN_ID);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _handleRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            // 1. 更新手柄物理位置
            float clampedX = Mathf.Clamp(localPoint.x, -_currentMaxOffsetX, _currentMaxOffsetX);
            _handleRect.anchoredPosition = new Vector2(clampedX, _initialAnchoredPos.y);

            // 2. 【修改】只更新方向状态，不在这里调用移动
            int newDirection = 0;
            float triggerValue = _currentMaxOffsetX * triggerThreshold;

            if (clampedX > triggerValue)
                newDirection = 1;
            else if (clampedX < -triggerValue)
                newDirection = -1;
            else
                newDirection = 0;

            // 如果方向变了，且获取到玩家，立即调用一次（防止切换方向时的卡顿）
            if (newDirection != _currentDirection && TryGetPlayerInput())
            {
                _localPlayerInput.SetMoveDirection(newDirection);
            }

            _currentDirection = newDirection;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 【新增】确保结束交互
        _isInteracting = false;

        RestoreScale();

        DOTween.Kill(POSITION_TWEEN_ID);
        _handleRect.DOAnchorPosX(_initialAnchoredPos.x, returnDuration)
            .SetEase(Ease.OutQuad)
            .SetId(POSITION_TWEEN_ID)
            .OnComplete(() =>
            {
                _currentDirection = 0;
                if (TryGetPlayerInput())
                {
                    _localPlayerInput.SetMoveDirection(0);
                }
            });
    }
    #endregion

    #region 辅助功能
    public void ForceReset()
    {
        _isInteracting = false;
        _currentDirection = 0;

        DOTween.Kill(SCALE_TWEEN_ID);
        DOTween.Kill(POSITION_TWEEN_ID);

        _handleRect.anchoredPosition = _initialAnchoredPos;
        _handleRect.localScale = _originalScale;

        if (TryGetPlayerInput())
        {
            _localPlayerInput.SetMoveDirection(0);
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(SCALE_TWEEN_ID);
        DOTween.Kill(POSITION_TWEEN_ID);
    }
    #endregion
}