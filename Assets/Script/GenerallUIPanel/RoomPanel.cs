using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class RoomPanel : BasePanel
{
    [Header("两个小区域面板")]
    public RectTransform LefRect;
    public RectTransform UpRect;
    [Header("两个面板的Canvas")]
    public CanvasGroup LeftCanvasGroup;
    public CanvasGroup UpCanvasGroup;

    public TextMeshProUGUI TopicText;

    // 动画序列引用
    private Sequence LeftCanvasGroupAnima;
    private Sequence UpCanvasGroupAnima;

    [Header("模式介绍信息")]
    public TextMeshProUGUI Topic;
    public TextMeshProUGUI Content;
    public RectTransform PromptImage;
    public CanvasGroup PromptImageCanvasGroup;
    private Sequence PromptImageAnima;

    [Header("介绍内容LAN")]
    public string Prompt_LAN;
    [TextArea(3, 10)]
    public string Content_LAN;
    [Header("介绍内容Remote")]
    public string Prompt_Remote;
    [TextArea(3, 10)]
    public string Content_Remote;
    [Header("介绍内容Match")] // 匹配模式文本
    public string Prompt_Match;
    [TextArea(3, 10)]
    public string Content_Match;

    /// <summary>
    /// 公共房间固定码（匹配模式统一加入这个房间）
    /// </summary>
    private const string PUBLIC_ROOM_CODE = "PUBLIC_MATCH_ROOM";

    /// <summary>
    /// 只负责提示图片的显隐动画，不负责文本更新
    /// </summary>
    public void TriggerPromptImageAnima(bool IsActive)
    {
        //设置显隐动画
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(PromptImageCanvasGroup, ref PromptImageAnima, IsActive, () => { });

        //位移动画
        float PosY = IsActive ? 0 : -200;
        PromptImage.DOKill();
        PromptImage.DOAnchorPos3DY(PosY, 0.4f).SetEase(IsActive ? Ease.OutBack : Ease.InBack);
    }

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

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(LeftCanvasGroup, ref LeftCanvasGroupAnima, IsActive, () => { }, 0.2f);
        LefRect.DOAnchorPosX(XPos, 0.3f).SetEase(Ease.OutBack).OnComplete(() => { CallBack?.Invoke(); });
    }

    private TypingWritingTask TextTypingTask1;
    private TypingWritingTask TextTypingTask2;

    /// <summary>
    /// 独立的文本更新方法（扩展支持匹配模式）
    /// </summary>
    public void TriggerTextAnima(NetworkMode Mode, bool isMatchMode = false)
    {
        // 停止旧的打字动画
        if (TextTypingTask1 != null) TextTypingTask1.StopTyping();
        if (TextTypingTask2 != null) TextTypingTask2.StopTyping();

        // 匹配模式（基于Remote服务）
        if (isMatchMode)
        {
            TextTypingTask1 = SimpleAnimatorTool.Instance.AddTypingTask(Prompt_Match, Topic);
            TextTypingTask2 = SimpleAnimatorTool.Instance.AddTypingTask(Content_Match, Content, 0.04f);
            return;
        }

        // 原有模式
        if (Mode == NetworkMode.LAN)
        {
            TextTypingTask1 = SimpleAnimatorTool.Instance.AddTypingTask(Prompt_LAN, Topic);
            TextTypingTask2 = SimpleAnimatorTool.Instance.AddTypingTask(Content_LAN, Content, 0.04f);
        }
        else
        {
            TextTypingTask1 = SimpleAnimatorTool.Instance.AddTypingTask(Prompt_Remote, Topic);
            TextTypingTask2 = SimpleAnimatorTool.Instance.AddTypingTask(Content_Remote, Content, 0.04f);
        }
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
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UpCanvasGroup, ref UpCanvasGroupAnima, IsActive, () => { }, 0.25f);
        UpRect.DOAnchorPosY(PosY, 0.4f).SetEase(Ease.OutBack).OnComplete(() => { CallBack?.Invoke(); });
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "Button_CreateRoom":
                // UI选择音效
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                UImanager.Instance.ShowPanel<CreateRoomPanel>();
                UImanager.Instance.HidePanel<RoomPanel>();
                break;

            case "Button_EnterRoom":
                // UI选择音效
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                if (Main.Instance.CurrentMode == NetworkMode.LAN)
                    UImanager.Instance.ShowPanel<EnterRoomPanel>();
                else if (Main.Instance.CurrentMode == NetworkMode.Match)
                    UImanager.Instance.ShowPanel<Match_EnterRoomPanel>();
                else
                    UImanager.Instance.ShowPanel<Remote_EnterRoomPanel>();
                UImanager.Instance.HidePanel<RoomPanel>();
                break;

            case "ExitButton":
                // UI返回音效
                MusicManager.Instance.PlayEffect("Music/update415/ui返回");
                IsActiveUpRect(false, () => {
                    IsActiveLefRect(true, null);
                });
                TriggerPromptImageAnima(false);
                break;

            case "LANModeChoose":
                // UI选择音效
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                Main.Instance.CurrentMode = NetworkMode.LAN;
                TopicText.text = "局域网模式";

                TriggerTextAnima(NetworkMode.LAN);
                TriggerPromptImageAnima(true);

                if (CustomNetworkManager.Instance != null)
                {
                    CustomNetworkManager.Instance.SwitchToLanMode();
                }

                IsActiveLefRect(false, () => { IsActiveUpRect(true, null); });
                break;

            case "RemoteModeChoose":
                // UI选择音效
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                Main.Instance.CurrentMode = NetworkMode.Remote;
                TopicText.text = "远程联机模式";

                TriggerTextAnima(NetworkMode.Remote);
                TriggerPromptImageAnima(true);

                if (CustomNetworkManager.Instance != null)
                {
                    CustomNetworkManager.Instance.SwitchToRelayMode();
                }

                IsActiveLefRect(false, () => { IsActiveUpRect(true, null); });
                break;

            case "ModeChooseExitButton":
                // UI返回音效
                MusicManager.Instance.PlayEffect("Music/update415/ui返回");
                UImanager.Instance.HidePanel<RoomPanel>();
                UImanager.Instance.ShowPanel<GameStartPanel>();
                ModeChooseSystem.instance.EnterSystem();
                break;

            #region 匹配模式完善
            case "MatchModeChoose":
                // UI选择音效
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                // 匹配模式使用Remote(Unity Relay)服务
                Main.Instance.CurrentMode = NetworkMode.Match;
                TopicText.text = "自动匹配模式";

                // 加载匹配模式专属文本
                TriggerTextAnima(NetworkMode.Remote, true);
                TriggerPromptImageAnima(true);

                // 切换为Relay远程服务
                if (CustomNetworkManager.Instance != null)
                {
                    CustomNetworkManager.Instance.SwitchToRelayMode();
                }

                // 统一面板动画
                IsActiveLefRect(false, () => {
                    IsActiveUpRect(true, null);
                });
                break;
                #endregion
        }
    }


    #region 生命周期
    public override void Start()
    {
        base.Start();
        //注册按钮组
        List<Button> ButtonGroup = new List<Button>();
        ButtonGroup.Add(controlDic["Button_CreateRoom"] as Button);
        ButtonGroup.Add(controlDic["Button_EnterRoom"] as Button);
        ButtonGroup.Add(controlDic["ExitButton"] as Button);
        ButtonGroup.Add(controlDic["LANModeChoose"] as Button);
        ButtonGroup.Add(controlDic["RemoteModeChoose"] as Button);
        ButtonGroup.Add(controlDic["ModeChooseExitButton"] as Button);
        // 添加匹配模式按钮到组
        ButtonGroup.Add(controlDic["MatchModeChoose"] as Button);

        SimpleEffectButtonGroup.Instance.RegisterGroup("RoomPanelGroup", ButtonGroup);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("RoomPanelGroup");
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
        TriggerPromptImageAnima(false);
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }
}

// 网络模式枚举（无修改，保持兼容）
public enum NetworkMode
{
    LAN,    // 局域网
    Remote, // 远程服务(Unity Relay)
    Match   // 自动匹配（基于远程服务）
}