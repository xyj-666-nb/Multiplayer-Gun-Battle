using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局枪口烟雾管理器（单例）
/// 场景唯一，管理所有枪械的烟雾生成，完全独立于枪械逻辑
/// </summary>
public class MuzzleSmokeManager : SingleMonoAutoBehavior<MuzzleSmokeManager>
{
    // 烟雾控制器引用
    private FluidController fluidController;

    // 存储所有活跃的烟雾实例
    private List<SmokeInstance> smokeInstances = new List<SmokeInstance>();

    // 烟雾实例数据结构
    private struct SmokeInstance
    {
        public float startTime;       // 启动时间
        public float duration;        // 持续时长
        public Transform firePoint;   // 射击点
        public Color color;           // 烟雾颜色
        public float sizeMin;         // 最小尺寸
        public float sizeMax;         // 最大尺寸
        public float decaySpeed;      // 衰减速度
        public float speedScale;      // 速度缩放
    }

    // 全局默认速度缩放（可在Inspector调整）
    [Header("全局烟雾配置")]
    public float defaultSpeedScale = 5f;

    private void Update()
    {
        // 仅在客户端处理视觉特效
        if (!Application.isPlaying || fluidController == null)
            return;

        // 倒序遍历，避免移除元素导致索引错乱
        for (int i = smokeInstances.Count - 1; i >= 0; i--)
        {
            SmokeInstance smoke = smokeInstances[i];

            // 跳过无效的烟雾实例（射击点被销毁）
            if (smoke.firePoint == null)
            {
                smokeInstances.RemoveAt(i);
                continue;
            }

            float elapsedTime = Time.time - smoke.startTime;

            // 烟雾未结束：生成动态烟雾
            if (elapsedTime < smoke.duration)
            {
                SpawnDynamicSmoke(smoke);
            }
            // 烟雾已结束：移除实例
            else
            {
                smokeInstances.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 全局调用方法：触发烟雾播放
    /// 仅需传入核心数据，无任何外部依赖
    /// </summary>
    /// <param name="firePoint">射击点（决定烟雾位置和方向）</param>
    /// <param name="gunInfo">枪械配置（提供烟雾参数）</param>
    public void PlayMuzzleSmoke(Transform firePoint, GunInfo gunInfo)
    {
        // 参数校验
        if (firePoint == null || gunInfo == null || fluidController == null)
        {
            Debug.LogWarning("[MuzzleSmokeManager] 烟雾参数不完整，跳过本次播放！");
            return;
        }

        // 创建新的烟雾实例（所有参数独立存储，不依赖外部引用）
        SmokeInstance newSmoke = new SmokeInstance
        {
            startTime = Time.time,
            duration = gunInfo.smokeDuration,
            firePoint = firePoint,
            color = gunInfo.smokeColor,
            sizeMin = gunInfo.smokeSizeMin,
            sizeMax = gunInfo.smokeSizeMax,
            decaySpeed = gunInfo.smokeDecaySpeed,
            speedScale = defaultSpeedScale
        };

        // 添加到实例列表（Update中自动处理）
        smokeInstances.Add(newSmoke);
    }

    /// <summary>
    /// 重载方法：支持自定义速度缩放
    /// </summary>
    public void PlayMuzzleSmoke(Transform firePoint, GunInfo gunInfo, float speedScale)
    {
        if (firePoint == null || gunInfo == null || fluidController == null)
        {
            Debug.LogWarning("[MuzzleSmokeManager] 烟雾参数不完整，跳过本次播放！");
            return;
        }

        SmokeInstance newSmoke = new SmokeInstance
        {
            startTime = Time.time,
            duration = gunInfo.smokeDuration,
            firePoint = firePoint,
            color = gunInfo.smokeColor,
            sizeMin = gunInfo.smokeSizeMin,
            sizeMax = gunInfo.smokeSizeMax,
            decaySpeed = gunInfo.smokeDecaySpeed,
            speedScale = speedScale
        };

        smokeInstances.Add(newSmoke);
    }

    /// <summary>
    /// 生成单帧烟雾效果（核心逻辑）
    /// 方向完全由射击点的本地红色轴（transform.localRight）决定
    /// </summary>
    /// <summary>
    /// 生成单帧烟雾效果（核心逻辑）
    /// 方向完全由射击点的本地红色轴（局部X轴）决定
    /// </summary>
    private void SpawnDynamicSmoke(SmokeInstance smoke)
    {
        // 计算剩余时长比例（用于烟雾衰减）
        float elapsedTime = Time.time - smoke.startTime;
        float remainingRatio = Mathf.Max(1 - (elapsedTime / smoke.duration), 0f);
        float sizeFactor = remainingRatio * smoke.decaySpeed;


        Vector3 localRightDir = smoke.firePoint.TransformDirection(Vector3.right).normalized;
        // 转2D向量（忽略Z轴）
        Vector2 smokeDir = new Vector2(localRightDir.x, localRightDir.y) * smoke.speedScale;

        // 调用流体控制器生成烟雾
        fluidController.QueueDrawAtPoint(
            smoke.firePoint.position,       // 烟雾位置（射击点实时位置）
            smoke.color,                    // 烟雾颜色
            smokeDir,                       // 喷射方向（射击点本地红色轴）
            smoke.sizeMin * sizeFactor,     // 动态最小尺寸
            smoke.sizeMax * sizeFactor,     // 动态最大尺寸
            FluidController.VelocityType.Direct
        );
    }

    /// <summary>
    /// 清空所有烟雾
    /// </summary>
    public void ClearAllSmoke()
    {
        smokeInstances.Clear();
    }

    protected override void Awake()
    {
        base.Awake();
        // 单例初始化
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // 获取流体控制器
        fluidController = FluidController.Instance;
        if (fluidController == null)
            Debug.LogError("[MuzzleSmokeManager] 未找到FluidController实例！");

        // 初始化烟雾列表
        smokeInstances = new List<SmokeInstance>();
    }
}