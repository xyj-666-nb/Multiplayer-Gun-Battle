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
        //在MonoMange中注册销毁
        MonoMange.Instance.AddLister_OnDestroy(OnDestroy);
    }


    private Dictionary<string, RadioButtonGroupPack> _radioGroupDict = new Dictionary<string, RadioButtonGroupPack>();

    #region 分组管理核心接口
    /// <summary>
    /// 创建一个新的单选按钮分组
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <returns>创建的分组</returns>
    public RadioButtonGroupPack CreateRadioGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError("单选分组名称不能为空！");
            return null;
        }

        if (_radioGroupDict.ContainsKey(groupName))
        {
            Debug.LogWarning($"分组 {groupName} 已存在，将返回已有分组！");
            return _radioGroupDict[groupName];
        }

        RadioButtonGroupPack newGroup = new RadioButtonGroupPack(groupName);
        _radioGroupDict.Add(groupName, newGroup);
        return newGroup;
    }

    /// <summary>
    /// 获取指定名称的单选分组
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <returns>对应的分组</returns>
    public RadioButtonGroupPack GetRadioGroup(string groupName)
    {
        if (_radioGroupDict.TryGetValue(groupName, out var group))
        {
            return group;
        }
        Debug.LogWarning($"分组 {groupName} 不存在！");
        return null;
    }

    /// <summary>
    /// 向指定分组添加单选按钮
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="button">UI按钮组件</param>
    /// <param name="triggerEvent">按钮选中时的回调</param>
    /// <param name="cancelEvent">按钮取消选中时的回调</param>
    /// <param name="chooseScale">选中时的缩放值（默认1.05）</param>
    /// <param name="changeDuration">动画时长（默认0.2）</param>
    /// <param name="chooseColor">选中时的颜色（默认亮绿色）</param>
    /// <returns>创建的单选按钮对象</returns>
    public RadioButton AddButtonToGroup(string groupName, Button button,
                                        UnityAction triggerEvent = null,
                                        UnityAction cancelEvent = null,
                                        float chooseScale = 1.05f,
                                        float changeDuration = 0.2f,
                                        Color? chooseColor = null)
    {
        // 校验参数
        if (button == null)
        {
            Debug.LogError("添加的单选按钮不能为空！");
            return null;
        }

        // 获取/创建分组
        RadioButtonGroupPack group = GetRadioGroup(groupName) ?? CreateRadioGroup(groupName);

        // 创建单选按钮对象并初始化
        RadioButton radioButton = new RadioButton();
        radioButton.InitRadioButton(button, triggerEvent, cancelEvent);
        // 设置自定义动画参数
        radioButton.ChooseScale = chooseScale;
        radioButton.ChangeDuration = changeDuration;
        if (chooseColor.HasValue)
        {
            radioButton.ChooseColor = chooseColor.Value;
        }

        // 添加到分组并绑定点击事件
        group.AddRadioButton(radioButton);

        return radioButton;
    }

    /// <summary>
    /// 从指定分组移除单选按钮
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="button">要移除的UI按钮</param>
    public void RemoveButtonFromGroup(string groupName, Button button)
    {
        if (button == null)
        {
            Debug.LogError("要移除的单选按钮不能为空！");
            return;
        }

        RadioButtonGroupPack group = GetRadioGroup(groupName);
        if (group == null) return;

        group.RemoveRadioButton(button);
    }

    /// <summary>
    /// 销毁指定的单选分组
    /// </summary>
    /// <param name="groupName">分组名称</param>
    public void DestroyRadioGroup(string groupName)
    {
        if (_radioGroupDict.TryGetValue(groupName, out var group))
        {
            group.ClearAllButtons();
            _radioGroupDict.Remove(groupName);
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
        // 销毁所有分组的动画序列
        foreach (var group in _radioGroupDict.Values)
        {
            group.ClearAllButtons();
        }
        _radioGroupDict.Clear();
    }
    #endregion
}

#region 单选按钮组

/// <summary>
/// 单选按钮分组实体
/// </summary>
public class RadioButtonGroupPack
{
    #region 属性字段以及构造函数
    public string GroupName { get; private set; } // 分组名称
    public List<RadioButton> RadioButtonList { get; private set; } // 分组内的所有按钮

    // 当前选中的按钮
    private RadioButton _currentSelectedButton;

    public RadioButtonGroupPack(string groupName)
    {
        GroupName = groupName;
        RadioButtonList = new List<RadioButton>();
    }

    #endregion

    #region 添加单选按钮
    /// <summary>
    /// 向分组添加单选按钮
    /// </summary>
    /// <param name="radioButton">要添加的单选按钮</param>
    public void AddRadioButton(RadioButton radioButton)
    {
        if (radioButton == null || radioButton.RadioButtonComponent == null)
        {
            Debug.LogError("无效的单选按钮，无法添加到分组！");
            return;
        }

        // 避免重复添加
        if (RadioButtonList.Contains(radioButton))
        {
            Debug.LogWarning($"按钮 {radioButton.RadioButtonComponent.name} 已在分组 {GroupName} 中，无需重复添加！");
            return;
        }

        // 添加按钮并绑定点击事件
        RadioButtonList.Add(radioButton);
        radioButton.RadioButtonComponent.onClick.AddListener(() => OnButtonClicked(radioButton));

        // 分组第一个按钮默认选中
        if (RadioButtonList.Count == 1 && _currentSelectedButton == null)
        {
            SetButtonSelected(radioButton, true);
        }
    }

    #endregion

    #region 移除单选按钮

    /// <summary>
    /// 从分组移除单选按钮
    /// </summary>
    /// <param name="button">要移除的UI按钮</param>
    public void RemoveRadioButton(Button button)
    {
        RadioButton targetButton = RadioButtonList.Find(b => b.RadioButtonComponent == button);
        if (targetButton == null)
        {
            Debug.LogWarning($"分组 {GroupName} 中未找到按钮 {button.name}，移除失败！");
            return;
        }

        // 移除点击事件和动画序列
        targetButton.RadioButtonComponent.onClick.RemoveAllListeners();
        targetButton.ClearAnimaSequence();
        RadioButtonList.Remove(targetButton);

        // 如果移除的是当前选中按钮，自动选中第一个按钮
        if (targetButton == _currentSelectedButton && RadioButtonList.Count > 0)
        {
            SetButtonSelected(RadioButtonList[0], true);
        }
    }

    #endregion

    #region 清空分组按钮

    /// <summary>
    /// 清空分组内所有按钮
    /// </summary>
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
    #endregion

    #region 按钮点击处理逻辑

    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    /// <param name="clickedButton">点击的按钮</param>
    private void OnButtonClicked(RadioButton clickedButton)
    {
        // 遍历分组内所有按钮，仅保留当前点击的按钮为选中状态
        foreach (var btn in RadioButtonList)
        {
            bool isSelected = btn == clickedButton;
            SetButtonSelected(btn, isSelected);
        }
    }

    /// <summary>
    /// 设置按钮的选中状态
    /// </summary>
    /// <param name="button">目标按钮</param>
    /// <param name="isSelected">是否选中</param>
    private void SetButtonSelected(RadioButton button, bool isSelected)
    {
        if (isSelected)
        {
            _currentSelectedButton = button;
        }
        button.IsChoose = isSelected;
    }

    #endregion

    #region 获取当前选中按钮

    /// <summary>
    /// 获取当前分组选中的按钮
    /// </summary>
    /// <returns>选中的按钮</returns>
    public RadioButton GetCurrentSelectedButton()
    {
        return _currentSelectedButton;
    }

    #endregion
}
#endregion

#region 单选按钮实体类

/// <summary>
/// 单选按钮实体
/// </summary>
public class RadioButton
{
    // 按钮核心组件
    private Button _radioButton;
    public Button RadioButtonComponent => _radioButton;

    // 事件回调
    public UnityAction ButtonTriggerEvent; // 选中时触发
    public UnityAction ButtonCancelEvent;  // 取消选中时触发

    // 动画相关
    private Sequence _animaSequence; // 动画序列
    private RectTransform _rt;       // 按钮RectTransform
    private Image _buttonImage;      // 按钮图片
    private Color _originalColor;    // 原始颜色
    private Vector3 _originalScale;  // 原始缩放

    // 动画参数
    [Header("默认动画参数")]
    public float ChooseScale = 1.05f;       // 选中缩放
    public float ChangeDuration = 0.2f;     // 动画时长
    public Color ChooseColor = ColorManager.LightGreen; // 选中颜色

    // 选中状态
    private bool _isChoose=false;
    public bool IsChoose
    {
        get => _isChoose;
        set
        {
            if (value == _isChoose) return;

            // 执行选中/取消动画和事件
            if (value)
            {
                PlayChooseAnima();
                ButtonTriggerEvent?.Invoke();
            }
            else
            {
                PlayCancelAnima();
                ButtonCancelEvent?.Invoke();
            }

            _isChoose = value;
        }
    }

    /// <summary>
    /// 初始化单选按钮
    /// </summary>
    /// <param name="button">UI按钮组件</param>
    /// <param name="triggerEvent">选中回调</param>
    /// <param name="cancelEvent">取消回调</param>
    public void InitRadioButton(Button button, UnityAction triggerEvent, UnityAction cancelEvent)
    {
        _radioButton = button;
        ButtonTriggerEvent = triggerEvent;
        ButtonCancelEvent = cancelEvent;

        // 获取组件并记录初始状态
        _rt = button.GetComponent<RectTransform>();
        _buttonImage = button.GetComponent<Image>();

        if (_rt == null)
        {
            Debug.LogError($"按钮 {button.name} 缺少 RectTransform 组件！");
        }
        if (_buttonImage == null)
        {
            Debug.LogWarning($"按钮 {button.name} 缺少 Image 组件，颜色动画将失效！");
        }

        // 记录原始状态
        _originalScale = _rt?.localScale ?? Vector3.one;
        _originalColor = _buttonImage?.color ?? Color.white;

        // 初始化动画序列
        _animaSequence = DOTween.Sequence();
    }

    /// <summary>
    /// 播放选中动画
    /// </summary>
    private void PlayChooseAnima()
    {
        if (_rt == null) return;

        // 清空原有动画
        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence();

        // 缩放+颜色动画同步执行
        _animaSequence.Append(_rt.DOScale(ChooseScale, ChangeDuration).SetEase(Ease.OutQuad));
        if (_buttonImage != null)
        {
            _animaSequence.Join(_buttonImage.DOColor(ChooseColor, ChangeDuration).SetEase(Ease.OutQuad));
        }
    }

    /// <summary>
    /// 播放取消选中动画
    /// </summary>
    private void PlayCancelAnima()
    {
        if (_rt == null) return;

        // 清空原有动画
        _animaSequence?.Kill();
        _animaSequence = DOTween.Sequence();

        // 恢复原始缩放+颜色
        _animaSequence.Append(_rt.DOScale(_originalScale, ChangeDuration).SetEase(Ease.InQuad));
        if (_buttonImage != null)
        {
            _animaSequence.Join(_buttonImage.DOColor(_originalColor, ChangeDuration).SetEase(Ease.InQuad));
        }
    }

    /// <summary>
    /// 清空动画序列
    /// </summary>
    public void ClearAnimaSequence()
    {
        _animaSequence?.Kill();
        _animaSequence = null;
    }
}
#endregion