using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 警告面板触发管理器,在这里触发打开警告面板
/// </summary>
public class WarnTriggerManager : SingleBehavior<WarnTriggerManager>
{
    private bool _isBlockingPanelShowing = false;

    #region 触发交互面板的方法

    #region 触发无交互警告面板
    /// <summary>
    /// 触发无交互警告面板
    /// </summary>
    /// <param name="duration">显示时长（秒）</param>
    /// <param name="content">警告内容</param>
    /// <param name="callback">面板关闭后的自定义回调</param>
    /// <param name="isUseDefaultAnima">是否使用默认动画</param>
    public void TriggerNoInteractionWarn(float duration, string content, UnityAction callback = null, bool isUseDefaultAnima = true)
    {
        // 无交互面板不受互斥限制，直接触发
        UImanager.Instance.ShowPanel<WarnPanel>().StartWarnPanel_NoInteraction(duration, content, callback, isUseDefaultAnima);
    }
    #endregion

    #region 触发单交互警告面板
    /// <summary>
    /// 触发单交互警告面板
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="content">警告内容</param>
    /// <param name="confirmCallback">确认按钮回调</param>
    /// <param name="IsUseTimePause">是否暂停游戏</param>
    /// <param name="isUseDefaultAnima">是否使用默认动画</param>
    public void TriggerSingleInteractionWarn(string topic, string content, UnityAction confirmCallback = null, bool IsUseTimePause = true, bool isUseDefaultAnima = true)
    {
        if (_isBlockingPanelShowing)
        {
            Debug.LogWarning("当前已有阻断式警告面板显示，无法触发新的单交互警告！");
            return;
        }

        // 标记为正在显示阻断式面板
        _isBlockingPanelShowing = true;

        if (IsUseTimePause)
        {
            EventCenter.Instance.TriggerEvent(E_EventType.E_GamePause);//打开游戏暂停
            confirmCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false; // 重置互斥标识
            };
        }
        else
        {
            // 未暂停游戏时，仅重置互斥标识
            confirmCallback += () => _isBlockingPanelShowing = false;
        }

        UImanager.Instance.ShowPanel<WarnPanel>().StartWarnPanel_SingleInteractionGroup(topic, content, confirmCallback, isUseDefaultAnima);
    }
    #endregion

    #region 触发双交互警告面板
    /// <summary>
    /// 触发双交互警告面板
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="content">警告内容</param>
    /// <param name="cancelCallback">取消按钮回调</param>
    /// <param name="confirmCallback">确认按钮回调</param>
    /// <param name="IsUseTimePause">是否暂停游戏</param>
    /// <param name="isUseDefaultAnima">是否使用默认动画</param>
    public void TriggerDoubleInteractionWarn(string topic, string content, UnityAction cancelCallback, UnityAction confirmCallback, bool IsUseTimePause = true, bool isUseDefaultAnima = true)
    {
        if (_isBlockingPanelShowing)
        {
            Debug.LogWarning("当前已有阻断式警告面板显示，无法触发新的双交互警告！");
            return;
        }

        _isBlockingPanelShowing = true;

        if (IsUseTimePause)
        {
            EventCenter.Instance.TriggerEvent(E_EventType.E_GamePause);//打开游戏暂停
            confirmCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false;
            };
            cancelCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false;
            };
        }
        else
        {
            // 未暂停游戏时，仅重置互斥标识
            confirmCallback += () => _isBlockingPanelShowing = false;
            cancelCallback += () => _isBlockingPanelShowing = false;
        }

        UImanager.Instance.ShowPanel<WarnPanel>().StartWarnPanel_DoubleInteractionGroup(topic, content, cancelCallback, confirmCallback, isUseDefaultAnima);
    }
    #endregion

    #region 触发无Content版双交互警告面板
    /// <summary>
    /// 触发无Content版双交互警告面板
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="cancelCallback">取消按钮回调</param>
    /// <param name="confirmCallback">确认按钮回调</param>
    /// <param name="IsUseTimePause">是否暂停游戏</param>
    /// <param name="isUseDefaultAnima">是否使用默认动画</param>
    public void TriggerDoubleInteraction2Warn(string topic, UnityAction cancelCallback, UnityAction confirmCallback, bool IsUseTimePause = true, bool isUseDefaultAnima = true)
    {
        if (_isBlockingPanelShowing)
        {
            Debug.LogWarning("当前已有阻断式警告面板显示，无法触发新的无Content版双交互警告！");
            return;
        }

        // 标记为正在显示阻断式面板
        _isBlockingPanelShowing = true;

        // 处理游戏暂停/恢复逻辑
        if (IsUseTimePause)
        {
            EventCenter.Instance.TriggerEvent(E_EventType.E_GamePause);//打开游戏暂停
            // 确认按钮回调：恢复游戏 + 重置互斥标识
            confirmCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false;
            };
            // 取消按钮回调：恢复游戏 + 重置互斥标识
            cancelCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false;
            };
        }
        else
        {
            // 未暂停游戏时，仅重置互斥标识
            confirmCallback += () => _isBlockingPanelShowing = false;
            cancelCallback += () => _isBlockingPanelShowing = false;
        }

        // 触发无Content版双交互警告面板
        UImanager.Instance.ShowPanel<WarnPanel>().StartWarnPanel_DoubleInteractionGroup2(topic, cancelCallback, confirmCallback, isUseDefaultAnima);
    }
    #endregion

    #region 触发协议交互警告面板
    /// <summary>
    /// 触发协议交互警告面板
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="content">警告内容</param>
    /// <param name="cancelCallback">取消按钮回调</param>
    /// <param name="confirmCallback">确认按钮回调</param>
    /// <param name="IsUseTimePause">是否暂停游戏</param>
    /// <param name="isUseDefaultAnima">是否使用默认动画</param>
    public void TriggerDealInteractionWarn(string topic, string content, UnityAction cancelCallback, UnityAction confirmCallback, bool IsUseTimePause = true, bool isUseDefaultAnima = true)
    {
        if (_isBlockingPanelShowing)
        {
            Debug.LogWarning("当前已有阻断式警告面板显示，无法触发新的协议交互警告！");
            return;
        }

        // 标记为正在显示阻断式面板
        _isBlockingPanelShowing = true;

        if (IsUseTimePause)
        {
            EventCenter.Instance.TriggerEvent(E_EventType.E_GamePause);//打开游戏暂停
            confirmCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false;
            };
            cancelCallback += () =>
            {
                EventCenter.Instance.TriggerEvent(E_EventType.E_GameResume);
                _isBlockingPanelShowing = false;
            };
        }
        else
        {
            confirmCallback += () => _isBlockingPanelShowing = false;
            cancelCallback += () => _isBlockingPanelShowing = false;
        }

        UImanager.Instance.ShowPanel<WarnPanel>().StartWarnPanel_DealInteractionGroup(topic, content, cancelCallback, confirmCallback, isUseDefaultAnima);
    }
    #endregion

    #endregion

    /// <summary>
    /// 外部获取当前是否有阻断式面板显示
    /// </summary>
    public bool IsBlockingPanelShowing => _isBlockingPanelShowing;
}