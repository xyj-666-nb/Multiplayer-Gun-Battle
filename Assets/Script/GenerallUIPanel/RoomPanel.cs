using Mirror.Transports.Encryption;
using UnityEngine.Events;

public class RoomPanel : BasePanel
{
    public override void Awake()
    {
        base.Awake();
        //本面板打开的时候清除残留面板

        UImanager.Instance.HidePanel<PlayerPanel>();
        UImanager.Instance.HidePanel<PlayerPreparaPanel>();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "Button_CreateRoom":
                UImanager.Instance.ShowPanel<CreateRoomPanel>();
                UImanager.Instance.HidePanel<RoomPanel>();

                break;
            case "Button_EnterRoom":
                UImanager.Instance.ShowPanel<EnterRoomPanel>();
                UImanager.Instance.HidePanel<RoomPanel>();
                break;
            case "ExitButton":
                UImanager.Instance.HidePanel<RoomPanel>();
                UImanager.Instance.ShowPanel<GameStartPanel>();//游戏开始面板
                //回到选择视角
                ModeChooseSystem.instance.EnterSystem();
                break;
        }

    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void Update()
    {
        base.Update();
    }
}
