using DG.Tweening;
using Mirror;
using UnityEngine;
using System.Collections;

public class ReBornShield : NetworkBehaviour
{
    [Header("核心配置")]
    public Player MyMonster;
    public float ShieldTime = 4f; // 护盾总持续时间
    public float FlashingTime = 2f; // 最后闪烁的时间

    [Header("护盾视觉和物理")]
    public SpriteRenderer ShieldVisual;
    public CircleCollider2D ShieldCollider;

    [Header("动画参数")]
    public float activateScale = 1.14f;    // 激活放大大小
    public float holdScale = 1.05f;       // 维持大小
    public float endScale = 0.8f;          // 消失收缩大小

    [SyncVar(hook = nameof(OnShieldActiveChanged))]
    public bool isShieldActive = false;

    // 内部变量
    private Coroutine _shieldLifeCoroutine;
    private readonly string _tweenID = "ReBornShield"; // 动画ID，防止冲突

    #region 网络生命周期（客户端表现）
    public override void OnStartClient()
    {
        base.OnStartClient();
        MyMonster = GetComponentInParent<Player>();

        if (isShieldActive)
        {
            PlayShieldActivateAnimation();
        }
        else
        {
            ResetShieldState();
        }
    }

    // 护盾状态同步回调
    private void OnShieldActiveChanged(bool oldValue, bool newValue)
    {
        // 如果不是客户端，不执行动画（防御性代码）
        if (!isClient) return;

        if (newValue)
        {
            PlayShieldActivateAnimation();
        }
        else
        {
            PlayShieldDeactivateAnimation();
        }
    }
    #endregion

    #region 核心服务器逻辑（Host/Server专属）

    // 取消原来的 Command，重生护盾必须由服务器绝对控制
    [Server]
    public void ServerTriggerShield()
    {
        if (isShieldActive) return;

        isShieldActive = true; // 触发 SyncVar
        if (_shieldLifeCoroutine != null) StopCoroutine(_shieldLifeCoroutine);
        _shieldLifeCoroutine = StartCoroutine(ShieldLifecycle());
    }

    [Server]
    private IEnumerator ShieldLifecycle()
    {
        // 1. 等待正常维持的时间
        yield return new WaitForSeconds(ShieldTime - FlashingTime);

        // 2. 通知所有客户端：护盾快结束了，你们自己去播放闪烁动画
        RpcPlayFlashing();

        // 3. 等待闪烁的时间
        yield return new WaitForSeconds(FlashingTime);

        // 4. 关闭护盾（触发 SyncVar，让客户端播放消失动画）
        isShieldActive = false;
    }
    #endregion

    #region 动画播放 (仅在客户端执行)

    [ClientRpc]
    private void RpcPlayFlashing()
    {
        // 收到服务器通知，所有客户端自己开始闪烁
        if (ShieldVisual != null)
        {
            ShieldVisual.DOFade(0.3f, 0.2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(_tweenID);
        }
    }

    private void PlayShieldActivateAnimation()
    {
        if (ShieldVisual == null || ShieldCollider == null) return;

        KillAllShieldTweens();
        ShieldVisual.enabled = true;
        ShieldCollider.enabled = true;
        ShieldVisual.color = ColorManager.SetColorAlpha(ShieldVisual.color, 0);

        transform.DOScale(activateScale, 0.5f)
            .SetEase(Ease.OutBack)
            .SetId(_tweenID)
            .OnComplete(() =>
            {
                transform.DOScale(holdScale, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetId(_tweenID);
            });

        ShieldVisual.DOFade(0.4f, 1f).SetId(_tweenID);
    }

    private void PlayShieldDeactivateAnimation()
    {
        if (ShieldVisual == null || ShieldCollider == null) return;

        KillAllShieldTweens();
        ShieldCollider.enabled = false;

        Sequence seq = DOTween.Sequence().SetId(_tweenID);
        seq.Append(transform.DOScale(endScale, 0.6f).SetEase(Ease.InBack));
        seq.Join(ShieldVisual.DOFade(0, 0.6f));
        seq.OnComplete(ResetShieldState);
    }
    #endregion

    #region 工具方法
    private void KillAllShieldTweens()
    {
        DOTween.Kill(_tweenID);
    }

    private void ResetShieldState()
    {
        KillAllShieldTweens();

        if (ShieldVisual != null)
        {
            ShieldVisual.enabled = false;
            ShieldVisual.color = ColorManager.SetColorAlpha(ShieldVisual.color, 0);
        }
        if (ShieldCollider != null) ShieldCollider.enabled = false;

        transform.localScale = Vector3.one;
        _shieldLifeCoroutine = null;
    }

    private void OnDestroy()
    {
        KillAllShieldTweens();
    }
    #endregion
}