using DG.Tweening;
using Mirror;
using UnityEngine;

public class playerHandControl : NetworkBehaviour//玩家手部控制
{
    [Header("手臂旋转相关")]
    [SyncVar(hook = nameof(OnRotationValueSynced))]
    private float _currentRotationValue_Z = 0f;// 改为私有SyncVar，服务器权威
    public float RotateSpeed = 100f;//手部旋转速度

    [Header("进入举枪瞄准状态")]
    public float AimStateTransform_X;//进入举枪瞄准状态时手部的坐标位置
    public float EnterAimDuration = 0.5f;
    public AnimationCurve aimEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SyncVar(hook = nameof(OnIsEnterAimChanged))]
    private bool _isEnterAim = false;
    public bool IsEnterAim => _isEnterAim;

    private Vector3 _originLocalPos;

    #region SyncVar钩子
    private void OnRotationValueSynced(float oldFloat, float newFloat)
    {
        _currentRotationValue_Z = newFloat;
    }

    private void OnIsEnterAimChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        if (oldValue == newValue)
            return;

        if (newValue)
            EnterAimState();
        else
            ExitAimState();
    }
    #endregion

    #region 生命周期
    private void Awake()
    {
        // 缓存初始本地X坐标
        _originLocalPos = transform.localPosition;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 服务器初始化旋转值为当前角度
        _currentRotationValue_Z = transform.eulerAngles.z;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // 所有客户端初始化旋转值
        transform.rotation = Quaternion.Euler(0, 0, _currentRotationValue_Z);
    }
    #endregion

    #region 瞄准动画
    public void EnterAimState()
    {
        if (!isClient) return; // 仅客户端执行动画

        transform.DOKill();
        // 所有客户端执行举枪位移动画
        transform.DOLocalMoveX(AimStateTransform_X, EnterAimDuration)
                 .SetEase(aimEaseCurve)
                 .SetUpdate(false);
    }

    public void ExitAimState()
    {
        if (!isClient) return; // 仅客户端执行动画

        transform.DOKill();
        // 还原到初始本地X坐标
        transform.DOLocalMoveX(_originLocalPos.x, EnterAimDuration)
                 .SetEase(aimEaseCurve)
                 .SetUpdate(false);
    }
    #endregion

    #region 旋转逻辑
    void Update()
    {
        // 仅客户端执行旋转/动画逻辑
        if (!isClient) 
            return;

        // 所有客户端都同步旋转到目标角度
        float currentEulerZ = transform.eulerAngles.z;
        float newEulerZ = Mathf.MoveTowardsAngle(
            currentEulerZ,
            _currentRotationValue_Z,
            RotateSpeed * Time.deltaTime
        );
        transform.rotation = Quaternion.Euler(0, 0, newEulerZ);
    }

    /// <summary>
    /// 外部调用：设置目标旋转角度
    /// </summary>
    /// <param name="targetZ">目标Z轴旋转角度</param>
    public void SetRotationZ(float targetZ)
    {
        if (isServer)
        {
            // 服务器直接修改SyncVar，同步到所有客户端
            _currentRotationValue_Z = targetZ;
        }
        else if (isClient && isOwned)
        {
            // 本地客户端通过Command通知服务器修改
            CmdSetRotationZ(targetZ);
        }
    }

    [Command(requiresAuthority = true)]
    private void CmdSetRotationZ(float targetZ)
    {
        // 服务器更新旋转值，自动同步到所有客户端
        _currentRotationValue_Z = targetZ;
    }
    #endregion

    #region 瞄准状态设置
    /// <summary>
    /// 外部调用：设置瞄准状态
    /// </summary>
    /// <param name="wantAim">是否要进入瞄准状态</param>
    public void SetAimState(bool wantAim)
    {
        if (isServer)
        {
            // 服务器直接修改SyncVar，触发所有客户端的钩子
            _isEnterAim = wantAim;
        }
        else if (isClient )
        {
            CmdSetAimState(wantAim);
        }

    }

    [Command(requiresAuthority = true)]
    private void CmdSetAimState(bool wantAim)
    {
        _isEnterAim = wantAim;
    }
    #endregion

    public float CurrentRotationValue_Z => _currentRotationValue_Z;
}