using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 计时管理器
/// </summary>
public class CountDownManager : SingleMonoAutoBehavior<CountDownManager>
{

    #region 变量声明
    private int COUNTDOWN_KEY = 0;

    private Dictionary<int, TimerItem> TimerDic = new Dictionary<int, TimerItem>();//会受到Time.timeScale的影响
    private Dictionary<int, TimerItem> TimerDic_RealTime = new Dictionary<int, TimerItem>();//使用真实时间
    private List<TimerItem> DelayRemoveTimerList = new List<TimerItem>();//等待移除列表

    private Coroutine CountDown;
    private Coroutine CountDown_RealTime;
    private const float intervalTime = 0.1f;//计时器固定间隔时间计时

    //性能优化
    private WaitForSecondsRealtime waitForSecondsRealtime;
    private WaitForSeconds waitForSeconds;
    #endregion

    #region 开启/停止计时器
    protected void Start()
    {
        // 初始化等待对象（避免重复创建）
        waitForSecondsRealtime = new WaitForSecondsRealtime(intervalTime);
        waitForSeconds = new WaitForSeconds(intervalTime);

        // 改用自身的协程（避免依赖未定义的MonoMange）
        CountDown = StartCoroutine(StartTiming(false, TimerDic));
        CountDown_RealTime = StartCoroutine(StartTiming(true, TimerDic_RealTime));
    }

    //关闭计时器
    public void Stop()
    {
        if (CountDown != null)
            StopCoroutine(CountDown);
        if (CountDown_RealTime != null)
            StopCoroutine(CountDown_RealTime);
    }
    #endregion

    #region 计时器主逻辑
    IEnumerator StartTiming(bool IsUseRealTime, Dictionary<int, TimerItem> TimerDic)
    {
        while (true)
        {
            if (IsUseRealTime)
                yield return waitForSecondsRealtime;
            else
                yield return waitForSeconds;

            foreach (var timer in TimerDic.Values)
            {
                if (!timer.IsRuning)
                    continue;

                if (timer.ScheduleOverCallBack != null && timer.MaxIntervalTime > 0)
                {
                    timer.intervalTime -= (int)(intervalTime * 1000);
                    if (timer.intervalTime <= 0)
                    {
                        timer.ScheduleOverCallBack.Invoke();
                        timer.intervalTime = timer.MaxIntervalTime;//重置间隔时间
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
                        DelayRemoveTimerList.Add(timer);
                    }
                }
            }

            for (int i = 0; i < DelayRemoveTimerList.Count; i++)
            {
                var timer = DelayRemoveTimerList[i];
                if (TimerDic.ContainsKey(timer.keyID))
                {
                    TimerDic.Remove(timer.keyID);
                    PoolManage.Instance.PushObj(timer);
                }
            }
            DelayRemoveTimerList.Clear();
        }
    }
    #endregion

    #region 创建单个计时器
    /// <summary>
    /// 创建单个计时器
    /// </summary>
    /// <param name="IsUseRealTime">是否使用真实时间（不受Time.timeScale影响）</param>
    /// <param name="AllTime">总时间（毫秒,毫秒:1秒等于1000毫秒）</param>
    /// <param name="OverCallback">时间结束回调</param>
    /// <param name="intervalTime">间隔触发时间（毫秒，0则不触发,毫秒:1秒等于1000毫秒）</param>
    /// <param name="Callback">间隔触发回调</param>
    /// <returns>计时器唯一ID</returns>
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

        // 添加到对应字典
        if (IsUseRealTime)
            TimerDic_RealTime.Add(KeyID, timer);
        else
            TimerDic.Add(KeyID, timer);

        return KeyID;
    }

    /// <summary>
    /// 创建永久间隔触发器
    /// </summary>
    /// <param name="IsUseRealTime">是否使用真实时间（不受Time.timeScale影响）</param>
    /// <param name="OverCallback">时间结束回调</param>
    /// <param name="intervalTime">间隔触发时间（毫秒，0则不触发,毫秒:1秒等于1000毫秒）</param>
    /// <param name="Callback">间隔触发回调</param>
    /// <returns>计时器唯一ID</returns>
    public int CreateTimer_Permanent(bool IsUseRealTime, int intervalTime, UnityAction Callback)
    {
        // 校验：间隔时间必须大于0
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
    /// <summary>
    /// 移除计时器
    /// </summary>
    /// <param name="KeyId">计时器唯一ID</param>
    public void RemoveTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
        {
            TimerDic[KeyId].IsRuning = false;
            DelayRemoveTimerList.Add(TimerDic[KeyId]);
        }
        if (TimerDic_RealTime.ContainsKey(KeyId))
        {
            TimerDic_RealTime[KeyId].IsRuning = false;
            DelayRemoveTimerList.Add(TimerDic_RealTime[KeyId]);
        }
    }

    /// <summary>
    /// 停止单个计时器
    /// </summary>
    /// <param name="KeyId">计时器唯一ID</param>
    public void StopTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
            TimerDic[KeyId].IsRuning = false;
        if (TimerDic_RealTime.ContainsKey(KeyId))
            TimerDic_RealTime[KeyId].IsRuning = false;
    }

    /// <summary>
    /// 开启单个计时器
    /// </summary>
    /// <param name="KeyId">计时器唯一ID</param>
    public void StartTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
            TimerDic[KeyId].IsRuning = true;
        if (TimerDic_RealTime.ContainsKey(KeyId))
            TimerDic_RealTime[KeyId].IsRuning = true;
    }

    /// <summary>
    /// 重置单个计时器
    /// </summary>
    /// <param name="KeyId">计时器唯一ID</param>
    public void ReSetTimer(int KeyId)
    {
        if (TimerDic.ContainsKey(KeyId))
            TimerDic[KeyId].ReSetTimer();
        if (TimerDic_RealTime.ContainsKey(KeyId))
            TimerDic_RealTime[KeyId].ReSetTimer();
    }
    #endregion

    #region 销毁处理

    // 场景销毁时停止协程，避免内存泄漏
    protected override void OnDestroy()
    {
        Stop();
        base.OnDestroy();
    }
    #endregion
}