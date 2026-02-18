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
