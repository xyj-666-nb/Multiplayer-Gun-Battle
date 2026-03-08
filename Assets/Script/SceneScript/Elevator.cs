using Mirror;
using UnityEngine;
using DG.Tweening;

public class Elevator : NetworkBehaviour
{
    [Header("===== 楼层配置（本地坐标） =====")]
    [Tooltip("1楼停留的本地 Y 坐标")]
    public float firstFloorLocalY = 0f;
    [Tooltip("2楼停留的本地 Y 坐标")]
    public float secondFloorLocalY = 5f;

    [Header("===== 移动配置 =====")]
    [Tooltip("电梯移动速度（单位/秒）")]
    public float moveSpeed = 2f;
    [Tooltip("到达楼层后停留时间（秒）")]
    public float waitTime = 2f;
    [Tooltip("移动曲线：先快后慢用 OutQuad/OutCubic，先慢后快用 InQuad，匀速用 Linear")]
    public Ease moveEase = Ease.OutQuad;

    // 内部状态变量
    private bool _isMovingToSecondFloor = true; // 默认先往2楼走
    private Tween _moveTween; // 缓存 DOTween 引用

    // 只在服务端初始化
    public override void OnStartServer()
    {
        base.OnStartServer();
        Init(); // 服务端启动时调用初始化
    }

    // 仅服务端执行
    [Server]
    public void Init()
    {
        gameObject.SetActive(true);
        Debug.Log("【Elevator】服务端初始化电梯");
        // 确保电梯初始位置正确（可选）
        transform.localPosition = new Vector2(transform.localPosition.x, firstFloorLocalY);
        // 开始电梯循环
        MoveElevator();
    }

    //仅服务端执行，通过 NetworkTransform 同步位置
    [Server]
    private void MoveElevator()
    {
        float targetLocalY = _isMovingToSecondFloor ? secondFloorLocalY : firstFloorLocalY;

        float distance = Mathf.Abs(targetLocalY - transform.localPosition.y);
        float duration = distance / moveSpeed;

        _moveTween = transform.DOLocalMoveY(targetLocalY, duration)
            .SetEase(moveEase) // 设置先快后慢的曲线
            .OnComplete(() =>
            {
                // 服务端处理停留和换向逻辑
                DOVirtual.DelayedCall(waitTime, () =>
                {
                    _isMovingToSecondFloor = !_isMovingToSecondFloor;
                    MoveElevator(); // 递归调用，实现循环
                });
            });
    }

    // 玩家进入触发器：设为子对象（仅本地玩家处理，无需同步）
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        NetworkIdentity playerIdentity = collision.GetComponent<NetworkIdentity>();
        if (playerIdentity != null && playerIdentity.isLocalPlayer)
        {
            collision.transform.SetParent(transform);
            Debug.Log("玩家已登上电梯");
        }
    }

    // 玩家离开触发器：断开父子关系（仅本地玩家处理）
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        NetworkIdentity playerIdentity = collision.GetComponent<NetworkIdentity>();
        if (playerIdentity != null && playerIdentity.isLocalPlayer)
        {
            collision.transform.SetParent(null);
            Debug.Log("玩家已离开电梯");
        }
    }

    // 脚本销毁时停止 DOTween
    private void OnDestroy()
    {
        _moveTween?.Kill();
    }
}