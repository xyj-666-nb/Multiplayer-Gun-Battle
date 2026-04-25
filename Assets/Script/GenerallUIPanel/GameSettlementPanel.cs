using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GameSettlementPanel : BasePanel
{
    private const string UiSelectSound = "Music/update415/ui选择";
    private const string UiBackSound = "Music/update415/ui返回";
    private const string SuccessSound = "Music/正式/交互/结束（成功";
    private const string FailSound = "Music/正式/交互/结束（失败";

    [Header("基础结算UI")]
    public Image RedImage;
    public Image BlueImage;
    public TextMeshProUGUI WinText;
    public TextMeshProUGUI RedScore;
    public TextMeshProUGUI BlueScore;
    public PlayableDirector TimeLine;

    [Header("金币显示")]
    public TextMeshProUGUI GoldNumber;
    private int _currentBaseGold = 0;
    private bool _hasGivenReward = false;
    private bool _isGoldRevealPlaying = false;
    private bool _canExitSettlement = false;
    private bool _shouldPopupAd = false;
    private Sequence _goldRevealSequence;
    private Color _originGoldColor = Color.white;
    private Vector3 _originGoldScale = Vector3.one;
    private const float MaxGoldScale = 1.3f;
    private const int GoldVisualCap = 300;

    [Header("激励广告面板")]
    public CanvasGroup MotivatePanelCanvasGroup;
    private Sequence MotivatePanelSequence;
    private const int GoldMulti = 3;
    public TextMeshProUGUI goldMultiText;
    [Header("广告弹出配置")]
    [Range(0, 100)] public int AdPopupChance = 60;

    public Team WinTeam;

    private Button _exitButton;

    #region 核心数据设置
    public void SetGoldData(int baseGold)
    {
        _currentBaseGold = Mathf.Max(0, baseGold);
        _hasGivenReward = false;
        _isGoldRevealPlaying = false;
        _canExitSettlement = false;
        PrepareHiddenGoldDisplay();
    }
    #endregion

    #region Timeline事件
    public void TimeLineTrigger()
    {
        RectTransform winRect;
        if (WinTeam == Team.Red)
        {
            winRect = RedImage.GetComponent<RectTransform>();
            WinText.text = "红方胜利";
        }
        else
        {
            winRect = BlueImage.GetComponent<RectTransform>();
            WinText.text = "蓝方胜利";
        }

        if (PlayerRespawnManager.Instance != null)
        {
            RedScore.text = PlayerRespawnManager.Instance.RedTeamScoreCount.ToString();
            BlueScore.text = PlayerRespawnManager.Instance.BlueTeamScoreCount.ToString();
        }

        PlaySettlementResultSound();

        winRect
            .DOScale(Vector3.one * 1.2f, 1f)
            .SetEase(Ease.OutQuad)
            .SetLink(winRect.gameObject);
    }

    public void TimeLineEnd()
    {
        TimeLine.Pause();
        PrepareHiddenGoldDisplay();
        SetExitButtonState(false, true);
        SetMotivatePanelActive(false);
        _shouldPopupAd = ShouldPopupAdPanel();

        WarRecordPanel warRecordPanel = UImanager.Instance.ShowPanel<WarRecordPanel>();
        warRecordPanel.SetCloseCallback(OnWarRecordPanelClosed);
    }
    #endregion

    #region 结算流程
    private void OnWarRecordPanelClosed()
    {
        if (_isGoldRevealPlaying)
        {
            return;
        }

        PlayGoldRevealAnimation();
    }

    private void PrepareHiddenGoldDisplay()
    {
        if (GoldNumber != null)
        {
            GoldNumber.text = "???";
            GoldNumber.color = _originGoldColor;
            GoldNumber.rectTransform.localScale = _originGoldScale;
        }

        if (goldMultiText != null)
        {
            goldMultiText.text = "???";
        }
    }

    private void PlayGoldRevealAnimation()
    {
        if (GoldNumber == null)
        {
            OnGoldRevealComplete();
            return;
        }

        _goldRevealSequence?.Kill();
        _isGoldRevealPlaying = true;
        _canExitSettlement = false;
        SetExitButtonState(false, true);

        float normalizedValue = Mathf.Clamp01(_currentBaseGold / (float)GoldVisualCap);
        float targetScaleValue = Mathf.Lerp(1f, MaxGoldScale, normalizedValue);
        Vector3 targetScale = _originGoldScale * targetScaleValue;
        Color targetColor = Color.Lerp(_originGoldColor, new Color(1f, 0.25f, 0.25f, 1f), normalizedValue);
        float duration = Mathf.Lerp(0.9f, 1.8f, normalizedValue);
        int displayedGold = 0;

        GoldNumber.text = "0";
        GoldNumber.color = _originGoldColor;
        GoldNumber.rectTransform.localScale = _originGoldScale;

        if (goldMultiText != null)
        {
            goldMultiText.text = (_currentBaseGold * GoldMulti).ToString();
        }

        _goldRevealSequence = DOTween.Sequence();
        _goldRevealSequence.Join(
            DOTween.To(() => displayedGold, x =>
            {
                displayedGold = x;
                GoldNumber.text = x.ToString();
            }, _currentBaseGold, duration).SetEase(Ease.OutCubic)
        );
        _goldRevealSequence.Join(GoldNumber.DOColor(targetColor, duration * 0.8f).SetEase(Ease.OutQuad));
        _goldRevealSequence.Join(GoldNumber.rectTransform.DOScale(targetScale, duration * 0.7f).SetEase(Ease.OutBack));
        _goldRevealSequence.AppendInterval(0.05f);
        _goldRevealSequence.Append(GoldNumber.rectTransform.DOPunchScale(targetScale * 0.08f, 0.35f, 6, 0.8f));
        _goldRevealSequence.OnComplete(OnGoldRevealComplete);
    }

    private void OnGoldRevealComplete()
    {
        _isGoldRevealPlaying = false;

        if (_shouldPopupAd)
        {
            SetMotivatePanelActive(true);
            return;
        }

        _canExitSettlement = true;
        SetExitButtonState(true, true);
    }
    #endregion

    #region 广告面板逻辑
    private bool ShouldPopupAdPanel()
    {
        int randomValue = UnityEngine.Random.Range(0, 100);
        /* Debug.Log($"[广告逻辑] 随机值:{randomValue}, 触发概率:{AdPopupChance}%"); */
        return randomValue < AdPopupChance;
    }

    public void SetMotivatePanelActive(bool isActive)
    {
        if (MotivatePanelCanvasGroup == null)
        {
            return;
        }

        MotivatePanelCanvasGroup.interactable = isActive;
        MotivatePanelCanvasGroup.blocksRaycasts = isActive;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(MotivatePanelCanvasGroup, ref MotivatePanelSequence, isActive, () => { });
    }
    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();

        if (GoldNumber != null)
        {
            _originGoldColor = GoldNumber.color;
            _originGoldScale = GoldNumber.rectTransform.localScale;
        }

        if (controlDic.ContainsKey("ExitButton"))
        {
            _exitButton = controlDic["ExitButton"] as Button;
        }

        if (MotivatePanelCanvasGroup != null)
        {
            MotivatePanelCanvasGroup.alpha = 0;
            MotivatePanelCanvasGroup.interactable = false;
            MotivatePanelCanvasGroup.blocksRaycasts = false;
        }

        PrepareHiddenGoldDisplay();
        SetExitButtonState(false, false);
    }

    protected override void OnDestroy()
    {
        _goldRevealSequence?.Kill();
        MotivatePanelSequence?.Kill();
        base.OnDestroy();
    }
    #endregion

    #region UI按钮点击
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "ExitButton":
                if (!_canExitSettlement)
                {
                    return;
                }
                MusicManager.Instance?.PlayEffect(UiBackSound);
                TryGiveBaseGoldAndExit();
                break;
            case "ConfirmButton":
                MusicManager.Instance?.PlayEffect(UiSelectSound);
                SetExitButtonState(false, true);
                /* Debug.Log($"[广告] 请求观看激励广告，预期奖励: {_currentBaseGold * GoldMulti}"); */
                TapAdManager.Instance.ShowRewardAd(
                    onRewarded: OnAdRewarded,
                    onFailed: OnAdFailed
                );
                break;
            case "CancelButton":
                MusicManager.Instance?.PlayEffect(UiBackSound);
                /* Debug.Log($"[广告] 取消观看激励广告，获得基础金币: {_currentBaseGold}"); */
                TryGiveBaseGoldAndExit();
                break;
        }
    }

    private void SetExitButtonState(bool isInteractable, bool isVisible)
    {
        if (_exitButton == null)
        {
            return;
        }

        _exitButton.gameObject.SetActive(isVisible);
        _exitButton.interactable = isInteractable;
    }

    #region 奖励发放核心逻辑
    private void TryGiveBaseGoldAndExit()
    {
        if (_hasGivenReward)
        {
            /* Debug.LogWarning("[奖励] 奖励已发放过，跳过重复操作"); */
            ExitGameSettlement();
            return;
        }

        if (GoldSystem.Instance != null)
        {
            GoldSystem.Instance.AddGold(_currentBaseGold);
            /* Debug.Log($"[奖励] 发放基础金币成功: {_currentBaseGold}"); */
        }
        else
        {
            /* Debug.LogError("[奖励] GoldSystem 不存在，无法发放金币！"); */
        }

        _hasGivenReward = true;
        SetMotivatePanelActive(false);
        ExitGameSettlement();
    }

    public void OnAdRewarded()
    {
        if (_hasGivenReward)
        {
            /* Debug.LogWarning("[奖励] 奖励已发放过，跳过重复操作"); */
            ExitGameSettlement();
            return;
        }

        int multiGold = _currentBaseGold * GoldMulti;

        if (GoldSystem.Instance != null)
        {
            GoldSystem.Instance.AddGold(multiGold);
            /* Debug.Log($"[奖励] 激励广告观看成功，发放3倍金币: {multiGold}"); */
        }
        else
        {
            /* Debug.LogError("[奖励] GoldSystem 不存在，无法发放金币！"); */
        }

        _hasGivenReward = true;
        SetMotivatePanelActive(false);
        ExitGameSettlement();
    }

    public void OnAdFailed()
    {
        /* Debug.LogWarning("[广告] 激励广告加载/观看失败，发放基础金币"); */
        TryGiveBaseGoldAndExit();
    }
    #endregion

    private void ExitGameSettlement()
    {
        /* Debug.Log("退出对局，返回主界面"); */

        AllMapManager.Instance?.TriggerMap(MapType.StartCG, true);
        ModeChooseSystem.instance?.EnterSystem_Quick();

        UImanager.Instance?.HidePanel<WarRecordPanel>(false);
        UImanager.Instance?.HidePanel<GameSettlementPanel>();
        UImanager.Instance.ShowPanel<GameStartPanel>();

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CleanupAndExitGame();
        }

        TriggerDelayedMemoryCleanup();
    }

    private void PlaySettlementResultSound()
    {
        if (Player.LocalPlayer == null || MusicManager.Instance == null)
        {
            return;
        }

        bool isVictory = Player.LocalPlayer.CurrentTeam == WinTeam;
        MusicManager.Instance.PlayEffect(isVictory ? SuccessSound : FailSound, 1f);
    }
    #endregion

    #region 内存回收
    private void TriggerDelayedMemoryCleanup()
    {
        MonoMange.Instance.StartCoroutine(DelayedMemoryCleanupCoroutine());
    }

    private IEnumerator DelayedMemoryCleanupCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ResourcesManager.Instance.UnloadUnusedAssets(() =>
        {
            GC.Collect();
            /* Debug.Log("[GameSettlementPanel] 已完成结算离场后的延迟内存回收"); */
        });
    }
    #endregion

    #region 面板显隐
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

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
    #endregion
}
