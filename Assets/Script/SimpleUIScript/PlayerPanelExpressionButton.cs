using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanelExpressionButton : MonoBehaviour
{
    [Header("UI控件")]
    public LoopVerticalScrollRect scrollView;
    public Transform scrollViewContent;
    private RectTransform scrollViewRect;
    private RectTransform scrollViewContentRect;
    private GridLayoutGroup scrollViewGridLayout;
    private ScrollRect manualScrollRect;
    public UnityEngine.UI.Button Button;
    private string SingleRegisterButton = "PlayerPanelExpressionButton";

    [Header("单个表情的按钮图")]
    public GameObject ExpressionPrefabs;

    [Header("滚动视图动画参数")]
    public float ShowHeight = 280f;
    public float ShowWidth = 205f;
    public float AnimationTime = 0.3f;
    public Ease ShowEase = Ease.OutBack;
    public Ease HideEase = Ease.InQuad;
    public CanvasGroup scrollViewCanvasGroup;

    private Sequence _animaSequence;
    private Vector2 _originalSizeDelta;
    private List<ExpressionOption> _allExpressionOptionList = new List<ExpressionOption>();
    private Coroutine _createCoroutine;
    private Coroutine _recycleCoroutine;

    // 状态标志位
    private bool _isShowing = false;
    private bool _isHiding = false;

    private void Awake()
    {
        ButtonGroupManager.Instance.AddToggleButtonToGroup(SingleRegisterButton, Button, onActive: ShowExpressionScrollView, onCancel: HideExpressionScrollView);
        scrollViewRect = scrollView != null ? scrollView.GetComponent<RectTransform>() : null;
        scrollViewContentRect = scrollViewContent as RectTransform;
        scrollViewGridLayout = scrollViewContent != null ? scrollViewContent.GetComponent<GridLayoutGroup>() : null;
        SetupManualScrollRect();
        _originalSizeDelta = scrollViewRect != null ? scrollViewRect.sizeDelta : Vector2.zero; // 记录初始尺寸
    }


    private void SetupManualScrollRect()
    {
        if (scrollView == null || scrollViewContentRect == null)
        {
            return;
        }

        RectTransform viewport = scrollView.viewport;
        Scrollbar verticalScrollbar = scrollView.verticalScrollbar;
        scrollView.enabled = false;

        manualScrollRect = scrollView.GetComponent<ScrollRect>();
        if (manualScrollRect == null)
        {
            manualScrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
        }

        manualScrollRect.content = scrollViewContentRect;
        manualScrollRect.viewport = viewport;
        manualScrollRect.horizontal = false;
        manualScrollRect.vertical = true;
        manualScrollRect.movementType = ScrollRect.MovementType.Elastic;
        manualScrollRect.elasticity = 0.1f;
        manualScrollRect.inertia = true;
        manualScrollRect.decelerationRate = 0.135f;
        manualScrollRect.scrollSensitivity = 1f;
        manualScrollRect.horizontalScrollbar = null;
        manualScrollRect.verticalScrollbar = verticalScrollbar;
        manualScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        manualScrollRect.enabled = true;
    }
    #region 核心显隐逻辑
    public void ShowExpressionScrollView(string ButtonName)
    {
        if (_isHiding)
        {
            StopHidingProcess();
        }

        // 如果已经在显示，直接返回
        if (_isShowing) return;
        _isShowing = true;

        // 杀掉之前的动画
        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence();

        // 同时播放：淡入 + 尺寸展开
        _animaSequence.Join(scrollViewCanvasGroup.DOFade(1f, AnimationTime).SetEase(ShowEase));
        _animaSequence.Join(scrollViewRect.DOSizeDelta(new Vector2(ShowWidth, ShowHeight), AnimationTime).SetEase(ShowEase));

        // 动画完成后，开始创建表情按钮
        _animaSequence.OnComplete(() => {
            /* Debug.Log("表情面板展开完成，开始创建表情按钮"); */
            StartCreateExpressionButtons();
        });
    }

    public void HideExpressionScrollView(string ButtonName)
    {
        // 如果正在显示，先停止创建协程
        if (_isShowing)
        {
            StopShowingProcess();
        }

        // 如果已经在隐藏，直接返回
        if (_isHiding) return;
        _isHiding = true;

        // 杀掉之前的动画
        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence();

        // 同时播放：淡出 + 尺寸收起
        _animaSequence.Join(scrollViewCanvasGroup.DOFade(0f, AnimationTime).SetEase(HideEase));
        _animaSequence.Join(scrollViewRect.DOSizeDelta(_originalSizeDelta, AnimationTime).SetEase(HideEase));

        // 动画完成后，开始回收表情按钮
        _animaSequence.OnComplete(() => {
            /* Debug.Log("表情面板收起完成，开始回收表情按钮"); */
            StartRecycleExpressionButtons();
        });
    }

    // 停止显示流程
    private void StopShowingProcess()
    {
        _isShowing = false;
        if (_createCoroutine != null)
        {
            StopCoroutine(_createCoroutine);
            _createCoroutine = null;
        }
    }

    // 停止隐藏流程
    private void StopHidingProcess()
    {
        _isHiding = false;
        _animaSequence?.Kill(); // 立即停止动画
        if (_recycleCoroutine != null)
        {
            StopCoroutine(_recycleCoroutine);
            _recycleCoroutine = null;
        }
        // 停止隐藏后，直接把状态设为显示，避免逻辑混乱
        _isShowing = true;
    }
    #endregion

    #region 表情按钮创建与回收
    // 开始创建表情按钮
    private void StartCreateExpressionButtons()
    {
        if (_createCoroutine != null)
            StopCoroutine(_createCoroutine);
        _createCoroutine = StartCoroutine(CreateExpressionButtonsCoroutine());
    }

    private IEnumerator CreateExpressionButtonsCoroutine()
    {
        // 先清空旧的列表
        _allExpressionOptionList.Clear();

        if (ExpressionSystem.Instance == null || ExpressionSystem.Instance.EquipmentExpressionList == null)
        {
            _createCoroutine = null;
            yield break;
        }

        foreach (var expressionId in ExpressionSystem.Instance.EquipmentExpressionList)
        {
            // 如果中途停止显示，立即退出协程
            if (!_isShowing)
                yield break;

            var pack = ExpressionSystem.Instance.GetExpressionPack(expressionId);
            if (pack == null)
            {
                continue;
            }

            // 从对象池获取按钮
            GameObject expressionBtn = PoolManage.Instance.GetObj(ExpressionPrefabs);
            expressionBtn.transform.parent = scrollViewContent.transform;
            expressionBtn.transform.SetParent(scrollViewContent, false);

            // 初始化数据
            ExpressionOption option = expressionBtn.GetComponent<ExpressionOption>();
            option.InitExpressionOption(pack);
            _allExpressionOptionList.Add(option);

            yield return null;
        }

        RefreshExpressionScrollContentLayout(_allExpressionOptionList.Count);
        /* Debug.Log($"表情按钮创建完成，共创建 {_allExpressionOptionList.Count} 个"); */
        _createCoroutine = null;
    }


    private void RefreshExpressionScrollContentLayout(int itemCount)
    {
        if (scrollViewContentRect == null)
        {
            return;
        }

        Vector2 sizeDelta = scrollViewContentRect.sizeDelta;
        sizeDelta.y = CalculateScrollContentHeight(itemCount);
        scrollViewContentRect.sizeDelta = sizeDelta;
        scrollViewContentRect.anchoredPosition = Vector2.zero;

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContentRect);
        if (scrollViewRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewRect);
        }

        if (manualScrollRect != null)
        {
            manualScrollRect.StopMovement();
            manualScrollRect.verticalNormalizedPosition = 1f;
        }

        Canvas.ForceUpdateCanvases();
    }

    private float CalculateScrollContentHeight(int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0f;
        }

        if (scrollViewGridLayout == null)
        {
            return LayoutUtility.GetPreferredHeight(scrollViewContentRect);
        }

        int columnCount = 1;
        if (scrollViewGridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            columnCount = Mathf.Max(1, scrollViewGridLayout.constraintCount);
        }

        int rowCount = Mathf.CeilToInt(itemCount / (float)columnCount);
        float cellsHeight = rowCount * scrollViewGridLayout.cellSize.y;
        float spacingHeight = Mathf.Max(0, rowCount - 1) * scrollViewGridLayout.spacing.y;
        float paddingHeight = scrollViewGridLayout.padding.top + scrollViewGridLayout.padding.bottom;
        return Mathf.Max(0f, paddingHeight + cellsHeight + spacingHeight);
    }
    // 开始回收表情按钮
    private void StartRecycleExpressionButtons()
    {
        if (_recycleCoroutine != null)
            StopCoroutine(_recycleCoroutine);
        _recycleCoroutine = StartCoroutine(RecycleExpressionButtonsCoroutine());
    }

    private IEnumerator RecycleExpressionButtonsCoroutine()
    {
        // 倒序回收，避免列表索引问题
        for (int i = _allExpressionOptionList.Count - 1; i >= 0; i--)
        {
            // 如果中途停止隐藏，立即退出协程
            if (!_isHiding) yield break;

            if (_allExpressionOptionList[i] != null)
            {
                PoolManage.Instance.PushObj(ExpressionPrefabs, _allExpressionOptionList[i].gameObject);
            }
            _allExpressionOptionList.RemoveAt(i);

            // 每回收一个，等待一帧
            yield return null;
        }

        /* Debug.Log("表情按钮回收完成"); */
        RefreshExpressionScrollContentLayout(0);
        _isHiding = false;
        _recycleCoroutine = null;
    }
    #endregion

    private void OnDestroy()
    {
        // 清理所有动画和协程
        _animaSequence?.Kill();
        if (_createCoroutine != null) StopCoroutine(_createCoroutine);
        if (_recycleCoroutine != null) StopCoroutine(_recycleCoroutine);
    }
}