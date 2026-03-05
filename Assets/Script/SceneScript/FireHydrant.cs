using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FireHydrant : BaseBulletInteract_NetWork
{
    [Header("血条显示的配置")]
    public WaterEffect waterEffect;//水粒子特效
    public CanvasGroup HpCanvasGroup;
    public Image HpFillImage;
    public float ShowTime=3;//默认存在时间是3秒
    private float CurrentCoolTime=3;
    private bool IsShow = false;
    private int CoolDownTaskID = -1;
    private int CoolUpTaskID = -1;
    private Sequence HpCanvasGroupSequence;
    public override void EffectTrigger()
    {
        //触发喷射的粒子特效

    }

    //每次血条改变就需要调用血量UI的显示
    public override void HealthChangeEffect(float Health)//根据血量来触发对应的效果
    {
        //更新当前的血条
        IsShow = true;
        CurrentCoolTime = ShowTime;//重置
        //打开倒计时
        if (CoolDownTaskID != -1)
            CountDownManager.Instance.StopTimer(CoolDownTaskID);

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(HpCanvasGroup, ref HpCanvasGroupSequence, true, () => { });

        //开启新的计时器
        CoolDownTaskID= CountDownManager.Instance.CreateTimer(false, (int)(ShowTime * 1000), () => {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(HpCanvasGroup, ref HpCanvasGroupSequence, false, () => { });
        });

        if(CoolUpTaskID!=-1)
            SimpleAnimatorTool.Instance.StopFloatLerpById(CoolUpTaskID);

        CoolUpTaskID= SimpleAnimatorTool.Instance.StartFloatLerp(HpFillImage.fillAmount, CurrentHealthValue / InitHealthValue,0.5f ,(v) => {
            HpFillImage.fillAmount = v;//不断设置当前的数值
        });

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
        throw new System.NotImplementedException();
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
