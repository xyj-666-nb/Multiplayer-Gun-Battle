using Mirror;
using System.Collections.Concurrent;
using UnityEngine.Events;

/// <summary>
/// 发送消息管理器
/// </summary>
public class SendMessageManger : SingleBehavior<SendMessageManger>
{
    #region 核心字段
    private ConcurrentQueue<MessagePack> _messagePackList = new ConcurrentQueue<MessagePack>();
    public UnityAction OnMessageAdded;
    #endregion

    #region 发送消息以及获取消息的方法
    // 入队消息
    public void SendMessage(string content, float duration)
    {
        // 边界校验：内容为空/时长<=0直接返回
        if (string.IsNullOrEmpty(content) || duration <= 0)
        {
            return;
        }

        var message = PoolManage.Instance.GetObj<MessagePack>();
        message.Init(content, duration);
        _messagePackList.Enqueue(message);

        // 触发事件，让UI自己处理
        OnMessageAdded?.Invoke();
        var panel = UImanager.Instance.ShowPanel<SendMessagePanel>();
            panel.SendMessage();
    }

    // 出队消息
    public MessagePack GetAMessage()
    {
        if (_messagePackList.TryDequeue(out var msg))
        {
            return msg;
        }
        return null;
    }

    #endregion
}

#region 消息数据包类
public class MessagePack : IPoolObject
{
    public string Content;
    public float Duration;
    public int CurrentIndex;//当前消息的索引
    public bool IsInRecycle=false;//是否正在回收

    public void Init(string content, float duration)
    {
        Content = content;
        Duration = duration;
        CurrentIndex = -1; // 初始化索引为无效值
    }

    public void ReSetDate()
    {
        Content = null;
        Duration = 0;
        CurrentIndex = -1; 
    }
}
#endregion
