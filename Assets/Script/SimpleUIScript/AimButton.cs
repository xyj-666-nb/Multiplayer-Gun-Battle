using UnityEngine;
using UnityEngine.EventSystems;

public class AimButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool IsPressed = false;
    // 记录当前是否处于瞄准状态
    private bool IsInAimState = false;
    private bool IsRemoveScript = false;


    public void OnPointerDown(PointerEventData eventData)
    {
        // 根据设置判断模式
        if (PlayerAndGameInfoManger.Instance.IsUseSinglePress_AimButton)
        {
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

        if(!IsRemoveScript)
        {
            //持续获取
            if (GetComponent<EventTrigger>())
            {
                GetComponent<EventTrigger>().enabled = false;
                IsRemoveScript =true;
            }
        }

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