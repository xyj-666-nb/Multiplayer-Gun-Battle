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

    // 新增：核心防护 - 标记面板是否已销毁/无效
    private bool _isPanelInvalid = false;
    #endregion

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        // 订阅消息添加事件（增加空值检查）
        if (SendMessageManger.Instance != null)
        {
            SendMessageManger.Instance.OnMessageAdded += OnMessageAdded;
        }
        else
        {
            Debug.LogError("[SendMessagePanel] SendMessageManger.Instance 为空，无法订阅消息事件！");
        }
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
        // 核心防护：面板无效时直接返回，不执行任何逻辑
        if (_isPanelInvalid) return;
        base.Update();
    }

    protected override void OnDestroy()
    {
        // 标记面板为无效，拦截所有后续逻辑
        _isPanelInvalid = true;
        Debug.Log("[SendMessagePanel] 面板已销毁，标记为无效");

        ClearAllMessages();
        // 取消事件订阅（增加空值检查）
        if (SendMessageManger.Instance != null)
        {
            SendMessageManger.Instance.OnMessageAdded -= OnMessageAdded;
        }
        // 清理所有动画
        foreach (var seq in _messageAnimaSeqDic.Values)
        {
            seq?.Kill();
        }
        _messageAnimaSeqDic.Clear();
        MessageInfoDic.Clear();
        WaitRemoveList.Clear();

        base.OnDestroy();
    }
    #endregion

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        if (_isPanelInvalid)
        {
            callback?.Invoke();
            return;
        }
        // 隐藏时清理所有消息
        ClearAllMessages();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        if (_isPanelInvalid) return;
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
        if (_isPanelInvalid || IsInAnimator)
            return;
        SendMessage();
    }

    // 发送一条消息（增加空值防护）
    public void SendMessage()
    {
        if (_isPanelInvalid || CurrentMessageCount >= MaxMessageCount)
            return;//超过最大消息数量，直接返回
        CreateMessage();
    }
    #endregion

    #region 消息UI创建（全链路空值检查）

    // 创建消息UI
    public GameObject CreateMessage()
    {
        // 核心防护：面板无效直接返回
        if (_isPanelInvalid) return null;

        // 空值检查：管理器为空
        if (SendMessageManger.Instance == null)
        {
            Debug.LogError("[SendMessagePanel] SendMessageManger.Instance 为空，无法获取消息");
            return null;
        }

        var info = SendMessageManger.Instance.GetAMessage();
        if (info == null)//当前没有消息
            return null;

        // 空值检查：预制体为空
        if (MessagePrefab == null)
        {
            Debug.LogError("[SendMessagePanel] MessagePrefab 未赋值！");
            return null;
        }

        var messageObj = PoolManage.Instance.GetObj(MessagePrefab);
        // 空值检查：对象池获取失败
        if (messageObj == null)
        {
            Debug.LogError("[SendMessagePanel] 消息预制体从对象池获取失败！");
            return null;
        }

        // 核心防护：检查自身transform是否有效（解决第112行报错）
        if (this == null || this.transform == null)
        {
            Debug.LogError("[SendMessagePanel] 面板transform已销毁，无法设置消息父物体");
            PoolManage.Instance.PushObj(MessagePrefab, messageObj);
            return null;
        }

        messageObj.transform.SetParent(this.transform);
        messageObj.transform.localScale = Vector3.one; // 重置缩放，避免异常

        var textComp = messageObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textComp == null)
        {
            Debug.LogError("[SendMessagePanel] 消息UI缺少TextMeshProUGUI组件！");
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

    #region 消息动画及位置调整（增加空值防护）

    // 消息进入动画
    public void MessageEnterAnima(GameObject message)
    {
        if (_isPanelInvalid || message == null || !MessageInfoDic.ContainsKey(message))
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
        if (rt == null)
        {
            Debug.LogError("[SendMessagePanel] 消息对象缺少RectTransform组件！");
            return;
        }
        rt.anchoredPosition = new Vector2(0, StartPosY);
        seq.Join(rt.DOAnchorPosY(-MessageInfoDic[message].CurrentIndex * 40, 1f))
           .OnComplete(() =>
           {
               // 核心防护：面板/消息无效时直接返回
               if (_isPanelInvalid || message == null || !MessageInfoDic.ContainsKey(message))
                   return;

               // 计时结束添加到待移除列表
               var duration = MessageInfoDic[message].Duration * 1000;
               if (duration <= 0)
                   duration = 3000; // 默认显示3秒
               CountDownManager.Instance.CreateTimer(false, (int)duration, () =>
               {
                   if (!_isPanelInvalid && message != null && !WaitRemoveList.Contains(message))
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
        if (_isPanelInvalid || message == null || !MessageInfoDic.ContainsKey(message))
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
        if (rt == null)
        {
            Debug.LogError("[SendMessagePanel] 消息对象缺少RectTransform组件！");
            return;
        }
        seq.Join(rt.DOAnchorPosX(EndPosX, 1f))
           .OnComplete(() =>
           {
               if (!_isPanelInvalid && message != null)
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
        if (_isPanelInvalid)
        {
            isAdjustingPos = false;
            return;
        }
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

    #region 消息移除/清除方法（增加空值防护）

    // 移除消息
    public void RemoveMessage(GameObject message)
    {
        if (_isPanelInvalid || message == null || !MessageInfoDic.ContainsKey(message))
            return;

        var removeIndex = MessageInfoDic[message].CurrentIndex;
        if (_messageAnimaSeqDic.ContainsKey(message))
        {
            _messageAnimaSeqDic[message]?.Kill();
            _messageAnimaSeqDic.Remove(message);
        }
        // 空值检查：MessagePack是否有效
        if (MessageInfoDic[message] != null)
        {
            PoolManage.Instance.PushObj(MessageInfoDic[message]);
        }
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
        if (_isPanelInvalid || WaitRemoveList.Count == 0 || isAdjustingPos) return;

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
        if (_isPanelInvalid) return;

        isAdjustingPos = false;
        WaitRemoveList.Clear();

        foreach (var item in MessageInfoDic.Keys)
        {
            if (item != null)
            {
                if (_messageAnimaSeqDic.ContainsKey(item))
                {
                    _messageAnimaSeqDic[item]?.Kill();
                }
                PoolManage.Instance.PushObj(MessagePrefab, item);
            }
        }
        _messageAnimaSeqDic.Clear();
        MessageInfoDic.Clear();
        CurrentMessageCount = 0;
    }

    public void ForceCleanupOnExit()
    {
        _isPanelInvalid = true;
        ClearAllMessages();
        Debug.Log("[SendMessagePanel] 执行退出清理，强制置空所有引用");
    }
    #endregion
}