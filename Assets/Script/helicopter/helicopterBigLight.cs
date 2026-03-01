using UnityEngine;
using DG.Tweening; // 必须导入DOTween命名空间

public class helicopterBigLight : MonoBehaviour
{
    [Header("摆动配置")]
    [Tooltip("Z轴摆动的最小角度（-180°）")]
    public float minZAngle = -180f;
    [Tooltip("Z轴摆动的最大角度（-160°）")]
    public float maxZAngle = -160f;
    [Tooltip("单次摆动的时长（秒），值越大摆动越慢")]
    public float swingDuration = 3f;
    [Tooltip("摆动的缓动效果（Linear=匀速，EaseInOutSine=更自然的缓入缓出）")]
    public Ease swingEase = Ease.InOutSine;

    private Tweener _swingTweener; // 缓存摆动动画，方便控制

    void Start()
    {
        // 初始化Z轴摆动动画
        StartZAxisSwing();
    }

    /// <summary>
    /// 启动Z轴来回摆动逻辑
    /// </summary>
    private void StartZAxisSwing()
    {
        // 停止已有动画，避免重复创建
        if (_swingTweener != null && _swingTweener.IsActive())
        {
            _swingTweener.Kill();
        }

        // 1. 获取当前物体的初始欧拉角（保留X、Y轴角度，只改Z轴）
        Vector3 startEuler = transform.rotation.eulerAngles;
        // 确保初始Z轴角度在摆动范围内
        startEuler.z = Mathf.Clamp(startEuler.z, minZAngle, maxZAngle);

        // 2. 构建无限循环的摆动动画
        _swingTweener = transform.DOLocalRotate(new Vector3(startEuler.x, startEuler.y, maxZAngle), swingDuration)
            .SetEase(swingEase) // 缓动效果，让摆动更自然
            .SetLoops(-1, LoopType.Yoyo) // 无限循环 + 往返模式（Yoyo=悠悠球，去-回）
            .SetLink(gameObject); // 绑定到物体，销毁时自动停止动画

        // 可选：如果初始角度不在minZAngle，先回到最小值再开始摆动
        // transform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, minZAngle);
    }

    /// <summary>
    /// 暂停摆动（可选扩展方法）
    /// </summary>
    public void PauseSwing()
    {
        if (_swingTweener != null && _swingTweener.IsPlaying())
        {
            _swingTweener.Pause();
        }
    }

    /// <summary>
    /// 恢复摆动（可选扩展方法）
    /// </summary>
    public void ResumeSwing()
    {
        if (_swingTweener != null && !_swingTweener.IsPlaying())
        {
            _swingTweener.Play();
        }
    }

    // 物体销毁时清理动画，避免内存泄漏
    private void OnDestroy()
    {
        if (_swingTweener != null)
        {
            _swingTweener.Kill();
            _swingTweener = null;
        }
    }
}