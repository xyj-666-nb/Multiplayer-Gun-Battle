using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameModeChoosePanel : BasePanel
{
    public CanvasGroup IntroducePanel;
    private Sequence IntroducePanelSequence;
    private string GameModeChoose= "GameModeChoose";
    private bool IsChooseOnLine= true;
    public void TriggerTimeLine()
    {
        //在这里注册按钮组
        ButtonGroupManager.Instance.AddRadioButtonToGroup(GameModeChoose, controlDic["OnlineGameButton"] as Button, () => { IsChooseOnLine = true;  });
        ButtonGroupManager.Instance.AddRadioButtonToGroup(GameModeChoose, controlDic["SinglePlayerButton"] as Button, () => { IsChooseOnLine = false; });

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(GameModeChoose);
        //显示介绍面板
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IntroducePanel, ref IntroducePanelSequence, true, () => { });
        //为显示面板的按钮注册逻辑
        (controlDic["EnterButton"] as Button).onClick.AddListener(() => {
            if (IsChooseOnLine)
            {
                UImanager.Instance.ShowPanel<RoomPanel>();//打开联机房间
                UImanager.Instance.HidePanel<GameModeChoosePanel>();
            }
            else
                WarnTriggerManager.Instance.TriggerSingleInteractionWarn("敬请期待", "抱歉影响您的体验，作者正在赶工制作", () => { });
        });

    }

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
       
    }
    public override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    #region UI控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName=="ExitButton")
        {
            UImanager.Instance.HidePanel<GameModeChoosePanel>();
            UImanager.Instance.ShowPanel<GameStartPanel>();
        }
    }
    #endregion

    #region 面板显隐以及特殊动画
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
