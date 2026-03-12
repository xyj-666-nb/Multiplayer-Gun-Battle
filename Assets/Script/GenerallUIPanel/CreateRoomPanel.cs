using System;
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
    public int GameTime;//游戏时间
    public int GameGoalScore;//游戏目标分数

    // 内部标记
    private bool _isButtonGroupRegistered = false;
    private string ScoreChooseName = "GameScoreChoose";
    private string TimeChooseName = "GameTimeChoose";

    public override void Awake()
    {
        base.Awake();

    }

    public override void Start()
    {
        base.Start();

        // 实时记录输入
        if (InputField != null)
            InputField.onValueChanged.AddListener(str => CurrentRoomName = str);
        if (InputField_PlayerName != null)
            InputField_PlayerName.onValueChanged.AddListener(str => CurrentPlayerName = str);

        // 【核心修复】延迟到 Start 安全注册按钮组
        SafeRegisterButtonGroups();
    }

    /// <summary>
    /// 安全注册按钮组
    /// </summary>
    private void SafeRegisterButtonGroups()
    {
        if (_isButtonGroupRegistered) return;

        try
        {
            // 1. 注册分数选择
            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                TryAddRadio(ScoreChooseName, "Button_10Score", () => { GameGoalScore = 10; });
                TryAddRadio(ScoreChooseName, "Button_15Score", () => { GameGoalScore = 15; });
                TryAddRadio(ScoreChooseName, "Button_30Score", () => { GameGoalScore = 30; });
                SafeSelectFirst(ScoreChooseName);
            }

            // 2. 注册时间选择
            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                TryAddRadio(TimeChooseName, "Button_5minute", () => { GameTime = 5; });
                TryAddRadio(TimeChooseName, "Button_10minute", () => { GameTime = 10; });
                TryAddRadio(TimeChooseName, "Button_15minute", () => { GameTime = 15; });
                SafeSelectFirst(TimeChooseName);
            }

            _isButtonGroupRegistered = true;
            Debug.Log("[CreateRoomPanel] 按钮组注册成功");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CreateRoomPanel] 注册按钮组时跳过: {e.Message}");
        }
    }

    /// <summary>
    /// 辅助：安全添加单选按钮
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
    /// 辅助：安全选择第一个
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
                if (Main.Instance.CurrentMode == NetworkMode.LAN)
                {
                    // 局域网逻辑（保持不变，加判空）
                    var roomName = string.IsNullOrWhiteSpace(CurrentRoomName) ? "Room" : CurrentRoomName;
                    var hostName = string.IsNullOrWhiteSpace(CurrentPlayerName) ? "Host" : CurrentPlayerName;
                    Main.PlayerName = hostName;

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
                            if (PlayerRespawnManager.Instance != null)
                                PlayerRespawnManager.Instance.InitGoalScoreCount(GameGoalScore, GameTime);
                        });
                    }

                    if (ModeChooseSystem.instance != null)
                        ModeChooseSystem.instance.ExitSystem();
                }
                else
                {
                    // 远程逻辑（保持不变）
                    RelayForCustomManager relay = FindObjectOfType<RelayForCustomManager>();
                    if (relay == null)
                    {
                        Debug.LogError("场景里没有找到 RelayForCustomManager 组件！");
                        return;
                    }

                    var hostNameRelay = string.IsNullOrWhiteSpace(CurrentPlayerName) ? "Host" : CurrentPlayerName;
                    Main.PlayerName = hostNameRelay;

                    if (Host != null)
                        Host.gameObject.SetActive(false);

                    if (UImanager.Instance != null)
                        UImanager.Instance.HidePanel<CreateRoomPanel>();

                    // 安全初始化
                    void SafeInitGameData()
                    {
                        try
                        {
                            if (PlayerRespawnManager.Instance != null)
                            {
                                PlayerRespawnManager.Instance.InitGoalScoreCount(GameGoalScore, GameTime);
                            }
                        }
                        catch { }
                    }

                    SafeInitGameData();

                    // 关联事件
                    ServerOnlinePanel onlinePanel = UImanager.Instance?.ShowPanel<ServerOnlinePanel>();

                    if (onlinePanel != null)
                    {
                        void UnsubscribeAll()
                        {
                            RelayForCustomManager.OnRelaySuccess -= HandleSuccess;
                            RelayForCustomManager.OnRelayFailed -= HandleFailed;
                        }

                        void HandleSuccess(string code)
                        {
                            Debug.Log($"连接成功！Code: {code}");
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            SafeInitGameData();

                            if (ModeChooseSystem.instance != null)
                                ModeChooseSystem.instance.ExitSystem();
                        }

                        void HandleFailed(string error)
                        {
                            Debug.LogError($"连接失败: {error}");
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                        }

                        void HandleCancel()
                        {
                            Debug.Log("用户点击了取消");
                            relay.CancelRelayConnection();
                            UnsubscribeAll();
                        }

                        RelayForCustomManager.OnRelaySuccess += HandleSuccess;
                        RelayForCustomManager.OnRelayFailed += HandleFailed;
                        onlinePanel.OnCancelAction += HandleCancel;
                    }

                    relay.StartRelayHost();
                }
                break;
            case "ExitButton":
                if (UImanager.Instance != null)
                {
                    UImanager.Instance.ShowPanel<RoomPanel>();
                    UImanager.Instance.HidePanel<CreateRoomPanel>();
                }
                break;
        }
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
            // 如果有 UnRegisterGroup，在这里调用
            ButtonGroupManager.Instance.DestroyRadioGroup(ScoreChooseName);
            ButtonGroupManager.Instance.DestroyRadioGroup(TimeChooseName);
        }
        catch { }
    }

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }
}