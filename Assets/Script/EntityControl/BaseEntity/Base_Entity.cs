using Mirror;
using UnityEngine;

public class Base_Entity : NetworkBehaviour
{
    public bool isFlip = true; // 全局翻转标识（控制是否允许缩放翻转）
    public Rigidbody2D MyRigdboby;

    #region 检测标识线 & 3D区域检测参数
    [Header("地面检测2D")]
    public Transform GroundCheck;
    public float GroundCheckDistance = 0.1f;
    public LayerMask Layer_Ground;

    [Header("墙壁检测2D")]
    public Transform WallCheck;
    public float WallCheckDistance = 0.1f;
    public LayerMask Layer_Wall;
    #endregion

    #region 墙壁以及地面检测
    // 2D地面检测
    public virtual bool IsGroundDetected() => Physics2D.Raycast(GroundCheck.position, Vector2.down,
        GroundCheckDistance, Layer_Ground);

    // 2D墙壁检测
    public virtual bool IsWallDetected()
    {
        if (WallCheck == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Layer_Wall);
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        int hitCount = Physics2D.Raycast(
            WallCheck.position,
            Vector2.right * FacingDir,
            filter,
            hits,
            WallCheckDistance
        );

        if (hitCount > 0)
        {
            return hits[0].collider.gameObject != gameObject;
        }
        return false;
    }
    #endregion

    #region 生物翻转
    [Header("角色朝向")]
    [SyncVar(hook = nameof(OnFacingDirChanged))]
    public int FacingDir = 1;


    /// <summary>
    /// 【客户端调用】请求服务器执行翻转
    /// </summary>
    [Command(requiresAuthority = true)]
    public void CmdRequestFlip(int targetDir)
    {
        // 服务器直接接受客户端的方向并同步给其他人
        FacingDir = targetDir;
    }

    /// <summary>
    /// FacingDir同步钩子（其他客户端同步缩放）
    /// </summary>
    private void OnFacingDirChanged(int oldValue, int newValue)
    {
        // 如果是本地玩家，因为我们已经"本地预测"提前翻转过了，所以这里不用重复执行
        if (isLocalPlayer) return;

        ApplyFlipVisual(newValue);
    }

    /// <summary>
    /// 纯视觉翻转表现（抽离出来方便本地和网络共用）
    /// </summary>
    public void ApplyFlipVisual(int dir)
    {
        if (!isFlip) return;
        float targetScaleX = dir;
        Vector3 currentScale = transform.localScale;
        currentScale.x = targetScaleX;
        transform.localScale = currentScale;
    }
    #endregion

    #region 生物初始化
    public virtual void Awake()
    {
        MyRigdboby = GetComponent<Rigidbody2D>();
        if (MyRigdboby == null)
            MyRigdboby = GetComponentInChildren<Rigidbody2D>();
    }
    #endregion

    #region Gizmos绘制
    public virtual void OnDrawGizmos()
    {
        if (GroundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(GroundCheck.position,
             new Vector3(GroundCheck.position.x,
             GroundCheck.position.y - GroundCheckDistance));
        }

        if (WallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(WallCheck.position,
            new Vector3(WallCheck.position.x + WallCheckDistance * FacingDir,
            WallCheck.position.y));
        }
    }
    #endregion

    //辅助函数
    public void SetVelocity(float X, float Y)
    {
        MyRigdboby.velocity = new Vector2(X, Y);
    }

    public virtual void DestroyMe(float Time = 0)
    {
        Destroy(this.gameObject, Time);
    }
}