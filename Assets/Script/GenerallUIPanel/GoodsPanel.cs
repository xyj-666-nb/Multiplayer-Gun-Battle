using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class GoodsPanel : BasePanel
{
    [Header("UI控件关联")]
    public RectTransform ScrollViewContent;
    public HorizontalLayoutGroup ContentLayoutGroup;
    [Header("商店页面预制体")]
    public GameObject GoodsPagePrefabs;
    public int MaxGoodsCount = 5;

    [Header("动画数据")]
    public float EnterAnimDuration = 0.8f;
    [Header("待机数据")]
    public float LayoutGroupSpacing = 15;
    public float LayoutGroupLeft = 20;
    public float ContentRightOffset = -80; // 最终的 Right 视觉效果
    [Header("动画入场起始数据")]
    public float AnimationStartSpacing = 600;
    public float AnimationStartLeft = 1400;

    private List<GameObject> GoodsPageManagerList;
    private Coroutine updateCoroutine; // 用于防抖动的协程

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        GoodsPageManagerList = new List<GameObject>();
        // 【优化】初始化时就把锚点定死，绝对不要在运行时改来改去
        // 锚点：Left-Stretch (0, 0) 到 (0, 1)
        if (ScrollViewContent != null)
        {
            ScrollViewContent.anchorMin = new Vector2(0, 0);
            ScrollViewContent.anchorMax = new Vector2(0, 1);
            ScrollViewContent.pivot = new Vector2(0, 0.5f);
        }
    }
    #endregion

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
        {
            ClearAllGoodsPage();
            UImanager.Instance.HidePanel<GoodsPanel>();
        }
    }
    #endregion

    #region 面板显隐
    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);

        if (GoodsPageManagerList.Count > 0)
        {
            ClearAllGoodsPage();
        }
        CreateGoodsPage();
        TriggerGoodsEnterAnima();
    }
    #endregion

    public void CreateGoodsPage()
    {
        // 1. 设置布局数据
        if (ContentLayoutGroup != null)
        {
            ContentLayoutGroup.spacing = AnimationStartSpacing;
            ContentLayoutGroup.padding.left = Mathf.RoundToInt(AnimationStartLeft);
        }

        // 2. 设置初始宽度（直接设，不做多余的重建）
        float initWidth = GetStartAnimTotalWidth();
        ScrollViewContent.sizeDelta = new Vector2(initWidth, 0);

        // 3. 生成物体
        for (int i = 0; i < MaxGoodsCount; i++)
        {
            if (GoodsPagePrefabs == null || GoodDataManager.Instance == null) return;

            GameObject goods = PoolManage.Instance.GetObj(GoodsPagePrefabs);
            if (goods == null) continue;

            goods.transform.SetParent(ScrollViewContent, false);

            GoodsPage page = goods.GetComponent<GoodsPage>();
            if (page != null)
            {
                page.InitData(GoodDataManager.Instance.GetData());
            }

            GoodsPageManagerList.Add(goods);
        }
    }

    // 【优化】公共方法：供外部调用
    public void UpdateContentWidth()
    {
        // 【优化】使用协程延迟一帧更新，避免在动画中间（同一帧）反复计算导致抖动
        if (gameObject.activeInHierarchy)
        {
            if (updateCoroutine != null) StopCoroutine(updateCoroutine);
            updateCoroutine = StartCoroutine(UpdateContentWidthEndOfFrame());
        }
    }

    private IEnumerator UpdateContentWidthEndOfFrame()
    {
        // 等到这一帧的结尾，所有动画数值都 settle 了再算
        yield return new WaitForEndOfFrame();
        SetContentWidthSmartly();
    }

    // 【核心逻辑】智能设置宽度，不切换锚点
    private void SetContentWidthSmartly()
    {
        if (ScrollViewContent == null) return;

        // 1. 检查是否有展开
        bool isAnyExpanded = false;
        float checkThreshold = 250;
        foreach (GameObject item in GoodsPageManagerList)
        {
            if (item == null) continue;
            RectTransform rect = item.GetComponent<RectTransform>();
            // 注意：这里用 sizeDelta.x 比 rect.width 更准，因为 rect.width 可能有延迟
            if (rect != null && rect.sizeDelta.x > checkThreshold)
            {
                isAnyExpanded = true;
                break;
            }
        }

        float targetWidth;

        if (isAnyExpanded)
        {
            // 2. 如果有展开，计算实际内容宽度
            targetWidth = CalculateCurrentTotalWidth();
        }
        else
        {
            RectTransform parentRect = ScrollViewContent.parent as RectTransform;
            if (parentRect != null)
            {
                // parentRect.rect.width 是父物体当前的宽度
                targetWidth = parentRect.rect.width - LayoutGroupLeft - (ContentRightOffset * -1);
            }
            else
            {
                // 兜底方案
                targetWidth = CalculateEstimatedIdleWidth();
            }
        }

        // 4. 直接赋值，只改这一次！
        ScrollViewContent.sizeDelta = new Vector2(targetWidth, ScrollViewContent.sizeDelta.y);
    }

    // 计算当前所有物体加起来的实际宽度
    private float CalculateCurrentTotalWidth()
    {
        if (ContentLayoutGroup == null || ScrollViewContent == null || GoodsPageManagerList == null)
            return 0;

        float totalWidth = 0;
        foreach (GameObject item in GoodsPageManagerList)
        {
            if (item == null) continue;
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                totalWidth += rect.sizeDelta.x; // 这里直接读 sizeDelta 是最准的
            }
        }

        totalWidth += ContentLayoutGroup.spacing * (GoodsPageManagerList.Count - 1);
        totalWidth += ContentLayoutGroup.padding.left;
        totalWidth += ContentLayoutGroup.padding.right;

        return totalWidth;
    }

    public void ClearAllGoodsPage()
    {
        for (int i = GoodsPageManagerList.Count - 1; i >= 0; i--)
        {
            if (GoodsPageManagerList[i] != null)
            {
                GoodsPageManagerList[i].GetComponent<GoodsPage>().ResetPos();
                PoolManage.Instance.PushObj(GoodsPagePrefabs, GoodsPageManagerList[i]);
            }
        }
        GoodsPageManagerList.Clear();
    }

    public void TriggerGoodsEnterAnima()
    {
        if (ContentLayoutGroup == null || ScrollViewContent == null) return;

        // 1. 入场前准备
        float startAnimWidth = GetStartAnimTotalWidth();
        // 直接设值，不要 ForceRebuild
        ScrollViewContent.sizeDelta = new Vector2(startAnimWidth, 0);
        ContentLayoutGroup.spacing = AnimationStartSpacing;
        ContentLayoutGroup.padding.left = Mathf.RoundToInt(AnimationStartLeft);

        // 2. 计算动画结束时的目标宽度 (模拟 Right = -80)
        RectTransform parentRect = ScrollViewContent.parent as RectTransform;
        float endAnimWidth = parentRect.rect.width - LayoutGroupLeft - (ContentRightOffset * -1);

        Sequence masterSeq = DOTween.Sequence();

        // 3. 动画：Spacing
        masterSeq.Join(DOTween.To(() => ContentLayoutGroup.spacing, x => ContentLayoutGroup.spacing = x, LayoutGroupSpacing, EnterAnimDuration)
            .SetEase(Ease.OutQuad));

        // 4. 动画：Padding Left
        masterSeq.Join(DOTween.To(() => (float)ContentLayoutGroup.padding.left, x => ContentLayoutGroup.padding.left = Mathf.RoundToInt(x), LayoutGroupLeft, EnterAnimDuration)
            .SetEase(Ease.OutQuad));

        // 5. 动画：Content 宽度
        masterSeq.Join(DOTween.To(() => ScrollViewContent.sizeDelta.x, x => ScrollViewContent.sizeDelta = new Vector2(x, ScrollViewContent.sizeDelta.y), endAnimWidth, EnterAnimDuration)
            .SetEase(Ease.OutQuad));

        // 6. 移除了动画结束后的强制切换逻辑，因为现在全程是一种模式

        float itemDelay = 0.1f;

        Transform[] children = new Transform[ScrollViewContent.childCount];
        for (int i = 0; i < ScrollViewContent.childCount; i++)
        {
            children[i] = ScrollViewContent.GetChild(i);
        }

        for (int i = 0; i < children.Length; i++)
        {
            int index = i;
            float delay = index * itemDelay;

            DOVirtual.DelayedCall(delay, () =>
            {
                if (children[index] != null)
                {
                    GoodsPage page = children[index].GetComponent<GoodsPage>();
                    if (page != null)
                    {
                        page.ShowAnima();
                    }
                }
            });
        }

        masterSeq.Play();
    }

    // 辅助：计算入场时那个超大的宽度
    private float GetStartAnimTotalWidth()
    {
        RectTransform prefabRect = GoodsPagePrefabs.GetComponent<RectTransform>();
        float singleItemWidth = prefabRect != null ? prefabRect.rect.width : 238;
        float totalWidth = (singleItemWidth * MaxGoodsCount);
        totalWidth += AnimationStartSpacing * (MaxGoodsCount - 1);
        totalWidth += AnimationStartLeft + ContentLayoutGroup.padding.right;
        return totalWidth;
    }

    // 辅助：估算收起时的宽度
    private float CalculateEstimatedIdleWidth()
    {
        RectTransform prefabRect = GoodsPagePrefabs.GetComponent<RectTransform>();
        float singleItemWidth = prefabRect != null ? prefabRect.rect.width : 238;
        float totalWidth = (singleItemWidth * MaxGoodsCount);
        totalWidth += LayoutGroupSpacing * (MaxGoodsCount - 1);
        totalWidth += LayoutGroupLeft + ContentLayoutGroup.padding.right;
        return totalWidth;
    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void SpecialAnimator_Hide()
    {
    }
}