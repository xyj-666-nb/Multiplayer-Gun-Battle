using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Remote_EnterRoomPanel : BasePanel
{
    [Header("远程联机专用")]
    public TMP_InputField joinCodeInputField;
    public TMP_InputField PlayerNameInputField;
    public TMP_Text statusText;
    public TextMeshProUGUI PromptText;//提示文本

    private string currentCleanedCode; // 保存当前清洗后的房间码

    public override void Awake()
    {
        base.Awake();
        List<Button> ButtonGroup = new List<Button>();
        ButtonGroup.Add(controlDic["JoinButton"] as Button);
        ButtonGroup.Add(controlDic["ExitButton"] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("Remote_EnterRoomPanel", ButtonGroup);
    }

    public override void Start()
    {
        base.Start();

        if (joinCodeInputField != null)
        {
            joinCodeInputField.onValueChanged.AddListener((str) =>
            {
                if (statusText != null) statusText.text = "";
            });
        }
    }

    public override void ShowMe(bool IsNeedDefalutAnimator = true)
    {
        base.ShowMe(IsNeedDefalutAnimator);

        if (joinCodeInputField != null)
            joinCodeInputField.text = "";
        if (statusText != null)
            statusText.text = "请输入房间码";
        statusText.color = Color.white;
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        switch (controlName)
        {
            case "JoinButton":
                statusText.color = Color.white;
                TryQueryRoomFirst(); 
                break;

            case "ExitButton":
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<Remote_EnterRoomPanel>();
                break;
        }
    }

    private int CountID = -1;
    private int CountID1 = -1;

    /// <summary>
    ///先查询房间是否存在
    /// </summary>
    private void TryQueryRoomFirst()
    {
        string rawCode = joinCodeInputField != null ? joinCodeInputField.text : "";
        string cleanedCode = rawCode.Trim();
        currentCleanedCode = cleanedCode.ToUpper(); // 保存起来，连接时用

        bool hasError = false;
        string errorMsg = "房间码不合法，请检查输入";

        if (string.IsNullOrEmpty(cleanedCode) ||
            cleanedCode.Length < 4 ||
            cleanedCode.Length > 12 ||
            !System.Text.RegularExpressions.Regex.IsMatch(cleanedCode, @"^[a-zA-Z0-9]+$"))
        {
            hasError = true;
        }

        if (hasError)
        {
            ShowPromptError(errorMsg);
            return;
        }

        if (statusText != null)
        {
            statusText.text = "正在查询房间...";
            statusText.DOKill();
            statusText.DOColor(Color.yellow, 0.2f); // 查询用黄色
        }
        (controlDic["JoinButton"] as Button).interactable = false;

        void UnsubscribeQuery()
        {
            UOSRelaySimple.OnQuerySuccess -= OnQueryRoomSuccess;
            UOSRelaySimple.OnQueryFailed -= OnQueryRoomFailed;
        }

        void OnQueryRoomSuccess(string code)
        {
            UnsubscribeQuery();
            Debug.Log($"【面板】查询成功，开始连接，房间码：{code}");
            StartConnectRelay(code); 
        }

        void OnQueryRoomFailed(string msg)
        {
            UnsubscribeQuery();
            if (statusText != null)
            {
                statusText.text = "未找到房间";
                statusText.DOKill();
                statusText.DOColor(Color.red, 0.2f).OnComplete(() => {
                    CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () => {
                        statusText.DOColor(Color.white, 0.5f);
                        statusText.text = "请输入房间码";
                    });
                });
            }
            (controlDic["JoinButton"] as Button).interactable = true;
            Debug.LogWarning($"【面板】{msg}");
        }

        UOSRelaySimple.OnQuerySuccess += OnQueryRoomSuccess;
        UOSRelaySimple.OnQueryFailed += OnQueryRoomFailed;

        UOSRelaySimple.Instance.QueryRoomOnly(currentCleanedCode);
    }

    /// <summary>
    /// 查询成功后，真正连接 Relay
    /// </summary>
    private void StartConnectRelay(string roomCode)
    {
        if (CustomNetworkManager.Instance != null)
        {
            CustomNetworkManager.Instance.SwitchToRelayMode();
        }

        // 显示“正在连接”状态
        if (statusText != null)
        {
            statusText.text = "正在连接...";
            statusText.DOKill();
            statusText.DOColor(Color.green, 0.2f); // 连接用绿色
        }

        // 订阅连接事件
        void UnsubscribeConnect()
        {
            UOSRelaySimple.OnRelaySuccess -= OnJoinSuccess;
            UOSRelaySimple.OnRelayFailed -= OnJoinFailed;
        }

        void OnJoinSuccess(string c)
        {
            UnsubscribeConnect();
            if (statusText != null)
            {
                statusText.text = "连接成功！";
            }
        }

        void OnJoinFailed(string error)
        {
            UnsubscribeConnect();
            if (statusText != null)
            {
                statusText.text = $"连接失败: {error}";
                statusText.DOKill();
                if (CountID1 != -1)
                {
                    CountDownManager.Instance.StopTimer(CountID1);
                }
                statusText.DOColor(Color.red, 0.2f).OnComplete(() => {
                    CountID1 = CountDownManager.Instance.CreateTimer(false, 1000, () => {
                        statusText.DOColor(Color.white, 0.5f);
                    });
                });
            }
            (controlDic["JoinButton"] as Button).interactable = true;
        }

        UOSRelaySimple.OnRelaySuccess += OnJoinSuccess;
        UOSRelaySimple.OnRelayFailed += OnJoinFailed;
        UOSRelaySimple.Instance.StartRelayClient(roomCode);
    }

    private void ShowPromptError(string msg)
    {
        if (PromptText != null)
        {
            PromptText.gameObject.SetActive(true);
            PromptText.text = msg;
            PromptText.color = Color.red;

            if (CountID != -1)
                CountDownManager.Instance.StopTimer(CountID);

            CountID = CountDownManager.Instance.CreateTimer(false, 1000, () =>
            {
                if (PromptText != null)
                    PromptText.gameObject.SetActive(false);
            });
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("Remote_EnterRoomPanel");
        CountDownManager.Instance.StopTimer(CountID);
        CountDownManager.Instance.StopTimer(CountID1);
    }

    protected override void SpecialAnimator_Show()
    {
        throw new System.NotImplementedException();
    }

    protected override void SpecialAnimator_Hide()
    {
        throw new System.NotImplementedException();
    }
}