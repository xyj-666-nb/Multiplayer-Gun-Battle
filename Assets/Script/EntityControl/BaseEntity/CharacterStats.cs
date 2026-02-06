using UnityEngine;
using UnityEngine.Events;

public abstract class  CharacterStats : MonoBehaviour
{
    [Header("角色基础属性")]
    [Space(5)]
    public float maxHealth;//最大生命值
    public float Damage;//攻击力

    [Header("当前玩家的状态")]
    [Space(5)]
    public float Currenthealth;

    private Rigidbody2D _rb2D;

    [Header("是否死亡")]
    [HideInInspector] public bool IsDead = false;

    [Header("是否需要战斗.如果不需要就不用管刀光")]
    public bool IsNeedBattle = true;//是否需要战斗

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
        //制造斩击
        CreateHitEffect(IsCritical);
        //专属效果
        SpecialWoundEffect();

        EntityWoundEvent?.Invoke();//进行特殊触发
    }

    #region 受伤特效（这里为刀光）
    [Header("刀光预制体")]
    public GameObject hurtEffectPrefab;
    public float prefabRotateMinAngle = 0f;
    public float prefabRotateMaxAngle = 360f;
    public void CreateHitEffect(bool IsCritical)
    {
        if (hurtEffectPrefab != null&& IsNeedBattle)
        {
            // 计算碰撞体范围内的随机位置
            Bounds bounds = MyboxCollider.bounds;
            Vector2 randomPos = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            // 计算预制体的随机旋转角度
            float randomRotateAngle = Random.Range(prefabRotateMinAngle, prefabRotateMaxAngle);
            Quaternion randomRotation = Quaternion.Euler(0, 0, randomRotateAngle);

            // 实例化预制体
            var obj = PoolManage.Instance.GetObj(hurtEffectPrefab);
            obj.transform.position = randomPos;
            obj.transform.rotation = randomRotation;

            //————————————攻击暴击的专属效果——————————————————
            if (IsCritical)//如果暴击就设置打击效果为红色
            {
                //放大一点
                obj.transform.localScale *= 1.1f;
                obj.GetComponent<SpriteRenderer>().color = Color.red;
            }
        }
    }

    #endregion

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