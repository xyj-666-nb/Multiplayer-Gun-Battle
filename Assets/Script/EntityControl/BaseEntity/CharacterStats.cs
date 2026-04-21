using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class CharacterStats : NetworkBehaviour
{
    [Header("???????????")]
    [Space(5)]
    public float maxHealth = 100f;//????????

    [Header("?????????")]
    [Space(5)]
    [SyncVar(hook = nameof(OnCurrentHealthChanged))]
    public float CurrentHealth;

    [Header("???????")]
    [HideInInspector]
    [SyncVar(hook = nameof(OnIsDeadChanged))]
    public bool IsDead = false;

    [Header("???????")]
    public UnityAction EntityWoundEvent;//?????????????
    [Header("???????")]
    public UnityAction EntityDeathEvent;//?????????????

    [Header("?????????")]
    public float MaxBllomSpeed = 2f;//?????????????
    public float MinBllomSpeed = 4f;//?????????§³???
    public float BllomAmount = 20;//???????????

    [Header("???????")]
    public float EnterBreatheHealTime = 5;//???????????????????
    public float HealSpeed = 40;//????????/???
    [SyncVar]//??????
    [SerializeField] private float CurrentRemainTime = 5;//?????????
    private int BreatheHealTaskId;//?????????????Id
    private bool IsEnterBreather = false;

    [Header("???????")]
    public Helmet MyHelmet;
    [Header("??????")]
    public PlayerWordUI MyWorldUI;

    #region ???Æü??
    private const string STR_UNKNOWN = "¦Ä?";
    private const string STR_UNKNOWN_GUN = "¦Ä??§Ö";
    private const string STR_BARE_HAND = "???";
    private const string STR_GRENADE = "????";
    private const string LOG_RB_NULL = "[{0}] CharacterStats ??? Rigidbody2D ?????";
    private const string LOG_MANAGER_NULL = "[CharacterStats] PlayerRespawnManager¦Ä???????";

    // ??????????????????new Vector2???????
    private readonly Vector2 ZERO_VECTOR = Vector2.zero;
    private readonly Vector2 BLOOD_RANDOM_OFFSET_MIN = new Vector2(-0.5f, 0.2f);
    private readonly Vector2 BLOOD_RANDOM_OFFSET_MAX = new Vector2(0.5f, 0.8f);
    private readonly Vector2 GRENADE_BLOOD_OFFSET = new Vector2(-0.4f, 0.4f);

    // ???›Å??????????????????.Instance????GC
    private BloodParticleGenerator _bloodGenerator;
    private SimpleAnimatorTool _animatorTool;
    private ScreenPulseController _screenPulse;
    private PlayerRespawnManager _respawnManager;
    #endregion

    #region ?????????
    private Rigidbody2D _rb2D;
    private bool _hasTriggeredDeath = false;
    private NetworkConnectionToClient _playerConn;

    private string _killerName; // ?????????
    private string _killerGunName; // ??????????§Ö??
    #endregion

    #region ????????
    public virtual void Awake()
    {
        _rb2D = GetComponent<Rigidbody2D>();
        if (_rb2D == null)
            Debug.LogError(string.Format(LOG_RB_NULL, gameObject.name), this);

        // ???????????§Ö???
        CacheSingletonInstances();

        // ???????????
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

    #region ???????????
    private void OnCurrentHealthChanged(float oldValue, float newValue)
    {
        newValue = Mathf.Clamp(newValue, 0, maxHealth);
        CurrentHealth = newValue;

        if (isLocalPlayer)
        {
            HealthUI.Instance?.SetValue(newValue / Mathf.Max(maxHealth, 1f));
            //?????????§¹
            if (oldValue >= newValue)
                _screenPulse?.Trigger_Wound();//??????????§¹  
        }

        if (oldValue >= newValue)
        {
            EntityWoundEvent?.Invoke();
            //??????????????
            MyWorldUI?.ShowInfo();//???UI
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

    #region ???????????
    [Command]
    public virtual void CmdChangeHealth(float value, Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        if (IsDead)
            return;

        if (value < 0 || !_respawnManager.IsGameRealStart)//???¦Ä???????????????
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
        Debug.Log($"[ServerApplyDamage] {gameObject.name} ?????{healthBefore} ?? {CurrentHealth}???????{damage}??");
        ResetCoolTime();//???¨²?????????
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
            // ????????????
            if (attacker is playerStats attackerStats)
            {
                // ????????????Main.PlayerName?????????????
                _killerName = UOSRelaySimple.Instance.playerName ?? attacker.gameObject.name;

                // ???????????????§Ö??
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

        Player Deather = gameObject.GetComponent<Player>();//??????????????Player???
        //????????????????????????
        _respawnManager.AddPlayerDeath(Deather.connectionToClient);
        //???????????????
        _respawnManager.AddPlayerKill(killer.gameObject.GetComponent<Player>().connectionToClient);
        //??????????????
        _respawnManager.AddScore(killer.gameObject.GetComponent<Player>().CurrentTeam);//??????
    }
    #endregion

    #region ??????§¹????
    [ClientRpc]
    public virtual void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        // ?????§¹???
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

        // ??????????+?????
        if (isLocalPlayer && attacker != null && _rb2D != null)
        {
            var Attacker = attacker as playerStats;
            float knockbackDir = Mathf.Sign(ColliderPoint.x - attacker.transform.position.x);
            _rb2D.AddForce(new Vector2(knockbackDir * Attacker.MyMonster.currentGun.gunInfo.Recoil_Enemy, 0), ForceMode2D.Impulse);
            MyCameraControl.Instance.AddTimeBasedShake(Attacker.MyMonster.currentGun.gunInfo.ShackStrength_Enemy, Attacker.MyMonster.currentGun.gunInfo.ShackTime_Enemy);
            Debug.Log("?????????");
        }
    }
    #endregion

    #region ????????????
    protected virtual void ClientHandleDeathVisual()
    {
        MyHelmet.TriggerHelmetDrop();//???????????
        if (isLocalPlayer)
        {
            Debug.Log("[ClientHandleDeathVisual] ?????????????????????/?????");
        }
    }
    #endregion

    #region ???????
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

    #region ??????????????
    [Server]
    public void ServerApplyGrenadeDamage(float damage, Vector2 explosionCenter, Vector2 knockbackForce, CharacterStats attacker)
    {
        if (IsDead)
            return;

        float healthBefore = CurrentHealth;
        if (_respawnManager.IsGameRealStart)
        {
            CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
            Debug.Log($"[ServerApplyGrenadeDamage] {gameObject.name} ????????§µ????: {healthBefore} -> {CurrentHealth}, ???: {damage}");
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
            _rb2D.velocity = ZERO_VECTOR; // ?????????????
            _rb2D.AddForce(knockbackForce, ForceMode2D.Impulse);

            // ????????????????????????????????
            float grenadeShakeStrength = 3;
            float grenadeShakeTime = 0.4f;
            MyCameraControl.Instance?.AddTimeBasedShake(grenadeShakeStrength, grenadeShakeTime);

            Debug.Log($"????????????????????: {knockbackForce}");
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
                // ????????????
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

            // ??????
            float duration = (maxHealth - CurrentHealth) / HealSpeed;
            //??????¦Ë??????§¹
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
        CurrentRemainTime = EnterBreatheHealTime;//???????
        IsEnterBreather = false;
        //??????
        _animatorTool?.StopFloatLerpById(BreatheHealTaskId);//?????????
    }
}