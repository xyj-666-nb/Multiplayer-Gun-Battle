using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TouchInputHandler Instance { get; private set; }
    private const int NoPointerId = int.MinValue;

    [Header("核心引用")]
    public playerHandControl HandControl;
    public Camera GameCamera;

    [Header("触摸设置")]
    public float MaxClickTolerance = 40f;
    [Tooltip("触摸灵敏度系数")]
    public float TouchSensitivity = 0.1f;

    // 内部状态
    private RectTransform _rectTransform;
    private int _currentControlFingerId = NoPointerId;
    private Vector2 _pressScreenPos;
    private Vector2 _lastFrameScreenPos; //
    private bool _isValidTouch = false;
    private bool _isDragging = false;
    private Vector2 _accumulatedTouchDelta;
    // 对外暴露的属性
    public Vector2 CurrentTouchWorldPos { get; private set; }
    public bool IsTouchActive => _currentControlFingerId != NoPointerId;
    public bool IsDragging => _isDragging;
    public Vector2 TouchDelta { get; private set; } // 新增：这一帧的滑动增量

    private void Awake()
    {
        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        GameCamera = MyCameraControl.Instance != null ? MyCameraControl.Instance.MainCamera : Camera.main;
    }

    private void Start()
    {
        transform.SetAsFirstSibling(); // 确保在最底层
        ResetStretchOffsets();
    }

    private void OnDisable()
    {
        ResetTouchState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void GetPlayerHand(playerHandControl HandControl)
    {
        this.HandControl = HandControl;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandlePointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HandlePointerUp(eventData);
    }

    public void ForwardPointerDown(PointerEventData eventData)
    {
        HandlePointerDown(eventData);
    }

    public void ForwardDrag(PointerEventData eventData)
    {
        HandleDrag(eventData);
    }

    public void ForwardPointerUp(PointerEventData eventData)
    {
        HandlePointerUp(eventData);
    }

    private void HandlePointerDown(PointerEventData eventData)
    {
        if (_currentControlFingerId != NoPointerId)
        {
            if (IsCurrentPointerStillDown())
                return;

            ResetTouchState();
        }

        _currentControlFingerId = eventData.pointerId;
        _pressScreenPos = eventData.position;
        _lastFrameScreenPos = eventData.position;
        _isValidTouch = true;
        _isDragging = false;

        _accumulatedTouchDelta = Vector2.zero;
        TouchDelta = Vector2.zero;
        UpdateTouchWorldPos(eventData.position);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != _currentControlFingerId) return;
        if (!_isValidTouch) return;

        Vector2 frameDelta = eventData.position - _lastFrameScreenPos;
        _lastFrameScreenPos = eventData.position;
        _accumulatedTouchDelta += frameDelta;
        TouchDelta = _accumulatedTouchDelta;
        UpdateTouchWorldPos(eventData.position);

        float moveDistance = Vector2.Distance(eventData.position, _pressScreenPos);
        if (moveDistance > MaxClickTolerance && !_isDragging)
        {
            _isDragging = true;
        }
    }

    private void HandlePointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _currentControlFingerId) return;

        ResetTouchState();
    }

    private void ResetTouchState()
    {
        _currentControlFingerId = NoPointerId;
        _isValidTouch = false;
        _isDragging = false;
        _accumulatedTouchDelta = Vector2.zero;
        TouchDelta = Vector2.zero;
    }

    public Vector2 ConsumeTouchDelta()
    {
        Vector2 delta = _accumulatedTouchDelta;
        _accumulatedTouchDelta = Vector2.zero;
        TouchDelta = Vector2.zero;
        return delta;
    }

    private bool IsCurrentPointerStillDown()
    {
        if (_currentControlFingerId == NoPointerId)
            return false;

        if (_currentControlFingerId == -1)
            return Input.GetMouseButton(0);
        if (_currentControlFingerId == -2)
            return Input.GetMouseButton(1);
        if (_currentControlFingerId == -3)
            return Input.GetMouseButton(2);
        if (_currentControlFingerId < 0)
            return false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId == _currentControlFingerId
                && touch.phase != TouchPhase.Ended
                && touch.phase != TouchPhase.Canceled)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryEnsureHandControl()
    {
        if (HandControl != null)
            return true;

        if (Player.LocalPlayer != null)
            HandControl = Player.LocalPlayer.MyHandControl;
        return HandControl != null;
    }

    private void UpdateTouchWorldPos(Vector2 screenPos)
    {
        TryEnsureHandControl();
        if (GameCamera == null || HandControl == null)
            return;
        Vector3 worldPos = GameCamera.ScreenToWorldPoint(new Vector3(
            screenPos.x, screenPos.y,
            HandControl.transform.position.z - GameCamera.transform.position.z
        ));
        worldPos.z = HandControl.transform.position.z;
        CurrentTouchWorldPos = worldPos;
    }

    private void ResetStretchOffsets()
    {
        if (_rectTransform == null)
            return;

        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
