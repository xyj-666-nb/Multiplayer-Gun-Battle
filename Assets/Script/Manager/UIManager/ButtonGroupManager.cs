using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 按钮组管理器
/// </summary>
public class ButtonGroupManager : SingleBehavior<ButtonGroupManager>
{
    #region 核心存储
    private Dictionary<string, RadioButtonGroupPack> _radioGroupDict = new Dictionary<string, RadioButtonGroupPack>();
    private Dictionary<string, ToggleButtonGroupPack> _toggleGroupDict = new Dictionary<string, ToggleButtonGroupPack>();
    #endregion

    #region 生命周期
    public ButtonGroupManager()
    {
        MonoMange.Instance.AddLister_OnDestroy(OnDestroy);
    }

    private void OnDestroy()
    {
        foreach (var group in _radioGroupDict.Values)
            group.ClearAllButtons();
        _radioGroupDict.Clear();

        foreach (var group in _toggleGroupDict.Values)
            group.ClearAllButtons();
        _toggleGroupDict.Clear();

        DOTween.KillAll();
    }
    #endregion

    #region 单选按钮组功能 (已简化重载)

    public RadioButtonGroupPack CreateRadioGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError("单选分组名称不能为空！");
            return null;
        }

        if (_radioGroupDict.TryGetValue(groupName, out var existingGroup))
        {
            Debug.LogWarning($"分组 {groupName} 已存在，将返回已有分组！");
            return existingGroup;
        }

        var newGroup = new RadioButtonGroupPack(groupName);
        _radioGroupDict.Add(groupName, newGroup);
        return newGroup;
    }

    public RadioButtonGroupPack GetRadioGroup(string groupName)
    {
        _radioGroupDict.TryGetValue(groupName, out var group);
        return group;
    }

    public RadioButton AddRadioButtonToGroup(string groupName, Button button,
                                           UnityAction triggerEvent = null,
                                           UnityAction cancelEvent = null,
                                           float chooseScale = 1.05f,
                                           float changeDuration = 0.2f,
                                           Color? chooseColor = null)
    {
        if (button == null)
        {
            Debug.LogError("添加的单选按钮不能为空！");
            return null;
        }

        var group = GetRadioGroup(groupName) ?? CreateRadioGroup(groupName);

        var radioButton = new RadioButton();
        // 包装一下，把无参回调转成有参的内部调用
        UnityAction<string> wrappedTrigger = (name) => triggerEvent?.Invoke();
        UnityAction<string> wrappedCancel = (name) => cancelEvent?.Invoke();

        radioButton.InitRadioButton(button, wrappedTrigger, wrappedCancel);
        radioButton.ChooseScale = chooseScale;
        radioButton.ChangeDuration = changeDuration;
        if (chooseColor.HasValue)
            radioButton.ChooseColor = chooseColor.Value;

        group.AddRadioButton(radioButton);
        return radioButton;
    }

    public RadioButton AddRadioButtonToGroup_Str(string groupName, Button button,
                                              UnityAction<string> triggerEventWithStr = null,
                                              UnityAction<string> cancelEventWithStr = null,
                                              float chooseScale = 1.05f,
                                              float changeDuration = 0.2f,
                                              Color? chooseColor = null)
    {
        if (button == null)
        {
            Debug.LogError("添加的单选按钮不能为空！");
            return null;
        }

        var group = GetRadioGroup(groupName) ?? CreateRadioGroup(groupName);

        var radioButton = new RadioButton();
        radioButton.InitRadioButton(button, triggerEventWithStr, cancelEventWithStr);
        radioButton.ChooseScale = chooseScale;
        radioButton.ChangeDuration = changeDuration;
        if (chooseColor.HasValue)
            radioButton.ChooseColor = chooseColor.Value;

        group.AddRadioButton(radioButton);
        return radioButton;
    }

    public void RemoveRadioButtonFromGroup(string groupName, Button button)
    {
        if (button == null)
        {
            Debug.LogError("要移除的单选按钮不能为空！");
            return;
        }

        var group = GetRadioGroup(groupName);
        group?.RemoveRadioButton(button);
    }

    public void DestroyRadioGroup(string groupName)
    {
        if (_radioGroupDict.Remove(groupName, out var group))
        {
            group.ClearAllButtons();
            Debug.Log($"单选分组 {groupName} 已销毁！");
        }
        else
        {
            Debug.LogWarning($"单选分组 {groupName} 不存在，无需销毁！");
        }
    }

    public void SelectFirstRadioButtonInGroup(string groupName, bool triggerOnClick = true)
    {
        var group = GetRadioGroup(groupName);
        if (group != null)
            group.SelectFirstButton(triggerOnClick);
        else
            Debug.LogWarning($"单选分组 {groupName} 不存在，无法选择第一个按钮！");
    }

    // 【新增】通过按钮名字手动选中指定按钮
    public void SelectRadioButtonByName(string groupName, string buttonName, bool triggerEvent = true)
    {
        var group = GetRadioGroup(groupName);
        if (group != null)
            group.SelectButtonByName(buttonName, triggerEvent);
        else
            Debug.LogWarning($"单选分组 {groupName} 不存在，无法选择按钮 {buttonName}！");
    }
    #endregion

    #region Toggle切换按钮组功能 (保持不变)
    public ToggleButtonGroupPack CreateToggleGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError("Toggle分组名称不能为空！");
            return null;
        }

        if (_toggleGroupDict.TryGetValue(groupName, out var existingGroup))
        {
            Debug.LogWarning($"Toggle分组 {groupName} 已存在，将返回已有分组！");
            return existingGroup;
        }

        var newGroup = new ToggleButtonGroupPack(groupName);
        _toggleGroupDict.Add(groupName, newGroup);
        return newGroup;
    }

    public ToggleButtonGroupPack GetToggleGroup(string groupName)
    {
        _toggleGroupDict.TryGetValue(groupName, out var group);
        return group;
    }

    public ToggleButton AddToggleButtonToGroup(string groupName, Button button,
                                              string buttonCustomName = "",
                                              UnityAction<string> onActive = null,
                                              UnityAction<string> onCancel = null,
                                              float chooseScale = 1.05f,
                                              float changeDuration = 0.2f,
                                              Color? chooseColor = null,
                                              bool isDefaultSelected = false,
                                              bool isManualTrigger = false)
    {
        if (button == null)
        {
            Debug.LogError("Toggle按钮不能为空！请传入已有按钮组件！");
            return null;
        }

        string finalBtnName = string.IsNullOrEmpty(buttonCustomName) ? button.gameObject.name : buttonCustomName;
        var group = GetToggleGroup(groupName) ?? CreateToggleGroup(groupName);

        var toggleButton = new ToggleButton();
        toggleButton.InitToggleButton(button, finalBtnName, onActive, onCancel);
        toggleButton.ChooseScale = chooseScale;
        toggleButton.ChangeDuration = changeDuration;
        toggleButton.ChooseColor = chooseColor ?? new Color(0.2f, 0.8f, 0.2f);
        toggleButton.SetSelectedState(isDefaultSelected, false);
        group.AddToggleButton(toggleButton, isManualTrigger);
        return toggleButton;
    }

    public void RemoveToggleButtonFromGroup(string groupName, Button button)
    {
        if (button == null)
        {
            Debug.LogError("要移除的Toggle按钮不能为空！");
            return;
        }

        var group = GetToggleGroup(groupName);
        group?.RemoveToggleButton(button);
    }

    public void DestroyToggleGroup(string groupName)
    {
        if (_toggleGroupDict.Remove(groupName, out var group))
        {
            group.ClearAllButtons();
            Debug.Log($"Toggle分组 {groupName} 已销毁！");
        }
        else
        {
            Debug.LogWarning($"Toggle分组 {groupName} 不存在，无需销毁！");
        }
    }

    public void SetToggleButtonSelected(string groupName, Button button, bool isSelected, bool triggerEvent = true)
    {
        var group = GetToggleGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 不存在！");
            return;
        }

        group.SetToggleButtonSelected(button, isSelected, triggerEvent);
    }

    public void ManualSelectToggleButton(string groupName, bool triggerEvent = true)
    {
        var group = GetToggleGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 不存在，无法选中按钮！");
            return;
        }

        var toggleButton = group.GetFirstToggleButton();
        if (toggleButton == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 内无按钮，无法执行选中操作！");
            return;
        }

        toggleButton.ManualSelect(triggerEvent);
    }

    public void ManualCancelToggleButton(string groupName, bool triggerEvent = true)
    {
        var group = GetToggleGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 不存在，无法取消按钮！");
            return;
        }

        var toggleButton = group.GetFirstToggleButton();
        if (toggleButton == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 内无按钮，无法执行取消操作！");
            return;
        }

        toggleButton.ManualCancel(triggerEvent);
    }
    #endregion
}

#region 单选按钮组实体
public class RadioButtonGroupPack
{
    public string GroupName { get; }
    public List<RadioButton> RadioButtonList { get; } = new List<RadioButton>();
    private RadioButton _currentSelectedButton;

    public RadioButtonGroupPack(string groupName)
    {
        GroupName = groupName;
    }

    public void AddRadioButton(RadioButton radioButton)
    {
        if (radioButton?.RadioButtonComponent == null)
        {
            Debug.LogError("无效的单选按钮，无法添加到分组！");
            return;
        }

        if (RadioButtonList.Contains(radioButton))
        {
            Debug.LogWarning($"按钮 {radioButton.RadioButtonComponent.name} 已在单选分组 {GroupName} 中，无需重复添加！");
            return;
        }

        RadioButtonList.Add(radioButton);
        radioButton.RadioButtonComponent.onClick.AddListener(() => OnButtonClicked(radioButton));

        if (RadioButtonList.Count == 1 && _currentSelectedButton == null)
        {
            Debug.Log($"[单选分组 {GroupName}] 第一个按钮 {radioButton.RadioButtonComponent.name} 默认选中");
            SetButtonSelected(radioButton, true);
        }
    }

    public void RemoveRadioButton(Button button)
    {
        var target = RadioButtonList.Find(b => b.RadioButtonComponent == button);
        if (target == null)
        {
            Debug.LogWarning($"单选分组 {GroupName} 中未找到按钮 {button.name}，移除失败！");
            return;
        }

        target.RadioButtonComponent.onClick.RemoveAllListeners();
        target.ClearAnimaSequence();
        RadioButtonList.Remove(target);

        if (target == _currentSelectedButton && RadioButtonList.Count > 0)
        {
            Debug.Log($"[单选分组 {GroupName}] 当前选中按钮被移除，自动选中 {RadioButtonList[0].RadioButtonComponent.name}");
            SetButtonSelected(RadioButtonList[0], true);
        }
    }

    public void ClearAllButtons()
    {
        foreach (var button in RadioButtonList)
        {
            button.RadioButtonComponent.onClick.RemoveAllListeners();
            button.ClearAnimaSequence();
        }
        RadioButtonList.Clear();
        _currentSelectedButton = null;
    }

    private void OnButtonClicked(RadioButton clickedButton)
    {
        foreach (var btn in RadioButtonList)
            SetButtonSelected(btn, btn == clickedButton);
    }

    private void SetButtonSelected(RadioButton button, bool isSelected)
    {
        if (isSelected)
            _currentSelectedButton = button;
        button.IsChoose = isSelected;
    }

    public void SelectFirstButton(bool triggerOnClick = true)
    {
        if (RadioButtonList == null || RadioButtonList.Count == 0)
        {
            Debug.LogWarning($"单选分组 {GroupName} 没有按钮，无法选择第一个！");
            return;
        }
        var first = RadioButtonList[0];
        SetButtonSelected(first, true);
        if (triggerOnClick)
        {
            first.RadioButtonComponent?.onClick?.Invoke();
        }
    }

    // 【新增】通过名字选中指定按钮
    public void SelectButtonByName(string buttonName, bool triggerEvent = true)
    {
        if (RadioButtonList == null || RadioButtonList.Count == 0)
        {
            Debug.LogWarning($"单选分组 {GroupName} 没有按钮，无法选择 {buttonName}！");
            return;
        }

        // 找到目标按钮
        RadioButton targetBtn = null;
        foreach (var btn in RadioButtonList)
        {
            if (btn.RadioButtonComponent != null && btn.RadioButtonComponent.gameObject.name == buttonName)
            {
                targetBtn = btn;
                break;
            }
        }

        if (targetBtn == null)
        {
            Debug.LogWarning($"单选分组 {GroupName} 中未找到按钮 {buttonName}！");
            return;
        }

        // 选中目标，取消其他
        foreach (var btn in RadioButtonList)
        {
            SetButtonSelected(btn, btn == targetBtn);
        }

        // 如果需要触发事件，手动调用一下
        if (triggerEvent)
        {
            // 这里不需要手动调用 onClick，因为设置 IsChoose 会自动触发回调
            // 如果需要强制触发，可以在这里手动调用 ButtonTriggerEventWithStr
        }
    }

    public RadioButton GetCurrentSelectedButton() => _currentSelectedButton;
}
#endregion

#region 单选按钮实体
public class RadioButton
{
    private Button _radioButton;
    public Button RadioButtonComponent => _radioButton;

    public UnityAction<string> ButtonTriggerEventWithStr;
    public UnityAction<string> ButtonCancelEventWithStr;

    private Sequence _animaSequence;
    private RectTransform _rt;
    private Image _buttonImage;
    private Color _originalColor;
    private Vector3 _originalScale;

    public float ChooseScale = 1.05f;
    public float ChangeDuration = 0.2f;
    public Color ChooseColor = new Color(0.2f, 0.8f, 0.2f);

    private bool _isChoose;
    public bool IsChoose
    {
        get => _isChoose;
        set
        {
            if (value == _isChoose)
                return;
            if (_radioButton == null || this == null) return;

            if (value)
            {
                PlayChooseAnima();
                string btnName = _radioButton?.gameObject?.name ?? "未知单选按钮";
                ButtonTriggerEventWithStr?.Invoke(btnName);

            }
            else
            {
                PlayCancelAnima();
                string btnName = _radioButton?.gameObject?.name ?? "未知单选按钮";
                ButtonCancelEventWithStr?.Invoke(btnName);
            }

            _isChoose = value;
        }
    }

    public void InitRadioButton(Button button, UnityAction<string> triggerEventWithStr, UnityAction<string> cancelEventWithStr)
    {
        _radioButton = button;
        ButtonTriggerEventWithStr = triggerEventWithStr;
        ButtonCancelEventWithStr = cancelEventWithStr;

        _rt = button.GetComponent<RectTransform>();
        _buttonImage = button.GetComponent<Image>();

        if (_rt == null)
            Debug.LogError($"单选按钮 {button.name} 缺少 RectTransform 组件，动画将无法播放！");
        if (_buttonImage == null)
            Debug.LogWarning($"单选按钮 {button.name} 缺少 Image 组件，颜色动画将失效！");

        _originalScale = _rt ? _rt.localScale : Vector3.one;
        _originalColor = _buttonImage ? _buttonImage.color : Color.white;

        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence();

        AddPressEventTrigger(button);
    }

    private void AddPressEventTrigger(Button button)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => PlayPressAnima());
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) => PlayReleaseAnima());
        trigger.triggers.Add(upEntry);
    }

    private void PlayPressAnima()
    {
        if (_rt == null) return;

        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence()
            .Append(_rt.DOScale(0.95f, 0.1f).SetEase(Ease.OutQuad));
    }

    private void PlayReleaseAnima()
    {
        if (_rt == null) return;

        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence()
            .Append(_rt.DOScale(_originalScale, 0.1f).SetEase(Ease.OutQuad));
    }

    private void PlayChooseAnima()
    {
        if (_rt == null) return;

        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence()
            .Append(_rt.DOScale(ChooseScale, ChangeDuration).SetEase(Ease.OutQuad));
        if (_buttonImage != null)
            _animaSequence.Join(_buttonImage.DOColor(ChooseColor, ChangeDuration).SetEase(Ease.OutQuad));
    }

    private void PlayCancelAnima()
    {
        if (_rt == null) return;

        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence()
            .Append(_rt.DOScale(_originalScale, ChangeDuration).SetEase(Ease.InQuad));
        if (_buttonImage != null)
            _animaSequence.Join(_buttonImage.DOColor(_originalColor, ChangeDuration).SetEase(Ease.InQuad));
    }

    public void ClearAnimaSequence()
    {
        _animaSequence?.Kill();
        _animaSequence = null;
    }
}
#endregion

#region Toggle切换按钮组实体 
public class ToggleButtonGroupPack
{
    public string GroupName { get; }
    public List<ToggleButton> ToggleButtonList { get; } = new List<ToggleButton>();

    public ToggleButtonGroupPack(string groupName)
    {
        GroupName = groupName;
    }

    public void AddToggleButton(ToggleButton toggleButton, bool isManualTrigger = false)
    {
        if (toggleButton?.ButtonComponent == null)
        {
            Debug.LogError("无效的Toggle按钮，无法添加到分组！");
            return;
        }

        if (ToggleButtonList.Contains(toggleButton))
        {
            Debug.LogWarning($"Toggle按钮 {toggleButton.ButtonName} 已在分组 {GroupName} 中，无需重复添加！");
            return;
        }

        ToggleButtonList.Add(toggleButton);

        if (!isManualTrigger)
        {
            toggleButton.ButtonComponent.onClick.AddListener(() => toggleButton.ToggleSelectedState());
        }
        else
        {
            Debug.Log($"[Toggle分组 {GroupName}] 按钮 {toggleButton.ButtonName} 已设为完全手动模式，点击事件未绑定。");
        }
    }

    public void RemoveToggleButton(Button button)
    {
        var target = ToggleButtonList.Find(b => b.ButtonComponent == button);
        if (target == null)
        {
            Debug.LogWarning($"Toggle分组 {GroupName} 中未找到按钮 {button.name}，移除失败！");
            return;
        }

        target.ButtonComponent.onClick.RemoveAllListeners();
        target.ClearAnimation();
        ToggleButtonList.Remove(target);
    }

    public void ClearAllButtons()
    {
        foreach (var btn in ToggleButtonList)
        {
            btn.ButtonComponent.onClick.RemoveAllListeners();
            btn.ClearAnimation();
        }
        ToggleButtonList.Clear();
    }

    public void SetToggleButtonSelected(Button button, bool isSelected, bool triggerEvent = true)
    {
        var target = ToggleButtonList.Find(b => b.ButtonComponent == button);
        if (target == null)
        {
            Debug.LogWarning($"Toggle分组 {GroupName} 中未找到按钮 {button.name}！");
            return;
        }

        target.SetSelectedState(isSelected, triggerEvent);
    }

    public void ManualSelectButton(Button button, bool triggerEvent = true)
    {
        SetToggleButtonSelected(button, true, triggerEvent);
    }

    public void ManualCancelButton(Button button, bool triggerEvent = true)
    {
        SetToggleButtonSelected(button, false, triggerEvent);
    }

    public ToggleButton GetFirstToggleButton()
    {
        return ToggleButtonList.Count > 0 ? ToggleButtonList[0] : null;
    }

    public List<ToggleButton> GetAllSelectedToggleButtons()
    {
        return ToggleButtonList.FindAll(b => b.IsSelected);
    }
}
#endregion

#region Toggle切换按钮实体 (保持不变)
public class ToggleButton
{
    private Button _button;
    private RectTransform _rt;
    private Image _buttonImage;
    private Vector3 _originalScale;
    private Color _originalColor;
    private Sequence _animationSequence;

    public float ChooseScale = 1.05f;
    public float ChangeDuration = 0.2f;
    public Color ChooseColor = new Color(0.2f, 0.8f, 0.2f);

    public string ButtonName { get; private set; }
    public UnityAction<string> OnActive;
    public UnityAction<string> OnCancel;
    public bool IsSelected { get; private set; }
    public Button ButtonComponent => _button;

    public void InitToggleButton(Button button, string btnName, UnityAction<string> onActive, UnityAction<string> onCancel)
    {
        _button = button;
        ButtonName = btnName;
        OnActive = onActive;
        OnCancel = onCancel;

        _rt = button.GetComponent<RectTransform>();
        _buttonImage = button.GetComponent<Image>();

        if (_rt == null)
            Debug.LogError($"Toggle按钮 {ButtonName} 缺少 RectTransform 组件，缩放动画失效！");
        if (_buttonImage == null)
            Debug.LogWarning($"Toggle按钮 {ButtonName} 缺少 Image 组件，颜色动画失效！");

        _originalScale = _rt ? _rt.localScale : Vector3.one;
        _originalColor = _buttonImage ? _buttonImage.color : Color.white;

        _animationSequence?.Kill();
        _animationSequence = DOTween.Sequence();

        AddPressEventTrigger(button);
    }

    private void AddPressEventTrigger(Button button)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => PlayPressAnima());
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) => PlayReleaseAnima());
        trigger.triggers.Add(upEntry);
    }

    private void PlayPressAnima()
    {
        if (_rt == null) return;

        _animationSequence?.Kill();
        _animationSequence = DOTween.Sequence()
            .Append(_rt.DOScale(0.95f, 0.1f).SetEase(Ease.OutQuad));
    }

    private void PlayReleaseAnima()
    {
        if (_rt == null) return;

        _animationSequence?.Kill();
        _animationSequence = DOTween.Sequence()
            .Append(_rt.DOScale(_originalScale, 0.1f).SetEase(Ease.OutQuad));
    }

    public void ToggleSelectedState()
    {
        SetSelectedState(!IsSelected, true);
    }

    public void SetSelectedState(bool isSelected, bool triggerEvent = true)
    {
        if (IsSelected == isSelected) return;

        // 安全检查
        if (_button == null) return;

        IsSelected = isSelected;

        if (IsSelected) PlaySelectedAnimation();
        else PlayCancelAnimation();

        if (triggerEvent)
        {
            if (IsSelected)
            {
                OnActive?.Invoke(ButtonName);
            }
            else
            {
                OnCancel?.Invoke(ButtonName);
            }
        }
    }

    public void ManualSelect(bool triggerEvent = true)
    {
        SetSelectedState(true, triggerEvent);
    }

    public void ManualCancel(bool triggerEvent = true)
    {
        SetSelectedState(false, triggerEvent);
    }

    private void PlaySelectedAnimation()
    {
        if (_rt == null) return;

        _animationSequence?.Kill();
        _animationSequence = DOTween.Sequence()
            .Append(_rt.DOScale(ChooseScale, ChangeDuration).SetEase(Ease.OutQuad))
            .SetUpdate(true);

        if (_buttonImage != null)
            _animationSequence.Join(_buttonImage.DOColor(ChooseColor, ChangeDuration).SetEase(Ease.OutQuad));
    }

    private void PlayCancelAnimation()
    {
        if (_rt == null) return;

        _animationSequence?.Kill();
        _animationSequence = DOTween.Sequence()
            .Append(_rt.DOScale(_originalScale, ChangeDuration).SetEase(Ease.InQuad))
            .SetUpdate(true);

        if (_buttonImage != null)
            _animationSequence.Join(_buttonImage.DOColor(_originalColor, ChangeDuration).SetEase(Ease.InQuad));
    }

    public void ClearAnimation()
    {
        _animationSequence?.Kill();
        _animationSequence = null;

        if (_rt != null) _rt.localScale = _originalScale;
        if (_buttonImage != null) _buttonImage.color = _originalColor;
    }
}
#endregion