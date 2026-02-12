using System;
using System.Collections.Generic;
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
    private string ButtonGroupRegisterName_Armament = "ArmamentTypeGroup"; // 枪械类型分组
    private List<GameObject> CreatedArmamentButtons = new List<GameObject>();

    [Header("具体的装备按钮父对象以及预制体")]
    public Transform ArmamentButtonParent;
    public GameObject SpecialArmamentButtonPrefab;
    private string ButtonGroupRegisterName_SpecialArmament = "SpecialArmamentGroup"; // 具体枪械分组
    private List<GameObject> CreatedSpecialArmamentButtons = new List<GameObject>();

    [Header("军备种类的按钮（枪械/投掷物/护甲）")]
    public Button Gun_Button;
    public Button ThrowObj_Button;
    public Button Armor_Button;
    private string ButtonGroupRegisterName_ArmamentType = "ArmamentMainTypeGroup"; // 主分类分组

    [Header("装备数值显示条")]
    public Transform ArmamentValueSliderParent;
    public GameObject ArmamentValueSliderPrefab;

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
    private List<GameObject> CreatedValueSliders = new List<GameObject>(); // 管理生成的数值滑块

    #region 生命周期

    public override void Awake()
    {
        base.Awake();

        // 注册主分类按钮（枪械、投掷物、护甲）
        RadioGroupManager.Instance.AddButtonToGroup(ButtonGroupRegisterName_ArmamentType, Gun_Button, CreateAndRegisterArmamentButton);
        RadioGroupManager.Instance.AddButtonToGroup(ButtonGroupRegisterName_ArmamentType, ThrowObj_Button);
        RadioGroupManager.Instance.AddButtonToGroup(ButtonGroupRegisterName_ArmamentType, Armor_Button);
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁时清理所有创建的分组
        RadioGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_Armament);
        RadioGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_SpecialArmament);
        RadioGroupManager.Instance.DestroyRadioGroup(ButtonGroupRegisterName_ArmamentType);
    }

    #endregion

    #region 枪械类型按钮管理

    /// <summary>
    /// 清理已创建的枪械类型按钮，并从单选组中移除
    /// </summary>
    private void ClearCreatedArmamentButtons()
    {
        foreach (var obj in CreatedArmamentButtons)
        {
            var btn = obj.GetComponent<Button>();
            RadioGroupManager.Instance.RemoveButtonFromGroup(ButtonGroupRegisterName_Armament, btn);
            PoolManage.Instance.PushObj(TypeButtonPrefab, obj);
        }
        CreatedArmamentButtons.Clear();
    }

    /// <summary>
    /// 创建并注册枪械类型按钮
    /// </summary>
    public void CreateAndRegisterArmamentButton()
    {

        ClearCreatedArmamentButtons();

        GunType[] allGunTypes = (GunType[])Enum.GetValues(typeof(GunType));
        foreach (GunType gunType in allGunTypes)
        {
            GameObject buttonObj = PoolManage.Instance.GetObj(TypeButtonPrefab);
            buttonObj.transform.SetParent(TypeButtonParent, false);
            CreatedArmamentButtons.Add(buttonObj);

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = GunManager.Instance.getChineseGunTypeName(gunType);

            // 注册到单选组（无默认回调，因为自动选中时会触发 onClick 模拟）
            RadioGroupManager.Instance.AddButtonToGroup(ButtonGroupRegisterName_Armament, button);

            // 添加点击监听：切换特殊装备列表
            button.onClick.AddListener(() =>
            {
                Debug.Log("Selected Gun Type: " + gunType);
                CreateAndRegisterSpecialArmamentButton(gunType);
            });
        }

        RadioGroupManager.Instance.SelectFirstButtonInGroup(ButtonGroupRegisterName_Armament, true);
    }

    #endregion

    #region 具体枪械按钮管理

    /// <summary>
    /// 清理已创建的具体枪械按钮，并从单选组中移除
    /// </summary>
    private void ClearCreatedSpecialArmamentButtons()
    {
        foreach (var obj in CreatedSpecialArmamentButtons)
        {
            var btn = obj.GetComponent<Button>();
            RadioGroupManager.Instance.RemoveButtonFromGroup(ButtonGroupRegisterName_SpecialArmament, btn);
            PoolManage.Instance.PushObj(SpecialArmamentButtonPrefab, obj);
        }
        CreatedSpecialArmamentButtons.Clear();
    }

    /// <summary>
    /// 创建并注册具体枪械按钮
    /// </summary>
    public void CreateAndRegisterSpecialArmamentButton(GunType currentGunType)
    {
        ClearCreatedSpecialArmamentButtons();

        // 获取该类型下的所有枪械信息
        List<GunInfo> gunInfoList = GunManager.Instance.getGunTypeInfo(currentGunType);
        foreach (GunInfo gunInfo in gunInfoList)
        {
            GameObject buttonObj = PoolManage.Instance.GetObj(SpecialArmamentButtonPrefab);
            buttonObj.transform.SetParent(ArmamentButtonParent, false);
            CreatedSpecialArmamentButtons.Add(buttonObj);

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = gunInfo.Name;
            buttonObj.transform.GetChild(1).GetComponent<Image>().sprite = gunInfo.GunSprite;

            // 注册到单选组
            RadioGroupManager.Instance.AddButtonToGroup(ButtonGroupRegisterName_SpecialArmament, button);

            // 添加点击监听：更新界面信息
            button.onClick.AddListener(() =>
            {
                UpdateCurrentInfo(gunInfo);
            });
        }

        RadioGroupManager.Instance.SelectFirstButtonInGroup(ButtonGroupRegisterName_SpecialArmament, true);
    }

    #endregion

    #region 界面信息更新

    public void UpdateCurrentInfo(GunInfo info)
    {
        GunName.text = info.Name;
        GunDescribe.text = info.description;
        GunSprite.sprite = info.GunBodySprite;

        //因为枪械的图片没有设置好这里需要代码调整
        if (info.Name == "UZI")
        {
            //设置一下图片的大小
            GunSprite.rectTransform.localScale = new Vector3(0.6f, 0.6f, 1);
            GunSprite.rectTransform.anchoredPosition = new Vector2(0, 0);
        }
        else if (info.Name == "AKM")
        {
            GunSprite.rectTransform.localScale = new Vector3(2, 1, 1);
            //往后移动一点
            GunSprite.rectTransform.anchoredPosition = new Vector2(70, 0);
        }
        else if (info.Name == "M110" || info.Name == "Vector-45")
        {
            GunSprite.rectTransform.localScale = new Vector3(0.9f, 1, 1);
            GunSprite.rectTransform.anchoredPosition = new Vector2(0, 0);
        }
        else
        {
            GunSprite.rectTransform.localScale = Vector3.one;
            GunSprite.rectTransform.anchoredPosition = new Vector2(0, 0);
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
    }

    #endregion

    #region 面板显隐（保留原样）

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true) => base.HideMe(callback, isNeedDefaultAnimator);
    public override void ShowMe(bool isNeedDefaultAnimator = true) => base.ShowMe(isNeedDefaultAnimator);
    public override void SliderValueChange(string sliderName, float value) => base.SliderValueChange(sliderName, value);
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }

    #endregion
}