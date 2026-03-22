using System.Collections.Generic;
using UnityEngine;

public class MilitaryManager : SingleMonoAutoBehavior<MilitaryManager>//枪械管理
{
    #region 枪械管理
    public List<GameObject> GunsPrefabsList = new List<GameObject>();
    private List<GunInfo> gunInfoList = new List<GunInfo>();//枪械的信息列表

    private Dictionary<string, GameObject> _gunPrefabDict; // 枪械名称→预制体
    private Dictionary<string, GunInfo> _gunInfoDict;     // 枪械名称→GunInfo
    private Dictionary<GunType, List<GunInfo>> _gunTypeDict; // 枪械类型→GunInfo列表

    #endregion

    #region 战术设备管理
    public List<TacticInfo> TacticPrefabsList = new List<TacticInfo>();//战术设备列表（注射器/投掷物）

    // 新增：战术设备缓存字典（仅内部使用）
    private Dictionary<TacticType, TacticInfo> _tacticInfoDict; // 战术类型→TacticInfo

    #endregion

    #region 护甲管理
    public List<ArmorInfoPack> ArmorInfoPackList;//护甲管理包

    // 新增：护甲缓存字典（仅内部使用）
    private Dictionary<ArmorType, ArmorInfoPack> _armorInfoDict; // 护甲类型→ArmorInfoPack

    #endregion

    protected override void Awake()
    {
        base.Awake();

        // 初始化缓存字典
        _gunPrefabDict = new Dictionary<string, GameObject>();
        _gunInfoDict = new Dictionary<string, GunInfo>();
        _gunTypeDict = new Dictionary<GunType, List<GunInfo>>();
        _tacticInfoDict = new Dictionary<TacticType, TacticInfo>();
        _armorInfoDict = new Dictionary<ArmorType, ArmorInfoPack>();

        InitGunCache();

        InitTacticCache();

        InitArmorCache();
    }

    #region 内部初始化缓存方法（仅内部使用）
    /// <summary>
    /// 初始化枪械缓存
    /// </summary>
    private void InitGunCache()
    {
        if (GunsPrefabsList == null || GunsPrefabsList.Count == 0)
        {
            Debug.LogWarning("[MilitaryManager] 枪械预制体列表为空！");
            return;
        }

        foreach (var Gun in GunsPrefabsList)
        {
            if (Gun == null) continue;

            BaseGun baseGun = Gun.GetComponent<BaseGun>();
            if (baseGun != null && baseGun.gunInfo != null)
            {
                gunInfoList.Add(baseGun.gunInfo);

                string gunName = baseGun.gunInfo.Name;
                if (!_gunPrefabDict.ContainsKey(gunName))
                {
                    _gunPrefabDict.Add(gunName, Gun);
                }

                if (!_gunInfoDict.ContainsKey(gunName))
                {
                    _gunInfoDict.Add(gunName, baseGun.gunInfo);
                }

                GunType gunType = baseGun.gunInfo.type;
                if (!_gunTypeDict.ContainsKey(gunType))
                {
                    _gunTypeDict.Add(gunType, new List<GunInfo>());
                }
                if (!_gunTypeDict[gunType].Contains(baseGun.gunInfo))
                {
                    _gunTypeDict[gunType].Add(baseGun.gunInfo);
                }
            }
            else
            {
                Debug.LogWarning($"[MilitaryManager] 枪械预制体 {Gun.name} 缺少 BaseGun 组件或 GunInfo", Gun);
            }
        }
    }

    /// <summary>
    /// 初始化战术设备缓存
    /// </summary>
    private void InitTacticCache()
    {
        if (TacticPrefabsList == null)
        {
            TacticPrefabsList = new List<TacticInfo>(); // 空值初始化，避免后续遍历空引用
            Debug.LogWarning("[MilitaryManager] 战术设备列表为空，已初始化空列表！");
            return;
        }

        foreach (var Tactic in TacticPrefabsList)
        {
            if (Tactic == null) continue;

            if (!_tacticInfoDict.ContainsKey(Tactic.tacticType))
            {
                _tacticInfoDict.Add(Tactic.tacticType, Tactic);
            }
        }
    }

    /// <summary>
    /// 初始化护甲缓存（Awake中只执行一次）
    /// </summary>
    private void InitArmorCache()
    {
        if (ArmorInfoPackList == null)
        {
            ArmorInfoPackList = new List<ArmorInfoPack>(); // 空值初始化，避免后续遍历空引用
            Debug.LogWarning("[MilitaryManager] 护甲列表为空，已初始化空列表！");
            return;
        }

        foreach (var InfoPack in ArmorInfoPackList)
        {
            if (InfoPack == null) continue;

            if (!_armorInfoDict.ContainsKey(InfoPack.armorType))
            {
                _armorInfoDict.Add(InfoPack.armorType, InfoPack);
            }
        }
    }
    #endregion

    #region 枪械管理
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

        // 优化：字典直接查询，替代遍历列表
        if (_gunPrefabDict.TryGetValue(gunName, out GameObject gunPrefab))
        {
            return gunPrefab;
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

        if (_gunInfoDict.TryGetValue(Name, out GunInfo gunInfo))
        {
            return gunInfo;
        }

        Debug.LogWarning($"[MilitaryManager] 未找到名为 {Name} 的枪械信息");
        return null;
    }

    public List<GunInfo> GetGunTypeInfo(GunType Type)
    {

        if (_gunTypeDict.TryGetValue(Type, out List<GunInfo> result))
        {
            if (result.Count == 0)
            {
                Debug.LogWarning($"[MilitaryManager] 未找到类型为 {Type} 的枪械信息");
            }
            return result;
        }

        Debug.LogWarning($"[MilitaryManager] 未找到类型为 {Type} 的枪械信息");
        return new List<GunInfo>(); // 返回空列表，避免外部接收到null
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
    /// <summary>
    /// 根据战术小类型获取预制体
    /// </summary>
    public GameObject GetTactic(TacticType Type)
    {
        if (_tacticInfoDict.TryGetValue(Type, out TacticInfo tacticInfo))
        {
            return tacticInfo.TacticPrefab;
        }

        Debug.LogWarning($"[MilitaryManager] 没有找到类型为 {Type} 的战术设备预制体");
        return null;
    }

    public TacticInfo GetTacticInfo(TacticType Type)//根据战术小类型获取战术设备信息
    {
        if (_tacticInfoDict.TryGetValue(Type, out TacticInfo tacticInfo))
        {
            return tacticInfo;
        }

        Debug.LogWarning($"[MilitaryManager] 没有找到类型为 {Type} 的战术设备信息");
        return null;
    }

    /// <summary>
    /// 根据战术小类型获取UI图标（优化：字典查询）
    /// </summary>
    public Sprite GetTacticUISprite(TacticType Type)
    {
        if (_tacticInfoDict.TryGetValue(Type, out TacticInfo tacticInfo))
        {
            return tacticInfo.UISprite;
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
                result.Add(TacticType.Green_injection);
                result.Add(TacticType.Yellow_injection);
                break;

            case TacticBigType.throwobj:
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
            if (_tacticInfoDict.ContainsKey(tacticType))
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

    #region 护甲管理（优化内部实现，保持对外接口不变）
    public ArmorInfoPack GetArmorInfoPack(ArmorType Type)//获取护甲管理包
    {
        if (_armorInfoDict.TryGetValue(Type, out ArmorInfoPack armorInfo))
        {
            return armorInfo;
        }

        Debug.Log("未找到名为" + Type.ToString() + "的护甲包");
        return null;
    }
    #endregion
}

#region 枚举定义（完全保留原有命名）
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

[System.Serializable]
public enum ArmorType
{
    Empty_handed,//空手
    Army_Heavy,//陆军——重型
    Navy_Balanced,//海军——均衡
    AirForce_Light,//空军——轻型
}

#endregion
[System.Serializable]
public class ArmorInfoPack
{
    //护甲信息包
    [Header("护甲类型以及名字")]
    public ArmorType armorType;
    public string armorName;
    [Header("护甲的描述")]
    [TextArea(3, 8)]
    public string armorDescription;
    [Header("护甲图片")]
    public Sprite HelmetSprite;//头盔图片
    public Sprite ArmorSprite;//护甲图片
    [Header("护甲UI图片")]
    public Sprite UISprite;//护甲UI图片
    [Header("数值加成")]
    public float HealthAdd;//生命力加成
    public float SpeedAdd;//速度加成
}