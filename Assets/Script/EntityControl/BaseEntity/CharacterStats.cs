using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class CharacterStats : NetworkBehaviour
{
    [Header("最大生命")]
    [Space(5)]
    public float maxHealth = 100f;

    [Header("当前枪械")]
    [Space(5)]
    [SyncVar(hook = nameof(OnCurrentHealthChanged))]
    public float CurrentHealth;

    [Header("是否死亡")]
    [HideInInspector]
    [SyncVar(hook = nameof(OnIsDeadChanged))]
    public bool IsDead = false;

    [Header("受伤事件")]
    public UnityAction EntityWoundEvent;
    [Header("死亡事件")]
    public UnityAction EntityDeathEvent;

    [Header("弹跳设置")]
    public float MaxBllomSpeed = 2f;
    public float MinBllomSpeed = 4f;
    public float BllomAmount = 20;

    [Header("呼吸回血数值")]
    public float EnterBreatheHealTime = 5;
    public float HealSpeed = 30;
    [SyncVar]
    [SerializeField] private float CurrentRemainTime = 5;
    private int BreatheHealTaskId;
    private bool IsEnterBreather = false;

    [Header("护甲")]
    public Helmet MyHelmet;
    [Header("世界信息UI")]
    public PlayerWordUI MyWorldUI;

    #region ???泣??
    private const string STR_UNKNOWN = "δ?";
    private const string STR_UNKNOWN_GUN = "δ??е";
    private const string STR_BARE_HAND = "???";
    private const string STR_GRENADE = "????";
    private const string SOUND_PLAYER_HIT = "Music/\u6B63\u5F0F/\u4EA4\u4E92/\u51FB\u4E2D\u654C\u4EBA";
    private const string SOUND_PLAYER_WOUND = "Music/\u6B63\u5F0F/\u4EA4\u4E92/\u53D7\u51FB";
    private const string SOUND_PLAYER_KILL = "Music/\u6B63\u5F0F/\u4EA4\u4E92/\u51FB\u6740";
    private const string LOG_RB_NULL = "[{0}] CharacterStats ??? Rigidbody2D ?????";
    private const string LOG_MANAGER_NULL = "[CharacterStats] PlayerRespawnManagerδ???????";

    private readonly Vector2 ZERO_VECTOR = Vector2.zero;
    private readonly Vector2 BLOOD_RANDOM_OFFSET_MIN = new Vector2(-0.5f, 0.2f);
    private readonly Vector2 BLOOD_RANDOM_OFFSET_MAX = new Vector2(0.5f, 0.8f);
    private readonly Vector2 GRENADE_BLOOD_OFFSET = new Vector2(-0.4f, 0.4f);

    private BloodParticleGenerator _bloodGenerator;
    private SimpleAnimatorTool _animatorTool;
    private ScreenPulseController _screenPulse;
    private PlayerRespawnManager _respawnManager;
    #endregion

    #region
    private Rigidbody2D _rb2D;
    private bool _hasTriggeredDeath = false;
    private NetworkConnectionToClient _playerConn;

    private string _killerName;
    private string _killerGunName;
    #endregion

    #region
    public virtual void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();

        CacheSingletonInstances();

        EntityDeathEvent += OnEntityDeath;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentHealth = maxHealth;
        IsDead = false;
        _hasTriggeredDeath = false;
        _playerConn = connectionToClient; // ??????????
        //???????????????
        CurrentRemainTime = EnterBreatheHealTime;
        // ?????????????
        if (isServer)
        {
            _killerName = STR_UNKNOWN;
            _killerGunName = STR_UNKNOWN;
        }
    }

    /// <summary>
    /// ???????????????????????
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

    #region
    private void OnCurrentHealthChanged(float oldValue, float newValue)
    {
        newValue = Mathf.Clamp(newValue, 0, maxHealth);
        CurrentHealth = newValue;

        if (isLocalPlayer)
        {
            HealthUI.Instance?.SetValue(newValue / Mathf.Max(maxHealth, 1f));
            if (oldValue >= newValue)
                _screenPulse?.Trigger_Wound();
        }

        if (oldValue >= newValue)
        {
            EntityWoundEvent?.Invoke();
            MyWorldUI?.ShowInfo();
        }
    }

    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        if (newValue && !oldValue && !_hasTriggeredDeath)
        {
            ClientHandleDeathVisual();
            EntityDeathEvent?.Invoke(); // ?????????????????????
        }
    }
    #endregion

    #region
    [Command]
    public virtual void CmdChangeHealth(float value, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
            return;

        EnsureRespawnManager();

        if (value < 0)
        {
            if (_respawnManager != null && !_respawnManager.IsGameRealStart)
                return;

            float damage = ModifyIncomingDamage(Mathf.Abs(value));
            if (damage <= 0f)
                return;

            Wound(damage, ColliderPoint, hitNormal, attacker);
        }
        else
        {
            CurrentHealth = Mathf.Clamp(value, 0, maxHealth);
        }
    }

    [Server]
    public virtual void ServerApplyDamage(float damage, Vector2 hitPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        EnsureRespawnManager();

        if (IsDead || (_respawnManager != null && !_respawnManager.IsGameRealStart))
            return;

        damage = ModifyIncomingDamage(damage);
        if (damage <= 0f)
            return;

        Wound(damage, hitPoint, hitNormal, attacker);
    }

    private void Update()
    {
        if (!isServer || IsDead)
            return;

        EnsureRuntimeSingletons();
        if (_respawnManager != null && _respawnManager.IsGameRealStart)
        {
            HandleBreatheHealTime();
        }
    }

    [Server]
    public virtual void Wound(float finalDamage, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead || finalDamage <= 0f)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth = Mathf.Max(CurrentHealth - finalDamage, 0);
        ResetCoolTime();

        if (CurrentHealth <= 0 && !_hasTriggeredDeath && attacker != null)
        {
            // ????????????
            if (attacker is playerStats attackerStats)
            {
                Player attackerPlayer = attackerStats.MyMonster;
                _killerName = attackerPlayer != null ? attackerPlayer.GetDisplayName() : attacker.gameObject.name;

                if (attackerPlayer != null && attackerPlayer.currentGun != null && attackerPlayer.currentGun.gunInfo != null)
                {
                    _killerGunName = attackerPlayer.currentGun.gunInfo.name ?? STR_UNKNOWN_GUN;
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

        EnsureRespawnManager();

        IsDead = true;
        _hasTriggeredDeath = true;

        if (_rb2D != null)
        {
            _rb2D.velocity = ZERO_VECTOR;
            _rb2D.isKinematic = true;
        }

        Player Deather = gameObject.GetComponent<Player>();
        Player killerPlayer = killer != null ? killer.gameObject.GetComponent<Player>() : null;
        bool isSuicide = killerPlayer != null && Deather != null && killerPlayer == Deather;
        NetworkConnectionToClient deadConn = Deather != null ? Deather.connectionToClient : null;

        RPCActiveCurrentPlayer(false);

        if (_respawnManager != null && deadConn != null)
        {
            _respawnManager.AddPlayerDeath(deadConn);
            TargetPlayDeathFeedback(deadConn);
        }

        if (_respawnManager != null && killerPlayer != null && killerPlayer.connectionToClient != null && !isSuicide)
        {
            _respawnManager.AddPlayerKill(killerPlayer.connectionToClient);
            _respawnManager.AddScore(killerPlayer.CurrentTeam);
            TargetPlayKillFeedback(killerPlayer.connectionToClient);
        }

        if (_respawnManager != null && deadConn != null && _respawnManager.IsGameStart && !_respawnManager._isGameEnded)
        {
            string finalKillerName = string.IsNullOrEmpty(_killerName) ? STR_UNKNOWN : _killerName;
            string finalKillerGunName = string.IsNullOrEmpty(_killerGunName) ? STR_UNKNOWN : _killerGunName;

            _respawnManager.TargetShowDeathPanel(deadConn, _respawnManager.respawnDelay, finalKillerName, finalKillerGunName);
            _respawnManager.RespawnPlayer(deadConn);
        }
    }
    #endregion

    #region
    #endregion

    #region
    [ClientRpc]
    public virtual void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
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

        if (attacker != null && attacker.isLocalPlayer)
        {
            MusicManager.Instance?.PlayEffect(SOUND_PLAYER_HIT, 0.9f);
        }

        if (isLocalPlayer && attacker != null && _rb2D != null)
        {
            MusicManager.Instance?.PlayEffect(SOUND_PLAYER_WOUND, 1f);
            var attackerStats = attacker as playerStats;
            var attackerGunInfo = attackerStats?.MyMonster?.currentGun?.gunInfo;
            if (attackerGunInfo == null)
                return;

            float knockbackDir = Mathf.Sign(ColliderPoint.x - attacker.transform.position.x);
            _rb2D.AddForce(new Vector2(knockbackDir * attackerGunInfo.Recoil_Enemy, 0), ForceMode2D.Impulse);
            MyCameraControl.Instance?.AddTimeBasedShake(attackerGunInfo.ShackStrength_Enemy, attackerGunInfo.ShackTime_Enemy);
        }
    }
    #endregion

    [TargetRpc]
    private void TargetPlayDeathFeedback(NetworkConnectionToClient target)
    {
        MusicManager.Instance?.PlayEffect(SOUND_PLAYER_WOUND, 1f);
    }

    [TargetRpc]
    private void TargetPlayKillFeedback(NetworkConnectionToClient target)
    {
        MusicManager.Instance?.PlayEffect(SOUND_PLAYER_KILL, 1f);
    }

    #region
    protected virtual void ClientHandleDeathVisual()
    {
        MyHelmet.TriggerHelmetDrop();

    }
    #endregion

    #region
    private void OnEntityDeath()
    {
        // Respawn is scheduled on the server in Death().
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
            /* Debug.LogError(LOG_MANAGER_NULL); */
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

    #region
    [Server]
    public void ServerApplyGrenadeDamage(float damage, Vector2 explosionCenter, Vector2 knockbackForce, CharacterStats attacker)
    {
        EnsureRespawnManager();

        if (IsDead)
            return;

        damage = ModifyIncomingDamage(damage);
        if (damage <= 0f)
            return;

        float healthBefore = CurrentHealth;
        if (_respawnManager == null || _respawnManager.IsGameRealStart)
        {
            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
            ResetCoolTime();
            /* Debug.Log($"[ServerApplyGrenadeDamage] {gameObject.name} ????????У????: {healthBefore} -> {CurrentHealth}, ???: {damage}"); */
        }


        Vector2 hitDir = ((Vector2)transform.position - explosionCenter).normalized;
        Vector2 hitPoint = (Vector2)transform.position;

        if (CurrentHealth <= 0 && !_hasTriggeredDeath && attacker != null)
        {
            if (attacker is playerStats attackerStats)
            {
                Player attackerPlayer = attackerStats.MyMonster;
                _killerName = attackerPlayer != null ? attackerPlayer.GetDisplayName() : attacker.gameObject.name;
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

        if (attacker != null && attacker.isLocalPlayer)
        {
            MusicManager.Instance?.PlayEffect(SOUND_PLAYER_HIT, 0.9f);
        }
        if (isLocalPlayer && _rb2D != null)
        {
            MusicManager.Instance?.PlayEffect(SOUND_PLAYER_WOUND, 1f);
            _rb2D.velocity = ZERO_VECTOR; // ?????????????
            _rb2D.AddForce(knockbackForce, ForceMode2D.Impulse);

            // ????????????????????????????????
            float grenadeShakeStrength = 3;
            float grenadeShakeTime = 0.4f;
            MyCameraControl.Instance?.AddTimeBasedShake(grenadeShakeStrength, grenadeShakeTime);

            /* Debug.Log($"????????????????????: {knockbackForce}"); */
        }
    }
    #endregion

    [Server]
    public void HandleBreatheHealTime()
    {
        if (maxHealth <= 0)
            return;

        if (CurrentHealth >= maxHealth)
        {
            CurrentHealth = maxHealth;
            if (Mathf.Abs(CurrentRemainTime - EnterBreatheHealTime) > 0.01f)
                CurrentRemainTime = EnterBreatheHealTime;

            if (IsEnterBreather)
            {
                IsEnterBreather = false;
                _animatorTool?.StopFloatLerpById(BreatheHealTaskId);
            }
            return;
        }

        if (CurrentRemainTime > 0)
        {
            CurrentRemainTime -= Time.deltaTime;
            return;
        }

        if (!IsEnterBreather)
        {
            IsEnterBreather = true;
            if (isLocalPlayer)
                _screenPulse?.Trigger_Heal();
        }

        float healPerSecond = Mathf.Max(0.1f, HealSpeed);
        CurrentHealth = Mathf.Min(CurrentHealth + healPerSecond * Time.deltaTime, maxHealth);

        if (CurrentHealth >= maxHealth)
        {
            ResetCoolTime();
        }
    }

    public void ResetCoolTime()
    {
        CurrentRemainTime = EnterBreatheHealTime;//???????
        IsEnterBreather = false;
        //??????
        _animatorTool?.StopFloatLerpById(BreatheHealTaskId);//?????????
    }

    protected virtual float ModifyIncomingDamage(float damage)
    {
        return Mathf.Max(0f, damage);
    }
    private void EnsureRespawnManager()
    {
        if (_respawnManager == null)
            _respawnManager = PlayerRespawnManager.Instance;
    }

    private void EnsureRuntimeSingletons()
    {
        EnsureRespawnManager();
        if (_animatorTool == null)
            _animatorTool = SimpleAnimatorTool.Instance;
        if (_screenPulse == null)
            _screenPulse = ScreenPulseController.Instance;
    }
}
