using Mirror;
using UnityEngine;

public abstract class BaseBulletInteract_NetWork : NetworkBehaviour
{
    [Header("配置")]
    public float InitHealthValue = 100;

    // 完全公开，让外面直接改
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float CurrentHealthValue = 100;

    [SyncVar]
    public bool IsTrigger = false;
    private void Awake()
    {
        this.gameObject.SetActive(true);
    }

    // 只做一件事：血量变化了通知子类
    private void OnHealthChanged(float oldVal, float newVal)
    {
        HealthChangeEffect(newVal);

        // 服务端逻辑：血量归0
        if (isServer && newVal <= 0 && !IsTrigger)
        {
            IsTrigger = true;
            EffectTrigger();
        }
    }

    // 初始化
    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentHealthValue = InitHealthValue;
        IsTrigger = false;

        // 注册逻辑
        int mapIndex = transform.root.name.Contains("1") ? 1 : (transform.root.name.Contains("2") ? 2 : -1);
        if (mapIndex != -1)
            PlayerRespawnManager.Instance?.InitInteractObj(netIdentity, mapIndex);
    }

    // 子类实现
    public abstract void HealthChangeEffect(float Health);
    public abstract void EffectTrigger();
    public abstract void ResetObj();
    public abstract void Init();
}