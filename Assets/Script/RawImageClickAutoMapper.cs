using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RawImageClickAutoMapper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("核心引用（必须赋值）")]
    [Tooltip("渲染画面到RT的正交摄像机")]
    public Camera RenderCamera;
    [Tooltip("RawImage显示的RenderTexture")]
    public RenderTexture TargetRT;

    [Header("场景适配（和你的设置保持一致）")]
    [Tooltip("你的RT是水平翻转的，必须勾选")]
    public bool HorizontalFlipRT = true;
    [Tooltip("2D游戏必须勾选")]
    public bool Is2DGame = true;
    public float RaycastDistance = 1000f;

    [Header("调试")]
    public bool ShowDebugLog = true;

    private RawImage _targetRawImage;
    private RectTransform _rawImageRect;

    private void Awake()
    {
        _targetRawImage = GetComponent<RawImage>();
        _rawImageRect = _targetRawImage.rectTransform;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ProcessClick(eventData, ExecuteEvents.pointerClickHandler);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ProcessClick(eventData, ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ProcessClick(eventData, ExecuteEvents.pointerUpHandler);
    }

    // 【核心：适配你的场景的坐标转换逻辑】
    private void ProcessClick<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> eventFunction) where T : IEventSystemHandler
    {
        // 1. 基础校验
        if (_targetRawImage == null || RenderCamera == null || TargetRT == null)
        {
            Debug.LogError("【映射失败】核心引用未赋值！请检查RenderCamera和TargetRT");
            return;
        }

        // 2. 屏幕坐标 → RawImage局部坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rawImageRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        ))
        {
            if (ShowDebugLog) Debug.LogWarning("点击不在RawImage范围内");
            return;
        }

        // 3. 【关键修复】处理Fit In Parent的黑边，计算有效显示区域
        Rect rect = _rawImageRect.rect;
        float rawImageWidth = rect.width;
        float rawImageHeight = rect.height;
        float rtAspect = (float)TargetRT.width / TargetRT.height;
        float rawImageAspect = rawImageWidth / rawImageHeight;

        float uvXMin = 0f;
        float uvXMax = 1f;
        float uvYMin = 0f;
        float uvYMax = 1f;

        // 计算黑边的偏移量，只保留实际画面的有效区域
        if (rawImageAspect > rtAspect)
        {
            // RawImage比RT宽，左右有黑边
            float effectiveWidth = rawImageHeight * rtAspect;
            float offsetX = (rawImageWidth - effectiveWidth) / 2f;
            uvXMin = offsetX / rawImageWidth;
            uvXMax = 1f - uvXMin;
        }
        else
        {
            // RawImage比RT高，上下有黑边
            float effectiveHeight = rawImageWidth / rtAspect;
            float offsetY = (rawImageHeight - effectiveHeight) / 2f;
            uvYMin = offsetY / rawImageHeight;
            uvYMax = 1f - uvYMin;
        }

        // 把局部坐标转换为归一化UV，过滤黑边区域
        float uvX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float uvY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        // 点击在黑边里，直接不响应
        if (uvX < uvXMin || uvX > uvXMax || uvY < uvYMin || uvY > uvYMax)
        {
            if (ShowDebugLog) Debug.LogWarning("点击在黑边区域，无响应");
            return;
        }

        // 把有效区域的UV重新映射到0-1的RT坐标
        uvX = Mathf.InverseLerp(uvXMin, uvXMax, uvX);
        uvY = Mathf.InverseLerp(uvYMin, uvYMax, uvY);

        // 【修复水平翻转】你的RT是水平翻转的，反转X轴坐标
        if (HorizontalFlipRT)
        {
            uvX = 1f - uvX;
        }

        // 调试日志，方便你看坐标转换是否正确
        if (ShowDebugLog)
        {
            Debug.Log($"【坐标转换】屏幕点击坐标：{eventData.position}");
            Debug.Log($"【坐标转换】RawImage局部坐标：{localPoint}");
            Debug.Log($"【坐标转换】最终RT的UV坐标：({uvX:F4}, {uvY:F4})");
        }

        // 4. UV坐标 → 摄像机射线检测
        Vector2 viewportPoint = new Vector2(uvX, uvY);
        Ray ray = RenderCamera.ViewportPointToRay(viewportPoint);
        GameObject hitObject = null;

        if (Is2DGame)
        {
            // 2D正交相机专用射线检测
            RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction, RaycastDistance);
            if (hit2D.collider != null)
            {
                hitObject = hit2D.collider.gameObject;
            }
        }
        else
        {
            // 3D射线检测
            if (Physics.Raycast(ray, out RaycastHit hit, RaycastDistance))
            {
                hitObject = hit.collider.gameObject;
            }
        }

        // 5. 自动转发点击事件到按钮
        if (hitObject != null)
        {
            ExecuteEvents.Execute(hitObject, eventData, eventFunction);
            if (ShowDebugLog) Debug.Log($"【点击成功】已转发到物体：{hitObject.name}");
        }
        else
        {
            if (ShowDebugLog) Debug.LogWarning("射线未检测到任何物体，请检查碰撞体、Layer设置");
        }
    }
}