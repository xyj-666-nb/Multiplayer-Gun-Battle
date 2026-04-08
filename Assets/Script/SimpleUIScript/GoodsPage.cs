using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 【新增】必须引用

public class GoodsPage : MonoBehaviour
{
    [Header("组件关联")]
    public RectTransform DiscountRect;
    public TextMeshProUGUI DiscountText;
    public CanvasGroup DiscountCanvasGroup;
    private Sequence DiscountSequence;
    private Vector3 OriginalPos;
    public RectTransform GoldRect;
    public Image GoldBackGround;

    [Header("商品信息")]
    public Image GoodsImage;
    public Image BackGround;
    public TextMeshProUGUI GoodsName;
    public TextMeshProUGUI GoldNumber;

    [Header("动画参数")]
    public float DiscountShowTime = 1f;
    public float GoldCountTime = 1f;
    [Tooltip("打字速度（字符/秒）")]
    public float TypingSpeed = 30f;

    [Header("颜色信息配置")]
    public Color NormalColor;
    public Color RareColor;
    public Color EpicColor;

    [Header("介绍面板逻辑关联")]
    public CanvasGroup IntroduceCanvasGroup;
    public RawImage IntroduceRawImage;
    public TextMeshProUGUI IntroduceText;

    [Header("展开动画参数")]
    public float ExpandAnimaDuration = 0.5f;
    public float ExpendWight = 750;
    public float IdleWight = 238;

    private RectTransform MyRect;
    private GoodsData goodsData;
    [Header("交互展开按钮")]
    public Button MyButton;
    [Header("购买按钮")]
    public Button PurchaseButton;

    private bool isExpanded = false;
    private Sequence currentExpandSeq;

    private Coroutine typingCoroutine;

    void Start()
    {
        MyRect = GetComponent<RectTransform>();
        OriginalPos = DiscountRect.anchoredPosition;
        if (MyButton != null)
        {
            MyButton.onClick.AddListener(ToggleExpand);
        }

        PurchaseButton.onClick.AddListener(JudgePurchaseState);

        if (IntroduceCanvasGroup != null)
        {
            IntroduceCanvasGroup.alpha = 0;
            IntroduceCanvasGroup.blocksRaycasts = false;
        }
        
    }

    //判断购买状态
    public void JudgePurchaseState()
    {
        if (GoldSystem.Instance.GetGold() >= goodsData.goodsPrice)
        {
            WarnTriggerManager.Instance.TriggerDoubleInteraction2Warn($"是否购买商品:{goodsData.goodsName}", () => { }, () =>
            {
                //设置已购买状态
                SetAlreadyPurchase();//设置购买状态
                MerchantPeople.instance.MerchantPeopleSpeak("谢谢惠顾！赚大发了！");
                GoodDataManager.Instance.PurchaseGoodToUser(goodsData);//购买数据
            });
        }
        else
        {
            WarnTriggerManager.Instance.TriggerSingleInteractionWarn("金币不足!", "您当前金币不足！请以后再来。",() =>{ });
        }
    }

    #region 金币的消耗动画

    #endregion

    // 切换展开/收起状态
    public void ToggleExpand()
    {
        if (isExpanded)
        {
            HideExpendPage();
        }
        else
        {
            ShowExpandPage();
        }
    }

    // 展开页面
    // 展开页面
    public void ShowExpandPage()
    {
        isExpanded = true;
        currentExpandSeq?.Kill();

        StopTyping();

        MyRect.sizeDelta = new Vector2(IdleWight, MyRect.sizeDelta.y);
        IntroduceCanvasGroup.alpha = 0;
        IntroduceText.text = "";

        currentExpandSeq = DOTween.Sequence();

        currentExpandSeq.Append(MyRect.DOSizeDelta(new Vector2(ExpendWight, MyRect.sizeDelta.y), ExpandAnimaDuration)
            .SetEase(Ease.OutBack, 1.2f));

        currentExpandSeq.Insert(ExpandAnimaDuration * 0.4f, IntroduceCanvasGroup.DOFade(1, ExpandAnimaDuration * 0.4f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                TriggerIntroduceText();
                //打开射线检查的交互
                IntroduceCanvasGroup.blocksRaycasts = true;
            }));

        currentExpandSeq.OnComplete(() =>
        {
            if (MerchantPeople.instance != null)
                MerchantPeople.instance.MerchantPeopleSpeak("眼光不错！");

            var panel = UImanager.Instance.GetPanel<GoodsPanel>();
            panel?.UpdateContentWidth();
        });
    }

    // 收起页面
    public void HideExpendPage()
    {
        isExpanded = false;
        currentExpandSeq?.Kill();

        StopTyping();

        currentExpandSeq = DOTween.Sequence();
        IntroduceCanvasGroup.blocksRaycasts = false;
        currentExpandSeq.Append(IntroduceCanvasGroup.DOFade(0, ExpandAnimaDuration * 0.3f)
            .SetEase(Ease.InQuad));

        currentExpandSeq.AppendCallback(() =>
        {
            IntroduceText.text = "";
        });

        currentExpandSeq.Append(MyRect.DOSizeDelta(new Vector2(IdleWight, MyRect.sizeDelta.y), ExpandAnimaDuration * 0.6f)
            .SetEase(Ease.OutSine));

        currentExpandSeq.OnComplete(() =>
        {
            var panel = UImanager.Instance.GetPanel<GoodsPanel>();
            panel?.UpdateContentWidth();
        });
    }

    private IEnumerator TypeTextCoroutine(string text)
    {
        IntroduceText.text = "";
        float interval = 1f / TypingSpeed;

        for (int i = 0; i < text.Length; i++)
        {
            IntroduceText.text += text[i];
            yield return new WaitForSeconds(interval);
        }

        // 打字完成
        typingCoroutine = null;
    }

    // 统一的停止方法
    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (IntroduceText != null)
        {
            IntroduceText.text = "";
        }
    }

    // 触发介绍文本
    public void TriggerIntroduceText()
    {
        if (goodsData != null && !string.IsNullOrEmpty(goodsData.goodsDescription))
        {
            // 先停后开
            StopTyping();
            typingCoroutine = StartCoroutine(TypeTextCoroutine(goodsData.goodsDescription));
        }
    }

    public void InitData(GoodsData Data)
    {
        goodsData = Data;
    }

    public void SetDataInfo()
    {
        SetGoodsColor(goodsData.quality);
        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
        GoodsImage.DOFade(1, 0.5f);

        GoodsImage.sprite = goodsData.goodsIcon;
        GoodsName.text = goodsData.goodsName;

        PlayGoldNumberAnimation();
        //判断一次是否已经购买，设置购买状态
        if(GoodDataManager.Instance.JudgeUserHasGood(goodsData))//判断是否有这个商品了
        {
            //如果有了，设置已购买状态
            SetAlreadyPurchase();
        }
    }

    // 在对象池调用前进行重置
    public void ResetPos()
    {
        isExpanded = false;
        currentExpandSeq?.Kill();

        StopTyping();

        if (MyRect != null)
        {
            MyRect.sizeDelta = new Vector2(IdleWight, MyRect.sizeDelta.y);
        }

        if (IntroduceCanvasGroup != null)
        {
            IntroduceCanvasGroup.alpha = 0;
            IntroduceCanvasGroup.blocksRaycasts = false;
        }

        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
        GoldNumber.text = "0";
        DiscountRect.anchoredPosition = OriginalPos;
        if (DiscountSequence != null && DiscountSequence.IsActive())
        {
            DiscountSequence.Kill();
            DiscountCanvasGroup.alpha = 0;
        }
    }

    public void SetGoodsColor(GoodsQuality Quality)
    {
        switch (Quality)
        {
            case GoodsQuality.Normal:
                BackGround.DOColor(NormalColor, 1f);
                break;
            case GoodsQuality.Epic:
                BackGround.DOColor(RareColor, 1f);
                break;
            case GoodsQuality.Rare:
                BackGround.DOColor(EpicColor, 1f);
                break;
        }
    }

    public void ShowAnima()
    {
        DiscountAnima();
        SetDataInfo();
    }

    public void DiscountAnima()
    {
        DiscountRect.DOAnchorPos(Vector3.zero, DiscountShowTime);
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(DiscountCanvasGroup, ref DiscountSequence, true, () => { }, DiscountShowTime / 2);
    }

    private void PlayGoldNumberAnimation()
    {
        if (goodsData == null) return;

        int targetPrice = goodsData.goodsPrice;
        GoldNumber.text = "0";

        DOTween.To(() => 0, x => GoldNumber.text = x.ToString(), targetPrice, GoldCountTime)
            .SetEase(Ease.OutQuad);
    }

    //设置购买状态
    public void SetAlreadyPurchase()
    {
        // 设置已经购买的状态
        GoldBackGround.DOColor(ColorManager.EmeraldGreen, 1);//购买成功
        GoldNumber.text = "已购买";
        GoldNumber.fontSize = 40;//小字体
        // 禁止购买按钮
        PurchaseButton.GetComponentInChildren<TextMeshProUGUI>().text= "已购买";
        PurchaseButton.onClick.RemoveAllListeners();//移除监听
    }
}