using Mirror;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static InputInfoManager;

public class MyPlayerInput : NetworkBehaviour
{
    private Player Myplayer;
    private playerStats MyStats;

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
            Debug.Log("MyPlayerInput：本地玩家输入事件绑定成功！");
        }
    }

    #region 跳跃逻辑
    private bool IsCanJump = true;
    private bool IsJumpCheck = false;
    public void Jump_Start(InputAction.CallbackContext Content)
    {
        if (!isLocalPlayer || !IsCanJump)
            return;

        if (Myplayer.currentGun != null && Myplayer.currentGun.IsInShoot)
            Myplayer.MyRigdboby.AddForce(new Vector2(0, MyStats.JumpPower / 2), ForceMode2D.Impulse);//跳跃力变小
        else
            Myplayer.MyRigdboby.AddForce(new Vector2(0, MyStats.JumpPower), ForceMode2D.Impulse);

        IsCanJump = false;
        CountDownManager.Instance.CreateTimer(false, 100, () => { IsJumpCheck = true; });
    }

    public void Jump_Continue(CustomInputContext Content)
    {
        if (!isLocalPlayer)
            return;
        if (Myplayer.MyRigdboby != null && Myplayer.MyRigdboby.velocity.y > 0)
            Myplayer.MyRigdboby.gravityScale = 0.75f;
    }

    public void Jump_End(InputAction.CallbackContext Content)
    {
        if (!isLocalPlayer)
            return;

        if (Myplayer.MyRigdboby != null)
            Myplayer.MyRigdboby.gravityScale = 1f;
    }

    public void IsCanJumpCheck()
    {
        if (!isLocalPlayer)
            return;

        if (Myplayer.IsGroundDetected())
        {
            IsCanJump = true;
            IsJumpCheck = false;
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
        if (!isLocalPlayer)
            return;

        float finalMovePower = MyStats.movePower;
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
        float dynamicMaxSpeed = MyStats.MaxXSpeed;
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
        if (!isLocalPlayer)
            return;
        Debug.Log("开始射击");
    }

    public void Shoot_Continue(CustomInputContext Content)
    {
        if (!isLocalPlayer || Myplayer.currentGun == null)
            return;

        //判断射击条件,如果是连发枪就自动检测并自动触发射击
        if(Myplayer.currentGun.IsCanShoot())
        {
            Myplayer.currentGun.TriggerSingleShoot();//触发射击
        }
    }
    #endregion

    #region 换弹相关
    public void Reload_start(InputAction.CallbackContext Content)//开始换弹
    {
        if (!isLocalPlayer || Myplayer.currentGun == null)
            return;

        //判断当前换弹条件
        if (Myplayer.currentGun.IsCanReload())
            Myplayer.currentGun.TriggerReload();//调用换弹
    }
    #endregion

    #region 拾取枪械控制逻辑
    public void PickUpGnn_Start(InputAction.CallbackContext Content)
    {
        if (!isLocalPlayer)
            return;

        //判断条件
        if (Myplayer.CurrentTouchGun != null)
        {
            Myplayer.PickUpSceneGun();
        }
    }
    #endregion

    #region 丢弃枪械控制逻辑
    public void DiscardGun_Start(InputAction.CallbackContext Content)
    {
        if (!isLocalPlayer)
            return;

        Debug.Log("正在判断丢弃条件");
        if (Myplayer.currentGun != null && !Myplayer.currentGun.IsInReload && !Myplayer.currentGun.IsInShoot)
        {
            //触发丢弃
            Myplayer.DropCurrentGun();//丢弃当前的枪械
        }
    }
    #endregion

    private void Update()
    {
        if (IsJumpCheck)
            IsCanJumpCheck();
    }
}