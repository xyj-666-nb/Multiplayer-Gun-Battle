using System.Collections;
using System.Collections.Generic;
using System.Linq; // 必须添加，用于 ToList()
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 计时管理器
/// </summary>
public class CountDownManager : SingleMonoAutoBehavior<CountDownManager>
{
    #region 变量声明
    private int COUNTDOWN_KEY = 0;

    private Dictionary<int, TimerItem> TimerDic = new Dictionary<int, TimerItem>();
    private Dictionary<int, TimerItem> TimerDic_RealTime = new Dictionary<int, TimerItem>();

    private Coroutine CountDown;
    private Coroutine CountDown_RealTime;
    private const float intervalTime = 0.1f;

    private WaitForSecondsRealtime waitForSecondsRealtime;
    private WaitForSeconds waitForSeconds;
    #endregion

    #region 开启/停止计时器
    protected void Start()
    {
        waitForSecondsRealtime = new WaitForSecondsRealtime(intervalTime);
        waitForSeconds = new WaitForSeconds(intervalTime);

        CountDown = StartCoroutine(StartTiming(false, TimerDic));
        CountDown_RealTime = StartCoroutine(StartTiming(true, TimerDic_RealTime));
    }

    public void Stop()
    {
        if (CountDown != null) StopCoroutine(CountDown);
        if (CountDown_RealTime != null) StopCoroutine(CountDown_RealTime);
    }
    #endregion

    #region 计时器主逻辑
    IEnumerator StartTiming(bool IsUseRealTime, Dictionary<int, TimerItem> TimerDic)
    {
        // 【修复2】在协程内部定义局部延迟列表，每个协程独立使用
        List<TimerItem> localDelayRemoveList = new List<TimerItem>();

        while (true)
        {
            if (IsUseRealTime)
                yield return waitForSecondsRealtime;
            else
                yield return waitForSeconds;

            // 【修复3】遍历前先复制一份 Values 的副本（ToList()）
            // 这样即使原字典被添加/移除元素，遍历也不会报错
            List<TimerItem> timersSnapshot = TimerDic.Values.ToList();

            foreach (var timer in timersSnapshot)
            {
                // 双重保险：检查计时器是否还在原字典中（可能已被其他逻辑移除）
                if (!TimerDic.ContainsKey(timer.keyID)) continue;
                if (!timer.IsRuning) continue;

                if (timer.ScheduleOverCallBack != null && timer.MaxIntervalTime > 0)
                {
                    timer.intervalTime -= (int)(intervalTime * 1000);
                    if (timer.intervalTime <= 0)
                    {
                        timer.ScheduleOverCallBack.Invoke();
                        timer.intervalTime = timer.MaxIntervalTime;
                    }
                }

                if (!timer.IsPermanent)
                {
                    timer.AllTime -= (int)(intervalTime * 1000);
                    if (timer.AllTime <= 0)
                    {
                        timer.AllTime = 0;
                        timer.OverCallBack?.Invoke();
                        timer.IsRuning = false;
                        // 【修复4】添加到局部列表，而不是全局列表
                        localDelayRemoveList.Add(timer);
                    }
                }
            }

            // 处理局部延迟移除列表
            for (int i = 0; i < localDelayRemoveList.Count; i++)
            {
                var timer = localDelayRemoveList[i];
                if (TimerDic.ContainsKey(timer.keyID))
                {
                    TimerDic.Remove(timer.keyID);
                    PoolManage.Instance.PushObj(timer);
                }
            }
            // 清空局部列表
            localDelayRemoveList.Clear();
        }
    }
    #endregion

    #region 创建单个计时器
    public int CreateTimer(bool IsUseRealTime, int AllTime, UnityAction OverCallback, int intervalTime = 0, UnityAction Callback = null)
    {
        if (AllTime <= 0)
        {
            Debug.LogError("计时器总时间必须大于0！");
            return -1;
        }
        if (OverCallback == null && Callback == null)
        {
            Debug.LogWarning("计时器未设置任何回调，创建无意义！");
        }

        int KeyID = COUNTDOWN_KEY++;
        TimerItem timer = PoolManage.Instance.GetObj<TimerItem>();
        timer.InitInfo(KeyID, AllTime, OverCallback, Callback, intervalTime);

        if (IsUseRealTime)
            TimerDic_RealTime.Add(KeyID, timer);
        else
            TimerDic.Add(KeyID, timer);

        return KeyID;
    }

    public int CreateTimer_Permanent(bool IsUseRealTime, int intervalTime, UnityAction Callback)
    {
        if (intervalTime <= 0)
        {
            Debug.LogError("永久间隔触发器的间隔时间必须大于0！");
            return -1;
        }
        if (Callback == null)
        {
            Debug.LogWarning("永久间隔触发器未设置回调，创建无意义！");
            return -1;
        }

        int KeyID = COUNTDOWN_KEY++;
        TimerItem timer = PoolManage.Instance.GetObj<TimerItem>();
        timer.InitInfo(KeyID, -1, null, Callback, intervalTime);

        if (IsUseRealTime)
            TimerDic_RealTime.Add(KeyID, timer);
        else
            TimerDic.Add(KeyID, timer);

        return KeyID;
    }
    #endregion

    #region 计时器控制方法
    public void RemoveTimer(int KeyId)
    {
        if (TimerDic.TryGetValue(KeyId, out var timer))
        {
            timer.IsRuning = false;
        }
        if (TimerDic_RealTime.TryGetValue(KeyId, out var timerRt))
        {
            timerRt.IsRuning = false;
        }
    }

    public void StopTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
            TimerDic[KeyId].IsRuning = false;
        if (TimerDic_RealTime.ContainsKey(KeyId))
            TimerDic_RealTime[KeyId].IsRuning = false;
    }

    public void StartTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
            TimerDic[KeyId].IsRuning = true;
        if (TimerDic_RealTime.ContainsKey(KeyId))
            TimerDic_RealTime[KeyId].IsRuning = true;
    }

    public void ReSetTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
            TimerDic[KeyId].ReSetTimer();
        if (TimerDic_RealTime.ContainsKey(KeyId))
            TimerDic_RealTime[KeyId].ReSetTimer();
    }
    #endregion

    #region 销毁处理
    protected override void OnDestroy()
    {
        Stop();
        base.OnDestroy();
    }
    #endregion
}