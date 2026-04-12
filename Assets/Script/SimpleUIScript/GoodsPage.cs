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

        if (IntroduceRawImage != null)
        {
            IntroduceRawImage.texture = null; // 清空渲染纹理
            IntroduceRawImage.color = Color.white; // 重置颜色
        }
        if (playerImage != null)
        {
            playerImage.color = ColorManager.SetColorAlpha(playerImage.color, 0); // 隐藏角色图
            playerImage.sprite = null; // 清空精灵
        }
        if (PlayerButton != null)
        {
            PlayerButton.onClick.RemoveAllListeners(); // 清空监听
            PlayerButton.interactable = false; // 默认禁用
            PlayerButton.gameObject.SetActive(false); // 默认隐藏
        }
    }

    /// <summary>
    /// 展开时才设置预览显示
    /// </summary>
    private void SetupPreviewDisplay()
    {
        if (goodsData == null || IntroduceRawImage == null) return;

        //判断是否为 子弹皮肤 / 打击特效皮肤
        bool isBulletGoods = goodsData.skinType == SkinType.SpecialBullet && goodsData.bulletPack != null;
        bool isHitEffectGoods = goodsData.skinType == SkinType.GunHitEffect && goodsData.gunHitData != null;

        if (isBulletGoods || isHitEffectGoods)
        {
            IntroduceRawImage.gameObject.SetActive(true);
            playerImage.gameObject.SetActive(false);
            // 赋值实时渲染图
            if (EffectTexture != null)
                IntroduceRawImage.texture = EffectTexture;

            // 播放按钮：启用+显示
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
                IntroduceRawImage.texture = DefaultSprite.texture; // RawImage用纹理

            playerImage.gameObject.SetActive(true);
            playerImage.DOKill();
            SimpleSpritePlayer.Stop(playerImage); // 先停止旧动画

            if (goodsData.playerSkinPack != null)
            {
                playerImage.sprite = goodsData.playerSkinPack.IdleSprite;
                playerImage.color = Color.white;

                // 有动画就播放动画
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

        // 播放子弹演示
        if (goodsData.skinType == SkinType.SpecialBullet && goodsData.bulletPack != null)
        {
            DemoGun.Instance.TestShoot(goodsData.bulletPack);
        }
        // 播放打击特效演示
        else if (goodsData.skinType == SkinType.GunHitEffect && goodsData.gunHitData != null)
        {
            DemoGun.Instance.TestHitEffect(goodsData.gunHitData);
        }
    }

    //判断购买状态
    public void JudgePurchaseState()
    {
        // 获取折后价
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
            // 收起时清空渲染纹理（可选，性能优化）
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
        // ========== 所有商品通用的基础UI渲染（无论是否购买都执行） ==========
        // 设置品质背景色
        SetGoodsColor(goodsData.quality);
        // 设置商品名称
        GoodsName.text = goodsData.goodsName;
        // 重置图标透明度
        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
        // 设置商品图标
        GoodsImage.sprite = goodsData.goodsIcon;
        // 刷新子弹/打击特效模型显示
        RefreshBulletDisplay();
        // 非特效类商品，显示图标
        if (goodsData.skinType != SkinType.SpecialBullet && goodsData.skinType != SkinType.GunHitEffect)
        {
            GoodsImage.DOFade(1, 0.5f);
        }

        // ========== 分状态处理：已购买 / 未购买 ==========
        bool isPurchased = GoodDataManager.Instance.JudgeUserHasGood(goodsData);
        if (isPurchased)
        {
            // 已购买：直接设置已购买状态，不处理折扣和价格动画
            SetAlreadyPurchase();
        }
        else
        {
            // 未购买：处理折扣显示 + 价格动画
            RefreshDiscountUI();
            PlayGoldNumberAnimation();
        }
    }

    // 折扣UI逻辑
    private void RefreshDiscountUI()
    {
        float discount = GoodDataManager.Instance.GetGoodsDiscount(goodsData.goodsGuid);

        // 确保折扣文本显示
        if (DiscountText != null)
        {
            DiscountText.gameObject.SetActive(true);

            if (discount < 1.0f)
            {
                // 有折扣：显示 "X折"
                int discountInt = Mathf.RoundToInt(discount * 10);
                DiscountText.text = $"{discountInt}折";
                DiscountText.fontSize = 63;
            }
            else
            {
                // 无折扣：显示 "无打折"
                DiscountText.text = "无打折";
                DiscountText.fontSize = 45;
            }
        }
    }

    /// <summary>
    /// 子弹/打击特效显示逻辑
    /// </summary>
    private void RefreshBulletDisplay()
    {
        // 组件缺失直接返回
        if (BulletShowCanvas == null || _bulletImage == null || _cartridgeCaseImage == null)
        {
            Debug.LogWarning("子弹显示组件未绑定！");
            return;
        }

        // 判断是否为特效类商品（子弹/打击特效）
        bool isEffectGoods = (goodsData.skinType == SkinType.SpecialBullet && goodsData.bulletPack != null)
                          || (goodsData.skinType == SkinType.GunHitEffect && goodsData.gunHitData != null);

        // 非特效商品：恢复默认显示
        if (!isEffectGoods)
        {
            BulletShowCanvas.alpha = 0;
            BulletShowCanvas.blocksRaycasts = false;
            GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 1);
            _bulletImage.transform.localScale = Vector3.one;
            return;
        }

        // 打击特效商品：仅隐藏图标，不显示子弹模型
        if (goodsData.skinType == SkinType.GunHitEffect)
        {
            BulletShowCanvas.alpha = 0;
            GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
            return;
        }

        // 仅子弹商品：显示子弹模型
        BulletShowCanvas.alpha = 1;
        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);

        SpecialBulletBindPack bulletPack = goodsData.bulletPack;
        MilitaryManager military = MilitaryManager.Instance;
        GunType gunType = bulletPack.gunType;

        if (gunType == GunType.Rifle || gunType == GunType.LightMachineGun)
        {
            _bulletImage.transform.localScale = new Vector3(0.7f, 1f, 1f);
        }
        else
        {
            _bulletImage.transform.localScale = Vector3.one;
        }

        // 赋值子弹/弹壳图片
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

        // 赋值图片+颜色
        if (bulletSprite != null)
            _bulletImage.sprite = bulletSprite;
        if (caseSprite != null)
            _cartridgeCaseImage.sprite = caseSprite;

        if (bulletPack.bulletVisualConfig != null)
        {
            _bulletImage.color = bulletPack.bulletVisualConfig.bulletColor;
            _cartridgeCaseImage.color = bulletPack.bulletVisualConfig.cartridgeCaseColor;
            _bulletImage.SetAllDirty();
            _cartridgeCaseImage.SetAllDirty();
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

        InitBulletUI(); // 调用统一初始化，重置所有UI

        GoodsImage.color = ColorManager.SetColorAlpha(GoodsImage.color, 0);
        GoldNumber.text = "0";
        GoldNumber.fontSize = 63; // 重置字体大小，防止被"已购买"改了不还原
        DiscountRect.anchoredPosition = OriginalPos;

        // 停止并重置折扣动画
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

    // 动画入口：已购买的不播放折扣动画
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
        // 确保位置重置
        DiscountRect.anchoredPosition = OriginalPos;
        DiscountCanvasGroup.alpha = 0;

        DiscountRect.DOAnchorPos(Vector3.zero, DiscountShowTime);
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(DiscountCanvasGroup, ref DiscountSequence, true, () => { }, DiscountShowTime / 2);
    }

    private void PlayGoldNumberAnimation()
    {
        if (goodsData == null)
            return;

        // 获取折后价作为目标
        int targetPrice = GoodDataManager.Instance.GetGoodsDiscountedPrice(goodsData);

        GoldNumber.text = "0";
        DOTween.To(() => 0, x => GoldNumber.text = x.ToString(), targetPrice, GoldCountTime).SetEase(Ease.OutQuad);
    }

    // 设置已购买状态
    public void SetAlreadyPurchase()
    {
        // 价格区域设置
        GoldBackGround.DOColor(ColorManager.EmeraldGreen, 1);
        GoldNumber.text = "已购买";
        GoldNumber.fontSize = 40;

        // 完全隐藏折扣相关
        if (DiscountText != null) DiscountText.gameObject.SetActive(false);
        if (DiscountCanvasGroup != null) DiscountCanvasGroup.alpha = 0;
        // 停止可能正在播放的折扣动画
        if (DiscountSequence != null && DiscountSequence.IsActive())
        {
            DiscountSequence.Kill();
        }

        // 购买按钮设置
        PurchaseButton.GetComponentInChildren<TextMeshProUGUI>().text = "已购买";
        PurchaseButton.onClick.RemoveAllListeners();
    }
}