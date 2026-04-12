using UnityEngine;

/// <summary>
/// 演示用枪械控制器
/// 配合 SkinDemoShooter 实现全套枪械皮肤效果演示（射击/火光/弹壳/命中特效/相机震动）
/// </summary>
public class DemoGun : MonoBehaviour
{
    public static DemoGun Instance;

    [Header("=== 核心点位 ===")]
    [Tooltip("子弹发射点")]
    public Transform FirePoint;
    [Tooltip("弹壳抛射点")]
    public Transform CasePoint;

    [Header("=== 特效资源 ===")]
    [Tooltip("枪口火光组件")]
    public MuzzleFlash MuzzleFlash;
    [Tooltip("弹壳预制体")]
    public GameObject CartridgePrefab;
    [Tooltip("子弹命中打击特效（墙面/地面）")]
    public GameObject HitEffect;

    [Header("=== 相机震动 ===")]
    [Tooltip("震动持续时间")]
    public float ShakeTime = 0.4f;
    [Tooltip("震动强度")]
    public float ShakeStrength = 0.18f;

    private void Awake()
    {
        // 安全单例初始化
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 【新增】测试打击特效：一键演示命中特效
    /// </summary>
    /// <param name="hitData">打击特效数据</param>
    public void TestHitEffect(GunHitData hitData)
    {
        // 空值校验
        if (FirePoint == null || hitData == null)
        {
            Debug.LogError("[DemoGun] 射击点或打击特效数据为空！");
            return;
        }

        // 替换命中打击特效为当前商品的特效
        HitEffect = hitData.HitObj;

        // 执行默认射击演示（应用新的打击特效）
        PlayShootDemo(null);
    }

    /// <summary>
    /// 测试射击：一键演示全套效果（子弹捆绑包）
    /// </summary>
    /// <param name="InfoPack">子弹皮肤捆绑包</param>
    public void TestShoot(SpecialBulletBindPack InfoPack)
    {
        // 核心空值校验
        if (FirePoint == null)
        {
            Debug.LogError("[DemoGun] FirePoint（射击点）未赋值！");
            return;
        }
        if (InfoPack == null)
        {
            Debug.LogError("[DemoGun] SpecialBulletBindPack（子弹捆绑包）未传入！");
            return;
        }

        // 读取捆绑包内的 子弹配置 + 枪口火光配置
        BulletVisualConfig bulletConfig = InfoPack.bulletVisualConfig;
        MuzzleFlashConfig flashConfig = InfoPack.muzzleFlashConfig;

        //应用枪口火光配置到组件
        ApplyMuzzleFlashConfig(flashConfig);

        // 执行射击演示
        PlayShootDemo(bulletConfig);
    }

    /// <summary>
    /// 测试射击：一键演示效果（仅子弹视觉配置）
    /// </summary>
    /// <param name="bulletConfig">子弹视觉配置</param>
    public void TestShoot(BulletVisualConfig bulletConfig)
    {
        if (FirePoint == null || bulletConfig == null)
        {
            Debug.LogError("[DemoGun] 射击点或子弹配置为空！");
            return;
        }

        // 仅用子弹配置，火光保持默认
        PlayShootDemo(bulletConfig);
    }

    /// <summary>
    /// 【新增】应用枪口火光配置
    /// </summary>
    private void ApplyMuzzleFlashConfig(MuzzleFlashConfig config)
    {
        if (MuzzleFlash == null || config == null)
            return;

        // 自动将捆绑包内的火光配置赋值给枪口组件
        MuzzleFlash.SetConfig(config);
    }

    /// <summary>
    /// 统一封装：执行射击演示逻辑
    /// </summary>
    private void PlayShootDemo(BulletVisualConfig bulletConfig)
    {
        // 判断是否启用弹壳功能
        bool hasCartridge = CartridgePrefab != null && CasePoint != null;

        if (hasCartridge)
        {
            // 完整版：子弹 + 枪口火光 + 弹壳 + 命中打击特效
            SkinDemoShooter.FireAllFull(
                firePoint: FirePoint,
                bulletConfig: bulletConfig,
                muzzleFlash: MuzzleFlash,
                cartridgePrefab: CartridgePrefab,
                cartridgeEjectPoint: CasePoint,
                hitEffectPrefab: HitEffect, // 传递打击特效
                range: 50f
            );
        }
        else
        {
            // 简洁版：子弹 + 枪口火光 + 命中打击特效
            SkinDemoShooter.FireAll(
                firePoint: FirePoint,
                bulletConfig: bulletConfig,
                muzzleFlash: MuzzleFlash,
                hitEffectPrefab: HitEffect // 传递打击特效
            );
        }

        // 触发相机震动
        PlayCameraShake();
    }

    /// <summary>
    /// 触发相机震动
    /// </summary>
    private void PlayCameraShake()
    {
        if (DemoCameraShack.instance != null)
        {
            DemoCameraShack.instance.ShakeCamera(ShakeTime, ShakeStrength);
        }
        else
        {
            Debug.LogWarning("[DemoGun] 未找到 DemoCameraShack 相机震动脚本！");
        }
    }

    /// <summary>
    /// 编辑器调试：手动点击测试射击
    /// </summary>
    [ContextMenu("测试射击")]
    public void DebugTestShoot()
    {
        if (FirePoint != null)
        {
            SkinDemoShooter.FireAll(FirePoint, null, MuzzleFlash, HitEffect);
            PlayCameraShake();
        }
    }
}