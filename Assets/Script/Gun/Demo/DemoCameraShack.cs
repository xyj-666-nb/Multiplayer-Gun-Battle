using UnityEngine;
using System.Collections;

public class DemoCameraShack : MonoBehaviour
{
    // 单例实例，方便全局调用
    public static DemoCameraShack instance;

    [Header("震动设置")]
    [Tooltip("相机原始本地位置")]
    private Vector3 _originalLocalPos;
    [Tooltip("防止重复触发震动")]
    private bool _isShaking = false;

    private void Awake()
    {
        // 单例初始化
        if (instance == null)
            instance = this;

        // 记录相机初始本地位置
        _originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// 外部调用的相机震动方法
    /// </summary>
    /// <param name="shakeDuration">震动持续时间</param>
    /// <param name="shakeMagnitude">震动强度</param>
    public void ShakeCamera(float shakeDuration, float shakeMagnitude)
    {
        // 防止震动过程中重复调用，导致异常
        if (!_isShaking)
        {
            StartCoroutine(StartCameraShake(shakeDuration, shakeMagnitude));
        }
    }

    /// <summary>
    /// 相机震动协程（每帧执行圆内随机偏移）
    /// </summary>
    private IEnumerator StartCameraShake(float duration, float magnitude)
    {
        _isShaking = true;
        float elapsedTime = 0f;

        // 在指定时间内持续震动
        while (elapsedTime < duration)
        {
            // 生成单位圆内的随机2D点
            Vector2 randomCirclePoint = Random.insideUnitCircle;

            // 转换为3D偏移量
            Vector3 shakeOffset = new Vector3(randomCirclePoint.x, randomCirclePoint.y, 0) * magnitude;

            // 应用震动偏移到相机位置
            transform.localPosition = _originalLocalPos + shakeOffset;

            // 累计时间，等待下一帧
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _originalLocalPos;
        _isShaking = false;
    }
}