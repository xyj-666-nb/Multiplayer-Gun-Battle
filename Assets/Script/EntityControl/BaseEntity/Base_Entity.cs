using Mirror;
using UnityEngine;

public class Base_Entity : NetworkBehaviour
{
    public bool isFlip = true;
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
    public int FacingDir = 1;
    private bool FacingRight = true;

    public void Flip()
    {
        FacingDir *= -1;
        FacingRight = !FacingRight;
        Vector3 scale = transform.localScale;
        scale.x = FacingRight ? 1 : -1;
        transform.localScale = scale;
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

    #region Gizmos绘制（新增3D区域检测的可视化）
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

