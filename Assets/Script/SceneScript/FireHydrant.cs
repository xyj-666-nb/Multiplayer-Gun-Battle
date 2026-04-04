using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FireHydrant : BaseBulletInteract_NetWork
{
    [Header("血条显示的配置")]
    public WaterEffect waterEffect;

    [Header("场景损坏图 (5种状态)")]
    public SpriteRenderer spriteRenderer;
    public Sprite NormalSprite;      // 1. 完好状态 (>85血)
    public Sprite SlightSprite;      // 2. 轻微漏水 (65-85血)
    public Sprite SeriousSprite;     // 3. 严重漏水 (45-65血)
    public Sprite BurstSprite;       // 4. 爆裂状态 (20-45血)
    public Sprite DestroyedSprite;   // 5. 【新增】完全损毁 (<=20血)

    public override void EffectTrigger()
    {
        // 仅服务器修改状态，客户端同步表现
    }

    // 血量变化视觉效果
    public override void HealthChangeEffect(float Health)
    {
        UpdateSprite(Health);

        // 【建议】如果你的 WaterEffect 也有对应第5种特效，可以在这里同步加上
        if (Health <= 85 && Health > 65)
            waterEffect.triggerParticleSystem(WaterType.ShallowWater);
        else if (Health <= 65 && Health > 45)
            waterEffect.triggerParticleSystem(WaterType.DeepWater);
        else if (Health > 20 && Health <= 45)
            waterEffect.triggerParticleSystem(WaterType.BurstWater);
        else if (Health <= 0)
        {
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }

    // Sprite切换核心逻辑 (5档细分)
    private void UpdateSprite(float health)
    {
        if (spriteRenderer == null) return;

        // 重新划分血量区间，加入第5种完全损毁状态
        spriteRenderer.sprite = health switch
        {
            > 85 => NormalSprite,
            > 65 => SlightSprite,
            > 45 => SeriousSprite,
            > 20 => BurstSprite,
            _ => DestroyedSprite // <=20血显示完全损毁
        };
    }

    public override void InitClient()
    {
        gameObject.tag = "BulletInteractObj";
        gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");
        waterEffect.StopAll();

        UpdateSprite(CurrentHealthValue);
    }

    public override void ResetClient()
    {
        waterEffect.StopAll();
        gameObject.tag = "BulletInteractObj";
        gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");

        UpdateSprite(InitHealthValue);
    }
}