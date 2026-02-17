using System.Collections.Generic;
using UnityEngine;

public class GunManager : SingleMonoAutoBehavior<GunManager>//枪械管理
{
    public List<GameObject> GunsPrefabsList = new List<GameObject>();
    private List<GunInfo> gunInfoList = new List<GunInfo>();//枪械的信息列表

    protected override void Awake()
    {
        base.Awake();
        foreach(var Gun in GunsPrefabsList)
        {
            gunInfoList.Add(Gun.GetComponent<BaseGun>().gunInfo);//获取所有的信息
        }
    }

    /// <summary>
    /// 获取枪械实例
    /// </summary>
    public GameObject GetGun(string gunName)
    {
        foreach (var gunPack in GunsPrefabsList)
        {
            if (gunPack.name == gunName)
            {
                return gunPack;
            }
        }
        return null;
    }

    public  GunInfo GetInfo(string Name)
    {
        foreach (var item in GunsPrefabsList)
        {
            var gunInfo = item.GetComponent<BaseGun>().gunInfo;
            if (gunInfo.Name == Name)
            {
                return gunInfo;
            }
        }
        return null;
    }
    public List<GunInfo> getGunTypeInfo(GunType Type)
    {
        //根据枪械类型获取对应的枪械信息列表
        List<GunInfo> result = new List<GunInfo>();

        foreach (var item in GunsPrefabsList)
        {
            var gunInfo = item.GetComponent<BaseGun>().gunInfo;
            if (gunInfo.type == Type)
            {
                result.Add(gunInfo);
            }
        }
        return result;
    }

    public string getChineseGunTypeName(GunType type)//获取中文枪械类型名称
    {
        switch (type)
        {
            case GunType.Rifle:
                return "步枪";
            case GunType.Charge:
                return "冲锋枪";
            case GunType.Snipe:
                return "栓动步枪";
            case GunType.Shotgun:
                return "散弹枪";
            case GunType.DMR:
                return "射手步枪";
            default:
                return "未知类型";
        }
    }

}

[System.Serializable]
public enum GunType
{
    Rifle,
    Charge,
    Snipe,
    Shotgun,
    DMR,//射手步枪
}
