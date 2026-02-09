using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 完全独立的枪口烟雾控制器
/// 所有参数自行配置，零依赖外部脚本，换弹/射击状态完全不影响烟雾播放
/// </summary>
public class MuzzleSmokeController : MonoBehaviour
{
    [Header("=== 烟雾核心配置（完全独立） ===")]
    [Tooltip("烟雾生成的枪口点（自行拖拽赋值）")]
    public Transform firePoint;          // 自身配置的枪口点
    [Tooltip("枪械配置文件（自行拖拽赋值）")]
    public GunInfo gunInfo;              // 自身配置的枪械参数
    [Tooltip("玩家朝向（可选：若需要根据玩家方向调整烟雾）")]
    public float facingDir = 1f;         // 1=右，-1=左，可由外部赋值但不强制

    // 烟雾控制器引用
    private FluidController fluidController;

    // 烟雾实例列表（存储每一发独立的烟雾）
    private List<SmokeInstance> smokeInstances = new List<SmokeInstance>();

    // 烟雾实例结构体（仅依赖自身配置）
    private struct SmokeInstance
    {
        public float startTime;       // 烟雾启动时间
        public float duration;        // 总持续时间（读自身gunInfo）
        public Vector2 position;      // 生成位置（读自身firePoint）
        public Vector2 direction;     // 发射方向（读自身facingDir）
        public Color color;           // 颜色（读自身gunInfo）
        public float sizeMin;         // 最小尺寸（读自身gunInfo）
        public float sizeMax;         // 最大尺寸（读自身gunInfo）
        public float decaySpeed;      // 衰减速度（读自身gunInfo）
    }

    private void Awake()
    {
        // 仅获取全局烟雾控制器，无其他外部依赖
        fluidController = FluidController.Instance;

        // 初始化烟雾列表
        smokeInstances = new List<SmokeInstance>();

        // 自检：提示必填参数
        if (firePoint == null)
            Debug.LogError($"[{gameObject.name}] MuzzleSmokeController - firePoint未赋值！");
        if (gunInfo == null)
            Debug.LogError($"[{gameObject.name}] MuzzleSmokeController - gunInfo未赋值！");
    }

    private void Update()
    {
        // 仅在客户端处理烟雾（视觉特效），完全不判断任何外部状态
        if (!Application.isPlaying || fluidController == null || gunInfo == null || firePoint == null)
            return;

        // 倒序遍历，避免移除元素导致索引错乱
        for (int i = smokeInstances.Count - 1; i >= 0; i--)
        {
            SmokeInstance smoke = smokeInstances[i];
            float elapsedTime = Time.time - smoke.startTime;

            // 烟雾未结束：生成动态烟雾（不受任何外部状态中断）
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
    /// 对外暴露的触发方法：仅触发，无任何参数依赖
    /// BaseGun调用此方法即可，无需传递任何数据
    /// </summary>
    public void TriggerSingleSmoke()
    {
        // 前置检查（仅提示，不中断，避免影响已有烟雾）
        if (gunInfo == null || fluidController == null || firePoint == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 烟雾参数不完整，跳过本次烟雾生成！");
            return;
        }

        // 计算烟雾发射方向（仅用自身配置的facingDir）
        Vector2 smokeDir = firePoint.transform.right * facingDir * 5f;

        // 创建新的烟雾实例（所有参数均读取自身配置）
        SmokeInstance newSmoke = new SmokeInstance
        {
            startTime = Time.time,
            duration = gunInfo.smokeDuration,
            position = firePoint.position,
            direction = smokeDir,
            color = gunInfo.smokeColor,
            sizeMin = gunInfo.smokeSizeMin,
            sizeMax = gunInfo.smokeSizeMax,
            decaySpeed = gunInfo.smokeDecaySpeed
        };

        // 添加到实例列表（Update中自动处理，不受外部干扰）
        smokeInstances.Add(newSmoke);
    }

    /// <summary>
    /// 可选：更新玩家朝向（供外部调用，比如BaseGun同步朝向）
    /// </summary>
    /// <param name="newFacingDir">1=右，-1=左</param>
    public void UpdateFacingDir(float newFacingDir)
    {
        facingDir = newFacingDir;
    }

    /// <summary>
    /// 生成单条烟雾的动态效果（完全独立逻辑）
    /// </summary>
    private void SpawnDynamicSmoke(SmokeInstance smoke)
    {
        // 计算剩余时长比例（用于衰减）
        float elapsedTime = Time.time - smoke.startTime;
        float remainingRatio = Mathf.Max(1 - (elapsedTime / smoke.duration), 0f);
        float sizeFactor = remainingRatio * smoke.decaySpeed;

        // 调用烟雾控制器生成烟雾（纯本地逻辑，无外部依赖）
        fluidController.QueueDrawAtPoint(
            smoke.position,
            smoke.color,
            smoke.direction,
            smoke.sizeMin * sizeFactor,
            smoke.sizeMax * sizeFactor,
            FluidController.VelocityType.Direct
        );
    }

    /// <summary>
    /// 清空所有烟雾（可选：比如对象销毁时调用）
    /// </summary>
    public void ClearAllSmoke()
    {
        smokeInstances.Clear();
    }

    private void OnDestroy()
    {
        // 销毁时清空烟雾，避免内存泄漏
        ClearAllSmoke();
    }
}