using Mirror;
using UnityEngine;
using UnityEngine.Playables;

public class Injection : NetworkBehaviour
{
    public PlayableDirector TimeLine_Inject; // 注射动画Timeline
    public TacticType injectionType; // 注射器类型

    [SyncVar]
    private NetworkIdentity _ownerPlayerIdentity; // 绑定所属玩家的NetworkIdentity
    public playerHandControl _playerHand;

    // 标记是否已销毁，避免重复操作
    private bool _isDestroyed = false;

    #region 生命周期 & 初始化
    private void Awake()
    {
        // 空引用防护
        if (TimeLine_Inject == null)
        {
            /* Debug.LogWarning($"[Injection] {gameObject.name} 的TimeLine_Inject未赋值！", this); */
        }
    }

    // 注射器生成时绑定所属玩家
    [Server]
    public void BindToPlayer(NetworkIdentity playerIdentity)
    {
        if (playerIdentity == null)
        {
            /* Debug.LogError("[Injection] 绑定玩家失败：playerIdentity为空", this); */
            return;
        }
        _ownerPlayerIdentity = playerIdentity;
    }

    private void OnDestroy()
    {
        _isDestroyed = true;
        if (TimeLine_Inject != null )
        {
            TimeLine_Inject.Stop();
        }
    }
    #endregion

    [Command(requiresAuthority = true)]
    private void CmdDestroySelf()
    {
        if (_isDestroyed || !isServer) return;
        ServerDestroySelf();
    }

    [Server]
    private void ServerDestroySelf()
    {
        if (_isDestroyed)
            return;

        playerHandControl ownerHand = GetOwnerHandControl();
        if (ownerHand != null)
        {
            ownerHand.ServerSetHolsterState(false);
            if (ownerHand.CurrentInjection == gameObject)
                ownerHand.CurrentInjection = null;
        }

        DestroySelfImmediate();
    }

    [Server]
    private playerHandControl GetOwnerHandControl()
    {
        if (_ownerPlayerIdentity == null)
            return _playerHand;

        Player ownerPlayer = _ownerPlayerIdentity.GetComponent<Player>();
        if (ownerPlayer != null && ownerPlayer.MyHandControl != null)
            return ownerPlayer.MyHandControl;

        return _ownerPlayerIdentity.GetComponent<playerHandControl>() ??
               _ownerPlayerIdentity.GetComponentInChildren<playerHandControl>() ??
               _playerHand;
    }

    [ClientRpc]//都进行执行
    public void RpcTriggerTHolsterStateFalse()
    {
        if (_playerHand != null && _playerHand.isOwned)
            _playerHand.SetHolsterState(false);
    }

    [Server]
    private void DestroySelfImmediate()
    {
        if (_isDestroyed) return;

        // 停止动画
        if (TimeLine_Inject != null)
        {
            TimeLine_Inject.Stop();
        }

        _isDestroyed = true;

        // 安全销毁网络对象
        if (gameObject != null && NetworkServer.active)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    //Timeline动画事件回调：触发注射效果
    public void TriggerEffect()
    {
        TriggerInjectionEffectToOwner();//触发效果
    }

    [Server]
    private void TriggerInjectionEffectToOwner()
    {
        if (_ownerPlayerIdentity == null)
        {
            /* Debug.LogError("[Injection] 触发效果失败：未绑定所属玩家", this); */
            return;
        }

        // 获取所属玩家的playerStats组件
        var playerStats = _ownerPlayerIdentity.GetComponent<playerStats>();
        if (playerStats == null)
        {
            /* Debug.LogError($"[Injection] 所属玩家 {_ownerPlayerIdentity.name} 缺少playerStats组件", this); */
            return;
        }

        // 调用ClientRpc定向触发效果
        playerStats.ServerApplyInjectionEffect(injectionType);
        playerStats.TriggerEffect_Injection(injectionType);
    }

    public void Destroy()
    {
        RequestSelfDestruction();
    }


    // 【Timeline回调专用】请求销毁自身
    public void RequestSelfDestruction()
    {
        if (_isDestroyed) return;

        if (isOwned && !isServer)
        {
            CmdDestroySelf();
        }
        else if (isServer)
        {
            ServerDestroySelf();
        }
    }

    [Command(requiresAuthority = true)]
    public void CmdTriggerInjection()
    {
        ServerTriggerInjection();
    }

    [Server]
    public void ServerTriggerInjection()
    {
        if (_isDestroyed || !isServer)
            return;
        RpcTriggerInjection();
    }

    // 所有客户端播放注射动画
    [ClientRpc]
    public void RpcTriggerInjection()
    {
        if (_isDestroyed) 
            return;

        if (TimeLine_Inject != null)
        {
            TimeLine_Inject.Play();
        }
        else if (TimeLine_Inject == null && !_isDestroyed)
        {
            /* Debug.LogWarning("[Injection] TimeLine_Inject为空，无法播放注射动画", this); */
        }
    }
}
