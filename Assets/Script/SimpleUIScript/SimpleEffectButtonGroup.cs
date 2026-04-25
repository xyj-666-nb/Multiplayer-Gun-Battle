using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 按钮效果管理器（挂载在场景中）
/// </summary>
public class SimpleEffectButtonGroup : SingleMonoAutoBehavior<SimpleEffectButtonGroup>
{
    [Header("管理的所有按钮组")]
    public List<SimpleEffectButtonGroupPack> AllButtonGroups = new List<SimpleEffectButtonGroupPack>();

    #region 注册与创建组

    /// <summary>
    /// 注册按钮组
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="buttons">按钮列表</param>
    /// <param name="isNeedColorChange">是否需要颜色变化（默认true）</param>
    /// <param name="defaultScale">常规状态的缩放（默认1）</param>
    /// <param name="pressScale">按下时的缩放（默认0.85）</param>
    /// <param name="stayScale">悬停时的缩放（默认0.95）</param>
    public SimpleEffectButtonGroupPack RegisterGroup(string groupName, List<Button> buttons, bool isNeedColorChange = true, float defaultScale = 1f, float pressScale = 0.85f, float stayScale = 0.95f)
    {
        if (GetGroupByName(groupName) != null)
        {
            return null;
        }

        SimpleEffectButtonGroupPack newGroup = new SimpleEffectButtonGroupPack(groupName, buttons);
        // 应用自定义配置
        newGroup.IsNeedColorChange = isNeedColorChange;
        newGroup.CustomDefaultScale = defaultScale;
        newGroup.CustomPressScale = pressScale;
        newGroup.CustomStayScale = stayScale;

        AllButtonGroups.Add(newGroup);
        newGroup.Init();
        return newGroup;
    }

    public SimpleEffectButtonGroupPack RegisterGroup(SimpleEffectButtonGroupPack group)
    {
        if (group == null) return null;
        if (!string.IsNullOrEmpty(group.GroupName) && GetGroupByName(group.GroupName) != null)
        {
            return null;
        }

        if (!AllButtonGroups.Contains(group)) AllButtonGroups.Add(group);
        group.Init();
        return group;
    }

    #endregion

    #region 动态添加/移除单个按钮

    public bool AddButtonToGroup(string groupName, Button button)
    {
        var group = GetGroupByName(groupName);
        if (group == null) {  return false; }
        return group.AddSingleButton(button);
    }

    public bool RemoveButtonFromGroup(string groupName, Button button)
    {
        var group = GetGroupByName(groupName);
        if (group == null) {return false; }
        return group.RemoveSingleButton(button);
    }

    public void RemoveButtonFromAllGroups(Button button)
    {
        foreach (var group in AllButtonGroups) group.RemoveSingleButton(button);
    }

    #endregion

    #region 辅助：查找与移除组

    public SimpleEffectButtonGroupPack GetGroupByName(string groupName) => AllButtonGroups.FirstOrDefault(g => g.GroupName == groupName);

    public void UnRegisterGroup(string groupName)
    {
        var group = GetGroupByName(groupName);
        if (group != null) UnRegisterGroup(group);
    }

    public void UnRegisterGroup(SimpleEffectButtonGroupPack group)
    {
        if (group == null) return;
        group.ClearEvents();
        AllButtonGroups.Remove(group);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var group in AllButtonGroups) group?.ClearEvents();
        DOTween.Kill(this);
    }

    #endregion


}

#region 按钮组数据包

[System.Serializable]
public class SimpleEffectButtonGroupPack
{
    [Header("基础信息")]
    public string GroupName;
    public List<Button> ButtonGroup;

    [Header("状态设置")]
    public ButtonState DefaultState;
    public ButtonState StayState;   // 悬停
    public ButtonState PressState;  // 按下
    public ButtonState BallState;   // 回弹 

    [Header("自定义配置")]
    public bool IsNeedColorChange = false; // 是否需要颜色变化
    public float CustomDefaultScale = 1f;  // 自定义常规缩放
    public float CustomPressScale = 0.85f; // 自定义按下缩放
    public float CustomStayScale = 0.95f;  // 自定义悬停缩放

    // 内部存储：记录每个按钮的交互状态
    private Dictionary<Button, ButtonInteractionInfo> _btnInfoDict = new Dictionary<Button, ButtonInteractionInfo>();

    #region 构造函数

    public SimpleEffectButtonGroupPack() => InitDefaultStates();

    public SimpleEffectButtonGroupPack(string name, List<Button> buttons)
    {
        GroupName = name;
        ButtonGroup = buttons ?? new List<Button>();
        InitDefaultStates();
    }

    private void InitDefaultStates()
    {
        DefaultState = new ButtonState { StateColor = Color.white, StateScale = 1f, StateType = ButtonStateType.Default };
        StayState = new ButtonState { StateColor = new Color(1f, 1f, 1f, 0.6f), StateScale = 0.95f, StateType = ButtonStateType.Stay };
        PressState = new ButtonState { StateColor = new Color(0.7f, 1f, 0.7f, 1f), StateScale = 0.85f, StateType = ButtonStateType.Press };
        BallState = new ButtonState { StateColor = Color.white, StateScale = 1.1f, StateType = ButtonStateType.Ball };
    }

    #endregion

    #region 初始化与清理

    public void Init()
    {
        if (ButtonGroup == null || ButtonGroup.Count == 0) return;
        EnsureStatesNotNull();
        ClearEvents();
        foreach (var btn in ButtonGroup) AddSingleButtonInternal(btn);
    }

    private void EnsureStatesNotNull()
    {
        if (DefaultState == null) DefaultState = new ButtonState { StateColor = Color.white, StateScale = 1f };
        if (StayState == null) StayState = new ButtonState { StateColor = Color.white, StateScale = 1.05f };
        if (PressState == null) PressState = new ButtonState { StateColor = Color.white * 0.8f, StateScale = 0.95f };
        if (BallState == null) BallState = new ButtonState { StateColor = Color.white, StateScale = 1f };
    }

    public void ClearEvents()
    {
        foreach (var kvp in _btnInfoDict) CleanUpButton(kvp.Key, kvp.Value);
        _btnInfoDict.Clear();
    }

    #endregion

    #region 单个按钮添加/移除

    public bool AddSingleButton(Button btn)
    {
        if (btn == null) return false;
        if (_btnInfoDict.ContainsKey(btn)) { /* Debug.LogWarning($"按钮 [{btn.name}] 已在组 [{GroupName}] 中"); */ return false; }
        if (!ButtonGroup.Contains(btn)) ButtonGroup.Add(btn);
        AddSingleButtonInternal(btn);
        return true;
    }

    public bool RemoveSingleButton(Button btn)
    {
        if (btn == null) return false;
        ButtonGroup.Remove(btn);
        if (_btnInfoDict.TryGetValue(btn, out var info))
        {
            CleanUpButton(btn, info);
            _btnInfoDict.Remove(btn);
            return true;
        }
        return false;
    }

    private void AddSingleButtonInternal(Button btn)
    {
        if (btn == null) return;

        Graphic graphic = btn.GetComponent<Graphic>();

        SimpleEffectButtonListener listener = btn.gameObject.GetOrAddComponent<SimpleEffectButtonListener>();

        // 创建状态信息
        ButtonInteractionInfo info = new ButtonInteractionInfo
        {
            Listener = listener,
            IsPressed = false,
            IsHovered = false,
            BaseColor = graphic != null ? graphic.color : Color.white,
            BaseScale = btn.transform.localScale
        };

        // 绑定事件时保存委托引用，避免重复注册或错误移除导致状态错乱
        info.PointerEnterAction = () => OnBtnEnter(btn, info);
        info.PointerExitAction = () => OnBtnExit(btn, info);
        info.PointerDownAction = () => OnBtnDown(btn, info);
        info.PointerUpAction = () => OnBtnUp(btn, info);

        listener.OnPointerEnterEvent += info.PointerEnterAction;
        listener.OnPointerExitEvent += info.PointerExitAction;
        listener.OnPointerDownEvent += info.PointerDownAction;
        listener.OnPointerUpEvent += info.PointerUpAction;

        _btnInfoDict.Add(btn, info);
        SetStateImmediately(btn, DefaultState);
    }

    private void CleanUpButton(Button btn, ButtonInteractionInfo info)
    {
        SimpleEffectButtonListener listener = info != null ? info.Listener : null;
        if (listener != null)
        {
            if (info.PointerEnterAction != null) listener.OnPointerEnterEvent -= info.PointerEnterAction;
            if (info.PointerExitAction != null) listener.OnPointerExitEvent -= info.PointerExitAction;
            if (info.PointerDownAction != null) listener.OnPointerDownEvent -= info.PointerDownAction;
            if (info.PointerUpAction != null) listener.OnPointerUpEvent -= info.PointerUpAction;
        }

        if (btn != null)
        {
            btn.transform.DOKill();
            btn.transform.localScale = info != null ? info.BaseScale : Vector3.one;

            if (btn.TryGetComponent(out Graphic graphic))
            {
                graphic.DOKill();
                graphic.color = info != null ? info.BaseColor : Color.white;
            }
        }
    }

    #endregion

    #region 核心：修复后的交互逻辑

    private void OnBtnEnter(Button btn, ButtonInteractionInfo info)
    {
        info.IsHovered = true;
        if (info.IsPressed) return;
        ConvertState(btn, StayState);
    }

    private void OnBtnExit(Button btn, ButtonInteractionInfo info)
    {
        info.IsHovered = false;
        if (info.IsPressed) return;
        ConvertState(btn, DefaultState);
    }

    private void OnBtnDown(Button btn, ButtonInteractionInfo info)
    {
        info.IsPressed = true;
        ConvertState(btn, PressState);
    }

    private void OnBtnUp(Button btn, ButtonInteractionInfo info)
    {
        info.IsPressed = false;

        ConvertState(btn, BallState, () =>
        {
            if (info.IsHovered)
            {
                ConvertState(btn, StayState);
            }
            else
            {
                ConvertState(btn, DefaultState);
            }
        });
    }

    #endregion

    #region 状态转换核心逻辑 (支持自定义配置)

    /// <summary>
    /// 过渡到指定状态（仅处理颜色变化）
    /// </summary>
    public void ConvertState(Button btn, ButtonState targetState, TweenCallback onComplete = null)
    {
        if (btn == null || targetState == null) return;

        btn.transform.DOKill();
        if (btn.TryGetComponent(out Graphic graphic)) graphic.DOKill();

        float duration = 0.15f;
        Ease colorEase = Ease.OutQuad;

        switch (targetState.StateType)
        {
            case ButtonStateType.Press:
                duration = 0.08f;
                colorEase = Ease.OutQuad;
                break;

            case ButtonStateType.Ball:
                duration = 0.25f;
                colorEase = Ease.OutQuad;
                break;

            case ButtonStateType.Stay:
            case ButtonStateType.Default:
            default:
                duration = 0.15f;
                colorEase = Ease.OutQuad;
                break;
        }

        Vector3 baseScale = _btnInfoDict.TryGetValue(btn, out var info)
            ? info.BaseScale
            : Vector3.one;
        Vector3 targetScale = baseScale * targetState.StateScale;

        Tween scaleTween = btn.transform.DOScale(targetScale, duration)
            .SetEase(colorEase)
            .SetLink(btn.gameObject);

        if (graphic != null && IsNeedColorChange)
        {
            Tween colorTween = graphic.DOColor(targetState.StateColor, duration)
                .SetEase(colorEase)
                .SetLink(btn.gameObject);
            if (onComplete != null)
            {
                scaleTween.OnComplete(onComplete);
            }
            return;
        }

        if (graphic != null && !IsNeedColorChange)
        {
            graphic.color = info != null ? info.BaseColor : graphic.color;
        }

        if (onComplete != null)
        {
            scaleTween.OnComplete(onComplete);
            return;
        }
    }

    private void SetStateImmediately(Button btn, ButtonState state)
    {
        if (btn == null || state == null) return;

        if (_btnInfoDict.TryGetValue(btn, out var info))
        {
            btn.transform.localScale = info.BaseScale * state.StateScale;
        }

        if (btn.TryGetComponent(out Graphic graphic) && IsNeedColorChange)
        {
            graphic.color = state.StateColor;
        }
    }

    public ButtonState GetStateTypeInfo(ButtonStateType type)
    {
        switch (type)
        {
            case ButtonStateType.Default: return DefaultState;
            case ButtonStateType.Stay: return StayState;
            case ButtonStateType.Press: return PressState;
            case ButtonStateType.Ball: return BallState;
            default: return DefaultState;
        }
    }

    #endregion

    #region 内部辅助类：记录按钮状态

    private class ButtonInteractionInfo
    {
        public SimpleEffectButtonListener Listener;
        public bool IsPressed;
        public bool IsHovered;
        public Color BaseColor;
        public Vector3 BaseScale;
        public System.Action PointerEnterAction;
        public System.Action PointerExitAction;
        public System.Action PointerDownAction;
        public System.Action PointerUpAction;
    }

    #endregion
}

#endregion

#region 辅助类：状态数据与枚举

[System.Serializable]
public class ButtonState
{
    public Color StateColor = Color.white;
    public float StateScale = 1f;
    public ButtonStateType StateType;
}

public enum ButtonStateType
{
    Press,
    Ball,
    Default,
    Stay
}

#endregion

#region 内部工具：事件监听器

[DisallowMultipleComponent]
public class SimpleEffectButtonListener : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    public System.Action OnPointerEnterEvent;
    public System.Action OnPointerExitEvent;
    public System.Action OnPointerDownEvent;
    public System.Action OnPointerUpEvent;

    public void OnPointerEnter(PointerEventData eventData) => OnPointerEnterEvent?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => OnPointerExitEvent?.Invoke();
    public void OnPointerDown(PointerEventData eventData) => OnPointerDownEvent?.Invoke();
    public void OnPointerUp(PointerEventData eventData) => OnPointerUpEvent?.Invoke();
}

public static class SimpleEffectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}

#endregion
