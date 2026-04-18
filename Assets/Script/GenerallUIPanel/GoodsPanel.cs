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
        CheckRefreshState();//检查一下状态

    }
    #endregion

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
        {
            // UI返回音效
            MusicManager.Instance.PlayEffect("Music/update415/ui返回");
            ClearAllGoodsPage();
            UImanager.Instance.HidePanel<GoodsPanel>();
            ShopInteract.Instance.ExitShopSystem();
        }
        else if (controlName == "GoldPromptButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            GoldIntroducePanel.blocksRaycasts = true;
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GoldIntroducePanel, ref GoldIntroduceSequence, true, () => { });
        }
        else if (controlName == "ConfirmButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            GoldIntroducePanel.blocksRaycasts = false;
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GoldIntroducePanel, ref GoldIntroduceSequence, false, () => { });
        }
        else if (controlName == "RefreshButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            //如果已经达到上限就进行提示
            if (GoodDataManager.Instance.IsReachUpperLimit())
            {
                WarnTriggerManager.Instance.TriggerNoInteractionWarn(1, "今日已经达到上限！");
            }
            else
            {
                WarnTriggerManager.Instance.TriggerNoInteractionWarn(1, "刷新成功！");
                // 核心：执行商品刷新逻辑
                RefreshGoods();
            }
        }
    }

    public void CheckRefreshState()
    {
        if (GoodDataManager.Instance.IsReachUpperLimit())
        {
            //设置按钮状态
            SetRefreshState();
        }
    }

    private bool IsEnterState = false;
    public void SetRefreshState()
    {
        if (!IsEnterState)
        {
            IsEnterState = true;
            controlDic["RefreshButton"].GetComponent<Image>().DOColor(ColorManager.BrickRed, 1f);//设置状态
            controlDic["RefreshButton"].GetComponentInChildren<TextMeshProUGUI>().text = "已上限";
        }
    }

    /// <summary>
    /// 刷新商品：回收→重新加载→播放动画
    /// </summary>
    private void RefreshGoods()
    {
        //清空所有旧商品（回收到对象池）
        ClearAllGoodsPage();

        //刷新商品数据
        GoodDataManager.Instance.RefRefreshToDay();

        // 检查刷新上限状态
        CheckRefreshState();

        //重新创建商品
        CreateGoodsPage();

        //重新播放商品入场动画
        TriggerGoodsEnterAnima();

        //刷新金币显示
        PlayGoldNumberAnimation();
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

        var goodsList = GoodDataManager.Instance.ToDayRefreshGoodsList;
        // 修复1：用实际商品数量计算初始宽度
        float initWidth = CalculateRealTotalWidth(AnimationStartSpacing, AnimationStartLeft);
        ScrollViewContent.sizeDelta = new Vector2(initWidth, 0);

        for (int i = 0; i < goodsList.Count; i++)
        {
            if (GoodsPagePrefabs == null || GoodDataManager.Instance == null) return;

            // 获取当前索引的商品数据
            GoodsData currentData = goodsList[i];

            GameObject goods = PoolManage.Instance.GetObj(GoodsPagePrefabs);

            if (goods == null)
                continue;

            goods.transform.SetParent(ScrollViewContent, false);

            GoodsPage page = goods.GetComponent<GoodsPage>();

            if (page != null)
            {
                page.InitBulletUI();
                page.InitData(currentData);
                page.SetDataInfo(); 
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
        // 直接用精准计算，不搞复杂判断
        ScrollViewContent.sizeDelta = new Vector2(CalculateRealTotalWidth(LayoutGroupSpacing, LayoutGroupLeft), ScrollViewContent.sizeDelta.y);
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

        var goodsList = GoodDataManager.Instance.ToDayRefreshGoodsList;
        float startAnimWidth = CalculateRealTotalWidth(AnimationStartSpacing, AnimationStartLeft);
        ScrollViewContent.sizeDelta = new Vector2(startAnimWidth, 0);
        ContentLayoutGroup.spacing = AnimationStartSpacing;
        ContentLayoutGroup.padding.left = Mathf.RoundToInt(AnimationStartLeft);

        RectTransform parentRect = ScrollViewContent.parent as RectTransform;
        // 动画结束宽度
        float endAnimWidth = CalculateRealTotalWidth(LayoutGroupSpacing, LayoutGroupLeft);

        // 杀死旧动画，防止冲突
        DOTween.Kill(ContentLayoutGroup);
        DOTween.Kill(ScrollViewContent);

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

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }

    private float CalculateRealTotalWidth(float spacing, float leftPadding)
    {
        int childCount = ScrollViewContent.childCount;
        if (childCount == 0) return leftPadding + ContentLayoutGroup.padding.right;

        float totalWidth = leftPadding;
        totalWidth += ContentLayoutGroup.padding.right;
        totalWidth += spacing * (childCount - 1);

        // 累加所有商品真实宽度
        for (int i = 0; i < childCount; i++)
        {
            RectTransform childRT = ScrollViewContent.GetChild(i).GetComponent<RectTransform>();
            if (childRT != null) totalWidth += childRT.sizeDelta.x;
        }
        return totalWidth;
    }
}