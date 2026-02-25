using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using DG.Tweening; // 必须导入DOTween命名空间

public class BaseGun : NetworkBehaviour
{
    #region 基础组件引用
    [Header("刚体组件")]
    public Rigidbody2D myRigidbody;
    private NetworkIdentity _netIdentity;
    private SpriteRenderer MySprite;
    #endregion

    #region 网络同步状态（SyncVar）
    [Header("=== 枪械核心状态（服务器权威，SyncVar同步） ===")]
    [SerializeField]
    [SyncVar(hook = nameof(OnIsInReloadChanged))]
    protected bool _isInReload = false;
    [SerializeField]
    [SyncVar(hook = nameof(OnIsInShootChanged))]
    protected bool _isInShoot = false;
    [SerializeField]
    [SyncVar(hook = nameof(OnCanShootChanged))]
    protected bool _canShoot = true;
    [SerializeField]
    [SyncVar(hook = nameof(OnCurrentMagazineBulletChanged))]
    protected float _currentMagazineBulletCount = 0;
    [SerializeField]
    [SyncVar(hook = nameof(OnAllReserveBulletChanged))]
    protected float _allReserveBulletCount = 0;
    [SyncVar(hook = nameof(OnIsEnterAimState))]
    public bool IsEnterAimState = false;

    [Header("销毁计时")]
    [SyncVar(hook = nameof(OnChangeRemainTime))]
    public float RemainingDestoryTime;
    public float DestoryTime = 20;

    [Header("当前拾取玩家")]
    [SyncVar]
    public Player ownerPlayer;
    [SyncVar(hook = nameof(OnIsInPlayerHandChanged))]
    public bool isInPlayerHand = false;
    #endregion

    #region 检测与输入配置
    [Header("射线检测配置")]
    [Tooltip("射线仅检测这些层（Player和Ground）")]
    public LayerMask shootRaycastLayers;
    #endregion

    #region 枪械组件与配置
    [Header("=== 枪械组件配置 ===")]
    [Header("Timeline动画")]
    public PlayableDirector timelineDirector_Reload;
    public PlayableDirector timelineDirector_Shoot;

    [Header("枪械配置文件")]
    public GunInfo gunInfo;
    #endregion

    #region 射击特效配置
    [Header("射击特效")]
    public Transform firePoint;
    public GameObject cartridgeCasePrefab;
    public Transform cartridgeEjectPoint;
    public float recoilForceScale = 1f;
    public Vector3 cartridgeFixedScale = new Vector3(0.2f, 0.2f, 1f);
    public GameObject hitwalleffect;

    [Header("是否应用自动抛壳")]
    public bool applyAutoEjectCartridge = true;
    #endregion

    #region 调试与子弹视觉配置
    [Header("调试配置")]
    public bool isDebug = true;

    [Header("子弹小线段配置")]
    public Color bulletColor = new Color(0.83f, 0.68f, 0.22f);
    public float bulletSegmentLength = 0.2f;
    public float bulletLineWidth = 0.03f;
    public float bulletFlySpeed = 80f;
    public float bulletShowDuration = 0.5f;
    [SerializeField]
    public GameObject bulletSegmentPrefab;
    #endregion

    #region 内部缓存字段
    private GameObject _autoBulletSegmentTemplate;
    private readonly string _bulletSegmentTemplateName = "Auto_BulletSegmentTemplate";
    [HideInInspector]
    public Vector3 originalWorldScale;
    private GunWorldInfoShow _gunWorldInfoShow;
    #endregion

    #region 事件与管理器引用
    public UnityAction ReloadSuccessAction;
    public GunWorldInfoShow GunInfoManager;
    #endregion

    #region 销毁计时与动画变量
    private Coroutine _destroyTimerCoroutine;
    private Tween _flashTween;
    #endregion

    #region 公有属性封装
    public bool IsInReload
    {
        get => _isInReload;
        set
        {
            if (!isServer) return;
            if (_isInReload != value)
            {
                _isInReload = value;
                if (value) _isInShoot = false;
            }
        }
    }

    public bool IsInShoot
    {
        get => _isInShoot;
        set
        {
            if (!isServer) return;
            if (_isInShoot != value)
            {
                _isInShoot = value;
                if (value) _canShoot = false;
            }
        }
    }

    public bool CanShoot
    {
        get => _canShoot;
        set
        {
            if (!isServer) return;
            if (_canShoot != value) _canShoot = value;
        }
    }

    public float CurrentMagazineBulletCount => _currentMagazineBulletCount;
    public float AllReserveBulletCount => _allReserveBulletCount;
    #endregion

    #region SyncVar钩子（状态同步回调）
    private void OnIsInReloadChanged(bool oldValue, bool newValue)
    {
        if (!isClient) return;
        if (timelineDirector_Reload != null)
        {
            if (newValue) timelineDirector_Reload.Play();
            else timelineDirector_Reload.Stop();
        }
        else Debug.LogError($"[客户端] [{gameObject.name}] 换弹Timeline未赋值！");
    }

    private void OnIsInShootChanged(bool oldValue, bool newValue)
    {
        if (!isClient) return;
        if (timelineDirector_Shoot != null)
        {
            if (newValue) timelineDirector_Shoot.Play();
            else timelineDirector_Shoot.Stop();
        }
        else Debug.LogError($"[客户端] [{gameObject.name}] 射击Timeline未赋值！");
    }

    private void OnCanShootChanged(bool oldValue, bool newValue) { }

    private void OnCurrentMagazineBulletChanged(float oldValue, float newValue)
    {
        if (this == Player.LocalPlayer.currentGun && UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().UpdateGunBulletAmountText();
        }
    }

    private void OnAllReserveBulletChanged(float oldValue, float newValue)
    {
        if (this == Player.LocalPlayer.currentGun && UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().UpdateGunBulletAmountText();
        }
    }

    private void OnIsInPlayerHandChanged(bool oldValue, bool newValue)
    {
        if (!isClient || myRigidbody == null) return;
        myRigidbody.simulated = !newValue;
        myRigidbody.isKinematic = newValue;
        if (newValue)
        {
            myRigidbody.velocity = Vector2.zero;
            myRigidbody.angularVelocity = 0;
            InitLocalAimProperties();

            // 客户端：停止闪烁动画
            if (isClient) StopFlashAnimation();
            // 服务器：停止销毁计时
            if (isServer)
            {
                StopDestroyTimer();
                RemainingDestoryTime = DestoryTime;
            }
        }
        else
        {
            StopAllAimLerp();
            ResetLocalAimProperties();

            // 服务器：开启销毁计时
            if (isServer) 
                StartDestroyTimer();
        }
    }

    private void OnIsEnterAimState(bool oldValue, bool newValue)
    {
        if (!isClient || gunInfo == null || ownerPlayer == null || ownerPlayer.myStats == null || SimpleAnimatorTool.Instance == null)
        {
            Debug.LogError("[瞄准状态] 执行条件不满足，跳过状态切换");
            return;
        }
        if (newValue) EnterAimState();
        else ExitAimState();
    }

    private void OnChangeRemainTime(float oldValue, float newValue)
    {
        if (!isClient || MySprite == null) return;

        if (newValue < DestoryTime * 0.5f)
        {
            // 动态计算闪烁速度：时间越少，速度越快
            float minCycle = 0.1f;
            float maxCycle = 1f;
            float timeProgress = newValue / (DestoryTime * 0.5f);
            float currentCycle = Mathf.Lerp(minCycle, maxCycle, timeProgress);
            StartFlashAnimation(currentCycle);
        }
        else
        {
            StopFlashAnimation();
        }
    }
    #endregion

    #region 核心Command方法（客户端→服务器）
    [Command]
    public void ChangeAimState(bool IsEnter)
    {
        IsEnterAimState = IsEnter;
    }

    [Command(requiresAuthority = true)]
    public void CmdStartShoot()
    {
        if (!isServer) { Debug.LogError($"[服务器] CmdStartShoot非服务器环境！"); return; }
        bool canShootServer = IsCanShoot();
        if (!canShootServer) return;
        IsInShoot = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdExecuteShootLogic()
    {
        if (!isServer) { Debug.LogError($"[服务器] CmdExecuteShootLogic非服务器环境！"); return; }
        _currentMagazineBulletCount = Mathf.Max(0, _currentMagazineBulletCount - 1);

        if (firePoint != null && gunInfo != null && ownerPlayer != null)
        {
            Vector2 firePointRightDir = firePoint.transform.right;
            Vector2 baseDir = -firePointRightDir * ownerPlayer.FacingDir;
            Vector2 shootDir = CalculateBulletScattering(baseDir);

            RaycastHit2D hit = Physics2D.Raycast(
                firePoint.position,
                shootDir,
                gunInfo.Range,
                shootRaycastLayers
            );

            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    CharacterStats hitTarget = hit.collider.GetComponent<playerStats>();
                    if (hitTarget != null && !hitTarget.IsDead)
                    {
                        CharacterStats attackerStats = ownerPlayer.myStats;
                        if (attackerStats == null)
                        {
                            Debug.LogError($"[BaseGun] 攻击者{ownerPlayer.name} 无myStats组件！");
                            return;
                        }
                        hitTarget.ServerApplyDamage(gunInfo.Damage, hit.point, hit.normal, attackerStats);
                    }
                }
                else if (hit.collider.CompareTag("Ground"))
                {
                    RpcSpawnHitEffect(hit.point, hit.normal);
                }
            }

            Vector2 bulletTargetPos = hit ? hit.point : (Vector2)firePoint.position + shootDir * gunInfo.Range;
            if (isDebug) RpcDrawBulletSegment(firePoint.position, bulletTargetPos, shootDir);
        }
        else
        {
            Debug.LogError($"[BaseGun] 射击失败：firePoint={firePoint != null} | gunInfo={gunInfo != null} | ownerPlayer={ownerPlayer != null}");
        }
        RpcPlaySingleShootVFX();
    }

    [Command(requiresAuthority = true)]
    public void CmdFinishShoot()
    {
        if (!isServer) { Debug.LogError($"[服务器] CmdFinishShoot非服务器环境！"); return; }
        IsInShoot = false;
        CanShoot = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdStartReload()
    {
        if (!isServer) { Debug.LogError($"[服务器] CmdStartReload非服务器环境！"); return; }
        bool canReloadServer = IsCanReload();
        if (!canReloadServer) return;
        IsInReload = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdFinishReloadLogic()
    {
        ServerFinishReload(); // 复用服务器端换弹完成逻辑
    }
    #endregion

    #region ClientRpc（服务器→所有客户端）
    [ClientRpc]
    private void RpcPlaySingleShootVFX() => PlaySingleShootVFX();

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector2 hitPos, Vector2 hitNormal)
    {
        if (hitwalleffect == null) { Debug.LogError("[打击特效] hitwalleffect 预制体未赋值！"); return; }
        GameObject hitEffectObj = PoolManage.Instance.GetObj(hitwalleffect);
        if (hitEffectObj == null) return;
        hitEffectObj.transform.position = hitPos;
        hitEffectObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, hitNormal);
        CountDownManager.Instance.CreateTimer(false, 1000, () => { PoolManage.Instance.PushObj(hitwalleffect, hitEffectObj); });
    }

    [ClientRpc]
    private void RpcDrawBulletSegment(Vector2 startPos, Vector2 targetPos, Vector2 shootDir)
    {
        if (!isDebug) return;
        GameObject template = GetBulletSegmentTemplate();
        if (template == null) { Debug.LogError("[子弹线段] 模板创建失败，跳过绘制"); return; }

        GameObject bulletObj = PoolManage.Instance?.GetObj(template);
        if (bulletObj == null)
        {
            bulletObj = Instantiate(template);
            bulletObj.name = _bulletSegmentTemplateName;
        }

        bulletObj.transform.SetParent(null);
        bulletObj.SetActive(true);
        template.SetActive(false);

        LineRenderer lr = bulletObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = bulletObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }
        lr.startColor = bulletColor;
        lr.endColor = bulletColor;
        lr.startWidth = bulletLineWidth;
        lr.endWidth = bulletLineWidth;
        lr.positionCount = 2;
        lr.sortingOrder = 100;
        lr.enabled = true;
        Color resetColor = lr.startColor;
        resetColor.a = 1f;
        lr.startColor = resetColor;
        lr.endColor = resetColor;

        BulletSegmentFly fly = bulletObj.GetComponent<BulletSegmentFly>();
        if (fly == null) fly = bulletObj.AddComponent<BulletSegmentFly>();
        fly.Init(lr, startPos, targetPos, shootDir, bulletSegmentLength, bulletFlySpeed, bulletShowDuration, template);

        float totalDuration = Vector2.Distance(startPos, targetPos) / bulletFlySpeed + bulletShowDuration;
        CountDownManager.Instance.CreateTimer(false, (int)(totalDuration * 1000), () =>
        {
            if (bulletObj != null) PoolManage.Instance.PushObj(template, bulletObj);
        });
    }
    #endregion

    #region 客户端视觉特效逻辑
    public void PlaySingleShootVFX()
    {
        if (applyAutoEjectCartridge) SpawnCartridgeCase();
        ApplyRecoil();
        MuzzleSmokeManager.Instance?.PlayMuzzleSmoke(firePoint, gunInfo);
    }

    public void handMovement_SpawnCartridgeCase_TimeLine()
    {
        SpawnCartridgeCase();
    }

    private void SpawnCartridgeCase()
    {
        if (cartridgeCasePrefab == null) return;
        if (cartridgeEjectPoint == null) { Debug.LogError($"[客户端] 抛壳点未赋值！"); return; }

        GameObject cartridgeObj = PoolManage.Instance?.GetObj(cartridgeCasePrefab);
        if (cartridgeObj == null) { Debug.LogError($"[客户端] 对象池获取弹壳失败！"); return; }

        Rigidbody2D rb2D = cartridgeObj.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        cartridgeObj.transform.position = cartridgeEjectPoint.position;
        cartridgeObj.transform.rotation = cartridgeEjectPoint.rotation;
        cartridgeObj.transform.localScale = cartridgeFixedScale;

        if (rb2D != null)
        {
            Vector2 localRightDir = cartridgeEjectPoint.transform.TransformDirection(Vector2.right);
            Vector2 ejectForce = localRightDir * Random.Range(1f, 3f) + Vector2.up * Random.Range(0.5f, 2f);
            rb2D.AddForce(ejectForce, ForceMode2D.Impulse);
            rb2D.AddTorque(Random.Range(-5f, 5f));
        }

        CountDownManager.Instance.CreateTimer(false, 3000, () =>
        {
            cartridgeObj.transform.localScale = cartridgeFixedScale;
            PoolManage.Instance.PushObj(cartridgeCasePrefab, cartridgeObj);
        });
    }

    private void ApplyRecoil()
    {
        if (ownerPlayer == null || ownerPlayer.MyRigdboby == null || gunInfo == null)
        {
            Debug.LogError($"[客户端] 后坐力参数未赋值！");
            return;
        }
        Vector2 recoilForce = new Vector2(-ownerPlayer.FacingDir * _localRecoil * recoilForceScale, 0);
        ownerPlayer.MyRigdboby.AddForce(recoilForce, ForceMode2D.Impulse);
        if (ownerPlayer.isLocalPlayer)
            MyCameraControl.Instance?.AddTimeBasedShake(gunInfo.ShackStrength, gunInfo.ShackTime);
        MusicManager.Instance.PlayEffect3D(gunInfo.ShootAudio, 2, 1, 10, this.transform);
    }
    #endregion

    #region 服务器辅助逻辑
    private Vector2 CalculateBulletScattering(Vector2 centerDir)
    {
        if (gunInfo == null) { Debug.LogError($"[服务器] gunInfo未赋值！"); return centerDir; }
        int baseAngle = 20;
        float maxAngle = baseAngle * (1 - _localAccuracy / 100f);
        float randomAngle = Random.Range(-maxAngle, maxAngle);
        return Quaternion.Euler(0, 0, randomAngle) * centerDir;
    }

    /// <summary>
    /// 服务器端：完成换弹逻辑（直接修改状态）
    /// </summary>
    [Server]
    private void ServerFinishReload()
    {
        if (!isServer) { Debug.LogError($"[服务器] ServerFinishReload非服务器环境！"); return; }
        if (gunInfo == null) { Debug.LogError($"[服务器] gunInfo未赋值！"); return; }

        float needBulletCount = gunInfo.Bullet_capacity - _currentMagazineBulletCount;
        if (needBulletCount <= 0) { IsInReload = false; CanShoot = true; return; }

        if (needBulletCount <= _allReserveBulletCount)
        {
            _currentMagazineBulletCount = gunInfo.Bullet_capacity;
            _allReserveBulletCount -= needBulletCount;
        }
        else
        {
            _currentMagazineBulletCount += _allReserveBulletCount;
            _allReserveBulletCount = 0;
        }

        IsInReload = false;
        CanShoot = true;
    }
    #endregion

    #region 对外封装方法
    public virtual void TriggerSingleShoot()
    {
        if (!IsCanShoot()) return;
        CmdStartShoot();
    }

    public void TriggerReload()
    {
        CmdStartReload();
        ReloadSuccessAction?.Invoke();
    }
    #endregion

    #region Timeline动画回调
    public void OnShootFire_Timeline() => CmdExecuteShootLogic();
    public void OnShootEnd_Timeline() => CmdFinishShoot();
    public void OnReloadEnd_Timeline() => CmdFinishReloadLogic();
    public void OnShootVFX_Timeline() => PlaySingleShootVFX();
    #endregion

    #region 状态检测方法
    public bool IsCanReload()
    {
        if (gunInfo == null) { Debug.LogError($"IsCanReload：gunInfo未赋值！"); return false; }
        return !IsInReload && !IsInShoot && AllReserveBulletCount > 0 && CurrentMagazineBulletCount < gunInfo.Bullet_capacity;
    }

    public bool IsCanShoot()
    {
        if (gunInfo == null) { Debug.LogError($"IsCanShoot：gunInfo未赋值！"); return false; }
        return !IsInReload && !IsInShoot && CanShoot && CurrentMagazineBulletCount > 0;
    }
    #endregion

    #region 初始化与生命周期
    private void Awake()
    {
        MySprite = GetComponent<SpriteRenderer>();
        _netIdentity = GetComponent<NetworkIdentity>() ?? gameObject.AddComponent<NetworkIdentity>();
        myRigidbody = GetComponent<Rigidbody2D>();
        _gunWorldInfoShow = GetComponentInChildren<GunWorldInfoShow>() ?? GetComponent<GunWorldInfoShow>();
        GunInfoManager = _gunWorldInfoShow;
        InitBulletSegmentTemplate();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gunInfo != null)
        {
            _currentMagazineBulletCount = 0;
            _allReserveBulletCount = gunInfo.AllBulletAmount;
            Debug.Log($"[服务器] 初始化子弹 → 弹匣:{_currentMagazineBulletCount} | 备用:{_allReserveBulletCount}");
        }
        else Debug.LogError($"[服务器] 初始化子弹失败：gunInfo未赋值！");

        RemainingDestoryTime = DestoryTime;
    }


    #endregion

    #region 枪械拾取/丢弃逻辑
    [Server]
    public void SafeServerOnGunPicked()
    {
        if (GunInfoManager == null) { Debug.LogError($"GunInfoManager为null！"); return; }
        GunInfoManager.ServerOnGunPicked();
    }

    [Server]
    public void SafeServerOnGunDropped()
    {
        if (GunInfoManager == null) { Debug.LogError($"GunInfoManager为null！"); return; }
        GunInfoManager.ServerOnGunDropped();
    }
    #endregion

    #region 子弹线段模板管理
    private void InitBulletSegmentTemplate()
    {
        if (bulletSegmentPrefab != null)
        {
            _autoBulletSegmentTemplate = bulletSegmentPrefab;
            return;
        }

        _autoBulletSegmentTemplate = new GameObject(_bulletSegmentTemplateName);
        _autoBulletSegmentTemplate.SetActive(false);
        _autoBulletSegmentTemplate.transform.SetParent(this.transform);

        LineRenderer templateLr = _autoBulletSegmentTemplate.AddComponent<LineRenderer>();
        templateLr.material = new Material(Shader.Find("Sprites/Default"));
        templateLr.startColor = bulletColor;
        templateLr.endColor = bulletColor;
        templateLr.startWidth = bulletLineWidth;
        templateLr.endWidth = bulletLineWidth;
        templateLr.positionCount = 2;
        templateLr.sortingOrder = 100;
        templateLr.enabled = false;

        _autoBulletSegmentTemplate.AddComponent<BulletSegmentFly>();
        Debug.Log($"[子弹线段] 自动创建模板：{_bulletSegmentTemplateName}");
    }

    private GameObject GetBulletSegmentTemplate()
    {
        if (_autoBulletSegmentTemplate != null) return _autoBulletSegmentTemplate;
        InitBulletSegmentTemplate();
        return _autoBulletSegmentTemplate;
    }
    #endregion

    #region 瞄准状态逻辑
    [Header("瞄准状态变量设置")]
    public float Duration = 0.5f;
    private int AnimationID_Recoil = -1;
    private int AnimationID_ViewRange = -1;
    private int AnimationID_Accuracy = -1;

    [Header("本地的瞄准属性")]
    public float _localRecoil;
    public float _localViewRange;
    public float _localAccuracy;

    private void InitLocalAimProperties()
    {
        if (gunInfo == null) return;
        _localRecoil = gunInfo.Recoil;
        _localViewRange = gunInfo.ViewRange;
        _localAccuracy = gunInfo.Accuracy;
    }

    private void ResetLocalAimProperties()
    {
        if (gunInfo == null) return;
        _localRecoil = gunInfo.Recoil;
        _localViewRange = gunInfo.ViewRange;
        _localAccuracy = gunInfo.Accuracy;
    }

    public void StopAllAimLerp()
    {
        if (SimpleAnimatorTool.Instance == null) return;
        if (AnimationID_Recoil != -1)
        {
            SimpleAnimatorTool.Instance.StopFloatLerpById(AnimationID_Recoil);
            AnimationID_Recoil = -1;
        }
        if (AnimationID_ViewRange != -1)
        {
            SimpleAnimatorTool.Instance.StopFloatLerpById(AnimationID_ViewRange);
            AnimationID_ViewRange = -1;
        }
        if (AnimationID_Accuracy != -1)
        {
            SimpleAnimatorTool.Instance.StopFloatLerpById(AnimationID_Accuracy);
            AnimationID_Accuracy = -1;
        }
    }

    public void EnterAimState()
    {
        StopAllAimLerp();
        AnimationID_Recoil = SimpleAnimatorTool.Instance.StartFloatLerp(
            _localRecoil,
            _localRecoil * (1 - ownerPlayer.myStats.AimRecoilBonus),
            Duration,
            (value) => { _localRecoil = value; }
        );
        AnimationID_ViewRange = SimpleAnimatorTool.Instance.StartFloatLerp(
            _localViewRange,
            _localViewRange * (1 + ownerPlayer.myStats.AimViewBonus),
            Duration,
            (value) => { _localViewRange = value; }
        );
        AnimationID_Accuracy = SimpleAnimatorTool.Instance.StartFloatLerp(
            _localAccuracy,
            _localAccuracy * (1 + ownerPlayer.myStats.AimAccuracyBonus),
            Duration,
            (value) => { _localAccuracy = value; }
        );
    }

    public void ExitAimState()
    {
        StopAllAimLerp();
        AnimationID_Recoil = SimpleAnimatorTool.Instance.StartFloatLerp(
            _localRecoil,
            gunInfo.Recoil,
            Duration,
            (value) => { _localRecoil = value; }
        );
        AnimationID_ViewRange = SimpleAnimatorTool.Instance.StartFloatLerp(
            _localViewRange,
            gunInfo.ViewRange,
            Duration,
            (value) => { _localViewRange = value; }
        );
        AnimationID_Accuracy = SimpleAnimatorTool.Instance.StartFloatLerp(
            _localAccuracy,
            gunInfo.Accuracy,
            Duration,
            (value) => { _localAccuracy = value; }
        );
    }
    #endregion

    #region 强制丢弃枪械
    [Command(requiresAuthority = false)]
    public void CmdForceDiscardGun()
    {
        if (!isServer) { Debug.LogError($"[强制丢枪] 仅服务器可执行该逻辑！"); return; }
        if (ownerPlayer == null || !isInPlayerHand || ownerPlayer.currentGun != this) return;

        IsInReload = false;
        IsInShoot = false;
        CanShoot = true;
        IsEnterAimState = false;
        StopAllTimeLine();
        StartCoroutine(ServerWaitAndDropGun());
    }

    private System.Collections.IEnumerator ServerWaitAndDropGun()
    {
        yield return null;
        if (ownerPlayer != null)
        {
            ownerPlayer.ServerHandleDropGun(gameObject);
            ownerPlayer.currentGun = null;
        }
    }

    [ClientRpc]
    public void StopAllTimeLine()
    {
        if (timelineDirector_Shoot != null)
        {
            timelineDirector_Shoot.Stop();
            if (timelineDirector_Shoot.playableGraph.IsValid())
                timelineDirector_Shoot.playableGraph.Destroy();
        }
        if (timelineDirector_Reload != null)
        {
            timelineDirector_Reload.Stop();
            if (timelineDirector_Reload.playableGraph.IsValid())
                timelineDirector_Reload.playableGraph.Destroy();
        }
        StopAllAimLerp();
        if (IsEnterAimState)
        {
            ExitAimState();
            IsEnterAimState = false;
        }
        transform.localPosition = transform.localPosition;
        transform.localRotation = transform.localRotation;
        transform.localScale = transform.localScale;
    }
    #endregion

    #region 服务器端销毁计时逻辑
    [Server]
    private void StartDestroyTimer()
    {
        if (_destroyTimerCoroutine != null) StopCoroutine(_destroyTimerCoroutine);
        _destroyTimerCoroutine = StartCoroutine(DestroyTimerCoroutine());
    }

    [Server]
    private void StopDestroyTimer()
    {
        if (_destroyTimerCoroutine != null)
        {
            StopCoroutine(_destroyTimerCoroutine);
            _destroyTimerCoroutine = null;
        }
    }

    private System.Collections.IEnumerator DestroyTimerCoroutine()
    {
        while (RemainingDestoryTime > 0)
        {
            yield return new WaitForSeconds(1f);
            RemainingDestoryTime = Mathf.Max(0, RemainingDestoryTime - 1f);
        }
        NetworkServer.Destroy(gameObject);
    }
    #endregion

    #region 客户端DOTween闪烁动画逻辑
    [Client]
    private void StartFlashAnimation(float cycleDuration)
    {
        StopFlashAnimation();
        _flashTween = MySprite.DOFade(0, cycleDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);
    }

    [Client]
    private void StopFlashAnimation()
    {
        if (_flashTween != null && _flashTween.IsActive())
        {
            _flashTween.Kill();
            _flashTween = null;
        }
        if (MySprite != null)
        {
            Color resetColor = MySprite.color;
            resetColor.a = 1f;
            MySprite.color = resetColor;
        }
    }

    private void OnDestroy()
    {
        StopFlashAnimation();
    }
    #endregion
}

#region 辅助类：子弹飞行逻辑
public class BulletSegmentFly : MonoBehaviour
{
    private LineRenderer _lr;
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private Vector2 _shootDir;
    private float _segmentLength;
    private float _flySpeed;
    private float _fadeDuration;
    private Vector2 _currentCenterPos;
    private float _elapsedTime;
    private bool _isReachTarget;
    private GameObject _prefab;

    public void Init(LineRenderer lr, Vector2 startPos, Vector2 targetPos, Vector2 shootDir,
        float segmentLength, float flySpeed, float fadeDuration, GameObject prefab)
    {
        _elapsedTime = 0f;
        _isReachTarget = false;
        _prefab = prefab;
        _lr = lr;
        _startPos = startPos;
        _targetPos = targetPos;
        _shootDir = shootDir.normalized;
        _segmentLength = segmentLength;
        _flySpeed = flySpeed;
        _fadeDuration = fadeDuration;
        _currentCenterPos = startPos;
        UpdateBulletSegmentPos();
    }

    private void Update()
    {
        if (_isReachTarget)
        {
            FadeOutBullet();
            return;
        }
        _elapsedTime += Time.deltaTime;
        float moveDistance = _flySpeed * Time.deltaTime;
        _currentCenterPos = Vector2.MoveTowards(_currentCenterPos, _targetPos, moveDistance);
        UpdateBulletSegmentPos();
        if (Vector2.Distance(_currentCenterPos, _targetPos) < 0.01f)
        {
            _isReachTarget = true;
            _elapsedTime = 0f;
        }
    }

    private void OnDisable()
    {
        _elapsedTime = 0f;
        _isReachTarget = false;
        _currentCenterPos = Vector2.zero;
        if (_lr != null)
        {
            Color resetColor = _lr.startColor;
            resetColor.a = 1f;
            _lr.startColor = resetColor;
            _lr.endColor = resetColor;
        }
    }

    private void UpdateBulletSegmentPos()
    {
        Vector2 pos1 = _currentCenterPos - _shootDir * (_segmentLength / 2);
        Vector2 pos2 = _currentCenterPos + _shootDir * (_segmentLength / 2);
        _lr.SetPosition(0, pos1);
        _lr.SetPosition(1, pos2);
    }

    private void FadeOutBullet()
    {
        _elapsedTime += Time.deltaTime;
        float fadeProgress = Mathf.Clamp01(_elapsedTime / _fadeDuration);
        Color currentColor = _lr.startColor;
        currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
        _lr.startColor = currentColor;
        _lr.endColor = currentColor;
    }
}
#endregion