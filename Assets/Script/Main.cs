
public class Main : SingleMonoAutoBehavior<Main>
{
    public LanguageType CurrentLanguageType;
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
        //Developer_GUITestManger.Instance.RegisterGuiButton("播放对话1", () => { DialogueManager.Instance.StartPlayDialogue(1, DialoguePlayType.AutoPlay); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("播放对话2", () => { DialogueManager.Instance.StartPlayDialogue(2); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("发送一条消息1", () => { SendMessageManger.Instance.SendMessage("这是一条测试消息持续2秒", 2f); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("打开警告面板", () => { WarnTriggerManager.Instance.TriggerDoubleInteractionWarn("22", "22", () => { }, () => { }); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("打开标题面板", () => { UImanager.Instance.ShowPanel<ShowTopicPanel>().SetDurationTime(3); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("摄像机震动", () => { MyCameraControl.Instance.AddTimeBasedShake(1, 1); });
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AKM", () => { Player.LocalPlayer.SpawnAndPickGun("AKM"); },"枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械FAMAS", () => { Player.LocalPlayer.SpawnAndPickGun("FAMAS"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械SCAR-L", () => { Player.LocalPlayer.SpawnAndPickGun("SCAR-L"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械417", () => { Player.LocalPlayer.SpawnAndPickGun("417"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械P90", () => { Player.LocalPlayer.SpawnAndPickGun("P90"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械Vector-45", () => { Player.LocalPlayer.SpawnAndPickGun("Vector-45"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械UZI", () => { Player.LocalPlayer.SpawnAndPickGun("UZI"); }, "枪械获取");
        Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("打开设置面板", "关闭设置面板", () => { UImanager.Instance.ShowPanel<SettingPanel>(); },() => { UImanager.Instance.HidePanel<SettingPanel>(); } );
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