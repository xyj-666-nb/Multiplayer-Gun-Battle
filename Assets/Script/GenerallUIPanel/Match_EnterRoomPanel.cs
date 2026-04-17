using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Sync.Relay.Model;
using Unity.Sync.Relay.Lobby;

public class Match_EnterRoomPanel : BasePanel
{
    [Header("匹配模式专用")]
    public TMP_InputField PlayerNameInputField;
    public TMP_Text statusText;
    public TextMeshProUGUI PromptText;

    private const string MATCH_ROOM_NAME_PREFIX = "PUBLIC_MATCH_";
    private const int MATCH_ROOM_COUNT = 1000;
    private int _currentQueryIndex = 0;
    private bool _isQuerying = false;
    // 新增：保存查询协程，用于取消时停止
    private Coroutine _queryCoroutine;

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
        _currentQueryIndex = 0;
        _isQuerying = false;
        if (statusText != null)
        {
            statusText.text = "点击检测查找公共房间";
            statusText.color = Color.white;
        }
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "CheckButton":
                if (!_isQuerying)
                {
                    _currentQueryIndex = 0;
                    StartQueryMatchRooms();
                }
                break;
            case "JoinButton":
                if (!_isQuerying)
                {
                    _currentQueryIndex = 0;
                    StartQueryMatchRooms();
                }
                break;
            case "ExitButton":
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<Match_EnterRoomPanel>();
                break;
        }
    }

    private void StartQueryMatchRooms()
    {
        _isQuerying = true;
        SetAllButtonsInteractable(false);

        // 显示加载面板，并绑定取消事件
        ServerOnlinePanel onlinePanel = UImanager.Instance?.ShowPanel<ServerOnlinePanel>();
        if (onlinePanel != null)
        {
            onlinePanel.TriggerMatchCheck();
            onlinePanel.OnCancelAction = CancelMatchQuery;
        }

        if (statusText != null)
        {
            statusText.text = $"正在检测公共房间... ({_currentQueryIndex + 1}/{MATCH_ROOM_COUNT})";
            statusText.DOKill();
            statusText.DOColor(Color.yellow, 0.2f);
        }
        // 保存协程
        _queryCoroutine = StartCoroutine(QueryMatchRoomsCoroutine());
    }

    private void CancelMatchQuery()
    {
        // 停止查询协程
        if (_queryCoroutine != null)
        {
            StopCoroutine(_queryCoroutine);
            _queryCoroutine = null;
        }

        // 隐藏加载面板
        UImanager.Instance.HidePanel<ServerOnlinePanel>();

        // 重置所有状态
        _isQuerying = false;
        _currentQueryIndex = 0;
        SetAllButtonsInteractable(true);

        // 恢复文本显示
        if (statusText != null)
        {
            statusText.DOKill();
            statusText.text = "已取消查找";
            statusText.color = Color.gray;
            // 1秒后恢复默认文本
            CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () =>
            {
                statusText.color = Color.white;
                statusText.text = "点击检测查找公共房间";
            });
        }

        // 清理事件绑定
        ServerOnlinePanel onlinePanel = FindObjectOfType<ServerOnlinePanel>();
        if (onlinePanel != null)
        {
            onlinePanel.OnCancelAction = null;
        }
    }

    private IEnumerator QueryMatchRoomsCoroutine()
    {
        while (_currentQueryIndex < MATCH_ROOM_COUNT)
        {
            if (statusText != null)
            {
                statusText.text = $"正在检测公共房间... ({_currentQueryIndex + 1}/{MATCH_ROOM_COUNT})";
            }

            bool queryFinished = false;
            bool roomFound = false;
            LobbyRoom foundRoom = null;

            UOSRelaySimple.Instance.QueryMatchRoom(_currentQueryIndex, (success, room) =>
            {
                roomFound = success;
                foundRoom = room;
                queryFinished = true;
            });

            yield return new WaitUntil(() => queryFinished);

            if (roomFound)
            {
                Debug.Log($"【匹配模式】找到可用房间：{foundRoom.Name}");
                StartConnectRelay(foundRoom);
                yield break;
            }

            _currentQueryIndex++;
            yield return null;
        }

        // 未找到房间，隐藏面板
        UImanager.Instance.HidePanel<ServerOnlinePanel>();
        _isQuerying = false;
        _queryCoroutine = null;
        SetAllButtonsInteractable(true);

        if (statusText != null)
        {
            statusText.text = "未找到可用公共房间";
            statusText.DOKill();
            statusText.DOColor(Color.red, 0.2f).OnComplete(() =>
            {
                CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () =>
                {
                    statusText.DOColor(Color.white, 0.5f);
                    statusText.text = "点击检测查找公共房间";
                });
            });
        }
    }

    private void StartConnectRelay(LobbyRoom room)
    {
        _isQuerying = false;
        _queryCoroutine = null;

        if (statusText != null)
        {
            statusText.text = "正在连接...";
            statusText.DOKill();
            statusText.DOColor(Color.green, 0.2f);
        }

        void UnsubscribeConnect()
        {
            UOSRelaySimple.OnRelaySuccess -= OnJoinSuccess;
            UOSRelaySimple.OnRelayFailed -= OnJoinFailed;
        }

        void OnJoinSuccess(string c)
        {
            UnsubscribeConnect();
            UImanager.Instance.HidePanel<ServerOnlinePanel>();
            Debug.Log("【匹配模式】连接成功！");
            SetAllButtonsInteractable(true);
            if (statusText != null) statusText.text = "连接成功！";
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
                        statusText.DOColor(Color.white, 0.5f);
                        statusText.text = "点击检测查找公共房间";
                    });
                });
            }
        }

        UOSRelaySimple.OnRelaySuccess += OnJoinSuccess;
        UOSRelaySimple.OnRelayFailed += OnJoinFailed;
        UOSRelaySimple.Instance.JoinMatchRoom(room);
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
        // 销毁时清理协程
        if (_queryCoroutine != null) StopCoroutine(_queryCoroutine);
    }

    protected override void SpecialAnimator_Show() { }
    protected override void SpecialAnimator_Hide() { }
}