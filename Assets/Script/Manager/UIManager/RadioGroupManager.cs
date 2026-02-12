using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 单选按钮组管理器
/// </summary>
public class RadioGroupManager : SingleBehavior<RadioGroupManager>
{
    public RadioGroupManager()
    {
        MonoMange.Instance.AddLister_OnDestroy(OnDestroy);
    }

    private Dictionary<string, RadioButtonGroupPack> _radioGroupDict = new Dictionary<string, RadioButtonGroupPack>();

    #region 分组管理核心接口
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
        if (_radioGroupDict.TryGetValue(groupName, out var group))
            return group;

        Debug.LogWarning($"分组 {groupName} 不存在！");
        return null;
    }

    public RadioButton AddButtonToGroup(string groupName, Button button,
                                        UnityAction triggerEvent = null,
                                        UnityAction cancelEvent = null,
                                        float chooseScale = 1.05f,
                                        float changeDuration = 0.2f,
                                        Color? chooseColor = null)
    {
        // 调用带参版本，无参委托包装为带参
        return AddButtonToGroup_Str(groupName, button,
                               (btnName) => triggerEvent?.Invoke(),
                               cancelEvent, chooseScale, changeDuration, chooseColor);
    }


    public RadioButton AddButtonToGroup_Str(string groupName, Button button,
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


    public void RemoveButtonFromGroup(string groupName, Button button)
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
            Debug.Log($"分组 {groupName} 已销毁！");
        }
        else
        {
            Debug.LogWarning($"分组 {groupName} 不存在，无需销毁！");
        }
    }
    #endregion

    #region 生命周期
    private void OnDestroy()
    {
        foreach (var group in _radioGroupDict.Values)
            group.ClearAllButtons();
        _radioGroupDict.Clear();
    }
    #endregion

    public void SelectFirstButtonInGroup(string groupName, bool triggerOnClick = true)
    {
        var group = GetRadioGroup(groupName);
        if (group != null)
            group.SelectFirstButton(triggerOnClick);
        else
            Debug.LogWarning($"分组 {groupName} 不存在，无法选择第一个按钮！");
    }
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
            Debug.LogWarning($"按钮 {radioButton.RadioButtonComponent.name} 已在分组 {GroupName} 中，无需重复添加！");
            return;
        }

        RadioButtonList.Add(radioButton);
        radioButton.RadioButtonComponent.onClick.AddListener(() => OnButtonClicked(radioButton));

        // 第一个加入的按钮自动选中
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
            Debug.LogWarning($"分组 {GroupName} 中未找到按钮 {button.name}，移除失败！");
            return;
        }

        target.RadioButtonComponent.onClick.RemoveAllListeners();
        target.ClearAnimaSequence();
        RadioButtonList.Remove(target);

        // 若移除的是当前选中按钮，自动选中列表中的第一个按钮
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
            Debug.LogWarning($"分组 {GroupName} 没有按钮，无法选择第一个！");
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

    public UnityAction<string> ButtonTriggerEventWithStr; // 带string参数的触发事件
    public UnityAction ButtonCancelEvent;                 // 保留原有取消事件

    private Sequence _animaSequence;
    private RectTransform _rt;
    private Image _buttonImage;
    private Color _originalColor;
    private Vector3 _originalScale;

    public float ChooseScale = 1.05f;
    public float ChangeDuration = 0.2f;
    public Color ChooseColor = Color.green;

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
                // 触发带参事件，传入按钮GameObject名称
                string btnName = _radioButton?.gameObject?.name ?? "未知按钮";
                ButtonTriggerEventWithStr?.Invoke(btnName);
                Debug.Log($"[单选按钮 {btnName}] 选中，执行带参回调（传入名称：{btnName}）");
            }
            else
            {
                PlayCancelAnima();
                ButtonCancelEvent?.Invoke();
                Debug.Log($"[单选按钮 {_radioButton?.name}] 取消选中，执行回调");
            }

            _isChoose = value;
        }
    }

    // ========== 初始化方法：仅接收带参委托 ==========
    public void InitRadioButton(Button button, UnityAction<string> triggerEventWithStr, UnityAction cancelEvent)
    {
        _radioButton = button;
        ButtonTriggerEventWithStr = triggerEventWithStr;
        ButtonCancelEvent = cancelEvent;

        _rt = button.GetComponent<RectTransform>();
        _buttonImage = button.GetComponent<Image>();

        if (_rt == null)
            Debug.LogError($"按钮 {button.name} 缺少 RectTransform 组件，动画将无法播放！");
        if (_buttonImage == null)
            Debug.LogWarning($"按钮 {button.name} 缺少 Image 组件，颜色动画将失效！");

        _originalScale = _rt ? _rt.localScale : Vector3.one;
        _originalColor = _buttonImage ? _buttonImage.color : Color.white;

        // 清理可能遗留的动画序列
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