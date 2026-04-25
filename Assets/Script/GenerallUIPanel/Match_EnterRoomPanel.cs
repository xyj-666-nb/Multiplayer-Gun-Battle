using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sync.Relay.Lobby;
using System.Collections.Generic;

public class Match_EnterRoomPanel : BasePanel
{
    [Header("匹配模式专用")]
    public TMP_InputField PlayerNameInputField;
    public TMP_Text statusText;
    public TextMeshProUGUI PromptText;

    private bool _isQuerying = false;
    private int _queryVersion = 0;

    private int CountID = -1;
    private int CountID1 = -1;

    public override void Awake()
    {
        base.Awake();
        List<Button> ButtonGroup = new List<Button>();
        ButtonGroup.Add(controlDic["CheckButton"] as Button);
        ButtonGroup.Add(controlDic["JoinButton"] as Button);
        ButtonGroup.Add(controlDic["ExitButton"] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("Match_EnterRoomPanel", ButtonGroup);
    }

    public override void Start()
    {
        base.Start();
        PlayerNameInputField.onValueChanged.AddListener((str) =>
        {
            UOSRelaySimple.Instance.GetPlayerName(str);
        });
    }

    public override void ShowMe(bool IsNeedDefalutAnimator = true)
    {
        base.ShowMe(IsNeedDefalutAnimator);
        _isQuerying = false;
        _queryVersion++;
        ResetStatusText();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "CheckButton":
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                if (!_isQuerying)
                {
                    StartMatchSearch(false);
                }
                break;
            case "JoinButton":
                MusicManager.Instance.PlayEffect("Music/update415/ui选择");
                if (!_isQuerying)
                {
                    StartMatchSearch(true);
                }
                break;
            case "ExitButton":
                MusicManager.Instance.PlayEffect("Music/update415/ui返回");
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<Match_EnterRoomPanel>();
                break;
        }
    }

    private void StartMatchSearch(bool shouldJoinWhenFound)
    {
        _isQuerying = true;
        _queryVersion++;
        int currentVersion = _queryVersion;
        SetAllButtonsInteractable(false);

        ServerOnlinePanel onlinePanel = UImanager.Instance?.ShowPanel<ServerOnlinePanel>();
        if (onlinePanel != null)
        {
            onlinePanel.TriggerMatchCheck();
            onlinePanel.OnCancelAction = CancelMatchQuery;
        }

        if (statusText != null)
        {
            statusText.text = shouldJoinWhenFound ? "正在匹配公共房间..." : "正在检测公共房间...";
            statusText.DOKill();
            statusText.DOColor(Color.yellow, 0.2f);
        }

        UOSRelaySimple.Instance.QueryBestMatchRoom((success, room, message) =>
        {
            if (this == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (!_isQuerying || currentVersion != _queryVersion)
            {
                return;
            }

            HandleMatchQueryResult(shouldJoinWhenFound, success, room, message);
        });
    }

    private void HandleMatchQueryResult(bool shouldJoinWhenFound, bool success, LobbyRoom room, string message)
    {
        if (shouldJoinWhenFound && success && room != null)
        {
            if (statusText != null)
            {
                statusText.text = $"找到房间，正在连接... ({room.PlayerCount}/{room.MaxPlayers})";
                statusText.DOKill();
                statusText.DOColor(Color.green, 0.2f);
            }
            StartConnectRelay(room);
            return;
        }

        _isQuerying = false;
        SetAllButtonsInteractable(true);
        UImanager.Instance.HidePanel<ServerOnlinePanel>();

        if (success && room != null)
        {
            if (statusText != null)
            {
                statusText.text = $"检测到可加入房间 ({room.PlayerCount}/{room.MaxPlayers})";
                statusText.DOKill();
                statusText.DOColor(Color.green, 0.2f);
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = shouldJoinWhenFound ? "当前没有可加入房间，请先创建房间" : "当前没有可加入房间";
                statusText.DOKill();
                statusText.DOColor(Color.red, 0.2f).OnComplete(() =>
                {
                    CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () =>
                    {
                        ResetStatusText();
                    });
                });
            }

            if (!string.IsNullOrEmpty(message))
            {
                ShowPromptError(message);
            }
        }
    }

    private void CancelMatchQuery()
    {
        _isQuerying = false;
        _queryVersion++;
        UImanager.Instance.HidePanel<ServerOnlinePanel>();
        SetAllButtonsInteractable(true);

        if (statusText != null)
        {
            statusText.DOKill();
            statusText.text = "已取消查找";
            statusText.color = Color.gray;
            CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () =>
            {
                ResetStatusText();
            });
        }

        ServerOnlinePanel onlinePanel = FindObjectOfType<ServerOnlinePanel>();
        if (onlinePanel != null)
        {
            onlinePanel.OnCancelAction = null;
        }
    }

    private void StartConnectRelay(LobbyRoom room)
    {
        _isQuerying = false;

        void UnsubscribeConnect()
        {
            UOSRelaySimple.OnRelaySuccess -= OnJoinSuccess;
            UOSRelaySimple.OnRelayFailed -= OnJoinFailed;
        }

        void OnJoinSuccess(string c)
        {
            UnsubscribeConnect();
            UImanager.Instance.HidePanel<ServerOnlinePanel>();
            UImanager.Instance.HidePanel<Match_EnterRoomPanel>();
            /* Debug.Log("【匹配模式】连接成功！"); */
            SetAllButtonsInteractable(true);
            if (statusText != null)
            {
                statusText.text = "连接成功！";
                statusText.DOKill();
                statusText.DOColor(Color.green, 0.2f);
            }
        }

        void OnJoinFailed(string error)
        {
            UnsubscribeConnect();
            UImanager.Instance.HidePanel<ServerOnlinePanel>();
            SetAllButtonsInteractable(true);
            if (statusText != null)
            {
                statusText.text = $"连接失败: {error}";
                statusText.DOKill();
                if (CountID1 != -1) CountDownManager.Instance.StopTimer(CountID1);
                statusText.DOColor(Color.red, 0.2f).OnComplete(() =>
                {
                    CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () =>
                    {
                        ResetStatusText();
                    });
                });
            }
        }

        UOSRelaySimple.OnRelaySuccess += OnJoinSuccess;
        UOSRelaySimple.OnRelayFailed += OnJoinFailed;
        UOSRelaySimple.Instance.JoinMatchRoom(room);
    }

    private void ResetStatusText()
    {
        if (statusText != null)
        {
            statusText.text = "点击检测查找公共房间";
            statusText.color = Color.white;
            statusText.DOKill();
        }
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (controlDic != null)
        {
            if (controlDic.ContainsKey("CheckButton") && controlDic["CheckButton"] is Button btnCheck)
                btnCheck.interactable = interactable;
            if (controlDic.ContainsKey("JoinButton") && controlDic["JoinButton"] is Button btnJoin)
                btnJoin.interactable = interactable;
            if (controlDic.ContainsKey("ExitButton") && controlDic["ExitButton"] is Button btnExit)
                btnExit.interactable = interactable;
        }
    }

    private void ShowPromptError(string msg)
    {
        if (PromptText != null)
        {
            PromptText.gameObject.SetActive(true);
            PromptText.text = msg;
            PromptText.color = Color.red;
            if (CountID != -1) CountDownManager.Instance.StopTimer(CountID);
            CountID = CountDownManager.Instance.CreateTimer(false, 1000, () =>
            {
                if (PromptText != null) PromptText.gameObject.SetActive(false);
            });
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("Match_EnterRoomPanel");
        CountDownManager.Instance.StopTimer(CountID);
        CountDownManager.Instance.StopTimer(CountID1);
        _isQuerying = false;
        _queryVersion++;
    }

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }
}