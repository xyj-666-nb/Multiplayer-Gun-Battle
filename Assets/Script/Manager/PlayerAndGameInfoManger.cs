using System.Collections.Generic;
using UnityEngine;

public class PlayerAndGameInfoManger : SingleMonoAutoBehavior<PlayerAndGameInfoManger>
{
    [Header("玩家战备数据")]
    public int MaxSlotCount = 4;
    // 这个 List 依然保留在 Inspector 中作为默认配置，但运行时会被存档覆盖
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();

    [Header("当前的装备槽位")]
    public int SlotCount = 1;
    public SlotInfoPack CurrentSlotInfoPack;

    [Header("当前的地图信息")]
    public List<MapInfo> AllMapInfoList = new List<MapInfo>();
    public List<MapManager> AllMapManagerList = new List<MapManager>();

    [Header("控制系统记录")]
    public bool IsUseSinglePress_AimButton = false;

    [Header("自定义面板的数据")]
    public List<PlayerCustomUIInfo> playerCustomUIInfoList = new List<PlayerCustomUIInfo>();
    public List<GameObject> AllCustomUIPrefabsList = new List<GameObject>();

    [Header("画面设置")]
    public FpsType CurrentFPS;
    public ScreenType CurrentScreen;

    [Header("蓝队的队伍标识")]
    public Sprite BlueTeamSprite;
    [Header("红队的队伍标识")]
    public Sprite RedTeamSprite;

    // 存档文件名常量
    private const string SAVE_FILE_NAME = "PlayerGameData";

    // ================= 原有逻辑  =================

    public void AddCustomUIInfoList(PlayerCustomUIInfo info)
    {
        if (GetPlayerCustomUIInfo(info.UIType, false) == null)
        {
            playerCustomUIInfoList.Add(info);
        }
    }

    public PlayerCustomUIInfo GetPlayerCustomUIInfo(NeedCustomUIType Type, bool IsNeedDebug = true)
    {
        foreach (var Info in playerCustomUIInfoList)
        {
            if (Info.UIType == Type)
                return Info;
        }

        if (IsNeedDebug)
            Debug.LogError("未找到自定义UI的类型");
        return null;
    }

    public void SetSlotInfoPack(int Index)
    {
        var listIndex = Index - 1;
        // 增加安全检查
        if (listIndex >= 0 && listIndex < PlayerSlotInfoPacksList.Count)
        {
            SlotCount = Index;
            CurrentSlotInfoPack = PlayerSlotInfoPacksList[listIndex];
        }
        else
        {
            Debug.LogWarning($"槽位索引 {Index} 无效");
        }
    }

    public void EquipCurrentSlot()
    {
        if (CurrentSlotInfoPack == null) return;

        Player.LocalPlayer.CmdGetArmor(CurrentSlotInfoPack.CurrentArmorType);
        CountDownManager.Instance.CreateTimer(false, 1500, () => {
            Player.LocalPlayer.SpawnAndPickGun(CurrentSlotInfoPack.CurrentGunInfo.Name);
            PlayerTacticControl.Instance?.UpdateCurrentTactic();
            PlayerTacticControl.Instance.SetTacticControl(true);
        });
    }

    public GunInfo GetCurrentGunInfo()
    {
        return CurrentSlotInfoPack?.CurrentGunInfo;
    }

    public TacticInfo GetCurrentTacticInfo(int Index)
    {
        if (CurrentSlotInfoPack == null) return null;

        if (Index == 1)
            return CurrentSlotInfoPack.CurrentTactic_1Info;
        else if (Index == 2)
            return CurrentSlotInfoPack.CurrentTactic_2Info;

        Debug.LogError("未知的战备索引（这里只能传1或者2）");
        return null;
    }


    protected override void Awake()
    {
        base.Awake();
        LoadPlayerData();
        SetSlotInfoPack(SlotCount);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁时自动保存一次
    }


    /// <summary>
    /// 保存玩家数据到本地
    /// </summary>
    public void SavePlayerData()
    {
        // 1. 准备要保存的数据
        PlayerGameSaveData saveData = new PlayerGameSaveData
        {
            PlayerSlotInfoPacksList = this.PlayerSlotInfoPacksList,
            CurrentSlotIndex = this.SlotCount,
            playerCustomUIInfoList = this.playerCustomUIInfoList,
            CurrentFPS = (int)this.CurrentFPS,
            CurrentScreen = (int)this.CurrentScreen
        };

        JsonManager.Instance.SaveData(saveData, SAVE_FILE_NAME, JsonType.JsonUtlity);

        Debug.Log($"[PlayerAndGameInfoManger] 玩家数据已保存");
    }

    /// <summary>
    /// 从本地读取玩家数据
    /// </summary>
    public void LoadPlayerData()
    {
        PlayerGameSaveData loadData = JsonManager.Instance.LoadData<PlayerGameSaveData>(SAVE_FILE_NAME, JsonType.JsonUtlity);

        if (loadData == null)
        {
            Debug.Log("[PlayerAndGameInfoManger] 未找到存档，使用默认配置");
            return;
        }

        // 只有当存档里有数据时才覆盖，防止把 Inspector 里的默认配置清空
        if (loadData.PlayerSlotInfoPacksList != null && loadData.PlayerSlotInfoPacksList.Count > 0)
        {
            this.PlayerSlotInfoPacksList = loadData.PlayerSlotInfoPacksList;
        }

        // 恢复当前选中的槽位
        if (loadData.CurrentSlotIndex > 0)
        {
            this.SlotCount = loadData.CurrentSlotIndex;
        }

        // 恢复自定义UI数据
        if (loadData.playerCustomUIInfoList != null)
        {
            this.playerCustomUIInfoList = loadData.playerCustomUIInfoList;
        }

        // 恢复画面设置
        this.CurrentFPS = (FpsType)loadData.CurrentFPS;
        this.CurrentScreen = (ScreenType)loadData.CurrentScreen;

        Debug.Log("[PlayerAndGameInfoManger] 玩家数据已加载");
    }
}

public class PlayerGameSaveData
{
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();

    public int CurrentSlotIndex = 1;

    public List<PlayerCustomUIInfo> playerCustomUIInfoList = new List<PlayerCustomUIInfo>();

    public int CurrentFPS; // 存 enum 的 int 值
    public int CurrentScreen;
}