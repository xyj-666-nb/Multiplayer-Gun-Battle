using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class BaseGun : NetworkBehaviour
{
    [Header("刚体组件")]
    public Rigidbody2D myRigidbody;

    [Header("=== 枪械核心状态（服务器权威，SyncVar同步） ===")]
    [SerializeField]
    [SyncVar(hook = nameof(OnIsInReloadChanged))]
    private bool _isInReload = false; // 换弹状态
    [SerializeField]
    [SyncVar(hook = nameof(OnIsInShootChanged))]
    private bool _isInShoot = false;  // 单次射击状态
    [SerializeField]
    [SyncVar(hook = nameof(OnCanShootChanged))]
    private bool _canShoot = true;    // 可射击状态
    [SerializeField]
    [SyncVar(hook = nameof(OnCurrentMagazineBulletChanged))]
    private float _currentMagazineBulletCount = 0; // 当前弹匣子弹
    [SerializeField]
    [SyncVar(hook = nameof(OnAllReserveBulletChanged))]
    private float _allReserveBulletCount = 0;      // 备用子弹

    [Header("=== 枪械组件配置 ===")]
    [Header("Timeline动画")]
    public PlayableDirector timelineDirector_Reload; // 换弹动画
    public PlayableDirector timelineDirector_Shoot;  // 单次射击动画

    [Header("枪械配置文件")]
    public GunInfo gunInfo; // 枪械属性配置

    [Header("射击特效")]
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

    // 缓存NetworkIdentity
    private NetworkIdentity _netIdentity;

    [HideInInspector]
    public Vector3 originalWorldScale; // 存储枪械原始世界缩放

    [Header("当前拾取玩家")]
    [SyncVar]
    public Player ownerPlayer;//全局的变量
    [SyncVar(hook = nameof(OnIsInPlayerHandChanged))]
    public bool isInPlayerHand = false; // 是否被玩家拾取

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
        if (!isServer) { Debug.LogError($"[服务器] CmdStartReload非服务器环境！"); return; }
        bool canReloadServer = IsCanReload();
        if (!canReloadServer) return;
        IsInReload = true;
    }

    [Command(requiresAuthority = true)]
    public void CmdFinishReloadLogic()
    {
        if (!isServer) { Debug.LogError($"[服务器] CmdFinishReloadLogic非服务器环境！"); return; }
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

    #region ClientRpc
    [ClientRpc]
    private void RpcPlaySingleShootVFX() => PlaySingleShootVFX();

    [ClientRpc]
    private void RpcSpawnHitEffect(Vector2 hitPos, Vector2 hitNormal) => Debug.Log("播放打击特效");

    /// <summary>
    /// 同步所有客户端绘制子弹小线段
    /// </summary>
    [ClientRpc]
    private void RpcDrawBulletSegment(Vector2 startPos, Vector2 targetPos, Vector2 shootDir)
    {
        if (!isDebug) return;

        // 创建子弹小线段的临时对象
        GameObject bulletObj = new GameObject($"BulletSegment_{System.Guid.NewGuid().ToString().Substring(0, 8)}");
        bulletObj.transform.SetParent(null);
        bulletObj.transform.position = Vector3.zero;

        LineRenderer lr = bulletObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = bulletColor;
        lr.endColor = bulletColor;
        lr.startWidth = bulletLineWidth;
        lr.endWidth = bulletLineWidth; // 两端等宽，不再拖尾
        lr.positionCount = 2;          // 两个顶点形成单根小线段
        lr.sortingOrder = 100;         // 置顶显示
        lr.enabled = true;

        // 添加小线段飞行脚本，传入所有参数
        BulletSegmentFly fly = bulletObj.AddComponent<BulletSegmentFly>();
        fly.Init(lr, startPos, targetPos, shootDir, bulletSegmentLength, bulletFlySpeed, bulletShowDuration);

        // 自动销毁子弹对象（飞行+淡出总时长）
        float totalDuration = Vector2.Distance(startPos, targetPos) / bulletFlySpeed + bulletShowDuration;
        Destroy(bulletObj, totalDuration);
    }
    #endregion

    #region 客户端视觉特效
    public void PlaySingleShootVFX()
    {
        SpawnCartridgeCase();
        ApplyRecoil();

        // ?? 调用全局烟雾管理器（仅传入射击点和枪械配置，无其他依赖）
        // 烟雾方向由firePoint.transform.right（红色轴）决定，与玩家朝向无关
        MuzzleSmokeManager.Instance?.PlayMuzzleSmoke(firePoint, gunInfo);
    }

    private void SpawnCartridgeCase()
    {
        if (cartridgeCasePrefab == null) return;
        if (cartridgeEjectPoint == null)
        { Debug.LogError($"[客户端] 抛壳点未赋值！"); return; }

        GameObject cartridgeObj = PoolManage.Instance?.GetObj(cartridgeCasePrefab);
        if (cartridgeObj == null)
        { Debug.LogError($"[客户端] 对象池获取弹壳失败！"); return; }

        cartridgeObj.transform.position = cartridgeEjectPoint.position;
        cartridgeObj.transform.rotation = cartridgeEjectPoint.rotation;
        cartridgeObj.transform.localScale = cartridgeFixedScale;

        Rigidbody2D rb2D = cartridgeObj.GetComponent<Rigidbody2D>();
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
    public void TriggerSingleShoot()
    {
        if (!IsCanShoot()) return;
        CmdStartShoot();
    }

    public void TriggerReload() => CmdStartReload();
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
}

/// <summary>
/// 让小线段整体向目标点飞行，始终朝向飞行方向
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

    /// <summary>
    /// 初始化子弹飞行参数
    /// </summary>
    public void Init(LineRenderer lr, Vector2 startPos, Vector2 targetPos, Vector2 shootDir,
        float segmentLength, float flySpeed, float fadeDuration)
    {
        _lr = lr;
        _startPos = startPos;
        _targetPos = targetPos;
        _shootDir = shootDir.normalized; // 归一化方向
        _segmentLength = segmentLength;
        _flySpeed = flySpeed;
        _fadeDuration = fadeDuration;

        _elapsedTime = 0f;
        _isReachTarget = false;
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
    /// 效果：小线段整体移动，朝向不变
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
}