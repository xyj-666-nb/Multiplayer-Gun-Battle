using Mirror;
using UnityEngine;
using System;

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

    #region 缓存变量
    private RaycastHit2D[] _wallHitCache;
    private ContactFilter2D _wallContactFilter;
    private readonly Vector2 _downDir = Vector2.down;
    private readonly Vector2 _rightDir = Vector2.right;
    private bool _lastGroundedState;
    public event Action<bool> OnGroundStateChanged;
    #endregion

    #region 墙壁以及地面检测
    // 2D地面检测
    public virtual bool IsGroundDetected() => Physics2D.Raycast(GroundCheck.position, _downDir,
        GroundCheckDistance, Layer_Ground);

    // 2D墙壁检测
    public virtual bool IsWallDetected()
    {
        if (WallCheck == null)
            return false;

        if (_wallHitCache == null)
            _wallHitCache = new RaycastHit2D[1];

        // 保守优化：仅初始化一次过滤器，杜绝重复分配
        if (_wallContactFilter.layerMask == 0)
        {
            _wallContactFilter = new ContactFilter2D();
            _wallContactFilter.SetLayerMask(Layer_Wall);
            _wallContactFilter.useTriggers = false;
        }

        int hitCount = Physics2D.Raycast(
            WallCheck.position,
            _rightDir * FacingDir,
            _wallContactFilter,
            _wallHitCache,
            WallCheckDistance
        );

        if (hitCount > 0)
        {
            return _wallHitCache[0].collider.gameObject != gameObject;
        }
        return false;
    }
    #endregion

    #region 生物翻转
    [Header("角色朝向")]
    [SyncVar(hook = nameof(OnFacingDirChanged))]
    public int FacingDir = 1;

    [Command(requiresAuthority = true)]
    public void CmdRequestFlip(int targetDir)
    {
        FacingDir = targetDir;
    }

    private void OnFacingDirChanged(int oldValue, int newValue)
    {
        if (oldValue == newValue || isLocalPlayer) return;
        ApplyFlipVisual(newValue);
    }

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
        if (MyRigdboby == null)
        {
            MyRigdboby = GetComponent<Rigidbody2D>();
            if (MyRigdboby == null)
                MyRigdboby = GetComponentInChildren<Rigidbody2D>();
        }

        if (_wallHitCache == null)
            _wallHitCache = new RaycastHit2D[1];

        if (GroundCheck != null)
        {
            _lastGroundedState = IsGroundDetected();
        }
    }

    public virtual void FixedUpdate()
    {
        if (GroundCheck == null) return;

        bool currentGrounded = IsGroundDetected();

        if (currentGrounded != _lastGroundedState)
        {
            OnGroundStateChanged?.Invoke(currentGrounded);
            _lastGroundedState = currentGrounded;
        }
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

    public void SetVelocity(float X, float Y)
    {
        MyRigdboby.velocity = new Vector2(X, Y);
    }

    public virtual void DestroyMe(float Time = 0)
    {
        CancelInvoke();
        Destroy(this.gameObject, Time);
    }

    protected virtual void OnDestroy()
    {
        _wallHitCache = null;
        MyRigdboby = null;
        OnGroundStateChanged = null;
    }
}