using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    private const string UiSelectSound = "Music/update415/ui\u9009\u62e9";
    private const string UiBackSound = "Music/update415/ui\u8fd4\u56de";

    public RectTransform PanelParentObj;

    private TypingWritingTask _currentTypingWritingTask;
    private bool _isPanelInitialized = false;
    private readonly string _totalGroupName = "Setting_TotalGroup";

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

    private void RegisterAllButtonsToSingleGroup()
    {
        AddButtonToGroup(_totalGroupName, "Button_ChangeKey", ChooseButton_ChangeKey, CancelButton_ChangeKey);
        AddButtonToGroup(_totalGroupName, "Button_MusicSetting", ChooseButton_MusicSetting, CancelButton_MusicSetting);
        AddButtonToGroup(_totalGroupName, "Button_PictureSetting", ChooseButton_PictureSetting, CancelButton_PictureSetting);
        AddButtonToGroup(_totalGroupName, "Button_Language", ChooseButton_Language, CancelButton_Language);
        AddButtonToGroup(_totalGroupName, "Button_Introduce", ChooseButton_Introduce, CancelButton_Introduce);
    }

    private void AddButtonToGroup(string groupName, string buttonName, UnityAction chooseEvent, UnityAction cancelEvent, float scale = 1.1f, float duration = 0.2f)
    {
        if (!controlDic.ContainsKey(buttonName))
        {
            return;
        }

        if (controlDic[buttonName] == null)
        {
            return;
        }

        if (controlDic[buttonName] is Button button)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(groupName, button, chooseEvent, cancelEvent, scale, duration);
        }
    }

    public void ChooseButton_ChangeKey()
    {
        if (PanelParentObj == null)
            return;

        var changeKeyPanel = UImanager.Instance.ShowPanel<MoveSettingPanel>();
        if (changeKeyPanel != null)
        {
            changeKeyPanel.transform.SetParent(PanelParentObj);
            ResetPanelOffset(changeKeyPanel.GetComponent<RectTransform>());
        }
    }

    public void CancelButton_ChangeKey()
    {
        StopCurrentTypingTask();
        try
        {
            UImanager.Instance.HidePanel<MoveSettingPanel>();
        }
        catch (System.Exception)
        {
        }
    }

    public void ChooseButton_MusicSetting()
    {
        if (PanelParentObj == null)
            return;

        var musicPanel = UImanager.Instance.ShowPanel<MusicPanel>();
        if (musicPanel != null)
        {
            musicPanel.transform.SetParent(PanelParentObj);
            ResetPanelOffset(musicPanel.GetComponent<RectTransform>());
        }
    }

    public void CancelButton_MusicSetting()
    {
        try
        {
            UImanager.Instance.HidePanel<MusicPanel>();
        }
        catch (System.Exception)
        {
        }
    }

    public void ChooseButton_PictureSetting()
    {
        StopCurrentTypingTask();

        var screenPanel = UImanager.Instance.ShowPanel<ScreenSettingPanel>();
        if (screenPanel != null && PanelParentObj != null)
        {
            screenPanel.transform.SetParent(PanelParentObj);
            ResetPanelOffset(screenPanel.GetComponent<RectTransform>());
        }
    }

    public void CancelButton_PictureSetting()
    {
        StopCurrentTypingTask();
        try
        {
            UImanager.Instance.HidePanel<ScreenSettingPanel>();
        }
        catch (System.Exception)
        {
        }
    }

    public void ChooseButton_Language()
    {
        StopCurrentTypingTask();

        var languagePanel = UImanager.Instance.ShowPanel<LanguePanel>();
        if (languagePanel != null && PanelParentObj != null)
        {
            languagePanel.transform.SetParent(PanelParentObj);
            ResetPanelOffset(languagePanel.GetComponent<RectTransform>());
        }
    }

    public void CancelButton_Language()
    {
        StopCurrentTypingTask();
        try
        {
            UImanager.Instance.HidePanel<LanguePanel>();
        }
        catch (System.Exception)
        {
        }
    }

    public void ChooseButton_Introduce()
    {
        StopCurrentTypingTask();

        if (PanelParentObj == null)
            return;

        var introducePanel = UImanager.Instance.ShowPanel<GameIntroducePanel>();
        if (introducePanel != null)
        {
            introducePanel.transform.SetParent(PanelParentObj);
            ResetPanelOffset(introducePanel.GetComponent<RectTransform>());
        }
    }

    public void CancelButton_Introduce()
    {
        StopCurrentTypingTask();
        try
        {
            UImanager.Instance.HidePanel<GameIntroducePanel>();
        }
        catch (System.Exception)
        {
        }
    }

    private void StopCurrentTypingTask()
    {
        if (_currentTypingWritingTask != null)
        {
            _currentTypingWritingTask.StopTyping();
            _currentTypingWritingTask = null;
        }
    }

    private void ResetPanelOffset(RectTransform panelRt)
    {
        if (panelRt == null)
            return;

        panelRt.offsetMin = new Vector2(0, panelRt.offsetMin.y);
        panelRt.offsetMax = new Vector2(panelRt.offsetMax.x, 0);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopCurrentTypingTask();
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
        CancelButton_Introduce();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (controlName == "Button_ChangeKey"
            || controlName == "Button_MusicSetting"
            || controlName == "Button_PictureSetting"
            || controlName == "Button_Language"
            || controlName == "Button_Introduce")
        {
            MusicManager.Instance?.PlayEffect(UiSelectSound);
        }

        if (controlName == "Button_ReturnGame")
        {
            MusicManager.Instance?.PlayEffect(UiBackSound);
            UImanager.Instance.HidePanel<SettingPanel>();
        }

        if (controlName == "Button_ExitGame")
        {
            MusicManager.Instance?.PlayEffect(UiBackSound);
            UImanager.Instance.HidePanel<SettingPanel>();
        }
    }

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
}