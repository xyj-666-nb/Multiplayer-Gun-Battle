using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
}

public class EquipmentConfigurationPanel : BasePanel
{
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

    private SlotInfoPack _currentSlotInfoPack;
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
            }
        }
    }

    [Header("槽位按钮相关")]
    public GameObject SlotButtonPrefabs;//槽位按钮预制体
    public Transform SlotButtonParent;//槽位按钮生成的父物体(自动排列)
    private string SlotButtonGroupName = "EquipmentSlotButtonGroup";//槽位按钮组名称
    private string ArmamentButtonGroupName = "ArmamentButtonGroup";//装备按钮组名称

    public void initArmamentPack()
    {
        GunInfoPack.DescriptionText = EquipmentDescriptionText;
        Tactical_1_InfoPack.DescriptionText = EquipmentDescriptionText;
        Tactical_2_InfoPack.DescriptionText = EquipmentDescriptionText;
        ArmorInfoPack.DescriptionText = EquipmentDescriptionText;
    }

    public void RegisterSlotButton()
    {
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
            textComp.text = (i + 1).ToString();//对TextMesh组件进行赋值

            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(SlotButtonGroupName, Obj.GetComponent<Button>(), SlotButtonTriggerEvent);
        }

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(SlotButtonGroupName, true);
    }

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

        // 手动选中第一个装备按钮
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ArmamentButtonGroupName, true);
    }

    //装备按钮点击事件
    public void ArmamentButtonTriggerEvent(string ButtonName)
    {
        // 更新描述文本为当前点击的装备的描述文本
        switch (ButtonName)
        {
            case "Gun_Button":
                //点击了枪械按钮，进行相应的逻辑处理
                EquipmentDescriptionText.text = _currentSlotInfoPack.CurrentGunInfo.description;//更新描述文本
                break;
            case "Tactic1_Button":
                //点击了战术道具1按钮，进行相应的逻辑处理
                EquipmentDescriptionText.text = _currentSlotInfoPack.CurrentTactic_1Info.Description;//更新描述文本
                break;
            case "Tactic2_Button":
                //点击了战术道具2按钮，进行相应的逻辑处理
                EquipmentDescriptionText.text = _currentSlotInfoPack.CurrentTactic_2Info.Description;//更新描述文本
                break;
            case "Armor_Button":
                //点击了护甲按钮，进行相应的逻辑处理
                break;
            default:
                Debug.LogWarning($"未知的装备按钮名称：{ButtonName}");
                break;
        }
    }

    public void UpdateCurrentSlotInfo(SlotInfoPack slotInfoPack)
    {
        if (slotInfoPack == null)
        {
            Debug.LogWarning("槽位信息包为空，跳过UI更新！");
            return;
        }
        if (slotInfoPack.CurrentGunInfo == null)
        {
            Debug.LogWarning("当前槽位无枪械信息！");
            GunInfoPack.NameText.text = "无枪械";
            GunInfoPack.IconImage.sprite = null;
            return;
        }

        //更新枪械
        GunInfoPack.NameText.text = slotInfoPack.CurrentGunInfo.Name;//更新名称
        GunInfoPack.IconImage.sprite = slotInfoPack.CurrentGunInfo.GunSprite;//更新图标
        //更新战术道具
        if (slotInfoPack.CurrentTactic_1Info != null)
        {
            Tactical_1_InfoPack.NameText.text = slotInfoPack.CurrentTactic_1Info.Name;
            Tactical_1_InfoPack.IconImage.sprite = slotInfoPack.CurrentTactic_1Info.UISprite;
        }

        if (slotInfoPack.CurrentTactic_2Info != null)
        {
            Tactical_2_InfoPack.NameText.text = slotInfoPack.CurrentTactic_2Info.Name;
            Tactical_2_InfoPack.IconImage.sprite = slotInfoPack.CurrentTactic_2Info.UISprite;
        }

        //更新护甲
    }

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        initArmamentPack();// 初始化信息包的描述文本引用
        RegisterSlotButton();//注册槽位按钮
        RegisterArmamentButton();//注册装备按钮
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
    }
    #endregion

    #region UI控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
            UImanager.Instance.HidePanel<EquipmentConfigurationPanel>();
    }
    #endregion

    #region 面板显隐以及特殊动画
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
    #endregion
}