using Mirror;
using System;
using System.Collections;
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
    private int ViewTaskID=-1;//视野缩放任务ID
    private float ChangeSpeed_View = 4;//缩放视野的速度
    #endregion

    public bool IsInteractButtonTrigger=false;//是否触发交互按钮
    public float InteractCoolTime = 1f;//交互键的冷却时间
    public bool IsInCooldown = false;

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
        // 仅本地玩家监听事件
        if (isLocalPlayer)
        {
            Myplayer.OnGroundStateChanged -= OnGroundStateChangedHandler;
            Myplayer.OnGroundStateChanged += OnGroundStateChangedHandler;
        }
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
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.RightMove, null, null, Move_End, Move_Right_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.LeftMove, null, null, Move_End, Move_Left_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Attack, Shoot_Start, null, null, Shoot_Continue);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Reload, Reload_start, null, null);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.PickUpGun, PickUpGnn_Start, null, null);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.DiscardGun, DiscardGun_Start, null, null);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.GunAim, GunAim_Start, null, GunAim_End, GunAim_UpdateCheck);
            InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Interact, Interact_Start, null, null, null);
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

    // 事件处理器，只有状态变化时才执行
    private void OnGroundStateChangedHandler(bool isGrounded)
    {
        if (isGrounded)
        {
            CmdPlaySound("Music/正式/落地");
        }
    }
    public void Jump_Start(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (IsCanJump)
        {
            //先重置速度(Y)
            Myplayer.MyRigdboby.velocity = new Vector2(Myplayer.MyRigdboby.velocity.x, 0);
            float jumpPower = Myplayer.MyHandControl != null && Myplayer.MyHandControl.IsEnterAim
                ? MyStats.AimJumpPower
                : MyStats.JumpPower;

            if (Myplayer.currentGun != null && Myplayer.currentGun.IsInShoot)
                Myplayer.MyRigdboby.AddForce(new Vector2(0, jumpPower / 2), ForceMode2D.Impulse);
            else
                Myplayer.MyRigdboby.AddForce(new Vector2(0, jumpPower), ForceMode2D.Impulse);

            IsCanJump = false;
            CmdPlaySound("Music/正式/起跳");
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

            int targetFacingDir = -Myplayer.FacingDir; // 墙跳意味着方向要反转

            Myplayer.FacingDir = targetFacingDir;     
            Myplayer.ApplyFlipVisual(targetFacingDir); 
            Myplayer.CmdRequestFlip(targetFacingDir);
            CmdPlaySound("Music/正式/起跳");
        }

        CountDownManager.Instance.CreateTimer(false, 50, () => { IsJumpCheck = true; });//0.2秒后才开启检测
        //播放跳跃音效
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
            //播放落地音效
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
        if (!CheckCommonTriggerCondition())
            return;
        if (!IsCanHorizontalMove)
            return;
        //在这里也设置一下表情的朝向
        Myplayer.MyExpressionSystem.SetParentRectDir(targetFacingDir);
        float dynamicMaxSpeed = Myplayer.MyHandControl != null && Myplayer.MyHandControl.IsEnterAim
            ? MyStats.AimMoveMaxSpeed : MyStats.MaxXSpeed;

        if (Myplayer.currentGun != null && Myplayer.currentGun.IsInShoot)
            dynamicMaxSpeed /= 2;

        float targetVelocityX = moveDirection * dynamicMaxSpeed;

        float currentVelocityX = Myplayer.MyRigdboby.velocity.x;
        float smoothedVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, 15f * Time.deltaTime);

        Myplayer.MyRigdboby.velocity = new Vector2(smoothedVelocityX, Myplayer.MyRigdboby.velocity.y);

        // 只有实际有水平移动，才开启脚步协程
        bool isActuallyMoving = Mathf.Abs(Myplayer.MyRigdboby.velocity.x) > 0.1f;
        if (isActuallyMoving && !IsInMove)
        {
            IsInMove = true;
            // 安全开启协程：先停旧的，再开新的，绝对避免重复开启
            if (SoundMoveCoroutine != null)
            {
                StopCoroutine(SoundMoveCoroutine);
                SoundMoveCoroutine = null;
            }
            SoundMoveCoroutine = StartCoroutine(playerMoveMusic());
        }
        //没实际移动时，直接结束脚步状态
        else if (!isActuallyMoving && IsInMove)
        {
            Move_End(default);
        }

        if (Myplayer.FacingDir != targetFacingDir)
        {
            // 本地立刻强行表现翻转，不依赖服务器回传
            Myplayer.FacingDir = targetFacingDir;
            Myplayer.ApplyFlipVisual(targetFacingDir);
            Myplayer.CmdRequestFlip(targetFacingDir);
        }
    }

    //移动协程
    private Coroutine SoundMoveCoroutine;
    private bool IsInMove = false;
    [Header("脚步基础配置")]
    public float BaseStepInterval = 0.6f; // 满速下的脚步基础间隔（秒）
    public float MinStepInterval = 0.3f;  // 最快脚步间隔
    public float MaxStepInterval = 1.2f;  // 最慢脚步间隔
    private float MoveSoundTimer = 0f;

    private string SoundPath_Move = "Music/正式/脚步";
    private int SoundId = 1;
    private int currentStepSoundIndex = 1; // 脚步轮换索引

    IEnumerator playerMoveMusic()
    {
        // 每次开启协程，重置计时器和索引，避免残留值
        MoveSoundTimer = 0f;
        currentStepSoundIndex = 1;

        while (IsInMove)
        {
            // 只有在地面+实际有水平移动，才累加计时器
            bool isGrounded = Myplayer.IsGroundDetected();
            bool isMoving = Mathf.Abs(Myplayer.MyRigdboby.velocity.x) > 0.1f;

            if (isGrounded && isMoving)
            {
                // 动态计算脚步间隔，匹配当前移速
                float currentMoveSpeed = Mathf.Abs(Myplayer.MyRigdboby.velocity.x);
                float maxSpeed = MyStats.MaxXSpeed;
                // 移速越快，间隔越短；移速越慢，间隔越长
                float dynamicInterval = BaseStepInterval * (maxSpeed / Mathf.Max(currentMoveSpeed, 0.1f));
                // 限制间隔上下限，防止极端情况
                dynamicInterval = Mathf.Clamp(dynamicInterval, MinStepInterval, MaxStepInterval);

                MoveSoundTimer += Time.deltaTime;
                if (MoveSoundTimer >= dynamicInterval)
                {
                    // 1→2→3循环轮换播放
                    SoundId = currentStepSoundIndex;
                    currentStepSoundIndex++;
                    if (currentStepSoundIndex > 3)
                    {
                        currentStepSoundIndex = 1;
                    }

                    CmdPlaySound(SoundPath_Move + SoundId);
                    MoveSoundTimer = 0f; // 重置计时器
                }
            }
            else
            {
                // 空中/没移动时，暂停计时器，不做累加
                MoveSoundTimer = 0f;
            }

            // 每帧等待，保证协程正常执行
            yield return null;
        }
    }

    public void Move_End(InputAction.CallbackContext Content)
    {
        IsInMove = false;

        // 安全停止协程，彻底清理状态
        if (SoundMoveCoroutine != null)
        {
            StopCoroutine(SoundMoveCoroutine);
            SoundMoveCoroutine = null;
        }
        // 重置计时器和索引，下次移动从零开始
        MoveSoundTimer = 0f;
        currentStepSoundIndex = 1;
    }

    // 左移
    public void Move_Left_Continue(CustomInputContext Content)
    {
        if (!GlobalPictureFlipManager.Instance.IsFlipped)
            HandleMoveLogic(-1, -1); // 左移：方向=-1，目标朝向=-1
        else
            HandleMoveLogic(1, 1);
    }

    // 右移
    public void Move_Right_Continue(CustomInputContext Content)
    {
        if (!GlobalPictureFlipManager.Instance.IsFlipped)
            HandleMoveLogic(1, 1);// 右移：方向=1，目标朝向=1
        else
            HandleMoveLogic(-1, -1);
    }
    #endregion

    #region 全局音效播放3D
    [Command]
    public void CmdPlaySound(string SoundPath)
    {
        RpcPlaySound(SoundPath);
    }

    [ClientRpc]
    void RpcPlaySound(string SoundPath)
    {
        MusicManager.Instance.PlayEffect3D(SoundPath, 20f, owner: this.transform);//默认两倍
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
        {
            //如果正在处于瞄准状态，先退出瞄准状态
            if (IsEnterAim)
            {
                AimState_Exit();
                IsEnterAim = false;
            }
            Myplayer.currentGun.TriggerReload();//调用换弹
            //通知UI开始显示
            UImanager.Instance.GetPanel<PlayerPanel>()?.EnterReloadPrompt(Myplayer.currentGun.gunInfo.ReloadTime);//传入换弹时间
        }
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
        if (Myplayer != null
      && Myplayer.currentGun != null
      && !Myplayer.currentGun.IsInReload
      && !Myplayer.currentGun.IsInShoot)
        {
            //触发丢弃
            Myplayer.DropCurrentGun();//丢弃当前的枪械
        }
        else
        {
        }
    }
    #endregion

    #region 枪械瞄准控制逻辑
    public bool IsEnterAim=false;
    private void GunAim_Start(InputAction.CallbackContext Content)
    {
        CheckAndHandleAim();
    }
    //统一封装方便给外部调用
    public bool CheckAndHandleAim()
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return false;

        if (Myplayer.currentGun == null || Myplayer.currentGun.IsInReload)
            return false;

        IsEnterAim = true;
        AimState_Enter();
        //播放瞄准音效
        MusicManager.Instance.PlayEffect("Music/正式/瞄准");
        return true;
    }


    //瞄准状态持续检测
    private void GunAim_UpdateCheck(CustomInputContext Content)
    {
        UpdateCheckAimState();
    }

    public bool UpdateCheckAimState()
    {
        if (Myplayer.currentGun == null || Myplayer.currentGun.IsInReload)
        {
            if (IsEnterAim)
            {
                AimState_Exit();
                IsEnterAim = false;
                return true; // 成功退出瞄准状态
            }
            IsEnterAim = false;
            return false;
        }
        return true;
    }

    private void AimState_Enter()
    {
        Debug.Log("进入瞄准状态");
        //触发枪进入瞄准状态
        Myplayer.currentGun.ChangeAimState(true);
        //手动触发toggle按钮
        ButtonGroupManager.Instance.ManualSelectToggleButton(PlayerPanel.GetAimButtonButtonGroupName());
        //触发手部进入瞄准状态
        Myplayer.MyHandControl.SetAimState(true);
        //缩放视野
        MyCameraControl.Instance.ResetZoomTask(ViewTaskID);
        ViewTaskID = MyCameraControl.Instance.AddZoomTask_ByPercent_TemporaryManual(1 + Myplayer.myStats.AimViewBonus, ChangeSpeed_View, Player.LocalPlayer.CameraSizeBaseValue);//对记录的基础摄像机大小进行缩放
    }


    public bool AimStateExit()
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return false;

        if (Myplayer.currentGun == null)
            return false;

        IsEnterAim = false;
        AimState_Exit();
        return true;
    }

    private void AimState_Exit()
    {
        Debug.Log("退出瞄准状态");
        //触发枪退出瞄准状态
        Myplayer.currentGun.ChangeAimState(false);
        //手动退出触发toggle按钮
        ButtonGroupManager.Instance.ManualCancelToggleButton(PlayerPanel.GetAimButtonButtonGroupName());
        //触发手部退出瞄准状态
        Myplayer.MyHandControl.SetAimState(false);
        //停止任务
        MyCameraControl.Instance.ResetZoomTask(ViewTaskID);
        CmdPlaySound("Music/正式/瞄准");
    }

    private void GunAim_End(InputAction.CallbackContext Content)
    {
        AimStateExit();
    }
    #endregion

    #region 交互逻辑
    public void Interact_Start(InputAction.CallbackContext Content)
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (IsInCooldown)
            return;

        IsInCooldown = true;
        IsInteractButtonTrigger = true;
        //开启计时器
        SimpleAnimatorTool.Instance.StartFloatLerp(1,0, InteractCoolTime, (v) => {
            UImanager.Instance.GetPanel<PlayerPanel>()?.UpdateInteractButtonCool(v);//进行UI更新  
        }, () => { IsInCooldown = false; });

        StartCoroutine(ResetInteractTriggerAfterOneFrame());
    }

    //提供给外部的触发按钮
    public void ExtraInteractTrigger()
    {
        // 第一步：通用校验
        if (!CheckCommonTriggerCondition())
            return;

        if (IsInCooldown)
            return;

        IsInCooldown = true;
        IsInteractButtonTrigger = true;
        //开启计时器
        SimpleAnimatorTool.Instance.StartFloatLerp(1, 0, InteractCoolTime, (v) => {
            UImanager.Instance.GetPanel<PlayerPanel>()?.UpdateInteractButtonCool(v);//进行UI更新  
        }, () => { IsInCooldown = false; });

        StartCoroutine(ResetInteractTriggerAfterOneFrame());
    }

    /// <summary>
    /// 协程：等待一帧后重置交互触发标记
    /// </summary>
    /// <returns></returns>
    private IEnumerator ResetInteractTriggerAfterOneFrame()
    {
        yield return null;

        IsInteractButtonTrigger = false;
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

    #region 移动端拉杆专用接口
    /// <summary>
    /// 移动端拉杆直接调用的移动接口
    /// </summary>
    /// <param name="direction">移动方向：-1=左，0=停止，1=右</param>
    public void SetMoveDirection(int direction)
    {
        // 停止移动：直接调用原有结束逻辑
        if (direction == 0)
        {
            Move_End(default);
            return;
        }

        // 完全复用你原有左右移动的翻转逻辑
        if (!GlobalPictureFlipManager.Instance.IsFlipped)
        {
            if (direction == 1)
                HandleMoveLogic(1, 1); // 右移
            else if (direction == -1)
                HandleMoveLogic(-1, -1); // 左移
        }
        else
        {
            // 画面翻转时，方向反转，和你原有逻辑完全一致
            if (direction == 1)
                HandleMoveLogic(-1, -1);
            else if (direction == -1)
                HandleMoveLogic(1, 1);
        }
    }
    #endregion
}