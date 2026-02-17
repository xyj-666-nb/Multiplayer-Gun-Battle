using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    public CanvasGroup TacticSlotButtonArea;//战术道具的槽位选择区域（只有切换到战术道具界面才会显示）
    public int CurrentChooseTacticSlotIndex = 1;//当前选择的战术道具槽位索引
    public List<Button> TacticSlotButtonList;
    private string ButtonGroupRegisterName_TacticSlot = "TacticSlotGroup"; //战术槽位分组

    public GunInfo CurrentChooseGunInfo;//当前选择的枪械信息
    public TacticInfo CurrentChooseTacticInfo;//当前选择的战术道具信息

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
                PoolManage.Instance.PushObj(ArmamentValueSliderPrefab, sliderObj);
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

        // 注册主分类按钮（枪械、投掷物、护甲）
        ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_ArmamentType, Gun_Button, CreateAndRegisterArmamentButton);
        ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_ArmamentType, ThrowObj_Button, CreateAndRegisterTacticButton);
        ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_ArmamentType, Armor_Button, OnArmorButtonClick);

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
        for (int i = 0; i < SlotButtonList.Count; i++)
        {
            int index = i; // 捕获当前索引
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ButtonGroupRegisterName_Slot, SlotButtonList[i], (str) =>
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
        // 校验战术槽位数量（强制限制为2个）
        if (TacticSlotButtonList.Count != 2)
        {
            Debug.LogWarning($"战术槽位按钮数量应为2个，当前为{TacticSlotButtonList.Count}个！");
        }

        for (int i = 0; i < TacticSlotButtonList.Count; i++)
        {
            int index = i; // 捕获当前索引
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ButtonGroupRegisterName_TacticSlot, TacticSlotButtonList[i], (str) =>
            {
                CurrentChooseTacticSlotIndex = ExtractNumberWithRegex(str);
                Debug.Log($"选中战术槽位：{CurrentChooseTacticSlotIndex}");
            });
        }

        // 默认选中第一个战术槽位
        if (TacticSlotButtonList.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_TacticSlot, TacticSlotButtonList[0]);
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
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_ArmamentType, Gun_Button);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁时清理所有创建的分组
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_Armament);
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_SpecialArmament);
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_ArmamentType);
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_Slot);
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_TacticSlot); // 新增：清理战术槽位分组

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
    }

    /// <summary>
    /// 创建并注册枪械类型按钮
    /// </summary>
    public void CreateAndRegisterArmamentButton()
    {
        // 核心修复：先清理旧按钮
        ClearCreatedArmamentButtons();
        // 清理具体装备按钮和数值滑块
        ClearCreatedSpecialArmamentButtons();
        ClearValueSliders();
        SetTacticSlotAreaActive(false); // 隐藏战术槽位

        GunType[] allGunTypes = (GunType[])Enum.GetValues(typeof(GunType));
        foreach (GunType gunType in allGunTypes)
        {
            var buttonObj = CreateTypeButtonPrefab();
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = MilitaryManager.Instance.GetChineseGunTypeName(gunType);

            // 注册到单选组
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_Armament, button);

            // 添加点击监听：切换特殊装备列表
            button.onClick.AddListener(() =>
            {
                Debug.Log("Selected Gun Type: " + gunType);
                CreateAndRegisterSpecialArmamentButton(gunType);
            });
        }

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_Armament, true);
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

        TacticBigType[] allTacticTypes = (TacticBigType[])Enum.GetValues(typeof(TacticBigType));
        foreach (var TacticBig in allTacticTypes)
        {
            var buttonObj = CreateTypeButtonPrefab();
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = MilitaryManager.Instance.GetChineseTacticBigTypeName(TacticBig);

            // 注册到单选组
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_Armament, button);

            // 添加点击监听：切换特殊装备列表
            button.onClick.AddListener(() =>
            {
                CreateCreateAndRegisterSpecialTacticButton(TacticBig);
            });
        }

        // 选中第一个战术大类按钮
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_Armament, true);
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
        foreach (var btn in TacticSlotButtonList)
        {
            if (btn != null)
            {
                btn.interactable = isActive;
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
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_Armament);

        foreach (var obj in CreatedArmamentButtons)
        {
            if (obj != null)
            {
                PoolManage.Instance.PushObj(TypeButtonPrefab, obj);
            }
        }
        CreatedArmamentButtons.Clear();

        // 额外清理父对象下的残留（防止对象池异常导致的残留）
        foreach (Transform child in TypeButtonParent)
        {
            if (child.gameObject != null)
            {
                PoolManage.Instance.PushObj(TypeButtonPrefab, child.gameObject);
            }
        }
    }

    public GameObject CreateTypeButtonPrefab()
    {
        GameObject buttonObj = PoolManage.Instance.GetObj(TypeButtonPrefab);
        buttonObj.transform.SetParent(TypeButtonParent, false);
        CreatedArmamentButtons.Add(buttonObj);
        return buttonObj;
    }

    public GameObject CreateSpecialArmamentButtonPrefab()
    {
        GameObject buttonObj = PoolManage.Instance.GetObj(SpecialArmamentButtonPrefab);
        buttonObj.transform.SetParent(ArmamentButtonParent, false);
        CreatedSpecialArmamentButtons.Add(buttonObj);
        return buttonObj;
    }
    #endregion

    #region 具体装备按钮管理
    /// <summary>
    /// 清理已创建的具体枪械/战术道具按钮，并从单选组中移除
    /// </summary>
    private void ClearCreatedSpecialArmamentButtons()
    {
        ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_SpecialArmament);

        foreach (var obj in CreatedSpecialArmamentButtons)
        {
            if (obj != null)
            {
                PoolManage.Instance.PushObj(SpecialArmamentButtonPrefab, obj);
            }
        }
        CreatedSpecialArmamentButtons.Clear();

        // 额外清理父对象下的残留
        foreach (Transform child in ArmamentButtonParent)
        {
            if (child.gameObject != null)
            {
                PoolManage.Instance.PushObj(SpecialArmamentButtonPrefab, child.gameObject);
            }
        }
    }

    /// <summary>
    /// 创建并注册具体枪械按钮
    /// </summary>
    public void CreateAndRegisterSpecialArmamentButton(GunType currentGunType)
    {
        ClearCreatedSpecialArmamentButtons();

        // 获取该类型下的所有枪械信息
        List<GunInfo> gunInfoList = MilitaryManager.Instance.GetGunTypeInfo(currentGunType);
        foreach (GunInfo gunInfo in gunInfoList)
        {
            if (gunInfo == null) continue; // 空值检查

            GameObject buttonObj = CreateSpecialArmamentButtonPrefab();

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = gunInfo.Name;

            // 空值检查：防止图片组件缺失
            Transform imgTrans = buttonObj.transform.GetChild(1);
            if (imgTrans != null)
            {
                Image img = imgTrans.GetComponent<Image>();
                if (img != null && gunInfo.GunSprite != null)
                {
                    img.sprite = gunInfo.GunSprite;
                }
                else
                {
                    Debug.LogWarning($"枪械 {gunInfo.Name} 缺少图标");
                }
            }

            // 注册到单选组
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_SpecialArmament, button);

            // 添加点击监听：更新界面信息
            button.onClick.AddListener(() =>
            {
                UpdateCurrentInfo(gunInfo);
                //设置当前选择的枪械信息
                CurrentChooseGunInfo = gunInfo;
                //清空当前选择的战术道具信息
                CurrentChooseTacticInfo = null;
            });
        }

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_SpecialArmament, true);
    }

    public void CreateCreateAndRegisterSpecialTacticButton(TacticBigType Type)
    {
        ClearCreatedSpecialArmamentButtons();

        List<TacticType> tacticTypesList = MilitaryManager.Instance.GetTacticTypesByBigType(Type);
        foreach (var tacticType in tacticTypesList)
        {
            GameObject buttonObj = CreateSpecialArmamentButtonPrefab();
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            // 获取战术小类型的中文名称
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
                    }
                    else
                    {
                        Debug.LogWarning($"战术道具 {tacticType} 缺少UI图标");
                    }
                }
            }

            // 注册到单选组
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ButtonGroupRegisterName_SpecialArmament, button);

            // 添加点击监听：更新界面信息
            button.onClick.AddListener(() =>
            {
                UpdateCurrentTacticInfo(tacticType);
                //设置当前选择的战术道具信息
                CurrentChooseTacticInfo = MilitaryManager.Instance.GetTacticInfo(tacticType);
                //清空当前选择的枪械信息
                CurrentChooseGunInfo = null;
            });
        }

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupRegisterName_SpecialArmament, true);
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

        GunName.text = info.Name;
        GunDescribe.text = info.description;

        // 空值检查
        if (info.GunBodySprite != null)
        {
            GunSprite.sprite = info.GunBodySprite;
        }
        else
        {
            Debug.LogWarning($"枪械 {info.Name} 缺少主图标");
            GunSprite.sprite = null;
        }

        // 枪械图片大小调整
        ResetGunSpriteTransform();
        if (info.Name == "UZI")
        {
            GunSprite.rectTransform.localScale = new Vector3(0.6f, 0.6f, 1);
        }
        else if (info.Name == "AKM")
        {
            GunSprite.rectTransform.localScale = new Vector3(2, 1, 1);
            GunSprite.rectTransform.anchoredPosition = new Vector2(70, 0);
        }
        else if (info.Name == "M110" || info.Name == "Vector-45")
        {
            GunSprite.rectTransform.localScale = new Vector3(0.9f, 1, 1);
        }
        else if (info.Name == "AUG")
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
        GunSprite.rectTransform.localScale = Vector3.one;
        GunSprite.rectTransform.anchoredPosition = Vector2.zero;
    }

    public void UpdateCurrentTacticInfo(TacticType tacticType)
    {
        // 根据战术设备的类型获取信息进行更新
        TacticInfo info = MilitaryManager.Instance.GetTacticInfo(tacticType);
        if (info == null)
        {
            Debug.LogWarning($"战术道具 {tacticType} 未找到配置信息");
            GunName.text = "未知道具";
            GunDescribe.text = "暂无描述";
            GunSprite.sprite = null;
            ClearValueSliders();
            return;
        }

        // 对界面进行更新
        GunName.text = info.Name;//名字
        GunDescribe.text = info.Description;//描述的信息

        // 空值检查
        if (info.GameBodySprite != null)
        {
            GunSprite.sprite = info.GameBodySprite;//战术道具的图片
        }
        else
        {
            Debug.LogWarning($"战术道具 {info.Name} 缺少主图标");
            GunSprite.sprite = null;
        }

        // 投掷物图片缩小
        ResetGunSpriteTransform();
        if (tacticType == TacticType.Smoke || tacticType == TacticType.Grenade)
        {
            GunSprite.rectTransform.localScale = new Vector3(0.2f, 0.2f, 1);
        }

        // 清理数值滑块
        ClearValueSliders();
    }

    /// <summary>
    /// 生成单个数值滑块并设置数值
    /// </summary>
    /// <param name="targetValue">目标数值</param>
    /// <param name="valueName">数值名称</param>
    /// <param name="maxValue">最大值</param>
    private void CreateValueSlider(float targetValue, string valueName, string maxValue)
    {
        // 从对象池获取滑块预制体
        GameObject sliderObj = PoolManage.Instance.GetObj(ArmamentValueSliderPrefab);
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
            UImanager.Instance.HidePanel<ArmamentPanel>();
        }
        else if(controlName == "EquipButton")
        {
            //根据当前的选择对数据进行更新
            if(CurrentChooseGunInfo!=null)
            {
                PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentGunInfo = CurrentChooseGunInfo;// 进行数据更新
            }
            else if(CurrentChooseTacticInfo!=null)
            {
                if(CurrentChooseTacticSlotIndex==1)
                    PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentTactic_1Info = CurrentChooseTacticInfo;// 进行数据更新
                else if(CurrentChooseTacticSlotIndex == 2)
                    PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[CurrentChooseSlotIndex - 1].CurrentTactic_2Info = CurrentChooseTacticInfo;// 进行数据更新
            }
            //进行提示
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1, "装备成功！");
        }
    }
    #endregion

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true) => base.HideMe(callback, isNeedDefaultAnimator);
    public override void ShowMe(bool isNeedDefaultAnimator = true) => base.ShowMe(isNeedDefaultAnimator);
    public override void SliderValueChange(string sliderName, float value) => base.SliderValueChange(sliderName, value);
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion
}