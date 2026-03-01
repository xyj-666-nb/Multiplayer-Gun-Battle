using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 必须导入DOTween命名空间

public class Propeller : MonoBehaviour
{
    [Header("旋转配置")]
    [Tooltip("旋转速度（度/秒），正数顺时针，负数逆时针")]
    public float rotateSpeed = 1080; // 1秒旋转360°，可根据需求调整
    [Tooltip("旋转轴（默认绕Z轴旋转，2D/3D都适用）")]
    public Vector3 rotateAxis = Vector3.forward; // Z轴：Vector3.forward | Y轴：Vector3.up

    private Tweener _rotateTweener; // 缓存旋转动画，方便后续控制

    void Start()
    {
        // 初始化旋转动画
        StartPropellerRotate();
    }

    /// <summary>
    /// 启动螺旋桨旋转（无限循环，0~360°循环）
    /// </summary>
    private void StartPropellerRotate()
    {
        // 停止已有动画，避免重复创建
        if (_rotateTweener != null && _rotateTweener.IsActive())
        {
            _rotateTweener.Kill();
        }

        // 计算旋转一圈的时长（360° / 旋转速度）
        float rotateDuration = 360f / Mathf.Abs(rotateSpeed);

        // DOTween实现无限循环旋转（0→360°→0循环）
        _rotateTweener = transform.DORotate(
            new Vector3(transform.rotation.eulerAngles.x + 360f * rotateAxis.x,
                        transform.rotation.eulerAngles.y + 360f * rotateAxis.y,
                        transform.rotation.eulerAngles.z + 360f * rotateAxis.z),
            rotateDuration,
            RotateMode.FastBeyond360 // 关键：允许旋转超过360°，且不重置角度
        )
        .SetEase(Ease.Linear) // 匀速旋转，符合螺旋桨物理效果
        .SetLoops(-1, LoopType.Restart) // 无限循环，每次循环后重置角度到0再旋转
        .SetLink(gameObject); // 绑定到物体，物体销毁时自动停止动画，避免内存泄漏
    }

    /// <summary>
    /// 暂停旋转（可选扩展方法）
    /// </summary>
    public void PauseRotate()
    {
        if (_rotateTweener != null && _rotateTweener.IsActive())
        {
            _rotateTweener.Pause();
        }
    }

    /// <summary>
    /// 恢复旋转（可选扩展方法）
    /// </summary>
    public void ResumeRotate()
    {
        if (_rotateTweener != null && !_rotateTweener.IsPlaying() && _rotateTweener.IsActive())
        {
            _rotateTweener.Play();
        }
    }

    // 物体销毁时停止动画，避免内存泄漏
    private void OnDestroy()
    {
        if (_rotateTweener != null)
        {
            _rotateTweener.Kill();
            _rotateTweener = null;
        }
    }
}