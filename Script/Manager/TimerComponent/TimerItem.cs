using UnityEngine.Events;

public class TimerItem : IPoolObject
{
    #region 变量声明
    public int keyID;//唯一ID
    /// <summary>
    /// 定时器结束的回调函数
    /// </summary>
    public UnityAction OverCallBack;
    /// <summary>
    /// 间隔事件触发
    /// </summary>
    public UnityAction ScheduleOverCallBack;
    /// <summary>
    /// 计时器的总时间,毫秒:1秒等于1000毫秒
    /// </summary>
    public int AllTime;
    /// <summary>
    /// 记录当前的总时间，用于重置数据
    /// </summary>
    private int MaxAllTime;
    /// <summary>
    /// 间隔执行的时间，,毫秒:1秒等于1000毫秒
    /// </summary>
    public int intervalTime;
    /// <summary>
    /// 一开始记录的间隔执行时间，用于重置数据
    /// </summary>
    public int MaxIntervalTime;

    public bool IsRuning;//是否开始计时
    public bool IsPermanent;//是否永久
    #endregion

    #region 初始化方法

    /// <summary>
    /// 补全初始化方法
    /// </summary>
    /// <param name="Id">唯一ID</param>
    /// <param name="AllTime">总时间(传入-1意味无限时间)</param>
    /// <param name="OverCallBack">结束回调</param>
    /// <param name="ScheduleOverCallBack">间隔回调</param>
    /// <param name="intervalTime">间隔时间</param>
    public void InitInfo(int Id, int AllTime, UnityAction OverCallBack, UnityAction ScheduleOverCallBack = null, int intervalTime = 0)
    {
        this.keyID = Id;
        this.OverCallBack = OverCallBack;
        this.ScheduleOverCallBack = ScheduleOverCallBack;
        this.intervalTime = intervalTime;
        this.MaxIntervalTime = intervalTime;
        this.IsRuning = true;

        if (AllTime < 0)
        {
            IsPermanent = true;
            this.AllTime = int.MaxValue;
            this.MaxAllTime = int.MaxValue;
        }
        else
        {
            IsPermanent = false;
            this.AllTime = AllTime;
            this.MaxAllTime = AllTime;
        }
    }
    #endregion

    #region 重置计时器方法
    /// <summary>
    /// 重置计时器
    /// </summary>
    public void ReSetTimer()
    {
        AllTime = MaxAllTime;
        intervalTime = MaxIntervalTime;
        IsRuning = true;
    }

    /// <summary>
    /// 改为隐式实现接口，确保外部可调用
    /// </summary>
    public void ReSetDate()
    {
        OverCallBack = null;
        ScheduleOverCallBack = null;
        keyID = 0;
        AllTime = 0;
        MaxAllTime = 0;
        intervalTime = 0;
        MaxIntervalTime = 0;
        IsPermanent = false;
        IsRuning = false;
    }
    #endregion

}

// 补充接口定义
public interface IPoolObject
{
    void ReSetDate();//重置数据
}