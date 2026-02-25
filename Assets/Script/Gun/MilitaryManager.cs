using System.Collections.Generic;
using UnityEngine;

public class MilitaryManager : SingleMonoAutoBehavior<MilitaryManager>//枪械管理
{
    #region 枪械管理
    public List<GameObject> GunsPrefabsList = new List<GameObject>();
    private List<GunInfo> gunInfoList = new List<GunInfo>();//枪械的信息列表

    protected override void Awake()
    {
        base.Awake();
        // 初始化枪械信息列表（增加空值检查）
        if (GunsPrefabsList != null && GunsPrefabsList.Count > 0)
        {
            foreach (var Gun in GunsPrefabsList)
            {
                if (Gun != null)
                {
                    BaseGun baseGun = Gun.GetComponent<BaseGun>();
                    if (baseGun != null && baseGun.gunInfo != null)
                    {
                        gunInfoList.Add(baseGun.gunInfo);
                    }
                    else
                    {
                        Debug.LogWarning($"[MilitaryManager] 枪械预制体 {Gun.name} 缺少 BaseGun 组件或 GunInfo", Gun);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[MilitaryManager] 枪械预制体列表为空！");
        }
    }

    /// <summary>
    /// 获取枪械实例
    /// </summary>
    public GameObject GetGun(string gunName)
    {
        if (string.IsNullOrEmpty(gunName))
        {
            Debug.LogWarning("[MilitaryManager] GetGun：枪械名称为空！");
            return null;
        }

        foreach (var gunPack in GunsPrefabsList)
        {
            if (gunPack != null && gunPack.name == gunName)
            {
                return gunPack;
            }
        }
        Debug.LogWarning($"[MilitaryManager] 未找到名为 {gunName} 的枪械预制体");
        return null;
    }

    public GunInfo GetInfo(string Name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning("[MilitaryManager] GetInfo：枪械名称为空！");
            return null;
        }

        foreach (var item in GunsPrefabsList)
        {
            if (item != null)
            {
                BaseGun baseGun = item.GetComponent<BaseGun>();
                if (baseGun != null && baseGun.gunInfo != null && baseGun.gunInfo.Name == Name)
                {
                    return baseGun.gunInfo;
                }
            }
        }
        Debug.LogWarning($"[MilitaryManager] 未找到名为 {Name} 的枪械信息");
        return null;
    }

    public List<GunInfo> GetGunTypeInfo(GunType Type)
    {
        //根据枪械类型获取对应的枪械信息列表
        List<GunInfo> result = new List<GunInfo>();

        foreach (var item in GunsPrefabsList)
        {
            if (item != null)
            {
                BaseGun baseGun = item.GetComponent<BaseGun>();
                if (baseGun != null && baseGun.gunInfo != null && baseGun.gunInfo.type == Type)
                {
                    result.Add(baseGun.gunInfo);
                }
            }
        }

        if (result.Count == 0)
        {
            Debug.LogWarning($"[MilitaryManager] 未找到类型为 {Type} 的枪械信息");
        }
        return result;
    }

    public string GetChineseGunTypeName(GunType type)//获取中文枪械类型名称
    {
        switch (type)
        {
            case GunType.Rifle:
                return "步枪";
            case GunType.Charge:
                return "冲锋枪";
            case GunType.Snipe:
                return "栓动步枪";
            case GunType.LightMachineGun:
                return "轻机枪";
            case GunType.DMR:
                return "射手步枪";
            default:
                return "未知类型";
        }
    }
    #endregion

    #region 战术设备管理
    public List<TacticInfo> TacticPrefabsList = new List<TacticInfo>();//战术设备列表（注射器/投掷物）

    /// <summary>
    /// 根据战术小类型获取预制体
    /// </summary>
    public GameObject GetTactic(TacticType Type)
    {
        foreach (var Tactic in TacticPrefabsList)
        {
            if (Tactic != null && Tactic.tacticType == Type)
            {
                return Tactic.TacticPrefab;
            }
        }
        Debug.LogWarning($"[MilitaryManager] 没有找到类型为 {Type} 的战术设备预制体");
        return null;
    }

    public TacticInfo GetTacticInfo(TacticType Type)//根据战术小类型获取战术设备信息
    {
        foreach (var Tactic in TacticPrefabsList)
        {
            if (Tactic != null && Tactic.tacticType == Type)
            {
                return Tactic;
            }
        }
        Debug.LogWarning($"[MilitaryManager] 没有找到类型为 {Type} 的战术设备信息");
        return null;
    }

    /// <summary>
    /// 根据战术小类型获取UI图标
    /// </summary>
    public Sprite GetTacticUISprite(TacticType Type)
    {
        foreach (var Tactic in TacticPrefabsList)
        {
            if (Tactic != null && Tactic.tacticType == Type)
            {
                return Tactic.UISprite;
            }
        }
        Debug.LogWarning($"[MilitaryManager] 没有找到类型为 {Type} 的战术设备UI图标");
        return null;
    }

    /// <summary>
    /// 根据战术大类获取该类下所有的战术小类型
    /// </summary>
    public List<TacticType> GetTacticTypesByBigType(TacticBigType bigType)
    {
        List<TacticType> result = new List<TacticType>();

        switch (bigType)
        {
            case TacticBigType.injection:
                // 针剂类包含的小类型
                result.Add(TacticType.Green_injection);
                result.Add(TacticType.Yellow_injection);
                break;

            case TacticBigType.throwobj:
                // 投掷物类包含的小类型
                result.Add(TacticType.Grenade);
                result.Add(TacticType.Smoke);
                break;

            default:
                Debug.LogWarning($"[MilitaryManager] 未知的战术大类：{bigType}");
                break;
        }

        List<TacticType> validTypes = new List<TacticType>();
        foreach (var tacticType in result)
        {
            if (TacticPrefabsList.Exists(t => t != null && t.tacticType == tacticType))
            {
                validTypes.Add(tacticType);
            }
            else
            {
                Debug.LogWarning($"[MilitaryManager] 战术大类 {bigType} 下的 {tacticType} 没有对应的预制体配置");
            }
        }

        return validTypes;
    }

    /// <summary>
    /// 获取战术大类的中文名称
    /// </summary>
    public string GetChineseTacticBigTypeName(TacticBigType bigType)
    {
        switch (bigType)
        {
            case TacticBigType.injection:
                return "针剂";
            case TacticBigType.throwobj:
                return "投掷物";
            default:
                return "未知战术大类";
        }
    }

    public string GetChineseTacticTypeName(TacticType type)
    {
        switch (type)
        {
            case TacticType.Green_injection:
                return "绿色针剂";
            case TacticType.Yellow_injection:
                return "黄色针剂";
            case TacticType.Grenade:
                return "手雷";
            case TacticType.Smoke:
                return "烟雾弹";
            default:
                return "未知战术小类型";
        }
    }

    /// <summary>
    /// 根据战术小类型反查所属大类
    /// </summary>
    public TacticBigType GetTacticBigType(TacticType tacticType)
    {
        switch (tacticType)
        {
            case TacticType.Green_injection:
            case TacticType.Yellow_injection:
                return TacticBigType.injection;

            case TacticType.Grenade:
            case TacticType.Smoke:
                return TacticBigType.throwobj;

            default:
                Debug.LogWarning($"[MilitaryManager] 未知的战术小类型：{tacticType}");
                return TacticBigType.injection; // 默认值
        }
    }
    #endregion
}

#region 枚举定义
[System.Serializable]
public enum GunType
{
    Rifle,      // 步枪
    Charge,     // 冲锋枪
    Snipe,      // 栓动步枪
    LightMachineGun,    // 轻机枪
    DMR         // 射手步枪
}

/// <summary>
/// 战术小类型（具体的战术设备）
/// </summary>
[System.Serializable]
public enum TacticType
{
    Green_injection,  // 绿色针剂
    Yellow_injection, // 黄色针剂
    Grenade,          // 手雷
    Smoke             // 烟雾弹
}

/// <summary>
/// 战术大类（用于分类管理）
/// </summary>
[System.Serializable]
public enum TacticBigType
{
    injection,  // 针剂类
    throwobj    // 投掷物类
}
#endregion