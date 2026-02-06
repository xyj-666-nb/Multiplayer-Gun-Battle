using System.Collections.Generic;
using UnityEngine.Events;
/// <summary>
/// 用里氏替换原则，装载子类的父类
/// </summary>
public abstract class BaseEventInfo { };

/// <summary>
/// 用来包裹对应观察者函数委托的类
/// </summary>
/// <typeparam name="T"></typeparam>
public class EventInfo<T> : BaseEventInfo
{
    public EventInfo(UnityAction<T> EventAction)
    {
        this.EventAction = EventAction;
    }
    //真正的观察者对应的函数信息 记录在其中
    public UnityAction<T> EventAction;
}

/// <summary>
/// 这是无参的事件信息类
/// </summary>

public class EventInfo : BaseEventInfo
{
    public UnityAction EventAction;
    public EventInfo(UnityAction EventAction)
    {
        this.EventAction = EventAction;
    }
}


public class EventCenter:SingleBehavior<EventCenter>
{
    private Dictionary<E_EventType, BaseEventInfo> EventCenterDir = new Dictionary<E_EventType, BaseEventInfo>();


    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="Name"></param>
    public void TriggerEvent<T>(E_EventType Name, T Info)//默认空
    {
        //如果存在关心我的事件，就触发它
        if (EventCenterDir.ContainsKey(Name))
            (EventCenterDir[Name] as EventInfo<T>).EventAction?.Invoke(Info);
    }
    public void TriggerEvent(E_EventType Name)//默认空
    {
        //如果存在关心我的事件，就触发它
        if (EventCenterDir.ContainsKey(Name))
            (EventCenterDir[Name] as EventInfo).EventAction?.Invoke();
    }

    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Event"></param>
    public void AddEventLister<T>(E_EventType Name, UnityAction<T> Event)
    {
        if (EventCenterDir.ContainsKey(Name))
        {
            (EventCenterDir[Name] as EventInfo<T>).EventAction += Event; //如果存在就添加
        }
        else
        {
            EventCenterDir.Add(Name, new EventInfo<T>(Event));
        }

    }

    public void AddEventLister(E_EventType Name, UnityAction Event)
    {
        if (EventCenterDir.ContainsKey(Name))
        {
            (EventCenterDir[Name] as EventInfo).EventAction += Event; //如果存在就添加
        }
        else
        {
            EventCenterDir.Add(Name, new EventInfo(Event));
        }

    }

    /// <summary>
    /// 移除事件监听
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Event"></param>
    public void RemoveEventLister<T>(E_EventType Name, UnityAction<T> Event)
    {
        if (EventCenterDir.ContainsKey(Name))
        {
            (EventCenterDir[Name] as EventInfo<T>).EventAction -= Event;
            if (EventCenterDir[Name] == null)
                EventCenterDir.Remove(Name);
        }
    }

    public void RemoveEventLister(E_EventType Name, UnityAction Event)
    {
        if (EventCenterDir.ContainsKey(Name))
        {
            (EventCenterDir[Name] as EventInfo).EventAction -= Event;
            if (EventCenterDir[Name] == null)
                EventCenterDir.Remove(Name);
        }
    }

    /// <summary>
    /// 清除所有事件监听
    /// </summary>
    public void ClearAllEvent()
    {
        EventCenterDir.Clear();
    }


    /// <summary>
    /// 清除单个事件监听
    /// </summary>
    /// <param name="Name"></param>
    public void ClearEvent(E_EventType Name)
    {
        if (EventCenterDir.ContainsKey(Name))
            EventCenterDir.Remove(Name);
    }

}
