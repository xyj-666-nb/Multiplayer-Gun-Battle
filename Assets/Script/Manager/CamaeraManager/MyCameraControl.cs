using UnityEngine;
using Cinemachine;
using System.Collections.Generic;
using System.Linq;

#region 震动信息包
/// <summary>
/// 时间控制的相机震动信息结构体
/// </summary>
[System.Serializable]
public class CameraShakePack
{
    #region 任务参数变量
    public float BaseShakeStrength;
    public float ShakeTime;

    [Header("手动震动任务的唯一ID")]
    public int ShakeID;

    [Header("是否使用距离衰减功能")]
    public bool IsUseDistanceDecay = false;//是否使用距离衰减
    public Transform ShakeTransform;//震动位置
    public Transform TargetTransform;//作用的目标位置

    public float FinalStrength
    {
        get
        {
            if (IsUseDistanceDecay)
            {
                float DecayValue = Vector3.Distance(ShakeTransform.position, TargetTransform.position) * MyCameraControl.DistanceDecay;
                return BaseShakeStrength * (1 - DecayValue);//返还衰减后的强度
            }
            else
                return BaseShakeStrength;
        }
    }

    #endregion

    #region 任务包的构造函数

    //带有时间且不需要距离衰减的震动的构造函数
    public CameraShakePack(float _ShakeStrength, float _ShakeTime)
    {
        BaseShakeStrength = _ShakeStrength;
        ShakeTime = _ShakeTime;
    }

    //带有时间且需要距离衰减的震动的构造函数
    public CameraShakePack(float _ShakeStrength, float _ShakeTime, Transform _ShakeTransform, Transform _TargetTransform)
    {
        BaseShakeStrength = _ShakeStrength;
        ShakeTime = _ShakeTime;
        TargetTransform = _TargetTransform;
        ShakeTransform = _ShakeTransform;
        IsUseDistanceDecay = true;
    }

    // 不带时间的震动构造函数
    public CameraShakePack(float _ShakeStrength, int shakeID)
    {
        BaseShakeStrength = _ShakeStrength;
        ShakeID = shakeID;
    }

    // 不带时间且需要距离衰减的震动的构造函数
    public CameraShakePack(float _ShakeStrength, Transform _ShakeTransform, Transform _TargetTransform, int shakeID)
    {
        BaseShakeStrength = _ShakeStrength;
        TargetTransform = _TargetTransform;
        ShakeTransform = _ShakeTransform;
        IsUseDistanceDecay = true;
        ShakeID = shakeID;
    }
    #endregion
}
#endregion

#region 缩放任务类
/// <summary>
/// 缩放任务
/// </summary>
public class ZoomTask
{
    public int TaskID;               // 任务唯一ID
    public ZoomTaskType Type;        // 缩放类型
    public float TargetSize;         // 目标缩放尺寸
    public float Speed;              // 缩放速度
    public float RemainStayTime;     // 剩余停留时间
    public bool IsZoomingToTarget;   // 是否正在向目标尺寸缩放
    public bool IsStaying;           // 是否处于停留阶段
    public bool IsZoomingBack;       // 是否正在还原到基准尺寸
    public bool IsCompleted;         // 任务是否已完成

    // 构造函数
    public ZoomTask(int id, ZoomTaskType type, float targetSize, float speed, float stayTime = 0f)
    {
        TaskID = id;
        Type = type;
        TargetSize = Mathf.Max(0.1f, targetSize); // 避免尺寸为0
        Speed = Mathf.Max(0.1f, speed);           // 避免速度为0
        RemainStayTime = Mathf.Max(0f, stayTime);
        IsZoomingToTarget = true;
        IsStaying = false;
        IsZoomingBack = false;
        IsCompleted = false;
    }
}

/// <summary>
/// 缩放类型
/// </summary>
public enum ZoomTaskType
{
    TemporaryWithTime,   // 带停留时间，自动还原
    TemporaryManual      // 无固定时间，手动还原
}
#endregion

#region 相机模式枚举及区域锁定包
public enum CameraMode
{
    //主要服务于2D横板游戏的相机模式
    FollowPlayerMode,//完全跟随玩家
    XFollowTargetMode,//X轴跟随目标
    YFollowTargetMode,//Y轴跟随目标
    AreaLockingMode,//区域锁定模式
}

public class AreaLockingPack
{
    //4个区域的位置
    public Transform LeftUpPos;//左上角位置
    public Transform RightDownPos;//右下角位置
}
#endregion

/************************************************************************************
 * 脚本名称：MyCameraControl.cs
 * 脚本作用：基于Unity Cinemachine插件实现的单例化相机控制核心脚本
 * 核心功能：
 *  1. 相机震动：支持时间驱动自动衰减震动、手动控制启停震动，且均支持距离衰减效果
 *  2. 相机缩放：支持多任务叠加的正交相机缩放，包含带停留时间自动还原、手动触发还原两种模式
 *  3. 相机模式：基于Cinemachine原生API实现跟随/区域锁定，保留缓动/预测跟随功能
 ************************************************************************************/
public class MyCameraControl : SingleMonoAutoBehavior<MyCameraControl>
{
    #region 组件引用
    public CinemachineVirtualCamera virtualCamera;
    public Camera MainCamera;

    private CinemachineConfiner _confiner;
    private CinemachineFramingTransposer _framingTransposer;
    private CinemachineBasicMultiChannelPerlin _noise;

    [Header("Cinemachine 缓动配置（生效所有模式）")]
    public float xDamping = 1f; // X轴跟随阻尼（越大跟随越慢）
    public float yDamping = 1f; // Y轴跟随阻尼
    public float zDamping = 0f; // Z轴阻尼（2D游戏设0）
    #endregion

    #region 缩放核心配置
    private float _baseOrthographicSize;// 相机初始基准正交尺寸
    private List<ZoomTask> _zoomTaskList = new List<ZoomTask>();// 所有未完成的缩放任务列表
    private int _nextZoomTaskID = 0;    // 下一个任务的ID生成器
    private const float _sizeErrorThreshold = 0.1f; // 尺寸判定误差阈值
    #endregion

    #region 震动相关字段
    private float _totalShakeStrength = 0f;
    [Header("震动消失的速度")]
    [SerializeField] private float _shakeFadeSpeed = 5f;
    [Header("震动随距离衰减的强度")]
    public static float DistanceDecay;//距离衰减强度(每米削减百分之多少)

    [Header("震动任务列表")]
    private List<CameraShakePack> _timeBasedShakeList = new List<CameraShakePack>();
    private List<CameraShakePack> _manualShakeList = new List<CameraShakePack>();
    private static int IdCounter = 0;//震动任务ID计数器
    #endregion

    #region 相机模式相关字段
    [Header("相机模式配置")]
    public CameraMode CurrentCameraMode; // 当前相机模式
    public Vector2 OffsetPos; // 相机偏移量
    public AreaLockingPack areaLockingPack; // 区域锁定信息包

    private Transform _cameraTarget; // 相机跟踪的目标Transform
    private float _cameraHalfHeight; // 正交相机半高
    private float _cameraHalfWidth; // 正交相机半宽
    private PolygonCollider2D _boundaryCollider;
    #endregion

    // 修改访问修饰符以匹配基类的定义
    protected override void Awake()
    {
        base.Awake();
        InitializeCameraComponents();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        EventCenter.Instance.AddEventLister<GameObject>(E_EventType.E_PlayerInit, SetCameraTargetToPlayer);
    }

    private void Update()
    {
        UpdateZoomTasks();    // 处理所有缩放任务（核心）
        UpdateShakeLogic();   // 震动逻辑（保留）
    }

    #region 初始化
    private void InitializeCameraComponents()
    {
        // 自动查找虚拟相机
        if (virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera != null)
        {
            _noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            // 获取2D跟随核心组件（必须设置Body为FramingTransposer）
            _framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            // 获取或添加Confiner组件
            _confiner = virtualCamera.GetComponent<CinemachineConfiner>();
            if (_confiner == null)
                _confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner>();

            // 记录初始基准尺寸
            _baseOrthographicSize = virtualCamera.m_Lens.OrthographicSize;

            if (_noise == null)
                Debug.LogWarning("虚拟相机缺少 CinemachineBasicMultiChannelPerlin 组件，无法实现震动");
            if (_framingTransposer == null)
                Debug.LogWarning("虚拟相机缺少 FramingTransposer 组件！请将VirtualCamera的Body类型改为FramingTransposer");
        }
        else
        {
            Debug.LogError("未找到 CinemachineVirtualCamera，相机控制功能失效");
        }
    }

    private void SetCameraTargetToPlayer(GameObject player)
    {
        SetCameraMode_FollowPlayerMode(player);
    }
    #endregion

    #region 缩放方法以及逻辑处理
    /// <summary>
    /// 添加带停留时间的临时缩放任务
    /// </summary>
    /// <param name="targetSize">目标缩放尺寸</param>
    /// <param name="stayTime">达到目标后停留时间（秒）</param>
    /// <param name="speed">缩放速度</param>
    /// <returns>任务ID</returns>
    public int AddZoomTask_TemporaryWithTime(float targetSize, float stayTime, float speed)
    {
        if (virtualCamera == null) return -1;

        int taskID = _nextZoomTaskID++;
        _zoomTaskList.Add(new ZoomTask(taskID, ZoomTaskType.TemporaryWithTime, targetSize, speed, stayTime));
        return taskID;
    }

    /// <summary>
    /// 添加无固定时间的临时缩放任务
    /// </summary>
    /// <param name="targetSize">目标缩放尺寸</param>
    /// <param name="speed">缩放速度</param>
    /// <returns>任务ID</returns>
    public int AddZoomTask_TemporaryManual(float targetSize, float speed)
    {
        if (virtualCamera == null) return -1;

        int taskID = _nextZoomTaskID++;
        _zoomTaskList.Add(new ZoomTask(taskID, ZoomTaskType.TemporaryManual, targetSize, speed));
        return taskID;
    }

    /// <summary>
    /// 按百分比添加带时间的缩放任务
    /// </summary>
    /// <param name="percent">缩放百分比</param>
    /// <param name="stayTime">停留时间</param>
    /// <param name="speed">缩放速度</param>
    /// <returns>任务ID</returns>
    public int AddZoomTask_ByPercent(float percent, float stayTime, float speed)
    {
        if (virtualCamera == null)
            return -1;

        float targetSize = virtualCamera.m_Lens.OrthographicSize * percent;
        return AddZoomTask_TemporaryWithTime(targetSize, stayTime, speed);
    }

    /// <summary>
    /// 按百分比添加无固定时间的临时缩放任务（手动还原）
    /// </summary>
    /// <param name="percent">缩放百分比（支持任意正数）</param>
    /// <param name="speed">缩放速度</param>
    /// <returns>任务ID（用于后续手动还原）</returns>
    public int AddZoomTask_ByPercent_TemporaryManual(float percent, float speed)
    {
        if (virtualCamera == null)
            return -1;

        // 计算目标尺寸：当前相机尺寸 * 百分比（支持>1的百分比）
        float targetSize = virtualCamera.m_Lens.OrthographicSize * percent;
        // 调用无固定时间的缩放任务方法，返回任务ID
        return AddZoomTask_TemporaryManual(targetSize, speed);
    }

    /// <summary>
    /// 手动还原指定ID的临时缩放任务
    /// </summary>
    /// <param name="taskID">要还原的任务ID</param>
    public void ResetZoomTask(int taskID)
    {
        ZoomTask task = _zoomTaskList.Find(t => t.TaskID == taskID && !t.IsCompleted);
        if (task == null)
        {
            Debug.LogWarning($"未找到ID为{taskID}的有效缩放任务");
            return;
        }

        // 仅对TemporaryManual类型生效
        if (task.Type == ZoomTaskType.TemporaryManual)
        {
            task.IsZoomingToTarget = false;
            task.IsStaying = false;
            task.IsZoomingBack = true; // 开始还原到基准尺寸
        }
    }

    /// <summary>
    /// 手动还原所有TemporaryManual类型的缩放任务
    /// </summary>
    public void ResetAllManualZoomTasks()
    {
        foreach (var task in _zoomTaskList)
        {
            if (!task.IsCompleted && task.Type == ZoomTaskType.TemporaryManual)
            {
                task.IsZoomingToTarget = false;
                task.IsStaying = false;
                task.IsZoomingBack = true;
            }
        }
    }

    /// <summary>
    /// 强制清空所有缩放任务，立即还原到基准尺寸
    /// </summary>
    public void ClearAllZoomTasks()
    {
        _zoomTaskList.Clear();
        virtualCamera.m_Lens.OrthographicSize = _baseOrthographicSize;
        _nextZoomTaskID = 0;
    }

    #region List缩放任务处理逻辑
    /// <summary>
    /// 核心缩放任务更新方法
    /// </summary>
    private void UpdateZoomTasks()
    {
        if (virtualCamera == null || _zoomTaskList.Count == 0)
            return;

        for (int i = _zoomTaskList.Count - 1; i >= 0; i--)
        {
            ZoomTask task = _zoomTaskList[i];
            if (task.IsCompleted)
            {
                _zoomTaskList.RemoveAt(i);
                continue;
            }

            UpdateSingleZoomTask(task);
        }

        float finalTargetSize = CalculateFinalZoomTargetSize();
        ApplyFinalZoom(finalTargetSize);
    }

    /// <summary>
    /// 更新单个缩放任务的生命周期
    /// </summary>
    private void UpdateSingleZoomTask(ZoomTask task)
    {
        float currentSize = virtualCamera.m_Lens.OrthographicSize;

        if (task.IsZoomingToTarget)
        {
            if (Mathf.Abs(currentSize - task.TargetSize) <= _sizeErrorThreshold)
            {
                task.IsZoomingToTarget = false;

                if (task.Type == ZoomTaskType.TemporaryWithTime)// 带时间的任务，进入停留阶段
                    task.IsStaying = true;
            }
        }
        if (task.IsStaying)// 任务停留阶段
        {
            task.RemainStayTime -= Time.deltaTime;
            if (task.RemainStayTime <= 0)
            {
                task.IsStaying = false;
                task.IsZoomingBack = true; // 停留结束，开始还原
            }
        }

        if (task.IsZoomingBack)
        {
            if (Mathf.Abs(currentSize - _baseOrthographicSize) <= _sizeErrorThreshold)
            {
                task.IsZoomingBack = false;
                task.IsCompleted = true; // 任务完成
            }
        }
    }

    /// <summary>
    /// 计算叠加后的最终目标尺寸
    /// 修复逻辑：支持放大（百分比>1）和缩小（百分比<1）两种场景
    /// - 若有任务要放大视野（targetSize > 基准），取最大的目标尺寸
    /// - 若有任务要缩小视野（targetSize < 基准），取最小的目标尺寸
    /// - 无任务时还原基准尺寸
    /// </summary>
    private float CalculateFinalZoomTargetSize()
    {
        // 初始化：先收集所有有效任务的目标尺寸
        List<float> validTaskTargets = new List<float>();

        foreach (var task in _zoomTaskList)
        {
            if (task.IsCompleted)
                continue;

            // 确定当前任务的目标尺寸：还原阶段取基准，否则取任务目标
            float taskTarget = task.IsZoomingBack ? _baseOrthographicSize : task.TargetSize;
            validTaskTargets.Add(taskTarget);
        }

        // 无有效任务 → 还原基准尺寸
        if (validTaskTargets.Count == 0)
            return _baseOrthographicSize;

        // 有有效任务 → 计算最终目标：
        float minTarget = validTaskTargets.Min();
        float maxTarget = validTaskTargets.Max();

        if (minTarget < _baseOrthographicSize)
            return minTarget;
        // 无缩小任务时，取最大
        else
            return maxTarget;
    }

    /// <summary>
    /// 应用叠加后的最终缩放
    /// </summary>
    private void ApplyFinalZoom(float finalTargetSize)
    {
        float currentSize = virtualCamera.m_Lens.OrthographicSize;
        // 误差阈值内直接返回，避免抖动
        if (Mathf.Abs(currentSize - finalTargetSize) < _sizeErrorThreshold)
            return;

        // 取所有有效任务的最大速度
        float maxSpeed = 0f;
        foreach (var task in _zoomTaskList)
        {
            if (!task.IsCompleted && task.Speed > maxSpeed)
            {
                maxSpeed = task.Speed;
            }
        }
        maxSpeed = maxSpeed == 0 ? 1f : maxSpeed; // 兜底

        // 稳定插值：无论放大还是缩小，都向finalTargetSize靠近
        float newSize = Mathf.MoveTowards(currentSize, finalTargetSize, maxSpeed * Time.deltaTime);
        // 额外兜底：避免尺寸过小/过大（可选，根据你的游戏需求调整上下限）
        newSize = Mathf.Clamp(newSize, _baseOrthographicSize * 0.1f, _baseOrthographicSize * 3f);
        virtualCamera.m_Lens.OrthographicSize = newSize;
    }
    #endregion
    #endregion

    #region 震动逻辑
    /// <summary>
    /// 添加带时间的震动任务，无距离衰减
    /// </summary>
    public void AddTimeBasedShake(float strength, float duration)
    {
        if (float.IsNaN(strength) || float.IsInfinity(strength) ||
            float.IsNaN(duration) || float.IsInfinity(duration))
        {
            Debug.LogWarning("震动参数非法（NaN/无穷大），已忽略");
            return;
        }

        _timeBasedShakeList.Add(new CameraShakePack(
            Mathf.Clamp(strength, 0f, 10f),
            Mathf.Clamp(duration, 0.1f, 10f)
        ));
    }

    /// <summary>
    /// 添加带时间的震动任务，带距离衰减
    /// </summary>
    public void AddTimeBasedShake(float strength, float duration, Transform Target, Transform ShakeTransform)
    {
        if (float.IsNaN(strength) || float.IsInfinity(strength) ||
            float.IsNaN(duration) || float.IsInfinity(duration))
        {
            Debug.LogWarning("震动参数非法（NaN/无穷大），已忽略");
            return;
        }

        _timeBasedShakeList.Add(new CameraShakePack(
            Mathf.Clamp(strength, 0f, 10f),
            Mathf.Clamp(duration, 0.1f, 10f),
            ShakeTransform,
            Target
        ));
    }

    /// <summary>
    /// 添加不带时间的手动震动任务，无距离衰减
    /// </summary>
    public int AddManualShake(float strength)
    {
        if (float.IsNaN(strength) || float.IsInfinity(strength))
        {
            Debug.LogWarning("震动强度非法（NaN/无穷大），已忽略");
            return 0;
        }
        var ID = IdCounter++;
        _manualShakeList.Add(new CameraShakePack(Mathf.Clamp(strength, 0f, 10f), ID));
        return ID;//返还唯一任务ID
    }

    /// <summary>
    /// 添加不带时间的手动震动任务，带距离衰减
    /// </summary>
    public int AddManualShake(float strength, Transform Target, Transform ShakeTransform)
    {
        if (float.IsNaN(strength) || float.IsInfinity(strength))
        {
            Debug.LogWarning("震动强度非法（NaN/无穷大），已忽略");
            return 0;
        }
        var ID = IdCounter++;
        _manualShakeList.Add(new CameraShakePack(Mathf.Clamp(strength, 0f, 10f), ShakeTransform, Target, ID));
        return ID;//返还唯一任务ID
    }

    /// <summary>
    /// 震动的更新处理
    /// </summary>
    private void UpdateShakeLogic()
    {
        if (_noise == null)
            return;

        _totalShakeStrength = 0f;

        // 处理时间基震动
        for (int i = _timeBasedShakeList.Count - 1; i >= 0; i--)
        {
            var shake = _timeBasedShakeList[i];
            if (shake == null)
            {
                _timeBasedShakeList.RemoveAt(i);
                continue;
            }

            shake.ShakeTime -= Time.deltaTime;
            if (shake.ShakeTime > 0)
            {
                _totalShakeStrength += shake.FinalStrength;
            }
            else
            {
                shake.BaseShakeStrength = Mathf.MoveTowards(shake.BaseShakeStrength, 0f, Time.deltaTime * _shakeFadeSpeed);//计算基础震动衰减
                _totalShakeStrength += shake.FinalStrength;

                if (shake.BaseShakeStrength <= 0.01f)//如果基础震动值接近0.01则移除该震动
                    _timeBasedShakeList.RemoveAt(i);
            }
        }

        // 叠加手动震动
        foreach (var pack in _manualShakeList)
        {
            _totalShakeStrength += pack.FinalStrength;
        }

        _noise.m_AmplitudeGain = Mathf.Clamp(_totalShakeStrength, 0f, 10f);
    }

    /// <summary>
    /// 停止指定ID的手动震动任务
    /// </summary>
    public void StopManualShake(int TaskID)
    {
        for (int i = _manualShakeList.Count - 1; i >= 0; i--)
        {
            if (_manualShakeList[i].ShakeID == TaskID)
            {
                _manualShakeList.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 停止所有手动震动任务
    /// </summary>
    public void StopAllManualShake() => _manualShakeList.Clear();

    /// <summary>
    /// 重置所有震动（清空列表+立即停止震动）
    /// </summary>
    public void ResetAllShake()
    {
        _timeBasedShakeList.Clear();
        _manualShakeList.Clear();
        _totalShakeStrength = 0f;
        if (_noise != null)
            _noise.m_AmplitudeGain = 0f;
    }
    #endregion

    #region 相机模式切换以及控制
    /// <summary>
    /// 初始化相机尺寸参数
    /// </summary>
    private void InitCameraSizeParams()
    {
        if (MainCamera == null)
            return;
        // 正交相机的半高 = OrthographicSize，半宽 = 半高 * 屏幕宽高比
        _cameraHalfHeight = virtualCamera.m_Lens.OrthographicSize;
        _cameraHalfWidth = _cameraHalfHeight * MainCamera.aspect;
    }

    /// <summary>
    /// 设置相机模式 - 完全跟随玩家模式
    /// </summary>
    /// <param name="Target">跟随的目标物体</param>
    /// <param name="needLookAt">是否需要设置LookAt（默认false）</param>
    public void SetCameraMode_FollowPlayerMode(GameObject Target, bool needLookAt = false)
    {
        if (Target == null)
        {
            Debug.LogError("跟随目标为空，无法设置完全跟随模式！");
            return;
        }

        CurrentCameraMode = CameraMode.FollowPlayerMode;
        _cameraTarget = Target.transform;

        // 设置跟随目标
        virtualCamera.Follow = _cameraTarget;
        // 仅当needLookAt为true时设置LookAt
        if (needLookAt)
            virtualCamera.LookAt = _cameraTarget;

        // 清除区域限制
        if (_confiner != null)
        {
            _confiner.m_BoundingShape2D = null;
            _confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        }

        // 设置阻尼（缓动跟随）
        if (_framingTransposer != null)
        {
            _framingTransposer.m_XDamping = xDamping;
            _framingTransposer.m_YDamping = yDamping;
            _framingTransposer.m_ZDamping = zDamping;
        }
    }

    /// <summary>
    /// 设置相机模式 - X轴跟随目标模式
    /// </summary>
    /// <param name="Target">跟随的目标物体</param>
    /// <param name="offset">X/Y偏移量（Y偏移用于固定Y轴位置）</param>
    /// <param name="needLookAt">是否需要设置LookAt（默认false）</param>
    public void SetCameraMode_XFollowTargetMode(GameObject Target, Vector2 offset, bool needLookAt = false)
    {
        if (Target == null)
        {
            Debug.LogError("跟随目标为空，无法设置X轴跟随模式！");
            return;
        }

        CurrentCameraMode = CameraMode.XFollowTargetMode;
        _cameraTarget = Target.transform;
        OffsetPos = offset;

        // 设置跟随目标
        virtualCamera.Follow = _cameraTarget;
        // 仅当needLookAt为true时设置LookAt
        if (needLookAt)
            virtualCamera.LookAt = _cameraTarget;

        // 清除区域限制
        if (_confiner != null)
        {
            _confiner.m_BoundingShape2D = null;
            _confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        }

        // 对于固定Y轴，我们使用一个技巧：创建一个Y轴限制的区域
        CreateYAxisLockBoundary(Target.transform.position.y + offset.y);

        // 设置阻尼
        if (_framingTransposer != null)
        {
            _framingTransposer.m_XDamping = xDamping;
            _framingTransposer.m_YDamping = yDamping;
        }
    }

    /// <summary>
    /// 设置相机模式 - Y轴跟随目标模式
    /// </summary>
    /// <param name="Target">跟随的目标物体</param>
    /// <param name="offset">X/Y偏移量（X偏移用于固定X轴位置）</param>
    /// <param name="needLookAt">是否需要设置LookAt（默认false）</param>
    public void SetCameraMode_YFollowTargetMode(GameObject Target, Vector2 offset, bool needLookAt = false)
    {
        if (Target == null)
        {
            Debug.LogError("跟随目标为空，无法设置Y轴跟随模式！");
            return;
        }

        CurrentCameraMode = CameraMode.YFollowTargetMode;
        _cameraTarget = Target.transform;
        OffsetPos = offset;

        // 设置跟随目标
        virtualCamera.Follow = _cameraTarget;
        // 仅当needLookAt为true时设置LookAt
        if (needLookAt)
            virtualCamera.LookAt = _cameraTarget;

        // 清除区域限制
        if (_confiner != null)
        {
            _confiner.m_BoundingShape2D = null;
            _confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        }

        // 对于固定X轴，我们使用一个技巧：创建一个X轴限制的区域
        CreateXAxisLockBoundary(Target.transform.position.x + offset.x);

        // 设置阻尼
        if (_framingTransposer != null)
        {
            _framingTransposer.m_XDamping = xDamping;
            _framingTransposer.m_YDamping = yDamping;
        }
    }

    /// <summary>
    /// 设置相机模式 - 区域锁定模式
    /// </summary>
    /// <param name="Target">参考目标物体</param>
    /// <param name="posPack">区域锁定的四个位置信息</param>
    /// <param name="offset">相机偏移量</param>
    /// <param name="needLookAt">是否需要设置LookAt（默认false）</param>
    public void SetCameraMode_AreaLockingMode(GameObject Target, AreaLockingPack posPack, Vector2 offset, bool needLookAt = false)
    {
        if (Target == null)
        {
            Debug.LogError("参考目标为空，无法设置区域锁定模式！");
            return;
        }
        if (posPack == null || posPack.LeftUpPos == null || posPack.RightDownPos == null)
        {
            Debug.LogError("区域锁定信息包不完整（缺少左上/右下位置），无法设置区域锁定模式！");
            return;
        }

        // 初始化相机尺寸参数
        InitCameraSizeParams();
        // 校验区域合法性
        if (!ValidateAreaLockingRegion(posPack))
        {
            Debug.LogError("区域锁定的方形区域参数不合法！请检查左上/右下位置的坐标关系");
            return;
        }

        CurrentCameraMode = CameraMode.AreaLockingMode;
        _cameraTarget = Target.transform;
        areaLockingPack = posPack;
        OffsetPos = offset;

        // 设置跟随目标
        virtualCamera.Follow = _cameraTarget;
        // 仅当needLookAt为true时设置LookAt
        if (needLookAt)
            virtualCamera.LookAt = _cameraTarget;

        // 创建边界Collider
        CreateBoundaryCollider(posPack);

        // 设置Confiner使用这个边界
        if (_confiner != null && _boundaryCollider != null)
        {
            _confiner.m_BoundingShape2D = _boundaryCollider;
            _confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        }

        // 设置阻尼
        if (_framingTransposer != null)
        {
            _framingTransposer.m_XDamping = xDamping;
            _framingTransposer.m_YDamping = yDamping;
        }

        // 如果区域比相机视口小，提示
        if (IsRegionSmallerThanCamera(posPack))
        {
            Debug.LogWarning("区域锁定的区域小于相机视口，Cinemachine会自动固定到区域内！");
        }
    }

    #region 区域锁定模式的辅助方法
    /// <summary>
    /// 校验区域锁定的方形区域是否合法（左上X < 右下X，左上Y > 右下Y）
    /// </summary>
    private bool ValidateAreaLockingRegion(AreaLockingPack pack)
    {
        float left = pack.LeftUpPos.position.x;
        float right = pack.RightDownPos.position.x;
        float top = pack.LeftUpPos.position.y;
        float bottom = pack.RightDownPos.position.y;

        // 合法的方形区域：左 < 右，上 > 下
        return left < right && top > bottom;
    }

    /// <summary>
    /// 判断区域锁定的区域是否小于相机视口
    /// </summary>
    private bool IsRegionSmallerThanCamera(AreaLockingPack pack)
    {
        // 区域宽度 = 右下X - 左上X，区域高度 = 左上Y - 右下Y
        float regionWidth = pack.RightDownPos.position.x - pack.LeftUpPos.position.x;
        float regionHeight = pack.LeftUpPos.position.y - pack.RightDownPos.position.y;

        // 相机视口宽度 = 2 * 半宽，高度 = 2 * 半高
        float cameraViewportWidth = _cameraHalfWidth * 2;
        float cameraViewportHeight = _cameraHalfHeight * 2;

        // 区域宽度/高度都小于相机视口，则判定为过小
        return regionWidth < cameraViewportWidth && regionHeight < cameraViewportHeight;
    }

    /// <summary>
    /// 获取区域锁定的方形区域中心坐标
    /// </summary>
    private Vector2 GetRegionCenter(AreaLockingPack pack)
    {
        float centerX = (pack.LeftUpPos.position.x + pack.RightDownPos.position.x) / 2;
        float centerY = (pack.LeftUpPos.position.y + pack.RightDownPos.position.y) / 2;
        return new Vector2(centerX, centerY);
    }

    /// <summary>
    /// 创建边界碰撞体
    /// </summary>
    private void CreateBoundaryCollider(AreaLockingPack pack)
    {
        // 如果已存在，先销毁
        if (_boundaryCollider != null)
        {
            Destroy(_boundaryCollider.gameObject);
        }

        // 创建新的GameObject来承载Collider
        GameObject boundaryObj = new GameObject("CameraBoundary");
        boundaryObj.transform.SetParent(transform);
        _boundaryCollider = boundaryObj.AddComponent<PolygonCollider2D>();

        // 设置碰撞体为触发器
        _boundaryCollider.isTrigger = true;

        // 计算边界点（矩形）
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(pack.LeftUpPos.position.x, pack.LeftUpPos.position.y); // 左上
        points[1] = new Vector2(pack.RightDownPos.position.x, pack.LeftUpPos.position.y); // 右上
        points[2] = new Vector2(pack.RightDownPos.position.x, pack.RightDownPos.position.y); // 右下
        points[3] = new Vector2(pack.LeftUpPos.position.x, pack.RightDownPos.position.y); // 左下

        _boundaryCollider.points = points;
    }

    /// <summary>
    /// 创建Y轴锁定的边界
    /// </summary>
    private void CreateYAxisLockBoundary(float fixedY)
    {
        if (_boundaryCollider != null)
        {
            Destroy(_boundaryCollider.gameObject);
        }

        GameObject boundaryObj = new GameObject("YAxisBoundary");
        boundaryObj.transform.SetParent(transform);
        _boundaryCollider = boundaryObj.AddComponent<PolygonCollider2D>();
        _boundaryCollider.isTrigger = true;

        // 创建一个非常窄的垂直条带，限制Y轴移动但允许X轴移动
        Vector2[] points = new Vector2[4];
        float halfHeight = 0.1f; // 非常小的高度
        points[0] = new Vector2(-1000, fixedY + halfHeight); // 左上
        points[1] = new Vector2(1000, fixedY + halfHeight);  // 右上
        points[2] = new Vector2(1000, fixedY - halfHeight);  // 右下
        points[3] = new Vector2(-1000, fixedY - halfHeight); // 左下

        _boundaryCollider.points = points;

        // 设置Confiner
        if (_confiner != null)
        {
            _confiner.m_BoundingShape2D = _boundaryCollider;
        }
    }

    /// <summary>
    /// 创建X轴锁定的边界
    /// </summary>
    private void CreateXAxisLockBoundary(float fixedX)
    {
        if (_boundaryCollider != null)
        {
            Destroy(_boundaryCollider.gameObject);
        }

        GameObject boundaryObj = new GameObject("XAxisBoundary");
        boundaryObj.transform.SetParent(transform);
        _boundaryCollider = boundaryObj.AddComponent<PolygonCollider2D>();
        _boundaryCollider.isTrigger = true;

        // 创建一个非常窄的水平条带，限制X轴移动但允许Y轴移动
        Vector2[] points = new Vector2[4];
        float halfWidth = 0.1f; // 非常小的宽度
        points[0] = new Vector2(fixedX - halfWidth, 1000);  // 左上
        points[1] = new Vector2(fixedX + halfWidth, 1000);  // 右上
        points[2] = new Vector2(fixedX + halfWidth, -1000); // 右下
        points[3] = new Vector2(fixedX - halfWidth, -1000); // 左下

        _boundaryCollider.points = points;

        // 设置Confiner
        if (_confiner != null)
        {
            _confiner.m_BoundingShape2D = _boundaryCollider;
        }
    }
    #endregion
    #endregion
}