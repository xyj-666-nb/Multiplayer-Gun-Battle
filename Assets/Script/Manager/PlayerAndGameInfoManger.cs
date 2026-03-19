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

    // ================= 逻辑修复 =================

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
        if (CurrentSlotInfoPack == null) 
            return null;

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
        ApplyGraphicsSettings(); 
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

    // ================= 图形设置应用方法 =================

    /// <summary>
    /// 应用当前的图形设置
    /// </summary>
    public void ApplyGraphicsSettings()
    {
        QualitySettings.vSyncCount = 0;

        // 应用帧率（适配移动端，不超过屏幕刷新率）
        int targetFrameRate = GetTargetFrameRate(CurrentFPS);
        Application.targetFrameRate = targetFrameRate;
        Debug.Log($"[PlayerAndGameInfoManger] 设置目标帧率: {targetFrameRate}");

        // 应用画质
        int qualityLevel = GetQualityLevel(CurrentScreen);
        QualitySettings.SetQualityLevel(qualityLevel, true);
        Debug.Log($"[PlayerAndGameInfoManger] 设置画质级别: {qualityLevel} ({CurrentScreen})");
    }

    /// <summary>
    /// 根据 FpsType 获取目标帧率，自动适配屏幕刷新率
    /// </summary>
    private int GetTargetFrameRate(FpsType fpsType)
    {
        int target = 60; // 默认
        switch (fpsType)
        {
            case FpsType.Standard: target = 60; break;
            case FpsType.High: target = 90; break;
            case FpsType.Ultra: target = 120; break;
        }

        double refreshRateValue = Screen.currentResolution.refreshRateRatio.value;
        int refreshRate = (int)System.Math.Round(refreshRateValue);

        if (refreshRate > 0 && target > refreshRate)
        {
            Debug.LogWarning($"目标帧率 {target} 高于屏幕刷新率 {refreshRate}，已限制为 {refreshRate}");
            target = refreshRate;
        }
        return target;
    }
    /// <summary>
    /// 根据 ScreenType 映射到 Unity 质量设置索引
    /// </summary>
    private int GetQualityLevel(ScreenType screenType)
    {
        // 获取所有质量等级的名称
        string[] qualityNames = QualitySettings.names;
        if (qualityNames == null || qualityNames.Length == 0)
        {
            Debug.LogWarning("项目中未定义质量等级，返回当前级别");
            return QualitySettings.GetQualityLevel();
        }

        string targetName = "";
        switch (screenType)
        {
            case ScreenType.Standard: targetName = "Standard"; break;
            case ScreenType.High: targetName = "High"; break;
            case ScreenType.Ultra: targetName = "Ultra"; break;
        }

        // 查找第一个包含目标名称的索引（不区分大小写）
        for (int i = 0; i < qualityNames.Length; i++)
        {
            if (qualityNames[i].IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return i;
        }

        // 若未找到，返回当前级别并警告
        Debug.LogWarning($"未找到匹配的画质名称 '{targetName}'，使用当前级别");
        return QualitySettings.GetQualityLevel();
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