using System.Collections; // 【新增】用于协程
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#region 数据结构定义
//战备的信息包
[System.Serializable]
public class ArmamentPack
{
    public TextMeshProUGUI NameText;//名称文本
    public Image IconImage;//图标
    [HideInInspector]
    public TextMeshProUGUI DescriptionText;
}

//槽位的信息包
[System.Serializable]
public class SlotInfoPack
{
    public GunInfo CurrentGunInfo;//当前枪械信息
    //然后就是战术道具1，2以及护甲信息类了
    public TacticInfo CurrentTactic_1Info;//当前战术道具1信息
    public TacticInfo CurrentTactic_2Info;//当前战术道具2信息
    public ArmorType CurrentArmorType;//护甲类型
}
#endregion

public class EquipmentConfigurationPanel : BasePanel
{
    #region UI引用与基础配置
    [Header("信息控件关联")]
    [Header("枪械区")]
    public ArmamentPack GunInfoPack;
    [Header("战术道具区")]
    public ArmamentPack Tactical_1_InfoPack;
    public ArmamentPack Tactical_2_InfoPack;
    [Header("护甲区")]
    public ArmamentPack ArmorInfoPack;

    [Header("装备描述Text")]
    public TextMeshProUGUI EquipmentDescriptionText;

    [Header("槽位按钮相关")]
    public GameObject SlotButtonPrefabs;//槽位按钮预制体
    public Transform SlotButtonParent;//槽位按钮生成的父物体(自动排列)
    private string SlotButtonGroupName = "EquipmentSlotButtonGroup";//槽位按钮组名称
    private string ArmamentButtonGroupName = "ArmamentButtonGroup";//装备按钮组名称
    public int SlotButtonIndex = 1;//当前正在浏览的索引

    [Header("当前槽位显示Text")]
    public TextMeshProUGUI SlotIndexText;

    private SlotInfoPack _currentSlotInfoPack;
    private bool _isFirstTimeInit = true; // 【新增】标记是否为第一次初始化数据
    #endregion

    #region 核心属性
    public SlotInfoPack CurrentSlotInfoPack
    {
        get => _currentSlotInfoPack;
        set
        {
            if (_currentSlotInfoPack != value)
            {
                _currentSlotInfoPack = value;
                //在这里带动更新
                UpdateCurrentSlotInfo(value);

                // 【新增】第一次有数据时，手动选中第一个装备按钮
                if (_isFirstTimeInit && value != null)
                {
                    _isFirstTimeInit = false;
                    StartCoroutine(SelectFirstEquipButtonNextFrame());
                }
            }
        }
    }
    #endregion

    #region 初始化与注册逻辑
    public void UpdateSlotIndexText()
    {
        if (SlotIndexText != null && PlayerAndGameInfoManger.Instance != null)
            SlotIndexText.text = PlayerAndGameInfoManger.Instance.SlotCount.ToString();
    }

    public void initArmamentPack()
    {
        GunInfoPack.DescriptionText = EquipmentDescriptionText;
        Tactical_1_InfoPack.DescriptionText = EquipmentDescriptionText;
        Tactical_2_InfoPack.DescriptionText = EquipmentDescriptionText;
        ArmorInfoPack.DescriptionText = EquipmentDescriptionText;
    }

    public void RegisterSlotButton()
    {
        if (SlotButtonParent == null || SlotButtonPrefabs == null) return;

        foreach (Transform child in SlotButtonParent)
        {
            PoolManage.Instance.PushObj(SlotButtonPrefabs, child.gameObject);
        }

        ButtonGroupManager.Instance.DestroyRadioGroup(SlotButtonGroupName);

        //开始注册槽位按钮的点击事件
        for (int i = 0; i < PlayerAndGameInfoManger.Instance.MaxSlotCount; i++)//注册4个槽位按钮，后续可以根据需要进行修改
        {
            var Obj = PoolManage.Instance.GetObj(SlotButtonPrefabs);
            //设置父物体
            Obj.transform.SetParent(SlotButtonParent, false);

            // 强制命名为Slot+数字
            string btnName = $"Slot{i + 1}";
            Obj.name = btnName;//命名为Slot1，Slot2，Slot3，Slot4
            var textComp = Obj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
                textComp.text = (i + 1).ToString();//对TextMesh组件进行赋值

            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(SlotButtonGroupName, Obj.GetComponent<Button>(), SlotButtonTriggerEvent);
        }

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(SlotButtonGroupName, true);
    }

    //装备按钮按钮组注册
    public void RegisterArmamentButton()
    {
        // 先清空旧的装备按钮组
        ButtonGroupManager.Instance.DestroyRadioGroup(ArmamentButtonGroupName);

        // 校验控件是否存在，避免空引用
        if (!controlDic.ContainsKey("Gun_Button") || controlDic["Gun_Button"] is not Button gunBtn)
        {
            Debug.LogError("未找到Gun_Button控件！");
            return;
        }
        if (!controlDic.ContainsKey("Tactic1_Button") || controlDic["Tactic1_Button"] is not Button tac1Btn)
        {
            Debug.LogError("未找到Tactic1_Button控件！");
            return;
        }
        if (!controlDic.ContainsKey("Tactic2_Button") || controlDic["Tactic2_Button"] is not Button tac2Btn)
        {
            Debug.LogError("未找到Tactic2_Button控件！");
            return;
        }
        if (!controlDic.ContainsKey("Armor_Button") || controlDic["Armor_Button"] is not Button armorBtn)
        {
            Debug.LogError("未找到Armor_Button控件！");
            return;
        }

        // 注册装备按钮（使用兼容的带参方法）
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ArmamentButtonGroupName, gunBtn, ArmamentButtonTriggerEvent);
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ArmamentButtonGroupName, tac1Btn, ArmamentButtonTriggerEvent);
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ArmamentButtonGroupName, tac2Btn, ArmamentButtonTriggerEvent);
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ArmamentButtonGroupName, armorBtn, ArmamentButtonTriggerEvent);

        // ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ArmamentButtonGroupName, true);
    }

    // 协程：等一帧，确保UI和数据都就绪后再选中装备按钮
    private IEnumerator SelectFirstEquipButtonNextFrame()
    {
        yield return null; // 等待一帧
        if (ButtonGroupManager.Instance != null)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ArmamentButtonGroupName, true);
        }
    }
    #endregion

    #region 按钮事件处理
    //槽位按钮点击事件
    public void SlotButtonTriggerEvent(string ButtonName)
    {
        int slotNum = -1;
        string numStr = ButtonName.Replace("Slot", "").Trim(); // Trim去除可能的空格
        if (int.TryParse(numStr, out slotNum))
        {
            Debug.Log($"从按钮名称解析：{ButtonName} → {slotNum}");
        }
        else
        {
            Debug.Log($"尝试从按钮文本解析数字（名称解析失败）");
            // 找到触发事件的按钮对象
            Button targetBtn = null;
            foreach (Transform child in SlotButtonParent)
            {
                if (child.name == ButtonName)
                {
                    targetBtn = child.GetComponent<Button>();
                    break;
                }
            }
            // 从按钮文本解析数字
            if (targetBtn != null)
            {
                var textComp = targetBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null && int.TryParse(textComp.text, out slotNum))
                {
                    Debug.Log($"从按钮文本解析：{textComp.text} → {slotNum}");
                }
                else
                {
                    Debug.LogError($"按钮{ButtonName}名称和文本都无法解析为数字！文本内容：{textComp?.text ?? "无"}");
                    return;
                }
            }
            else
            {
                Debug.LogError($"未找到名称为{ButtonName}的槽位按钮！");
                return;
            }
        }

        // 计算槽位索引
        int slotIndex = slotNum - 1;
        SlotButtonIndex = slotNum;
        var slotList = PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList;
        if (slotList == null)
        {
            Debug.LogError("PlayerSlotInfoPacksList 未初始化！");
            return;
        }
        if (slotIndex < 0 || slotIndex >= slotList.Count)
        {
            Debug.LogError($"槽位索引{slotIndex}无效！当前最大槽位数量：{slotList.Count}，解析的数字：{slotNum}");
            return;
        }

        // 安全更新槽位信息
        CurrentSlotInfoPack = slotList[slotIndex];
    }

    //装备按钮点击事件
    public void ArmamentButtonTriggerEvent(string ButtonName)
    {
        // 【修复】核心保护：如果文本组件或当前槽位为空，直接返回
        if (EquipmentDescriptionText == null) return;
        if (_currentSlotInfoPack == null)
        {
            EquipmentDescriptionText.text = "数据加载中...";
            return;
        }

        // 更新描述文本为当前点击的装备的描述文本
        switch (ButtonName)
        {
            case "Gun_Button":
                if (_currentSlotInfoPack.CurrentGunInfo != null)
                {
                    EquipmentDescriptionText.text = _currentSlotInfoPack.CurrentGunInfo.description;
                }
                else
                {
                    EquipmentDescriptionText.text = "未装备枪械";
                }
                break;
            case "Tactic1_Button":
                if (_currentSlotInfoPack.CurrentTactic_1Info != null)
                {
                    EquipmentDescriptionText.text = _currentSlotInfoPack.CurrentTactic_1Info.Description;
                }
                else
                {
                    EquipmentDescriptionText.text = "未装备战术道具1";
                }
                break;
            case "Tactic2_Button":
                if (_currentSlotInfoPack.CurrentTactic_2Info != null)
                {
                    EquipmentDescriptionText.text = _currentSlotInfoPack.CurrentTactic_2Info.Description;
                }
                else
                {
                    EquipmentDescriptionText.text = "未装备战术道具2";
                }
                break;
            case "Armor_Button":
                // 这里也可以加个检查，确保 MilitaryManager 正常
                var armorPack = MilitaryManager.Instance?.GetArmorInfoPack(_currentSlotInfoPack.CurrentArmorType);
                if (armorPack != null)
                {
                    EquipmentDescriptionText.text = armorPack.armorDescription;
                }
                else
                {
                    EquipmentDescriptionText.text = "未装备护甲";
                }
                break;
            default:
                Debug.LogWarning($"未知的装备按钮名称：{ButtonName}");
                break;
        }
    }
    #endregion

    #region UI更新逻辑
    public void UpdateCurrentSlotInfo(SlotInfoPack slotInfoPack)
    {
        if (slotInfoPack == null)
        {
            Debug.LogWarning("槽位信息包为空，跳过UI更新！");
            return;
        }

        //更新枪械
        if (slotInfoPack.CurrentGunInfo != null && GunInfoPack.NameText != null)
        {
            GunInfoPack.NameText.text = slotInfoPack.CurrentGunInfo.Name;
            if (GunInfoPack.IconImage != null)
                GunInfoPack.IconImage.sprite = slotInfoPack.CurrentGunInfo.GunSprite;
        }
        else
        {
            if (GunInfoPack.NameText != null) GunInfoPack.NameText.text = "无枪械";
            if (GunInfoPack.IconImage != null) GunInfoPack.IconImage.sprite = null;
        }

        //更新战术道具1
        if (slotInfoPack.CurrentTactic_1Info != null)
        {
            if (Tactical_1_InfoPack.NameText != null)
                Tactical_1_InfoPack.NameText.text = slotInfoPack.CurrentTactic_1Info.Name;
            if (Tactical_1_InfoPack.IconImage != null)
                Tactical_1_InfoPack.IconImage.sprite = slotInfoPack.CurrentTactic_1Info.UISprite;
        }
        else
        {
            if (Tactical_1_InfoPack.NameText != null) Tactical_1_InfoPack.NameText.text = "无";
            if (Tactical_1_InfoPack.IconImage != null) Tactical_1_InfoPack.IconImage.sprite = null;
        }

        //更新战术道具2
        if (slotInfoPack.CurrentTactic_2Info != null)
        {
            if (Tactical_2_InfoPack.NameText != null)
                Tactical_2_InfoPack.NameText.text = slotInfoPack.CurrentTactic_2Info.Name;
            if (Tactical_2_InfoPack.IconImage != null)
                Tactical_2_InfoPack.IconImage.sprite = slotInfoPack.CurrentTactic_2Info.UISprite;
        }
        else
        {
            if (Tactical_2_InfoPack.NameText != null) Tactical_2_InfoPack.NameText.text = "无";
            if (Tactical_2_InfoPack.IconImage != null) Tactical_2_InfoPack.IconImage.sprite = null;
        }

        //更新护甲
        var InfoPack = MilitaryManager.Instance?.GetArmorInfoPack(slotInfoPack.CurrentArmorType);
        if (InfoPack != null)
        {
            if (ArmorInfoPack.NameText != null) ArmorInfoPack.NameText.text = InfoPack.armorName;
            if (ArmorInfoPack.DescriptionText != null) ArmorInfoPack.DescriptionText.text = InfoPack.armorDescription;
            if (ArmorInfoPack.IconImage != null) ArmorInfoPack.IconImage.sprite = InfoPack.UISprite;
        }
    }
    #endregion

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        initArmamentPack();// 初始化信息包的描述文本引用
        RegisterSlotButton();//注册槽位按钮
        RegisterArmamentButton();//注册装备按钮
        UpdateSlotIndexText();//进行初始化
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 重置标记，防止如果面板复用出现问题
        _isFirstTimeInit = true;
    }
    #endregion

    #region UI交互逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
        {
            UImanager.Instance.HidePanel<EquipmentConfigurationPanel>();
        }
        else if (controlName == "Button_SetCurrentSkot")
        {
            //玩家点击设置当前槽位
            PlayerAndGameInfoManger.Instance.SetSlotInfoPack(SlotButtonIndex);
            UpdateSlotIndexText();//更新一下
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1, "设置成功");
        }
    }
    #endregion

    bool IsPlayerPanelHide = false;

    #region 面板显隐以及特殊动画
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        if (IsPlayerPanelHide)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
            IsPlayerPanelHide = false;
        }
    }
    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        if (UImanager.Instance.GetPanel<PlayerPanel>() != null)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().SimpleHidePanel();
            IsPlayerPanelHide = true;
        }
    }
    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
    #endregion
}