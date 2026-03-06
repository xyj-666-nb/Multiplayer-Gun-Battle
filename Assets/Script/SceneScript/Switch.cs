using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Switch : BaseBulletInteract_NetWork
{
    [Header("电闸配置")]
    public List<StreetLamp> ManageStreetLampList;

    public override void EffectTrigger()
    {
        // 可添加特效逻辑
        gameObject.tag = "BackGround";
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public override void HealthChangeEffect(float health)
    {
        if (!isServer) return;

        // 1. 计算电闸当前的破坏等级
        StreetLampDegreeOfDestruction switchInfluence = health switch
        {
            > 150 => StreetLampDegreeOfDestruction.Normal,   // 电闸完好，不添乱
            > 100 => StreetLampDegreeOfDestruction.Slight,   // 电闸损坏，施加轻微影响
            > 50 => StreetLampDegreeOfDestruction.Serious,   // 电闸快炸了，施加严重影响
            _ => StreetLampDegreeOfDestruction.Lose           // 电闸炸了，施加失效影响
        };

        // 2. 将这个影响广播给所有路灯
        SetStreetLampInfluence(switchInfluence);
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

    public override void Init()
    {
        if (isServer)
        {
            // 初始化时，确保电闸对所有路灯施加 Normal 影响
            SetStreetLampInfluence(StreetLampDegreeOfDestruction.Normal);
        }
    }

    public override void ResetObj()
    {
        if (isServer)
        {
            CurrentHealthValue = InitHealthValue;
            IsTrigger = false;
            // 重置时，恢复对路灯的 Normal 影响
            SetStreetLampInfluence(StreetLampDegreeOfDestruction.Normal);
            gameObject.tag = "BulletInteractObj";//设置会回层级然后
            gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");
            //恢复血量
            CmdHeal();
        }
    }
}