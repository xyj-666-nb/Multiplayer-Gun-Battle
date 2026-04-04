using Cinemachine;
using Localization;
using Mirror;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Android;
//using TapSDK.Login.Internal;

public class Main : SingleMonoAutoBehavior<Main>
{
    public LanguageType CurrentLanguageType;

    [Header("Network配置")]
    public CustomNetworkManager customNetworkManager;
    [Header("当前联机模式")]
    public NetworkMode CurrentMode;
 
    public Team CurrentTeam;//当前的队伍
    public GameStartCG CG;
    public CinemachineBrain brain;
    public CinemachineVirtualCamera AnimaVC;//动画虚拟相机
    [Header("CG开场动画")]
    public PlayableDirector StartTimeLine;
    public GameObject SkipCanvasGroup;//跳过Canvas

    [Header("是否启用TapTap服务")]
    public bool IsUseTapTapServer = true;

    public bool IsInSingleMode = false;//是否处于单人模式

    public void StartCG()
    {
        StartTimeLine.Play();
        SkipCanvasGroup.gameObject.SetActive(true);//激活
        CG.TriggerGameCG();
    }
    public void SwitchToAnimaVCFast()
    {
        if (AnimaVC == null) 
            return;

        AnimaVC.Priority = 999; // 优先级拉满
        AnimaVC.MoveToTopOfPrioritySubqueue(); // 强制插队到最前面
        if (brain != null)
        {
            // 设置过渡时间：0.1秒 
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.001f);
            Debug.Log("已切换到 AnimaVC，快速转场");
        }
    }

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
        AllMapManager.Instance.TriggerMap(MapType.StartCG, true);
        if(IsUseTapTapServer)
            UImanager.Instance.ShowPanel<TapTapLoginPanel>();
        else
            Main.Instance.StartCG();//开始游戏CG

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 安卓9.0+ 强制开启明文网络请求（解决联机时明文IP访问被拦截）
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject application = currentActivity.Call<AndroidJavaObject>("getApplication");
            
            // 获取安卓系统版本
            AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
            int sdkVersion = buildVersion.GetStatic<int>("SDK_INT");
            
            // Android 9.0 (API 28) 及以上需要手动开启明文流量
            if (sdkVersion >= 28)
            {
                AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>(
                    "getPackageInfo", Application.identifier, 0
                );
                AndroidJavaObject appInfo = packageInfo.Get<AndroidJavaObject>("applicationInfo");
                appInfo.Set<bool>("cleartextTrafficPermitted", true);
            }
            
            Debug.Log("安卓网络配置初始化成功，已允许明文流量");
        }
        catch (System.Exception e)
        {
            // 即使配置失败，也不影响游戏运行（仅打印警告）
            Debug.LogWarning("安卓网络配置初始化警告：" + e.Message);
        }
#endif



    }
    /// <summary>
    /// 服务端启动时，自动生成全局重生管理器
    /// </summary>
    private void OnServerStarted()
    {
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
        // // 原有UI和测试按钮逻辑
        // // 枪械测试按钮
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AKM", () => { Player.LocalPlayer.SpawnAndPickGun("AKM"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械FAMAS", () => { Player.LocalPlayer.SpawnAndPickGun("FAMAS"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械SCAR-L", () => { Player.LocalPlayer.SpawnAndPickGun("SCAR-L"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械417", () => { Player.LocalPlayer.SpawnAndPickGun("417"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械P90", () => { Player.LocalPlayer.SpawnAndPickGun("P90"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械Vector-45", () => { Player.LocalPlayer.SpawnAndPickGun("Vector-45"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械UZI", () => { Player.LocalPlayer.SpawnAndPickGun("UZI"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AWP", () => { Player.LocalPlayer.SpawnAndPickGun("AWP"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械M110", () => { Player.LocalPlayer.SpawnAndPickGun("M110"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械98K", () => { Player.LocalPlayer.SpawnAndPickGun("98K"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械AUG", () => { Player.LocalPlayer.SpawnAndPickGun("AUG"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械M249", () => { Player.LocalPlayer.SpawnAndPickGun("M249"); }, "枪械获取");
        // Developer_GUITestManger.Instance.RegisterGuiButton("给玩家分配枪械M762", () => { Player.LocalPlayer.SpawnAndPickGun("M762"); }, "枪械获取");
        // // 面板测试按钮
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开军械库面板", () => { UImanager.Instance.ShowPanel<ArmamentPanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开设置面板", () => { UImanager.Instance.ShowPanel<SettingPanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开战备配置面板", () => { UImanager.Instance.ShowPanel<EquipmentConfigurationPanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开地图选择面板", () => { UImanager.Instance.ShowPanel<MapChoosePanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开玩家准备面板", () => { UImanager.Instance.ShowPanel<PlayerPreparaPanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开自定义UI面板", () => { UImanager.Instance.ShowPanel<PlayerCustomPanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开画面设置面板", () => { UImanager.Instance.ShowPanel<ScreenSettingPanel>(); }, "面板测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("打开胜利设置面板", () => { UImanager.Instance.ShowPanel<GameSettlementPanel>(); }, "面板测试");

        // Developer_GUITestManger.Instance.RegisterGuiButton("使用绿针", () => { Player.LocalPlayer.MyHandControl.TriggerInjection(TacticType.Green_injection); }, "战术设备测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("使用黄色针剂", () => { Player.LocalPlayer.MyHandControl.TriggerInjection(TacticType.Yellow_injection); }, "战术设备测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("拿出烟雾弹", () => { Player.LocalPlayer.MyHandControl.TriggerThrowObj(TacticType.Smoke); }, "战术设备测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("拿出手雷弹", () => { Player.LocalPlayer.MyHandControl.TriggerThrowObj(TacticType.Grenade); }, "战术设备测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("回收投掷物", () => { Player.LocalPlayer.MyHandControl.CmdRecycleThrowObj(); }, "战术设备测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("发射投掷物", () => { Player.LocalPlayer.MyHandControl.LaunchCurrentThrowObj(); }, "战术设备测试");

        // Developer_GUITestManger.Instance.RegisterGuiButton("中文", () => { LocalizationManager.SwitchLanguage(Language.Chinese); }, "切换语言");
        // Developer_GUITestManger.Instance.RegisterGuiButton("英文", () => { LocalizationManager.SwitchLanguage(Language.English); }, "切换语言");

        // Developer_GUITestManger.Instance.RegisterGuiButton("发送全局消息(持续两秒)", () => { PlayerRespawnManager.Instance.SendGlobalMessage("这是一条全局消息", 2f); }, "功能测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("发送个人消息(持续两秒)", () => { SendMessageManger.Instance.SendMessage("这是一条个人消息", 2f); }, "功能测试");
        // Developer_GUITestManger.Instance.RegisterGuiButton("获取当前的战备", () => { PlayerAndGameInfoManger.Instance.EquipCurrentSlot(); }, "功能测试");
        // // 调试按钮
        // Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("打开当前调试信息", "关闭当前调试信息", () => { Developer_GUITestManger.Instance.IsShowAllInfo(true); }, () => { Developer_GUITestManger.Instance.IsShowAllInfo(false); });
        // Developer_GUITestManger.Instance.RegisterGuiButton_TwoWay("收起枪械", "拿起枪械", () => { Player.LocalPlayer.MyHandControl.SetHolsterState(true); }, () => { Player.LocalPlayer.MyHandControl.SetHolsterState(false); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("清理烟雾", () => { FluidController.Instance.ClearTexture(); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("保存数据", () => { PlayerAndGameInfoManger.Instance.SavePlayerData(); });
         Developer_GUITestManger.Instance.RegisterGuiButton("进入地图选择", () => { MapChooseWall.Instance.EnterMapChooseSystem(); });
        Developer_GUITestManger.Instance.RegisterGuiButton("触发护盾", () => { Player.LocalPlayer.TriggerShield(); });
         Developer_GUITestManger.Instance.RegisterGuiButton("靶子音效", () => {         MusicManager.Instance.PlayEffect3D("Music/正式/交互/击中靶子1", 10f, owner: this.transform); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("进入飞机视角", () => { MapChooseWall.Instance.EnterVC(); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("测试地图2视角", () => { MapChooseWall.Instance.TestScene2(); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("播放第一个动画", () => { CG.PlayAnima1(); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("播放第二个动画", () => { CG.PlayAnima2(); });
        //// Developer_GUITestManger.Instance.RegisterGuiButton("翻转画面", () => { CameraFlipper.Instance.ToggleFlip(); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("播放场景二动画", () => { Map2StartAnimaCG.Instance.TimeLine.Play(); SwitchToAnimaVCFast();UImanager.Instance.GetPanel<PlayerPanel>().SimpleHidePanel(); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("训练场", () => { AllMapManager.Instance.TriggerMap(MapType.Training, true); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("地图1", () => { AllMapManager.Instance.TriggerMap(MapType.map1, true); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("地图2", () => { AllMapManager.Instance.TriggerMap(MapType.map2, true); });
        //  Developer_GUITestManger.Instance.RegisterGuiButton("恢复", () => { ScreenPulseController.Instance.Trigger_Heal(); });
        // Developer_GUITestManger.Instance.RegisterGuiButton("受伤", () => { ScreenPulseController.Instance.Trigger_Wound(); });
        //Developer_GUITestManger.Instance.RegisterGuiButton("丢弃头盔", () => { Player.LocalPlayer.myStats.MyHelmet.TriggerHelmetDrop(); });

        //   Developer_GUITestManger.Instance.RegisterGuiButton("进行登录", () => { TapTapGameLogin.Instance.OnTapLoginClick(); });

           Developer_GUITestManger.Instance.RegisterGuiButton("购买测试商品1", () => { GoodDataManager.Instance.PurchaseGoodToUser(GoodDataManager.Instance.AllGoodsDataList[0]); });
        Developer_GUITestManger.Instance.RegisterGuiButton("购买测试商品2", () => { GoodDataManager.Instance.PurchaseGoodToUser(GoodDataManager.Instance.AllGoodsDataList[1]); });
        Developer_GUITestManger.Instance.RegisterGuiButton("置换子弹(紫色)", () => { GameSkinManager.Instance.EquipGunSki(GunSkinConfigType.Bullet, 5); });
        Developer_GUITestManger.Instance.RegisterGuiButton("置换子弹(默认黄色)", () => { GameSkinManager.Instance.EquipGunSki(GunSkinConfigType.Bullet, 4); });
        Developer_GUITestManger.Instance.RegisterGuiButton("置换枪口火光(紫色)", () => { GameSkinManager.Instance.EquipGunSki(GunSkinConfigType.MuzzleFlash, 5); });
        Developer_GUITestManger.Instance.RegisterGuiButton("置换枪口火光(默认黄色)", () => { GameSkinManager.Instance.EquipGunSki(GunSkinConfigType.MuzzleFlash, 4); });
        Developer_GUITestManger.Instance.RegisterGuiButton("获取所有表情资源", () => { ExpressionSystem.Instance.obtainAllExpression(); });
    }

    private void Update()
    {
    }

    public void GameStart()//游戏开始
    {
        UImanager.Instance.ShowPanel<GameStartPanel>();
    }


    public void PauseTimeLine()
    {
        StartTimeLine.Pause();
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

