using UnityEngine;
using UnityEngine.EventSystems;

public class AimButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool IsPressed = false;
    // 单击模式专用：记录当前是否处于瞄准状态
    private bool IsInAimState = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        // 基础安全检查
        if (Player.LocalPlayer == null || Player.LocalPlayer.myInputSystem == null)
            return;
        if (PlayerAndGameInfoManger.Instance == null)
            return;

        // 根据设置判断模式
        if (PlayerAndGameInfoManger.Instance.IsUseSinglePress_AimButton)
        {
            // 【单击切换模式】
            if (IsInAimState)
            {
                // 当前在瞄准 -> 退出
                Player.LocalPlayer.myInputSystem.AimStateExit();
                IsInAimState = false;
            }
            else
            {
                // 当前没瞄准 -> 进入
                if (Player.LocalPlayer.myInputSystem.CheckAndHandleAim())
                {
                    IsInAimState = true;
                }
            }
        }
        else
        {

            if (Player.LocalPlayer.myInputSystem.CheckAndHandleAim())
            {
                IsPressed = true;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 基础安全检查
        if (PlayerAndGameInfoManger.Instance == null)
            return;

        // 如果是单击模式，直接跳过
        if (PlayerAndGameInfoManger.Instance.IsUseSinglePress_AimButton)
            return;

        if (Player.LocalPlayer != null && Player.LocalPlayer.myInputSystem != null)
        {
            Player.LocalPlayer.myInputSystem.AimStateExit();
            IsPressed = false;
        }
        else
        {
            IsPressed = false;
        }
    }

    private void Update()
    {
        // 基础安全检查
        if (PlayerAndGameInfoManger.Instance == null)
            return;

        // 如果是单击模式，直接跳过
        if (PlayerAndGameInfoManger.Instance.IsUseSinglePress_AimButton)
            return;

        if (IsPressed)
        {
            if (Player.LocalPlayer == null || Player.LocalPlayer.myInputSystem == null)
            {
                IsPressed = false;
                return;
            }

            if (!Player.LocalPlayer.myInputSystem.UpdateCheckAimState())
            {
                IsPressed = false;
            }
        }
    }
}