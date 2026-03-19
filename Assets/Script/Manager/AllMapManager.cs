using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class AllMapManager : SingleMonoAutoBehavior<AllMapManager>
{
    public List<MapPack> mapPacksList;

    public void TriggerMap(MapType type,bool IsActive)
    {
        foreach (MapPack pack in mapPacksList)
        {
            if(pack.Type== type)
            {
                pack.Obj.SetActive(IsActive);
            }
        }
    }

    public void CloseAllMap()
    {
        foreach (MapPack pack in mapPacksList)
        {
            pack.Obj.SetActive(false);
        }
    }
    public void CloseAllMap(MapType Type)
    {
        foreach (MapPack pack in mapPacksList)
        {
            if(pack.Type==Type)
                continue;
            pack.Obj.SetActive(false);
        }
    }


    //所有的地图激活与失活管理器
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}

public enum MapType
{
map1,//地图1
map2,//地图2
Training,//模式选择地图
StartCG,//训练场
}

[System.Serializable]
public class MapPack
{
    public MapType Type;
    public GameObject Obj;

}


