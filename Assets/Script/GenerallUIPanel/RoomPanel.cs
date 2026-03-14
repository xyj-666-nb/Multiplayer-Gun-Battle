using Mirror.Transports.Encryption;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Unity.Sync.Relay.Transport.Mirror;

public class RoomPanel : BasePanel
{
    [Header("两个小区域面板")]
    public RectTransform LefRect;
    public RectTransform UpRect;
    [Header("两个面板的Canvas")]
    public CanvasGroup LeftCanvasGroup;
    public CanvasGroup UpCanvasGroup;

    public TextMeshProUGUI TopicText;

    // 动画序列引用，复用自GameStartPanel
    private Sequence LeftCanvasGroupAnima;
    private Sequence UpCanvasGroupAnima;


    public override void Awake()
    {
        base.Awake();
        //本面板打开的时候清除残留面板
        UImanager.Instance.HidePanel<PlayerPanel>();
        UImanager.Instance.HidePanel<PlayerPreparaPanel>();
        IsActiveLefRect(true, null);
    }

    public void IsActiveLefRect(bool IsActive, UnityAction CallBack)
    {
        LefRect.DOKill();
        float XPos = 0;
        LeftCanvasGroup.blocksRaycasts = true;
        if (!IsActive)
        {
            XPos = -100;
            LeftCanvasGroup.blocksRaycasts = false;
        }

        // 复用SimpleAnimatorTool的淡入淡出
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(LeftCanvasGroup, ref LeftCanvasGroupAnima, IsActive, () => { }, 0.2f);
        // 复用DOTween的位移动画
        LefRect.DOAnchorPosX(XPos, 0.4f).SetEase(Ease.OutBack).OnComplete(() => { CallBack?.Invoke(); });
    }

    public void IsActiveUpRect(bool IsActive, UnityAction CallBack)
    {
        UpRect.DOKill();
        float PosY = 0;
        UpCanvasGroup.blocksRaycasts = true;
        if (!IsActive)
        {
            PosY = 400;
            UpCanvasGroup.blocksRaycasts = false;
        }
        // 复用SimpleAnimatorTool的淡入淡出
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UpCanvasGroup, ref UpCanvasGroupAnima, IsActive, () => { }, 0.25f);
        // 复用DOTween的位移动画
        UpRect.DOAnchorPosY(PosY, 0.4f).SetEase(Ease.OutBack).OnComplete(() => { CallBack?.Invoke(); });
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "Button_CreateRoom":
                UImanager.Instance.ShowPanel<CreateRoomPanel>();
                UImanager.Instance.HidePanel<RoomPanel>();
                break;

            case "Button_EnterRoom":
                //根据模式不同选用不同的面板
                if (Main.Instance.CurrentMode == NetworkMode.LAN)
                    UImanager.Instance.ShowPanel<EnterRoomPanel>();//打开局域网加入面板
                else
                    UImanager.Instance.ShowPanel<Remote_EnterRoomPanel>();//打开远程加入面板
                UImanager.Instance.HidePanel<RoomPanel>();
                break;

            case "ExitButton":
                IsActiveUpRect(false, () => { IsActiveLefRect(true, null); });
                break;

            case "LANModeChoose":
                Main.Instance.CurrentMode = NetworkMode.LAN;
                TopicText.text = "局域网模式";

                if (CustomNetworkManager.Instance != null)
                {
                    CustomNetworkManager.Instance.SwitchToLanMode();
                }

                IsActiveLefRect(false, () => { IsActiveUpRect(true, null); });
                break;

            case "RemoteModeChoose":
                Main.Instance.CurrentMode = NetworkMode.Remote;
                TopicText.text = "远程模式 (国内UOS)";

                if (CustomNetworkManager.Instance != null)
                {
                    CustomNetworkManager.Instance.SwitchToRelayMode();
                }

                IsActiveLefRect(false, () => { IsActiveUpRect(true, null); });
                break;

            case "ModeChooseExitButton":
                UImanager.Instance.HidePanel<RoomPanel>();
                UImanager.Instance.ShowPanel<GameStartPanel>();//游戏开始面板
                ModeChooseSystem.instance.EnterSystem();
                break;
        }
    }

    // ... (删除原来的 SwitchTransport 方法，不需要了)

    #region 生命周期
    public override void Start()
    {
        base.Start();
        //注册一下按钮组
        List<Button> ButtonGroup = new List<Button>();
        // 根据你ClickButton里的按钮名，自动添加到组里
        ButtonGroup.Add(controlDic["Button_CreateRoom"] as Button);
        ButtonGroup.Add(controlDic["Button_EnterRoom"] as Button);
        ButtonGroup.Add(controlDic["ExitButton"] as Button);
        ButtonGroup.Add(controlDic["LANModeChoose"] as Button);
        ButtonGroup.Add(controlDic["RemoteModeChoose"] as Button);
        ButtonGroup.Add(controlDic["ModeChooseExitButton"] as Button);

        SimpleEffectButtonGroup.Instance.RegisterGroup("RoomPanelGroup", ButtonGroup);//注册组
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("RoomPanelGroup");//销毁组
    }
    #endregion

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        IsActiveLefRect(true, null);
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

}

public enum NetworkMode
{
    LAN,//局域网
    Remote,//远程服务
}