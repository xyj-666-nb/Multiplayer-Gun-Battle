using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 发送消息面板
/// </summary>

public class SendMessagePanel : BasePanel
{
    #region 变量声名
    public int CurrentMessageCount = 0;//当前显示的消息数量
    public GameObject MessagePrefab;//消息预制体
    public int MaxMessageCount = 6;//最大能显示的消息数量
    public Dictionary<GameObject, MessagePack> MessageInfoDic = new Dictionary<GameObject, MessagePack>();
    private float StartPosY = -300;//消息起始位置
    private float EndPosX = -420;//消息结束位置X

    // 存储每个消息的动画序列，方便销毁
    private Dictionary<GameObject, Sequence> _messageAnimaSeqDic = new Dictionary<GameObject, Sequence>();
    private List<GameObject> WaitRemoveList = new List<GameObject>();
    private bool isAdjustingPos = false;
    #endregion

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        // 订阅消息添加事件
        SendMessageManger.Instance.OnMessageAdded += OnMessageAdded;
        // 边界校验：最大数量不能小于1
        if (MaxMessageCount < 1)
            MaxMessageCount = 1;
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearAllMessages();
        // 取消事件订阅，避免内存泄漏
        SendMessageManger.Instance.OnMessageAdded -= OnMessageAdded;
        // 清理所有动画
        foreach (var seq in _messageAnimaSeqDic.Values)
        {
            seq?.Kill();
        }
        _messageAnimaSeqDic.Clear();
        MessageInfoDic.Clear();
        WaitRemoveList.Clear();
    }
    #endregion

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        // 隐藏时清理所有消息
        ClearAllMessages();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        // 显示面板时，尝试加载队列中的消息
        SendMessage();
    }
    #endregion

    #region 面板特殊动画
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion

    #region 消息发送方法
    private void OnMessageAdded()
    {
        if (!IsInAnimator)
            SendMessage();
    }

    // 发送一条消息
    public void SendMessage()
    {
        if (CurrentMessageCount >= MaxMessageCount)
            return;//超过最大消息数量，直接返回
        CreateMessage();
    }
    #endregion

    #region 消息UI创建

    // 创建消息UI
    public GameObject CreateMessage()
    {
        var info = SendMessageManger.Instance.GetAMessage();
        if (info == null)//当前没有消息
            return null;

        var messageObj = PoolManage.Instance.GetObj(MessagePrefab);
        messageObj.transform.SetParent(this.transform);
        if (messageObj == null)
        {
            Debug.LogError("消息预制体获取失败！");
            return null;
        }
        var textComp = messageObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textComp == null)
        {
            Debug.LogError("消息UI缺少TextMeshProUGUI组件！");
            PoolManage.Instance.PushObj(MessagePrefab, messageObj);
            return null;
        }

        textComp.text = "";
        SimpleAnimatorTool.Instance.AddTypingTask(info.Content, textComp, 0.02f);
        MessageInfoDic.Add(messageObj, info);

        info.CurrentIndex = CurrentMessageCount;
        info.IsInRecycle = false; // 初始化回收标记
        MessageEnterAnima(messageObj);
        CurrentMessageCount++;

        return messageObj;
    }
    #endregion

    #region 消息动画及位置调整

    // 消息进入动画
    public void MessageEnterAnima(GameObject message)
    {
        if (!MessageInfoDic.ContainsKey(message))
            return;

        if (_messageAnimaSeqDic.ContainsKey(message))
        {
            _messageAnimaSeqDic[message]?.Kill();
            _messageAnimaSeqDic.Remove(message);
        }

        var seq = DOTween.Sequence();
        _messageAnimaSeqDic.Add(message, seq);

        var messageImage = message.GetComponent<Image>();
        if (messageImage != null)
        {
            messageImage.color = ColorManager.SetColorAlpha(messageImage.color, 0);
            seq.Append(messageImage.DOFade(0.4f, 0.5f));
        }

        var rt = message.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, StartPosY);
        seq.Join(rt.DOAnchorPosY(-MessageInfoDic[message].CurrentIndex * 40, 1f))
           .OnComplete(() =>
           {
               // 计时结束添加到待移除列表
               var duration = MessageInfoDic[message].Duration * 1000;
               if (duration <= 0)
                   duration = 3000; // 默认显示3秒
               CountDownManager.Instance.CreateTimer(false, (int)duration, () =>
               {
                   if (message != null && !WaitRemoveList.Contains(message))
                   {
                       WaitRemoveList.Add(message);
                       // 若未在调整位置，立即处理待移除列表
                       if (!isAdjustingPos)
                       {
                           ClearWaitRemoveMessage();
                       }
                   }
               });
           });
    }

    // 消息退出动画
    public void MessageExitAnima(GameObject message)
    {
        if (!MessageInfoDic.ContainsKey(message))
            return;

        // 清理旧动画
        if (_messageAnimaSeqDic.ContainsKey(message))
        {
            _messageAnimaSeqDic[message]?.Kill();
        }

        MessageInfoDic[message].IsInRecycle = true;//标记为正在回收
        // 创建退出动画序列
        var seq = DOTween.Sequence();
        var messageImage = message.GetComponent<Image>();
        if (messageImage != null)
        {
            seq.Append(messageImage.DOFade(0, 1f));
        }

        var rt = message.GetComponent<RectTransform>();
        seq.Join(rt.DOAnchorPosX(EndPosX, 1f))
           .OnComplete(() =>
           {
               if (message != null)
               {
                   RemoveMessage(message);
                   // 重置回收标记
                   if (MessageInfoDic.ContainsKey(message))
                   {
                       MessageInfoDic[message].IsInRecycle = false;
                   }
               }
           });
        _messageAnimaSeqDic[message] = seq;
    }

    // 更新消息位置
    public void UpdateCurrentMessagePos(int removeIndex)
    {
        // 正在调整位置则直接返回，避免重复调整
        if (isAdjustingPos) return;

        isAdjustingPos = true;
        int needAdjustCount = 0; // 需要调整的消息数量
        int adjustedCount = 0;    // 已完成调整的消息数量

        foreach (var item in MessageInfoDic)
        {
            if (item.Value.CurrentIndex > removeIndex && !item.Value.IsInRecycle)
            {
                needAdjustCount++;
            }
        }

        // 无需要调整的消息，直接处理待移除列表
        if (needAdjustCount == 0)
        {
            isAdjustingPos = false;
            ClearWaitRemoveMessage();
            CreateMessage();
            return;
        }

        foreach (var item in MessageInfoDic)
        {
            if (item.Value.CurrentIndex > removeIndex && !item.Value.IsInRecycle)
            {
                item.Value.CurrentIndex--;
                var rt = item.Key.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.DOAnchorPosY(-item.Value.CurrentIndex * 40, 0.5f)
                       .SetEase(Ease.OutQuad)
                       .OnComplete(() =>
                       {
                           adjustedCount++;
                           // 所有消息调整完成后处理待移除列表
                           if (adjustedCount >= needAdjustCount)
                           {
                               isAdjustingPos = false;
                               ClearWaitRemoveMessage();
                               CreateMessage();
                           }
                       });
                }
                else
                {
                    adjustedCount++;
                }
            }
        }
    }
    #endregion

    #region 消息移除/清除方法

    // 移除消息
    public void RemoveMessage(GameObject message)
    {
        if (!MessageInfoDic.ContainsKey(message))
            return;

        var removeIndex = MessageInfoDic[message].CurrentIndex;
        if (_messageAnimaSeqDic.ContainsKey(message))
        {
            _messageAnimaSeqDic[message]?.Kill();
            _messageAnimaSeqDic.Remove(message);
        }
        PoolManage.Instance.PushObj(MessageInfoDic[message]);
        MessageInfoDic.Remove(message);
        CurrentMessageCount--;
        // 移除后从待移除列表删除
        if (WaitRemoveList.Contains(message))
        {
            WaitRemoveList.Remove(message);
        }
        UpdateCurrentMessagePos(removeIndex);
        PoolManage.Instance.PushObj(MessagePrefab, message);
    }


    // 处理待移除消息列表
    public void ClearWaitRemoveMessage()
    {
        if (WaitRemoveList.Count == 0 || isAdjustingPos) return;

        // 遍历待移除列表，执行退出动画
        List<GameObject> tempList = new List<GameObject>(WaitRemoveList); // 避免遍历中修改列表
        foreach (var item in tempList)
        {
            if (item != null && MessageInfoDic.ContainsKey(item) && !MessageInfoDic[item].IsInRecycle)
            {
                MessageExitAnima(item);
                WaitRemoveList.Remove(item);
            }
        }
    }

    private void ClearAllMessages()
    {
        isAdjustingPos = false;
        WaitRemoveList.Clear();

        foreach (var item in MessageInfoDic.Keys)
        {
            if (_messageAnimaSeqDic.ContainsKey(item))
            {
                _messageAnimaSeqDic[item]?.Kill();
            }
            PoolManage.Instance.PushObj(MessagePrefab, item);
        }
        _messageAnimaSeqDic.Clear();
        MessageInfoDic.Clear();
        CurrentMessageCount = 0;
    }
    #endregion
}