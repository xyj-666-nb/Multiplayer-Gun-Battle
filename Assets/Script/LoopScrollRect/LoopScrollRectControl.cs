using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 无限滚动列表控制器
/// 【核心功能】封装LoopScrollRect的所有对外接口，提供统一、安全的调用入口
/// 【设计目的】屏蔽底层LoopScrollRect的复杂细节，简化外部调用，增加空值防护和日志提示
/// 【使用说明】挂载到包含LoopScrollRect组件的GameObject上，赋值item预制体即可使用
/// </summary>
[DisallowMultipleComponent]       // 禁止重复挂载
[RequireComponent(typeof(LoopScrollRect))] // 自动依赖LoopScrollRect组件
public class LoopScrollRectControl : MonoBehaviour,  LoopScrollPrefabSource,    LoopScrollDataSource     // 为列表项提供数据的接口
{
    #region 字段定义
    /// <summary>
    /// 绑定的无限滚动列表核心逻辑组件（LoopScrollRect的基类）
    /// 【自动赋值】Awake阶段会尝试从当前物体获取，无需手动绑定（除非特殊场景）
    /// </summary>
    [Header("无限滚动列表核心配置")]
    [Tooltip("循环列表核心逻辑组件（自动获取，无需手动赋值）")]
    public LoopScrollRectBase loopScrollRect;

    /// <summary>
    /// 列表项预制体（必须赋值）
    /// 【对象池关联】该预制体的创建/回收会通过PoolManage全局对象池完成
    /// </summary>
    [Tooltip("列表项预制体（必填：需包含ScrollCellIndex/ScrollCellReturn回调方法）")]
    public GameObject item;

    /// <summary>
    /// 列表总项数（默认-1表示无限模式）
    /// 【取值规则】负数=无限滚动；非负数=固定项数滚动
    /// </summary>
    [Tooltip("列表总项数（负数=无限模式，非负数=固定项数）")]
    public int totalCount = -1;
    #endregion

    #region 枚举定义（滚动模式）
    /// <summary>
    /// 滚动目标位置模式（与底层LoopScrollRectBase.ScrollMode一一对应）
    /// 【设计目的】对外暴露简洁的枚举，屏蔽底层组件的枚举细节
    /// </summary>
    public enum ScrollMode
    {
        /// <summary>滚动指定项到视口顶部/左侧（垂直/水平列表）</summary>
        ToStart,
        /// <summary>滚动指定项到视口中心（推荐用于选中项高亮场景）</summary>
        ToCenter,
        /// <summary>仅滚动到指定项出现在视口中（不保证具体位置）</summary>
        JustAppear
    }
    #endregion

    #region 生命周期方法（内部初始化）
    /// <summary>
    /// 初始化阶段：自动绑定核心组件，做基础校验
    /// 【执行时机】物体激活时优先执行，早于Start
    /// </summary>
    private void Awake()
    {
        // 自动获取当前物体上的LoopScrollRectBase组件（兼容实际挂载的LoopScrollRect子类）
        if (loopScrollRect == null)
        {
            loopScrollRect = GetComponent<LoopScrollRectBase>();

            // 空值校验：核心组件缺失时输出错误日志
            if (loopScrollRect == null)
            {
                Debug.LogError($"[{nameof(LoopScrollRectControl)}] 未找到LoopScrollRectBase组件！请确保物体挂载了LoopScrollRect相关组件", this);
            }
        }

        // 预制体空值提前校验（避免运行时才报错）
        if (item == null)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] 列表项预制体（item）未赋值！请在Inspector面板绑定", this);
        }
    }

    /// <summary>
    /// 启动阶段：初始化循环列表的核心配置
    /// 【执行时机】所有Awake执行完毕后执行
    /// </summary>
    private void Start()
    {
        // 获取实际的LoopScrollRect组件（接口绑定需要）
        var ls = GetComponent<LoopScrollRect>();

        // 空值防护：核心组件缺失时终止初始化
        if (ls == null)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 缺少LoopScrollRect组件！无法初始化循环列表", this);
            return;
        }

        // 绑定预制体源和数据源到当前控制器（核心步骤）
        ls.prefabSource = this;
        ls.dataSource = this;

        // 设置列表总项数并初始化填充
        ls.totalCount = totalCount;
        ls.RefillCells();

        Debug.Log($"[{nameof(LoopScrollRectControl)}] 循环列表初始化完成，总项数：{totalCount}", this);
    }
    #endregion

    #region 基础配置属性封装（对外提供安全的读写接口）
    /// <summary>
    /// 列表总项数（安全读写：包含空值校验）
    /// </summary>
    public int TotalCount
    {
        get
        {
            CheckScrollRectReference();
            return loopScrollRect.totalCount;
        }
        set
        {
            CheckScrollRectReference();
            loopScrollRect.totalCount = value;
        }
    }

    /// <summary>
    /// 是否反向滚动（垂直列表：从下到上；水平列表：从右到左）
    /// </summary>
    public bool ReverseDirection
    {
        get
        {
            CheckScrollRectReference();
            return loopScrollRect.reverseDirection;
        }
        set
        {
            CheckScrollRectReference();
            loopScrollRect.reverseDirection = value;
        }
    }

    /// <summary>
    /// 是否开启滚动惯性（滑动后是否继续减速滚动）
    /// </summary>
    public bool Inertia
    {
        get
        {
            CheckScrollRectReference();
            return loopScrollRect.inertia;
        }
        set
        {
            CheckScrollRectReference();
            loopScrollRect.inertia = value;
        }
    }

    /// <summary>
    /// 滚动减速速率（仅开启惯性时生效）
    /// 【取值范围】0~1，值越大减速越慢（建议使用默认值0.135）
    /// </summary>
    public float DecelerationRate
    {
        get
        {
            CheckScrollRectReference();
            return loopScrollRect.decelerationRate;
        }
        set
        {
            CheckScrollRectReference();
            loopScrollRect.decelerationRate = value;
        }
    }

    /// <summary>
    /// 滚轮/触摸滚动灵敏度
    /// 【取值建议】默认1.0，值越大滚动越快
    /// </summary>
    public float ScrollSensitivity
    {
        get
        {
            CheckScrollRectReference();
            return loopScrollRect.scrollSensitivity;
        }
        set
        {
            CheckScrollRectReference();
            loopScrollRect.scrollSensitivity = value;
        }
    }

    /// <summary>
    /// 滚动边界模式（Unrestricted=无限制/Elastic=弹性/Clamped=夹紧）
    /// </summary>
    public LoopScrollRectBase.MovementType MovementType
    {
        get
        {
            CheckScrollRectReference();
            return loopScrollRect.movementType;
        }
        set
        {
            CheckScrollRectReference();
            loopScrollRect.movementType = value;
        }
    }
    #endregion

    #region 核心操作方法封装（对外提供的主要功能）
    /// <summary>
    /// 清空列表中所有已生成的项
    /// 【注意事项】仅运行时有效，会触发列表项的回收（通过对象池）
    /// </summary>
    public void ClearCells()
    {
        // 前置校验：确保核心组件有效
        CheckScrollRectReference();

        // 运行时校验：编辑器模式下不执行
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(ClearCells)} 方法仅在运行时生效", this);
            return;
        }

        // 执行清空操作并输出日志
        loopScrollRect.ClearCells();
        Debug.Log($"[{nameof(LoopScrollRectControl)}] 已清空所有列表项", this);
    }

    /// <summary>
    /// 刷新当前可见列表项的数据
    /// 【适用场景】列表数据更新后，仅刷新已显示的项（不重建列表）
    /// </summary>
    public void RefreshCells()
    {
        CheckScrollRectReference();

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(RefreshCells)} 方法仅在运行时生效", this);
            return;
        }

        loopScrollRect.RefreshCells();
        Debug.Log($"[{nameof(LoopScrollRectControl)}] 已刷新可见列表项数据", this);
    }

    /// <summary>
    /// 从指定索引重新填充列表（清空原有项，重新生成）
    /// </summary>
    /// <param name="startItem">起始填充索引（默认0）</param>
    /// <param name="contentOffset">起始项相对于视口的偏移量（默认0）</param>
    public void RefillCells(int startItem = 0, float contentOffset = 0)
    {
        CheckScrollRectReference();

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(RefillCells)} 方法仅在运行时生效", this);
            return;
        }

        // 处理反向滚动的索引转换（反向时起始索引需要适配）
        int actualStartItem = loopScrollRect.reverseDirection ? (loopScrollRect.totalCount - startItem) : startItem;
        loopScrollRect.RefillCells(actualStartItem, contentOffset);

        Debug.Log($"[{nameof(LoopScrollRectControl)}] 从索引 {startItem} 重新填充列表（实际索引：{actualStartItem}）", this);
    }

    /// <summary>
    /// 从指定结束索引反向填充列表（从底部/右侧开始填充）
    /// </summary>
    /// <param name="endItem">结束填充索引（默认0）</param>
    /// <param name="contentOffset">结束项相对于视口的偏移量（默认0）</param>
    public void RefillCellsFromEnd(int endItem = 0, float contentOffset = 0)
    {
        CheckScrollRectReference();

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(RefillCellsFromEnd)} 方法仅在运行时生效", this);
            return;
        }

        // 反向滚动的索引转换
        int actualEndItem = loopScrollRect.reverseDirection ? endItem : (loopScrollRect.totalCount - endItem);
        loopScrollRect.RefillCellsFromEnd(actualEndItem, contentOffset);

        Debug.Log($"[{nameof(LoopScrollRectControl)}] 从索引 {endItem} 反向填充列表（实际索引：{actualEndItem}）", this);
    }

    /// <summary>
    /// 获取当前视口中可见的第一个列表项索引
    /// </summary>
    /// <param name="offset">第一个项相对于视口的偏移量（输出参数）</param>
    /// <returns>可见第一个项的索引（-1表示无可见项）</returns>
    public int GetFirstItem(out float offset)
    {
        CheckScrollRectReference();
        offset = 0;

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(GetFirstItem)} 方法仅在运行时生效", this);
            return -1;
        }

        int firstItem = loopScrollRect.GetFirstItem(out offset);
        Debug.Log($"[{nameof(LoopScrollRectControl)}] 可见第一个列表项索引：{firstItem}，偏移量：{offset:F2}", this);
        return firstItem;
    }

    /// <summary>
    /// 获取当前视口中可见的最后一个列表项索引
    /// </summary>
    /// <param name="offset">最后一个项相对于视口的偏移量（输出参数）</param>
    /// <returns>可见最后一个项的索引（-1表示无可见项）</returns>
    public int GetLastItem(out float offset)
    {
        CheckScrollRectReference();
        offset = 0;

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(GetLastItem)} 方法仅在运行时生效", this);
            return -1;
        }

        int lastItem = loopScrollRect.GetLastItem(out offset);
        Debug.Log($"[{nameof(LoopScrollRectControl)}] 可见最后一个列表项索引：{lastItem}，偏移量：{offset:F2}", this);
        return lastItem;
    }

    /// <summary>
    /// 按指定速度滚动到目标项
    /// </summary>
    /// <param name="index">目标项索引</param>
    /// <param name="speed">滚动速度（像素/秒，建议1000左右）</param>
    /// <param name="offset">额外偏移量（默认0）</param>
    /// <param name="mode">滚动模式（默认ToStart）</param>
    public void ScrollToCell(int index, float speed, float offset = 0, ScrollMode mode = ScrollMode.ToStart)
    {
        CheckScrollRectReference();

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(ScrollToCell)} 方法仅在运行时生效", this);
            return;
        }

        // 参数合法性校验：速度必须大于0
        if (speed <= 0)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 滚动速度必须大于0，当前值：{speed}", this);
            return;
        }

        // 参数合法性校验：索引不能超出范围（无限模式除外）
        if (loopScrollRect.totalCount >= 0 && (index < 0 || index >= loopScrollRect.totalCount))
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 目标索引超出范围！索引：{index}，总项数：{loopScrollRect.totalCount}", this);
            return;
        }

        // 转换枚举（对外枚举 → 底层组件枚举）
        LoopScrollRectBase.ScrollMode actualMode = (LoopScrollRectBase.ScrollMode)mode;
        loopScrollRect.ScrollToCell(index, speed, offset, actualMode);

        Debug.Log($"[{nameof(LoopScrollRectControl)}] 开始滚动到索引 {index}，速度：{speed}，模式：{mode}", this);
    }

    /// <summary>
    /// 在指定时长内滚动到目标项（不支持JustAppear模式）
    /// </summary>
    /// <param name="index">目标项索引</param>
    /// <param name="time">滚动时长（秒，建议0.3~1.0）</param>
    /// <param name="offset">额外偏移量（默认0）</param>
    /// <param name="mode">滚动模式（默认ToStart）</param>
    public void ScrollToCellWithinTime(int index, float time, float offset = 0, ScrollMode mode = ScrollMode.ToStart)
    {
        CheckScrollRectReference();

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(ScrollToCellWithinTime)} 方法仅在运行时生效", this);
            return;
        }

        // 参数合法性校验：时长必须大于0
        if (time <= 0)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 滚动时长必须大于0，当前值：{time}", this);
            return;
        }

        // 参数合法性校验：索引不能超出范围
        if (loopScrollRect.totalCount >= 0 && (index < 0 || index >= loopScrollRect.totalCount))
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 目标索引超出范围！索引：{index}，总项数：{loopScrollRect.totalCount}", this);
            return;
        }

        // 参数合法性校验：不支持JustAppear模式
        if (mode == ScrollMode.JustAppear)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 按时长滚动不支持JustAppear模式！", this);
            return;
        }

        // 转换枚举并执行滚动
        LoopScrollRectBase.ScrollMode actualMode = (LoopScrollRectBase.ScrollMode)mode;
        loopScrollRect.ScrollToCellWithinTime(index, time, offset, actualMode);

        Debug.Log($"[{nameof(LoopScrollRectControl)}] 开始在 {time:F2} 秒内滚动到索引 {index}，模式：{mode}", this);
    }

    /// <summary>
    /// 停止所有滚动运动（包括惯性滚动）
    /// </summary>
    public void StopMovement()
    {
        CheckScrollRectReference();

        if (!Application.isPlaying)
        {
            Debug.LogWarning($"[{nameof(LoopScrollRectControl)}] {nameof(StopMovement)} 方法仅在运行时生效", this);
            return;
        }

        loopScrollRect.StopMovement();
        Debug.Log($"[{nameof(LoopScrollRectControl)}] 已停止列表滚动", this);
    }
    #endregion

    #region 快捷操作方法
    /// <summary>
    /// 快速滚动到列表顶部/左侧
    /// </summary>
    /// <param name="speed">滚动速度（默认1000）</param>
    public void ScrollToTop(float speed = 1000)
    {
        ScrollToCell(0, speed);
    }

    /// <summary>
    /// 快速滚动到列表底部/右侧
    /// </summary>
    /// <param name="speed">滚动速度（默认1000）</param>
    public void ScrollToBottom(float speed = 1000)
    {
        // 无限模式无法滚动到底部
        if (loopScrollRect.totalCount < 0)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 无限模式下无法滚动到底部！", this);
            return;
        }

        ScrollToCell(loopScrollRect.totalCount - 1, speed);
    }
    #endregion

    #region 内部辅助方法
    /// <summary>
    /// 检查LoopScrollRect组件引用是否有效
    /// 【异常处理】引用为空时抛出异常，避免空指针错误
    /// </summary>
    private void CheckScrollRectReference()
    {
        if (loopScrollRect == null)
        {
            string errorMsg = $"[{nameof(LoopScrollRectControl)}] LoopScrollRectBase组件引用为空！";
            Debug.LogError(errorMsg, this);
            throw new System.NullReferenceException(errorMsg);
        }
    }
    #endregion

    #region 接口实现
    /// <summary>
    /// 【接口实现】获取列表项预制体实例
    /// 【内部调用】由LoopScrollRect组件自动调用，外部无需调用
    /// </summary>
    /// <param name="index">列表项索引</param>
    /// <returns>列表项GameObject实例</returns>
    public GameObject GetObject(int index)
    {
        // 空值防护：预制体未赋值时返回null
        if (item == null)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 列表项预制体（item）未赋值！无法创建列表项", this);
            return null;
        }

        // 核心逻辑：从全局对象池获取预制体实例
        GameObject go = PoolManage.Instance.GetObj(item);
        return go;
    }

    /// <summary>
    /// 【接口实现】回收列表项
    /// 【内部调用】由LoopScrollRect组件自动调用，外部无需调用
    /// </summary>
    /// <param name="trans">要回收的列表项Transform</param>
    public void ReturnObject(Transform trans)
    {
        // 空值防护
        if (trans == null)
            return;

        // 调用列表项的回收回调（重置数据/状态）
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);

        // 核心逻辑：回收至全局对象池
        PoolManage.Instance.PushObj(item, trans.gameObject);
    }

    /// <summary>
    /// 【接口实现】为列表项提供数据
    /// 【内部调用】由LoopScrollRect组件自动调用，外部无需调用
    /// </summary>
    /// <param name="transform">列表项Transform</param>
    /// <param name="idx">列表项索引</param>
    public void ProvideData(Transform transform, int idx)
    {
        // 空值防护
        if (transform == null)
        {
            Debug.LogError($"[{nameof(LoopScrollRectControl)}] 索引{idx}的列表项对象为空！无法绑定数据", this);
            return;
        }

        // 调用列表项的数据绑定回调（传递索引）
        transform.SendMessage("ScrollCellIndex", idx, SendMessageOptions.DontRequireReceiver);
    }
    #endregion
}