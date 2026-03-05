using UnityEngine;
using DG.Tweening; // 必须引入DoTween命名空间

public class ObjTrackMove : MonoBehaviour
{
    public Transform Up;          // 上方目标点
    public Transform Down;        // 下方目标点
    public float moveDuration = 1f; // 单次移动耗时（秒）
    private float upY;            // 上方目标Y坐标
    private float downY;          // 下方目标Y坐标
    private Sequence moveSequence; // DoTween序列对象

    void Start()
    {
        // 空引用检查，避免报错
        if (Up == null || Down == null)
        {
            Debug.LogError("请为Up和Down赋值对应的Transform对象！");
            return;
        }

        // 获取两个点的Y轴坐标
        upY = Up.localPosition.y;
        downY = Down.localPosition.y;

        // 初始化并启动移动序列
        InitMoveSequence();
    }

    /// <summary>
    /// 初始化DoTween序列，纯DoTween实现循环移动+停留
    /// </summary>
    private void InitMoveSequence()
    {
        if (moveSequence != null)
        {
            moveSequence.Kill();
        }
        moveSequence = DOTween.Sequence();

        moveSequence.Append(transform.DOLocalMoveY(upY, moveDuration)
            .SetEase(Ease.Linear)); // 线性缓动，匀速移动

        moveSequence.AppendInterval(1f);

        moveSequence.Append(transform.DOLocalMoveY(downY, moveDuration)
            .SetEase(Ease.Linear));

        moveSequence.AppendInterval(1f);

        moveSequence.Append(transform.DOLocalMoveY(upY, moveDuration)
            .SetEase(Ease.Linear)); // 线性缓动，匀速移动


        moveSequence.SetLoops(-1);
    }

    // 停止移动的方法
    public void StopMovement()
    {
        if (moveSequence != null)
        {
            moveSequence.Kill(); // 销毁序列，停止所有动画
            moveSequence = null;
        }
        transform.DOKill(); // 兜底：杀死该物体上所有DoTween动画
    }

    // 物体销毁时清理DoTween资源，避免内存泄漏
    private void OnDestroy()
    {
        StopMovement();
    }
}