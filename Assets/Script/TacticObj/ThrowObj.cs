using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class ThrowObj : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnChangeState))]
    public bool IsTackOut;

    [SyncVar(hook = nameof(OnIsThrownChanged))]
    public bool IsThrown = false;

    private Rigidbody2D _rb;

    [SyncVar]
    public playerHandControl HandControl;
    public CharacterStats MyMonster;


    [Header("通用配置")]
    public float TriggerTime = 2; // 触发时间（手雷引信/烟雾弹生效延迟）

    [Header("烟雾配置")]
    public float Duration = 8;

    [Header("手雷爆炸配置")]
    [Tooltip("爆炸影响半径")]
    public float explosionRadius = 4f;
    [Tooltip("爆炸中心点最大伤害")]
    public int maxDamage = 100;
    [Tooltip("爆炸中心点最大击退力")]
    public float maxKnockbackForce = 10f;
    [Tooltip("爆炸边缘最小击退力")]
    public float minKnockbackForce = 3f;

    private double _serverStartTime;
    private bool _isDestroyed = false;

    [Header("预制体引用")]
    public GameObject ExplosionPrefab;
    public TacticType tacticType;

    #region 网络同步回调
    private void OnChangeState(bool OldValue, bool NewValue)
    {
        if (OldValue == NewValue || IsInAnimation) return;

        if (NewValue)
        {
            IsInAnimation = true;
            if (ThrowObjTimeLine != null) ThrowObjTimeLine.Play();
            else IsInAnimation = false;
        }
        else
        {
            PlayAnimaRecycle();
        }
    }

    private void OnIsThrownChanged(bool oldValue, bool newValue)
    {
        if (_rb == null) return;

        // 【修复2】删除这里的 GetComponentInParent，因为扔出去后已经没有父物体了

        if (oldValue == false && newValue == true)
        {
            if (isServer)
            {
                _serverStartTime = NetworkTime.time;
                if (tacticType == TacticType.Grenade)
                {
                    StartCoroutine(ServerGrenadeExplodeCoroutine());
                }
                else if (tacticType == TacticType.Smoke)
                {
                    StartCoroutine(ServerSmokeEffectCoroutine());
                }
            }

            _rb.isKinematic = false;
            _rb.simulated = true;
        }

        if (oldValue == true && newValue == false)
        {
            _rb.isKinematic = true;
            _rb.simulated = false;
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }
    #endregion

    public bool IsInAnimation = false;
    public PlayableDirector ThrowObjTimeLine;

    #region 生命周期
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.simulated = false;
            _rb.isKinematic = true;
        }

        if (ExplosionPrefab == null && tacticType == TacticType.Grenade)
        {
            Debug.LogError("[ThrowObj] 手雷的 ExplosionPrefab 未赋值！", this);
        }
        CountDownManager.Instance.CreateTimer(false, 100, () => { MyMonster = HandControl.ownerPlayer.myStats; });
    }

    private void Update()
    {
        if (isServer && IsThrown && !_isDestroyed)
        {
            if (tacticType == TacticType.Smoke)
                ServerUpdateSmoke();
        }
    }

    private void OnDestroy()
    {
        _isDestroyed = true;
        if (ThrowObjTimeLine != null) ThrowObjTimeLine.Stop();
        StopAllCoroutines();
    }
    #endregion

    #region 服务器逻辑
    [Server]
    private void ServerUpdateSmoke()
    {
        double elapsedTime = NetworkTime.time - _serverStartTime;

        if (elapsedTime > TriggerTime && elapsedTime < (TriggerTime + Duration))
        {
            Vector2 serverPos = transform.position;
            float t = (float)elapsedTime;
            float sizeMultiplier = Mathf.Clamp01(-t * 0.25f + 6.0f);
            RpcSpawnSmoke(serverPos, sizeMultiplier);
        }
    }

    [Server]
    private IEnumerator ServerSmokeEffectCoroutine()
    {
        if (tacticType != TacticType.Smoke || _isDestroyed) yield break;

        float totalLifeTime = TriggerTime + Duration;
        yield return new WaitForSeconds(totalLifeTime);

        if (_isDestroyed || !IsThrown) yield break;

        Debug.Log($"[烟雾弹] 效果结束，销毁对象：{gameObject.name}", this);
        RequestSelfDestruction();
    }

    [Server]
    private IEnumerator ServerGrenadeExplodeCoroutine()
    {
        if (tacticType != TacticType.Grenade || _isDestroyed) yield break;

        yield return new WaitForSeconds(TriggerTime);

        if (_isDestroyed || !IsThrown) yield break;

        ServerProcessGrenadeExplosion();
        RpcTriggerExplosion();

        yield return new WaitForSeconds(0.5f);
        if (!_isDestroyed)
        {
            RequestSelfDestruction();
        }
    }

    /// <summary>
    /// 服务器端处理手雷伤害判定
    /// </summary>
    [Server]
    private void ServerProcessGrenadeExplosion()
    {
        Vector2 explosionPos = transform.position;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(explosionPos, explosionRadius, LayerMask.GetMask("Player"));

        // 2. 获取攻击者数据
        CharacterStats attackerStats = null;
          attackerStats = HandControl.ownerPlayer.myStats;

        foreach (Collider2D col in hitColliders)
        {
            Vector2 closestPointOnPlayer = col.ClosestPoint(explosionPos);
            Vector2 dirToPlayer = (closestPointOnPlayer - explosionPos).normalized;
            Vector2 rayOrigin = explosionPos + dirToPlayer * 0.1f;

            // 3. 遮挡检测 (请确保这里的 "Ground" 与你项目中的层级名称一致)
            RaycastHit2D hit = Physics2D.Raycast(
                rayOrigin,
                dirToPlayer,
                explosionRadius,
                LayerMask.GetMask("Ground")
            );

            float distanceToTarget = Vector2.Distance(rayOrigin, closestPointOnPlayer);
            bool isBlocked = hit && hit.distance < distanceToTarget;

            if (!isBlocked)
            {
                // 4. 计算实际距离
                float distance = Vector2.Distance(explosionPos, closestPointOnPlayer);
                // 防止除以0
                if (distance < 0.1f) distance = 0.1f;

                // 5. 计算伤害
                int damage = Mathf.RoundToInt(maxDamage * (1 - distance / explosionRadius));
                damage = Mathf.Max(damage, 10);

                // 6. 计算击退力 (Lerp：距离越近力越大)
                float forceMultiplier = 1 - (distance / explosionRadius);
                float currentKnockbackAmount = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, forceMultiplier);
                Vector2 finalKnockbackForce = dirToPlayer * currentKnockbackAmount;

                // 7. 应用伤害
                Player targetPlayer = col.GetComponent<Player>();
                if (targetPlayer != null && targetPlayer.myStats != null)
                {
                    Debug.Log($"[Server] 手雷命中 {targetPlayer.name}，伤害 {damage}，击退 {finalKnockbackForce}");
                    targetPlayer.myStats.ServerApplyGrenadeDamage(damage, explosionPos, finalKnockbackForce, attackerStats);
                }
            }
        }
    }
    #endregion

    #region 客户端 RPC
    [ClientRpc]
    private void RpcSpawnSmoke(Vector2 worldPos, float sizeMultiplier)
    {
        if (FluidController.Instance == null || _isDestroyed) return;

        FluidController.Instance.QueueDrawAtPoint(
            worldPos,
            new Color(1.0f, 1.0f, 1.0f, 1.2f),
            Vector2.zero,
            0.3f * sizeMultiplier,
            0.4f * sizeMultiplier,
            FluidController.VelocityType.Explore
        );
    }

    [ClientRpc]
    private void RpcTriggerExplosion()
    {
        if (_isDestroyed || ExplosionPrefab == null)
            return;

        Debug.Log($"[爆炸特效] 客户端触发：{gameObject.name}");
        GameObject explosion = Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
        explosion.SetActive(true);
        Destroy(explosion, 2f);
    }
    #endregion

    #region 销毁逻辑
    [Command(requiresAuthority = true)]
    private void CmdDestroySelf()
    {
        if (_isDestroyed || !isServer) return;
        ServerDestroySelf();
    }

    [Server]
    private void ServerDestroySelf()
    {
        if (_isDestroyed) return;
        DestroySelfImmediate();
    }

    [Server]
    private void DestroySelfImmediate()
    {
        if (_isDestroyed) return;
        if (ThrowObjTimeLine != null) ThrowObjTimeLine.Stop();
        _isDestroyed = true;
        if (gameObject != null && NetworkServer.active)
        {
            NetworkServer.Destroy(gameObject);
            Debug.Log($"[销毁] 投掷物已销毁：{gameObject.name} 类型：{tacticType}", this);
        }
    }

    public void RequestSelfDestruction()
    {
        if (_isDestroyed) return;
        if (isOwned && !isServer) CmdDestroySelf();
        else if (isServer) ServerDestroySelf();
    }
    #endregion

    #region 动画回调
    public void TackOutAnimaComplete()
    {
        IsInAnimation = false;
        if (ThrowObjTimeLine != null) ThrowObjTimeLine.Pause();
    }

    public void PlayAnimaRecycle()
    {
        if (IsInAnimation) return;
        IsInAnimation = true;
        if (ThrowObjTimeLine != null) ThrowObjTimeLine.Play();
        else
        {
            IsInAnimation = false;
            RecycleAnimaComplete();
        }
    }

    public void RecycleAnimaComplete()
    {
        IsInAnimation = false;
        RequestSelfDestruction();
        if (HandControl != null)
            HandControl.CurrentThrowObj = null;
    }
    #endregion

    #region 命令
    [Command(requiresAuthority = true)]
    public void CmdRecallThrow()
    {
        if (_isDestroyed || !isServer) return;
        IsThrown = false;
    }
    #endregion

    [ClientRpc]
    public void ServerLaunch(Vector2 velocity)
    {
        if (_isDestroyed) return;

        IsThrown = true;
        _serverStartTime = NetworkTime.time;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.simulated = true;
            _rb.velocity = velocity;
        }
    }
}