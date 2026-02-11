using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.Events;

public class BaseGun : NetworkBehaviour
{
    [Header("刚体组件")]
    public Rigidbody2D myRigidbody;

    [Header("=== 枪械核心状态（服务器权威，SyncVar同步） ===")]
    [SerializeField]
    [SyncVar(hook = nameof(OnIsInReloadChanged))]
    protected bool _isInReload = false; // 换弹状态
    [SerializeField]
    [SyncVar(hook = nameof(OnIsInShootChanged))]
    protected bool _isInShoot = false;  // 单次射击状态
    [SerializeField]
    [SyncVar(hook = nameof(OnCanShootChanged))]
    protected bool _canShoot = true;    // 可射击状态
    [SerializeField]
    [SyncVar(hook = nameof(OnCurrentMagazineBulletChanged))]
    protected float _currentMagazineBulletCount = 0; // 当前弹匣子弹
    [SerializeField]
    [SyncVar(hook = nameof(OnAllReserveBulletChanged))]
    protected float _allReserveBulletCount = 0;      // 备用子弹

    [Header("=== 枪械组件配置 ===")]
    [Header("Timeline动画")]
    public PlayableDirector timelineDirector_Reload; // 换弹动画
    public PlayableDirector timelineDirector_Shoot;  // 单次射击动画

    [Header("枪械配置文件")]
    public GunInfo gunInfo; // 枪械属性配置

    [Header("射击特效")]
    public GameObject hitwalleffect;//击中墙壁粒子效果；
    public Transform firePoint;        // 射击点（仅传递给全局管理器）
    public GameObject cartridgeCasePrefab;  // 弹壳预制体
    public Transform cartridgeEjectPoint;   // 抛壳点
    public float recoilForceScale = 1f;     // 后坐力缩放
    public Vector3 cartridgeFixedScale = new Vector3(0.2f, 0.2f, 1f);

    [Header("调试配置")]
    public bool isDebug = true; // 是否打印调试日志
    [Header("子弹小线段配置")]
    public Color bulletColor = new Color(0.83f, 0.68f, 0.22f); // 子弹黄铜色 #D4AF37
    public float bulletSegmentLength = 0.2f; // 子弹小线段的长度（可调节）
    public float bulletLineWidth = 0.03f;    // 子弹小线段的宽度（可调节）
    public float bulletFlySpeed = 80f;       // 子弹飞行速度
    public float bulletShowDuration = 0.5f;  // 子弹飞行完成后淡出时长
    [SerializeField]
    public GameObject bulletSegmentPrefab;   // 可选：手动赋值预制体（不赋值则自动创建）

    [Header("是否应用自动抛壳")]
    public bool applyAutoEjectCartridge = true;

    private GameObject _autoBulletSegmentTemplate;
    // 缓存模板名称
    private readonly string _bulletSegmentTemplateName = "Auto_BulletSegmentTemplate";

    // 缓存NetworkIdentity
    private NetworkIdentity _netIdentity;

    [HideInInspector]
    public Vector3 originalWorldScale; // 存储枪械原始世界缩放

    [Header("当前拾取玩家")]
    [SyncVar]
    public Player ownerPlayer;//全局的变量
    [SyncVar(hook = nameof(OnIsInPlayerHandChanged))]
    public bool isInPlayerHand = false; // 是否被玩家拾取

    public UnityAction ReloadSuccessAction;// 换弹成功回调事件

    // 缓存枪械信息显示脚本
    private GunWorldInfoShow _gunWorldInfoShow;

    public GunWorldInfoShow GunInfoManager;


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
                if (value) _isInShoot = false; // 换弹时强制停止射击
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

    // 对外暴露子弹数
    public float CurrentMagazineBulletCount => _currentMagazineBulletCount;
    public float AllReserveBulletCount => _allReserveBulletCount;

    #endregion

    #region SyncVar钩子
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
        //如果是当前玩家的枪械就更新一下UI
        if (this == Player.LocalPlayer.currentGun && UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().UpdateGunBulletAmountText();//更新一下子弹信息
        }
    }
    private void OnAllReserveBulletChanged(float oldValue, float newValue)
    {
        //如果是当前玩家的枪械就更新一下UI
        if (this == Player.LocalPlayer.currentGun && UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().UpdateGunBulletAmountText();//更新一下子弹信息
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
        }
    }
    #endregion

    #region 核心Command方法
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

        // 扣子弹
        _currentMagazineBulletCount = Mathf.Max(0, _currentMagazineBulletCount - 1);

        // 服务器射线检测
        if (firePoint != null && gunInfo != null && ownerPlayer != null)
        {
            Vector2 baseDir = new Vector2(ownerPlayer.FacingDir, 0);
            Vector2 shootDir = CalculateBulletScattering(baseDir);

            HashSet<string> ignoreTags = new HashSet<string>
            {
                "Player",
                "Gun",
                "cartridgeCase"
            };

            // 执行基础射线检测
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, shootDir, gunInfo.Range);

            if (hit.collider != null && ignoreTags.Contains(hit.collider.tag))
            {
                hit = new RaycastHit2D();
            }

            // 计算子弹目标位置
            Vector2 bulletTargetPos = hit ? hit.point : (Vector2)firePoint.position + shootDir * gunInfo.Range;

            // 同步客户端绘制子弹小线段
            if (isDebug)
                RpcDrawBulletSegment(firePoint.position, bulletTargetPos, shootDir);

            // 仅在命中有效对象时触发打击特效
            if (hit)
                RpcSpawnHitEffect(hit.point, hit.normal);
        }

        // 同步客户端播放射击视觉特效
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
        if (!isServer)
        { Debug.LogError($"[服务器] CmdStartReload非服务器环境！"); return; }
        bool canReloadServer = IsCanReload();
        if (!canReloadServer)
            return;
        IsInReload = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdFinishReloadLogic()//计算最后的子弹
    {
        if (!isServer)
        { Debug.LogError($"[服务器] CmdFinishReloadLogic非服务器环境！"); return; }
        if (gunInfo == null) { Debug.LogError($"[服务器] gunInfo未赋值！"); return; }

        float needBulletCount = gunInfo.Bullet_capacity - _currentMagazineBulletCount;
        if (needBulletCount <= 0)
        { IsInReload = false; CanShoot = true; return; }

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

    #region ClientRpc
    [ClientRpc]
    private void RpcPlaySingleShootVFX() => PlaySingleShootVFX();

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector2 hitPos, Vector2 hitNormal)
    {
        if (hitwalleffect == null)
        {
            Debug.LogError("[打击特效] hitwalleffect 预制体未赋值！");
            return;
        }
        GameObject hitEffectObj = Instantiate(
            hitwalleffect,
            hitPos,
            Quaternion.LookRotation(Vector3.forward, hitNormal) 
        );

    }

    /// <summary>
    /// 同步所有客户端绘制子弹小线段（无预制体自动创建模板）
    /// </summary>
    [ClientRpc]
    private void RpcDrawBulletSegment(Vector2 startPos, Vector2 targetPos, Vector2 shootDir)
    {
        if (!isDebug)
            return;

        GameObject template = GetBulletSegmentTemplate();
        if (template == null)
        {
            Debug.LogError("[子弹线段] 模板创建失败，跳过绘制");
            return;
        }

        GameObject bulletObj = PoolManage.Instance?.GetObj(template);
        if (bulletObj == null)
        {
            // 对象池无缓存，基于模板实例化新对象
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
        // 重置透明度
        Color resetColor = lr.startColor;
        resetColor.a = 1f;
        lr.startColor = resetColor;
        lr.endColor = resetColor;

        BulletSegmentFly fly = bulletObj.GetComponent<BulletSegmentFly>();
        if (fly == null) fly = bulletObj.AddComponent<BulletSegmentFly>();
        fly.Init(
            lr,
            startPos,
            targetPos,
            shootDir,
            bulletSegmentLength,
            bulletFlySpeed,
            bulletShowDuration,
            template // 传入模板用于回收
        );

        float totalDuration = Vector2.Distance(startPos, targetPos) / bulletFlySpeed + bulletShowDuration;
        CountDownManager.Instance.CreateTimer(false, (int)(totalDuration * 1000), () =>
        {
            if (bulletObj != null)
            {
                PoolManage.Instance.PushObj(template, bulletObj);
            }
        });
    }
    #endregion

    #region 客户端视觉特效
    public void PlaySingleShootVFX()
    {
        if (applyAutoEjectCartridge)//如果不应用自己的抛壳逻辑就不执行了
            SpawnCartridgeCase();
        ApplyRecoil();

        MuzzleSmokeManager.Instance?.PlayMuzzleSmoke(firePoint, gunInfo);
    }

    public void handMovement_SpawnCartridgeCase_TimeLine()//手动调用的抛壳事件
    {
        SpawnCartridgeCase();
    }

    private void SpawnCartridgeCase()
    {
        if (cartridgeCasePrefab == null) return;
        if (cartridgeEjectPoint == null)
        { Debug.LogError($"[客户端] 抛壳点未赋值！"); return; }

        GameObject cartridgeObj = PoolManage.Instance?.GetObj(cartridgeCasePrefab);
        if (cartridgeObj == null)
        { Debug.LogError($"[客户端] 对象池获取弹壳失败！"); return; }

        // 重置弹壳刚体状态
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
            // 抛壳方向：抛壳点本地红色轴（X轴）
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

        Vector2 recoilForce = new Vector2(-ownerPlayer.FacingDir * gunInfo.Recoil * recoilForceScale, 0);
        ownerPlayer.MyRigdboby.AddForce(recoilForce, ForceMode2D.Impulse);

        if (ownerPlayer.isLocalPlayer)
            MyCameraControl.Instance?.AddTimeBasedShake(gunInfo.ShackStrength, gunInfo.ShackTime);
        MusicManager.Instance.PlayEffect3D(gunInfo.ShootAudio);
    }
    #endregion

    #region 服务器辅助逻辑
    private Vector2 CalculateBulletScattering(Vector2 centerDir)
    {
        if (gunInfo == null)
        { Debug.LogError($"[服务器] gunInfo未赋值！"); return centerDir; }
        int baseAngle = 5;
        float maxAngle = baseAngle * (1 - gunInfo.Accuracy / 100f);
        float randomAngle = Random.Range(-maxAngle, maxAngle);
        return Quaternion.Euler(0, 0, randomAngle) * centerDir;
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
        ReloadSuccessAction?.Invoke();//触发事件
     }
    #endregion

    #region Timeline回调
    public void OnShootFire_Timeline() => CmdExecuteShootLogic();
    public void OnShootEnd_Timeline() => CmdFinishShoot();
    public void OnReloadEnd_Timeline() => CmdFinishReloadLogic();
    public void OnShootVFX_Timeline() => PlaySingleShootVFX();
    #endregion

    #region 状态检测方法
    public bool IsCanReload()
    {
        if (gunInfo == null)
        { Debug.LogError($"IsCanReload：gunInfo未赋值！"); return false; }
        return !IsInReload && !IsInShoot && AllReserveBulletCount > 0 && CurrentMagazineBulletCount < gunInfo.Bullet_capacity;
    }

    public bool IsCanShoot()
    {
        if (gunInfo == null)
        { Debug.LogError($"IsCanShoot：gunInfo未赋值！"); return false; }
        return !IsInReload && !IsInShoot && CanShoot && CurrentMagazineBulletCount > 0;
    }
    #endregion

    #region 初始化与生命周期
    private void Awake()
    {
        _netIdentity = GetComponent<NetworkIdentity>() ?? gameObject.AddComponent<NetworkIdentity>();
        myRigidbody = GetComponent<Rigidbody2D>();
        _gunWorldInfoShow = GetComponentInChildren<GunWorldInfoShow>() ?? GetComponent<GunWorldInfoShow>();
        GunInfoManager = _gunWorldInfoShow;

        // 初始化子弹线段模板（无手动预制体则自动创建）
        InitBulletSegmentTemplate();

        // 移除本地烟雾控制器依赖，改为调用全局管理器
        if (GunInfoManager == null && isDebug) Debug.LogError($"未找到GunWorldInfoShow组件！");
        if (gunInfo == null) Debug.LogError($"gunInfo配置文件未挂载！");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gunInfo != null)
        {
            _currentMagazineBulletCount = gunInfo.Bullet_capacity;
            _allReserveBulletCount = gunInfo.AllBulletAmount;
            Debug.Log($"[服务器] 初始化子弹 → 弹匣:{_currentMagazineBulletCount} | 备用:{_allReserveBulletCount}");
        }
        else Debug.LogError($"[服务器] 初始化子弹失败：gunInfo未赋值！");
    }

    #endregion

    #region 枪械拾取/丢弃
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

    #region 子弹线段模板自动创建逻辑
    /// <summary>
    /// 初始化子弹线段模板
    /// </summary>
    private void InitBulletSegmentTemplate()
    {
        if (bulletSegmentPrefab != null)
        {
            _autoBulletSegmentTemplate = bulletSegmentPrefab;
            return;
        }

        _autoBulletSegmentTemplate = new GameObject(_bulletSegmentTemplateName);
        _autoBulletSegmentTemplate.SetActive(false); // 模板不显示
        _autoBulletSegmentTemplate.transform.SetParent(this.transform); // 挂到枪械对象下，防止丢失

        LineRenderer templateLr = _autoBulletSegmentTemplate.AddComponent<LineRenderer>();
        templateLr.material = new Material(Shader.Find("Sprites/Default"));
        templateLr.startColor = bulletColor;
        templateLr.endColor = bulletColor;
        templateLr.startWidth = bulletLineWidth;
        templateLr.endWidth = bulletLineWidth;
        templateLr.positionCount = 2;
        templateLr.sortingOrder = 100;
        templateLr.enabled = false; // 模板禁用渲染

        _autoBulletSegmentTemplate.AddComponent<BulletSegmentFly>();

        Debug.Log($"[子弹线段] 自动创建模板：{_bulletSegmentTemplateName}");
    }

    /// <summary>
    /// 获取子弹线段模板
    /// </summary>
    private GameObject GetBulletSegmentTemplate()
    {
        if (_autoBulletSegmentTemplate != null) return _autoBulletSegmentTemplate;
        // 兜底：再次尝试初始化模板
        InitBulletSegmentTemplate();
        return _autoBulletSegmentTemplate;
    }
    #endregion
}

/// <summary>
/// 让小线段整体向目标点飞行，始终朝向飞行方向（适配对象池）
/// </summary>
public class BulletSegmentFly : MonoBehaviour
{
    private LineRenderer _lr;          // 子弹的LineRenderer
    private Vector2 _startPos;         // 发射起点
    private Vector2 _targetPos;        // 飞行目标点
    private Vector2 _shootDir;         // 飞行方向
    private float _segmentLength;      // 小线段长度
    private float _flySpeed;           // 飞行速度
    private float _fadeDuration;       // 淡出时长

    private Vector2 _currentCenterPos; // 小线段当前中心位置
    private float _elapsedTime;        // 已飞行时间
    private bool _isReachTarget;       // 是否到达目标点
    private GameObject _prefab;        // 预制体引用（用于回收）

    /// <summary>
    /// 初始化子弹飞行参数（适配对象池）
    /// </summary>
    public void Init(LineRenderer lr, Vector2 startPos, Vector2 targetPos, Vector2 shootDir,
        float segmentLength, float flySpeed, float fadeDuration, GameObject prefab)
    {
        // 重置所有运行时状态
        _elapsedTime = 0f;
        _isReachTarget = false;
        _prefab = prefab;

        // 赋值核心参数
        _lr = lr;
        _startPos = startPos;
        _targetPos = targetPos;
        _shootDir = shootDir.normalized; // 归一化方向
        _segmentLength = segmentLength;
        _flySpeed = flySpeed;
        _fadeDuration = fadeDuration;

        _currentCenterPos = startPos; // 初始中心在发射点

        // 初始化第一帧的小线段顶点位置
        UpdateBulletSegmentPos();
    }

    private void Update()
    {
        if (_isReachTarget)
        {
            // 到达目标点：开始淡出
            FadeOutBullet();
            return;
        }

        // 未到达目标点：计算中心位置并移动
        _elapsedTime += Time.deltaTime;
        float moveDistance = _flySpeed * Time.deltaTime;
        _currentCenterPos = Vector2.MoveTowards(_currentCenterPos, _targetPos, moveDistance);

        // 更新小线段的顶点位置
        UpdateBulletSegmentPos();

        // 判断是否到达目标点
        if (Vector2.Distance(_currentCenterPos, _targetPos) < 0.01f)
        {
            _isReachTarget = true;
            _elapsedTime = 0f; // 重置时间用于淡出
        }
    }

    /// <summary>
    /// 更新小线段的两个顶点位置：始终以中心为基准，向飞行方向延伸固定长度
    /// </summary>
    private void UpdateBulletSegmentPos()
    {
        // 小线段的两个顶点 = 中心位置 ± 飞行方向 * 线段长度/2
        Vector2 pos1 = _currentCenterPos - _shootDir * (_segmentLength / 2);
        Vector2 pos2 = _currentCenterPos + _shootDir * (_segmentLength / 2);
        _lr.SetPosition(0, pos1);
        _lr.SetPosition(1, pos2);
    }

    /// <summary>
    /// 子弹到达目标点后：逐渐淡出
    /// </summary>
    private void FadeOutBullet()
    {
        _elapsedTime += Time.deltaTime;
        float fadeProgress = Mathf.Clamp01(_elapsedTime / _fadeDuration);
        Color currentColor = _lr.startColor;
        currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
        _lr.startColor = currentColor;
        _lr.endColor = currentColor;
    }

    /// <summary>
    /// 回收时重置所有状态
    /// </summary>
    private void OnDisable()
    {
        _elapsedTime = 0f;
        _isReachTarget = false;
        _currentCenterPos = Vector2.zero;

        // 重置LineRenderer透明度
        if (_lr != null)
        {
            Color resetColor = _lr.startColor;
            resetColor.a = 1f;
            _lr.startColor = resetColor;
            _lr.endColor = resetColor;
        }
    }
}