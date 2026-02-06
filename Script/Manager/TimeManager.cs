using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间缩放包实体类
/// 封装单个时间缩放效果的参数和生命周期状态
/// </summary>
public class TimeChangePack
{

    public float timeScale;// 该时间包的时间缩放值
    public float duration;// 时间缩放效果的持续时长，≤0时为永久生效
    public float elapsedTime;// 该时间包已流逝的时间
    public int ID;// 时间包唯一标识ID

    #region 构造函数
    /// <summary>
    /// 时间缩放包构造函数
    /// </summary>
    /// <param name="_timeScale">时间缩放值</param>
    /// <param name="_duration">持续时长</param>
    /// <param name="_elapsedTime">初始已流逝时间</param>
    /// <param name="_ID">唯一ID</param>
    public TimeChangePack(float _timeScale, float _duration, float _elapsedTime, int _ID)
    {
        // 初始化时限制时间缩放值在合法范围，避免异常值
        timeScale = TimeManage.ClampTimeScale(_timeScale);
        duration = _duration;
        elapsedTime = _elapsedTime;
        ID = _ID;
    }

    #endregion

    #region 更新包状态方法

    /// <summary>
    /// 每帧更新时间包的计时状态
    /// </summary>
    public void PackUpdate()
    {
        elapsedTime += Time.unscaledDeltaTime;
        if (duration > 0f && elapsedTime >= duration)
            TimeManage.WaitRemoveList.Add(this);
    }

    #endregion
}

/// <summary>
/// 时间管理单例类
/// 核心功能：
/// 基于时间缩放包管理全局Time.timeScale
/// 支持高/低优先级时间效果
/// 同优先级时间包采用乘法叠加时间缩放值
/// 支持游戏全局暂停/恢复、时间包精准增删、场景切换清空等
/// </summary>
public class TimeManage : SingleBehavior<TimeManage>
{
    #region 常量定义以及静态字段
    private const float MIN_TIME_SCALE = 0f;// 时间缩放最小值
    private const float MAX_TIME_SCALE = 100f;// 时间缩放最大值
    private static int IDCounter = 0;// 用于生成时间包唯一ID的计数器
    #endregion

    #region 状态字段
    private bool isTimePaused = false;// 标记是否存在生效的时间缩放效果
    private bool isGamePause = false;// 标记是否处于游戏全局暂停状态
    public float currentTimeScale = 1f;// 当前所有生效时间包叠加后的总时间缩放值
    #endregion

    #region 时间包列表
    private List<TimeChangePack> timeChangePacksList_LowPriority = new List<TimeChangePack>();// 低优先级时间缩放包列表
    public static List<TimeChangePack> WaitRemoveList = new List<TimeChangePack>();// 等待移除的时间包列表
    private List<TimeChangePack> timeChangePacksList_HightPriority = new List<TimeChangePack>();// 高优先级时间缩放包列表
    #endregion

    #region 构造哦函数
    public TimeManage()
    {
        // 与Mono管理器关联
        MonoMange.Instance.AddLister_Update(TimeUpdate);
    }

    #endregion

    #region 时间包管理方法

    #region 添加高/低优先级时间缩放包
    /// <summary>
    /// 添加低优先级时间缩放包
    /// </summary>
    /// <param name="timeScale">时间缩放值）</param>
    /// <param name="duration">持续时长（秒），≤0时永久生效，需手动调用RemoveTimeScalePack移除</param>
    /// <returns>时间包唯一ID</returns>
    public int AddTimeScalePack(float timeScale, float duration)
    {
        int ID = IDCounter++;
        TimeChangePack pack = new TimeChangePack(timeScale, duration, 0f, ID);
        timeChangePacksList_LowPriority.Add(pack);
        return ID;
    }

    /// <summary>
    /// 添加高优先级时间缩放包
    /// </summary>
    /// <param name="timeScale">时间缩放值）</param>
    /// <param name="duration">持续时长，≤0时永久生效，需手动调用RemoveTimeScalePack移除</param>
    /// <returns>时间包唯一ID</returns>
    public int AddTimeScalePack_HightPriority(float timeScale, float duration)
    {
        int ID = IDCounter++;
        TimeChangePack pack = new TimeChangePack(timeScale, duration, 0f, ID);
        timeChangePacksList_HightPriority.Add(pack);
        return ID;
    }

    #endregion

    #region 移除时间包方法

    /// <summary>
    /// 根据ID移除指定的时间缩放包
    /// </summary>
    /// <param name="ID">时间包唯一ID</param>
    public void RemoveTimeScalePack(int ID)
    {
        var Pack = GetTimeScalePack(ID);
        if (Pack == null)
        {
            Debug.LogError($"移除时间包失败，未找到ID为{ID}的时间包");
            return;
        }
        // 判断所属优先级列表并移除
        if (timeChangePacksList_LowPriority.Contains(Pack))
        {
            timeChangePacksList_LowPriority.Remove(Pack);
        }
        else if (timeChangePacksList_HightPriority.Contains(Pack))
        {
            timeChangePacksList_HightPriority.Remove(Pack);
        }
    }

    /// <summary>
    /// 清理等待移除列表中的所有时间包
    /// </summary>
    public void ClearWaitRemoveList()
    {
        if (WaitRemoveList.Count > 0)
        {
            foreach (var pack in WaitRemoveList)
            {
                RemoveTimeScalePack(pack.ID);// 通过ID精准移除
            }
            WaitRemoveList.Clear();// 清空等待列表
        }
    }

    #endregion

    #region 查询时间包方法

    /// <summary>
    /// 根据ID查询时间缩放包
    /// </summary>
    /// <param name="ID">时间包唯一ID</param>
    /// <returns>匹配的TimeChangePack，未找到返回Null并打印错误日志</returns>
    public TimeChangePack GetTimeScalePack(int ID)
    {
        // 优先查询高优先级列表
        foreach (var pack in timeChangePacksList_HightPriority)
        {
            if (pack.ID == ID)
            {
                return pack;
            }
        }
        // 再查询低优先级列表
        foreach (var pack in timeChangePacksList_LowPriority)
        {
            if (pack.ID == ID)
            {
                return pack;
            }
        }
        Debug.LogError($"并未发现ID为{ID}的时间包");
        return null;
    }
    #endregion

    #region 清空所有时间包
    /// <summary>
    /// 清空所有时间包
    /// </summary>
    public void ClearAllTimePacks()
    {
        timeChangePacksList_HightPriority.Clear();
        timeChangePacksList_LowPriority.Clear();
        WaitRemoveList.Clear();
        // 重置状态和时间缩放
        isTimePaused = false;
        currentTimeScale = 1f;
        Time.timeScale = 1f;
    }
    #endregion

    #endregion

    #region 核心更新逻辑
    /// <summary>
    /// 时间管理器核心更新逻辑
    /// 逻辑流程：
    /// 1. 全局暂停则直接返回
    /// 2. 无时间包则重置时间缩放为1
    /// 3. 高优先级完全优先处理，无高优先级时处理低优先级
    /// 4. 清理到期时间包，更新全局Time.timeScale
    /// </summary>
    public void TimeUpdate()
    {
        // 游戏全局暂停时，不处理任何时间包逻辑
        if (isGamePause)
            return;

        // 有生效标记但无任何时间包时，重置时间缩放为正常状态
        if (isTimePaused && timeChangePacksList_HightPriority.Count == 0 && timeChangePacksList_LowPriority.Count == 0)
        {
            isTimePaused = false;
            Time.timeScale = 1f;
            currentTimeScale = 1f;
            return;
        }

        // 每帧重置叠加值，避免无限累积
        currentTimeScale = 1f;
        // 高优先级完全优先：有高优先级包时仅处理高优先级，否则处理低优先级
        if (timeChangePacksList_HightPriority.Count > 0)
            HandleTimePackList(timeChangePacksList_HightPriority);
        else
            HandleTimePackList(timeChangePacksList_LowPriority);

        // 清理到期的时间包，更新全局时间缩放值
        ClearWaitRemoveList();
        UpdateCurrenTimeInfo();
    }

    /// <summary>
    /// 处理单个优先级的时间包列表
    /// 核心逻辑：同优先级时间包乘法叠加时间缩放值 + 每帧更新时间包计时
    /// </summary>
    /// <param name="PackList">待处理的时间包列表</param>
    public void HandleTimePackList(List<TimeChangePack> PackList)
    {
        // 标记存在生效的时间缩放效果
        isTimePaused = true;
        // 遍历列表，乘法叠加时间缩放值
        foreach (var pack in PackList)
        {
            currentTimeScale *= pack.timeScale;// 同优先级乘法叠加
            pack.PackUpdate();// 更新时间包计时状态
        }
    }

    /// <summary>
    /// 更新全局时间缩放值
    /// 注：全局暂停时不执行此逻辑
    /// </summary>
    private void UpdateCurrenTimeInfo()
    {
        if (!isGamePause)
        {
            // 限制时间缩放值在合法范围，避免异常
            var CurrentTimeScale = ClampTimeScale(currentTimeScale);
            // 应用到Unity全局时间缩放
            Time.timeScale = CurrentTimeScale;
        }
    }

    /// <summary>
    /// 限制时间缩放值在合法范围内
    /// 防止传入异常值导致时间逻辑出错
    /// </summary>
    /// <param name="timeScale">待限制的时间缩放值</param>
    /// <returns>限制后的合法时间缩放值</returns>
    public static float ClampTimeScale(float timeScale)
    {
        return Mathf.Clamp(timeScale, MIN_TIME_SCALE, MAX_TIME_SCALE);
    }

    #endregion

    #region 游戏全局暂停/恢复
    /// <summary>
    /// 游戏全局暂停
    /// </summary>
    public void GamePause()
    {
        if (!isGamePause)
        {
            isGamePause = true;     // 标记全局暂停
            Time.timeScale = 0f;    // 强制暂停所有时间相关逻辑
        }
    }

    /// <summary>
    /// 恢复游戏全局运行
    /// </summary>
    public void GameResume()
    {
        if (isGamePause)
        {
            isGamePause = false;    // 取消全局暂停标记
            Time.timeScale = 1f;    // 恢复正常时间缩放
        }
    }
    #endregion
}