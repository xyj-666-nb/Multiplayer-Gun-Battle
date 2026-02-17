using UnityEngine;

/// <summary>
/// 保持子物体世界缩放的【大小】固定，缩放的【正负方向】跟随父对象
/// </summary>
[DisallowMultipleComponent]
public class KeepWorldScale : MonoBehaviour
{
    [Header("目标世界缩放的绝对值（想要固定的尺寸大小）")]
    private Vector3 targetWorldScaleAbs;

    private Transform _transform;
    private Vector3 _lastParentScale; 

    private void Awake()
    {
        targetWorldScaleAbs= transform.localScale;
        _transform = transform;
        UpdateLocalScale();
        // 记录初始父物体缩放
        _lastParentScale = GetParentLossyScale();
    }

    private void Update()
    {
        // 只有父物体缩放变化时，才更新子物体本地缩放
        Vector3 currentParentScale = GetParentLossyScale();
        if (!IsScaleEqual(currentParentScale, _lastParentScale))
        {
            UpdateLocalScale();
            _lastParentScale = currentParentScale;
        }
    }

    /// <summary>
    /// 核心逻辑：固定缩放大小，保留父对象的正负方向
    /// </summary>
    private void UpdateLocalScale()
    {
        if (_transform.parent == null)
        {
            // 没有父物体，直接设置本地缩放为目标大小（正负为正）
            _transform.localScale = targetWorldScaleAbs;
            return;
        }

        Vector3 parentWorldScale = _transform.parent.lossyScale;

        Vector3 parentScaleSign = new Vector3(
            Mathf.Sign(parentWorldScale.x), // 保留父对象X轴缩放的正负
            Mathf.Sign(parentWorldScale.y), // 保留父对象Y轴缩放的正负
            Mathf.Sign(parentWorldScale.z)  // 保留父对象Z轴缩放的正负
        );

        Vector3 parentScaleAbs = new Vector3(
            Mathf.Max(Mathf.Abs(parentWorldScale.x), 0.001f),
            Mathf.Max(Mathf.Abs(parentWorldScale.y), 0.001f),
            Mathf.Max(Mathf.Abs(parentWorldScale.z), 0.001f)
        );

        //    → 大小固定为targetWorldScaleAbs，正负跟随父对象
        Vector3 newLocalScale = new Vector3(
            (targetWorldScaleAbs.x / parentScaleAbs.x) * parentScaleSign.x,
            (targetWorldScaleAbs.y / parentScaleAbs.y) * parentScaleSign.y,
            (targetWorldScaleAbs.z / parentScaleAbs.z) * parentScaleSign.z
        );

        // 应用本地缩放
        _transform.localScale = newLocalScale;
    }

    /// <summary>
    /// 获取父物体的世界缩放（无父物体返回Vector3.one）
    /// </summary>
    private Vector3 GetParentLossyScale()
    {
        return _transform.parent != null ? _transform.parent.lossyScale : Vector3.one;
    }

    /// <summary>
    /// 比较两个缩放是否相等（避免浮点精度问题）
    /// </summary>
    private bool IsScaleEqual(Vector3 a, Vector3 b)
    {
        const float epsilon = 0.001f;
        return Mathf.Abs(a.x - b.x) < epsilon &&
               Mathf.Abs(a.y - b.y) < epsilon &&
               Mathf.Abs(a.z - b.z) < epsilon;
    }

    // 父物体层级变化时，重新计算缩放
    private void OnTransformParentChanged()
    {
        UpdateLocalScale();
        _lastParentScale = GetParentLossyScale();
    }

    // 可选：编辑器下实时预览效果（不用运行游戏就能看到）
    private void OnValidate()
    {
        if (_transform == null) _transform = transform;
        UpdateLocalScale();
    }
}