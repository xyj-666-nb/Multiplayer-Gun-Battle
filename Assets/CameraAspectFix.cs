using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFix : MonoBehaviour
{
    // 你设计的原始比例（竖屏 9:16，横屏 16:9）
    [Header("设计宽高比（宽/高）")]
    public float targetAspect =1.777f;

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        // 强制使用目标比例
        _cam.aspect = targetAspect;
    }

    // 可选：运行中也保持（比如旋转屏幕）
    void Update()
    {
        if (_cam.aspect != targetAspect)
            _cam.aspect = targetAspect;
    }
}