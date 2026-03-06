using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FireHydrant : BaseBulletInteract_NetWork
{
    [Header("血条显示的配置")]
    public WaterEffect waterEffect;//水粒子特效

    public override void EffectTrigger()
    {
        //改变层级不进行碰撞
        gameObject.tag = "BackGround";
        gameObject.layer = LayerMask.NameToLayer("Default");

    }

    //每次血条改变就需要调用血量UI的显示
    public override void HealthChangeEffect(float Health)//根据血量来触发对应的效果
    {
      
        //根据当前的值依次触发对应的粒子效果
        if (Health <= 80 && Health > 50)
            waterEffect.triggerParticleSystem(WaterType.ShallowWater);
        else if(Health <= 50 && Health > 30)
            waterEffect.triggerParticleSystem(WaterType.DeepWater);
        else if(Health <= 30)
            waterEffect.triggerParticleSystem(WaterType.BurstWater);

    }

    public override void Init()
    {
        gameObject.tag = "BulletInteractObj";//设置会回层级然后
        gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");
        //恢复血量
        CmdHeal();
        //触发粒子结束
        waterEffect.StopAll();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void ResetObj()
    {
        waterEffect.StopAll();
    }

   
}
