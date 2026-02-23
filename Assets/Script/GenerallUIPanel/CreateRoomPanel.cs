using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomPanel : BasePanel
{
    [SerializeField] private TMP_InputField InputField;           // 房间名输入框
    public static string CurrentRoomName;                         // 当前的房间名

    [SerializeField] private LanRoomHost Host;// 房主组件引用(也是广播器)
    [SerializeField] private TMP_InputField InputField_PlayerName;// 房主名输入框
    public static string CurrentPlayerName;                       // 当前的房主名

    private GameMode currentGameMode = GameMode.Team_Battle;     // 当前的游戏模式（后面通过检测输入而引入）

    [Header("房间设置的数据")]
    public int GameTime;//游戏时间
    public int GameGoalScore;//游戏目标分数


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

                Host.CreateRoom(roomName, hostName, GameTime, GameGoalScore); // 传房间名、房主名,人数（后面还需要传入游戏模式等等）
                ReserveHost(); // 分离广播器
                UImanager.Instance.HidePanel<CreateRoomPanel>();//隐藏创建房间的面板
                                                                //在这里进行注册数据
                CountDownManager.Instance.CreateTimer(false, 1000, () => { PlayerRespawnManager.Instance.InitGoalScoreCount(GameGoalScore,GameTime);
                
                
                });
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
        //记录一下当前的物体
        CustomNetworkManager.Instance.BroadcasterObj = Host.gameObject;//记录这个物体
    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void SpecialAnimator_Hide()
    {

    }

    private string ScoreChooseName = "GameScoreChoose";
    private string TimeChooseName= "GameTimeChoose";
    public override void Awake()
    {
        base.Awake();
        //注册选择按钮

        ButtonGroupManager.Instance.AddRadioButtonToGroup(ScoreChooseName, controlDic["Button_10Score"] as Button, () => { GameGoalScore = 10; });
        ButtonGroupManager.Instance.AddRadioButtonToGroup(ScoreChooseName, controlDic["Button_15Score"] as Button, () => { GameGoalScore = 15; });
        ButtonGroupManager.Instance.AddRadioButtonToGroup(ScoreChooseName, controlDic["Button_30Score"] as Button, () => { GameGoalScore = 30; });
        //设置默认选择
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ScoreChooseName);


        ButtonGroupManager.Instance.AddRadioButtonToGroup(TimeChooseName, controlDic["Button_5minute"] as Button, () => { GameTime = 5; });
        ButtonGroupManager.Instance.AddRadioButtonToGroup(TimeChooseName, controlDic["Button_10minute"] as Button, () => { GameTime = 10; });
        ButtonGroupManager.Instance.AddRadioButtonToGroup(TimeChooseName, controlDic["Button_15minute"] as Button, () => { GameTime = 15; });
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(TimeChooseName);
    }
}
