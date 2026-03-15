using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Events;

public class ArmamentPanel : BasePanel
{
    [Header("当前枪械的信息")]
    public TextMeshProUGUI GunName;
    public TextMeshProUGUI GunDescribe;
    public Image GunSprite;

    [Header("军械选择的按钮父对象以及预制体")]
    public Transform TypeButtonParent;
    public GameObject TypeButtonPrefab;
    private string ButtonGroupRegisterName_Armament = "ArmamentTypeGroup"; //枪械类型分组
    private List<GameObject> CreatedArmamentButtons = new List<GameObject>();

    [Header("具体的装备按钮父对象以及预制体")]
    public Transform ArmamentButtonParent;
    public GameObject SpecialArmamentButtonPrefab;
    private string ButtonGroupRegisterName_SpecialArmament = "SpecialArmamentGroup"; //具体枪械/战术道具分组
    private List<GameObject> CreatedSpecialArmamentButtons = new List<GameObject>();

    [Header("军备种类的按钮（枪械/投掷物/护甲）")]
    public Button Gun_Button;
    public Button ThrowObj_Button;
    public Button Armor_Button;
    private string ButtonGroupRegisterName_ArmamentType = "ArmamentMainTypeGroup"; //主分类分组

    [Header("装备数值显示条")]
    public Transform ArmamentValueSliderParent;
    public GameObject ArmamentValueSliderPrefab;

    [Header("枪械装备槽位选择")]
    public List<Button> SlotButtonList;
    private string ButtonGroupRegisterName_Slot = "ArmamentSlotGroup"; //槽位分组
    public int CurrentChooseSlotIndex = 1; //当前选择的枪械槽位索引

    [Header("战术道具槽位选择")]
    public CanvasGroup TacticSlotButtonArea;//战术道具的槽位选择区域
    public int CurrentChooseTacticSlotIndex = 1;//当前选择的战术道具槽位索引
    public List<Button> TacticSlotButtonList;
    private string ButtonGroupRegisterName_TacticSlot = "TacticSlotGroup"; //战术槽位分组

    public GunInfo CurrentChooseGunInfo;//当前选择的枪械信息
    public TacticInfo CurrentChooseTacticInfo;//当前选择的战术道具信息
    public ArmorInfoPack CurrentChooseArmorInfoPack;//当前选择的护甲信息

    #region 数值滑块管理
    /// <summary>
    /// 清理已创建的数值滑块
    /// </summary>
    private void ClearValueSliders()
    {
        foreach (var sliderObj in CreatedValueSliders)
        {
            if (sliderObj != null)
            {
                // 增加对象池空值检查
                if (PoolManage.Instance != null)
                {
                    PoolManage.Instance.PushObj(ArmamentValueSliderPrefab, sliderObj);
                }
                else
                {
                    Destroy(sliderObj); // 备用方案：对象池为空时直接销毁
                }
            }
        }
        CreatedValueSliders.Clear();
    }
    #endregion

    [Header("数值滑块管理")]
    private List<GameObject> CreatedValueSliders = new List<GameObject>(); //管理生成的数值滑块

    #region 生命周期
    public override void Awake()
    {
        base.Awake();

        // 空值检查：关键组件必须赋值
        if (Gun_Button == null || ThrowObj_Button == null || Armor_Button == null)
        {
            Debug.LogError("主分类按钮（枪械/投掷物/护甲）未赋值！");
            return;
        }

        // 注册主分类按钮（枪械、投掷物、护甲）
        ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_ArmamentType, Gun_Button, CreateAndRegisterArmamentButton);
        ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_ArmamentType, ThrowObj_Button, CreateAndRegisterTacticButton);
        ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_ArmamentType, Armor_Button, OnArmorButtonClick);

        // 注册枪械槽位按钮
        RegisterGunSlotButtons();
        // 注册战术槽位按钮
        RegisterTacticSlotButtons();

        // 初始隐藏战术槽位区域
        SetTacticSlotAreaActive(false);
    }

    /// <summary>
    /// 注册枪械槽位按钮
    /// </summary>
    private void RegisterGunSlotButtons()
    {
        if (SlotButtonList == null || SlotButtonList.Count == 0)
        {
            Debug.LogWarning("枪械槽位按钮列表为空！");
            return;
        }

        for (int i = 0; i < SlotButtonList.Count; i++)
        {
            int index = i; // 捕获当前索引
            if (SlotButtonList[i] == null)
            {
                Debug.LogWarning($"第{index}个枪械槽位按钮为空！");
                continue;
            }

            ButtonGroupManager.Instance?.AddRadioButtonToGroup_Str(ButtonGroupRegisterName_Slot, SlotButtonList[i], (str) =>
            {
                CurrentChooseSlotIndex = ExtractNumberWithRegex(str);
                Debug.Log($"选中枪械槽位：{CurrentChooseSlotIndex}");
            });
        }
    }

    /// <summary>
    /// 注册战术槽位按钮（仅2个槽位）
    /// </summary>
    private void RegisterTacticSlotButtons()
    {
        if (TacticSlotButtonList == null || TacticSlotButtonList.Count == 0)
        {
            Debug.LogWarning("战术槽位按钮列表为空！");
            return;
        }

        // 校验战术槽位数量（强制限制为2个）
        if (TacticSlotButtonList.Count != 2)
        {
            Debug.LogWarning($"战术槽位按钮数量应为2个，当前为{TacticSlotButtonList.Count}个！");
        }

        for (int i = 0; i < TacticSlotButtonList.Count; i++)
        {
            int index = i; // 捕获当前索引
            if (TacticSlotButtonList[i] == null)
            {
                Debug.LogWarning($"第{index}个战术槽位按钮为空！");
                continue;
            }

            ButtonGroupManager.Instance?.AddRadioButtonToGroup_Str(ButtonGroupRegisterName_TacticSlot, TacticSlotButtonList[i], (str) =>
            {
                CurrentChooseTacticSlotIndex = ExtractNumberWithRegex(str);
                Debug.Log($"选中战术槽位：{CurrentChooseTacticSlotIndex}");
            });
        }

        // 默认选中第一个战术槽位
        if (TacticSlotButtonList.Count > 0 && TacticSlotButtonList[0] != null)
        {
            ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_TacticSlot, TacticSlotButtonList[0]);
        }
    }

    public int ExtractNumberWithRegex(string inputStr)
    {
        if (string.IsNullOrEmpty(inputStr))
        {
            Debug.LogWarning("传入的字符串为空！");
            return -1;
        }

        // 匹配所有连续数字
        Match match = Regex.Match(inputStr, @"\d+");
        if (match.Success)
        {
            return int.Parse(match.Value);
        }
        else
        {
            Debug.LogWarning($"字符串 {inputStr} 中未找到数字！");
            return -1;
        }
    }

    public override void Start()
    {
        base.Start();
        // 默认选中枪械按钮
        if (Gun_Button != null)
        {
            ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_ArmamentType, Gun_Button);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁时清理所有创建的分组
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_Armament);
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_SpecialArmament);
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_ArmamentType);
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_Slot);
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_TacticSlot);

        // 额外清理所有创建的对象
        ClearCreatedArmamentButtons();
        ClearCreatedSpecialArmamentButtons();
        ClearValueSliders();
    }
    #endregion

    #region 主分类按钮回调
    /// <summary>
    /// 护甲按钮点击回调
    /// </summary>
    private void OnArmorButtonClick()
    {
        ClearCreatedArmamentButtons(); // 清理类型按钮
        ClearCreatedSpecialArmamentButtons(); // 清理具体装备按钮
        ClearValueSliders(); // 清理数值滑块
        SetTacticSlotAreaActive(false); // 隐藏战术槽位

        // 护甲面板默认显示
        GunName.text = "护甲";
        GunDescribe.text = "请选择护甲类型";
        GunSprite.sprite = null;
        ResetGunSpriteTransform();

        // 空值检查：预制体和父对象必须赋值
        if (TypeButtonPrefab == null || TypeButtonParent == null)
        {
            Debug.LogError("TypeButtonPrefab 或 TypeButtonParent 未赋值！");
            return;
        }

        var buttonObj = CreateTypeButtonPrefab();
        if (buttonObj == null) return;

        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null) buttonText.text = "单兵护甲";
        if (button == null)
        {
            Debug.LogError("创建的护甲类型按钮缺少Button组件！");
            return;
        }

        // 添加点击监听：切换特殊装备列表
        button.onClick.AddListener(() =>
        {
            CreateCreateAndRegisterSpecialArmorButton();//生成对应的护甲按钮
        });

        // 注册到正确的分组，并选中第一个按钮
        ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_Armament, button);
        ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_Armament, button);
    }

    public void CreateCreateAndRegisterSpecialArmorButton()//生成具体护甲按钮
    {
        // 先清理旧按钮
        ClearCreatedSpecialArmamentButtons();

        // 核心空值检查：MilitaryManager单例
        if (MilitaryManager.Instance == null)
        {
            Debug.LogError("MilitaryManager.Instance 为空！");
            return;
        }

        ArmorType[] allArmorTypes = (ArmorType[])Enum.GetValues(typeof(ArmorType));
        foreach (var Type in allArmorTypes)
        {
            if(Type== ArmorType.Empty_handed)
                continue;//不创建空手按钮

            // 开始创建对应的具体按钮
            GameObject buttonObj = CreateSpecialArmamentButtonPrefab();
            if (buttonObj == null)
            {
                Debug.LogError("创建护甲具体按钮失败！");
                continue;
            }

            // 获取护甲信息并做空值检查
            var InfoPack = MilitaryManager.Instance.GetArmorInfoPack(Type);
            if (InfoPack == null)
            {
                Debug.LogWarning($"护甲类型 {Type} 未找到配置信息！");
                continue;
            }

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null) 
                buttonText.text = InfoPack.armorName;
            if (button == null)
            {
                Debug.LogError("护甲具体按钮缺少Button组件！");
                continue;
            }

            // 设置护甲图标（增加多层空值检查）
            Transform imgTrans = buttonObj.transform.GetChild(1);
            if (imgTrans != null)
            {
                Image img = imgTrans.GetComponent<Image>();
                if (img != null && InfoPack.HelmetSprite != null)
                {
                    img.sprite = InfoPack.HelmetSprite;
                    //设置图片的缩放大小适配各类型图片
                    img.GetComponent<RectTransform>().localScale= new Vector3(0.5f, 0.5f, 1);
                }
                else if (img != null)
                {
                    Debug.LogWarning($"护甲 {InfoPack.armorName} 缺少图标！");
                    img.sprite = null;
                }
            }

            // 注册到单选组
            ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_SpecialArmament, button);

            // 捕获当前InfoPack
            ArmorInfoPack currentInfo = InfoPack;
            // 添加点击监听：更新界面信息
            button.onClick.AddListener(() =>
            {
                UpdateInfoArmor(currentInfo);
                //设置当前选择的护甲信息
                CurrentChooseArmorInfoPack = currentInfo;
                //清空其他选择
                CurrentChooseGunInfo = null;
                CurrentChooseTacticInfo = null;
            });
        }

        // 选中第一个护甲具体按钮（分组名匹配）
        ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_SpecialArmament, true);
    }

    /// <summary>
    /// 创建并注册枪械类型按钮
    /// </summary>
    public void CreateAndRegisterArmamentButton()
    {
        // 先清理旧按钮
        ClearCreatedArmamentButtons();
        // 清理具体装备按钮和数值滑块
        ClearCreatedSpecialArmamentButtons();
        ClearValueSliders();
        SetTacticSlotAreaActive(false); // 隐藏战术槽位

        if (MilitaryManager.Instance == null)
        {
            Debug.LogError("MilitaryManager.Instance 为空！");
            return;
        }

        GunType[] allGunTypes = (GunType[])Enum.GetValues(typeof(GunType));
        foreach (GunType gunType in allGunTypes)
        {
            var buttonObj = CreateTypeButtonPrefab();
            if (buttonObj == null) continue;

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = MilitaryManager.Instance.GetChineseGunTypeName(gunType);

            if (button == null) continue;

            // 注册到单选组
            ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_Armament, button);

            // 捕获当前gunType
            GunType currentGunType = gunType;
            // 添加点击监听：切换特殊装备列表
            button.onClick.AddListener(() =>
            {
                Debug.Log("Selected Gun Type: " + currentGunType);
                CreateAndRegisterSpecialArmamentButton(currentGunType);
            });
        }

        ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_Armament, true);
    }

    /// <summary>
    /// 创建并注册战术道具类型按钮
    /// </summary>
    public void CreateAndRegisterTacticButton()
    {
        // 核心修复：先清理旧的枪械类型按钮
        ClearCreatedArmamentButtons();
        // 清理具体装备按钮和数值滑块
        ClearCreatedSpecialArmamentButtons();
        ClearValueSliders();
        SetTacticSlotAreaActive(true); // 显示战术槽位

        if (MilitaryManager.Instance == null)
        {
            Debug.LogError("MilitaryManager.Instance 为空！");
            return;
        }

        TacticBigType[] allTacticTypes = (TacticBigType[])Enum.GetValues(typeof(TacticBigType));
        foreach (var TacticBig in allTacticTypes)
        {
            var buttonObj = CreateTypeButtonPrefab();
            if (buttonObj == null) continue;

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = MilitaryManager.Instance.GetChineseTacticBigTypeName(TacticBig);

            if (button == null) continue;

            // 注册到单选组
            ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_Armament, button);

            // 捕获当前TacticBig
            TacticBigType currentTacticBig = TacticBig;
            // 添加点击监听：切换特殊装备列表
            button.onClick.AddListener(() =>
            {
                CreateCreateAndRegisterSpecialTacticButton(currentTacticBig);
            });
        }

        // 选中第一个战术大类按钮
        ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_Armament, true);
    }
    #endregion

    #region 战术槽位显示/隐藏控制
    /// <summary>
    /// 设置战术槽位区域的显隐和交互状态
    /// </summary>
    /// <param name="isActive">是否激活</param>
    private void SetTacticSlotAreaActive(bool isActive)
    {
        if (TacticSlotButtonArea == null)
        {
            Debug.LogError("TacticSlotButtonArea 未赋值！");
            return;
        }

        // CanvasGroup 控制显隐和交互
        TacticSlotButtonArea.alpha = isActive ? 1f : 0f; // 透明度
        TacticSlotButtonArea.interactable = isActive; // 可交互性
        TacticSlotButtonArea.blocksRaycasts = isActive; // 射线检测（是否响应点击）

        // 额外确保槽位按钮的交互状态
        if (TacticSlotButtonList != null)
        {
            foreach (var btn in TacticSlotButtonList)
            {
                if (btn != null)
                {
                    btn.interactable = isActive;
                }
            }
        }
    }
    #endregion

    #region 枪械/战术类型按钮管理
    /// <summary>
    /// 清理已创建的枪械/战术类型按钮，并从单选组中移除
    /// </summary>
    private void ClearCreatedArmamentButtons()
    {
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_Armament);

        foreach (var obj in CreatedArmamentButtons)
        {
            if (obj != null)
            {
                if (PoolManage.Instance != null)
                {
                    PoolManage.Instance.PushObj(TypeButtonPrefab, obj);
                }
                else
                {
                    Destroy(obj);
                }
            }
        }
        CreatedArmamentButtons.Clear();

        // 额外清理父对象下的残留（防止对象池异常导致的残留）
        if (TypeButtonParent != null)
        {
            foreach (Transform child in TypeButtonParent)
            {
                if (child.gameObject != null)
                {
                    if (PoolManage.Instance != null)
                    {
                        PoolManage.Instance.PushObj(TypeButtonPrefab, child.gameObject);
                    }
                    else
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
    }

    public GameObject CreateTypeButtonPrefab()
    {
        if (TypeButtonPrefab == null || TypeButtonParent == null)
        {
            Debug.LogError("TypeButtonPrefab 或 TypeButtonParent 未赋值！");
            return null;
        }

        GameObject buttonObj = null;
        if (PoolManage.Instance != null)
        {
            buttonObj = PoolManage.Instance.GetObj(TypeButtonPrefab);
        }
        else
        {
            // 备用方案：对象池为空时直接实例化
            buttonObj = Instantiate(TypeButtonPrefab);
            Debug.LogWarning("PoolManage.Instance 为空，直接实例化按钮预制体！");
        }

        if (buttonObj != null)
        {
            buttonObj.transform.SetParent(TypeButtonParent, false);
            CreatedArmamentButtons.Add(buttonObj);
        }
        return buttonObj;
    }

    public GameObject CreateSpecialArmamentButtonPrefab()
    {
        if (SpecialArmamentButtonPrefab == null || ArmamentButtonParent == null)
        {
            Debug.LogError("SpecialArmamentButtonPrefab 或 ArmamentButtonParent 未赋值！");
            return null;
        }

        GameObject buttonObj = null;
        if (PoolManage.Instance != null)
        {
            buttonObj = PoolManage.Instance.GetObj(SpecialArmamentButtonPrefab);
        }
        else
        {
            // 备用方案：对象池为空时直接实例化
            buttonObj = Instantiate(SpecialArmamentButtonPrefab);
            Debug.LogWarning("PoolManage.Instance 为空，直接实例化特殊装备按钮预制体！");
        }

        if (buttonObj != null)
        {
            buttonObj.transform.SetParent(ArmamentButtonParent, false);
            CreatedSpecialArmamentButtons.Add(buttonObj);
        }
        return buttonObj;
    }
    #endregion

    #region 具体装备按钮管理
    /// <summary>
    /// 清理已创建的具体枪械/战术道具按钮，并从单选组中移除
    /// </summary>
    private void ClearCreatedSpecialArmamentButtons()
    {
        ButtonGroupManager.Instance?.DestroyRadioGroup(ButtonGroupRegisterName_SpecialArmament);

        foreach (var obj in CreatedSpecialArmamentButtons)
        {
            if (obj != null)
            {
                if (PoolManage.Instance != null)
                {
                    PoolManage.Instance.PushObj(SpecialArmamentButtonPrefab, obj);
                }
                else
                {
                    Destroy(obj);
                }
            }
        }
        CreatedSpecialArmamentButtons.Clear();

        // 额外清理父对象下的残留
        if (ArmamentButtonParent != null)
        {
            foreach (Transform child in ArmamentButtonParent)
            {
                if (child.gameObject != null)
                {
                    if (PoolManage.Instance != null)
                    {
                        PoolManage.Instance.PushObj(SpecialArmamentButtonPrefab, child.gameObject);
                    }
                    else
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 创建并注册具体枪械按钮
    /// </summary>
    public void CreateAndRegisterSpecialArmamentButton(GunType currentGunType)
    {
        ClearCreatedSpecialArmamentButtons();

        if (MilitaryManager.Instance == null)
        {
            Debug.LogError("MilitaryManager.Instance 为空！");
            return;
        }

        // 获取该类型下的所有枪械信息
        List<GunInfo> gunInfoList = MilitaryManager.Instance.GetGunTypeInfo(currentGunType);
        if (gunInfoList == null || gunInfoList.Count == 0)
        {
            Debug.LogWarning($"枪械类型 {currentGunType} 下无配置信息！");
            return;
        }

        foreach (GunInfo gunInfo in gunInfoList)
        {
            if (gunInfo == null) continue; // 空值检查

            GameObject buttonObj = CreateSpecialArmamentButtonPrefab();
            if (buttonObj == null) continue;

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null) buttonText.text = gunInfo.Name;

            // 空值检查：防止图片组件缺失
            Transform imgTrans = buttonObj.transform.GetChild(1);
            if (imgTrans != null)
            {
                Image img = imgTrans.GetComponent<Image>();
                if (img != null && gunInfo.GunSprite != null)
                {
                    img.sprite = gunInfo.GunSprite;
                    img.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                }
                else if (img != null)
                {
                    Debug.LogWarning($"枪械 {gunInfo.Name} 缺少图标");
                    img.sprite = null;
                }
            }

            if (button == null) continue;

            // 注册到单选组
            ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_SpecialArmament, button);

            // 捕获当前gunInfo
            GunInfo currentGunInfo = gunInfo;
            // 添加点击监听：更新界面信息
            button.onClick.AddListener(() =>
            {
                UpdateCurrentInfo(currentGunInfo);
                //设置当前选择的枪械信息
                CurrentChooseGunInfo = currentGunInfo;
                //清空其他选择
                CurrentChooseTacticInfo = null;
                CurrentChooseArmorInfoPack = null;
            });
        }

        ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_SpecialArmament, true);
    }

    public void CreateCreateAndRegisterSpecialTacticButton(TacticBigType Type)
    {
        ClearCreatedSpecialArmamentButtons();

        if (MilitaryManager.Instance == null)
        {
            Debug.LogError("MilitaryManager.Instance 为空！");
            return;
        }

        List<TacticType> tacticTypesList = MilitaryManager.Instance.GetTacticTypesByBigType(Type);
        if (tacticTypesList == null || tacticTypesList.Count == 0)
        {
            Debug.LogWarning($"战术大类 {Type} 下无配置信息！");
            return;
        }

        foreach (var tacticType in tacticTypesList)
        {
            GameObject buttonObj = CreateSpecialArmamentButtonPrefab();
            if (buttonObj == null) continue;

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = MilitaryManager.Instance.GetChineseTacticTypeName(tacticType);

            // 赋值图标（增加空值检查）
            Transform imgTrans = buttonObj.transform.GetChild(1);
            if (imgTrans != null)
            {
                Image img = imgTrans.GetComponent<Image>();
                if (img != null)
                {
                    Sprite tacticSprite = MilitaryManager.Instance.GetTacticUISprite(tacticType);
                    if (tacticSprite != null)
                    {
                        img.sprite = tacticSprite;
                        img.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    }
                    else
                    {
                        Debug.LogWarning($"战术道具 {tacticType} 缺少UI图标");
                        img.sprite = null;
                    }
                }
            }

            if (button == null) continue;

            // 注册到单选组
            ButtonGroupManager.Instance?.AddRadioButtonToGroup(ButtonGroupRegisterName_SpecialArmament, button);

            // 捕获当前tacticType
            TacticType currentTacticType = tacticType;
            // 添加点击监听：更新界面信息
            button.onClick.AddListener(() =>
            {
                UpdateCurrentTacticInfo(currentTacticType);
                //设置当前选择的战术道具信息
                CurrentChooseTacticInfo = MilitaryManager.Instance.GetTacticInfo(currentTacticType);
                //清空其他选择
                CurrentChooseGunInfo = null;
                CurrentChooseArmorInfoPack = null;
            });
        }

        ButtonGroupManager.Instance?.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_SpecialArmament, true);
    }
    #endregion

    #region 界面信息更新
    public void UpdateCurrentInfo(GunInfo info)
    {
        if (info == null)
        {
            Debug.LogWarning("GunInfo 为空，无法更新界面");
            return;
        }

        if (GunName != null) GunName.text = info.Name;
        if (GunDescribe != null) GunDescribe.text = info.description;

        // 空值检查
        if (GunSprite != null)
        {
            if (info.GunBodySprite != null)
            {
                GunSprite.sprite = info.GunBodySprite;
            }
            else
            {
                Debug.LogWarning($"枪械 {info.Name} 缺少主图标");
                GunSprite.sprite = null;
            }
        }

        // 枪械图片大小调整
        ResetGunSpriteTransform();
        if (info.Name == "UZI" && GunSprite != null)
        {
            GunSprite.rectTransform.localScale = new Vector3(0.6f, 0.6f, 1);
        }
        else if (info.Name == "AKM" && GunSprite != null)
        {
            GunSprite.rectTransform.localScale = new Vector3(2, 0.7f, 1);
        }
        else if ((info.Name == "M110" || info.Name == "Vector-45") && GunSprite != null)
        {
            GunSprite.rectTransform.localScale = new Vector3(0.8f, 0.8f, 1);
        }
        else if (info.Name == "AUG" && GunSprite != null)
        {
            GunSprite.rectTransform.localScale = new Vector3(0.6f, 0.6f, 1);
        }
        else if (info.Name == "M249" && GunSprite != null)
        {
            GunSprite.rectTransform.localScale = new Vector3(0.8f, 0.8f, 1);
        }

        ClearValueSliders();

        // 伤害（最大值100）
        CreateValueSlider(info.Damage, "伤害", "100");
        // 射程（最大值500）
        CreateValueSlider(info.Range, "射程", "500");
        // 后坐力（最大值5）
        CreateValueSlider(info.Recoil, "后坐力", "5");
        // 射速（最大值1000）
        CreateValueSlider(info.RateOfFires, "射速", "1000");
        // 换弹时间（最大值5）
        CreateValueSlider(info.ReloadTime, "换弹时间", "5");
        // 精准度（最大值100）
        CreateValueSlider(info.Accuracy, "精准度", "100");
        // 枪械震动（最大值5）
        CreateValueSlider(info.ShackStrength, "枪械震动", "5");
        // 子弹速度（最大值1000）
        CreateValueSlider(info.BulletSpeed, "子弹速度", "1000");
        // 单个弹夹弹药量（最大值50）
        CreateValueSlider(info.Bullet_capacity, "弹夹容量", "50");
    }

    // 重置枪械图片Transform
    private void ResetGunSpriteTransform()
    {
        if (GunSprite != null)
        {
            GunSprite.rectTransform.localScale = Vector3.one;
            GunSprite.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void UpdateCurrentTacticInfo(TacticType tacticType)
    {
        if (MilitaryManager.Instance == null)
        {
            Debug.LogError("MilitaryManager.Instance 为空！");
            return;
        }

        // 根据战术设备的类型获取信息进行更新
        TacticInfo info = MilitaryManager.Instance.GetTacticInfo(tacticType);
        if (info == null)
        {
            Debug.LogWarning($"战术道具 {tacticType} 未找到配置信息");
            if (GunName != null) GunName.text = "未知道具";
            if (GunDescribe != null) GunDescribe.text = "暂无描述";
            if (GunSprite != null) GunSprite.sprite = null;
            ClearValueSliders();
            return;
        }

        // 对界面进行更新
        if (GunName != null) GunName.text = info.Name;//名字
        if (GunDescribe != null) GunDescribe.text = info.Description;//描述的信息

        // 空值检查
        if (GunSprite != null)
        {
            if (info.GameBodySprite != null)
            {
                GunSprite.sprite = info.GameBodySprite;//战术道具的图片
            }
            else
            {
                Debug.LogWarning($"战术道具 {info.Name} 缺少主图标");
                GunSprite.sprite = null;
            }
        }

        // 投掷物图片缩小
        ResetGunSpriteTransform();
        if ((tacticType == TacticType.Smoke || tacticType == TacticType.Grenade) && GunSprite != null)
        {
            GunSprite.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1);
        }

        // 清理数值滑块
        ClearValueSliders();
    }

    /// <summary>
    /// 更新护甲面板信息
    /// </summary>
    /// <param name="InfoPack">护甲信息包</param>
    public void UpdateInfoArmor(ArmorInfoPack InfoPack)
    {
        // 清空旧滑块
        ClearValueSliders();

        // 更新文本
        if (GunName != null)
            GunName.text = InfoPack.armorName;
        if (GunDescribe != null)
            GunDescribe.text = InfoPack.armorDescription;

        // 重置并设置护甲图片
        ResetGunSpriteTransform();
        if (GunSprite != null)
        {
            if (InfoPack.HelmetSprite != null)
            {
                GunSprite.sprite = InfoPack.UISprite;//显示UI图
                GunSprite.rectTransform.localScale = new Vector3(0.3f, 0.3f, 1);
            }
        }

        // 更新数值滑块
        CreateValueSlider(InfoPack.HealthAdd, "生命加成", "50");
        CreateValueSlider(InfoPack.SpeedAdd, "速度加成", "1");//速度加成
    }

    /// <summary>
    /// 生成单个数值滑块并设置数值
    /// </summary>
    /// <param name="targetValue">目标数值</param>
    /// <param name="valueName">数值名称</param>
    /// <param name="maxValue">最大值</param>
    private void CreateValueSlider(float targetValue, string valueName, string maxValue)
    {
        // 空值检查：预制体和父对象
        if (ArmamentValueSliderPrefab == null || ArmamentValueSliderParent == null)
        {
            Debug.LogError("ArmamentValueSliderPrefab 或 ArmamentValueSliderParent 未赋值！");
            return;
        }

        // 从对象池获取滑块预制体
        GameObject sliderObj = null;
        if (PoolManage.Instance != null)
        {
            sliderObj = PoolManage.Instance.GetObj(ArmamentValueSliderPrefab);
        }
        else
        {
            sliderObj = Instantiate(ArmamentValueSliderPrefab);
            Debug.LogWarning("PoolManage.Instance 为空，直接实例化数值滑块！");
        }

        if (sliderObj == null) return;

        sliderObj.transform.SetParent(ArmamentValueSliderParent, false);
        // 添加到管理列表
        CreatedValueSliders.Add(sliderObj);

        // 获取滑块组件并设置数值
        GunValueSlider valueSlider = sliderObj.GetComponent<GunValueSlider>();
        if (valueSlider != null)
        {
            valueSlider.SetValue(targetValue, valueName, maxValue);
        }
        else
        {
            Debug.LogError("数值滑块预制体缺少 GunValueSlider 组件！");
        }
    }
    #endregion

    #region UI 事件处理
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
        {
            UImanager.Instance?.HidePanel<ArmamentPanel>();
        }
        else if (controlName == "EquipButton")
        {
            //根据当前的选择对数据进行更新
            if (CurrentChooseGunInfo != null && PlayerAndGameInfoManger.Instance != null)
            {
                if (CurrentChooseSlotIndex - 1 >= 0 && CurrentChooseSlotIndex - 1 < PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList.Count)
                {
                    PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentGunInfo = CurrentChooseGunInfo;// 进行数据更新
                }
                else
                {
                    Debug.LogWarning($"枪械槽位索引 {CurrentChooseSlotIndex} 超出范围！");
                }
            }
            else if (CurrentChooseTacticInfo != null && PlayerAndGameInfoManger.Instance != null)
            {
                if (CurrentChooseSlotIndex - 1 >= 0 && CurrentChooseSlotIndex - 1 < PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList.Count)
                {
                    if (CurrentChooseTacticSlotIndex == 1)
                        PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentTactic_1Info = CurrentChooseTacticInfo;// 进行数据更新
                    else if (CurrentChooseTacticSlotIndex == 2)
                        PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentTactic_2Info = CurrentChooseTacticInfo;// 进行数据更新
                }
                else
                {
                    Debug.LogWarning($"枪械槽位索引 {CurrentChooseSlotIndex} 超出范围！");
                }
            }
            else  if(CurrentChooseArmorInfoPack!=null && PlayerAndGameInfoManger.Instance != null)
            {
                if (CurrentChooseSlotIndex - 1 >= 0 && CurrentChooseSlotIndex - 1 < MilitaryManager.Instance.ArmorInfoPackList.Count)
                {
                    PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentArmorType = CurrentChooseArmorInfoPack.armorType;// 进行数据更新
                }
            }
            //进行提示
            WarnTriggerManager.Instance?.TriggerNoInteractionWarn(1, "装备成功！");
        }
    }
    #endregion

    #region 面板显隐
    bool IsPlayerPanelHide = false;
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        if(IsPlayerPanelHide)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
            IsPlayerPanelHide = false;
        }
    }
    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        if(UImanager.Instance.GetPanel<PlayerPanel>()!=null)
        {
            UImanager.Instance.GetPanel<PlayerPanel>().SimpleHidePanel();
            IsPlayerPanelHide = true;
        }
    }
    public override void SliderValueChange(string sliderName, float value) => base.SliderValueChange(sliderName, value);
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion
}

