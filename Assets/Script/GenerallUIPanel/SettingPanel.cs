using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    #region 面板控件字段
    public TextMeshProUGUI CurrentTopic;
    public RectTransform PanelParentObj;

    private TypingWritingTask _currentTypingWritingTask;
    private bool _isPanelInitialized = false;
    private readonly string _totalGroupName = "Setting_TotalGroup";
    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();
        if (!_isPanelInitialized)
        {
            RegisterAllButtonsToSingleGroup();
            _isPanelInitialized = true;
        }
    }
    #endregion

    #region 单选按钮注册
    private void RegisterAllButtonsToSingleGroup()
    {
        AddButtonToGroup(_totalGroupName, "Button_ChangeKey", ChooseButton_ChangeKey, CancelButton_ChangeKey);
        AddButtonToGroup(_totalGroupName, "Button_MusicSetting", ChooseButton_MusicSetting, CancelButton_MusicSetting);
        AddButtonToGroup(_totalGroupName, "Button_PictureSetting", ChooseButton_PictureSetting, CancelButton_PictureSetting);
         AddButtonToGroup(_totalGroupName, "Button_Language", ChooseButton_Language, CancelButton_Language);

    }

    private void AddButtonToGroup(string groupName, string buttonName, UnityAction chooseEvent, UnityAction cancelEvent, float scale = 1.1f, float duration = 0.2f)
    {
        if (!controlDic.ContainsKey(buttonName))
        {
            Debug.LogError($"设置面板控件字典中无按钮：{buttonName}");
            return;
        }
        if (controlDic[buttonName] == null)
        {
            Debug.LogError($"设置面板中按钮 {buttonName} 为空");
            return;
        }
        if (controlDic[buttonName] is Button button)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(groupName, button, chooseEvent, cancelEvent, scale, duration);
        }
        else
        {
            Debug.LogError($"设置面板中 {buttonName} 不是Button组件");
        }
    }
    #endregion

    #region 按钮选中/取消事件
    public void ChooseButton_ChangeKey()
    {
        _currentTypingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask("操作设置", CurrentTopic);
        if (PanelParentObj != null )
        {
            var changeKeyPanel = UImanager.Instance.ShowPanel<MoveSettingPanel>();
            if (changeKeyPanel != null )
            {
                changeKeyPanel.transform.SetParent(PanelParentObj);
                // 重置面板的 Left 和 Top 偏移
                ResetPanelOffset(changeKeyPanel.GetComponent<RectTransform>());
            }
            else
            {
                Debug.LogError("ChangeKeyPanel 面板为空或已销毁");
            }
        }
    }

    public void CancelButton_ChangeKey()
    {
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }

        try
        {
            UImanager.Instance.HidePanel<MoveSettingPanel>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"关闭ChangeKeyPanel失败：{e.Message}");
        }
    }

    public void ChooseButton_MusicSetting()
    {
        _currentTypingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask("音乐设置", CurrentTopic);
        if (PanelParentObj != null )
        {
            var musicPanel = UImanager.Instance.ShowPanel<MusicPanel>();
            if (musicPanel != null )
            {
                musicPanel.transform.SetParent(PanelParentObj);
                // 重置面板的 Left 和 Top 偏移
                ResetPanelOffset(musicPanel.GetComponent<RectTransform>());
            }
            else
            {
                Debug.LogError("MusicPanel 面板为空或已销毁");
            }
        }
    }

    public void CancelButton_MusicSetting()
    {
        try
        {
            UImanager.Instance.HidePanel<MusicPanel>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"关闭MusicPanel失败：{e.Message}");
        }
    }

    public void ChooseButton_PictureSetting()
    {
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }
        _currentTypingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask("画面设置", CurrentTopic);

        var musicPanel = UImanager.Instance.ShowPanel<ScreenSettingPanel>();
        if (musicPanel != null)
        {
            musicPanel.transform.SetParent(PanelParentObj);
            // 重置面板的 Left 和 Top 偏移
            ResetPanelOffset(musicPanel.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogError("ScreenSettingPanel 面板为空或已销毁");
        }
    }

    public void CancelButton_PictureSetting()
    {
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }
        try
        {
            UImanager.Instance.HidePanel<ScreenSettingPanel>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"关闭MusicPanel失败：{e.Message}");
        }
    }

    public void ChooseButton_Language()
    {
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }
        _currentTypingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask("语言设置", CurrentTopic);
        var musicPanel = UImanager.Instance.ShowPanel<LanguePanel>();
        if (musicPanel != null)
        {
            musicPanel.transform.SetParent(PanelParentObj);
            // 重置面板的 Left 和 Top 偏移
            ResetPanelOffset(musicPanel.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogError("MusicPanel 面板为空或已销毁");
        }
    }

    public void CancelButton_Language()
    {
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }
        try
        {
            UImanager.Instance.HidePanel<LanguePanel>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"关闭MusicPanel失败：{e.Message}");
        }
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 重置面板的 Left 和 Top 偏移为 0
    /// </summary>
    private void ResetPanelOffset(RectTransform panelRt)
    {
        if (panelRt == null) return;

        panelRt.offsetMin = new Vector2(0, panelRt.offsetMin.y);
        panelRt.offsetMax = new Vector2(panelRt.offsetMax.x, 0);
    }
    #endregion

    #region 生命周期修复
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }
        if (ButtonGroupManager.Instance != null)
        {
            ButtonGroupManager.Instance.DestroyRadioGroup(_totalGroupName);
        }
        _isPanelInitialized = false;
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        EventCenter.Instance.TriggerEvent(E_EventType.E_GamePause);
        base.ShowMe(isNeedDefaultAnimator);
        if (!_isPanelInitialized)
        {
            RegisterAllButtonsToSingleGroup();
            _isPanelInitialized = true;
        }
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
        CancelButton_ChangeKey();
        CancelButton_MusicSetting();
        CancelButton_PictureSetting();
        CancelButton_Language();
        base.HideMe(callback, isNeedDefaultAnimator);
    }
    #endregion

    #region 按钮点击事件处理
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "Button_ReturnGame")
            UImanager.Instance.HidePanel<SettingPanel>();
        if (controlName == "Button_ExitGame")
        {
            UImanager.Instance.HidePanel<SettingPanel>();
        }
    }

    #endregion

    #region 面板特殊动画

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
    #endregion
}