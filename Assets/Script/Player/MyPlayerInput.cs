using Mirror;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static InputInfoManager;

public class MyPlayerInput : NetworkBehaviour
{
    private Player Myplayer;
    private playerStats MyStats;

    public bool IsCanControl = true;//玩家当前是否可以操作

    #region 跳跃核心状态
    private bool IsCanJump = true;       // 地面跳可用
    public bool IsCanWallJump = false;   // 墙跳可用
    private bool IsJumpCheck = false;    // 跳跃状态检测标记
    #endregion

    #region 水平移动控制
    private bool IsCanHorizontalMove = true; // 是否可以水平移动
    [Tooltip("墙跳后禁用水平移动的时间")]
    public float WallJumpMoveLockTime = 0.2f; // 0.2秒禁用
    #endregion

    #region 视野缩放相关
    private int ViewTaskID;//视野缩放任务ID
    private float ChangeSpeed_View = 4;//缩放视野的速度
    #endregion

    private void Awake()
    {
        MyStats = GetComponent<playerStats>();
    }

    // 新的初始化方法，由Player调用
    public void Initialize(Player player, playerStats stats)
    {
        this.Myplayer = player;
        this.MyStats = stats;
        TriggerBinding();
    }

    public void TriggerBinding()
    {
        if (Myplayer == null)
        {
            Debug.LogError("MyPlayerInput.TriggerBinding：本地玩家实例为null，无法绑定输入！");
            return;
        }

        if (Myplayer.isLocalPlayer)
        {
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Jump, Jump_Start, null, Jump_End, Jump_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.RightMove, null, null, null, Move_Right_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.LeftMove, null, null, null, Move_Left_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Attack, Shoot_Start, null, null, Shoot_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Reload, Reload_start, null, null);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.PickUpGun, PickUpGnn_Start, null, null);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.DiscardGun, DiscardGun_Start, null, null);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.GunAim, GunAim_Start, null, GunAim_End);
            Debug.Log("MyPlayerInput：本地玩家输入事件绑定成功！");
        }
    }

    #region 核心通用校验函数
    /// <summary>
    /// 输入触发通用前置校验
    /// </summary>
    /// <returns>true=满足通用条件，false=不满足</returns>
    private bool CheckCommonTriggerCondition()
    {
        // 必须是本地玩家
        if (!isLocalPlayer)
        {
            return false;
        }

        //如果关闭控制就不允许玩家进行控制
        if (!IsCanControl)
            return false;


        // 玩家实例不能为空
        if (Myplayer == null)
        {
            Debug.LogWarning("[MyPlayerInput] 玩家实例为空，跳过输入触发");
            return false;
        }

        // Rigidbody2D不能为空
        if (Myplayer.MyRigdboby == null)
        {
            Debug.LogWarning("[MyPlayerInput] 玩家Rigidbody2D为空/已销毁，跳过输入触发");
            return false;
        }

        // 所有通用条件满足
        return true;
    }
    #endregion

    #region 跳跃逻辑
    public void Jump_Start(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (IsCanJump)
        {
            float jumpPower = Myplayer.MyHandControl != null && Myplayer.MyHandControl.IsEnterAim
                ? MyStats.AimJumpPower
                : MyStats.JumpPower;

            if (Myplayer.currentGun != null && Myplayer.currentGun.IsInShoot)
                Myplayer.MyRigdboby.AddForce(new Vector2(0, jumpPower / 2), ForceMode2D.Impulse);
            else
                Myplayer.MyRigdboby.AddForce(new Vector2(0, jumpPower), ForceMode2D.Impulse);

            IsCanJump = false;
            return; // 地面跳触发后，直接结束，不执行墙跳逻辑
        }

        if (IsCanWallJump)
        {
            float wallJumpHorizontal = MyStats.WallJumpPower_Side * -Myplayer.FacingDir;
            float wallJumpVertical = MyStats.WallJumpPower_Up;

            // 清空下落速度，保证墙跳高度稳定
            Myplayer.MyRigdboby.velocity = new Vector2(Myplayer.MyRigdboby.velocity.x, 0);
            // 施加墙跳力
            Myplayer.MyRigdboby.AddForce(new Vector2(wallJumpHorizontal, wallJumpVertical), ForceMode2D.Impulse);

            IsCanHorizontalMove = false;
            // 启动计时器，0.2秒后恢复移动
            Invoke(nameof(ResetHorizontalMove), WallJumpMoveLockTime);

            // 墙跳后立即关闭，防止连续墙跳
            IsCanWallJump = false;
            //然后翻转玩家
            Myplayer.CmdRequestFlip();
        }

        CountDownManager.Instance.CreateTimer(false, 50, () => { IsJumpCheck = true; });//0.2秒后才开启检测
    }

    /// <summary>
    /// 恢复水平移动权限
    /// </summary>
    private void ResetHorizontalMove()
    {
        IsCanHorizontalMove = true;
    }

    public void Jump_Continue(CustomInputContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        // 第二步：自身特殊条件（原有逻辑完全不变）
        if (Myplayer.MyRigdboby.velocity.y > 0)
            Myplayer.MyRigdboby.gravityScale = 0.75f;
    }

    public void Jump_End(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        // 第二步：自身特殊条件（原有逻辑完全不变）
        Myplayer.MyRigdboby.gravityScale = 1f;
    }

    /// <summary>
    /// 跳跃状态检测
    /// </summary>
    public void IsCanJumpCheck()
    {
        if (!Myplayer.IsGroundDetected() && Myplayer.IsWallDetected())
            IsCanWallJump = true;
        else
            IsCanWallJump = false;

        // 这里保留原有校验
        if (!isLocalPlayer || !IsJumpCheck)
            return;

        if (Myplayer.IsGroundDetected())
        {
            IsCanJump = true;
            IsCanWallJump = false;
            IsJumpCheck = false;
            // 落地后立即恢复水平移动
            IsCanHorizontalMove = true;
            CancelInvoke(nameof(ResetHorizontalMove));
            return;
        }
    }
    #endregion

    #region 移动相关
    /// <summary>
    /// 通用移动逻辑处理
    /// </summary>
    /// <param name="moveDirection">移动方向：-1=左，1=右</param>
    /// <param name="targetFacingDir">目标朝向：-1=左，1=右</param>
    private void HandleMoveLogic(float moveDirection, int targetFacingDir)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        // 第二步：自身特殊条件
        if (!IsCanHorizontalMove)
            return;

        // 判断是否处于瞄准状态
        float finalMovePower = Myplayer.MyHandControl != null && Myplayer.MyHandControl.IsEnterAim
            ? MyStats.AimMovePower
            : MyStats.movePower;

        if (Myplayer.currentGun != null && Myplayer.currentGun.IsInShoot)
        {
            finalMovePower /= 4;
        }

        float applyPower = finalMovePower;
        if (Myplayer.MyRigdboby.velocity.x * moveDirection < 0)
        {
            applyPower *= 2;
        }

        Myplayer.MyRigdboby.AddForce(new Vector2(moveDirection * applyPower, 0), ForceMode2D.Impulse);

        float currentXVelocity = Myplayer.MyRigdboby.velocity.x;
        // 判断是否处于瞄准状态
        float dynamicMaxSpeed = Myplayer.MyHandControl != null && Myplayer.MyHandControl.IsEnterAim
            ? MyStats.AimMoveMaxSpeed
            : MyStats.MaxXSpeed;

        // 射击状态 → 最大速度减半
        if (Myplayer.currentGun != null && Myplayer.currentGun.IsInShoot)
        {
            dynamicMaxSpeed /= 2;
        }
        // 用动态最大速度限制
        if (Math.Abs(currentXVelocity) > dynamicMaxSpeed)
        {
            Myplayer.MyRigdboby.velocity = new Vector2(targetFacingDir * dynamicMaxSpeed, Myplayer.MyRigdboby.velocity.y);
        }

        if (Myplayer.FacingDir != targetFacingDir)
        {
            Myplayer.CmdRequestFlip();
        }
    }

    // 左移
    public void Move_Left_Continue(CustomInputContext Content)
    {
        // 左移：方向=-1，目标朝向=-1
        HandleMoveLogic(-1, -1);
    }

    // 右移
    public void Move_Right_Continue(CustomInputContext Content)
    {
        // 右移：方向=1，目标朝向=1
        HandleMoveLogic(1, 1);
    }
    #endregion

    #region 射击相关
    public void Shoot_Start(InputAction.CallbackContext Content)
    {
        if (!CheckCommonTriggerCondition())
            return;

        //在这里触发战术设备的使用
        if (Player.LocalPlayer.MyHandControl.CurrentThrowObj != null)
        {
            //在这里就触发投掷物的使用
            PlayerTacticControl.Instance.LaunchCurrentThrowObj();//发射投掷物
            return;
        }

        if (PlayerTacticControl.Instance.IsPrepararingInjection)//询问是否正在准备注射
        {
            PlayerTacticControl.Instance.triggerInjection();//触发注射器
                                      
            PlayerTacticControl.Instance.CancelTactic();
          
            PlayerTacticControl.Instance.SetIsChooseButton(false);
        }

        Debug.Log("开始射击");
    }

    public void Shoot_Continue(CustomInputContext Content)
    {
        if (!CheckCommonTriggerCondition())
            return;

        if (Myplayer.currentGun == null || Myplayer.MyHandControl.IsHolsterGun || Myplayer.MyHandControl._isHolsterAnimaPlaying|| PlayerTacticControl.Instance.IsPrepararingInjection)//收枪就不允许射击
            return;

        //判断射击条件,如果是连发枪就自动检测并自动触发射击
        if (Myplayer.currentGun.IsCanShoot())
        {
            Myplayer.currentGun.TriggerSingleShoot();//触发射击
        }
    }
    #endregion

    #region 换弹相关
    public void Reload_start(InputAction.CallbackContext Content)//开始换弹
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (Myplayer.currentGun == null || Myplayer.MyHandControl.IsHolsterGun)
            return;

        //判断当前换弹条件
        if (Myplayer.currentGun.IsCanReload())
            Myplayer.currentGun.TriggerReload();//调用换弹
    }
    #endregion

    #region 拾取枪械控制逻辑
    public void PickUpGnn_Start(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (Myplayer.CurrentTouchGun != null)
        {
            Myplayer.PickUpSceneGun();
        }
    }
    #endregion

    #region 丢弃枪械控制逻辑
    public void DiscardGun_Start(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (Myplayer.MyHandControl.IsEnterAim || Myplayer.MyHandControl.IsHolsterGun)
            return;

        Debug.Log("正在判断丢弃条件");
        if (Myplayer.currentGun != null && !Myplayer.currentGun.IsInReload && !Myplayer.currentGun.IsInShoot)
        {
            //触发丢弃
            Myplayer.DropCurrentGun();//丢弃当前的枪械
        }
    }
    #endregion

    #region 枪械瞄准控制逻辑
    public void GunAim_Start(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (Myplayer.currentGun == null)
            return;

        Debug.Log("进入瞄准状态");
        //触发枪进入瞄准状态
        Myplayer.currentGun.ChangeAimState(true);
        //手动触发toggle按钮
        ButtonGroupManager.Instance.ManualSelectToggleButton(PlayerPanel.GetAimButtonButtonGroupName());
        //触发手部进入瞄准状态
        Myplayer.MyHandControl.SetAimState(true);
        //缩放视野
        MyCameraControl.Instance.ResetZoomTask(ViewTaskID);
        ViewTaskID = MyCameraControl.Instance.AddZoomTask_ByPercent_TemporaryManual(1 + Myplayer.myStats.AimViewBonus, ChangeSpeed_View);//通过数据进行缩放
    }

    public void GunAim_End(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (Myplayer.currentGun == null)
            return;

        Debug.Log("退出瞄准状态");
        //触发枪退出瞄准状态
        Myplayer.currentGun.ChangeAimState(false);
        //手动退出触发toggle按钮
        ButtonGroupManager.Instance.ManualCancelToggleButton(PlayerPanel.GetAimButtonButtonGroupName());
        //触发手部退出瞄准状态
        Myplayer.MyHandControl.SetAimState(false);
        //停止任务
        MyCameraControl.Instance.ResetZoomTask(ViewTaskID);
    }
    #endregion

    private void Update()
    {
        if (IsJumpCheck)
            IsCanJumpCheck();
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(ResetHorizontalMove));
    }
}