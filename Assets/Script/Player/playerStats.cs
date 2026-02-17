using DG.Tweening;
using Mirror;
using UnityEngine;

public class playerStats : CharacterStats
{
    public Player MyMonster;// 自身Player组件引用
    [Header("移动相关")]
    public float MaxYSpeed = 6f; // 最大Y轴速度
    public float MaxXSpeed = 8f; // 最大X轴速度
    public float MaxYStretch = 0.3f; // Y轴最大拉伸量
    public float MaxXStretch = 0.2f; // X轴最大拉伸量
    public float MoveBumpyRange = 0.1f; // 移动上下抖动幅度
    public float MoveBumpySpeed = 8f; // 上下抖动频率

    [Header("移动力")]
    public float movePower;//移动力
    [Header("跳跃力")]
    public float JumpPower;//跳跃力
    [Header("墙跳力")]
    public float WallJumpPower_Up;//墙跳力_向上
    public float WallJumpPower_Side;//墙跳力_向侧面

    [Header("瞄准状态的数值")]
    [Header("瞄准移动相关")]
    public float AimMovePower;//瞄准状态的移动力
    public float AimMoveMaxSpeed;//瞄准状态的最大移动速度
    [Header("瞄准跳跃相关")]
    public float AimJumpPower;//瞄准状态的跳跃力

    [Header("瞄准对于枪械精度的提升")]
    public float AimAccuracyBonus = 0.3f;//瞄准状态对于枪械精度的提升(百分之30左右)
    public float AimRecoilBonus = 0.3f;//瞄准状态对于枪械后坐力的削弱(百分之30左右)
    public float AimViewBonus = 0.3f; //瞄准状态对于当前枪械的视野的提升(百分之30左右)

    [Header("注射器效果数值")]
    public float InjectionHealTime = 0.5f;//注射器动画的持续时间(为期1秒)
    public float StayTime = 0.5f;//颜色停留时间
    [Header("绿色注射器颜色")]
    public Color HealAnimaColor = ColorManager.FreshGreen;
    [Header("黄色注射器颜色")]
    public Color SpeedAnimaColor = ColorManager.LemonYellow;

    [Header("黄色针剂的效果")]
    public float SpeedBuff_MovePowerBonus = 1f;//黄色针剂的移动力提升(移动力加1)
    public float MaxSpeedBuff_Bonus = 1f;//黄色针剂的最大速度提升(+1)
    public float ViewBuff_Bonus = 0.2f;//黄色针剂的视野提升
    public float DurationBuff_Bonus = 20f;//黄色针剂的持续时间(持续20秒)
    public float JumpBuff_MovePowerBonus = 1f;//黄色针剂的跳跃力提升(跳跃力加1)
    // 仅保留buff激活标记，去掉原始值记录变量
    private bool _isYellowBuffActive = false;

    public override void Awake()
    {
        base.Awake();
        if (MyMonster == null)
        {
            MyMonster = GetComponent<Player>();
            if (MyMonster == null)
            {
                MyMonster = GetComponentInParent<Player>();
            }
        }
        // 初始化血量
        if (isLocalPlayer || isServer)
        {
            CurrentHealth = maxHealth;
        }
    }

    protected override void ClientHandleDeathVisual()//死亡视觉效果
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
            var attacker_ = attacker as playerStats;
            float knockbackDirection = Mathf.Sign(transform.position.x - attacker.transform.position.x);
            if (MyMonster?.MyRigdboby != null && attacker_?.MyMonster?.currentGun?.gunInfo != null)
            {
                MyMonster.MyRigdboby.AddForce(new Vector2(knockbackDirection * attacker_.MyMonster.currentGun.gunInfo.Recoil_Enemy, 0), ForceMode2D.Impulse);//设置击退力
            }

            //本地进行震屏
            if (isLocalPlayer && MyCameraControl.Instance != null)
            {
                MyCameraControl.Instance.AddTimeBasedShake(attacker_.MyMonster.currentGun.gunInfo.ShackStrength_Enemy, attacker_.MyMonster.currentGun.gunInfo.ShackTime_Enemy);//对自己进行震动
            }
        }
    }

    //————————————注射器效果————————————
    private int HealAnimaTaskId = -1;//注射器动画的任务ID，防止重复触发动画
    public bool IsInInjectionGreenEffect = false;//是否正在绿色注射器的效果中

    [ClientRpc]
    public void TriggerEffect_Injection(TacticType injectionType)
    {
        switch (injectionType)
        {
            case TacticType.Green_injection:
                Debug.Log($"[本地玩家] 触发绿色针剂回血");
                if (isLocalPlayer)//数值上面的回血是只有自己执行，而颜色的动画是共有的
                {
                    // 触发回血
                    HealAnimaTaskId = SimpleAnimatorTool.Instance.StartFloatLerp(CurrentHealth, maxHealth, InjectionHealTime * 2 + StayTime, (Value) => {
                        CmdChangeHealth(Value, Vector2.zero, Vector2.zero, null);
                    });
                    IsInInjectionGreenEffect = true;
                    //触发BuffUI
                    UImanager.Instance.GetPanel<PlayerPanel>()?.CreateBuff(MilitaryManager.Instance.GetTacticUISprite(injectionType), InjectionHealTime * 2 + StayTime);
                }

                InjectionColorAnima(HealAnimaColor);
                break;

            case TacticType.Yellow_injection:
                Debug.Log($"[本地玩家] 触发黄色针剂速度buff");
                if (isLocalPlayer && !_isYellowBuffActive) // 防止重复激活buff
                {
                    movePower += SpeedBuff_MovePowerBonus; // 基础移动力提升
                    MaxXSpeed += MaxSpeedBuff_Bonus; // 基础最大速度提升
                    AimMovePower += SpeedBuff_MovePowerBonus; // 瞄准状态移动力提升
                    AimMoveMaxSpeed += MaxSpeedBuff_Bonus; // 瞄准状态最大速度提升
                    JumpPower += JumpBuff_MovePowerBonus; // 基础跳跃力小幅提升（可自定义比例）
                    AimJumpPower += JumpBuff_MovePowerBonus; // 瞄准状态跳跃力小幅提升
                    AimViewBonus += ViewBuff_Bonus; // 视野提升

                    // 标记buff已激活
                    _isYellowBuffActive = true;

                    CountDownManager.Instance.CreateTimer(false, (int)(DurationBuff_Bonus * 1000), () =>
                    {
                        // 减去固定加成值，恢复原始数值
                        movePower -= SpeedBuff_MovePowerBonus;
                        MaxXSpeed -= MaxSpeedBuff_Bonus;
                        AimMovePower -= SpeedBuff_MovePowerBonus;
                        AimMoveMaxSpeed -= MaxSpeedBuff_Bonus;
                        JumpPower -= JumpBuff_MovePowerBonus;
                        AimJumpPower -= JumpBuff_MovePowerBonus;
                        AimViewBonus -= ViewBuff_Bonus;

                        _isYellowBuffActive = false;
                        Debug.Log($"[本地玩家] 黄色针剂buff结束，已恢复原始数值");
                    });

                    UImanager.Instance.GetPanel<PlayerPanel>()?.CreateBuff(MilitaryManager.Instance.GetTacticUISprite(injectionType), DurationBuff_Bonus);
                }

                InjectionColorAnima(SpeedAnimaColor);
                break;
        }
    }

    public void InjectionColorAnima(Color AnimaColor)
    {
        var spriteRenderer = MyMonster?.MyBody?.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("玩家SpriteRenderer为空，无法执行颜色动画");
            return;
        }

        // 颜色动画
        spriteRenderer.DOColor(AnimaColor, InjectionHealTime).OnComplete(() =>
        {
            CountDownManager.Instance.CreateTimer(false, (int)(StayTime * 1000), () => {
                spriteRenderer.DOColor(Color.white, InjectionHealTime);
                IsInInjectionGreenEffect = false;
            });
        });
    }
}
