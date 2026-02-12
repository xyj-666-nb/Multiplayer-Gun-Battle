using UnityEngine;

public class playerStats : CharacterStats
{
   
    public Player MyMonster;// 自身Player组件引用
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

    protected override void ClientHandleDeathVisual()//死亡视觉效果（也是通过钩子在所有的客户端执行）
    {
        base.ClientHandleDeathVisual();
    }

    /// <summary>
    /// 受伤特效（也是通过钩子在所有的客户端执行）
    /// </summary>
    /// <param name="ColliderPoint">击中的点</param>
    /// <param name="hitNormal">受击的方向</param>
    /// <param name="attacker">攻击者</param>
    public override void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)//受伤触发的函数（所有客户端都会执行）
    {
        base.RpcPlayWoundEffect(ColliderPoint, hitNormal, attacker);

        //执行受击击退力
        if (attacker != null)
        {
            var attacker_= attacker as playerStats;
            float knockbackDirection = Mathf.Sign(transform.position.x - attacker.transform.position.x);
            MyMonster.MyRigdboby.AddForce(new Vector2(knockbackDirection * attacker_.MyMonster.currentGun.gunInfo.Recoil_Enemy,0), ForceMode2D.Impulse);//设置击退力

            //本地进行震屏
            if (isLocalPlayer)
            {
                MyCameraControl.Instance.AddTimeBasedShake(attacker_.MyMonster.currentGun.gunInfo.ShackStrength_Enemy, attacker_.MyMonster.currentGun.gunInfo.ShackTime_Enemy);//对自己进行震动
            }
        }
    }
}  
