using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameStartPanel : BasePanel
{
    public CanvasGroup IntroducePanel;
    private Sequence IntroducePanelAnima;
    public RectTransform LeftRect;
    public RectTransform OperateRect;//呼唤选项面板

    [Header("两个小面板")]
    public CanvasGroup LeftRectCanvasGroup;
    public CanvasGroup RightRectCanvasGroup;
    private Sequence LeftRectCanvasGroupAnima;
    private Sequence RightRectCanvasGroupAnima;

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
        isStartOperatePanel = IsActive;
        float PosY = 0;
        RightRectCanvasGroup.blocksRaycasts = true;
        if (!IsActive)
        {
            PosY = 700;
            RightRectCanvasGroup.blocksRaycasts = false;
        }
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(RightRectCanvasGroup, ref RightRectCanvasGroupAnima, IsActive, () => { }, 0.25f);
        OperateRect.DOAnchorPosY(PosY, 0.4f).SetEase(Ease.OutBack).OnComplete(() => { Callback?.Invoke(); });
    }


    public void IsActiveLeftRect(bool IsActive,UnityAction Callback=null)
    {
        LeftRect.DOKill();
        float XPos = 0;
        LeftRectCanvasGroup.blocksRaycasts = true;
        if (!IsActive)
        {
            XPos = -200;
            LeftRectCanvasGroup.blocksRaycasts = false;
        }

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(LeftRectCanvasGroup, ref LeftRectCanvasGroupAnima, IsActive, () => { }, 0.2f);
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
        //注册一下按钮组
        List<Button> ButtonGroup = new List<Button>();
        ButtonGroup.Add(controlDic["GameStartButton"] as Button);
        ButtonGroup.Add(controlDic["GameExitButton"] as Button);
        ButtonGroup.Add(controlDic["DevelopmentTeamButton"] as Button);
        ButtonGroup.Add(controlDic["PanelExitButton"] as Button);
        ButtonGroup.Add(controlDic["OptionButton "] as Button);
        ButtonGroup.Add(controlDic["GameSettingButton"] as Button);
        ButtonGroup.Add(controlDic["ReturnButton"] as Button);

        SimpleEffectButtonGroup.Instance.RegisterGroup("GameStartGroup", ButtonGroup);//注册组
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("GameStartGroup");//销毁组
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
                ModeChooseSystem.instance.EnterSystem();
                break;
            case "GameExitButton":
                //弹出警告
                WarnTriggerManager.Instance.TriggerDoubleInteraction2Warn("是否确认退出游戏？", null, ()=> { Application.Quit();PlayerAndGameInfoManger.Instance. SavePlayerData(); } );
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
