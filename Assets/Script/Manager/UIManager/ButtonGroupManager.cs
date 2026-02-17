using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 按钮组管理器
/// </summary>
public class ButtonGroupManager : SingleBehavior<ButtonGroupManager>
{
    #region 核心存储
    // 单选按钮组字典
    private Dictionary<string, RadioButtonGroupPack> _radioGroupDict = new Dictionary<string, RadioButtonGroupPack>();
    // Toggle切换按钮组字典
    private Dictionary<string, ToggleButtonGroupPack> _toggleGroupDict = new Dictionary<string, ToggleButtonGroupPack>();

    #endregion

    #region 生命周期
    public ButtonGroupManager()
    {
        MonoMange.Instance.AddLister_OnDestroy(OnDestroy);
    }

    private void OnDestroy()
    {
        // 清理所有单选组
        foreach (var group in _radioGroupDict.Values)
            group.ClearAllButtons();
        _radioGroupDict.Clear();

        // 清理所有Toggle组
        foreach (var group in _toggleGroupDict.Values)
            group.ClearAllButtons();
        _toggleGroupDict.Clear();

        DOTween.KillAll();
    }
    #endregion

    #region 原有单选按钮组功能
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
        return AddRadioButtonToGroup_Str(groupName, button,
                                       (btnName) => triggerEvent?.Invoke(),
                                       cancelEvent, chooseScale, changeDuration, chooseColor);
    }

    public RadioButton AddRadioButtonToGroup_Str(string groupName, Button button,
                                               UnityAction<string> triggerEventWithStr = null,
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
        radioButton.InitRadioButton(button, triggerEventWithStr, cancelEvent);
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
    #endregion

    #region Toggle切换按钮组功能
    /// <summary>
    /// 创建Toggle组
    /// </summary>
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

    /// <summary>
    /// 获取Toggle组
    /// </summary>
    public ToggleButtonGroupPack GetToggleGroup(string groupName)
    {
        _toggleGroupDict.TryGetValue(groupName, out var group);
        return group;
    }

    /// <summary>
    /// 注册已有按钮为Toggle按钮
    /// <param name="groupName">分组名称</param>
    /// <param name="button">传入的已有按钮（必传）</param>
    /// <param name="buttonCustomName">按钮自定义名称）</param>
    /// <param name="onActive">选中时触发的函数（传按钮名）</param>
    /// <param name="onCancel">取消选中时触发的函数（传按钮名）</param>
    /// <param name="chooseScale">选中缩放（默认1.05）</param>
    /// <param name="changeDuration">动画时长（默认0.2）</param>
    /// <param name="chooseColor">选中颜色（默认浅绿色）</param>
    /// <param name="isDefaultSelected">是否默认选中（默认false）</param>
    public ToggleButton AddToggleButtonToGroup(string groupName, Button button,
                                              string buttonCustomName = "",
                                              UnityAction<string> onActive = null,
                                              UnityAction<string> onCancel = null,
                                              float chooseScale = 1.05f,
                                              float changeDuration = 0.2f,
                                              Color? chooseColor = null,
                                              bool isDefaultSelected = false)
    {
        if (button == null)
        {
            Debug.LogError("Toggle按钮不能为空！请传入已有按钮组件！");
            return null;
        }

        string finalBtnName = string.IsNullOrEmpty(buttonCustomName) ? button.gameObject.name : buttonCustomName;
        var group = GetToggleGroup(groupName) ?? CreateToggleGroup(groupName);

        // 仅初始化传入的按钮，不生成新按钮
        var toggleButton = new ToggleButton();
        toggleButton.InitToggleButton(button, finalBtnName, onActive, onCancel);
        toggleButton.ChooseScale = chooseScale;
        toggleButton.ChangeDuration = changeDuration;
        toggleButton.ChooseColor = chooseColor ?? new Color(0.2f, 0.8f, 0.2f);

        // 公开方法设置默认状态
        toggleButton.SetSelectedState(isDefaultSelected, false);

        group.AddToggleButton(toggleButton);
        return toggleButton;
    }

    /// <summary>
    /// 从Toggle组移除已注册的按钮
    /// </summary>
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

    /// <summary>
    /// 销毁Toggle组
    /// </summary>
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

    /// <summary>
    /// 手动设置Toggle按钮选中状态
    /// </summary>
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

    /// <summary>
    /// 手动选中指定Toggle分组的按钮
    /// </summary>
    /// <param name="groupName">Toggle分组名</param>
    /// <param name="triggerEvent">是否触发选中事件（默认true）</param>
    public void ManualSelectToggleButton(string groupName, bool triggerEvent = true)
    {
        var group = GetToggleGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 不存在，无法选中按钮！");
            return;
        }

        // 获取分组内第一个按钮（适配单按钮场景）
        var toggleButton = group.GetFirstToggleButton();
        if (toggleButton == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 内无按钮，无法执行选中操作！");
            return;
        }

        toggleButton.ManualSelect(triggerEvent);
    }

    /// <summary>
    /// 手动取消指定Toggle分组的按钮
    /// </summary>
    /// <param name="groupName">Toggle分组名</param>
    /// <param name="triggerEvent">是否触发取消事件（默认true）</param>
    public void ManualCancelToggleButton(string groupName, bool triggerEvent = true)
    {
        var group = GetToggleGroup(groupName);
        if (group == null)
        {
            Debug.LogWarning($"Toggle分组 {groupName} 不存在，无法取消按钮！");
            return;
        }

        // 获取分组内第一个按钮
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

    public RadioButton GetCurrentSelectedButton() => _currentSelectedButton;
}
#endregion

#region 单选按钮实体
public class RadioButton
{
    private Button _radioButton;
    public Button RadioButtonComponent => _radioButton;

    public UnityAction<string> ButtonTriggerEventWithStr;
    public UnityAction ButtonCancelEvent;

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
            if (value == _isChoose) return;

            if (value)
            {
                PlayChooseAnima();
                string btnName = _radioButton?.gameObject?.name ?? "未知单选按钮";
                ButtonTriggerEventWithStr?.Invoke(btnName);
                Debug.Log($"[单选按钮 {btnName}] 选中，执行激活回调");
            }
            else
            {
                PlayCancelAnima();
                ButtonCancelEvent?.Invoke();
                Debug.Log($"[单选按钮 {_radioButton?.name}] 取消选中，执行取消回调");
            }

            _isChoose = value;
        }
    }

    public void InitRadioButton(Button button, UnityAction<string> triggerEventWithStr, UnityAction cancelEvent)
    {
        _radioButton = button;
        ButtonTriggerEventWithStr = triggerEventWithStr;
        ButtonCancelEvent = cancelEvent;

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

    /// <summary>添加已注册的Toggle按钮到组</summary>
    public void AddToggleButton(ToggleButton toggleButton)
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
        // 仅绑定点击事件到传入的按钮
        toggleButton.ButtonComponent.onClick.AddListener(() => toggleButton.ToggleSelectedState());
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

    /// <summary>通过公开方法设置选中状态</summary>
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

    /// <summary>手动选中组内指定Toggle按钮</summary>
    public void ManualSelectButton(Button button, bool triggerEvent = true)
    {
        SetToggleButtonSelected(button, true, triggerEvent);
    }

    /// <summary>手动取消组内指定Toggle按钮</summary>
    public void ManualCancelButton(Button button, bool triggerEvent = true)
    {
        SetToggleButtonSelected(button, false, triggerEvent);
    }

    /// <summary>获取分组内第一个Toggle按钮（单按钮场景专用）</summary>
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

#region Toggle切换按钮实体
public class ToggleButton
{
    // 仅引用传入的按钮组件
    private Button _button;
    private RectTransform _rt;
    private Image _buttonImage;
    private Vector3 _originalScale;
    private Color _originalColor;
    private Sequence _animationSequence;

    #region 可自定义参数
    public float ChooseScale = 1.05f;
    public float ChangeDuration = 0.2f;
    public Color ChooseColor = new Color(0.2f, 0.8f, 0.2f);
    #endregion

    #region 公开属性/事件
    public string ButtonName { get; private set; }
    public UnityAction<string> OnActive;
    public UnityAction<string> OnCancel;
    /// <summary>仅公开get，set通过SetSelectedState方法</summary>
    public bool IsSelected { get; private set; }
    /// <summary>返回传入的按钮组件</summary>
    public Button ButtonComponent => _button;
    #endregion

    /// <summary>初始化：仅接收传入的已有按钮</summary>
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
    }

    /// <summary>切换选中状态）</summary>
    public void ToggleSelectedState()
    {
        SetSelectedState(!IsSelected, true);
    }

    /// <summary>公开的设置状态方法</summary>
    /// <param name="isSelected">是否选中</param>
    /// <param name="triggerEvent">是否触发事件</param>
    public void SetSelectedState(bool isSelected, bool triggerEvent = true)
    {
        if (IsSelected == isSelected) return;

        IsSelected = isSelected;

        if (IsSelected) PlaySelectedAnimation();
        else PlayCancelAnimation();

        if (triggerEvent)
        {
            if (IsSelected)
            {
                OnActive?.Invoke(ButtonName);
                Debug.Log($"[Toggle按钮 {ButtonName}] 选中，执行激活回调");
            }
            else
            {
                OnCancel?.Invoke(ButtonName);
                Debug.Log($"[Toggle按钮 {ButtonName}] 取消选中，执行取消回调");
            }
        }
    }

    /// <summary>手动选中当前Toggle按钮</summary>
    /// <param name="triggerEvent">是否触发选中事件（默认true）</param>
    public void ManualSelect(bool triggerEvent = true)
    {
        SetSelectedState(true, triggerEvent);
    }

    /// <summary>手动取消当前Toggle按钮</summary>
    /// <param name="triggerEvent">是否触发取消事件（默认true）</param>
    public void ManualCancel(bool triggerEvent = true)
    {
        SetSelectedState(false, triggerEvent);
    }

    #region 动画逻辑
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

    /// <summary>清理动画</summary>
    public void ClearAnimation()
    {
        _animationSequence?.Kill();
        _animationSequence = null;

        if (_rt != null) _rt.localScale = _originalScale;
        if (_buttonImage != null) _buttonImage.color = _originalColor;
    }
    #endregion
}
#endregion