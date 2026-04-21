using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using DG.Tweening;

public class BaseGun : NetworkBehaviour
{
    #region 兼容C#9.0 字符串常量
    private const string LOG_PREFIX = "[BaseGun]";
    private const string LOG_TIMELINE_RELOAD_NULL = "[BaseGun] [客户端] 换弹Timeline未赋值！";
    private const string LOG_TIMELINE_SHOOT_NULL = "[BaseGun] [客户端] 射击Timeline未赋值！";
    private const string LOG_SERVER_ERROR = "[BaseGun] [服务器] 非服务器环境！";
    private const string LOG_HIT_EFFECT_NULL = "[BaseGun] [打击特效] hitwalleffect 预制体未赋值！";
    private const string LOG_CARTRIDGE_POINT_NULL = "[BaseGun] [客户端] 抛壳点未赋值！";
    private const string LOG_CARTRIDGE_POOL_NULL = "[BaseGun] [客户端] 对象池获取弹壳失败！";
    private const string LOG_RECOIL_NULL = "[BaseGun] [客户端] 后坐力参数未赋值！";
    private const string LOG_GUNINFO_NULL = "[BaseGun] gunInfo未赋值！";
    private const string LOG_GUNINFO_MANAGER_NULL = "[BaseGun] GunInfoManager为null！";
    private const string LOG_BULLET_TEMPLATE = "[BaseGun] [子弹线段] 自动创建模板：";
    private const string LOG_INTERACT_SCRIPT_NULL = "[BaseGun] 没找到交互脚本！";
    private const string LOG_SHOOT_FAIL = "[BaseGun] 射击失败：";
    private const string LOG_FORCE_DROP = "[BaseGun] [强制丢枪] 仅服务器可执行该逻辑！";
    private const string BULLET_TEMPLATE_NAME = "Auto_BulletSegmentTemplate";
    private const string SOUND_HIT_WALL = "Music/正式/交互/击中墙";
    private const string SOUND_HIT_BULLSEYE = "Music/正式/交互/击中靶子";
    private const string SOUND_DROP_GUN = "Music/正式/交互/掉枪";
    private const string SHADER_DEFAULT = "Sprites/Default";
    #endregion

    private static Material _sharedBulletMaterial;

    #region 【GC缓存优化】固定向量缓存
    private readonly Vector2 _zeroVector2 = Vector2.zero;
    private readonly Vector3 _zeroVector3 = Vector3.zero;
    #endregion

    #region 【GC缓存优化】全局单例缓存
    private ConfigManager _configManager;
    private GameSkinManager _gameSkinManager;
    private PoolManage _poolManage;
    private MusicManager _musicManager;
    private MyCameraControl _cameraControl;
    private SimpleAnimatorTool _animatorTool;
    private UImanager _uiManager;
    private CountDownManager _countDownManager;
    #endregion

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
    [SyncVar]
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
    [Header("Timeline动画")]
    public PlayableDirector timelineDirector_Reload;
    public PlayableDirector timelineDirector_Shoot;

    [Header("枪械配置文件")]
    public GunInfo gunInfo;
    #endregion

    #region 皮肤数据配置
    [Header("枪口火控")]
    public MuzzleFlashConfig muzzleFlashConfig;
    public MuzzleFlash muzzleFlash;
    [Header("子弹视觉配置")]
    public BulletVisualConfig bulletVisualConfig;//视觉配置类，包含颜色、长度、宽度、飞行速度等参数

    [Header("枪口火控数据ID")]
    [SyncVar(hook = nameof(OnChangeMuzzleFlashConfigID))]
    public int muzzleFlashConfigID;

    [Header("打击特效配置ID")]
    [SyncVar(hook = nameof(OnChangeHitEffectConfigID))]
    public int hitEffectConfigID = 1;


    private void OnChangeHitEffectConfigID(int OldValue, int newValue)
    {
        var skinManager = _gameSkinManager ?? GameSkinManager.Instance;
        if (skinManager != null)
        {
            hitwalleffect = skinManager.GetHitData(newValue)?.HitObj;
        }
    }

    private void OnChangeMuzzleFlashConfigID(int OldValue, int newValue)
    {
        if (newValue > 0)
        {
            var confManager = _configManager ?? ConfigManager.Instance;
            if (confManager != null)
            {
                muzzleFlashConfig = confManager.GetMuzzleConfig(newValue);
                if (muzzleFlash != null) muzzleFlash.config = muzzleFlashConfig;
            }
        }
    }

    [Header("子弹视觉数据ID")]
    [SyncVar(hook = nameof(OnChangeBulletVisualConfigID))]
    public int bulletVisualConfigID;

    private void OnChangeBulletVisualConfigID(int OldValue, int newValue)
    {
        if (newValue > 0)
        {
            var confManager = _configManager ?? ConfigManager.Instance;
            if (confManager != null)
                bulletVisualConfig = confManager.GetBulletConfig(newValue);
        }
    }

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

    [Header("子弹小线段配置(如果枪械原本的配置缺失就使用默认数值)")]
    public Color bulletColor = new Color(0.83f, 0.68f, 0.22f);
    public float bulletSegmentLength = 0.2f;
    public float bulletLineWidth = 0.03f;
    public float bulletFlySpeed = 80f;
    public float bulletShowDuration = 0.5f;
    [SerializeField]
    public GameObject bulletSegmentPrefab;

    [Header("伤害数字显示")]
    public GameObject DamageFloatObj;
    #endregion


    #region 伤害数字本地显示
    private void ServerNotifyShowDamage(float damage, Vector2 hitPos, Player attackerPlayer)
    {
        if (attackerPlayer == null || attackerPlayer.connectionToClient == null) return;
        TargetShowDamageNumber(attackerPlayer.connectionToClient, damage, hitPos);
    }

    [TargetRpc]
    private void TargetShowDamageNumber(NetworkConnectionToClient target, float damage, Vector2 hitPos)
    {
        ShowDamageNumberLocal(damage, hitPos);
    }

    private void ShowDamageNumberLocal(float damage, Vector2 hitPos)
    {
        if (DamageFloatObj == null || _poolManage == null) return;

        var Obj = _poolManage.GetObj(DamageFloatObj);
        if (Obj != null)
        {
            Obj.transform.position = hitPos;
            Obj.GetComponent<DamageFloat>()?.Init(damage, Obj.transform);
        }
    }
    #endregion

    #region 内部缓存字段
    private GameObject _autoBulletSegmentTemplate;
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
            if (!isServer)
                return;
            if (_isInReload != value)
            {
                _isInReload = value;
                if (value)
                {
                    _isInShoot = false;
                }
            }
        }
    }

    public bool IsInShoot
    {
        get => _isInShoot;
        set
        {
            if (!isServer)
                return;
            if (_isInShoot != value)
            {
                _isInShoot = value;
                if (value)
                    _canShoot = false;
            }
        }
    }

    public bool CanShoot
    {
        get => _canShoot;
        set
        {
            if (!isServer)
                return;
            if (_canShoot != value)
                _canShoot = value;
        }
    }

    public float CurrentMagazineBulletCount => _currentMagazineBulletCount;
    public float AllReserveBulletCount => _allReserveBulletCount;
    #endregion

    #region SyncVar钩子
    private void OnIsInReloadChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;
        if (timelineDirector_Reload != null)
        {
            if (newValue)
                timelineDirector_Reload.Play();
            else
                timelineDirector_Reload.Stop();
        }
        else Debug.LogError(LOG_TIMELINE_RELOAD_NULL, gameObject);
    }
    private void OnCanShootChanged(bool oldValue, bool newValue) { }

    private void OnCurrentMagazineBulletChanged(float oldValue, float newValue)
    {
        if (Player.LocalPlayer == null)
            return;

        if (this == Player.LocalPlayer.currentGun && _uiManager != null && _uiManager.GetPanel<PlayerPanel>() != null)
        {
            _uiManager.GetPanel<PlayerPanel>().UpdateGunBulletAmountText();
        }
    }

    private void OnAllReserveBulletChanged(float oldValue, float newValue)
    {
        if (Player.LocalPlayer == null)
            return;
        if (this == Player.LocalPlayer.currentGun && _uiManager != null && _uiManager.GetPanel<PlayerPanel>() != null)
        {
            _uiManager.GetPanel<PlayerPanel>().UpdateGunBulletAmountText();
        }
    }

    private void OnIsInPlayerHandChanged(bool oldValue, bool newValue)
    {
        if (!isClient || myRigidbody == null)
            return;

        myRigidbody.simulated = !newValue;
        myRigidbody.isKinematic = newValue;

        if (newValue)
        {
            myRigidbody.velocity = _zeroVector2;
            myRigidbody.angularVelocity = 0;
            InitLocalAimProperties();

            if (isClient)
                StopFlashAnimation();
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

            if (isServer)
                StartDestroyTimer();
        }
    }

    private void OnIsEnterAimState(bool oldValue, bool newValue)
    {
        if (!isClient || gunInfo == null || ownerPlayer == null || ownerPlayer.myStats == null || _animatorTool == null)
        {
            Debug.LogError($"{LOG_PREFIX}[瞄准状态] 执行条件不满足，跳过状态切换");
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

    #region 对子弹的补充
    [Command]
    public void CmdBulletSupplement()
    {
        if (!isServer)
        {
            Debug.LogError(LOG_SERVER_ERROR);
            return;
        }
        _allReserveBulletCount = gunInfo.AllBulletAmount;

        Debug.Log($"{LOG_PREFIX}[备弹补充] 完成！备弹已加满至 {_allReserveBulletCount}");
    }
    #endregion

    #region 核心Command方法
    [Command]
    public void ChangeAimState(bool IsEnter)
    {
        IsEnterAimState = IsEnter;
    }
    [Command(requiresAuthority = true)]
    public void CmdStartShoot()
    {
        if (!isServer)
        {
            Debug.LogError(LOG_SERVER_ERROR);
            return;
        }
        bool canShootServer = IsCanShoot();
        if (!canShootServer)
            return;

        IsInShoot = true;

        RpcPlayShootAnimation();
    }

    [Command(requiresAuthority = true)]
    public void CmdExecuteShootLogic()
    {
        if (!isServer)
        {
            Debug.LogError(LOG_SERVER_ERROR);
            return;
        }
        _currentMagazineBulletCount = Mathf.Max(0, _currentMagazineBulletCount - 1);

        Vector2 bulletTargetPos = _zeroVector2;

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
                            Debug.LogError($"{LOG_PREFIX} 攻击者{ownerPlayer.name} 无myStats组件！");
                            return;
                        }
                        ServerNotifyShowDamage(gunInfo.Damage, hit.point, ownerPlayer);
                        hitTarget.ServerApplyDamage(gunInfo.Damage, hit.point, hit.normal, attackerStats);
                    }
                }
                else if (hit.collider.CompareTag("BulletInteractObj"))
                {
                    BaseBulletInteract_NetWork interactObj = hit.collider.GetComponent<BaseBulletInteract_NetWork>();

                    if (interactObj == null)
                    {
                        Debug.LogError(LOG_INTERACT_SCRIPT_NULL, hit.collider);
                        return;
                    }

                    interactObj.TakeDamage(gunInfo.Damage);
                }
                else if (hit.collider.CompareTag("Ground"))
                {
                    RpcSpawnHitEffect(hit.point, hit.normal);
                }
            }

            bulletTargetPos = hit ? hit.point : (Vector2)firePoint.position + shootDir * gunInfo.Range;
        }
        else
        {
            Debug.LogError($"{LOG_SHOOT_FAIL} firePoint={firePoint != null} | gunInfo={gunInfo != null} | ownerPlayer={ownerPlayer != null}");
        }

        RpcPlaySingleShootVFX();
        //  完全保留子弹绘制，不动逻辑！
        RpcDrawBulletSegment(bulletTargetPos);
    }

    #region 全局音效播放
    [ClientRpc]
    public void RpcPlayerMusic(string SoundPath, float maxDistance, float minDistance)
    {
        _musicManager?.PlayEffect3D(SoundPath, maxDistance: maxDistance, minDistance: minDistance);
    }

    #endregion

    [Command(requiresAuthority = true)]
    public void CmdFinishShoot()
    {
        if (!isServer)
        {
            Debug.LogError(LOG_SERVER_ERROR);
            return;
        }
        IsInShoot = false;
        CanShoot = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdStartReload()
    {
        if (!isServer) { Debug.LogError(LOG_SERVER_ERROR); return; }
        bool canReloadServer = IsCanReload();
        if (!canReloadServer) return;
        IsInReload = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdFinishReloadLogic()
    {
        ServerFinishReload();
    }
    #endregion

    #region ClientRpc（服务器→所有客户端）
    [ClientRpc]
    private void RpcPlaySingleShootVFX() => PlaySingleShootVFX();

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector2 hitPos, Vector2 hitNormal)
    {
        if (hitwalleffect == null)
        {
            Debug.LogError(LOG_HIT_EFFECT_NULL);
            return;
        }
        if (_poolManage == null) return;

        GameObject hitEffectObj = _poolManage.GetObj(hitwalleffect);
        if (hitEffectObj == null)
            return;
        hitEffectObj.transform.position = hitPos;
        hitEffectObj.transform.rotation = Quaternion.LookRotation(_zeroVector3, hitNormal);
        _countDownManager?.CreateTimer(false, 500, () => { _poolManage.PushObj(hitwalleffect, hitEffectObj); });

        _musicManager?.PlayEffect3D_Custom($"{SOUND_HIT_WALL}{Random.Range(1, 4)}", 0.2f, hitPos, Player.LocalPlayer.transform.position, maxDistance: 5f);
    }

    [ClientRpc]
    private void RpcPlayShootAnimation()
    {
        if (ownerPlayer != null && ownerPlayer.isLocalPlayer)
            return;

        if (timelineDirector_Shoot != null)
        {
            timelineDirector_Shoot.Stop();
            timelineDirector_Shoot.Play();
        }
        else
        {
            Debug.LogError(LOG_TIMELINE_SHOOT_NULL, gameObject);
        }
    }

    [ClientRpc]
    private void RpcDrawBulletSegment(Vector2 targetPos)
    {
        //  子弹视觉100%保留，绝不关闭！
        if (firePoint == null)
            return;

        GameObject template = GetBulletSegmentTemplate();
        if (template == null)
        {
            Debug.LogError($"{LOG_PREFIX}[子弹线段] 模板创建失败，跳过绘制");
            return;
        }

        GameObject bulletObj = _poolManage?.GetObj(template);
        if (bulletObj == null)
        {
            bulletObj = Instantiate(template);
            bulletObj.name = BULLET_TEMPLATE_NAME;
        }

        bulletObj.transform.SetParent(null);
        bulletObj.SetActive(true);
        template.SetActive(false);

        LineRenderer lr = bulletObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = bulletObj.AddComponent<LineRenderer>();
            lr.material = GetSharedBulletMaterial();
        }

        Color applyColor = bulletVisualConfig != null ? bulletVisualConfig.bulletColor : bulletColor;
        lr.startColor = applyColor;
        lr.endColor = applyColor;

        float applyWidth = bulletVisualConfig != null ? bulletVisualConfig.bulletLineWidth : bulletLineWidth;
        lr.startWidth = applyWidth;
        lr.endWidth = applyWidth;

        lr.positionCount = 2;
        lr.sortingOrder = 100;
        lr.enabled = true;
        Color resetColor = lr.startColor;
        resetColor.a = 1f;
        lr.startColor = resetColor;
        lr.endColor = resetColor;

        Vector2 startPos = firePoint.position;
        Vector2 shootDir = (targetPos - startPos).normalized;

        BulletSegmentFly fly = bulletObj.GetComponent<BulletSegmentFly>();
        if (fly == null) fly = bulletObj.AddComponent<BulletSegmentFly>();

        float applyLength = bulletVisualConfig != null ? bulletVisualConfig.bulletSegmentLength : bulletSegmentLength;
        float applySpeed = bulletVisualConfig != null ? bulletVisualConfig.bulletFlySpeed : bulletFlySpeed;
        float applyDuration = bulletVisualConfig != null ? bulletVisualConfig.bulletShowDuration : bulletShowDuration;

        fly.Init(lr, startPos, targetPos, shootDir, applyLength, applySpeed, applyDuration, template);

        float totalDuration = Vector2.Distance(startPos, targetPos) / applySpeed + applyDuration;
        _countDownManager?.CreateTimer(false, (int)(totalDuration * 500), () =>
        {
            if (bulletObj != null) _poolManage.PushObj(template, bulletObj);
        });
    }
    #endregion

    private Material GetSharedBulletMaterial()
    {
        if (_sharedBulletMaterial == null)
        {
            _sharedBulletMaterial = new Material(Shader.Find(SHADER_DEFAULT));
            _sharedBulletMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        return _sharedBulletMaterial;
    }

    #region 客户端视觉特效逻辑
    public void PlaySingleShootVFX()
    {
        if (applyAutoEjectCartridge)
            SpawnCartridgeCase();
        ApplyRecoil();
        MuzzleSmokeManager.Instance?.PlayMuzzleSmoke(firePoint, gunInfo);

        if (bulletVisualConfig != null && cartridgeEjectPoint != null)
        {
            MuzzleSmokeManager.Instance?.PlayMuzzleSmoke(cartridgeEjectPoint, bulletVisualConfig);
        }

        if (muzzleFlash != null) muzzleFlash.PlayFlash();
        if (ownerPlayer != null && ownerPlayer.isLocalPlayer && firePoint != null && gunInfo != null)
        {
            Vector2 firePointRightDir = firePoint.transform.right;
            Vector2 baseDir = -firePointRightDir * ownerPlayer.FacingDir;
            Vector2 shootDir = CalculateLocalBulletScattering(baseDir);

            RaycastHit2D localHit = Physics2D.Raycast(
                firePoint.position,
                shootDir,
                gunInfo.Range,
                shootRaycastLayers
            );

            if (localHit.collider != null && localHit.collider.CompareTag("Bullseye"))
            {
                Bullseye localBullseye = localHit.collider.GetComponent<Bullseye>();
                if (localBullseye != null)
                {
                    localBullseye.Wound(gunInfo.Damage);
                }

                Vector2 playerPos = ownerPlayer.transform.position;
                Vector2 targetPos = localHit.collider.transform.position;
                float distance = Vector2.Distance(playerPos, targetPos);

                float minDelay = 50f;
                float maxDelay = 700f;
                float soundSpeed = 2.5f;

                float dynamicDelay = Mathf.Clamp(distance * soundSpeed * 10f, minDelay, maxDelay);

                _countDownManager?.CreateTimer(false, (int)dynamicDelay, () => {
                    _musicManager?.PlayEffect3D_Custom($"{SOUND_HIT_BULLSEYE}{Random.Range(1, 3)}", 0.5f, targetPos, playerPos);
                });

                Debug.Log($"{LOG_PREFIX}击中靶子 | 距离：{distance:F1}m | 声音延迟：{(int)dynamicDelay}ms");
            }
        }

        if (isLocalPlayer)
            _cameraControl?.AddTimeBasedShake(gunInfo.ShackStrength, gunInfo.ShackTime);
    }

    private Vector2 CalculateLocalBulletScattering(Vector2 centerDir)
    {
        if (gunInfo == null) return centerDir;
        int baseAngle = 20;
        float maxAngle = baseAngle * (1 - _localAccuracy / 100f);
        float randomAngle = Random.Range(-maxAngle, maxAngle);
        return Quaternion.Euler(0, 0, randomAngle) * centerDir;
    }

    public void handMovement_SpawnCartridgeCase_TimeLine()
    {
        SpawnCartridgeCase();
    }

    private void SpawnCartridgeCase()
    {
        if (cartridgeCasePrefab == null) return;
        if (cartridgeEjectPoint == null) { Debug.LogError(LOG_CARTRIDGE_POINT_NULL); return; }

        GameObject cartridgeObj = _poolManage?.GetObj(cartridgeCasePrefab);
        if (cartridgeObj == null) { Debug.LogError(LOG_CARTRIDGE_POOL_NULL); return; }

        Rigidbody2D rb2D = cartridgeObj.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.velocity = _zeroVector2;
            rb2D.angularVelocity = 0f;
        }

        cartridgeObj.transform.position = cartridgeEjectPoint.position;
        cartridgeObj.transform.rotation = cartridgeEjectPoint.rotation;

        SpriteRenderer cartridgeSr = cartridgeObj.GetComponent<SpriteRenderer>();
        if (bulletVisualConfig != null)
        {
            if (cartridgeSr != null)
                cartridgeSr.color = bulletVisualConfig.cartridgeCaseColor;
            cartridgeObj.transform.localScale = Vector3.one * bulletVisualConfig.cartridgeCaseSize;
        }
        else
        {
            if (cartridgeSr != null)
                cartridgeSr.color = new Color(0.83f, 0.68f, 0.22f);
            cartridgeObj.transform.localScale = cartridgeFixedScale;
        }

        if (rb2D != null)
        {
            Vector2 localRightDir = cartridgeEjectPoint.transform.TransformDirection(Vector2.right);
            Vector2 ejectForce = localRightDir * Random.Range(1f, 3f) + Vector2.up * Random.Range(0.5f, 2f);
            rb2D.AddForce(ejectForce, ForceMode2D.Impulse);
            rb2D.AddTorque(Random.Range(-5f, 5f));
        }

        _countDownManager?.CreateTimer(false, 1000, () =>
        {
            if (bulletVisualConfig != null)
                cartridgeObj.transform.localScale = Vector3.one * bulletVisualConfig.cartridgeCaseSize;
            else
                cartridgeObj.transform.localScale = cartridgeFixedScale;

            SpriteRenderer sr = cartridgeObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (bulletVisualConfig != null)
                    sr.color = bulletVisualConfig.cartridgeCaseColor;
                else
                    sr.color = new Color(0.83f, 0.68f, 0.22f);
            }

            _poolManage?.PushObj(cartridgeCasePrefab, cartridgeObj);
        });
    }

    private void ApplyRecoil()
    {
        if (ownerPlayer == null || ownerPlayer.MyRigdboby == null || gunInfo == null)
        {
            Debug.LogError(LOG_RECOIL_NULL);
            return;
        }
        Vector2 recoilForce = new Vector2(-ownerPlayer.FacingDir * _localRecoil * recoilForceScale, 0);
        ownerPlayer.MyRigdboby.AddForce(recoilForce, ForceMode2D.Impulse);
        if (ownerPlayer.isLocalPlayer)
        {
            _cameraControl?.AddTimeBasedShake(gunInfo.ShackStrength, gunInfo.ShackTime);
            Player.LocalPlayer.MyHandControl.AddGunMomentOfForce();
        }
        _musicManager?.PlayEffect3D(gunInfo.ShootAudio, 0.7f, 1, 10, this.transform);
    }
    #endregion

    #region 服务器辅助逻辑
    private Vector2 CalculateBulletScattering(Vector2 centerDir)
    {
        if (gunInfo == null)
        { Debug.LogError(LOG_GUNINFO_NULL); return centerDir; }
        if (_localAccuracy < 100)
        {
            int baseAngle = 20;
            float maxAngle = baseAngle * (1 - _localAccuracy / 100f);
            float randomAngle = Random.Range(-maxAngle, maxAngle);
            return Quaternion.Euler(0, 0, randomAngle) * centerDir;
        }
        else
        {
            return centerDir;
        }
    }

    [Server]
    private void ServerFinishReload()
    {
        if (!isServer) { Debug.LogError(LOG_SERVER_ERROR); return; }
        if (gunInfo == null) { Debug.LogError(LOG_GUNINFO_NULL); return; }

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
        if (!IsCanShoot())
            return;
        timelineDirector_Shoot.Play();
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
        if (gunInfo == null) { Debug.LogError(LOG_GUNINFO_NULL); return false; }
        return !IsInReload && !IsInShoot && AllReserveBulletCount > 0 && CurrentMagazineBulletCount < gunInfo.Bullet_capacity;
    }

    public bool IsCanShoot()
    {
        if (gunInfo == null)
        {
            Debug.LogError(LOG_GUNINFO_NULL);
            return false;
        }
        return !IsInReload && !IsInShoot && CanShoot && CurrentMagazineBulletCount > 0;
    }
    #endregion

    #region 初始化与生命周期
    public virtual void Awake()
    {
        MySprite = GetComponent<SpriteRenderer>();
        // 确保不会在此处挂载没注册预制体的NetworkIdentity导致问题
        _netIdentity = GetComponent<NetworkIdentity>();
        myRigidbody = GetComponent<Rigidbody2D>();
        _gunWorldInfoShow = GetComponentInChildren<GunWorldInfoShow>() ?? GetComponent<GunWorldInfoShow>();
        GunInfoManager = _gunWorldInfoShow;
        InitBulletSegmentTemplate();

        CacheSingletonInstances();
    }

    /// <summary>
    /// 缓存全局单例，消除重复Instance调用GC
    /// </summary>
    private void CacheSingletonInstances()
    {
        _configManager = ConfigManager.Instance;
        _gameSkinManager = GameSkinManager.Instance;
        _poolManage = PoolManage.Instance;
        _musicManager = MusicManager.Instance;
        _cameraControl = MyCameraControl.Instance;
        _animatorTool = SimpleAnimatorTool.Instance;
        _uiManager = UImanager.Instance;
        _countDownManager = CountDownManager.Instance;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (gunInfo != null)
        {
            _currentMagazineBulletCount = 0;
            _allReserveBulletCount = gunInfo.AllBulletAmount;
            Debug.Log($"{LOG_PREFIX}[服务器] 初始化子弹 → 弹匣:{_currentMagazineBulletCount} | 备用:{_allReserveBulletCount}");
        }
        else Debug.LogError(LOG_GUNINFO_NULL);

        RemainingDestoryTime = DestoryTime;
    }

    private GunSkinPack _currentSkinPack;

    [Header("枪械皮肤数据ID")]
    [SyncVar(hook = nameof(OnChangeGunSkinID))]
    public int gunSkinID;

    // 客户端收到同步后，加载皮肤图片和动画
    private void OnChangeGunSkinID(int oldValue, int newValue)
    {
        if (newValue > 0)
        {
            var skinManager = _gameSkinManager ?? GameSkinManager.Instance;
            if (skinManager != null)
            {
                _currentSkinPack = skinManager.GetGunSkinPack(newValue);
                if (_currentSkinPack != null)
                {
                    // 替换图片
                    if (MySprite != null)
                        MySprite.sprite = _currentSkinPack.skinIcon;

                    // 替换换弹动画
                    if (timelineDirector_Reload != null)
                        timelineDirector_Reload.playableAsset = _currentSkinPack.GunReload;
                }
            }
        }
    }

    // 这个方法是服务器专用的，绝不能带
    [Server]
    public void SetGunConfig(int muzzleFlashID, int bulletVisualID, int hitID, int skinID)
    {
        this.muzzleFlashConfigID = muzzleFlashID;
        this.bulletVisualConfigID = bulletVisualID;
        this.hitEffectConfigID = hitID;

        this.gunSkinID = skinID;
    }

    #endregion

    #region 枪械拾取/丢弃逻辑
    [Server]
    public void SafeServerOnGunPicked()
    {
        if (GunInfoManager == null) { Debug.LogError(LOG_GUNINFO_MANAGER_NULL); return; }
        GunInfoManager.ServerOnGunPicked();
    }

    [Server]
    public void SafeServerOnGunDropped()
    {
        if (GunInfoManager == null) { Debug.LogError(LOG_GUNINFO_MANAGER_NULL); return; }
        GunInfoManager.ServerOnGunDropped();
    }
    #endregion

    #region 子弹线段模板管理
    private void InitBulletSegmentTemplate()
    {
        _autoBulletSegmentTemplate = new GameObject(BULLET_TEMPLATE_NAME);
        _autoBulletSegmentTemplate.SetActive(false);
        _autoBulletSegmentTemplate.transform.SetParent(this.transform);

        LineRenderer templateLr = _autoBulletSegmentTemplate.AddComponent<LineRenderer>();
        templateLr.material = GetSharedBulletMaterial();

        if (bulletVisualConfig != null)
        {
            templateLr.startColor = bulletVisualConfig.bulletColor;
            templateLr.endColor = bulletVisualConfig.bulletColor;
            templateLr.startWidth = bulletVisualConfig.bulletLineWidth;
            templateLr.endWidth = bulletVisualConfig.bulletLineWidth;
        }
        else
        {
            templateLr.startColor = bulletColor;
            templateLr.endColor = bulletColor;
            templateLr.startWidth = bulletLineWidth;
            templateLr.endWidth = bulletLineWidth;
        }

        templateLr.positionCount = 2;
        templateLr.sortingOrder = 100;
        templateLr.enabled = false;

        _autoBulletSegmentTemplate.AddComponent<BulletSegmentFly>();
        Debug.Log($"{LOG_BULLET_TEMPLATE}{BULLET_TEMPLATE_NAME}");
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
    public float _localAccuracy;

    private void InitLocalAimProperties()
    {
        if (gunInfo == null)
            return;

        _localRecoil = gunInfo.Recoil;
        _localAccuracy = gunInfo.Accuracy;
    }

    private void ResetLocalAimProperties()
    {
        if (gunInfo == null)
            return;

        _localRecoil = gunInfo.Recoil;
        _localAccuracy = gunInfo.Accuracy;
    }

    public void StopAllAimLerp()
    {
        if (_animatorTool == null)
            return;

        if (AnimationID_Recoil != -1)
        {
            _animatorTool.StopFloatLerpById(AnimationID_Recoil);
            AnimationID_Recoil = -1;
        }
        if (AnimationID_ViewRange != -1)
        {
            _animatorTool.StopFloatLerpById(AnimationID_ViewRange);
            AnimationID_ViewRange = -1;
        }
        if (AnimationID_Accuracy != -1)
        {
            _animatorTool.StopFloatLerpById(AnimationID_Accuracy);
            AnimationID_Accuracy = -1;
        }
    }

    public void EnterAimState()
    {
        StopAllAimLerp();

        AnimationID_Recoil = _animatorTool.StartFloatLerp(
            _localRecoil,
            _localRecoil * (1 - ownerPlayer.myStats.AimRecoilBonus),
            Duration,
            (value) => { _localRecoil = value; }
        );
        AnimationID_Accuracy = _animatorTool.StartFloatLerp(
            _localAccuracy,
            _localAccuracy * (1 + ownerPlayer.myStats.AimAccuracyBonus),
            Duration,
            (value) => { _localAccuracy = value; }
        );
    }

    public void ExitAimState()
    {
        StopAllAimLerp();
        AnimationID_Recoil = _animatorTool.StartFloatLerp(
            _localRecoil,
            gunInfo.Recoil,
            Duration / 2,
            (value) => { _localRecoil = value; }
        );
        AnimationID_Accuracy = _animatorTool.StartFloatLerp(
            _localAccuracy,
            gunInfo.Accuracy,
            Duration / 2,
            (value) => { _localAccuracy = value; }
        );
    }
    #endregion

    #region 强制丢弃枪械
    [Command(requiresAuthority = false)]
    public void CmdForceDiscardGun()
    {
        if (!isServer) { Debug.LogError(LOG_FORCE_DROP); return; }
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
            ownerPlayer.ServerHandleDropGun(gameObject, false);
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

    #region 枪械物理相关
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground") && !isInPlayerHand)
        {
            _musicManager?.PlayEffect3D_Custom(
                SOUND_DROP_GUN,
                1f,
                transform.position,
                Player.LocalPlayer.transform.position,
                minDistance: 1f,
                maxDistance: 5f
            );
        }
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