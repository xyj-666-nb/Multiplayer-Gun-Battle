using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class GameStartPanel : BasePanel
{
    public CanvasGroup IntroducePanel;
    private Sequence IntroducePanelAnima;
    public RectTransform LeftRect;
    public RectTransform OperateRect;//呼唤选项面板

    bool IsStartPanel=false;
    bool isStartOperatePanel = false;
    public void IsActiveIntroducePanel(bool IsActive)
    {
        IsStartPanel= IsActive;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IntroducePanel,ref IntroducePanelAnima, IsActive, () => { });
        IntroducePanel.interactable = IsActive;
        IntroducePanel.blocksRaycasts = IsActive;
    }


    public void IsActiveOperate(bool IsActive, UnityAction Callback = null)
    {
        isStartOperatePanel= IsActive;
        float PosY = 0;
        if (!IsActive)
            PosY = 1089;
        OperateRect.DOAnchorPosY(PosY, 0.4f).SetEase(Ease.OutBack).OnComplete(() => { Callback?.Invoke(); });
    }


    public void IsActiveLeftRect(bool IsActive,UnityAction Callback=null)
    {
        LeftRect.DOKill();
        float XPos = 0;
        if(!IsActive)
            XPos = -400;
        LeftRect.DOAnchorPosX(XPos, 0.4f).SetEase(Ease.OutBack).OnComplete(() => { Callback?.Invoke(); }); ; 
    }

    #region  生命周期
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    #region 控件处理

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "GameStartButton":
                //进入游戏逻辑
                UImanager.Instance.HidePanel<GameStartPanel>();
                UImanager.Instance.ShowPanel<GameModeChoosePanel>();
                break;
            case "GameExitButton":
                //弹出警告
                WarnTriggerManager.Instance.TriggerDoubleInteraction2Warn("是否确认退出游戏？", null, ()=> { Application.Quit(); } );
                break;
            case "DevelopmentTeamButton":
                IsActiveIntroducePanel(!IsStartPanel);
                break;
            case "PanelExitButton":
                IsActiveIntroducePanel(false);
                break;
            case "OptionButton ":
                IsActiveLeftRect(false, () => { IsActiveOperate(true); });
                break;
            case "GameSettingButton":
                UImanager.Instance.ShowPanel<SettingPanel>();//打开设置面板
                break;
            case "OperateButton":
                UImanager.Instance.ShowPanel<PlayerCustomPanel>();
                break;
            case "ReturnButton":
                IsActiveOperate(false, () => { IsActiveLeftRect(true); });
                break;
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
        IsActiveLeftRect(true);
    }
    #endregion

    #region 特殊动画实现
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
}
