using DG.Tweening;
using Mirror;
using System.Collections;
using UnityEngine;

public class playerStats : CharacterStats
{
    [Header("自身引用")]
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
    [Header("枪械机动性对移速的影响")]
    [Range(0, 1)]
    public float GunMobilityMoveSpeedScale = 0.3f;//枪械机动性对移速的影响系数
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

    [Header("红色针剂的效果")]
    public Color RedAnimaColor = ColorManager.BrickRed;
    public float RedInjectionDuration = 15f;
    public float RedInjectionHealthBonus = 30f;

    [Header("紫色针剂的效果")]
    public Color PurpleAnimaColor = ColorManager.Violet;
    public float PurpleInjectionDuration = 15f;
    [Range(0f, 1f)]
    public float PurpleDamageReductionPercent = 0.1f;

    [Header("黄色针剂的效果")]
    public float SpeedBuff_MovePowerBonus = 1f;//黄色针剂的移动力提升(移动力加1)
    public float MaxSpeedBuff_Bonus = 1f;//黄色针剂的最大速度提升(+1)
    public float ViewBuff_Bonus = 0.2f;//黄色针剂的视野提升
    public float DurationBuff_Bonus = 20f;//黄色针剂的持续时间(持续20秒)
    public float JumpBuff_MovePowerBonus = 1f;//黄色针剂的跳跃力提升(跳跃力加1)

    // 内部私有变量
    private bool _isYellowBuffActive = false;
    private bool _isRedInjectionActive = false;
    private bool _isPurpleInjectionActive = false;
    private float _redInjectionAppliedHealthBonus = 0f;
    private float _activePurpleDamageReduction = 0f;
    private Coroutine _redInjectionCoroutine;
    private Coroutine _purpleInjectionCoroutine;

    //  记录初始的白板属性，用于护甲计算
    private float _originalMaxHealth;
    private float _originalMaxXSpeed;

    // 注射器动画任务ID
    private int HealAnimaTaskId = -1;
    public bool IsInInjectionGreenEffect = false;

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

        // 初始化时记录原始属性
        _originalMaxHealth = maxHealth;
        _originalMaxXSpeed = MaxXSpeed;

        // 初始化血量
        if (isLocalPlayer || isServer)
        {
            CurrentHealth = maxHealth;
        }
    }

    // 获取护甲效果，现在接收具体的属性包
    public void AddArmorEffect(ArmorInfoPack infoPack)
    {
        if (infoPack == null)
            return;

        // 基于原始值计算新属性
        maxHealth = _originalMaxHealth + infoPack.HealthAdd;
        MaxXSpeed = _originalMaxXSpeed + infoPack.SpeedAdd;

        //  回血逻辑
        if (isLocalPlayer || isServer)
        {
            // 确保当前血量不超过新的最大血量，且至少回一点
            float targetHealth = Mathf.Min(CurrentHealth + infoPack.HealthAdd, maxHealth);

            // 只有血量有变化时才播动画
            if (CurrentHealth < targetHealth)
            {
                HealAnimaTaskId = SimpleAnimatorTool.Instance.StartFloatLerp(CurrentHealth, targetHealth, 1f, (Value) => {
                    if (isLocalPlayer)
                    {
                        // 本地玩家通过 Command 同步到服务器
                        CmdChangeHealth(Value, Vector2.zero, Vector2.zero, null);
                    }
                    else if (isServer)
                    {
                        // 服务器直接修改当前血量
                        CurrentHealth = Value;
                    }
                });
            }
        }

        /* Debug.Log($"[护甲系统] 成功应用护甲 -> 血量上限: {maxHealth}, 移速加成: {infoPack.SpeedAdd}"); */
    }

    // 移除护甲效果，接收旧的属性包
    public void RemoveArmorEffect(ArmorInfoPack oldInfoPack)
    {
        if (oldInfoPack == null)
            return;

        // 直接恢复到原始属性
        maxHealth = _originalMaxHealth;
        MaxXSpeed = _originalMaxXSpeed;

        // 处理血量溢出
        if (isLocalPlayer || isServer)
        {
            if (CurrentHealth > maxHealth)
            {
                CurrentHealth = maxHealth;
                if (isLocalPlayer)
                {
                    CmdChangeHealth(CurrentHealth, Vector2.zero, Vector2.zero, null);
                }
            }
        }

        /* Debug.Log($"[护甲系统] 成功移除护甲 -> 血量上限恢复至: {maxHealth}"); */
    }

    protected override void ClientHandleDeathVisual()
    {
        base.ClientHandleDeathVisual();
        MyMonster?.ResetLocalCameraViewOnDeath();
        ClearLocalInjectionEffects();
    }

    /// <summary>
    /// 受伤特效
    /// </summary>
    public override void RpcPlayWoundEffect(Vector2 ColliderPoint, Vector2 hitNormal, CharacterStats attacker)
    {
        base.RpcPlayWoundEffect(ColliderPoint, hitNormal, attacker);

        if (attacker == null)
            return;

        var attackerStats = attacker as playerStats;
        var attackerGunInfo = attackerStats?.MyMonster?.currentGun?.gunInfo;
        if (attackerGunInfo == null)
            return;

        float knockbackDirection = Mathf.Sign(transform.position.x - attacker.transform.position.x);
        if (MyMonster?.MyRigdboby != null)
        {
            MyMonster.MyRigdboby.AddForce(new Vector2(knockbackDirection * attackerGunInfo.Recoil_Enemy, 0), ForceMode2D.Impulse);
        }

        if (isLocalPlayer && MyCameraControl.Instance != null)
        {
            MyCameraControl.Instance.AddTimeBasedShake(attackerGunInfo.ShackStrength_Enemy, attackerGunInfo.ShackTime_Enemy);
        }
    }

    //————————————注射器效果————————————
    [ClientRpc]
    public void TriggerEffect_Injection(TacticType injectionType)
    {
        switch (injectionType)
        {
            case TacticType.Green_injection:
                /* Debug.Log($"[本地玩家] 触发绿色针剂回血"); */
                if (isLocalPlayer)
                {
                    ScreenPulseController.Instance.Trigger_Heal();//触发特效
                    // 触发回血
                    HealAnimaTaskId = SimpleAnimatorTool.Instance.StartFloatLerp(CurrentHealth, maxHealth, InjectionHealTime * 2 + StayTime, (Value) => {
                        CmdChangeHealth(Value, Vector2.zero, Vector2.zero, null);
                    });
                    IsInInjectionGreenEffect = true;
                    // 触发BuffUI
                    UImanager.Instance.GetPanel<PlayerPanel>()?.CreateBuff(MilitaryManager.Instance.GetTacticUISprite(injectionType), InjectionHealTime * 2 + StayTime);
                }
                InjectionColorAnima(HealAnimaColor);
                break;

            case TacticType.Yellow_injection:
                /* Debug.Log($"[本地玩家] 触发黄色针剂速度buff"); */
                if (isLocalPlayer && !_isYellowBuffActive)
                {
                    ScreenPulseController.Instance.Trigger_Yellow();//触发特效
                    movePower += SpeedBuff_MovePowerBonus;
                    MaxXSpeed += MaxSpeedBuff_Bonus;
                    AimMovePower += SpeedBuff_MovePowerBonus;
                    AimMoveMaxSpeed += MaxSpeedBuff_Bonus;
                    JumpPower += JumpBuff_MovePowerBonus;
                    AimJumpPower += JumpBuff_MovePowerBonus;
                    AimViewBonus += ViewBuff_Bonus;

                    _isYellowBuffActive = true;

                    CountDownManager.Instance.CreateTimer(false, (int)(DurationBuff_Bonus * 1000), () =>
                    {
                        movePower -= SpeedBuff_MovePowerBonus;
                        MaxXSpeed -= MaxSpeedBuff_Bonus;
                        AimMovePower -= SpeedBuff_MovePowerBonus;
                        AimMoveMaxSpeed -= MaxSpeedBuff_Bonus;
                        JumpPower -= JumpBuff_MovePowerBonus;
                        AimJumpPower -= JumpBuff_MovePowerBonus;
                        AimViewBonus -= ViewBuff_Bonus;

                        _isYellowBuffActive = false;
                        /* Debug.Log($"[本地玩家] 黄色针剂buff结束，已恢复原始数值"); */
                    });

                    UImanager.Instance.GetPanel<PlayerPanel>()?.CreateBuff(MilitaryManager.Instance.GetTacticUISprite(injectionType), DurationBuff_Bonus);
                }
                InjectionColorAnima(SpeedAnimaColor);
                break;

            case TacticType.purple_injection:
                TriggerPurpleInjectionEffect();
                break;

            case TacticType.Red_injection:
                TriggerRedInjectionEffect();
                break;
        }
    }

    private void TriggerPurpleInjectionEffect()
    {
        if (isLocalPlayer)
        {
            ScreenPulseController.Instance?.Trigger_Purple();
            UImanager.Instance.GetPanel<PlayerPanel>()?.CreateBuff(MilitaryManager.Instance.GetTacticUISprite(TacticType.purple_injection), PurpleInjectionDuration);
        }

        InjectionColorAnima(PurpleAnimaColor);
    }

    private void TriggerRedInjectionEffect()
    {
        if (isLocalPlayer)
        {
            ScreenPulseController.Instance?.Trigger_Red();
            UImanager.Instance.GetPanel<PlayerPanel>()?.CreateBuff(MilitaryManager.Instance.GetTacticUISprite(TacticType.Red_injection), RedInjectionDuration);
        }

        if (!isServer)
            ClientStartRedInjectionPreview();

        InjectionColorAnima(RedAnimaColor);
    }

    [Server]
    public void ServerApplyInjectionEffect(TacticType injectionType)
    {
        switch (injectionType)
        {
            case TacticType.Red_injection:
                ServerStartRedInjection();
                break;
            case TacticType.purple_injection:
                ServerStartPurpleInjection();
                break;
        }
    }

    [Server]
    private void ServerStartRedInjection()
    {
        if (_redInjectionCoroutine != null)
            StopCoroutine(_redInjectionCoroutine);

        ApplyRedInjectionBonus(true);
        _redInjectionCoroutine = StartCoroutine(ServerRedInjectionTimer());
    }

    private IEnumerator ServerRedInjectionTimer()
    {
        yield return new WaitForSeconds(RedInjectionDuration);
        ServerEndRedInjection();
    }

    [Server]
    private void ServerEndRedInjection()
    {
        RemoveRedInjectionBonus();
        _redInjectionCoroutine = null;
    }

    [Server]
    private void ServerStartPurpleInjection()
    {
        if (_purpleInjectionCoroutine != null)
            StopCoroutine(_purpleInjectionCoroutine);

        _isPurpleInjectionActive = true;
        _activePurpleDamageReduction = Mathf.Clamp01(PurpleDamageReductionPercent);
        _purpleInjectionCoroutine = StartCoroutine(ServerPurpleInjectionTimer());
    }

    private IEnumerator ServerPurpleInjectionTimer()
    {
        yield return new WaitForSeconds(PurpleInjectionDuration);
        _isPurpleInjectionActive = false;
        _activePurpleDamageReduction = 0f;
        _purpleInjectionCoroutine = null;
    }

    private void ClientStartRedInjectionPreview()
    {
        if (_redInjectionCoroutine != null)
            StopCoroutine(_redInjectionCoroutine);

        ApplyRedInjectionBonus(true);
        _redInjectionCoroutine = StartCoroutine(ClientRedInjectionTimer());
    }

    private IEnumerator ClientRedInjectionTimer()
    {
        yield return new WaitForSeconds(RedInjectionDuration);
        RemoveRedInjectionBonus();
        _redInjectionCoroutine = null;
    }

    private void ApplyRedInjectionBonus(bool increaseCurrentHealth)
    {
        if (!_isRedInjectionActive)
        {
            _isRedInjectionActive = true;
            _redInjectionAppliedHealthBonus = Mathf.Max(0f, RedInjectionHealthBonus);
            maxHealth += _redInjectionAppliedHealthBonus;

            if (increaseCurrentHealth)
                CurrentHealth = Mathf.Min(CurrentHealth + _redInjectionAppliedHealthBonus, maxHealth);
            else
                CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        }

        RefreshHealthViews();
    }

    private void RemoveRedInjectionBonus()
    {
        if (!_isRedInjectionActive)
            return;

        maxHealth = Mathf.Max(_originalMaxHealth, maxHealth - _redInjectionAppliedHealthBonus);
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        _redInjectionAppliedHealthBonus = 0f;
        _isRedInjectionActive = false;
        RefreshHealthViews();
    }

    [Server]
    private void ClearServerInjectionEffects()
    {
        if (_redInjectionCoroutine != null)
        {
            StopCoroutine(_redInjectionCoroutine);
            _redInjectionCoroutine = null;
        }

        RemoveRedInjectionBonus();

        if (_purpleInjectionCoroutine != null)
        {
            StopCoroutine(_purpleInjectionCoroutine);
            _purpleInjectionCoroutine = null;
        }

        _isPurpleInjectionActive = false;
        _activePurpleDamageReduction = 0f;
    }

    private void RefreshHealthViews()
    {
        if (isLocalPlayer)
            HealthUI.Instance?.SetValue(CurrentHealth / Mathf.Max(maxHealth, 1f));

        MyWorldUI?.UpdateInfo();
    }

    private void ClearLocalInjectionEffects()
    {
        if (_redInjectionCoroutine != null)
        {
            StopCoroutine(_redInjectionCoroutine);
            _redInjectionCoroutine = null;
        }

        RemoveRedInjectionBonus();
        _isPurpleInjectionActive = false;
        _activePurpleDamageReduction = 0f;
    }

    protected override float ModifyIncomingDamage(float damage)
    {
        damage = base.ModifyIncomingDamage(damage);
        if (IsReBornShieldActive())
            return 0f;

        if (_isPurpleInjectionActive)
            damage *= 1f - Mathf.Clamp01(_activePurpleDamageReduction);

        return damage;
    }

    private bool IsReBornShieldActive()
    {
        if (MyMonster == null)
            MyMonster = GetComponent<Player>() ?? GetComponentInParent<Player>();

        ReBornShield shield = MyMonster != null ? MyMonster.reBornShield : null;
        if (shield == null && MyMonster != null)
            shield = MyMonster.GetComponentInChildren<ReBornShield>();

        return shield != null && shield.isShieldActive;
    }

    public void InjectionColorAnima(Color AnimaColor)
    {
        var spriteRenderer = MyMonster?.MyBody?.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            /* Debug.LogError("玩家SpriteRenderer为空，无法执行颜色动画"); */
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

    public override void Death(CharacterStats killer)
    {
        if (!PlayerRespawnManager.Instance.IsGameStart)
            return;//游戏如果没开始就无法死亡
        //在这里丢出头盔

        if (isServer)
            ClearServerInjectionEffects();

        base.Death(killer);
        if (MyMonster.currentGun != null)
        {
            MyMonster.currentGun.CmdForceDiscardGun();
        }
    }
}
