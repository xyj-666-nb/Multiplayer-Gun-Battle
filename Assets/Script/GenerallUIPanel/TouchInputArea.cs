using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TouchInputHandler Instance { get; private set; }

    [Header("核心引用")]
    public playerHandControl HandControl;
    public Camera GameCamera;

    [Header("触摸设置")]
    public float MaxClickTolerance = 40f;
    [Tooltip("触摸灵敏度系数")]
    public float TouchSensitivity = 0.1f; 

    // 内部状态
    private RectTransform _rectTransform;
    private int _currentControlFingerId = -1;
    private Vector2 _pressScreenPos;
    private Vector2 _lastFrameScreenPos; // 
    private bool _isValidTouch = false;
    private bool _isDragging = false;

    // 对外暴露的属性
    public Vector2 CurrentTouchWorldPos { get; private set; }
    public bool IsTouchActive => _currentControlFingerId != -1;
    public bool IsDragging => _isDragging;
    public Vector2 TouchDelta { get; private set; } // 新增：这一帧的滑动增量

    private void Awake()
    {
        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        GameCamera = MyCameraControl.Instance.MainCamera;
    }

    private void Start()
    {
        transform.SetAsFirstSibling(); // 确保在最底层
        ResetStretchOffsets();
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
        if (!TryEnsureHandControl())
            return;
        if (_currentControlFingerId != -1) return;

        _currentControlFingerId = eventData.pointerId;
        _pressScreenPos = eventData.position;
        _lastFrameScreenPos = eventData.position;
        _isValidTouch = true;
        _isDragging = false;

        TouchDelta = Vector2.zero;
        UpdateTouchWorldPos(eventData.position);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        if (!TryEnsureHandControl())
            return;
        if (eventData.pointerId != _currentControlFingerId) return;
        if (!_isValidTouch) return;

        TouchDelta = eventData.position - _lastFrameScreenPos;
        _lastFrameScreenPos = eventData.position;

        UpdateTouchWorldPos(eventData.position);

        float moveDistance = Vector2.Distance(eventData.position, _pressScreenPos);
        if (moveDistance > MaxClickTolerance && !_isDragging)
        {
            _isDragging = true;
        }
    }

    private void HandlePointerUp(PointerEventData eventData)
    {
        if (!TryEnsureHandControl())
            return;
        if (eventData.pointerId != _currentControlFingerId) return;

        _currentControlFingerId = -1;
        _isValidTouch = false;
        _isDragging = false;
        TouchDelta = Vector2.zero;
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
