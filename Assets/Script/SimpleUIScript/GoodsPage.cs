using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GoodsPage : MonoBehaviour
{
    [Header("组件关联")]
    public RectTransform DiscountRect;
    public TextMeshProUGUI DiscountText;//打折文本
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
    public GoodsData goodsData;
    [Header("交互展开按钮")]
    public Button MyButton;
    [Header("购买按钮")]
    public Button PurchaseButton;

    private bool isExpanded = false;
    private Sequence currentExpandSeq;

    private Coroutine typingCoroutine;

    [Header("子弹/打击特效 显示 —— 适配你的数据结构")]
    public CanvasGroup BulletShowCanvas;
    public Image _bulletImage;
    public Image _cartridgeCaseImage;
    [Header("配置")]
    public RenderTexture EffectTexture;//实时渲染图
    public Sprite DefaultSprite;//默认商品图
    public Image playerImage;//玩家形象图

    public Button PlayerButton;//播放按钮
    [Header("枪械皮肤专用")]
    public Image GunSkinImage;//枪械皮肤专用图
    public TextMeshProUGUI GunSkinName;//枪械皮肤专用名字

    void Start()
    {
        OriginalGoldBgColor = GoldBackGround.color;
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

        // 调用统一初始化函数
        InitBulletUI();
    }

    public void InitBulletUI()
    {
        // 原有代码不动
        if (BulletShowCanvas != null)
        {
            BulletShowCanvas.alpha = 0;
            BulletShowCanvas.blocksRaycasts = false;
        }
        if (GoodsImage != null)
        {
            GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 1);
        }
        if (_bulletImage != null)
        {
            _bulletImage.transform.localScale = Vector3.one;
        }

        // ========== 枪械皮肤：默认隐藏物体 ==========
        if (GunSkinImage != null) GunSkinImage.gameObject.SetActive(false);
        if (GunSkinName != null) GunSkinName.gameObject.SetActive(false);

        if (IntroduceRawImage != null)
        {
            IntroduceRawImage.texture = null;
            IntroduceRawImage.color = Color.white;
        }
        if (playerImage != null)
        {
            playerImage.color = ColorManager.SetColorAlpha(playerImage.color, 0);
            playerImage.sprite = null;
        }
        if (PlayerButton != null)
        {
            PlayerButton.onClick.RemoveAllListeners();
            PlayerButton.interactable = false;
            PlayerButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 展开时才设置预览显示
    /// </summary>
    private void SetupPreviewDisplay()
    {
        if (goodsData == null || IntroduceRawImage == null) return;

        // ========== 枪械皮肤显示逻辑 ==========
        if (goodsData.skinType == SkinType.GunAppearance && goodsData.gunSkinPack != null)
        {
            // 关闭所有其他显示
            IntroduceRawImage.gameObject.SetActive(false);
            playerImage.gameObject.SetActive(false);
            PlayerButton.gameObject.SetActive(false);
            // 开启枪械皮肤物体
            if (GunSkinImage != null) GunSkinImage.gameObject.SetActive(true);
            if (GunSkinName != null) GunSkinName.gameObject.SetActive(true);
            // 隐藏原有商品名称
            if (GoodsName != null) GoodsName.gameObject.SetActive(false);
            return;
        }
        else
        {
            // 非枪械皮肤：关闭枪械皮肤物体，恢复商品名称
            if (GunSkinImage != null) GunSkinImage.gameObject.SetActive(false);
            if (GunSkinName != null) GunSkinName.gameObject.SetActive(false);
            if (GoodsName != null) GoodsName.gameObject.SetActive(true);
        }

        //判断是否为 子弹皮肤 / 打击特效皮肤
        bool isBulletGoods = goodsData.skinType == SkinType.SpecialBullet && goodsData.bulletPack != null;
        bool isHitEffectGoods = goodsData.skinType == SkinType.GunHitEffect && goodsData.gunHitData != null;

        if (isBulletGoods || isHitEffectGoods)
        {
            IntroduceRawImage.gameObject.SetActive(true);
            playerImage.gameObject.SetActive(false);
            if (EffectTexture != null)
                IntroduceRawImage.texture = EffectTexture;

            if (PlayerButton != null)
            {
                PlayerButton.gameObject.SetActive(true);
                PlayerButton.interactable = true;
                PlayerButton.onClick.AddListener(PlaySkinDemo);
            }
        }
        else
        {
            IntroduceRawImage.gameObject.SetActive(true);
            if (DefaultSprite != null)
                IntroduceRawImage.texture = DefaultSprite.texture;

            playerImage.gameObject.SetActive(true);
            playerImage.DOKill();
            SimpleSpritePlayer.Stop(playerImage);

            if (goodsData.playerSkinPack != null)
            {
                playerImage.sprite = goodsData.playerSkinPack.IdleSprite;
                playerImage.color = Color.white;

                if (goodsData.playerSkinPack.IsHaveAnima && goodsData.playerSkinPack.AnimaSpriteList != null)
                {
                    SimpleSpritePlayer.PlayLoop(playerImage, goodsData.playerSkinPack.AnimaSpriteList, 0.1f);
                }
            }

            if (PlayerButton != null)
            {
                PlayerButton.gameObject.SetActive(false);
                PlayerButton.interactable = false;
                PlayerButton.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// 统一演示播放
    /// </summary>
    private void PlaySkinDemo()
    {
        if (DemoGun.Instance == null)
            return;

        if (goodsData.skinType == SkinType.SpecialBullet && goodsData.bulletPack != null)
        {
            DemoGun.Instance.TestShoot(goodsData.bulletPack);
        }
        else if (goodsData.skinType == SkinType.GunHitEffect && goodsData.gunHitData != null)
        {
            DemoGun.Instance.TestHitEffect(goodsData.gunHitData);
        }
    }

    //判断购买状态
    public void JudgePurchaseState()
    {
        int finalPrice = GoodDataManager.Instance.GetGoodsDiscountedPrice(goodsData);

        if (GoldSystem.Instance.GetGold() >= finalPrice)
        {
            WarnTriggerManager.Instance.TriggerDoubleInteraction2Warn($"是否购买商品:{goodsData.goodsName}", () => { }, () =>
            {
                SetAlreadyPurchase();
                MerchantPeople.instance.MerchantPeopleSpeak("谢谢惠顾！赚大发了！");
                GoodDataManager.Instance.PurchaseGoodToUser(goodsData);
            });
        }
        else
        {
            WarnTriggerManager.Instance.TriggerSingleInteractionWarn("金币不足!", "您当前金币不足！请以后再来。", () => { });
        }
    }

    // 切换展开/收起状态
    public void ToggleExpand()
    {
        if (isExpanded)
            HideExpendPage();
        else
            ShowExpandPage();
    }

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
        currentExpandSeq.Append(MyRect.DOSizeDelta(new Vector2(ExpendWight, MyRect.sizeDelta.y), ExpandAnimaDuration).SetEase(Ease.OutBack, 1.2f));

        currentExpandSeq.Insert(ExpandAnimaDuration * 0.1f, IntroduceCanvasGroup.DOFade(1, ExpandAnimaDuration * 0.1f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            TriggerIntroduceText();
            IntroduceCanvasGroup.blocksRaycasts = true;
            SetupPreviewDisplay();
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
        currentExpandSeq.Append(IntroduceCanvasGroup.DOFade(0, ExpandAnimaDuration * 0.3f).SetEase(Ease.InQuad));

        currentExpandSeq.AppendCallback(() => {
            IntroduceText.text = "";
            if (IntroduceRawImage != null) IntroduceRawImage.texture = null;
        });

        currentExpandSeq.Append(MyRect.DOSizeDelta(new Vector2(IdleWight, MyRect.sizeDelta.y), ExpandAnimaDuration * 0.6f).SetEase(Ease.OutSine));

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
        typingCoroutine = null;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = null;
        if (IntroduceText != null)
            IntroduceText.text = "";
    }

    public void TriggerIntroduceText()
    {
        if (goodsData != null && !string.IsNullOrEmpty(goodsData.goodsDescription))
        {
            StopTyping();
            typingCoroutine = StartCoroutine(TypeTextCoroutine(goodsData.goodsDescription));
        }
    }

    public void InitData(GoodsData Data)
    {
        goodsData = Data;
    }

    // 核心逻辑：先渲染通用UI，再分状态处理
    public void SetDataInfo()
    {
        SetGoodsColor(goodsData.quality);
        GoodsName.text = goodsData.goodsName;
        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
        GoodsImage.sprite = goodsData.goodsIcon;

        // ==========刷新所有显示==========
        RefreshAllDisplay();

        bool isPurchased = GoodDataManager.Instance.JudgeUserHasGood(goodsData);
        if (isPurchased)
        {
            SetAlreadyPurchase();
        }
        else
        {
            RefreshDiscountUI();
            PlayGoldNumberAnimation();
        }
    }

    // 折扣UI逻辑
    private void RefreshDiscountUI()
    {
        float discount = GoodDataManager.Instance.GetGoodsDiscount(goodsData.goodsGuid);

        if (DiscountText != null)
        {
            DiscountText.gameObject.SetActive(true);

            if (discount < 1.0f)
            {
                int discountInt = Mathf.RoundToInt(discount * 10);
                DiscountText.text = $"{discountInt}折";
                DiscountText.fontSize = 63;
            }
            else
            {
                DiscountText.text = "无打折";
                DiscountText.fontSize = 45;
            }
        }
    }

    public Color OriginalGoldBgColor;

    /// <summary>
    /// 统一刷新：枪械皮肤→子弹→默认图标
    /// </summary>
    private void RefreshAllDisplay()
    {
        BulletShowCanvas.alpha = 0;
        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 1);
        // 隐藏枪械皮肤物体
        if (GunSkinImage != null) GunSkinImage.gameObject.SetActive(false);
        if (GunSkinName != null) GunSkinName.gameObject.SetActive(false);
        // 默认显示商品名称
        if (GoodsName != null) GoodsName.gameObject.SetActive(true);

        if (goodsData == null) return;

        if (goodsData.skinType == SkinType.GunAppearance && goodsData.gunSkinPack != null)
        {
            if (GunSkinImage != null)
            {
                GunSkinImage.sprite = goodsData.gunSkinPack.skinIcon;
                GunSkinImage.gameObject.SetActive(true);

                string gunRealName = goodsData.gunSkinPack.GunRealName;
                if (gunRealName == "M762" || gunRealName == "AWP")
                {
                    GunSkinImage.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                }
                else
                {
                    GunSkinImage.transform.localScale = Vector3.one;
                }
            }
            if (GunSkinName != null)
            {
                GunSkinName.text = goodsData.gunSkinPack.skinName;
                GunSkinName.gameObject.SetActive(true);
            }
            // 隐藏主商品图标 + 原有商品名称
            GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
            if (GoodsName != null) GoodsName.gameObject.SetActive(false);
            return;
        }

        bool isEffectGoods = (goodsData.skinType == SkinType.SpecialBullet && goodsData.bulletPack != null)
                          || (goodsData.skinType == SkinType.GunHitEffect && goodsData.gunHitData != null);

        if (!isEffectGoods) return;

        if (goodsData.skinType == SkinType.GunHitEffect)
        {
            GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
            return;
        }

        BulletShowCanvas.alpha = 1;
        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);

        SpecialBulletBindPack bulletPack = goodsData.bulletPack;
        MilitaryManager military = MilitaryManager.Instance;
        GunType gunType = bulletPack.gunType;

        _bulletImage.transform.localScale = (gunType == GunType.Rifle || gunType == GunType.LightMachineGun)
            ? new Vector3(0.7f, 1f, 1f) : Vector3.one;

        Sprite bulletSprite = null;
        Sprite caseSprite = null;
        switch (gunType)
        {
            case GunType.Charge:
                bulletSprite = military.ChargeBullet;
                caseSprite = military.ChargeCartridgeCase;
                break;
            case GunType.Rifle:
            case GunType.LightMachineGun:
                bulletSprite = military.RifleBullet;
                caseSprite = military.RifleCartridgeCase;
                break;
            case GunType.Snipe:
            case GunType.DMR:
                bulletSprite = military.SnipeBullet;
                caseSprite = military.SnipeCartridgeCase;
                break;
        }

        if (bulletSprite != null) _bulletImage.sprite = bulletSprite;
        if (caseSprite != null) _cartridgeCaseImage.sprite = caseSprite;

        if (bulletPack.bulletVisualConfig != null)
        {
            _bulletImage.color = bulletPack.bulletVisualConfig.bulletColor;
            _cartridgeCaseImage.color = bulletPack.bulletVisualConfig.cartridgeCaseColor;
        }
    }

    // 对象池重置
    public void ResetPos()
    {
        isExpanded = false;
        currentExpandSeq?.Kill();
        StopTyping();

        if (MyRect != null)
            MyRect.sizeDelta = new Vector2(IdleWight, MyRect.sizeDelta.y);

        if (IntroduceCanvasGroup != null)
        {
            IntroduceCanvasGroup.alpha = 0;
            IntroduceCanvasGroup.blocksRaycasts = false;
        }

        InitBulletUI();

        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
        GoldNumber.text = "0";
        GoldNumber.fontSize = 63;
        DiscountRect.anchoredPosition = OriginalPos;
        // 重置商品名称显示
        if (GoodsName != null) GoodsName.gameObject.SetActive(true);

        if (DiscountSequence != null && DiscountSequence.IsActive())
        {
            DiscountSequence.Kill();
        }
        if (DiscountCanvasGroup != null) DiscountCanvasGroup.alpha = 0;
        if (DiscountText != null)
        {
            DiscountText.text = "";
            DiscountText.gameObject.SetActive(true);
        }

        if (IntroduceRawImage != null)
            IntroduceRawImage.texture = null;

        if (GunSkinImage != null)
        {
            GunSkinImage.transform.localScale = Vector3.one;
        }

        if (GoldBackGround != null)
        {
            GoldBackGround.DOKill(); // 停止颜色动画
            GoldBackGround.color = OriginalGoldBgColor;
        }

        // ======================重置购买按钮状态 ======================
        if (PurchaseButton != null)
        {
            PurchaseButton.onClick.RemoveAllListeners(); // 清空监听
            PurchaseButton.onClick.AddListener(JudgePurchaseState); // 重新绑定购买逻辑
            PurchaseButton.GetComponentInChildren<TextMeshProUGUI>().text = "购买";
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

    // 动画入口
    public void ShowAnima()
    {
        bool isPurchased = GoodDataManager.Instance.JudgeUserHasGood(goodsData);

        if (isPurchased)
        {
            SetDataInfo();
        }
        else
        {
            DiscountAnima();
            SetDataInfo();
        }
    }

    public void DiscountAnima()
    {
        DiscountRect.anchoredPosition = OriginalPos;
        DiscountCanvasGroup.alpha = 0;

        DiscountRect.DOAnchorPos(Vector3.zero, DiscountShowTime);
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(DiscountCanvasGroup, ref DiscountSequence, true, () => { }, DiscountShowTime / 2);
    }

    private void PlayGoldNumberAnimation()
    {
        if (goodsData == null)
            return;

        int targetPrice = GoodDataManager.Instance.GetGoodsDiscountedPrice(goodsData);

        GoldNumber.text = "0";
        DOTween.To(() => 0, x => GoldNumber.text = x.ToString(), targetPrice, GoldCountTime).SetEase(Ease.OutQuad);
    }

    // 设置已购买状态
    public void SetAlreadyPurchase()
    {
        GoldBackGround.DOColor(ColorManager.EmeraldGreen, 1);
        GoldNumber.text = "已购买";
        GoldNumber.fontSize = 40;

        if (DiscountText != null) DiscountText.gameObject.SetActive(false);
        if (DiscountCanvasGroup != null) DiscountCanvasGroup.alpha = 0;
        if (DiscountSequence != null && DiscountSequence.IsActive())
        {
            DiscountSequence.Kill();
        }

        PurchaseButton.GetComponentInChildren<TextMeshProUGUI>().text = "已购买";
        PurchaseButton.onClick.RemoveAllListeners();
    }
}