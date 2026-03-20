using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    [Header("面板动画")]
    public PlayableDirector TimeLineOperate;//选项面板TimeLine
    public List<CanvasGroup> ButtonCanvasGroup;

    [Header("轮盘")]
    public RectTransform WheelImage;//轮盘
    private float Angle_Up = 41;
    private float Angle_Down = -45;

    bool IsStartPanel = false;
    bool isStartOperatePanel = false;

    public void IsActiveIntroducePanel(bool IsActive)
    {
        IsStartPanel = IsActive;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IntroducePanel, ref IntroducePanelAnima, IsActive, () => { });
        IntroducePanel.interactable = IsActive;
        IntroducePanel.blocksRaycasts = IsActive;
    }

    public void IsActiveOperate(bool IsActive, UnityAction Callback = null)
    {
        //播放动画
        if (IsActive)
        {
            TimeLineOperate.time = 0;
            TimeLineOperate.Play();
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(RightRectCanvasGroup, ref RightRectCanvasGroupAnima, IsActive, () => { }, 0.1f);
            CountDownManager.Instance.CreateTimer(false, 1800, () => {
                isStartOperatePanel = IsActive;
                RightRectCanvasGroup.blocksRaycasts = true;
                Callback?.Invoke();
                foreach (CanvasGroup group in ButtonCanvasGroup)
                {
                    group.blocksRaycasts = true;
                }

            });//动画播放完全再触发
        }
        else
        {
            RightRectCanvasGroup.blocksRaycasts = false;
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(RightRectCanvasGroup, ref RightRectCanvasGroupAnima, IsActive, () => { Callback?.Invoke(); }, 0.25f);
        }
        isStartOperatePanel = IsActive;
    }

    public void IsActiveLeftRect(bool IsActive, UnityAction Callback = null)
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
        List<Button> ButtonGroup1 = new List<Button>();
        ButtonGroup.Add(controlDic["GameStartButton"] as Button);
        ButtonGroup.Add(controlDic["GameExitButton"] as Button);
        ButtonGroup.Add(controlDic["DevelopmentTeamButton"] as Button);
        ButtonGroup.Add(controlDic["PanelExitButton"] as Button);
        ButtonGroup1.Add(controlDic["ReturnButton"] as Button);
        ButtonGroup1.Add(controlDic["OperateButton"] as Button);
        ButtonGroup1.Add(controlDic["GameSettingButton"] as Button);
        ButtonGroup.Add(controlDic["OptionButton "] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("GameStartGroup", ButtonGroup, false);//注册组
        SimpleEffectButtonGroup.Instance.RegisterGroup("GameStartGroup1", ButtonGroup1, false, 1.4f, 1.35f, 1.45f);//注册组

        // ================== 绑定按下旋转轮盘的逻辑 ==================
        BindWheelRotateEvent(controlDic["ReturnButton"] as Button, Angle_Down);      // Return → 下 (-45)
        BindWheelRotateEvent(controlDic["OperateButton"] as Button, 0f);             // Operate → 中 (0)
        BindWheelRotateEvent(controlDic["GameSettingButton"] as Button, Angle_Up);    // Setting → 上 (41)
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("GameStartGroup");//销毁组
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("GameStartGroup1");//销毁组

        // 销毁时清理轮盘动画
        if (WheelImage != null)
            WheelImage.DOKill();
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
                WarnTriggerManager.Instance.TriggerDoubleInteraction2Warn("是否确认退出游戏？", null, () => { Application.Quit(); PlayerAndGameInfoManger.Instance.SavePlayerData(); });
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

    #region 轮盘旋转逻辑

    /// <summary>
    /// 给指定按钮绑定按下旋转轮盘的事件
    /// </summary>
    private void BindWheelRotateEvent(Button btn, float targetAngle)
    {
        if (btn == null) return;

        // 动态获取或添加 EventTrigger 来监听按下事件
        EventTrigger trigger = btn.gameObject.GetOrAddComponent<EventTrigger>();

        // 创建按下事件的入口
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;

        // 绑定回调
        entry.callback.AddListener((data) =>
        {
            // 只有按钮可交互时才旋转
            if (btn.IsInteractable())
            {
                RotateWheelTo(targetAngle);
            }
        });

        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 执行轮盘旋转动画 
    /// </summary>
    private void RotateWheelTo(float targetAngle)
    {
        if (WheelImage == null) return;

        // 杀掉之前的旋转动画，防止快速点击导致卡顿
        WheelImage.DOKill();

        // 执行旋转：0.2秒，先快后慢 (Ease.OutQuad)，只转 Z 轴
        WheelImage.DORotate(new Vector3(0, 0, targetAngle), 0.2f)
            .SetEase(Ease.OutQuad) // 先快后慢
            .SetLink(WheelImage.gameObject); // 物体销毁时自动杀死动画
    }

    #endregion
}