using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 纯触摸屏输入控制器
/// 功能：接收全屏触摸，把触摸坐标转换为世界坐标，驱动 playerHandControl
/// </summary>
public class TouchInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // 单例
    public static TouchInputHandler Instance { get; private set; }

    [Header("核心引用（必须赋值）")]
    [Tooltip("你的 playerHandControl 脚本")]
    public playerHandControl HandControl;

    [Tooltip("渲染游戏画面的摄像机")]
    public Camera GameCamera;

    [Header("触摸设置")]
    [Tooltip("最大点击容错像素，移动小于这个值算点击，大于算拖拽/瞄准")]
    public float MaxClickTolerance = 40f;


    // 内部状态
    private int _currentControlFingerId = -1; // 当前控制手臂/瞄准的手指ID
    private Vector2 _pressScreenPos; // 按下时的屏幕坐标
    private bool _isValidTouch = false; // 是否是有效触摸
    private bool _isDragging = false; // 是否正在拖拽/瞄准

    // 对外暴露的属性
    public Vector2 CurrentTouchWorldPos { get; private set; } // 当前触摸的世界坐标
    public bool IsTouchActive => _currentControlFingerId != -1; // 是否有有效触摸
    public bool IsDragging => _isDragging; // 是否正在拖拽/瞄准

    private void Awake()
    {
        Instance = this;
        GameCamera = MyCameraControl.Instance.MainCamera;
    }

    public void GetPlayerHand(playerHandControl HandControl)
    {
       this.HandControl= HandControl;
    }

    // 触摸按下
    public void OnPointerDown(PointerEventData eventData)
    {
        if (HandControl == null)
            return;

        // 如果已经有手指在控制，忽略新的触摸（只支持单指控制）
        if (_currentControlFingerId != -1)
            return;

        _currentControlFingerId = eventData.pointerId;
        _pressScreenPos = eventData.position;
        _isValidTouch = true;
        _isDragging = false;

        // 计算初始世界坐标
        UpdateTouchWorldPos(eventData.position);
    }

    // 触摸拖拽
    public void OnDrag(PointerEventData eventData)
    {
        if (HandControl == null)
            return;
        // 只处理当前控制的手指
        if (eventData.pointerId != _currentControlFingerId) 
            return;
        if (!_isValidTouch)
            return;

        // 更新世界坐标
        UpdateTouchWorldPos(eventData.position);

        // 判断是否超过点击容错距离
        float moveDistance = Vector2.Distance(eventData.position, _pressScreenPos);
        if (moveDistance > MaxClickTolerance && !_isDragging)
        {
            _isDragging = true;
        }
    }

    // 触摸抬起
    public void OnPointerUp(PointerEventData eventData)
    {
        if (HandControl == null)
            return;
        // 只处理当前控制的手指
        if (eventData.pointerId != _currentControlFingerId) return;
        // 重置状态
        _currentControlFingerId = -1;
        _isValidTouch = false;
        _isDragging = false;
    }

    private void UpdateTouchWorldPos(Vector2 screenPos)
    {
        if (GameCamera == null || HandControl == null) return;

        // 使用和原来鼠标逻辑一致的坐标转换
        Vector3 worldPos = GameCamera.ScreenToWorldPoint(new Vector3(
            screenPos.x, screenPos.y,
            HandControl.transform.position.z - GameCamera.transform.position.z
        ));
        worldPos.z = HandControl.transform.position.z;

        CurrentTouchWorldPos = worldPos;
    }
}