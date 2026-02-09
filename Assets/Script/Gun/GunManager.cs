using System.Collections.Generic;
using UnityEngine;

public class GunManager : SingleMonoAutoBehavior<GunManager>//枪械管理
{
    public List<GameObject> GunsPrefabsList = new List<GameObject>();
 
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

    //为面板提供专属的数据传输
    public Sprite GetGunInfo_GunSprite(string Name)
    {
        Debug.Log(Name);
        return GetInfo(Name).GunSprite;
    }

    public string GetGunInfo_describle(string Name)
    {
        if (GetInfo(Name) == null)
        {
            return "暂无描述";
        }
        return GetInfo(Name).description;
    }

    private GunInfo GetInfo(string Name)
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
}
