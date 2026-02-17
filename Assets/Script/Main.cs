
public class Main : SingleMonoAutoBehavior<Main>
{
    public LanguageType CurrentLanguageType;
    public static string PlayerName = "Player";
    protected override void Awake()
    {
        base.Awake();
    
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    void Start()
    {
        UImanager.Instance.ShowPanel<RoomPanel>();
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AKM", () => { Player.LocalPlayer.SpawnAndPickGun("AKM"); },"枪械获取");
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
        Developer_GUITestManger.Instance.RegisterGuiButton("打开军械库面板", () => { UImanager.Instance.ShowPanel<ArmamentPanel>(); },"面板测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("打开设置面板", () => { UImanager.Instance.ShowPanel<SettingPanel>(); }, "面板测试");
        Developer_GUITestManger.Instance.RegisterGuiButton("打开战备配置面板", () => { UImanager.Instance.ShowPanel<EquipmentConfigurationPanel>(); }, "面板测试");
        Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("打开当前调试信息", "关闭当前调试信息", () => { Developer_GUITestManger.Instance.IsShowAllInfo(true); }, () => { Developer_GUITestManger.Instance.IsShowAllInfo(false); }); 
        Developer_GUITestManger.Instance.RegisterGuiButton("清理烟雾", () => { FluidController.Instance.ClearTexture(); });
    }


    private void Update()
    {

    }
}
public enum LanguageType
{
    Chinese,
    English,
}

public enum GameMode//游戏模式
{
    Team_Battle,//团队竞技
    Control_Point,//站点模式
    Bomb_Mode//爆破模式
}
