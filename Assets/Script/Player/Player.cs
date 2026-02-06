using Mirror;
using UnityEngine;

public class Player : Base_Entity
{
    // 单例引用
    public static Player instance => Instance;
    public static Player Instance;

    // 自身状态组件
    private playerStats Mystats;
    private MyPlayerInput MyInputSystem;

    // 同步速度字段
    [SyncVar(hook = nameof(OnVelocitySynced))]
    private Vector2 _syncVelocity;

    // 速度属性
    public Vector2 MyVelocity
    {
        get => MyRigdboby != null ? MyRigdboby.velocity : Vector2.zero;
        set
        {
            if (!isServer)
            {
                Debug.LogWarning("客户端禁止直接修改刚体速度！请通过Command请求服务器");
                return;
            }
            // 服务器端先钳制速度，再赋值
            Vector2 clampedVel = ClampVelocity(value);
            MyRigdboby.velocity = clampedVel;
            _syncVelocity = clampedVel;
        }
    }

    #region 缩放插值核心变量
    private float _currentYScale;
    private float _targetYStretch;
    [Header("缩放插值配置")]
    public float MainLerpSpeed = 2f;
    public float MaxLerpSpeed = 15f;
    private Vector2 _baseScale;
    #endregion

    #region Mirror核心本地回调
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (Instance == null)
        {
            Instance = this;
            if (Mystats != null) Mystats.TriggerBinding();
            if (MyInputSystem != null) MyInputSystem.TriggerBinding();
        }
        else
            Destroy(gameObject);
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (Instance == this) Instance = null;
    }
    #endregion

    public override void Awake()
    {
        base.Awake();

        Mystats = GetComponent<playerStats>();
        if (Mystats == null)
        {
            Debug.LogError("Player.Awake：未挂载playerStats组件！拉伸和速度限制会失效");
        }
        MyInputSystem = GetComponent<MyPlayerInput>();

        // 初始化缩放变量
        _baseScale = transform.localScale;
        _currentYScale = _baseScale.y;

        //物理信息打印
        if (RigidbodyGUITestManager.Instance != null)
            RigidbodyGUITestManager.Instance.AddRigInfoShow_2D(MyRigdboby, "玩家");
    }

    public override void DestroyMe(float Time = 0)
    {
        base.DestroyMe(Time);
    }

    private void FixedUpdate()
    {
        if (!isServer || MyRigdboby == null || Mystats == null) return;

        // 服务器端实时钳制刚体速度（不管是AddForce还是其他方式修改，都能被限制）
        Vector2 clampedVel = ClampVelocity(MyRigdboby.velocity);
        if (MyRigdboby.velocity != clampedVel)
        {
            MyRigdboby.velocity = clampedVel;
            _syncVelocity = clampedVel; // 同步钳制后的速度到客户端
            Debug.Log($"服务器钳制速度：X={clampedVel.x:F2}, Y={clampedVel.y:F2}");
        }
    }

    public void Update()
    {
        playerMoveStretchAnima();
    }

    #region 运动拉伸动画
    #region 运动拉伸动画
    /// <summary>
    /// 垂直拉伸（跳跃）+ 移动抖动（地面移动）二合一逻辑
    /// 状态1：Y速度小（-0.1~0.1）+ X有速度 → 移动抖动
    /// 状态2：Y速度≥0.1/≤-0.1 → 跳跃垂直拉伸
    /// 状态3：无速度 → 回到基础缩放
    /// </summary>
    public void playerMoveStretchAnima()
    {
        if (Mystats == null || MyRigdboby == null) return;

        Vector2 clampedVel = ClampVelocity(MyVelocity);
        float currentYVel = clampedVel.y;
        float currentXVel = clampedVel.x;
        // Y速度阈值（-0.1 ~ 0.1）：判断是否为地面移动状态
        bool isGroundMoveState = Mathf.Abs(currentYVel) < 0.1f && Mathf.Abs(currentXVel) > 0.1f;
        // 跳跃拉伸状态：Y速度超出阈值
        bool isJumpStretchState = Mathf.Abs(currentYVel) >= 0.1f;

        if (isGroundMoveState)
        {
            float bumpyOffset = Mathf.Sin(Time.time * Mystats.MoveBumpySpeed) * Mystats.MoveBumpyRange;
            _targetYStretch = _baseScale.y + bumpyOffset;

            float bumpyLerpSpeed = 5f;
            _currentYScale = Mathf.Lerp(_currentYScale, _targetYStretch, bumpyLerpSpeed * Time.deltaTime);
        }
        else if (isJumpStretchState)
        {
            float ySpeedAbs = Mathf.Abs(currentYVel);
            float ySpeedRatio = ySpeedAbs / Mystats.MaxYSpeed;
            _targetYStretch = _baseScale.y + ySpeedRatio * Mystats.MaxYStretch;

            float scaleDelta = Mathf.Abs(_targetYStretch - _currentYScale);
            float speedRatio = Mathf.Clamp01(ySpeedAbs / Mystats.MaxYSpeed);
            float dynamicLerpSpeed = Mathf.Lerp(MainLerpSpeed, MaxLerpSpeed, speedRatio);
            dynamicLerpSpeed = Mathf.Lerp(MainLerpSpeed, MaxLerpSpeed, speedRatio + scaleDelta * 2f);

            _currentYScale = Mathf.Lerp(_currentYScale, _targetYStretch, dynamicLerpSpeed * Time.deltaTime);
            // 边界保护：不小于基础缩放的80%
            _currentYScale = Mathf.Max(_currentYScale, _baseScale.y * 0.8f);
        }
        else
        {
            _targetYStretch = _baseScale.y;
            // 慢插值回正，避免突变
            _currentYScale = Mathf.Lerp(_currentYScale, _targetYStretch, 2f * Time.deltaTime);
        }

        transform.localScale = new Vector3(
            _baseScale.x,
            _currentYScale,
            transform.localScale.z
        );

        string state = isGroundMoveState ? "移动抖动" : (isJumpStretchState ? "跳跃拉伸" : "无速度");
        Debug.Log($"状态：{state} | Y速度：{currentYVel:F2} | X速度：{currentXVel:F2} | 当前Y缩放：{_currentYScale:F2}");
    }
    #endregion
    #endregion

    #region 速度同步+钳制核心方法
    private void OnVelocitySynced(Vector2 oldVel, Vector2 newVel)
    {
        if (MyRigdboby == null || Mystats == null) 
            return;
        Vector2 clampedNewVel = ClampVelocity(newVel);
        MyRigdboby.velocity = Vector2.Lerp(MyRigdboby.velocity, clampedNewVel, Time.deltaTime * 10f);
    }

    // 客户端请求服务器施加跳跃力
    [Command]
    public void CmdAddJumpForce(float jumpPower)
    {
        if (MyRigdboby == null || Mystats == null)
            return;
        MyRigdboby.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
    }

    // 客户端请求服务器施加移动力
    [Command]
    public void CmdAddMoveForce(float movePower, int dir)
    {
        if (MyRigdboby == null || Mystats == null) return;
        float forceX = dir * movePower;
        // 增强控制力的逻辑
        if ((dir < 0 && MyRigdboby.velocity.x < 0) || (dir > 0 && MyRigdboby.velocity.x > 0))
        {
            forceX *= 2f;
        }
        MyRigdboby.AddForce(new Vector2(forceX, 0));
    }

    [Command]
    public void CmdUpdateVelocity(Vector2 targetVel)
    {
        MyVelocity = targetVel;
    }

    private Vector2 ClampVelocity(Vector2 velocity)
    {
        if (Mystats == null)
        {
            Debug.LogError("Player.ClampVelocity：playerStats组件未挂载，无法钳制速度！");
            return velocity;
        }

        float clampedX = Mathf.Clamp(velocity.x, -Mystats.MaxXSpeed, Mystats.MaxXSpeed);
        float clampedY = Mathf.Clamp(velocity.y, -Mystats.MaxYSpeed, Mystats.MaxYSpeed);

        return new Vector2(clampedX, clampedY);
    }
    #endregion

    // 对外暴露转向方法
    [Command]
    public void CmdFlip()
    {
        Flip(); // 假设Flip是父类Base_Entity的转向方法
    }
}