using System.Collections.Generic;
using UnityEngine;

public class PlayerAndGameInfoManger : SingleMonoAutoBehavior<PlayerAndGameInfoManger>
{
    [Header("玩家战备数据")]
    //玩家数据和游戏数据都保存在这里（小于4）
    public int MaxSlotCount = 4;//最大槽位数量，默认为4，可以根据需要进行修改
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();//玩家槽位的信息列表，进行一个默认的初始化，后续可以根据需要进行修改

    [Header("当前的装备槽位")]
    public int SlotCount = 1;
    public SlotInfoPack CurrentSlotInfoPack;//当前选中的槽位

    [Header("当前的地图信息")]
    public List<MapInfo> AllMapInfoList = new List<MapInfo>();//所有的地图信息
    public List <MapManager> AllMapManagerList=new List<MapManager> ();//所有的地图管理器

    [Header("控制系统记录")]
    public bool IsUseSinglePress_AimButton=false;//是否使用单击点击(瞄准按钮)

    [Header("自定义面板的数据")]
    public List<PlayerCustomUIInfo> playerCustomUIInfoList = new List<PlayerCustomUIInfo> ();//这个数是需要进行本地化保存的
    public List<GameObject> AllCustomUIPrefabsList = new List<GameObject> ();//所有的自定义UI预制体

    [Header("画面设置")]
    public FpsType CurrentFPS;
    public ScreenType CurrentScreen;


    //增加数据
    public void AddCustomUIInfoList(PlayerCustomUIInfo info)
    {
        if(GetPlayerCustomUIInfo(info.UIType,false)==null)//这里的作用不需要进行debug
        {
            //如果没有这个类型就进行注册
            playerCustomUIInfoList.Add(info);
        }
    }

    public PlayerCustomUIInfo GetPlayerCustomUIInfo(NeedCustomUIType Type,bool IsNeedDebug=true)
    {
        foreach(var Info in playerCustomUIInfoList)
        {
            if(Info.UIType== Type)
                return Info;
        }

        if(IsNeedDebug)
          Debug.LogError("未找到自定义UI的类型");
        return null;
    }

    public void SetSlotInfoPack(int Index)
    {
        var listIndex= Index-1;
        SlotCount= Index;//设置一下槽位
        CurrentSlotInfoPack = PlayerSlotInfoPacksList[listIndex];//进行赋值

    }

    public void EquipCurrentSlot()
    {
        //装备当前枪械以及投掷物
        Player.LocalPlayer.SpawnAndPickGun(CurrentSlotInfoPack.CurrentGunInfo.Name);//获取枪械
        //更新投掷物
        PlayerTacticControl.Instance?.UpdateCurrentTactic();//进行UI交互的更新
        //触发战术道具的交互
        PlayerTacticControl.Instance.SetTacticControl(true);
    }

   


    public GunInfo GetCurrentGunInfo()
    {
        return CurrentSlotInfoPack.CurrentGunInfo;
    }

    public TacticInfo GetCurrentTacticInfo(int Index)
    {
        if (Index == 1)
            return CurrentSlotInfoPack.CurrentTactic_1Info;
        else if(Index == 2)
            return CurrentSlotInfoPack.CurrentTactic_2Info;

        Debug.LogError("未知的战备索引（这里只能传1或者2）");
        return null;
    }

    protected override void Awake()
    {
        base.Awake();
        //初始化当前的战备
        SetSlotInfoPack(SlotCount);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
