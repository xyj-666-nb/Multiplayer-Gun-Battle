using Mirror;
using UnityEngine;

public class Main : SingleMonoAutoBehavior<Main>
{
    public LanguageType CurrentLanguageType;
    public static string PlayerName = "Player";

    [Header("Network配置")]
    public CustomNetworkManager customNetworkManager;

    public Team CurrentTeam;//当前的队伍


    protected override void Awake()
    {
        base.Awake();

        customNetworkManager = customNetworkManager ?? FindObjectOfType<CustomNetworkManager>();

        if (customNetworkManager == null)
        {
            Debug.LogError("[Main] 场景中未找到CustomNetworkManager组件！请将原生NetworkManager替换为CustomNetworkManager");
            return;
        }

        CustomNetworkManager.OnServerStartedEvent += OnServerStarted;
        CustomNetworkManager.OnServerStoppedEvent += OnServerStopped;

        CustomNetworkManager.OnClientConnectedSuccess += OnClientConnectedSuccess;

        Debug.Log("[Main] 已监听CustomNetworkManager事件，服务端启动时将自动生成重生管理器");
    }

    /// <summary>
    /// 服务端启动时，自动生成全局重生管理器
    /// </summary>
    private void OnServerStarted()
    {
        // 双重校验：仅服务端执行
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[Main] 非服务端环境，跳过重生管理器生成");
            return;
        }

        // 生成重生管理器
        PlayerRespawnManager.SpawnRespawnManager();
    }

    /// <summary>
    /// 服务端停止时，销毁重生管理器
    /// </summary>
    private void OnServerStopped()
    {
        if (!NetworkServer.active)
            return;

        PlayerRespawnManager.DestroyRespawnManager();
        Debug.Log("[Main] 服务端已停止，销毁重生管理器");
    }

    /// <summary>
    ///客户端连接服务器成功时执行
    /// </summary>
    private void OnClientConnectedSuccess()
    {
        // 打开玩家准备面板
        UImanager.Instance.ShowPanel<PlayerPreparaPanel>();
        Debug.Log("[Main] 客户端连接房间成功，已打开玩家准备面板");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 保留原有事件移除
        CustomNetworkManager.OnServerStartedEvent -= OnServerStarted;
        CustomNetworkManager.OnServerStoppedEvent -= OnServerStopped;
        CustomNetworkManager.OnClientConnectedSuccess -= OnClientConnectedSuccess;
    }

    void Start()
    {
        // 原有UI和测试按钮逻辑
        UImanager.Instance.ShowPanel<RoomPanel>();

        // 枪械测试按钮
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AKM", () => { Player.LocalPlayer.SpawnAndPickGun("AKM"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械FAMAS", () => { Player.LocalPlayer.SpawnAndPickGun("FAMAS"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械SCAR-L", () => { Player.LocalPlayer.SpawnAndPickGun("SCAR-L"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械417", () => { Player.LocalPlayer.SpawnAndPickGun("417"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械P90", () => { Player.LocalPlayer.SpawnAndPickGun("P90"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械Vector-45", () => { Player.LocalPlayer.SpawnAndPickGun("Vector-45"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械UZI", () => { Player.LocalPlayer.SpawnAndPickGun("UZI"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AWP", () => { Player.LocalPlayer.SpawnAndPickGun("AWP"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械M110", () => { Player.LocalPlayer.SpawnAndPickGun("M110"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械98K", () => { Player.LocalPlayer.SpawnAndPickGun("98K"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AUG", () => { Player.LocalPlayer.SpawnAndPickGun("AUG"); }, "枪械获取");

        // 面板测试按钮
        Developer_GUITestManger.Instance.RegisterGuiButton("打开军械库面板", () => { UImanager.Instance.ShowPanel<ArmamentPanel>(); }, "面板测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("打开设置面板", () => { UImanager.Instance.ShowPanel<SettingPanel>(); }, "面板测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("打开战备配置面板", () => { UImanager.Instance.ShowPanel<EquipmentConfigurationPanel>(); }, "面板测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("打开地图选择面板", () => { UImanager.Instance.ShowPanel<MapChoosePanel>(); }, "面板测试");

        Developer_GUITestManger.Instance.RegisterGuiButton("使用绿针", () => { Player.LocalPlayer.MyHandControl.TriggerInjection(TacticType.Green_injection); }, "战术设备测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("使用黄色针剂", () => { Player.LocalPlayer.MyHandControl.TriggerInjection(TacticType.Yellow_injection); }, "战术设备测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("拿出烟雾弹", () => { Player.LocalPlayer.MyHandControl.TriggerThrowObj(TacticType.Smoke); }, "战术设备测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("拿出手雷弹", () => { Player.LocalPlayer.MyHandControl.TriggerThrowObj(TacticType.Grenade); }, "战术设备测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("回收投掷物", () => { Player.LocalPlayer.MyHandControl.CmdRecycleThrowObj(); }, "战术设备测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("发射投掷物", () => { Player.LocalPlayer.MyHandControl.LaunchCurrentThrowObj(); }, "战术设备测试");

        Developer_GUITestManger.Instance.RegisterGuiButton("发送全局消息(持续两秒)", () => { PlayerRespawnManager.Instance.SendGlobalMessage("这是一条全局消息", 2f); }, "功能测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("发送个人消息(持续两秒)", () => { SendMessageManger.Instance.SendMessage("这是一条个人消息", 2f); }, "功能测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("获取当前的战备", () => { PlayerAndGameInfoManger.Instance.EquipCurrentSlot(); }, "功能测试");
        // 调试按钮
        Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("打开当前调试信息", "关闭当前调试信息", () => { Developer_GUITestManger.Instance.IsShowAllInfo(true); }, () => { Developer_GUITestManger.Instance.IsShowAllInfo(false); });
        Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("收起枪械", "拿起枪械", () => { Player.LocalPlayer.MyHandControl.SetHolsterState(true); }, () => { Player.LocalPlayer.MyHandControl.SetHolsterState(false); });
        Developer_GUITestManger.Instance.RegisterGuiButton("清理烟雾", () => { FluidController.Instance.ClearTexture(); });
    }

    private void Update()
    {
        // 预留Update逻辑
    }

 
}

public enum LanguageType
{
    Chinese,
    English,
}

//队伍
public enum Team
{
    Red,//红队
    Blue,//蓝队

}


public enum GameMode//游戏模式
{
    Team_Battle,//团队竞技
    Control_Point,//站点模式
    Bomb_Mode//爆破模式
}

