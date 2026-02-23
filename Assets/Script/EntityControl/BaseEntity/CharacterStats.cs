using DG.Tweening;
using Mirror;
using Unity.VisualScripting;
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
    private NetworkConnectionToClient _playerConn;

    private string _killerName; // 击杀者名字
    private string _killerGunName; // 击杀者使用的枪械名

    #endregion

    #region 生命周期
    public virtual void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        if (_rb2D == null)
            Debug.LogError($"[{gameObject.name}] CharacterStats 缺少 Rigidbody2D 组件！", this);

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

        // 初始化击杀者信息
        if (isServer)
        {
            _killerName = "未知";
            _killerGunName = "未知";
        }
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
            HealthUI.Instance?.SetValue(newValue / Mathf.Max(maxHealth, 1f));

        if (oldValue >= newValue)
            EntityWoundEvent?.Invoke();
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
    public void CmdChangeHealth(float value, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
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
    public void ServerApplyDamage(float damage, Vector2 hitPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        Debug.Log($"[ServerApplyDamage] {gameObject.name} 扣血：{healthBefore} → {CurrentHealth}（伤害：{damage}）");

        Wound(damage, hitPoint, hitNormal, attacker);
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
                _killerName = Main.PlayerName ?? attacker.gameObject.name;

                // 获取击杀者当前使用的枪械名
                if (attackerStats.MyMonster != null && attackerStats.MyMonster.currentGun != null)
                {
                    _killerGunName = attackerStats.MyMonster.currentGun.gunInfo.name ?? "未知枪械";
                }
                else
                {
                    _killerGunName = "徒手";
                }
            }
            else
            {
                _killerName = attacker?.gameObject.name ?? "未知";
                _killerGunName = "未知";
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
        if (IsDead || _hasTriggeredDeath) return;

        IsDead = true;
        _hasTriggeredDeath = true;

        if (_rb2D != null)
        {
            _rb2D.velocity = Vector2.zero;
            _rb2D.isKinematic = true;
        }

        Player Deather= gameObject.GetComponent<Player>();//获取当前玩家身上的Player脚本
        //记录死亡数，给对方增加击杀数
        PlayerRespawnManager.Instance.AddPlayerDeath(Deather.connectionToClient);
        //给击杀者增加击杀数
        PlayerRespawnManager.Instance.AddPlayerKill(killer.gameObject.GetComponent<Player>().connectionToClient);
        //增加击杀者的队伍比分
        PlayerRespawnManager.Instance.AddScore(killer.gameObject.GetComponent<Player>().CurrentTeam);//增加比分
    }
    #endregion

    #region 网络特效调用
    [ClientRpc]
    public virtual void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        // 喷血特效逻辑
        if (BloodParticleGenerator.Instance != null)
        {
            BloodParticleGenerator.Instance.GenerateBloodOnBackground(ColliderPoint);
            for (int i = 0; i < BllomAmount; i++)
            {
                Vector2 bloodDir = (hitNormal + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.8f))).normalized;
                float bloodSpeed = Random.Range(MaxBllomSpeed, MinBllomSpeed);
                BloodParticleGenerator.Instance.GenerateBloodParticle(ColliderPoint, bloodDir * bloodSpeed);
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
        if (PlayerRespawnManager.Instance != null)
        {
            string finalKillerName = string.IsNullOrEmpty(_killerName) ? "未知" : _killerName;
            string finalKillerGunName = string.IsNullOrEmpty(_killerGunName) ? "未知" : _killerGunName;

            PlayerRespawnManager.Instance.TargetShowDeathPanel(_playerConn, PlayerRespawnManager.Instance.respawnDelay, finalKillerName, finalKillerGunName);
            PlayerRespawnManager.Instance.RespawnPlayer(_playerConn);
        }
        else
        {
            Debug.LogError("[CharacterStats] PlayerRespawnManager未初始化！");
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

            //这里还没写强制丢枪的逻辑，后续可以在这里添加
            //// 强制丢枪（示例）
            //if (this is playerStats playerStats && playerStats.CurrentGun != null)
            //{
            //    playerStats.DropCurrentGun();
            //}
        }
    }
    #endregion

    #region 手雷专属受伤逻辑
    /// <summary>
    /// 【服务器端】被手雷炸伤时调用
    /// </summary>
    /// <param name="damage">最终伤害值</param>
    /// <param name="explosionCenter">爆炸中心点坐标</param>
    /// <param name="knockbackForce">计算好的击退力矢量</param>
    /// <param name="attacker">投掷手雷的攻击者</param>
    [Server]
    public void ServerApplyGrenadeDamage(float damage, Vector2 explosionCenter, Vector2 knockbackForce, CharacterStats attacker)
    {
        if (IsDead)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        Debug.Log($"[ServerApplyGrenadeDamage] {gameObject.name} 被手雷炸中！血量: {healthBefore} -> {CurrentHealth}, 伤害: {damage}");

        Vector2 hitDir = ((Vector2)transform.position - explosionCenter).normalized;
        Vector2 hitPoint = (Vector2)transform.position;

        if (CurrentHealth <= 0 && !_hasTriggeredDeath && attacker != null)
        {
            if (attacker is playerStats attackerStats)
            {
                _killerName = Main.PlayerName ?? attacker.gameObject.name;
                _killerGunName = "手雷";
            }
            else
            {
                _killerName = attacker?.gameObject.name ?? "未知";
                _killerGunName = "手雷";
            }
        }

        // 【修改】传入计算好的 knockbackForce
        RpcPlayGrenadeEffect(hitPoint, hitDir, explosionCenter, knockbackForce, attacker);

        if (CurrentHealth <= 0 && !_hasTriggeredDeath)
        {
            Death(attacker);
        }
    }

    /// <summary>
    /// 【客户端】播放手雷爆炸的视觉/物理反馈
    /// </summary>
    [ClientRpc]
    private void RpcPlayGrenadeEffect(Vector2 hitPoint, Vector2 hitDir, Vector2 explosionCenter, Vector2 knockbackForce, CharacterStats attacker)
    {
        if (BloodParticleGenerator.Instance != null)
        {
            BloodParticleGenerator.Instance.GenerateBloodOnBackground(hitPoint);
            for (int i = 0; i < BllomAmount; i++)
            {
                Vector2 bloodDir = (hitDir + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f))).normalized;
                float bloodSpeed = Random.Range(MinBllomSpeed, MaxBllomSpeed);
                BloodParticleGenerator.Instance.GenerateBloodParticle(hitPoint, bloodDir * bloodSpeed);
            }
        }

        if (isLocalPlayer && _rb2D != null)
        {
            // 【修改】直接应用手雷传过来的力
            _rb2D.velocity = Vector2.zero; // 先清零，防止叠加
            _rb2D.AddForce(knockbackForce, ForceMode2D.Impulse);

            // 屏幕震动依然保留在这里，因为这是客户端表现
            float grenadeShakeStrength = 3;
            float grenadeShakeTime = 0.4f;
            MyCameraControl.Instance?.AddTimeBasedShake(grenadeShakeStrength, grenadeShakeTime);

            Debug.Log($"本地玩家被手雷炸飞！受力: {knockbackForce}");
        }
    }
    #endregion
}