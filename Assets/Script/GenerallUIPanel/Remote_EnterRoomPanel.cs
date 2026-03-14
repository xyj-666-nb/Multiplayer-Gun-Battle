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
    public TextMeshProUGUI PromptText;//提升文本

    public override void Awake()
    {
        base.Awake();
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

        if (PlayerNameInputField != null)
        {
        }
    }

    public override void ShowMe(bool IsNeedDefalutAnimator = true)
    {
        base.ShowMe(IsNeedDefalutAnimator);

        // 每次打开面板，清空输入框
        if (joinCodeInputField != null)
            joinCodeInputField.text = "";
        if (statusText != null)
            statusText.text = "请输入房间码";

        if (PlayerNameInputField != null && !string.IsNullOrEmpty(UOSRelaySimple.Instance.playerName))
        {
           // UOSRelaySimple.Instance.SetPlayerData(PlayerNameInputField.text);//设置玩家数据
        }
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        switch (controlName)
        {
            case "JoinButton": // 点击加入按钮

              //  UOSRelaySimple.Instance.SetPlayerData(PlayerNameInputField.text);//设置玩家数据

                TryJoinRelayRoom();
                break;

            case "ExitButton": // 点击返回按钮
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<Remote_EnterRoomPanel>();
                break;
        }
    }

    /// <summary>
    /// 适配 UOSRelaySimple 单例
    /// </summary>
    private void TryJoinRelayRoom()
    {
        if (CustomNetworkManager.Instance != null)
        {
            CustomNetworkManager.Instance.SwitchToRelayMode();
            Debug.Log("[Remote_EnterRoomPanel] 已切换到【Relay模式（UOS）】");
        }

        // 获取输入的 Code，去除首尾空格
        string code = joinCodeInputField != null ? joinCodeInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(code))
        {
            if (statusText != null)
                PromptText.text = "请输入有效的房间码！";
            Debug.LogWarning("Join Code 为空！");
            return;
        }

        // 显示连接中状态
        if (statusText != null)
            statusText.text = "正在连接...";
        (controlDic["JoinButton"] as Button).interactable = false; // 防止重复点击

        // 关联 UOSRelaySimple 的事件
        void UnsubscribeAll()
        {
            UOSRelaySimple.OnRelaySuccess -= OnJoinSuccess;
            UOSRelaySimple.OnRelayFailed -= OnJoinFailed;
        }

        void OnJoinSuccess(string c)
        {
            UnsubscribeAll();
            if (statusText != null)
                statusText.text = "连接成功！";
        }

        void OnJoinFailed(string error)
        {
            UnsubscribeAll();
            if (statusText != null)
                statusText.text = $"连接失败: {error}";
            (controlDic["JoinButton"] as Button).interactable = true; // 恢复按钮
        }

        UOSRelaySimple.OnRelaySuccess += OnJoinSuccess;
        UOSRelaySimple.OnRelayFailed += OnJoinFailed;
        UOSRelaySimple.Instance.StartRelayClient(code);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void SpecialAnimator_Hide()
    {
    }
}