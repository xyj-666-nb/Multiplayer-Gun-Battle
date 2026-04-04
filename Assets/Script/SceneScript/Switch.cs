using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Switch : BaseBulletInteract_NetWork
{
    [Header("电闸配置")]
    public List<StreetLamp> ManageStreetLampList;

    [Header("场景损坏图")]
    public SpriteRenderer spriteRenderer;
    public Sprite NormalSprite;
    public Sprite SlightSprite;
    public Sprite SeriousSprite;

    public override void EffectTrigger()
    {
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public override void HealthChangeEffect(float health)
    {
        UpdateSprite(health);

        StreetLampDegreeOfDestruction switchInfluence = health switch
        {
            > 150 => StreetLampDegreeOfDestruction.Normal,
            > 100 => StreetLampDegreeOfDestruction.Slight,
            > 50 => StreetLampDegreeOfDestruction.Serious,
            _ => StreetLampDegreeOfDestruction.Lose
        };

        SetStreetLampInfluence(switchInfluence);
    }

    // 【新增】Sprite切换核心逻辑
    private void UpdateSprite(float health)
    {
        if (spriteRenderer == null) return;

        // 根据血量计算对应破坏程度
        StreetLampDegreeOfDestruction degree = health switch
        {
            > 150 => StreetLampDegreeOfDestruction.Normal,
            > 100 => StreetLampDegreeOfDestruction.Slight,
            > 50 => StreetLampDegreeOfDestruction.Serious,
            _ => StreetLampDegreeOfDestruction.Lose
        };

        // 安全切换Sprite
        spriteRenderer.sprite = degree switch
        {
            StreetLampDegreeOfDestruction.Normal => NormalSprite,
            StreetLampDegreeOfDestruction.Slight => SlightSprite,
            StreetLampDegreeOfDestruction.Serious => SeriousSprite,
            // 如果没有 LoseSprite，默认用 SeriousSprite，你也可以在这里补上
            _ => SeriousSprite
        };
    }

    [Server]
    public void SetStreetLampInfluence(StreetLampDegreeOfDestruction influence)
    {
        if (ManageStreetLampList == null) return;

        foreach (var lamp in ManageStreetLampList)
        {
            if (lamp != null)
            {
                lamp.SetSwitchInfluence(influence);
            }
        }
    }

    public override void InitClient()
    {
        // 纯视觉：恢复标签、层级（所有客户端执行）
        gameObject.tag = "BulletInteractObj";
        gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");

        // 【新增】初始化时同步当前血量对应的Sprite
        UpdateSprite(CurrentHealthValue);
    }

    public override void ResetClient()
    {
        // 纯视觉：恢复标签、层级
        gameObject.tag = "BulletInteractObj";
        gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");

        // 【新增】重置时Sprite归位为完好状态
        UpdateSprite(InitHealthValue);
    }

    [Server]
    public override void InitServer()
    {
        // 执行基类初始化
        base.InitServer();
        // 电闸专属服务器逻辑：恢复对路灯的正常影响
        SetStreetLampInfluence(StreetLampDegreeOfDestruction.Normal);
    }

    [Server]
    public override void ResetServer()
    {
        // 执行基类重置
        base.ResetServer();
        // 电闸专属服务器逻辑：恢复对路灯的正常影响
        SetStreetLampInfluence(StreetLampDegreeOfDestruction.Normal);
    }
}