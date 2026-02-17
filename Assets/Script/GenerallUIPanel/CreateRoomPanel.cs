using TMPro;
using UnityEngine;

public class CreateRoomPanel : BasePanel
{
    [SerializeField] private TMP_InputField InputField;           // 房间名输入框
    public static string CurrentRoomName;                         // 当前的房间名

    [SerializeField] private LanRoomHost Host;// 房主组件引用(也是广播器)
    [SerializeField] private TMP_InputField InputField_PlayerName;// 房主名输入框
    public static string CurrentPlayerName;                       // 当前的房主名

    private GameMode currentGameMode = GameMode.Team_Battle;     // 当前的游戏模式（后面通过检测输入而引入）

  
    public override void Start()
    {
        base.Start();

        // 实时记录输入（检测玩家的输入）
        InputField.onValueChanged.AddListener(str => CurrentRoomName = str);
        InputField_PlayerName.onValueChanged.AddListener(str => CurrentPlayerName = str);
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        switch (controlName)
        {
            case "CreateButton":
                // 防止为空，给个默认（获取当前的信息）
                var roomName = string.IsNullOrWhiteSpace(CurrentRoomName) ? "Room" : CurrentRoomName;
                var hostName = string.IsNullOrWhiteSpace(CurrentPlayerName) ? "Host" : CurrentPlayerName;
                Main.PlayerName = hostName;//赋值当前的名字
                Debug.Log(Main.PlayerName);

                Host.CreateRoom(roomName, hostName); // 传房间名、房主名,人数（后面还需要传入游戏模式等等）
                ReserveHost(); // 分离广播器
                UImanager.Instance.HidePanel<CreateRoomPanel>();//隐藏创建房间的面板
                break;

            case "ExitButton":
                UImanager.Instance.ShowPanel<RoomPanel>();
                UImanager.Instance.HidePanel<CreateRoomPanel>();
                break;
        }
    }

    public void ReserveHost()
    {
        // 把信号播放器分离出去
        Host.gameObject.transform.parent = null;//把房主组件从当前面板的层级中分离出去，避免在切换面板时被销毁
    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void SpecialAnimator_Hide()
    {

    }
}
