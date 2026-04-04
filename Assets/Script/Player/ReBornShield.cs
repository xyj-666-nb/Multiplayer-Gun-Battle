using DG.Tweening;
using Mirror;
using UnityEngine;
using System.Collections;

//重生护盾 
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
    private bool isShieldActive = false;

    // 内部变量
    private Coroutine _shieldLifeCoroutine;
    private readonly string _tweenID = "ReBornShield"; // 动画ID，防止冲突

    #region 网络生命周期
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        // 获取玩家组件
        MyMonster = GetComponentInParent<Player>();
        // 初始化状态：完全隐藏
        ResetShieldState();
    }

    // 护盾状态同步回调
    private void OnShieldActiveChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            // 激活护盾：开启显示+物理+动画
            PlayShieldActivateAnimation();
        }
        else
        {
            // 关闭护盾：播放消失动画
            PlayShieldDeactivateAnimation();
        }
    }
    #endregion

    #region 公共调用：触发护盾
    public void TriggerShield()
    {
        if (isShieldActive) 
            return; // 防止重复触发
        Debug.Log("触发护盾");
        // 修改服务器的同步变量
        CmdActivateShield();
    }
    #endregion

    #region 服务器激活护盾
    [Command]
    private void CmdActivateShield()
    {
        isShieldActive = true;
        // 服务器启动生命周期协程
        if (_shieldLifeCoroutine != null)
            StopCoroutine(_shieldLifeCoroutine);
        _shieldLifeCoroutine = StartCoroutine(ShieldLifecycle());
    }
    #endregion

    #region 护盾生命周期协程
    private IEnumerator ShieldLifecycle()
    {
        yield return new WaitForSeconds(ShieldTime - FlashingTime);

        if (ShieldVisual != null)
        {
            ShieldVisual.DOFade(0.3f, 0.2f)
                .SetLoops(-1, LoopType.Yoyo) // 呼吸循环闪烁
                .SetId(_tweenID);
        }

        // 等待闪烁结束
        yield return new WaitForSeconds(FlashingTime);

        // 护盾时间到，关闭
        CmdDeactivateShield();
    }
    #endregion

    #region 动画播放
    // 激活动画：缩放弹动 + 淡入
    private void PlayShieldActivateAnimation()
    {
        if (ShieldVisual == null || ShieldCollider == null) return;

        // 停止旧动画
        KillAllShieldTweens();
        // 启用组件
        ShieldVisual.enabled = true;
        ShieldCollider.enabled = true;
        ShieldVisual.color = ColorManager.SetColorAlpha(ShieldVisual.color, 0);

        // 第一段：弹性放大
        transform.DOScale(activateScale, 0.5f)
            .SetEase(Ease.OutBack)
            .SetId(_tweenID)
            .OnComplete(() =>
            {
                // 第二段：维持大小
                transform.DOScale(holdScale, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetId(_tweenID);
            });

        // 淡入效果
        ShieldVisual.DOFade(0.4f, 1f).SetId(_tweenID);
    }

    // 消失动画：收缩 + 淡出
    private void PlayShieldDeactivateAnimation()
    {
        if (ShieldVisual == null || ShieldCollider == null) return;

        // 停止闪烁/旧动画
        KillAllShieldTweens();
        // 关闭碰撞
        ShieldCollider.enabled = false;

        // 消失动画：收缩 + 淡出
        Sequence seq = DOTween.Sequence().SetId(_tweenID);
        seq.Append(transform.DOScale(endScale, 0.6f).SetEase(Ease.InBack));
        seq.Join(ShieldVisual.DOFade(0, 0.6f));

        // 动画结束后完全重置
        seq.OnComplete(ResetShieldState);
    }
    #endregion

    #region 工具方法
    // 停止所有护盾动画
    private void KillAllShieldTweens()
    {
        DOTween.Kill(_tweenID);
    }

    // 重置护盾到初始状态
    private void ResetShieldState()
    {
        KillAllShieldTweens();

        if (ShieldVisual != null)
        {
            ShieldVisual.enabled = false;
            ShieldVisual.color = ColorManager.SetColorAlpha(ShieldVisual.color, 0);
        }

        if (ShieldCollider != null)
        {
            ShieldCollider.enabled = false;
        }

        transform.localScale = Vector3.one;
        _shieldLifeCoroutine = null;
    }

    // 服务器关闭护盾
    [Command]
    private void CmdDeactivateShield()
    {
        isShieldActive = false;
    }
    #endregion

    // 销毁时清理动画
    private void OnDestroy()
    {
        KillAllShieldTweens();
    }
}