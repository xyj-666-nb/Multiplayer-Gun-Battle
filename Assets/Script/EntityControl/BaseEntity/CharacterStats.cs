using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class CharacterStats : NetworkBehaviour
{
    [Header("角色基础属性")]
    [Space(5)]
    public float maxHealth = 100f;//最大生命值

    [Header("当前玩家的状态")]
    [Space(5)]
    [SyncVar(hook = nameof(OnCurrentHealthChanged))]
    public float CurrentHealth;

    [Header("是否死亡")]
    [HideInInspector]
    [SyncVar(hook = nameof(OnIsDeadChanged))]
    public bool IsDead = false;

    [Header("受伤事件")]
    public UnityAction EntityWoundEvent;//外部关联受伤事件
    [Header("死亡事件")]
    public UnityAction EntityDeathEvent;//外部关联死亡事件

    [Header("血液飞溅参数")]
    public float MaxBllomSpeed = 2f;//血液飞溅的最大速度
    public float MinBllomSpeed = 4f;//血液飞溅的最小速度
    public float BllomAmount = 20;//血液飞溅的数量
    #region 组件与配置
    private Rigidbody2D _rb2D;
    private bool _hasTriggeredDeath = false;
    #endregion

    #region 生命周期
    public virtual void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        if (_rb2D == null)
            Debug.LogError($"[{gameObject.name}] CharacterStats 缺少 Rigidbody2D 组件！", this);

        if (isLocalPlayer)
        {
            CurrentHealth = maxHealth;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentHealth = maxHealth;
        IsDead = false;
        _hasTriggeredDeath = false;
    }
    #endregion

    #region 网络同步钩子
    private void OnCurrentHealthChanged(float oldValue, float newValue)
    {
        newValue = Mathf.Clamp(newValue, 0, maxHealth);
        CurrentHealth = newValue;

        if(isLocalPlayer)
          HealthUI.Instance?.SetValue(newValue / Mathf.Max(maxHealth, 1f));
        if (oldValue >= newValue)
            EntityWoundEvent?.Invoke();
    }

    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue && !_hasTriggeredDeath)
        {
            ClientHandleDeathVisual();
            EntityDeathEvent?.Invoke();
        }
    }
    #endregion

    #region 核心血量操作
    /// <summary>
    /// [Command] 客户端请求服务器修改自身血量
    /// </summary>
    [Command] 
    public void CmdChangeHealth(float value, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead) return;

        float newHealth = CurrentHealth + value;
        newHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        if (value < 0)
        {
            Wound(Mathf.Abs(value), ColliderPoint, hitNormal, attacker);
        }
        else
        {
            CurrentHealth = newHealth;
        }
    }

    /// <summary>
    /// 服务器直接扣血
    /// </summary>
    [Server]
    public void ServerApplyDamage(float damage, Vector2 hitPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
        {
            return;
        }

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        Debug.Log($"[ServerApplyDamage] {gameObject.name} 扣血：{healthBefore} → {CurrentHealth}（伤害：{damage}）");

        Wound(damage, hitPoint, hitNormal, attacker);
    }

    [Server]
    public virtual void Wound(float finalDamage, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
        {
            return;
        }

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - finalDamage, 0);

        RpcPlayWoundEffect(ColliderPoint, hitNormal, attacker);

        if (CurrentHealth <= 0 && !_hasTriggeredDeath)
        {
            Debug.Log($"[Wound] {gameObject.name} 血量扣至0，触发死亡！");
            Death(attacker);
        }
    }

    [Server]
    public virtual void Death(CharacterStats killer)
    {
        if (IsDead || _hasTriggeredDeath)
            return;

        IsDead = true;
        _hasTriggeredDeath = true;

        if (_rb2D != null)
        {
            _rb2D.velocity = Vector2.zero;
            _rb2D.isKinematic = true;
        }
    }
    #endregion

    #region 网络特效调用
    [ClientRpc]
    public virtual void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        // 喷血特效逻辑
        //进行喷血
        if (BloodParticleGenerator.Instance != null)
        {
            // 生成背景静态血迹
            BloodParticleGenerator.Instance.GenerateBloodOnBackground(ColliderPoint);

            // 循环生成多个血粒子
            for (int i = 0; i < BllomAmount; i++)
            {
                // 随机喷血方向
                Vector2 bloodDir = (hitNormal + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.8f))).normalized;
                // 随机喷血速度
                float bloodSpeed = Random.Range(MaxBllomSpeed, MinBllomSpeed);
                // 生成粒子
                BloodParticleGenerator.Instance.GenerateBloodParticle(ColliderPoint, bloodDir * bloodSpeed);
            }
        }

    }
    #endregion

    #region 客户端视觉表现
    protected virtual void ClientHandleDeathVisual()
    {
    }
    #endregion
}