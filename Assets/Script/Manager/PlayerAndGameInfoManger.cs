using System.Collections.Generic;
using UnityEngine;

public class PlayerAndGameInfoManger : SingleMonoAutoBehavior<PlayerAndGameInfoManger>
{
    [Header("玩家战备数据")]
    //玩家数据和游戏数据都保存在这里（小于4）
    public int MaxSlotCount = 4;//最大槽位数量，默认为4，可以根据需要进行修改
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();//玩家槽位的信息列表，进行一个默认的初始化，后续可以根据需要进行修改


    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
