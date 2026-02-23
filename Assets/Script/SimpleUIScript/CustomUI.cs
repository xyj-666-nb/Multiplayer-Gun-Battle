using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CustomUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("设置")]
    public NeedCustomUIType needCustomUIType;
    public Color selectedOutlineColor = Color.yellow;
    public float outlineThickness = 2f;
    public float fadeDuration = 0.5f;
    [Tooltip("是否在编辑器Scene视图中也显示描边")]
    public bool showInEditorSceneView = false;

    [Header("状态")]
    public bool isSelected = false;

    public static CustomUI currentSelectedUI;

    // 内部变量
    private PlayerCustomUIInfo CurrentPlayerCustomUIInfo;
    private RectTransform myRectTransform;
    private CanvasGroup MyCanvasGroup;
    private Vector2 dragOffset;
    private bool isDragging = false;

    // 编辑模式开关
    public static bool isEditModeEnabled = false;

    public Image OutLineImage;

    public RectTransform RectTransform => myRectTransform;
    public CanvasGroup CanvasGroup => MyCanvasGroup;
    public PlayerCustomUIInfo Info => CurrentPlayerCustomUIInfo;

    private void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
        MyCanvasGroup = GetComponent<CanvasGroup>();
        if (MyCanvasGroup == null)
            MyCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        InitOutlineImage();

        CurrentPlayerCustomUIInfo = PlayerAndGameInfoManger.Instance.GetPlayerCustomUIInfo(needCustomUIType, false);

        if (CurrentPlayerCustomUIInfo == null)
        {
            CurrentPlayerCustomUIInfo = CreateInfoPlayerCustomUIInfo();
            PlayerAndGameInfoManger.Instance.AddCustomUIInfoList(CurrentPlayerCustomUIInfo);
        }
        else
        {
            ApplicationInfo();
        }
    }

    #region 初始化 OutLineImage
    private void InitOutlineImage()
    {
        if (OutLineImage == null)
        {
            Transform child = transform.Find("SelectionOutline");
            if (child != null)
            {
                OutLineImage = child.GetComponent<Image>();
            }

            if (OutLineImage == null)
            {
                GameObject go = new GameObject("SelectionOutline");
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();

                OutLineImage = go.AddComponent<Image>();
                OutLineImage.sprite = CreateEmptySprite();
            }
        }

        OutLineImage.color = new Color(selectedOutlineColor.r, selectedOutlineColor.g, selectedOutlineColor.b, 0);
        OutLineImage.raycastTarget = false;

        RectTransform rt = OutLineImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-outlineThickness, -outlineThickness);
        rt.offsetMax = new Vector2(outlineThickness, outlineThickness);
    }

    private Sprite CreateEmptySprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
    }
    #endregion

    #region 点击选中/取消逻辑
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isEditModeEnabled) return;
        if (isDragging) return;

        if (isSelected) Deselect();
        else Select();
    }

    private void Select()
    {
        if (currentSelectedUI != null && currentSelectedUI != this)
        {
            currentSelectedUI.Deselect();
        }

        isSelected = true;
        currentSelectedUI = this;

        if (OutLineImage != null)
        {
            OutLineImage.gameObject.SetActive(true);
            OutLineImage.DOKill();
            OutLineImage.DOColor(new Color(selectedOutlineColor.r, selectedOutlineColor.g, selectedOutlineColor.b, 1), fadeDuration).SetUpdate(true);
        }

        PlayerCustomPanel panel = FindObjectOfType<PlayerCustomPanel>();
        if (panel != null && CurrentPlayerCustomUIInfo != null)
        {
            panel.UpdateCurrentControlPanel(CurrentPlayerCustomUIInfo);
        }

        Debug.Log($"玩家选中UI: {needCustomUIType}");
    }

    private void Deselect()
    {
        isSelected = false;
        if (currentSelectedUI == this)
        {
            currentSelectedUI = null;
        }

        if (OutLineImage != null)
        {
            OutLineImage.DOKill();
            OutLineImage.DOColor(new Color(selectedOutlineColor.r, selectedOutlineColor.g, selectedOutlineColor.b, 0), fadeDuration).SetUpdate(true);
        }

        Debug.Log($"玩家取消选中UI: {needCustomUIType}");
    }
    #endregion

    #region 拖拽逻辑
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isEditModeEnabled || !isSelected) return;
        isDragging = true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            myRectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        dragOffset = myRectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isEditModeEnabled || !isSelected || !isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            myRectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        myRectTransform.anchoredPosition = localPoint + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isEditModeEnabled || !isSelected) return;
        isDragging = false;
        UpdateInfo();
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (!showInEditorSceneView || myRectTransform == null) return;

        Gizmos.color = selectedOutlineColor;
        Vector3[] corners = new Vector3[4];
        myRectTransform.GetWorldCorners(corners);

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }

    #region 数据应用与更新
    public void ApplicationInfo()
    {
        if (CurrentPlayerCustomUIInfo == null)
        {
            Debug.LogWarning($"ApplicationInfo: {needCustomUIType} 的数据为空！");
            return;
        }

        if (myRectTransform != null)
        {
            myRectTransform.anchoredPosition = CurrentPlayerCustomUIInfo.anchoredPosition;
            myRectTransform.sizeDelta = CurrentPlayerCustomUIInfo.sizeDelta;
            myRectTransform.localEulerAngles = CurrentPlayerCustomUIInfo.localEulerAngles;
            myRectTransform.localScale = CurrentPlayerCustomUIInfo.localScale;
        }

        if (MyCanvasGroup != null)
        {
            MyCanvasGroup.alpha = CurrentPlayerCustomUIInfo.Alpha;
        }
    }

    public void UpdateInfo()
    {
        if (PlayerAndGameInfoManger.Instance.GetPlayerCustomUIInfo(needCustomUIType) != null)
        {
            var info = PlayerAndGameInfoManger.Instance.GetPlayerCustomUIInfo(needCustomUIType);

            if (myRectTransform != null)
            {
                info.anchoredPosition = myRectTransform.anchoredPosition;
                info.sizeDelta = myRectTransform.sizeDelta;
                info.localEulerAngles = myRectTransform.localEulerAngles;
                info.localScale = myRectTransform.localScale;
            }

            info.Alpha = MyCanvasGroup != null ? MyCanvasGroup.alpha : 1f;
            info.UIType = needCustomUIType;
        }
    }

    public PlayerCustomUIInfo CreateInfoPlayerCustomUIInfo()
    {
        var info = new PlayerCustomUIInfo();

        if (myRectTransform != null)
        {
            info.anchoredPosition = myRectTransform.anchoredPosition;
            info.sizeDelta = myRectTransform.sizeDelta;
            info.localEulerAngles = myRectTransform.localEulerAngles;
            info.localScale = myRectTransform.localScale;
        }

        info.Alpha = MyCanvasGroup != null ? MyCanvasGroup.alpha : 1f;
        info.UIType = needCustomUIType;

        return info;
    }
    #endregion

    private void OnDestroy()
    {
        if (OutLineImage != null)
        {
            OutLineImage.DOKill();
        }
    }

    public void OnPointerDown(PointerEventData eventData) { }
    public void OnPointerUp(PointerEventData eventData) { }
}