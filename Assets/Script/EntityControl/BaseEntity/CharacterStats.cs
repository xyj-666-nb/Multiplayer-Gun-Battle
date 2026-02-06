using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class  CharacterStats : NetworkBehaviour
{
    [Header("角色基础属性")]
    [Space(5)]
    public float maxHealth;//最大生命值

    [Header("当前玩家的状态")]
    [Space(5)]
    public float Currenthealth;

    private Rigidbody2D _rb2D;

    [Header("是否死亡")]
    [HideInInspector] public bool IsDead = false;

    [Header("受伤事件")]
    public UnityAction EntityWoundEvent;//在外部进行关联对应的受伤事件
    [Header("死亡事件")]
    public UnityAction EntityDeathEvent;//在外部进行关联对应的受伤事件
    public virtual void Awake()
    {
        Currenthealth = maxHealth;
        MyentityFX = gameObject.GetComponent<EntityFX>();
        MyboxCollider = gameObject.GetComponent<BoxCollider2D>();

        _rb2D = gameObject.GetComponent<Rigidbody2D>();
    }


    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="FinalDamage">传入基础伤害</param>
    /// <param name="Attacker">攻击者</param>
    /// <param name="defender">防御者</param>
    /// <param name="IsCritical">是否暴击，默认为false，有的游戏没有就忽略</param>
    public virtual void Wound(float FinalDamage, bool IsCritical=false, CharacterStats Attacker = null, CharacterStats defender = null)
    {
        Currenthealth -= FinalDamage;//减去伤害
        if (Currenthealth <= 0)
        {
            Death();
        }
        HandleSpecialBothsides(Attacker, defender);
        //专属效果
        SpecialWoundEffect();

        EntityWoundEvent?.Invoke();//进行特殊触发
    }

    #region 特殊受伤效果
    /// <summary>
    /// 专属受伤效果，击退，爆金币等等
    /// </summary>
    public abstract void SpecialWoundEffect();//如果没有就可以空放着

    public abstract void HandleSpecialBothsides(CharacterStats Attacker, CharacterStats defender);//对双方的特殊处理,没有就空着


    public virtual void Death()
    {
        IsDead = true;
        EntityDeathEvent?.Invoke();
    }
    #endregion

    #region 受伤闪光效果

    [Header("受伤闪光设置")]
    protected float WoundFlashDuration = 0.3f;
    protected float WoundFlashSpeed = 0.03f;
    [HideInInspector] public EntityFX MyentityFX;
    [HideInInspector] public BoxCollider2D MyboxCollider;


    /// <summary>
    /// 设置受伤闪烁效果
    /// </summary>
    /// <param name="_Duration"></param>
    /// <param name="_Speed"></param>
    public void Set_WoundFlash(float _Duration, float _Speed)
    {
        WoundFlashDuration = _Duration;
        WoundFlashSpeed = _Speed;
    }
    #endregion
}