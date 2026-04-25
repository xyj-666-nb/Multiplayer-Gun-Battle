using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 独立的皮肤演示发射器
/// 纯本地、无网络、可在任意地方调用，用于演示子弹/火光皮肤效果
/// </summary>
public static class SkinDemoShooter
{
    // 内部缓存的模板对象
    private static GameObject _bulletSegmentTemplate;
    private static readonly string _templateName = "SkinDemo_BulletTemplate";

    // 默认参数
    private static readonly Color DefaultBulletColor = new Color(0.83f, 0.68f, 0.22f);
    private static readonly float DefaultBulletLength = 0.2f;
    private static readonly float DefaultBulletWidth = 0.03f;
    private static readonly float DefaultBulletSpeed = 80f;
    private static readonly float DefaultBulletDuration = 0.2f;
    private static readonly Color DefaultCartridgeColor = new Color(0.83f, 0.68f, 0.22f);
    private static readonly float DefaultCartridgeSize = 0.2f;

    // 默认打击墙特效
    private static GameObject _defaultHitWallEffect;

    #region 一键演示皮肤效果
    /// <summary>
    /// 一键演示皮肤效果
    /// </summary>
    /// <param name="hitEffectPrefab">自定义打击特效</param>
    public static void PlayDemo(
        Transform firePoint,
        BulletVisualConfig bulletConfig = null,
        MuzzleFlash muzzleFlash = null,
        GameObject cartridgePrefab = null,
        Transform cartridgeEjectPoint = null,
        GameObject hitEffectPrefab = null,
        float range = 50f)
    {
        if (firePoint == null)
        {
            /* Debug.LogError("[SkinDemoShooter] 发射点firePoint不能为空！"); */
            return;
        }

        PlayMuzzleFlash(muzzleFlash);
        SpawnDemoBullet(firePoint, bulletConfig, hitEffectPrefab, range);
        SpawnDemoCartridge(cartridgePrefab, cartridgeEjectPoint, bulletConfig);
    }
    #endregion

    #region 超简洁一键全套发射
    public static void FireAll(
        Transform firePoint,
        BulletVisualConfig bulletConfig = null,
        MuzzleFlash muzzleFlash = null,
        GameObject hitEffectPrefab = null)
    {
        PlayDemo(
            firePoint: firePoint,
            bulletConfig: bulletConfig,
            muzzleFlash: muzzleFlash,
            cartridgePrefab: null,
            cartridgeEjectPoint: firePoint,
            hitEffectPrefab: hitEffectPrefab,
            range: 50f
        );
    }

    public static void FireAllFull(
        Transform firePoint,
        BulletVisualConfig bulletConfig,
        MuzzleFlash muzzleFlash,
        GameObject cartridgePrefab,
        Transform cartridgeEjectPoint,
        GameObject hitEffectPrefab = null,
        float range = 50f)
    {
        PlayDemo(
            firePoint,
            bulletConfig,
            muzzleFlash,
            cartridgePrefab,
            cartridgeEjectPoint,
            hitEffectPrefab,
            range
        );
    }
    #endregion

    #region 单独演示各个部分
    public static void PlayBulletOnly(Transform firePoint, BulletVisualConfig config = null, GameObject hitEffectPrefab = null, float range = 50f)
    {
        if (firePoint == null) return;
        SpawnDemoBullet(firePoint, config, hitEffectPrefab, range);
    }

    public static void PlayFlashOnly(MuzzleFlash muzzleFlash)
    {
        PlayMuzzleFlash(muzzleFlash);
    }

    public static void PlayCartridgeOnly(GameObject cartridgePrefab, Transform ejectPoint, BulletVisualConfig config = null)
    {
        if (cartridgePrefab == null || ejectPoint == null) return;
        SpawnDemoCartridge(cartridgePrefab, ejectPoint, config);
    }
    #endregion

    #region 内部实现：子弹
    private static void SpawnDemoBullet(Transform firePoint, BulletVisualConfig config, GameObject hitEffectPrefab, float range)
    {
        // 自动获取默认打击特效
        if (_defaultHitWallEffect == null)
        {
            BaseGun gun = Object.FindObjectOfType<BaseGun>();
            if (gun != null) _defaultHitWallEffect = gun.hitwalleffect;
        }

        GameObject template = GetOrCreateBulletTemplate(config);
        if (template == null) return;

        GameObject bulletObj = PoolManage.Instance.GetObj(template);
        if (bulletObj == null)
        {
            bulletObj = Object.Instantiate(template);
            bulletObj.name = template.name;
        }

        bulletObj.SetActive(true);
        bulletObj.transform.SetParent(null);

        Vector2 startPos = firePoint.position;
        Vector2 shootDir = firePoint.right;

        // 射线检测 Ground / 可交互物体
        LayerMask groundLayer = LayerMask.GetMask("Ground", "BulletInteractObj");
        RaycastHit2D hit = Physics2D.Raycast(startPos, shootDir, range, groundLayer);

        Vector2 targetPos = startPos + shootDir * range;
        Vector2 hitNormal = Vector2.up;
        bool hasHitGround = false;

        // 检测到碰撞
        if (hit)
        {
            targetPos = hit.point;
            hitNormal = hit.normal;

            // 仅标记击中Ground层
            if (hit.collider.CompareTag("Ground"))
            {
                hasHitGround = true;
            }
        }

        LineRenderer lr = bulletObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = bulletObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        // 应用配置
        Color bulletColor = config != null ? config.bulletColor : DefaultBulletColor;
        float bulletWidth = config != null ? config.bulletLineWidth : DefaultBulletWidth;
        float bulletLength = config != null ? config.bulletSegmentLength : DefaultBulletLength;
        float bulletSpeed = config != null ? config.bulletFlySpeed : DefaultBulletSpeed;
        float bulletDuration = config != null ? config.bulletShowDuration : DefaultBulletDuration;

        lr.startColor = bulletColor;
        lr.endColor = bulletColor;
        lr.startWidth = bulletWidth;
        lr.endWidth = bulletWidth;
        lr.positionCount = 2;
        lr.sortingOrder = 100;
        lr.enabled = true;

        Color resetColor = lr.startColor;
        resetColor.a = 1f;
        lr.startColor = resetColor;
        lr.endColor = resetColor;

        DemoBulletFly fly = bulletObj.GetComponent<DemoBulletFly>();
        if (fly == null) fly = bulletObj.AddComponent<DemoBulletFly>();

        // 初始化飞行脚本（传递碰撞信息）
        fly.Init(
            lr,
            startPos,
            targetPos,
            shootDir,
            bulletLength,
            bulletSpeed,
            bulletDuration,
            template,
            hasHitGround,
            targetPos,
            hitNormal,
            hitEffectPrefab,
            _defaultHitWallEffect,
            () =>
            {
                PoolManage.Instance.PushObj(template, bulletObj);
            });
    }

    private static GameObject GetOrCreateBulletTemplate(BulletVisualConfig config)
    {
        if (_bulletSegmentTemplate != null) return _bulletSegmentTemplate;

        _bulletSegmentTemplate = new GameObject(_templateName);
        _bulletSegmentTemplate.SetActive(false);

        LineRenderer lr = _bulletSegmentTemplate.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));

        Color color = config != null ? config.bulletColor : DefaultBulletColor;
        float width = config != null ? config.bulletLineWidth : DefaultBulletWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 2;
        lr.sortingOrder = 100;
        lr.enabled = false;

        _bulletSegmentTemplate.AddComponent<DemoBulletFly>();
        /* Debug.Log("[SkinDemoShooter] 创建子弹演示模板"); */
        return _bulletSegmentTemplate;
    }
    #endregion

    #region 内部实现：火光
    private static void PlayMuzzleFlash(MuzzleFlash muzzleFlash)
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.PlayFlash();
        }
    }
    #endregion

    #region 内部实现：弹壳
    private static void SpawnDemoCartridge(GameObject prefab, Transform ejectPoint, BulletVisualConfig config)
    {
        if (prefab == null || ejectPoint == null) return;

        GameObject cartridgeObj = PoolManage.Instance.GetObj(prefab);
        if (cartridgeObj == null)
        {
            cartridgeObj = Object.Instantiate(prefab);
            cartridgeObj.name = prefab.name;
        }

        cartridgeObj.SetActive(true);
        cartridgeObj.transform.position = ejectPoint.position;
        cartridgeObj.transform.rotation = ejectPoint.rotation;

        SpriteRenderer sr = cartridgeObj.GetComponent<SpriteRenderer>();
        if (config != null)
        {
            if (sr != null) sr.color = config.cartridgeCaseColor;
            cartridgeObj.transform.localScale = Vector3.one * config.cartridgeCaseSize;
        }
        else
        {
            if (sr != null) sr.color = DefaultCartridgeColor;
            cartridgeObj.transform.localScale = Vector3.one * DefaultCartridgeSize;
        }

        Rigidbody2D rb = cartridgeObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            Vector2 ejectForce = (ejectPoint.right * Random.Range(1f, 3f)) + Vector3.up * Random.Range(0.5f, 2f);
            rb.AddForce(ejectForce, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-5f, 5f));
        }

        CountDownManager.Instance?.CreateTimer(false, 1000, () =>
        {
            PoolManage.Instance.PushObj(prefab, cartridgeObj);
        });
    }
    #endregion

    #region 生成打击特效
    public static void SpawnHitEffect(Vector2 hitPos, Vector2 hitNormal, GameObject customEffect, GameObject defaultEffect)
    {
        GameObject effectPrefab = customEffect != null ? customEffect : defaultEffect;
        if (effectPrefab == null)
        {
            /* Debug.LogError("[SkinDemoShooter] 打击特效预制体为空！"); */
            return;
        }

        GameObject hitEffectObj = PoolManage.Instance.GetObj(effectPrefab);
        if (hitEffectObj == null)
        {
            hitEffectObj = Object.Instantiate(effectPrefab);
            hitEffectObj.name = effectPrefab.name;
        }

        hitEffectObj.transform.position = hitPos;
        hitEffectObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, hitNormal);
        hitEffectObj.SetActive(true);

        CountDownManager.Instance.CreateTimer(false, 500, () =>
        {
            PoolManage.Instance.PushObj(effectPrefab, hitEffectObj);
        });

        // 播放命中音效
        if (Player.LocalPlayer != null)
        {
            MusicManager.Instance.PlayEffect3D_Custom(
                "Music/正式/交互/击中墙" + Random.Range(1, 4),
                0.2f, hitPos, Player.LocalPlayer.transform.position, maxDistance: 5f);
        }
    }
    #endregion

    #region 清理方法
    public static void ClearAll()
    {
        if (_bulletSegmentTemplate != null)
        {
            Object.DestroyImmediate(_bulletSegmentTemplate);
            _bulletSegmentTemplate = null;
        }
        _defaultHitWallEffect = null;
        /* Debug.Log("[SkinDemoShooter] 已清理所有缓存"); */
    }
    #endregion
}

#region 演示用子弹飞行逻辑
public class DemoBulletFly : MonoBehaviour
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
    private System.Action _onComplete;

    // 打击特效
    private GameObject _customHitEffect;
    private GameObject _defaultHitEffect;
    private bool _hasSpawnedHitEffect = false;

    // 碰撞检测参数
    private bool _hasHitGround;
    private Vector2 _hitPosition;
    private Vector2 _hitNormal;

    /// <summary>
    /// 初始化（完整参数）
    /// </summary>
    public void Init(
        LineRenderer lr, Vector2 startPos, Vector2 targetPos, Vector2 shootDir,
        float segmentLength, float flySpeed, float fadeDuration, GameObject prefab,
        bool hasHitGround, Vector2 hitPosition, Vector2 hitNormal,
        GameObject customHitEffect, GameObject defaultHitEffect, System.Action onComplete)
    {
        _elapsedTime = 0f;
        _isReachTarget = false;
        _hasSpawnedHitEffect = false;

        _lr = lr;
        _startPos = startPos;
        _targetPos = targetPos;
        _shootDir = shootDir.normalized;
        _segmentLength = segmentLength;
        _flySpeed = flySpeed;
        _fadeDuration = fadeDuration;
        _currentCenterPos = startPos;
        _prefab = prefab;
        _onComplete = onComplete;

        // 碰撞信息
        _hasHitGround = hasHitGround;
        _hitPosition = hitPosition;
        _hitNormal = hitNormal;

        // 特效配置
        _customHitEffect = customHitEffect;
        _defaultHitEffect = defaultHitEffect;

        UpdateBulletPos();
    }

    private void Update()
    {
        if (_isReachTarget)
        {
            FadeOut();
            return;
        }

        _elapsedTime += Time.deltaTime;
        float moveDistance = _flySpeed * Time.deltaTime;
        _currentCenterPos = Vector2.MoveTowards(_currentCenterPos, _targetPos, moveDistance);
        UpdateBulletPos();

        // 到达目标点
        if (Vector2.Distance(_currentCenterPos, _targetPos) < 0.01f)
        {
            _isReachTarget = true;
            _elapsedTime = 0f;

            // 核心：仅击中GROUND才生成特效
            if (_hasHitGround)
            {
                SpawnHitEffectAtTarget();
            }
        }
    }

    private void OnDisable()
    {
        _elapsedTime = 0f;
        _isReachTarget = false;
        _hasSpawnedHitEffect = false;
        _currentCenterPos = Vector2.zero;

        if (_lr != null)
        {
            Color resetColor = _lr.startColor;
            resetColor.a = 1f;
            _lr.startColor = resetColor;
            _lr.endColor = resetColor;
        }
    }

    private void UpdateBulletPos()
    {
        Vector2 pos1 = _currentCenterPos - _shootDir * (_segmentLength / 2);
        Vector2 pos2 = _currentCenterPos + _shootDir * (_segmentLength / 2);
        _lr.SetPosition(0, pos1);
        _lr.SetPosition(1, pos2);
    }

    private void FadeOut()
    {
        _elapsedTime += Time.deltaTime;
        float fadeProgress = Mathf.Clamp01(_elapsedTime / _fadeDuration);
        Color currentColor = _lr.startColor;
        currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
        _lr.startColor = currentColor;
        _lr.endColor = currentColor;

        if (fadeProgress >= 1f)
        {
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 生成打击特效（仅击中Ground调用）
    /// </summary>
    private void SpawnHitEffectAtTarget()
    {
        if (_hasSpawnedHitEffect) return;
        _hasSpawnedHitEffect = true;

        SkinDemoShooter.SpawnHitEffect(_hitPosition, _hitNormal, _customHitEffect, _defaultHitEffect);
    }
}
#endregion