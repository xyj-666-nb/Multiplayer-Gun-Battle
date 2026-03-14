using System.Collections.Generic;
using UnityEngine;

public class PlayerAndGameInfoManger : SingleMonoAutoBehavior<PlayerAndGameInfoManger>
{
    [Header("玩家战备数据")]
    public int MaxSlotCount = 4;
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();

    private List<SlotInfoPack> _defaultSlotInfoBackup;

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

    private const string SAVE_FILE_NAME = "PlayerGameData";

    // ================= 逻辑修复  =================

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
        if (CurrentSlotInfoPack == null)
        {
            Debug.LogWarning("当前槽位信息为空，无法装备！");
            return;
        }

        var cachedArmorType = CurrentSlotInfoPack.CurrentArmorType;
        string cachedGunName = CurrentSlotInfoPack.CurrentGunInfo?.Name;

        if (Player.LocalPlayer != null)
        {
            Player.LocalPlayer.CmdGetArmor(cachedArmorType);
        }

        if (CountDownManager.Instance == null) 
            return;

        CountDownManager.Instance.CreateTimer(false, 1500, () => {
            if (this == null || Instance == null) 
                return;
            if (Player.LocalPlayer == null) 
                return;

            if (!string.IsNullOrEmpty(cachedGunName))
            {
                Player.LocalPlayer.SpawnAndPickGun(cachedGunName);
            }
        });
    }
    public void ShowTactic()
    {
        if (PlayerTacticControl.Instance != null)
        {
            PlayerTacticControl.Instance.UpdateCurrentTactic();
            PlayerTacticControl.Instance.SetTacticControl(true);
        }
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
        BackupDefaultData();
        LoadPlayerData();
        SetSlotInfoPack(SlotCount);
    }

    private void BackupDefaultData()
    {

        _defaultSlotInfoBackup = new List<SlotInfoPack>(PlayerSlotInfoPacksList);
        Debug.Log($"[PlayerAndGameInfoManger] 已备份默认配置，共 {_defaultSlotInfoBackup.Count} 个槽位");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public void SavePlayerData()
    {
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

    public void LoadPlayerData()
    {
        PlayerGameSaveData loadData = JsonManager.Instance.LoadData<PlayerGameSaveData>(SAVE_FILE_NAME, JsonType.JsonUtlity);

        if (loadData == null)
        {
            Debug.Log("[PlayerAndGameInfoManger] 未找到存档，使用默认配置");
            RestoreDefaultData(); // 恢复备份
            return;
        }

        bool hasValidSlotData = loadData.PlayerSlotInfoPacksList != null && loadData.PlayerSlotInfoPacksList.Count > 0;

        if (hasValidSlotData)
        {
            this.PlayerSlotInfoPacksList = loadData.PlayerSlotInfoPacksList;
            Debug.Log("[PlayerAndGameInfoManger] 已加载存档槽位数据");
        }
        else
        {
            Debug.LogWarning("[PlayerAndGameInfoManger] 存档中的槽位数据为空，回退到默认配置");
            RestoreDefaultData(); // 恢复备份
        }

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
    }

    private void RestoreDefaultData()
    {
        PlayerSlotInfoPacksList.Clear();

        // 从备份里复制回来
        if (_defaultSlotInfoBackup != null)
        {
            PlayerSlotInfoPacksList.AddRange(_defaultSlotInfoBackup);
        }
        else
        {
            Debug.LogError("备份数据也丢失了！");
        }
    }
}

public class PlayerGameSaveData
{
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();
    public int CurrentSlotIndex = 1;
    public List<PlayerCustomUIInfo> playerCustomUIInfoList = new List<PlayerCustomUIInfo>();
    public int CurrentFPS;
    public int CurrentScreen;
}