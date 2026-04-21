using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CreateRoomPanel : BasePanel
{
    [SerializeField] private TMP_InputField InputField;           // 房间名输入框
    public static string CurrentRoomName;                         // 当前的房间名

    [SerializeField] private LanRoomHost Host;// 房主组件引用(也是广播器)
    [SerializeField] private TMP_InputField InputField_PlayerName;// 房主名输入框
    public static string CurrentPlayerName;                       // 当前的房主名

    private GameMode currentGameMode = GameMode.Team_Battle;     // 当前的游戏模式

    [Header("房间设置的数据")]
    public int GameTime;//游戏时间 (5/10/15)
    public int GameGoalScore;//游戏目标分数 (10/15/30)

    // 内部标记
    private bool _isButtonGroupRegistered = false;
    private string ScoreChooseName = "CreateButton";
    private string TimeChooseName = "ExitButton";


    public override void Awake()
    {
        base.Awake();
        //注册一下按钮动画
        List<Button> ButtonGroup = new List<Button>();
        // 根据你ClickButton里的按钮名，自动添加到组里
        ButtonGroup.Add(controlDic["CreateButton"] as Button);
        ButtonGroup.Add(controlDic["ExitButton"] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("CreateRoomPanel", ButtonGroup);//注册组
    }

    public override void Start()
    {
        base.Start();

        // 实时记录输入
        if (InputField != null)
            InputField.onValueChanged.AddListener(str => CurrentRoomName = str);
        if (InputField_PlayerName != null)
            InputField_PlayerName.onValueChanged.AddListener(
                str => UOSRelaySimple.Instance.GetPlayerName(str));//获取姓名
        // 安全注册按钮组
        SafeRegisterButtonGroups();
    }

    #region 核心新增：档位映射逻辑
    /// <summary>
    /// 将 GameTime 转换为档位索引
    /// 5分钟 -> 0 (5分钟档) | 10分钟 -> 1 | 15分钟 -> 2
    /// </summary>
    private int GetTimeLevelIndex()
    {
        switch (GameTime)
        {
            case 5: return 0;  // 5分钟档
            case 10: return 1; // 10分钟档
            case 15: return 2; // 15分钟档
            default: return 0;
        }
    }

    /// <summary>
    /// 将 GameGoalScore 转换为档位索引
    /// 10分 -> 0 | 15分 -> 1 | 30分 -> 2
    /// </summary>
    private int GetScoreLimitLevelIndex()
    {
        switch (GameGoalScore)
        {
            case 10: return 0;
            case 15: return 1;
            case 30: return 2;
            default: return 0;
        }
    }

    /// <summary>
    /// 统一初始化游戏数据+档位设置
    /// </summary>
    private void SafeInitGameDataAndLevel()
    {
        try
        {
            if (PlayerRespawnManager.Instance != null)
            {
                // 1. 初始化基础数据
                PlayerRespawnManager.Instance.InitGoalScoreCount(GameGoalScore, GameTime);

                // 2. 设置时长档位
                int timeLevel = GetTimeLevelIndex();
                PlayerRespawnManager.Instance.CmdSetTimeLevel(timeLevel);

                // 3. 设置比分上限档位
                int scoreLevel = GetScoreLimitLevelIndex();
                PlayerRespawnManager.Instance.CmdSetScoreLimitLevel(scoreLevel);

                Debug.Log($"[房间设置] 数据初始化完成！时长:{GameTime}分(档位{timeLevel}) 比分上限:{GameGoalScore}分(档位{scoreLevel})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[房间设置] 初始化数据时跳过: {e.Message}");
        }
    }
    #endregion

    /// <summary>
    /// 安全注册按钮组
    /// </summary>
    private void SafeRegisterButtonGroups()
    {
        if (_isButtonGroupRegistered)
            return;

        try
        {
            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                TryAddRadio(ScoreChooseName, "Button_10Score", () => { GameGoalScore = 10; MusicManager.Instance.PlayEffect("Music/update415/ui选择"); });
                TryAddRadio(ScoreChooseName, "Button_15Score", () => { GameGoalScore = 15; MusicManager.Instance.PlayEffect("Music/update415/ui选择"); });
                TryAddRadio(ScoreChooseName, "Button_30Score", () => { GameGoalScore = 30; MusicManager.Instance.PlayEffect("Music/update415/ui选择"); });
                SafeSelectFirst(ScoreChooseName);
            }

            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                TryAddRadio(TimeChooseName, "Button_5minute", () => { GameTime = 5; MusicManager.Instance.PlayEffect("Music/update415/ui选择"); });
                TryAddRadio(TimeChooseName, "Button_10minute", () => { GameTime = 10; MusicManager.Instance.PlayEffect("Music/update415/ui选择"); });
                TryAddRadio(TimeChooseName, "Button_15minute", () => { GameTime = 15; MusicManager.Instance.PlayEffect("Music/update415/ui选择"); });
                SafeSelectFirst(TimeChooseName);
            }

            _isButtonGroupRegistered = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CreateRoomPanel] 注册按钮组时跳过: {e.Message}");
        }
    }

    /// <summary>
    /// 安全添加单选按钮
    /// </summary>
    private void TryAddRadio(string groupName, string controlName, UnityAction call)
    {
        if (controlDic == null || !controlDic.ContainsKey(controlName)) return;

        Button btn = controlDic[controlName] as Button;
        if (btn != null && ButtonGroupManager.Instance != null)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(groupName, btn, call);
        }
    }

    /// <summary>
    /// 安全选择第一个
    /// </summary>
    private void SafeSelectFirst(string groupName)
    {
        try
        {
            if (ButtonGroupManager.Instance != null)
            {
                ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(groupName);
            }
        }
        catch { }
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        switch (controlName)
        {
            case "CreateButton":
                // UI选择音效
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");

                if (Main.Instance.CurrentMode == NetworkMode.LAN)
                {
                    if (CustomNetworkManager.Instance != null)
                    {
                        CustomNetworkManager.Instance.SwitchToLanMode();
                    }
                    StartCoroutine(CreateLanRoomAfterFrame());
                    MusicManager.Instance.StopBgm();//停止背景音乐
                }
                else if (Main.Instance.CurrentMode == NetworkMode.Match)
                {

                    if (CustomNetworkManager.Instance != null)
                    {
                        CustomNetworkManager.Instance.SwitchToRelayMode();
                    }

                    UImanager.Instance.HidePanel<CreateRoomPanel>();

                    SafeInitGameDataAndLevel();

                    // 展示等待面板
                    ServerOnlinePanel onlinePanel = UImanager.Instance?.ShowPanel<ServerOnlinePanel>();
                    onlinePanel.TriggerRemoteCheck();
                    onlinePanel.TriggerRemoteCheck();//触发远程检测动画

                    if (onlinePanel != null)
                    {
                        void UnsubscribeAll()
                        {
                            UOSRelaySimple.OnRelaySuccess -= HandleSuccess;
                            UOSRelaySimple.OnRelayFailed -= HandleFailed;
                        }

                        void HandleSuccess(string code)
                        {
                            Debug.Log($"[匹配模式] 创建房间成功！Code: {code}");
                            MusicManager.Instance.StopBgm();
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            // 【修改】成功后再次确保数据同步
                            SafeInitGameDataAndLevel();

                            if (ModeChooseSystem.instance != null)
                                ModeChooseSystem.instance.ExitSystem();
                        }

                        void HandleFailed(string error)
                        {
                            Debug.LogError($"[匹配模式] 创建房间失败: {error}");
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                        }

                        void HandleCancel()
                        {
                            Debug.Log("[匹配模式] 用户点击了取消");
                            UOSRelaySimple.Instance.StopRelay();
                            UnsubscribeAll();
                        }

                        UOSRelaySimple.OnRelaySuccess += HandleSuccess;
                        UOSRelaySimple.OnRelayFailed += HandleFailed;
                        onlinePanel.OnCancelAction += HandleCancel;
                    }

                    UOSRelaySimple.Instance.StartMatchHost();
                }
                else
                {
                    // 远程手动模式：独立创建公开房间
                    if (CustomNetworkManager.Instance != null)
                    {
                        CustomNetworkManager.Instance.SwitchToRelayMode();
                    }

                    UImanager.Instance.HidePanel<CreateRoomPanel>();

                    SafeInitGameDataAndLevel();

                    ServerOnlinePanel onlinePanel = UImanager.Instance?.ShowPanel<ServerOnlinePanel>();

                    if (onlinePanel != null)
                    {
                        void UnsubscribeAll()
                        {
                            UOSRelaySimple.OnRelaySuccess -= HandleSuccess;
                            UOSRelaySimple.OnRelayFailed -= HandleFailed;
                        }
                        onlinePanel.TriggerRemoteCheck();//触发远程检测动画

                        void HandleSuccess(string code)
                        {
                            Debug.Log($"[远程模式] 创建房间成功！Code: {code}");
                            MusicManager.Instance.StopBgm();
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            SafeInitGameDataAndLevel();

                            if (ModeChooseSystem.instance != null)
                                ModeChooseSystem.instance.ExitSystem();
                        }

                        void HandleFailed(string error)
                        {
                            Debug.LogError($"[远程模式] 创建房间失败: {error}");
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                        }

                        void HandleCancel()
                        {
                            Debug.Log("[远程模式] 用户点击了取消");
                            UOSRelaySimple.Instance.StopRelay();
                            UnsubscribeAll();
                        }

                        UOSRelaySimple.OnRelaySuccess += HandleSuccess;
                        UOSRelaySimple.OnRelayFailed += HandleFailed;
                        onlinePanel.OnCancelAction += HandleCancel;
                    }

                    UOSRelaySimple.Instance.StartRelayHost();
                }
                break;
            case "ExitButton":
                // UI返回音效
                MusicManager.Instance.PlayEffect("Music/update415/ui返回");
                if (UImanager.Instance != null)
                {
                    UImanager.Instance.ShowPanel<RoomPanel>();
                    UImanager.Instance.HidePanel<CreateRoomPanel>();
                }
                break;
        }
    }

    private IEnumerator CreateLanRoomAfterFrame()
    {
        yield return null;
        yield return null;

        var roomName = string.IsNullOrWhiteSpace(CurrentRoomName) ? "Room" : CurrentRoomName;
        var hostName = string.IsNullOrWhiteSpace(CurrentPlayerName) ? "Host" : CurrentPlayerName;

        if (Host != null)
        {
            Host.CreateRoom(roomName, hostName, GameTime, GameGoalScore);
            ReserveHost();
        }

        if (UImanager.Instance != null)
            UImanager.Instance.HidePanel<CreateRoomPanel>();

        if (CountDownManager.Instance != null)
        {
            CountDownManager.Instance.CreateTimer(false, 1000, () => {
                SafeInitGameDataAndLevel();
            });
        }

        if (ModeChooseSystem.instance != null)
            ModeChooseSystem.instance.ExitSystem();
    }

    public void ReserveHost()
    {
        if (Host != null && CustomNetworkManager.Instance != null)
        {
            Host.gameObject.transform.parent = null;
            CustomNetworkManager.Instance.BroadcasterObj = Host.gameObject;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _isButtonGroupRegistered = false;

        try
        {
            ButtonGroupManager.Instance.DestroyRadioGroup(ScoreChooseName);
            ButtonGroupManager.Instance.DestroyRadioGroup(TimeChooseName);
        }
        catch { }

        //销毁注册
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("CreateRoomPanel");
    }

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }
}