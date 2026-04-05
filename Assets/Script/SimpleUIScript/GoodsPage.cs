using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoodsPage : MonoBehaviour
{
    [Header("组件关联")]
    public RectTransform DiscountRect;
    public TextMeshProUGUI DiscountText;
    public CanvasGroup DiscountCanvasGroup;
    private Sequence DiscountSequence;
    private Vector3 OriginalPos;
    public RectTransform GoldRect;

    [Header("商品信息")]
    public Image GoodsImage;
    public Image BackGround;
    public TextMeshProUGUI GoodsName;
    public TextMeshProUGUI GoldNumber;

    [Header("动画参数")]
    public float DiscountShowTime = 1f;
    public float GoldCountTime = 1f;

    [Header("颜色信息配置")]
    public Color NormalColor;
    public Color RareColor;
    public Color EpicColor;

    [Header("介绍面板逻辑关联")]
    public CanvasGroup IntroduceCanvasGroup;
    public RawImage IntroduceRawImage;
    public TextMeshProUGUI IntroduceText;

    [Header("展开动画参数")]
    public float ExpandAnimaDuration = 0.5f; // 稍微加快一点，配合弹性更灵动
    public float ExpendWight = 750;
    public float IdleWight = 238;

    private RectTransform MyRect;
    private GoodsData goodsData;
    private Button MyButton;

    private bool isExpanded = false; // 记录当前展开状态
    private Sequence currentExpandSeq; // 缓存当前的展开/收起序列，防止冲突

    void Start()
    {
        MyRect = GetComponent<RectTransform>();
        OriginalPos = DiscountRect.anchoredPosition;

        // 获取自身Button组件并绑定事件
        MyButton = GetComponent<Button>();
        if (MyButton != null)
        {
            MyButton.onClick.AddListener(ToggleExpand);
        }

        // 初始化介绍面板状态
        if (IntroduceCanvasGroup != null)
        {
            IntroduceCanvasGroup.alpha = 0;
            IntroduceCanvasGroup.blocksRaycasts = false;
        }
    }

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
    public void ShowExpandPage()
    {
        isExpanded = true;

        // 如果有正在进行的动画，先杀掉
        currentExpandSeq?.Kill();

        // 初始化宽度
        MyRect.sizeDelta = new Vector2(IdleWight, MyRect.sizeDelta.y);
        IntroduceCanvasGroup.alpha = 0;
        IntroduceText.text = "";

        // 创建展开序列
        currentExpandSeq = DOTween.Sequence();

        // 1宽度弹性展开 
        currentExpandSeq.Append(MyRect.DOSizeDelta(new Vector2(ExpendWight, MyRect.sizeDelta.y), ExpandAnimaDuration)
            .SetEase(Ease.OutBack, 1.2f)); // 1.2f 是弹性幅度

        // 在展开进行到 40% 的时候，插入介绍面板的淡入 
        currentExpandSeq.Insert(ExpandAnimaDuration * 0.4f, IntroduceCanvasGroup.DOFade(1, ExpandAnimaDuration * 0.4f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                TriggerIntroduceText();
            }));

        // 【新增】在整个展开动画结束后，更新父面板宽度
        currentExpandSeq.OnComplete(() =>
        {
            UImanager.Instance.GetPanel<GoodsPanel>()?.UpdateContentWidth();
        });
    }

    // 收起页面
    public void HideExpendPage()
    {
        isExpanded = false;

        currentExpandSeq?.Kill();

        // 创建收起序列
        currentExpandSeq = DOTween.Sequence();

        currentExpandSeq.Append(IntroduceCanvasGroup.DOFade(0, ExpandAnimaDuration * 0.3f)
            .SetEase(Ease.InQuad));

        currentExpandSeq.AppendCallback(() =>
        {
            IntroduceText.text = "";
            if (Task != null)
                SimpleAnimatorTool.Instance.RemoveTypingTask(Task);
        });

        currentExpandSeq.Append(MyRect.DOSizeDelta(new Vector2(IdleWight, MyRect.sizeDelta.y), ExpandAnimaDuration * 0.6f)
            .SetEase(Ease.OutSine));

        // 【新增】在整个收起动画结束后，更新父面板宽度
        currentExpandSeq.OnComplete(() =>
        {
            UImanager.Instance.GetPanel<GoodsPanel>()?.UpdateContentWidth();
        });
    }

    // 触发介绍文本
    private TypingWritingTask Task;
    public void TriggerIntroduceText()
    {
        if (goodsData != null && !string.IsNullOrEmpty(goodsData.goodsDescription))
        {
            IntroduceText.text = "";
            Task = SimpleAnimatorTool.Instance.AddTypingTask(goodsData.goodsDescription, IntroduceText);
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
    }

    // 在对象池调用前进行重置
    public void ResetPos()
    {
        // 重置状态
        isExpanded = false;
        currentExpandSeq?.Kill();

        // 重置尺寸
        if (MyRect != null)
        {
            MyRect.sizeDelta = new Vector2(IdleWight, MyRect.sizeDelta.y);
        }

        // 重置介绍面板
        if (IntroduceCanvasGroup != null)
        {
            IntroduceCanvasGroup.alpha = 0;
            IntroduceCanvasGroup.blocksRaycasts = false;
        }
        if (IntroduceText != null)
        {
            IntroduceText.text = "";
        }

        // 重置原有组件
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

    public void SetAlreadyPurchase()
    {
        // 设置已经购买的状态
    }
}