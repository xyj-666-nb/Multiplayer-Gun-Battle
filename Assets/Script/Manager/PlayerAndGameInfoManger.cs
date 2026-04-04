using System.Collections;
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
    public bool IsUseSinglePress_AimButton = false; // false=点击, true=长按
    public float AimSensitivity = 1.0f; // 瞄准灵敏度 (0.5 ~ 2.0)
    private bool isUseJoyStickMove=true;// 是否使用摇杆移动（如果为 false 则使用 按钮 移动）
    public bool IsUseJoyStickMove {
        get => isUseJoyStickMove;
        set
        {
            isUseJoyStickMove = value;
           Debug.Log($"[PlayerAndGameInfoManger] 已设置 IsUseJoyStickMove = {isUseJoyStickMove}");
            //如果Playerpanel存在就直接更新（否则就留给他自己读取）
            if (UImanager.Instance.GetPanel<PlayerPanel>())
            {
                UImanager.Instance.GetPanel<PlayerPanel>().UpdateMoveButton();//提示更新一下
            }
        }

    }

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

    private bool _canEquip = true;

    public void EquipCurrentSlot()
    {
        if (!_canEquip)
        {
            Debug.LogWarning("装备操作冷却中，请稍候再试！");
            return;
        }

        if (CurrentSlotInfoPack == null)
        {
            Debug.LogWarning("当前槽位信息为空，无法装备！");
            return;
        }

        var cachedArmorType = CurrentSlotInfoPack.CurrentArmorType;
        string cachedGunName = CurrentSlotInfoPack.CurrentGunInfo?.Name;
        //播放音效
        MusicManager.Instance.PlayEffect("Music/正式/交互/起装",0.7f);

        _canEquip = false;
        StartCoroutine(EquipCoolDownCoroutine());

        if (Player.LocalPlayer != null)
        {
            Player.LocalPlayer.CmdGetArmor(cachedArmorType);
        }

        if (CountDownManager.Instance == null)
            return;

        CountDownManager.Instance.CreateTimer(false, 500, () => {
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

    //3帧冷却协程
    private IEnumerator EquipCoolDownCoroutine()
    {
        // 等待3帧
        for (int i = 0; i < 3; i++)
        {
            // 如果对象被销毁，直接退出协程
            if (this == null)
            {
                yield break;
            }
            yield return null;
        }
        // 冷却结束，恢复可触发状态
        _canEquip = true;
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

    // ================= 数据保存与加载 (已深度更新) =================

    public void SavePlayerData()
    {
        PlayerGameSaveData saveData = new PlayerGameSaveData
        {
            PlayerSlotInfoPacksList = this.PlayerSlotInfoPacksList,
            CurrentSlotIndex = this.SlotCount,
            playerCustomUIInfoList = this.playerCustomUIInfoList,
            CurrentFPS = (int)this.CurrentFPS,
            CurrentScreen = (int)this.CurrentScreen,

            IsUseSinglePress_AimButton = this.IsUseSinglePress_AimButton,
            AimSensitivity = this.AimSensitivity,
            IsUseJoyStickMove = this.IsUseJoyStickMove
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
            RestoreDefaultDataAndSave(); // 恢复并覆盖存档
            return;
        }

        bool hasValidSlotData = loadData.PlayerSlotInfoPacksList != null && loadData.PlayerSlotInfoPacksList.Count > 0;

        if (hasValidSlotData)
        {
            hasValidSlotData = CheckAllSlotDataIntegrity(loadData.PlayerSlotInfoPacksList);
        }

        if (hasValidSlotData)
        {
            this.PlayerSlotInfoPacksList = loadData.PlayerSlotInfoPacksList;
            Debug.Log("[PlayerAndGameInfoManger] 已加载存档槽位数据 (数据完整性校验通过)");
        }
        else
        {
            Debug.LogWarning("[PlayerAndGameInfoManger] 存档数据损坏或内容为空，回退到默认配置并覆盖旧存档");
            RestoreDefaultDataAndSave(); // 恢复并覆盖存档
        }

        if (loadData.CurrentSlotIndex > 0)
        {
            this.SlotCount = loadData.CurrentSlotIndex;
        }

        // 加载自定义UI数据
        if (loadData.playerCustomUIInfoList != null)
        {
            this.playerCustomUIInfoList = loadData.playerCustomUIInfoList;
        }

        // 加载画面设置
        this.CurrentFPS = (FpsType)loadData.CurrentFPS;
        this.CurrentScreen = (ScreenType)loadData.CurrentScreen;

        // 新增：加载控制系统数据 (带默认值保护)
        this.IsUseSinglePress_AimButton = loadData.IsUseSinglePress_AimButton;
        this.AimSensitivity = loadData.AimSensitivity == 0 ? 1.0f : loadData.AimSensitivity;
        this.IsUseJoyStickMove = loadData.IsUseJoyStickMove;
    }

    /// <summary>
    /// 深度检查所有槽位数据的完整性
    /// </summary>
    private bool CheckAllSlotDataIntegrity(List<SlotInfoPack> listToCheck)
    {
        if (listToCheck == null || listToCheck.Count == 0) return false;

        foreach (var slot in listToCheck)
        {
            // 只要有一个槽位的核心数据为空，就认为整个存档无效
            if (!IsSingleSlotValid(slot))
            {
                Debug.LogWarning($"[数据校验] 发现无效槽位数据，触发回退机制");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 检查单个槽位是否有效
    /// </summary>
    private bool IsSingleSlotValid(SlotInfoPack slot)
    {
        if (slot == null) return false;

        if (slot.CurrentGunInfo == null)
        {
            Debug.LogWarning("[数据校验] 槽位中的 CurrentGunInfo 为空");
            return false;
        }

        if (slot.CurrentTactic_1Info == null)
        {
            Debug.LogWarning("[数据校验] 槽位中的 CurrentTactic_1Info 为空");
            return false;
        }

        if (slot.CurrentTactic_2Info == null)
        {
            Debug.LogWarning("[数据校验] 槽位中的 CurrentTactic_2Info 为空");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 恢复默认数据并立即覆盖保存
    /// </summary>
    private void RestoreDefaultDataAndSave()
    {
        RestoreDefaultData();
        // 恢复后立即保存，把当前面板里的默认值写入存档，覆盖掉坏档
        SavePlayerData();
    }

    private void RestoreDefaultData()
    {
        PlayerSlotInfoPacksList.Clear();

        // 从备份里复制回来
        if (_defaultSlotInfoBackup != null)
        {
            PlayerSlotInfoPacksList.AddRange(_defaultSlotInfoBackup);
            Debug.Log($"[PlayerAndGameInfoManger] 已从备份恢复 {_defaultSlotInfoBackup.Count} 个槽位");
        }
        else
        {
            Debug.LogError("备份数据也丢失了！");
        }
    }

    // ================= 图形设置应用方法 =================

    public void ApplyGraphicsSettings()
    {
        QualitySettings.vSyncCount = 0;

        int targetFrameRate = GetTargetFrameRate(CurrentFPS);
        Application.targetFrameRate = targetFrameRate;
        Debug.Log($"[PlayerAndGameInfoManger] 设置目标帧率: {targetFrameRate}");

        int qualityLevel = GetQualityLevel(CurrentScreen);
        QualitySettings.SetQualityLevel(qualityLevel, true);
        Debug.Log($"[PlayerAndGameInfoManger] 设置画质级别: {qualityLevel} ({CurrentScreen})");
    }

    private int GetTargetFrameRate(FpsType fpsType)
    {
        int target = 60;
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

    private int GetQualityLevel(ScreenType screenType)
    {
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

        for (int i = 0; i < qualityNames.Length; i++)
        {
            if (qualityNames[i].IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return i;
        }

        Debug.LogWarning($"未找到匹配的画质名称 '{targetName}'，使用当前级别");
        return QualitySettings.GetQualityLevel();
    }
}

// ================= 存档数据类 =================
public class PlayerGameSaveData
{
    public List<SlotInfoPack> PlayerSlotInfoPacksList = new List<SlotInfoPack>();
    public int CurrentSlotIndex = 1;
    public List<PlayerCustomUIInfo> playerCustomUIInfoList = new List<PlayerCustomUIInfo>();
    public int CurrentFPS;
    public int CurrentScreen;

    public bool IsUseSinglePress_AimButton;
    public float AimSensitivity;
    public bool IsUseJoyStickMove;
}
