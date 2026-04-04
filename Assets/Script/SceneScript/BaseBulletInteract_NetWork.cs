using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseBulletInteract_NetWork : NetworkBehaviour
{
    [Header("配置")]
    public float InitHealthValue = 100;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float CurrentHealthValue = 100;

    [SyncVar]
    private bool IsTrigger = false;

    public CanvasGroup HpCanvasGroup;
    public Image HpFillImage;
    public float ShowTime = 3;
    private float CurrentCoolTime = 3;
    private bool IsShow = false;
    private int CoolDownTaskID = -1;
    private int CoolUpTaskID = -1;
    private Sequence HpCanvasGroupSequence;

    private void Awake()
    {
        this.gameObject.SetActive(true);
    }

    private void OnHealthChanged(float oldVal, float newVal)
    {
        HealthChangeEffect(newVal);

        IsShow = true;
        CurrentCoolTime = ShowTime;
        if (CoolDownTaskID != -1)
            CountDownManager.Instance.StopTimer(CoolDownTaskID);

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(HpCanvasGroup, ref HpCanvasGroupSequence, true, () => { });

        CoolDownTaskID = CountDownManager.Instance.CreateTimer(false, (int)(ShowTime * 1000), () => {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(HpCanvasGroup, ref HpCanvasGroupSequence, false, () => { });
        });

        if (CoolUpTaskID != -1)
            SimpleAnimatorTool.Instance.StopFloatLerpById(CoolUpTaskID);

        CoolUpTaskID = SimpleAnimatorTool.Instance.StartFloatLerp(HpFillImage.fillAmount, newVal / InitHealthValue, 0.5f, (v) => {
            HpFillImage.fillAmount = v;
        });

        // 服务端逻辑
        if (newVal <= 0 && !IsTrigger)
        {
            IsTrigger = true;
            EffectTrigger();
        }
    }

    // ==============================================
    // 【服务器专属】数据初始化
    // ==============================================
    [Server]
    public virtual void InitServer()
    {
        IsTrigger = false;
        HealFull(); // 服务器回血，同步所有客户端
    }

    // ==============================================
    // 【客户端专属】视觉初始化
    // ==============================================
    public abstract void InitClient();

    // ==============================================
    // 【服务器专属】数据重置
    // ==============================================
    [Server]
    public virtual void ResetServer()
    {
        IsTrigger = false;
    }

    // ==============================================
    // 【客户端专属】视觉重置
    // ==============================================
    public abstract void ResetClient();

    // 服务器扣血
    [Server]
    public void TakeDamage(float damage)
    {
        Debug.Log("成功扣血");
        CurrentHealthValue = Mathf.Max(0, CurrentHealthValue - damage);
    }

    // 服务器满血
    [Server]
    public void HealFull()
    {
        CurrentHealthValue = InitHealthValue;
    }

    public int MapInIndex;//地图索引

    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentHealthValue = InitHealthValue;
        IsTrigger = false;
       PlayerRespawnManager.Instance?.InitInteractObj(netIdentity, MapInIndex);
    }

    // 子类实现
    public abstract void HealthChangeEffect(float Health);
    public abstract void EffectTrigger();
}