using UnityEngine;

public class playerStats : CharacterStats
{
    private Player MyMonster;// 自身Player组件引用
    [Header("移动相关")]
    public float MaxYSpeed=6f; // 最大Y轴速度
    public float MaxXSpeed=8f; // 最大X轴速度
    public float MaxYStretch = 0.3f; // Y轴最大拉伸量
    public float MaxXStretch = 0.2f; // X轴最大拉伸量
    public float MoveBumpyRange = 0.1f; // 移动上下抖动幅度
    public float MoveBumpySpeed = 8f; // 上下抖动频率

    [Header("移动力")]
    public float movePower;//移动力
    [Header("跳跃力")]
    public float　JumpPower;//跳跃力

    public override void Awake()
    {
        base.Awake();
    }

    public void TriggerBinding()
    {
        MyMonster = Player.instance;
    }


    //特殊双方逻辑
    public override void HandleSpecialBothsides(CharacterStats Attacker, CharacterStats defender)
    {
        
    }

    //特殊受伤效果
    public override void SpecialWoundEffect()
    {
       
    }

    public override void Death()
    {
        base.Death();
    }

    public override void Wound(float FinalDamage, bool IsCritical = false, CharacterStats Attacker = null, CharacterStats defender = null)
    {
        base.Wound(FinalDamage, IsCritical, Attacker, defender);
    }
}
