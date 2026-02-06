using UnityEngine;

public class Base_Entity : MonoBehaviour
{
    public EntityStateMachine MyStateMachine { get; private set; } //动画控制器
    public Animator MyAnimator { get; private set; }
    public bool isFlip = true;

    #region 组件相关
    #region 2D组件
    public Rigidbody2D MyRigdboby { get; private set; }
    public EntityFX MyentityFX;
    #endregion

    #region 3D组件
    public Rigidbody MyRight3D { get; private set; }
    #endregion
    #endregion

    public bool IsUse3D = false;//是否使用3d设置

    #region 检测标识线 & 3D区域检测参数
    // 修正拼写错误：Gorund → Ground
    [Header("地面检测2D")]
    public Transform GroundCheck;
    public float GroundCheckDistance = 0.1f;
    public LayerMask Layer_Ground;

    [Header("墙壁检测2D")]
    public Transform WallCheck;
    public float WallCheckDistance = 0.1f;
    public LayerMask Layer_Wall;

    [Header("3D区域检测参数")]
    [Tooltip("3D地面检测的盒形区域大小")]
    public Vector3 GroundCheckBoxSize_3D = new Vector3(0.5f, 0.1f, 0.5f);
    [Tooltip("3D墙壁检测的盒形区域大小")]
    public Vector3 WallCheckBoxSize_3D = new Vector3(0.1f, 0.8f, 0.5f);
    [Tooltip("3D检测时是否忽略自身碰撞体")]
    public bool IgnoreSelfIn3DCheck = true;
    #endregion

    #region 墙壁以及地面检测
    // 2D地面检测（保留原有逻辑）
    public virtual bool IsGroundDetected() => Physics2D.Raycast(GroundCheck.position, Vector2.down,
        GroundCheckDistance, Layer_Ground);

    // 3D地面检测：改为盒形区域检测（替换原射线检测）
    public virtual bool IsGroundDetected_3D()
    {
        if (GroundCheck == null) return false;

        // 盒形检测的中心（GroundCheck下方偏移，贴合地面）
        Vector3 checkCenter = GroundCheck.position + Vector3.down * (GroundCheckBoxSize_3D.y / 2);
        // 盒形检测的旋转（和物体自身旋转一致）
        Quaternion checkRotation = Quaternion.identity;

        // 执行3D盒形区域检测
        Collider[] hitColliders = Physics.OverlapBox(
            checkCenter,                  // 检测中心
            GroundCheckBoxSize_3D / 2,    // 盒形半尺寸
            checkRotation,                // 旋转
            Layer_Ground,                 // 检测层
            QueryTriggerInteraction.Ignore // 忽略触发器
        );

        // 过滤检测结果：排除自身碰撞体（如果开启）
        foreach (var coll in hitColliders)
        {
            if (IgnoreSelfIn3DCheck && coll.gameObject == gameObject)
                continue;
            return true; // 检测到地面
        }
        return false;
    }

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

    // 3D墙壁检测：改为盒形区域检测
    public virtual bool IsWallDetected_3D()
    {
        if (WallCheck == null) return false;

        // 盒形检测的中心
        Vector3 checkCenter = WallCheck.position + (Vector3.right * FacingDir) * (WallCheckBoxSize_3D.x / 2);
        // 盒形检测的旋转
        Quaternion checkRotation = Quaternion.identity;

        // 执行3D盒形区域检测
        Collider[] hitColliders = Physics.OverlapBox(
            checkCenter,                  // 检测中心
            WallCheckBoxSize_3D / 2,      // 盒形半尺寸
            checkRotation,                // 旋转
            Layer_Wall,                   // 检测层
            QueryTriggerInteraction.Ignore // 忽略触发器
        );

        // 过滤检测结果：排除自身碰撞体
        foreach (var coll in hitColliders)
        {
            if (IgnoreSelfIn3DCheck && coll.gameObject == gameObject)
                continue;
            return true; // 检测到墙壁
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

    public void SetFlip(bool _isFlip)
    {
        isFlip = _isFlip;
    }
    #endregion

    #region 生物初始化
    public virtual void Awake()
    {
        if (!IsUse3D)
        {
            MyentityFX = GetComponent<EntityFX>();
            MyRigdboby = GetComponent<Rigidbody2D>();
            if (MyRigdboby == null)
                MyRigdboby = GetComponentInChildren<Rigidbody2D>();
        }
        else
        {
            MyRight3D = GetComponent<Rigidbody>();//获取3D刚体
            if (MyRight3D == null)
                Debug.Log("未找到3D碰撞体");
        }

        MyStateMachine = new EntityStateMachine();
        MyAnimator = GetComponentInChildren<Animator>();
    }
    #endregion

    #region Gizmos绘制（新增3D区域检测的可视化）
    public virtual void OnDrawGizmos()
    {
        // ========== 2D检测绘制 ==========
        if (!IsUse3D)
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
        // ========== 3D检测绘制 ==========
        else
        {
            // 绘制3D地面检测盒
            if (GroundCheck != null)
            {
                Gizmos.color = Color.green;
                Vector3 groundCenter = GroundCheck.position + Vector3.down * (GroundCheckBoxSize_3D.y / 2);
                // 绘制盒形轮廓（线框）
                Gizmos.DrawWireCube(groundCenter, GroundCheckBoxSize_3D);
            }

            // 绘制3D墙壁检测盒
            if (WallCheck != null)
            {
                Gizmos.color = Color.red;
                Vector3 wallCenter = WallCheck.position + (Vector3.right * FacingDir) * (WallCheckBoxSize_3D.x / 2);
                // 绘制盒形轮廓（线框）
                Gizmos.DrawWireCube(wallCenter, WallCheckBoxSize_3D);
            }
        }
    }
    #endregion

    public virtual void Update()
    {
        MyStateMachine.CurrentState.update();
    }

    //辅助函数
    public void SetVelocity(float X, float Y)
    {
        MyRigdboby.velocity = new Vector2(X, Y);
    }
    public void SetVelocity3D(float X, float Y, float Z)
    {
        MyRight3D.velocity = new Vector3(X, Y, Z);
    }

    public virtual void DestroyMe(float Time = 0)
    {
        Destroy(this.gameObject, Time);
    }

    public virtual void AnimatorFinish()
    {
        MyStateMachine.CurrentState.AnimatorFinish();
    }
}

