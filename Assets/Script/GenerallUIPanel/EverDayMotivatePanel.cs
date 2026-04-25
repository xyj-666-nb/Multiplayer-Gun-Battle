using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EverDayMotivatePanel : BasePanel
{
    [Header("抽奖页面")]
    public CanvasGroup PrizeDrawCanvas;
    public TextMeshProUGUI PrizeDrawText;//抽奖页面文本
    public CanvasGroup ButtonGroup;//按钮组Group.这里有广告按钮以及退出按钮
    private Sequence PrizeDrawCanvasSequence;//显影
    private Sequence ButtonGroupSequence;

    [Header("抽奖配置")]
    public List<int> goldNumberList = new List<int>() { 50, 100, 150, 200, 250, 300, 400, 500 };//抽奖数字列表
    public float rollDuration = 2f; // 数字滚动总时长
    public Ease rollEase = Ease.OutQuad; // 滚动缓动曲线（先快后慢）

    [Header("视觉效果配置")]
    public Color startColor = Color.yellow; // 起始颜色
    public Color endColor = Color.red; // 结束颜色
    public float minScale = 1f; // 最小缩放
    public float maxScale = 1.5f; // 最大缩放

    // 内部状态
    private int _finalRewardGold = 0; // 最终抽到的金币数
    private bool _hasGivenReward = false; // 防止重复发奖
    private bool _isRolling = false; // 是否正在滚动动画中

    // 缓存的初始状态
    private Vector3 _originalTextScale;
    private float _originalFontSize;
    [Header("图片动画")]
    public Image AnimaImage;
    public Sprite[] AnimaSpriteList;

    #region 核心显隐逻辑
    private void IsTriggerPrizeDrawPanel(bool IsTrigger)
    {
        PrizeDrawCanvas.blocksRaycasts = IsTrigger;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(PrizeDrawCanvas, ref PrizeDrawCanvasSequence, IsTrigger, () => { });
        if (IsTrigger)
        {
            // 打开面板时重置状态和视觉
            _finalRewardGold = 0;
            _hasGivenReward = false;
            _isRolling = false;

            // 缓存初始状态
            if (PrizeDrawText != null)
            {
                _originalTextScale = PrizeDrawText.rectTransform.localScale;
                _originalFontSize = PrizeDrawText.fontSize;
                // 重置视觉
                PrizeDrawText.color = startColor;
                PrizeDrawText.rectTransform.localScale = _originalTextScale;
                PrizeDrawText.fontSize = _originalFontSize;
            }

            PrizeDrawText.text = "???";
            IsTriggerButtonGroup(false);//默认关闭按钮组
        }
    }

    public void IsTriggerButtonGroup(bool IsTrigger)
    {
        controlDic["PrizeDrawbutton"].gameObject.SetActive(!IsTrigger);
        ButtonGroup.blocksRaycasts = IsTrigger;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ButtonGroup, ref ButtonGroupSequence, IsTrigger, () => { });
    }
    #endregion

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        IsTriggerPrizeDrawPanel(false);//默认关闭
        SimpleSpritePlayer.PlayLoop(AnimaImage, AnimaSpriteList,0.1f);
    }
    public override void Start()
    {
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 清理 DOTween 动画
        PrizeDrawText.DOKill();
        SimpleSpritePlayer.Stop(AnimaImage);
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void Update()
    {
        base.Update();
    }
    #endregion

    #region 控件触发
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "EnterButton")
        {
            //打开抽奖界面
            IsTriggerPrizeDrawPanel(true);
        }
        else if (controlName == "PrizeDrawbutton")
        {
            //开启抽奖动画
            StartPrizeDrawAnimation();
        }
        else if (controlName == "AdButton")
        {
            //广告：看激励广告，领取奖励
            WatchAdForReward();
        }
        else if (controlName == "CancelButton")
        {
            //退出界面，领取基础奖励
            TryGiveRewardAndExit(false);
        }
    }
    #endregion

    #region 抽奖动画核心逻辑
    /// <summary>
    /// 开始抽奖数字滚动动画
    /// </summary>
    private void StartPrizeDrawAnimation()
    {
        if (_isRolling) return;
        if (goldNumberList == null || goldNumberList.Count == 0)
        {
            /* Debug.LogError("[抽奖] 金币列表为空！"); */
            return;
        }

        _isRolling = true;

        // 随机选择最终奖励
        _finalRewardGold = goldNumberList[Random.Range(0, goldNumberList.Count)];
        /* Debug.Log($"[抽奖] 抽到最终奖励: {_finalRewardGold}"); */

        //  计算视觉参数
        float goldRatio = Mathf.InverseLerp(goldNumberList.Min(), goldNumberList.Max(), _finalRewardGold);
        Color targetColor = Color.Lerp(startColor, endColor, goldRatio);
        float targetScale = Mathf.Lerp(minScale, maxScale, goldRatio);

        // 清理之前的动画
        PrizeDrawText.DOKill();

        int startValue = 0;
        DOTween.To(() => startValue, x =>
        {
            // 每帧更新文本，显示当前滚动到的数字
            PrizeDrawText.text = x.ToString();
        }, _finalRewardGold, rollDuration)
        .SetEase(rollEase)
        .OnComplete(() =>
        {
            // 动画完成，定格显示最终奖励
            PrizeDrawText.text = _finalRewardGold.ToString();
            _isRolling = false;

            // 播放最终的视觉强化动画
            PlayFinalVisualEffect(targetColor, targetScale);

            // 激活按钮组
            IsTriggerButtonGroup(true);
            /* Debug.Log("[抽奖] 动画完成，按钮组已激活"); */
        });
    }

    /// <summary>
    /// 播放最终的视觉强化动画
    /// </summary>
    private void PlayFinalVisualEffect(Color targetColor, float targetScale)
    {
        // 颜色渐变：从当前颜色渐变到目标颜色
        PrizeDrawText.DOColor(targetColor, 0.5f).SetEase(Ease.OutQuad);

        // 大小缩放：先放大一点，再回弹到目标大小
        PrizeDrawText.rectTransform.DOScale(_originalTextScale * targetScale * 1.2f, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PrizeDrawText.rectTransform.DOScale(_originalTextScale * targetScale, 0.2f).SetEase(Ease.InQuad);
            });
    }
    #endregion

    #region 奖励发放与广告逻辑
    /// <summary>
    /// 看激励广告领取奖励
    /// </summary>
    private void WatchAdForReward()
    {
        if (_hasGivenReward)
        {
            /* Debug.LogWarning("[奖励] 奖励已发放过，跳过重复操作"); */
            TryClosePanel();
            return;
        }

        /* Debug.Log("[广告] 请求观看激励广告，领取抽奖奖励（×2）"); */
        TapAdManager.Instance.ShowRewardAd(
            onRewarded: () =>
            {
                // 广告看完成功：发放×2奖励并关闭
                TryGiveRewardAndExit(true);
            },
            onFailed: () =>
            {
                // 广告失败：发放基础奖励，保证体验
                /* Debug.LogWarning("[广告] 激励广告加载/观看失败，发放基础奖励"); */
                TryGiveRewardAndExit(false);
            }
        );
    }

    /// <summary>
    /// 尝试发放奖励并关闭面板
    /// </summary>
    /// <param name="isAdWatched">是否看了广告</param>
    private void TryGiveRewardAndExit(bool isAdWatched)
    {
        if (_hasGivenReward)
        {
            /* Debug.LogWarning("[奖励] 奖励已发放过，跳过重复操作"); */
            TryClosePanel();
            return;
        }

        // 计算最终奖励：看了广告×2
        int rewardToGive = isAdWatched ? (_finalRewardGold * 2) : _finalRewardGold;

        if (GoldSystem.Instance != null)
        {
            //GoldSystem.Instance.AddGold(rewardToGive);
            /* Debug.Log($"[奖励] 发放抽奖金币成功: {rewardToGive} (基础:{_finalRewardGold}, 看广告:{isAdWatched})"); */
           GoodDataManager.Instance.MarkDailyRewardGiven();//标记今日奖励已领取
        }
        else
        {
            /* Debug.LogError("[奖励] GoldSystem 不存在，无法发放金币！"); */
        }

        _hasGivenReward = true;
        TryClosePanel();
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    private void TryClosePanel()
    {
        // 清理动画
        PrizeDrawText.DOKill();
        // 关闭面板
        UImanager.Instance?.HidePanel<EverDayMotivatePanel>();
    }
    #endregion

    #region 面板隐藏以及显示
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }
    #endregion
}