using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CreateRoomPanel : BasePanel
{
    [SerializeField] private TMP_InputField InputField;           // ???????????
    public static string CurrentRoomName;                         // ??????????

    [SerializeField] private LanRoomHost Host;// ???????????(??????)
    [SerializeField] private TMP_InputField InputField_PlayerName;// ???????????
    public static string CurrentPlayerName;                       // ??????????

    private GameMode currentGameMode = GameMode.Team_Battle;     // ??????????

    [Header("?????????????")]
    public int GameTime;//?????? (5/10/15)
    public int GameGoalScore;//????????? (10/15/30)

    // ??????
    private bool _isButtonGroupRegistered = false;
    private string ScoreChooseName = "CreateButton";
    private string TimeChooseName = "ExitButton";


    public override void Awake()
    {
        base.Awake();
        //?????°??????
        List<Button> ButtonGroup = new List<Button>();
        // ??????ClickButton????????????????????
        ButtonGroup.Add(controlDic["CreateButton"] as Button);
        ButtonGroup.Add(controlDic["ExitButton"] as Button);
        ButtonGroup.Add(controlDic["Button_TeamCompetition"] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("CreateRoomPanel", ButtonGroup);//?????
    }

    public override void Start()
    {
        base.Start();

        // 实时记录输入
        if (InputField != null)
            InputField.onValueChanged.AddListener(HandleRoomNameChanged);
        if (InputField_PlayerName != null)
            InputField_PlayerName.onValueChanged.AddListener(HandlePlayerNameChanged);//获取姓名
        // 安全注册按钮组
        SafeRegisterButtonGroups();
    }

    private void HandleRoomNameChanged(string roomName)
    {
        CurrentRoomName = roomName;
    }

    private void HandlePlayerNameChanged(string playerName)
    {
        if (UOSRelaySimple.Instance != null)
        {
            UOSRelaySimple.Instance.GetPlayerName(playerName);
        }
    }

    #region ????????????λ??????
    /// <summary>
    /// ?? GameTime ??????λ????
    /// 5???? -> 0 (5?????) | 10???? -> 1 | 15???? -> 2
    /// </summary>
    private int GetTimeLevelIndex()
    {
        switch (GameTime)
        {
            case 5: return 0;  // 5?????
            case 10: return 1; // 10?????
            case 15: return 2; // 15?????
            default: return 0;
        }
    }

    /// <summary>
    /// ?? GameGoalScore ??????λ????
    /// 10?? -> 0 | 15?? -> 1 | 30?? -> 2
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
    /// ??????????????+??λ????
    /// </summary>
    private void SafeInitGameDataAndLevel()
    {
        try
        {
            if (PlayerRespawnManager.Instance != null)
            {
                // 1. ?????????????
                PlayerRespawnManager.Instance.InitGoalScoreCount(GameGoalScore, GameTime);

                // 2. ?????????λ
                int timeLevel = GetTimeLevelIndex();
                PlayerRespawnManager.Instance.CmdSetTimeLevel(timeLevel);

                // 3. ???????????λ
                int scoreLevel = GetScoreLimitLevelIndex();
                PlayerRespawnManager.Instance.CmdSetScoreLimitLevel(scoreLevel);

                /* Debug.Log($"[????????] ???????????????:{GameTime}??(??λ{timeLevel}) ???????:{GameGoalScore}??(??λ{scoreLevel})"); */
            }
        }
        catch (System.Exception e)
        {
            /* Debug.LogWarning($"[????????] ??????????????: {e.Message}"); */
        }
    }
    #endregion

    /// <summary>
    /// ?????????
    /// </summary>
    private void SafeRegisterButtonGroups()
    {
        if (_isButtonGroupRegistered)
            return;

        try
        {
            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                TryAddRadio(ScoreChooseName, "Button_10Score", () => { GameGoalScore = 10; MusicManager.Instance.PlayEffect("Music/update415/ui???"); });
                TryAddRadio(ScoreChooseName, "Button_15Score", () => { GameGoalScore = 15; MusicManager.Instance.PlayEffect("Music/update415/ui???"); });
                TryAddRadio(ScoreChooseName, "Button_30Score", () => { GameGoalScore = 30; MusicManager.Instance.PlayEffect("Music/update415/ui???"); });
                SafeSelectFirst(ScoreChooseName);
            }

            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                TryAddRadio(TimeChooseName, "Button_5minute", () => { GameTime = 5; MusicManager.Instance.PlayEffect("Music/update415/ui???"); });
                TryAddRadio(TimeChooseName, "Button_10minute", () => { GameTime = 10; MusicManager.Instance.PlayEffect("Music/update415/ui???"); });
                TryAddRadio(TimeChooseName, "Button_15minute", () => { GameTime = 15; MusicManager.Instance.PlayEffect("Music/update415/ui???"); });
                SafeSelectFirst(TimeChooseName);
            }

            _isButtonGroupRegistered = true;
        }
        catch (System.Exception e)
        {
            /* Debug.LogWarning($"[CreateRoomPanel] ???????????: {e.Message}"); */
        }
    }

    /// <summary>
    /// ???????????
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
    /// ??????????
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
                // UI?????Ч
                MusicManager.Instance.PlayEffect("Music/update415/ui???");

                if (Main.Instance.CurrentMode == NetworkMode.LAN)
                {
                    if (CustomNetworkManager.Instance != null)
                    {
                        CustomNetworkManager.Instance.SwitchToLanMode();
                    }
                    StartCoroutine(CreateLanRoomAfterFrame());
                    MusicManager.Instance.StopBgm();//??????????
                }
                else if (Main.Instance.CurrentMode == NetworkMode.Match)
                {

                    if (CustomNetworkManager.Instance != null)
                    {
                        CustomNetworkManager.Instance.SwitchToRelayMode();
                    }

                    UImanager.Instance.HidePanel<CreateRoomPanel>();

                    SafeInitGameDataAndLevel();

                    // ????????
                    ServerOnlinePanel onlinePanel = UImanager.Instance?.ShowPanel<ServerOnlinePanel>();

                    if (onlinePanel != null)
                    {
                        onlinePanel.TriggerRemoteCheck();
                        void UnsubscribeAll()
                        {
                            UOSRelaySimple.OnRelaySuccess -= HandleSuccess;
                            UOSRelaySimple.OnRelayFailed -= HandleFailed;
                            onlinePanel.OnCancelAction -= HandleCancel;
                        }

                        void HandleSuccess(string code)
                        {
                            /* Debug.Log($"[?????] ????????????Code: {code}"); */
                            MusicManager.Instance.StopBgm();
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            // ????????????????????????
                            SafeInitGameDataAndLevel();

                            if (ModeChooseSystem.instance != null)
                                ModeChooseSystem.instance.ExitSystem();
                        }

                        void HandleFailed(string error)
                        {
                            /* Debug.LogError($"[?????] ???????????: {error}"); */
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            UImanager.Instance.ShowPanel<CreateRoomPanel>();
                        }

                        void HandleCancel()
                        {
                            /* Debug.Log("[?????] ???????????"); */
                            UOSRelaySimple.Instance.StopRelay();
                            UnsubscribeAll();
                            UImanager.Instance.ShowPanel<CreateRoomPanel>();
                        }

                        UOSRelaySimple.OnRelaySuccess += HandleSuccess;
                        UOSRelaySimple.OnRelayFailed += HandleFailed;
                        onlinePanel.OnCancelAction += HandleCancel;
                    }

                    UOSRelaySimple.Instance.StartMatchHost();
                }
                else
                {
                    // ??????????????????????????
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
                            onlinePanel.OnCancelAction -= HandleCancel;
                        }
                        onlinePanel.TriggerRemoteCheck();//???????????

                        void HandleSuccess(string code)
                        {
                            /* Debug.Log($"[?????] ????????????Code: {code}"); */
                            MusicManager.Instance.StopBgm();
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            SafeInitGameDataAndLevel();

                            if (ModeChooseSystem.instance != null)
                                ModeChooseSystem.instance.ExitSystem();
                        }

                        void HandleFailed(string error)
                        {
                            /* Debug.LogError($"[?????] ???????????: {error}"); */
                            UnsubscribeAll();
                            onlinePanel.HidePanel();
                            UImanager.Instance.ShowPanel<CreateRoomPanel>();
                        }

                        void HandleCancel()
                        {
                            /* Debug.Log("[?????] ???????????"); */
                            UOSRelaySimple.Instance.StopRelay();
                            UnsubscribeAll();
                            UImanager.Instance.ShowPanel<CreateRoomPanel>();
                        }

                        UOSRelaySimple.OnRelaySuccess += HandleSuccess;
                        UOSRelaySimple.OnRelayFailed += HandleFailed;
                        onlinePanel.OnCancelAction += HandleCancel;
                    }

                    UOSRelaySimple.Instance.StartRelayHost();
                }
                break;
            case "ExitButton":
                // UI??????Ч
                MusicManager.Instance.PlayEffect("Music/update415/ui????");
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

        if (InputField != null)
            InputField.onValueChanged.RemoveListener(HandleRoomNameChanged);
        if (InputField_PlayerName != null)
            InputField_PlayerName.onValueChanged.RemoveListener(HandlePlayerNameChanged);

        //销毁注册
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("CreateRoomPanel");
    }

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }
}
