using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GoodsPanel : BasePanel
{
    [Header("UI控件关联")]
    public RectTransform ScrollViewContent;
    public HorizontalLayoutGroup ContentLayoutGroup;
    [Header("商店页面预制体")]
    public GameObject GoodsPagePrefabs;
    public int MaxGoodsCount => GoodDataManager.Instance.EverydayRefreshGoodsAmount;

    [Header("动画数据")]
    public float EnterAnimDuration = 0.8f;
    [Header("待机数据")]
    public float LayoutGroupSpacing = 15;
    public float LayoutGroupLeft = 20;
    public float ContentRightOffset = -80;
    [Header("动画入场起始数据")]
    public float AnimationStartSpacing = 600;
    public float AnimationStartLeft = 1400;

    private Dictionary<GameObject, GoodsData> goodsPageDict;
    private Coroutine updateCoroutine;

    [Header("Up进入动画")]
    public RectTransform UpRect;
    public CanvasGroup UpCanvasGroup;
    private Sequence UpSequence;
    public TextMeshProUGUI PanelTopic;
    public TextMeshProUGUI GoldNumber;//金币数量

    [Header("动画数据")]
    public float UpAreaY_Show = 0;//显示坐标

    [Header("金币疑问面板")]
    public CanvasGroup GoldIntroducePanel;
    private Sequence GoldIntroduceSequence;

    #region 生命周期
    public override void Awake()
    {
        base.Awake();

        goodsPageDict = new Dictionary<GameObject, GoodsData>();

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
            ShopInteract.Instance.ExitShopSystem();
        }
        else if (controlName == "GoldPromptButton")
        {
            GoldIntroducePanel.blocksRaycasts = true;
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GoldIntroducePanel, ref GoldIntroduceSequence, true, () => { });
        }
        else if (controlName == "ConfirmButton")
        {
            GoldIntroducePanel.blocksRaycasts = false;
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GoldIntroducePanel, ref GoldIntroduceSequence, false, () => { });
        }
    }
    #endregion

    #region 面板显隐
    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);


        if (goodsPageDict.Count > 0)
        {
            ClearAllGoodsPage();
        }
        CreateGoodsPage();
        TriggerGoodsEnterAnima();
        TriggerUpAnima();
    }
    #endregion

    private TypingWritingTask TypingTask;
    public void TriggerUpAnima()
    {
        PanelTopic.text = "";
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UpCanvasGroup, ref UpSequence, true, () => { });
        UpRect.DOAnchorPosY(0, 1).OnComplete(() => {
            TypingTask = SimpleAnimatorTool.Instance.AddTypingTask("每日限时商店", PanelTopic);
            PlayGoldNumberAnimation();
        });
    }

    private void PlayGoldNumberAnimation()
    {
        int targetPrice = GoldSystem.Instance.GetGold();
        GoldNumber.text = "0";
        DOTween.To(() => 0, x => GoldNumber.text = x.ToString(), targetPrice, 2f).SetEase(Ease.OutQuad);
    }

    public void CreateGoodsPage()
    {
        if (ContentLayoutGroup != null)
        {
            ContentLayoutGroup.spacing = AnimationStartSpacing;
            ContentLayoutGroup.padding.left = Mathf.RoundToInt(AnimationStartLeft);
        }

        float initWidth = GetStartAnimTotalWidth();
        ScrollViewContent.sizeDelta = new Vector2(initWidth, 0);


        var goodsList = GoodDataManager.Instance.ToDayRefreshGoodsList;
        for (int i = 0; i < goodsList.Count; i++)
        {
            if (GoodsPagePrefabs == null || GoodDataManager.Instance == null) return;

            // 获取当前索引的商品数据
            GoodsData currentData = goodsList[i];

            GameObject goods = PoolManage.Instance.GetObj(GoodsPagePrefabs);
            if (goods == null) continue;

            goods.transform.SetParent(ScrollViewContent, false);

            GoodsPage page = goods.GetComponent<GoodsPage>();
            if (page != null)
            {
                page.InitData(currentData);
            }

            goodsPageDict.Add(goods, currentData);
        }
    }

    public void UpdateContentWidth()
    {
        if (gameObject.activeInHierarchy)
        {
            if (updateCoroutine != null) StopCoroutine(updateCoroutine);
            updateCoroutine = StartCoroutine(UpdateContentWidthEndOfFrame());
        }
    }

    private IEnumerator UpdateContentWidthEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        SetContentWidthSmartly();
    }

    private void SetContentWidthSmartly()
    {
        if (ScrollViewContent == null) return;

        bool isAnyExpanded = false;
        float checkThreshold = 250;

        foreach (GameObject item in goodsPageDict.Keys)
        {
            if (item == null) continue;
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.x > checkThreshold)
            {
                isAnyExpanded = true;
                break;
            }
        }

        float targetWidth;
        if (isAnyExpanded)
        {
            targetWidth = CalculateCurrentTotalWidth();
        }
        else
        {
            RectTransform parentRect = ScrollViewContent.parent as RectTransform;
            if (parentRect != null)
            {
                targetWidth = parentRect.rect.width - LayoutGroupLeft - (ContentRightOffset * -1);
            }
            else
            {
                targetWidth = CalculateEstimatedIdleWidth();
            }
        }

        ScrollViewContent.sizeDelta = new Vector2(targetWidth, ScrollViewContent.sizeDelta.y);
    }

    private float CalculateCurrentTotalWidth()
    {
        if (ContentLayoutGroup == null || ScrollViewContent == null || goodsPageDict == null)
            return 0;

        float totalWidth = 0;
        foreach (GameObject item in goodsPageDict.Keys)
        {
            if (item == null) continue;
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                totalWidth += rect.sizeDelta.x;
            }
        }

        totalWidth += ContentLayoutGroup.spacing * (goodsPageDict.Count - 1);
        totalWidth += ContentLayoutGroup.padding.left;
        totalWidth += ContentLayoutGroup.padding.right;

        return totalWidth;
    }

    public void ClearAllGoodsPage()
    {
        foreach (var item in goodsPageDict.Keys)
        {
            if (item != null)
            {
                item.GetComponent<GoodsPage>().ResetPos();
                PoolManage.Instance.PushObj(GoodsPagePrefabs, item);
            }
        }
        // 清空字典
        goodsPageDict.Clear();
    }

    public void TriggerGoodsEnterAnima()
    {
        if (ContentLayoutGroup == null || ScrollViewContent == null) return;

        float startAnimWidth = GetStartAnimTotalWidth();
        ScrollViewContent.sizeDelta = new Vector2(startAnimWidth, 0);
        ContentLayoutGroup.spacing = AnimationStartSpacing;
        ContentLayoutGroup.padding.left = Mathf.RoundToInt(AnimationStartLeft);

        RectTransform parentRect = ScrollViewContent.parent as RectTransform;
        float endAnimWidth = parentRect.rect.width - LayoutGroupLeft - (ContentRightOffset * -1);

        Sequence masterSeq = DOTween.Sequence();

        masterSeq.Join(DOTween.To(() => ContentLayoutGroup.spacing, x => ContentLayoutGroup.spacing = x, LayoutGroupSpacing, EnterAnimDuration).SetEase(Ease.OutQuad));
        masterSeq.Join(DOTween.To(() => (float)ContentLayoutGroup.padding.left, x => ContentLayoutGroup.padding.left = Mathf.RoundToInt(x), LayoutGroupLeft, EnterAnimDuration).SetEase(Ease.OutQuad));
        masterSeq.Join(DOTween.To(() => ScrollViewContent.sizeDelta.x, x => ScrollViewContent.sizeDelta = new Vector2(x, ScrollViewContent.sizeDelta.y), endAnimWidth, EnterAnimDuration).SetEase(Ease.OutQuad));

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

    private float GetStartAnimTotalWidth()
    {
        RectTransform prefabRect = GoodsPagePrefabs.GetComponent<RectTransform>();
        float singleItemWidth = prefabRect != null ? prefabRect.rect.width : 238;
        float totalWidth = (singleItemWidth * MaxGoodsCount);
        totalWidth += AnimationStartSpacing * (MaxGoodsCount - 1);
        totalWidth += AnimationStartLeft + ContentLayoutGroup.padding.right;
        return totalWidth;
    }

    private float CalculateEstimatedIdleWidth()
    {
        RectTransform prefabRect = GoodsPagePrefabs.GetComponent<RectTransform>();
        float singleItemWidth = prefabRect != null ? prefabRect.rect.width : 238;
        float totalWidth = (singleItemWidth * MaxGoodsCount);
        totalWidth += LayoutGroupSpacing * (MaxGoodsCount - 1);
        totalWidth += LayoutGroupLeft + ContentLayoutGroup.padding.right;
        return totalWidth;
    }

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }
}