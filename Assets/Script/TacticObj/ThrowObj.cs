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
    public playerHandControl playerHandControl;

    [Header("烟雾配置")]
    public float TriggerTime = 2;
    public float Duration = 8;

    private double _serverStartTime;
    private bool _isDestroyed = false;

    [Header("手雷爆炸物")]
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

        if (oldValue == false && newValue == true)
        {
            // 只有服务器记录时间 + 启动对应协程
            if (isServer)
            {
                _serverStartTime = NetworkTime.time;
                // 根据投掷物类型启动不同的协程
                if (tacticType == TacticType.Grenade)
                {
                    StartCoroutine(ServerGrenadeExplodeCoroutine()); // 手雷爆炸协程
                }
                else if (tacticType == TacticType.Smoke)
                {
                    StartCoroutine(ServerSmokeEffectCoroutine()); // 烟雾弹专属协程
                }
            }

            // 所有客户端开启物理
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
        // 初始强制关闭
        if (_rb != null)
        {
            _rb.simulated = false;
            _rb.isKinematic = true;
        }

        // 空值检查：避免爆炸预制体未赋值
        if (ExplosionPrefab == null && tacticType == TacticType.Grenade)
        {
            Debug.LogError("[ThrowObj] 手雷的 ExplosionPrefab 未赋值！", this);
        }
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
        StopAllCoroutines(); // 停止所有协程
    }
    #endregion

    #region 服务器逻辑
    [Server]
    private void ServerUpdateSmoke()
    {
        double elapsedTime = NetworkTime.time - _serverStartTime;

        // 仅在烟雾生效期间绘制烟雾
        if (elapsedTime > TriggerTime && elapsedTime < (TriggerTime + Duration))
        {
            Vector2 serverPos = transform.position;
            float t = (float)elapsedTime;
            float sizeMultiplier = Mathf.Clamp01(-t * 0.25f + 6.0f);
            RpcSpawnSmoke(serverPos, sizeMultiplier);
        }
    }

    /// <summary>
    /// 烟雾弹专属协程：效果结束后自动销毁
    /// </summary>
    [Server]
    private IEnumerator ServerSmokeEffectCoroutine()
    {
        if (tacticType != TacticType.Smoke || _isDestroyed) yield break;

        // 等待烟雾效果完全结束（触发时间 + 持续时间）
        float totalLifeTime = TriggerTime + Duration;
        yield return new WaitForSeconds(totalLifeTime);

        if (_isDestroyed || !IsThrown) yield break;

        Debug.Log($"[烟雾弹] 效果结束，销毁对象：{gameObject.name}", this);
        // 停止绘制烟雾并销毁自身
        RequestSelfDestruction();
    }

    [Server]
    private IEnumerator ServerGrenadeExplodeCoroutine()
    {
        if (tacticType != TacticType.Grenade || _isDestroyed) yield break;

        float explodeWaitTime = TriggerTime; // 修正：手雷爆炸等待时间应为TriggerTime（触发时间）
        yield return new WaitForSeconds(explodeWaitTime);

        if (_isDestroyed || !IsThrown) yield break;
        RpcTriggerExplosion();

        // 爆炸后延迟销毁
        yield return new WaitForSeconds(0.5f);
        if (!_isDestroyed)
        {
            RequestSelfDestruction();
        }
    }

    [ClientRpc]
    private void RpcTriggerExplosion()
    {
        if (_isDestroyed || ExplosionPrefab == null)
            return;

        Debug.Log($"[爆炸触发] 客户端：{isClientOnly}，对象：{gameObject.name}");
        // 修正：实例化爆炸预制体而不是直接激活原有对象
        GameObject explosion = Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
        explosion.SetActive(true);
        // 自动销毁爆炸特效
        Destroy(explosion, 2f);
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
        if (playerHandControl != null) playerHandControl.CurrentThrowObj = null;
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

