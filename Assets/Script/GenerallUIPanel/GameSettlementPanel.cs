using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GameSettlementPanel : BasePanel
{
    [Header("基础结算UI")]
    public Image RedImage;
    public Image BlueImage;
    public TextMeshProUGUI WinText;//胜利宣判文本
    public TextMeshProUGUI RedScore;
    public TextMeshProUGUI BlueScore;
    public PlayableDirector TimeLine;//时间线

    [Header("金币显示")]
    public TextMeshProUGUI GoldNumber;//基础金币数量文本
    private int _currentBaseGold = 0;//缓存本局基础金币
    private bool _hasGivenReward = false; // 防止重复发放奖励的标记

    [Header("激励广告面板")]
    public CanvasGroup MotivatePanelCanvasGroup;//广告激励面板
    private Sequence MotivatePanelSequence;//动画序列
    private const int GoldMulti = 3;//金币倍数
    public TextMeshProUGUI goldMultiText;//翻倍后金币文本
    [Header("广告弹出配置")]
    [Range(0, 100)] public int AdPopupChance = 60;//广告弹出概率（默认60%）

    public Team WinTeam;

    #region 核心数据设置
    /// <summary>
    /// 接收服务端下发的金币数据，初始化显示
    /// </summary>
    public void SetGoldData(int baseGold)
    {
        _currentBaseGold = baseGold;
        _hasGivenReward = false; // 重置奖励发放标记
        GoldNumber.text = baseGold.ToString();
        goldMultiText.text = (baseGold * GoldMulti).ToString();
    }
    #endregion

    #region Timeline事件
    // Timeline触发：播放胜利方动画、初始化比分
    public void TimeLineTrigger()
    {
        RectTransform winRect;
        // 初始化胜负文本和比分
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

        // 同步最终比分
        if (PlayerRespawnManager.Instance != null)
        {
            RedScore.text = PlayerRespawnManager.Instance.RedTeamScoreCount.ToString();
            BlueScore.text = PlayerRespawnManager.Instance.BlueTeamScoreCount.ToString();
        }

        // 胜利方缩放动画
        winRect
            .DOScale(Vector3.one * 1.2f, 1f)
            .SetEase(Ease.OutQuad)
            .SetLink(winRect.gameObject);
    }

    // Timeline结束：打开战绩面板、触发广告概率判定
    public void TimeLineEnd()
    {
        TimeLine.Pause();
        // 打开战绩面板
        UImanager.Instance.ShowPanel<WarRecordPanel>();
        controlDic["ExitButton"].gameObject.SetActive(true);

        // 60%概率弹出广告激励面板
        TryTriggerAdPanel();
    }
    #endregion

    #region 广告面板逻辑
    /// <summary>
    /// 按概率触发广告面板弹出
    /// </summary>
    private void TryTriggerAdPanel()
    {
        int randomValue = UnityEngine.Random.Range(0, 100);
        Debug.Log($"[广告逻辑] 随机值:{randomValue}, 触发概率:{AdPopupChance}%");

        // 命中概率则弹出面板
        if (randomValue < AdPopupChance)
        {
            SetMotivatePanelActive(true);
        }
    }

    /// <summary>
    /// 控制激励面板显隐
    /// </summary>
    public void SetMotivatePanelActive(bool isActive)
    {
        MotivatePanelCanvasGroup.interactable = isActive;
        MotivatePanelCanvasGroup.blocksRaycasts = isActive;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(MotivatePanelCanvasGroup, ref MotivatePanelSequence, isActive, () => { });
    }
    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        // 初始化隐藏激励面板
        if (MotivatePanelCanvasGroup != null)
        {
            MotivatePanelCanvasGroup.alpha = 0;
            MotivatePanelCanvasGroup.interactable = false;
            MotivatePanelCanvasGroup.blocksRaycasts = false;
        }
    }
    #endregion

    #region UI按钮点击
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "ExitButton":
                // 直接退出：给基础金币
                TryGiveBaseGoldAndExit();
                break;
            case "ConfirmButton":
                Debug.Log($"[广告] 请求观看激励广告，预期奖励: {_currentBaseGold * GoldMulti}");

                TapAdManager.Instance.ShowRewardAd(
                    onRewarded: OnAdRewarded,
                    onFailed: OnAdFailed
                );

                break;
            case "CancelButton":
                // 取消广告：给基础金币并退出
                Debug.Log($"[广告] 取消观看激励广告，获得基础金币: {_currentBaseGold}");
                TryGiveBaseGoldAndExit();
                break;
        }
    }

    #region 奖励发放核心逻辑
    /// <summary>
    /// 尝试发放基础金币并退出（防止重复发放）
    /// </summary>
    private void TryGiveBaseGoldAndExit()
    {
        if (_hasGivenReward)
        {
            Debug.LogWarning("[奖励] 奖励已发放过，跳过重复操作");
            ExitGameSettlement();
            return;
        }

        // 发放基础金币
        if (GoldSystem.Instance != null)
        {
            GoldSystem.Instance.AddGold(_currentBaseGold);
            Debug.Log($"[奖励] 发放基础金币成功: {_currentBaseGold}");
        }
        else
        {
            Debug.LogError("[奖励] GoldSystem 不存在，无法发放金币！");
        }

        _hasGivenReward = true;
        SetMotivatePanelActive(false); // 确保关闭广告面板
        ExitGameSettlement();
    }

    /// <summary>
    /// 广告观看成功回调：发放3倍金币并退出
    /// </summary>
    public void OnAdRewarded()
    {
        if (_hasGivenReward)
        {
            Debug.LogWarning("[奖励] 奖励已发放过，跳过重复操作");
            ExitGameSettlement();
            return;
        }

        int multiGold = _currentBaseGold * GoldMulti;

        // 发放3倍金币
        if (GoldSystem.Instance != null)
        {
            GoldSystem.Instance.AddGold(multiGold);
            Debug.Log($"[奖励] 激励广告观看成功，发放3倍金币: {multiGold}");
        }
        else
        {
            Debug.LogError("[奖励] GoldSystem 不存在，无法发放金币！");
        }

        _hasGivenReward = true;
        SetMotivatePanelActive(false);
        ExitGameSettlement();
    }

    /// <summary>
    /// 广告观看失败回调：给基础金币或提示
    /// </summary>
    public void OnAdFailed()
    {
        Debug.LogWarning("[广告] 激励广告加载/观看失败，发放基础金币");
        // 广告失败也给基础金币，保证玩家体验
        TryGiveBaseGoldAndExit();
    }
    #endregion

    /// <summary>
    /// 结算退出逻辑
    /// </summary>
    private void ExitGameSettlement()
    {
        Debug.Log("退出对局，返回主界面");

        AllMapManager.Instance?.TriggerMap(MapType.StartCG, true);
        ModeChooseSystem.instance?.EnterSystem_Quick();

        UImanager.Instance?.HidePanel<GameSettlementPanel>();
        UImanager.Instance.ShowPanel<GameStartPanel>();

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CleanupAndExitGame();
            GC.Collect();
            Resources.UnloadUnusedAssets();
        }
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