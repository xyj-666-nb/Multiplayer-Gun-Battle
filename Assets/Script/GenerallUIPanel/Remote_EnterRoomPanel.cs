using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Remote_EnterRoomPanel : BasePanel
{
    [Header("远程联机专用")]
    public TMP_InputField joinCodeInputField; // 输入 Join Code 的框
    public TMP_InputField PlayerNameInputField; // 玩家名字
    public TMP_Text statusText;               // 显示状态（可选）

    // 缓存 Relay 脚本引用
    private RelayForCustomManager _relayManager;

    public override void Awake()
    {
        base.Awake();
        // 自动查找 Relay 脚本
        _relayManager = FindObjectOfType<RelayForCustomManager>();
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
            PlayerNameInputField.onValueChanged.AddListener((str) =>
            {
                Main.PlayerName = str;
            });

            if (!string.IsNullOrEmpty(Main.PlayerName))
            {
                PlayerNameInputField.text = Main.PlayerName;
            }
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

        if (PlayerNameInputField != null && !string.IsNullOrEmpty(Main.PlayerName))
        {
            PlayerNameInputField.text = Main.PlayerName;
        }
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        switch (controlName)
        {
            case "JoinButton": // 点击加入按钮
                if (string.IsNullOrEmpty(Main.PlayerName) && PlayerNameInputField != null)
                {
                    Main.PlayerName = PlayerNameInputField.text;
                }

                if (string.IsNullOrEmpty(Main.PlayerName))
                {
                    if (statusText != null) statusText.text = "请先输入玩家名字！";
                    return;
                }

                TryJoinRelayRoom();
                break;

            case "ExitButton": // 点击返回按钮
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<Remote_EnterRoomPanel>();
                break;
        }
    }

    /// <summary>
    /// 核心逻辑：尝试加入 Relay 房间
    /// </summary>
    private void TryJoinRelayRoom()
    {
        if (_relayManager == null)
        {
            Debug.LogError("场景里没有找到 RelayForCustomManager！");
            return;
        }

        // 获取输入的 Code，去除首尾空格
        string code = joinCodeInputField != null ? joinCodeInputField.text.Trim() : "";

        if (string.IsNullOrEmpty(code))
        {
            if (statusText != null) statusText.text = "请输入有效的房间码！";
            Debug.LogWarning("Join Code 为空！");
            return;
        }

        // 显示连接中状态
        if (statusText != null)
            statusText.text = "正在连接...";
        (controlDic["JoinButton"] as Button).interactable = false; // 防止重复点击

        // ==========================================
        // 关联事件
        // ==========================================

        void UnsubscribeAll()
        {
            RelayForCustomManager.OnRelaySuccess -= OnJoinSuccess;
            RelayForCustomManager.OnRelayFailed -= OnJoinFailed;
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
                statusText.text = $"连接失败";
            (controlDic["JoinButton"] as Button).interactable = true; // 恢复按钮
        }

        // 订阅事件
        RelayForCustomManager.OnRelaySuccess += OnJoinSuccess;
        RelayForCustomManager.OnRelayFailed += OnJoinFailed;

        // 启动连接！
        _relayManager.StartRelayClient(code);
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