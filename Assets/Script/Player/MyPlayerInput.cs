using UnityEngine;
using UnityEngine.InputSystem;
using static InputInfoManager;

public class MyPlayerInput : MonoBehaviour
{
    //玩家控制器
    private Player Myplayer;
    private playerStats MyStats;

    private void Awake()
    {
        MyStats = GetComponent<playerStats>();
        //开始注册玩家行为
        InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Jump, Jump_Start, null, Jump_End, Jump_Continue);
        InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.RightMove, move_Start, null, move_End, Move_Right_Continue); // 修正方法名
        InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.LeftMove, move_Start, null, move_End, Move_Left_Continue);
        InputInfoManager.Instance.RegisterInputLogicEvent(E_InputAction.Attack, null, null, null, Shoot_Continue);
    }

    public void TriggerBinding()
    {
        Myplayer = Player.instance;
    }

    #region 跳跃逻辑
    private bool IsCanJump = true;
    private bool IsJumpCheck = false;
    public void Jump_Start(InputAction.CallbackContext Content)
    {
        if (!Myplayer.isLocalPlayer || !IsCanJump)
            return;

        Debug.Log("请求服务器跳跃");

        Myplayer.CmdAddJumpForce(MyStats.JumpPower);//客户端请求服务器加力
        IsCanJump = false;
        CountDownManager.Instance.CreateTimer(false, 100, () => { IsJumpCheck = true; });
    }

    public void Jump_Continue(CustomInputContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;

        // 重力缩放修改也需请求服务器（可选：简单场景可客户端临时改，服务器同步）
        if (Myplayer.MyRigdboby.velocity.y > 0)
            Myplayer.MyRigdboby.gravityScale = 0.75f;
    }

    public void Jump_End(InputAction.CallbackContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;
        Myplayer.MyRigdboby.gravityScale = 1f;
    }

    public void IsCanJumpCheck()
    {
        if (Myplayer.IsGroundDetected())
        {
            IsCanJump = true;
            IsJumpCheck = false;
        }
    }
    #endregion

    #region 移动相关
    public void move_Start(InputAction.CallbackContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;

        Debug.Log("开始移动");
    }

    public void move_End(InputAction.CallbackContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;

        Debug.Log("移动结束");
    }

    // 左移（修正方法名+逻辑）
    public void Move_Left_Continue(CustomInputContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;


        // 请求服务器施加左移力
        Myplayer.CmdAddMoveForce(MyStats.movePower, -1);
        // 请求服务器转向
        if (Myplayer.FacingDir != -1)
            Myplayer.CmdFlip();
    }

    // 右移（修正力的方向+请求服务器）
    public void Move_Right_Continue(CustomInputContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;

        // 请求服务器施加右移力
        Myplayer.CmdAddMoveForce(MyStats.movePower, 1);
        // 请求服务器转向
        if (Myplayer.FacingDir != 1)
            Myplayer.CmdFlip();
    }
    #endregion

    #region 射击相关
    public void Shoot_Continue(CustomInputContext Content)
    {
        if (!Myplayer.isLocalPlayer)
            return;
    }
    #endregion

    private void Update()
    {
        if (IsJumpCheck)
            IsCanJumpCheck();
    }
}