using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class Map2StartAnimaCG : MonoBehaviour
{
    public static Map2StartAnimaCG Instance;
    public PlayableDirector TimeLine;//开始动画

    public CinemachineVirtualCamera AnimaVC;//动画虚拟相机

    [Header("震动配置（仅作用于 AnimaVC）")]
    [Tooltip("震动频率（控制震动快慢，1-5为宜）")]
    public float shakeFrequency = 2.5f;
    [Tooltip("震动平滑过渡速度（启停的顺滑度）")]
    public float shakeSmoothSpeed = 5f;
    [Tooltip("普通震动持续时间（秒）")]
    public float shakeDuration = 1f;
    [Tooltip("枪声震动持续时间（秒）")]
    public float gunShakeDuration = 6f; // 新增：枪声专属时长

    [Header("震动强度预设")]
    [Tooltip("剧烈震动强度（0-2为宜，建议比正常大）")]
    public float bigShakeAmplitude = 0.8f;
    [Tooltip("正常震动强度（0-2为宜）")]
    public float normalShakeAmplitude = 0.6f;
    [Tooltip("枪声震动强度（0-2为宜）")]
    public float GunShakeAmplitude = 0.1f;

    // 内部震动状态变量
    private CinemachineBasicMultiChannelPerlin _animaNoise;
    private float _targetAmplitude;
    private Coroutine _autoStopCoroutine;

    private void Awake()
    {
        Instance = this;

        if (AnimaVC != null)
        {
            _animaNoise = AnimaVC.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (_animaNoise == null)
            {
                _animaNoise = AnimaVC.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }

            _animaNoise.m_AmplitudeGain = 0f;
            _animaNoise.m_FrequencyGain = shakeFrequency;
            _targetAmplitude = 0f;
        }
        else
        {
            Debug.LogError("请为 Map2StartAnimaCG 赋值 AnimaVC！");
        }
    }

    void Update()
    {
        if (_animaNoise != null && Mathf.Abs(_animaNoise.m_AmplitudeGain - _targetAmplitude) > 0.01f)
        {
            _animaNoise.m_AmplitudeGain = Mathf.Lerp(
                _animaNoise.m_AmplitudeGain,
                _targetAmplitude,
                Time.deltaTime * shakeSmoothSpeed
            );
        }
    }

    public void SetExit()
    {
        AnimaVC.Priority = -10;
    }

    /// <summary>
    /// 启动剧烈震动 (1秒后自动停止)
    /// </summary>
    public void StartShack_Big()
    {
        StartShakeInternal(bigShakeAmplitude);
        StartAutoStopTimer(shakeDuration); // 传入1秒
        Debug.Log("AnimaVC: 剧烈震动启动 (1秒)");
    }

    /// <summary>
    /// 启动正常震动 (1秒后自动停止)
    /// </summary>
    public void StartShack_Normal()
    {
        StartShakeInternal(normalShakeAmplitude);
        StartAutoStopTimer(shakeDuration); // 传入1秒
        Debug.Log("AnimaVC: 正常震动启动 (1秒)");
    }

    /// <summary>
    /// 启动枪声震动 (6秒后自动停止)
    /// </summary>
    public void StartShack_GunShake()
    {
        StartShakeInternal(GunShakeAmplitude); // 修复：这里改为使用枪声强度
        StartAutoStopTimer(gunShakeDuration); // 传入6秒
        Debug.Log("AnimaVC: 枪声震动启动 (6秒)");
    }

    /// <summary>
    /// 停止震动
    /// </summary>
    public void StopShack()
    {
        if (_animaNoise == null)
            return;

        // 如果手动停止，也把自动计时的协程停掉
        if (_autoStopCoroutine != null)
        {
            StopCoroutine(_autoStopCoroutine);
            _autoStopCoroutine = null;
        }

        _targetAmplitude = 0f;
        Debug.Log("AnimaVC: 震动停止");
    }

    /// <summary>
    /// 强制立即停止震动
    /// </summary>
    public void StopShackImmediate()
    {
        if (_animaNoise != null)
        {
            _animaNoise.m_AmplitudeGain = 0f;
            _targetAmplitude = 0f;

            if (_autoStopCoroutine != null)
            {
                StopCoroutine(_autoStopCoroutine);
                _autoStopCoroutine = null;
            }
        }
    }

    private void StartShakeInternal(float amplitude)
    {
        if (AnimaVC == null || _animaNoise == null)
        {
            Debug.LogWarning("AnimaVC 未初始化，无法震动！");
            return;
        }

        _targetAmplitude = amplitude;
        _animaNoise.m_FrequencyGain = shakeFrequency;
    }

    /// <summary>
    /// 启动协程（重载：支持传入具体时间）
    /// </summary>
    private void StartAutoStopTimer(float duration)
    {
        // 如果之前有协程在跑，先停掉旧的
        if (_autoStopCoroutine != null)
        {
            StopCoroutine(_autoStopCoroutine);
        }

        // 开启新的协程，并把时间传进去
        _autoStopCoroutine = StartCoroutine(AutoStopCoroutine(duration));
    }

    private System.Collections.IEnumerator AutoStopCoroutine(float duration)
    {
        // 等待传入的指定时间
        yield return new WaitForSeconds(duration);

        // 时间到，自动停止震动
        StopShack();
    }
}