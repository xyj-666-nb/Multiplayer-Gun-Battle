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

    public SimpleEffectButtonGroupPack RegisterGroup(string groupName, List<Button> buttons)
    {
        if (GetGroupByName(groupName) != null)
        {
            Debug.LogWarning($"注册失败：组名 [{groupName}] 已存在！");
            return null;
        }

        SimpleEffectButtonGroupPack newGroup = new SimpleEffectButtonGroupPack(groupName, buttons);
        AllButtonGroups.Add(newGroup);
        newGroup.Init();
        return newGroup;
    }

    public SimpleEffectButtonGroupPack RegisterGroup(SimpleEffectButtonGroupPack group)
    {
        if (group == null) return null;
        if (!string.IsNullOrEmpty(group.GroupName) && GetGroupByName(group.GroupName) != null)
        {
            Debug.LogWarning($"注册失败：组名 [{group.GroupName}] 已存在！");
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
        if (group == null) { Debug.LogError($"未找到组 [{groupName}]"); return false; }
        return group.AddSingleButton(button);
    }

    public bool RemoveButtonFromGroup(string groupName, Button button)
    {
        var group = GetGroupByName(groupName);
        if (group == null) { Debug.LogError($"未找到组 [{groupName}]"); return false; }
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

    #endregion

    private void OnDestroy()
    {
        foreach (var group in AllButtonGroups) group?.ClearEvents();
        DOTween.Kill(this);
    }
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
    public ButtonState BallState;   // 回弹 (仅作为动画过渡使用)

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
        foreach (var kvp in _btnInfoDict) CleanUpButton(kvp.Key, kvp.Value.Listener);
        _btnInfoDict.Clear();
    }

    #endregion

    #region 单个按钮添加/移除

    public bool AddSingleButton(Button btn)
    {
        if (btn == null) return false;
        if (_btnInfoDict.ContainsKey(btn)) { Debug.LogWarning($"按钮 [{btn.name}] 已在组 [{GroupName}] 中"); return false; }
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
            CleanUpButton(btn, info.Listener);
            _btnInfoDict.Remove(btn);
            return true;
        }
        return false;
    }

    private void AddSingleButtonInternal(Button btn)
    {
        if (btn == null) return;

        Graphic graphic = btn.GetComponent<Graphic>();
        if (graphic == null) Debug.LogWarning($"按钮 {btn.name} 没有 Graphic 组件！", btn);

        SimpleEffectButtonListener listener = btn.gameObject.GetOrAddComponent<SimpleEffectButtonListener>();

        // 创建状态信息
        ButtonInteractionInfo info = new ButtonInteractionInfo { Listener = listener, IsPressed = false, IsHovered = false };

        // 绑定事件 (使用闭包捕获 info)
        listener.OnPointerEnterEvent -= () => OnBtnEnter(btn, info);
        listener.OnPointerExitEvent -= () => OnBtnExit(btn, info);
        listener.OnPointerDownEvent -= () => OnBtnDown(btn, info);
        listener.OnPointerUpEvent -= () => OnBtnUp(btn, info);

        listener.OnPointerEnterEvent += () => OnBtnEnter(btn, info);
        listener.OnPointerExitEvent += () => OnBtnExit(btn, info);
        listener.OnPointerDownEvent += () => OnBtnDown(btn, info);
        listener.OnPointerUpEvent += () => OnBtnUp(btn, info);

        _btnInfoDict.Add(btn, info);
        SetStateImmediately(btn, DefaultState);
    }

    private void CleanUpButton(Button btn, SimpleEffectButtonListener listener)
    {
        if (listener != null)
        {
            listener.OnPointerEnterEvent = null;
            listener.OnPointerExitEvent = null;
            listener.OnPointerDownEvent = null;
            listener.OnPointerUpEvent = null;
        }

        if (btn != null)
        {
            btn.transform.DOKill();
            if (btn.TryGetComponent(out Graphic graphic))
            {
                graphic.DOKill();
                graphic.color = Color.white;
            }
            btn.transform.localScale = Vector3.one;
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

    #region 状态转换核心逻辑 (DOTween 动画曲线优化版)

    /// <summary>
    /// 过渡到指定状态（根据状态类型自动选择动画曲线）
    /// </summary>
    public void ConvertState(Button btn, ButtonState targetState, TweenCallback onComplete = null)
    {
        if (btn == null || targetState == null) return;

        btn.transform.DOKill();
        if (btn.TryGetComponent(out Graphic graphic)) graphic.DOKill();

        // --- 核心优化：根据不同状态选择不同的动画时长和曲线 ---
        float duration = 0.15f;
        Ease scaleEase = Ease.OutQuad;
        Ease colorEase = Ease.OutQuad;

        switch (targetState.StateType)
        {
            case ButtonStateType.Press:
                // 按下：要快，要干脆，有瞬间被按下去的感觉
                duration = 0.08f;
                scaleEase = Ease.OutQuad; // 快速下沉
                colorEase = Ease.OutQuad;
                break;

            case ButtonStateType.Ball:
                // 回弹：这是重点！使用 OutBack 产生弹性效果
                // 稍微拉长一点时间让弹性播完
                duration = 0.25f;
                scaleEase = Ease.OutBack; // 关键：OutBack 会先超过目标值，再弹回来，形成果冻感
                colorEase = Ease.OutQuad;
                break;

            case ButtonStateType.Stay:
            case ButtonStateType.Default:
            default:
                // 悬停/默认：平滑过渡即可
                duration = 0.15f;
                scaleEase = Ease.OutQuad;
                colorEase = Ease.OutQuad;
                break;
        }

        // 执行缩放动画
        btn.transform.DOScale(targetState.StateScale, duration)
            .SetEase(scaleEase)
            .SetLink(btn.gameObject);

        // 执行颜色动画
        if (graphic != null)
        {
            var tween = graphic.DOColor(targetState.StateColor, duration)
                .SetEase(colorEase)
                .SetLink(btn.gameObject);

            if (onComplete != null)
            {
                tween.OnComplete(onComplete);
            }
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void SetStateImmediately(Button btn, ButtonState state)
    {
        if (btn == null || state == null) return;
        btn.transform.localScale = Vector3.one * state.StateScale;
        if (btn.TryGetComponent(out Graphic graphic)) graphic.color = state.StateColor;
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