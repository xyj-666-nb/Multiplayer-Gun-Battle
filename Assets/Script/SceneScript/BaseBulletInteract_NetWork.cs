using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseBulletInteract_NetWork : NetworkBehaviour
{
    [Header("配置")]
    public float InitHealthValue = 100;

    // 完全公开，让外面直接改
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float CurrentHealthValue = 100;

    [SyncVar]
    public bool IsTrigger = false;

    public CanvasGroup HpCanvasGroup;
    public Image HpFillImage;
    public float ShowTime = 3;//默认存在时间是3秒
    private float CurrentCoolTime = 3;
    private bool IsShow = false;
    private int CoolDownTaskID = -1;
    private int CoolUpTaskID = -1;
    private Sequence HpCanvasGroupSequence;

    private void Awake()
    {
        this.gameObject.SetActive(true);
    }

    // 只做一件事：血量变化了通知子类
    private void OnHealthChanged(float oldVal, float newVal)
    {
        HealthChangeEffect(newVal);

        //更新当前的血条
        IsShow = true;
        CurrentCoolTime = ShowTime;//重置
        //打开倒计时
        if (CoolDownTaskID != -1)
            CountDownManager.Instance.StopTimer(CoolDownTaskID);

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(HpCanvasGroup, ref HpCanvasGroupSequence, true, () => { });

        //开启新的计时器
        CoolDownTaskID = CountDownManager.Instance.CreateTimer(false, (int)(ShowTime * 1000), () => {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(HpCanvasGroup, ref HpCanvasGroupSequence, false, () => { });
        });

        if (CoolUpTaskID != -1)
            SimpleAnimatorTool.Instance.StopFloatLerpById(CoolUpTaskID);

        CoolUpTaskID = SimpleAnimatorTool.Instance.StartFloatLerp(HpFillImage.fillAmount, CurrentHealthValue / InitHealthValue, 0.5f, (v) => {
            HpFillImage.fillAmount = v;//不断设置当前的数值
        });


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

    [Server]
    public void CmdHeal()
    {
        CurrentHealthValue = InitHealthValue;//设置满血量
    }

    // 子类实现
    public abstract void HealthChangeEffect(float Health);
    public abstract void EffectTrigger();
    public abstract void ResetObj();
    public abstract void Init();
}