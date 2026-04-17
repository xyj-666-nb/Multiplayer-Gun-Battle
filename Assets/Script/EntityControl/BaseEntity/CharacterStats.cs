using DG.Tweening;
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

    [Header("呼吸回血")]
    public float EnterBreatheHealTime = 5;//进入呼吸回血的时间限制
    public float HealSpeed = 40;//回血的速度/每秒
    [SyncVar]//全局同步
    [SerializeField] private float CurrentRemainTime = 5;//当前剩余时间
    private int BreatheHealTaskId;//呼吸回血的任务Id
    private bool IsEnterBreather = false;

    [Header("头盔控制")]
    public Helmet MyHelmet;
    [Header("血量显示")]
    public PlayerWordUI MyWorldUI;

    #region 缓存常量
    private const string STR_UNKNOWN = "未知";
    private const string STR_UNKNOWN_GUN = "未知枪械";
    private const string STR_BARE_HAND = "徒手";
    private const string STR_GRENADE = "手雷";
    private const string LOG_RB_NULL = "[{0}] CharacterStats 缺少 Rigidbody2D 组件！";
    private const string LOG_MANAGER_NULL = "[CharacterStats] PlayerRespawnManager未初始化！";

    // 缓存固定向量，避免高频new Vector2分配内存
    private readonly Vector2 ZERO_VECTOR = Vector2.zero;
    private readonly Vector2 BLOOD_RANDOM_OFFSET_MIN = new Vector2(-0.5f, 0.2f);
    private readonly Vector2 BLOOD_RANDOM_OFFSET_MAX = new Vector2(0.5f, 0.8f);
    private readonly Vector2 GRENADE_BLOOD_OFFSET = new Vector2(-0.4f, 0.4f);

    // 缓存单例引用，避免频繁调用.Instance产生GC
    private BloodParticleGenerator _bloodGenerator;
    private SimpleAnimatorTool _animatorTool;
    private ScreenPulseController _screenPulse;
    private PlayerRespawnManager _respawnManager;
    #endregion

    #region 组件与配置
    private Rigidbody2D _rb2D;
    private bool _hasTriggeredDeath = false;
    private NetworkConnectionToClient _playerConn;

    private string _killerName; // 击杀者名字
    private string _killerGunName; // 击杀者使用的枪械名
    #endregion

    #region 生命周期
    public virtual void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        if (_rb2D == null)
            Debug.LogError(string.Format(LOG_RB_NULL, gameObject.name), this);

        // 一次性缓存所有单例
        CacheSingletonInstances();

        // 订阅死亡事件
        EntityDeathEvent += OnEntityDeath;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentHealth = maxHealth;
        IsDead = false;
        _hasTriggeredDeath = false;
        _playerConn = connectionToClient; // 记录玩家连接
        //初始化呼吸冷却时间
        CurrentRemainTime = EnterBreatheHealTime;
        // 初始化击杀者信息
        if (isServer)
        {
            _killerName = STR_UNKNOWN;
            _killerGunName = STR_UNKNOWN;
        }
    }

    /// <summary>
    /// 缓存全局单例，避免重复查找
    /// </summary>
    private void CacheSingletonInstances()
    {
        _bloodGenerator = BloodParticleGenerator.Instance;
        _animatorTool = SimpleAnimatorTool.Instance;
        _screenPulse = ScreenPulseController.Instance;
        _respawnManager = PlayerRespawnManager.Instance;
    }

    private void OnDestroy()
    {
        EntityDeathEvent -= OnEntityDeath;
    }
    #endregion

    #region 网络同步钩子
    private void OnCurrentHealthChanged(float oldValue, float newValue)
    {
        newValue = Mathf.Clamp(newValue, 0, maxHealth);
        CurrentHealth = newValue;

        if (isLocalPlayer)
        {
            HealthUI.Instance?.SetValue(newValue / Mathf.Max(maxHealth, 1f));
            //调用受攻特效
            if (oldValue >= newValue)
                _screenPulse?.Trigger_Wound();//触发受伤特效  
        }

        if (oldValue >= newValue)
        {
            EntityWoundEvent?.Invoke();
            //打开受伤显示的血条
            MyWorldUI?.ShowInfo();//显示UI
        }
    }

    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue && !_hasTriggeredDeath)
        {
            ClientHandleDeathVisual();
            EntityDeathEvent?.Invoke(); // 仅触发事件，不处理重生
        }
    }
    #endregion

    #region 核心血量操作
    [Command]
    public virtual void CmdChangeHealth(float value, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
            return;

        if (value < 0 || !_respawnManager.IsGameRealStart)//游戏未真正开始就无法扣血
            return;

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

    [Server]
    public virtual void ServerApplyDamage(float damage, Vector2 hitPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead || !_respawnManager.IsGameRealStart)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        Debug.Log($"[ServerApplyDamage] {gameObject.name} 扣血：{healthBefore} → {CurrentHealth}（伤害：{damage}）");
        ResetCoolTime();//重置呼吸恢复时间
        Wound(damage, hitPoint, hitNormal, attacker);
    }

    private void Update()
    {
        if (isServer && _respawnManager != null && _respawnManager.IsGameRealStart)
        {
            HandleBreatheHealTime();
        }
    }

    [Server]
    public virtual void Wound(float finalDamage, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - finalDamage, 0);

        if (CurrentHealth <= 0 && !_hasTriggeredDeath && attacker != null)
        {
            // 获取击杀者名字
            if (attacker is playerStats attackerStats)
            {
                // 优先取攻击者的Main.PlayerName，无则取物体名
                _killerName = UOSRelaySimple.Instance.playerName ?? attacker.gameObject.name;

                // 获取击杀者当前使用的枪械名
                if (attackerStats.MyMonster != null && attackerStats.MyMonster.currentGun != null)
                {
                    _killerGunName = attackerStats.MyMonster.currentGun.gunInfo.name ?? STR_UNKNOWN_GUN;
                }
                else
                {
                    _killerGunName = STR_BARE_HAND;
                }
            }
            else
            {
                _killerName = attacker?.gameObject.name ?? STR_UNKNOWN;
                _killerGunName = STR_UNKNOWN;
            }
        }

        RpcPlayWoundEffect(ColliderPoint, hitNormal, attacker);

        if (CurrentHealth <= 0 && !_hasTriggeredDeath)
        {
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
            _rb2D.velocity = ZERO_VECTOR;
            _rb2D.isKinematic = true;
        }

        Player Deather = gameObject.GetComponent<Player>();//获取当前玩家身上的Player脚本
        //记录死亡数，给对方增加击杀数
        _respawnManager.AddPlayerDeath(Deather.connectionToClient);
        //给击杀者增加击杀数
        _respawnManager.AddPlayerKill(killer.gameObject.GetComponent<Player>().connectionToClient);
        //增加击杀者的队伍比分
        _respawnManager.AddScore(killer.gameObject.GetComponent<Player>().CurrentTeam);//增加比分
    }
    #endregion

    #region 网络特效调用
    [ClientRpc]
    public virtual void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        // 喷血特效逻辑
        if (_bloodGenerator != null)
        {
            _bloodGenerator.GenerateBloodOnBackground(ColliderPoint);
            for (int i = 0; i < BllomAmount; i++)
            {
                Vector2 bloodDir = (hitNormal + new Vector2(Random.Range(BLOOD_RANDOM_OFFSET_MIN.x, BLOOD_RANDOM_OFFSET_MAX.x), Random.Range(BLOOD_RANDOM_OFFSET_MIN.y, BLOOD_RANDOM_OFFSET_MAX.y))).normalized;
                float bloodSpeed = Random.Range(MaxBllomSpeed, MinBllomSpeed);
                _bloodGenerator.GenerateBloodParticle(ColliderPoint, bloodDir * bloodSpeed);
            }
        }

        // 本地玩家击退+屏幕震动
        if (isLocalPlayer && attacker != null && _rb2D != null)
        {
            var Attacker = attacker as playerStats;
            float knockbackDir = Mathf.Sign(ColliderPoint.x - attacker.transform.position.x);
            _rb2D.AddForce(new Vector2(knockbackDir * Attacker.MyMonster.currentGun.gunInfo.Recoil_Enemy, 0), ForceMode2D.Impulse);
            MyCameraControl.Instance.AddTimeBasedShake(Attacker.MyMonster.currentGun.gunInfo.ShackStrength_Enemy, Attacker.MyMonster.currentGun.gunInfo.ShackTime_Enemy);
            Debug.Log("触发屏幕震动");
        }
    }
    #endregion

    #region 客户端视觉表现
    protected virtual void ClientHandleDeathVisual()
    {
        MyHelmet.TriggerHelmetDrop();//触发头盔掉落
        if (isLocalPlayer)
        {
            Debug.Log("[ClientHandleDeathVisual] 本地玩家死亡，清理输入/摄像机");
        }
    }
    #endregion

    #region 死亡回调
    private void OnEntityDeath()
    {
        if (isLocalPlayer)
        {
            CmdActiveCurrentPlayer(false);
            CmdRequestRespawn();
        }
    }

    [Command]
    private void CmdRequestRespawn()
    {
        if (_respawnManager != null)
        {
            string finalKillerName = string.IsNullOrEmpty(_killerName) ? STR_UNKNOWN : _killerName;
            string finalKillerGunName = string.IsNullOrEmpty(_killerGunName) ? STR_UNKNOWN : _killerGunName;

            _respawnManager.TargetShowDeathPanel(_playerConn, _respawnManager.respawnDelay, finalKillerName, finalKillerGunName);
            _respawnManager.RespawnPlayer(_playerConn);
        }
        else
        {
            Debug.LogError(LOG_MANAGER_NULL);
        }
    }

    [Command]
    public void CmdActiveCurrentPlayer(bool IsActive)
    {
        RPCActiveCurrentPlayer(IsActive);
    }

    [ClientRpc]
    public void RPCActiveCurrentPlayer(bool IsActive)
    {
        if (IsActive)
        {
            gameObject.SetActive(true);
            gameObject.GetComponentInChildren<SpriteRenderer>().DOFade(1, 0.2f);
            if (_rb2D != null)
            {
                _rb2D.isKinematic = false;
                _rb2D.simulated = true;
            }
        }
        else
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().DOFade(0, 0.2f)
                .OnComplete(() => { gameObject.SetActive(false); });
        }
    }
    #endregion

    #region 手雷专属受伤逻辑
    [Server]
    public void ServerApplyGrenadeDamage(float damage, Vector2 explosionCenter, Vector2 knockbackForce, CharacterStats attacker)
    {
        if (IsDead)
            return;

        float healthBefore = CurrentHealth;
        if (_respawnManager.IsGameRealStart)
        {
            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
            Debug.Log($"[ServerApplyGrenadeDamage] {gameObject.name} 被手雷炸中！血量: {healthBefore} -> {CurrentHealth}, 伤害: {damage}");
        }


        Vector2 hitDir = ((Vector2)transform.position - explosionCenter).normalized;
        Vector2 hitPoint = (Vector2)transform.position;

        if (CurrentHealth <= 0 && !_hasTriggeredDeath && attacker != null)
        {
            if (attacker is playerStats attackerStats)
            {
                _killerName = UOSRelaySimple.Instance.playerName ?? attacker.gameObject.name;
                _killerGunName = STR_GRENADE;
            }
            else
            {
                _killerName = attacker?.gameObject.name ?? STR_UNKNOWN;
                _killerGunName = STR_GRENADE;
            }
        }

        RpcPlayGrenadeEffect(hitPoint, hitDir, explosionCenter, knockbackForce, attacker);

        if (CurrentHealth <= 0 && !_hasTriggeredDeath)
        {
            Death(attacker);
        }
    }

    [ClientRpc]
    private void RpcPlayGrenadeEffect(Vector2 hitPoint, Vector2 hitDir, Vector2 explosionCenter, Vector2 knockbackForce, CharacterStats attacker)
    {
        if (_bloodGenerator != null)
        {
            _bloodGenerator.GenerateBloodOnBackground(hitPoint);
            for (int i = 0; i < BllomAmount; i++)
            {
                Vector2 bloodDir = (hitDir + new Vector2(Random.Range(GRENADE_BLOOD_OFFSET.x, GRENADE_BLOOD_OFFSET.y), Random.Range(GRENADE_BLOOD_OFFSET.x, GRENADE_BLOOD_OFFSET.y))).normalized;
                float bloodSpeed = Random.Range(MinBllomSpeed, MaxBllomSpeed);
                _bloodGenerator.GenerateBloodParticle(hitPoint, bloodDir * bloodSpeed);
            }
        }

        if (isLocalPlayer && _rb2D != null)
        {
            _rb2D.velocity = ZERO_VECTOR; // 先清零，防止叠加
            _rb2D.AddForce(knockbackForce, ForceMode2D.Impulse);

            // 屏幕震动依然保留在这里，因为这是客户端表现
            float grenadeShakeStrength = 3;
            float grenadeShakeTime = 0.4f;
            MyCameraControl.Instance?.AddTimeBasedShake(grenadeShakeStrength, grenadeShakeTime);

            Debug.Log($"本地玩家被手雷炸飞！受力: {knockbackForce}");
        }
    }
    #endregion

    [Server]
    public void HandleBreatheHealTime()
    {
        if (CurrentHealth >= maxHealth)
        {
            if (Mathf.Abs(CurrentRemainTime - EnterBreatheHealTime) > 0.01f)
                CurrentRemainTime = EnterBreatheHealTime;

            if (IsEnterBreather)
            {
                IsEnterBreather = false;
                // 确保停止旧的任务
                _animatorTool?.StopFloatLerpById(BreatheHealTaskId);
            }
            return;
        }

        if (IsEnterBreather)
        {
            return;
        }

        CurrentRemainTime -= Time.deltaTime;

        if (CurrentRemainTime <= 0)
        {
            IsEnterBreather = true;

            // 开始回血
            float duration = (maxHealth - CurrentHealth) / HealSpeed;
            //触发一次回血的特效
            _screenPulse?.Trigger_Heal();
            BreatheHealTaskId = _animatorTool.StartFloatLerp(
                CurrentHealth,
                maxHealth,
                duration,
                (Value) => {
                    CurrentHealth = Value;
                },
                () => {
                    ResetCoolTime();
                }
            );
        }
    }

    public void ResetCoolTime()
    {
        CurrentRemainTime = EnterBreatheHealTime;//重置时间
        IsEnterBreather = false;
        //重置状态
        _animatorTool?.StopFloatLerpById(BreatheHealTaskId);//先暂停任务
    }
}