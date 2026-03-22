using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

public class MoveSettingPanel : BasePanel
{
    [Header("UI控件引用")]
    public TMP_Dropdown DropdownField;  // 0=点击，1=长按
    public Slider SliderField;           // 瞄准灵敏度滑块 (0.5 ~ 2.0)
    public TextMeshProUGUI SliderNumber; // 灵敏度数值显示

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        // 初始化时强制配置Slider，彻底解决只能选0/1的问题
        SetupSlider();
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
        // 销毁时强制移除事件+保存一次，防止数据丢失
        UnregisterUIEvents();
        PlayerAndGameInfoManger.Instance?.SavePlayerData();
    }
    #endregion

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }
    #endregion

    #region UI面板的显隐以及动画
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        UnregisterUIEvents();
        // 隐藏面板时保存数据，确保修改生效
        PlayerAndGameInfoManger.Instance?.SavePlayerData();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        // 每次打开面板重新校验Slider配置+刷新UI
        SetupSlider();
        LoadDataToUI();
        RegisterUIEvents();
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }
    #endregion

    #region 核心逻辑：数据与UI交互
    /// <summary>
    /// 强制配置Slider属性，彻底解决整数限制问题
    /// </summary>
    private void SetupSlider()
    {
        if (SliderField == null) return;

        // 【关键】强制关闭整数模式，解决只能选0/1的问题
        SliderField.wholeNumbers = false;
        // 强制锁定范围
        SliderField.minValue = 0.5f;
        SliderField.maxValue = 2.0f;
    }

    /// <summary>
    /// 从数据管理器读取数据并初始化UI显示
    /// </summary>
    private void LoadDataToUI()
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null)
        {
            Debug.LogError("未找到 PlayerAndGameInfoManger 实例！");
            return;
        }

        if (DropdownField != null)
        {
            DropdownField.value = manager.IsUseSinglePress_AimButton ? 0 : 1;
            // 强制刷新Dropdown显示，防止UI不更新
            DropdownField.RefreshShownValue();
        }

        // 初始化 Slider + 数值显示
        if (SliderField != null)
        {
            // 钳制数值，防止超出范围
            float clampedValue = Mathf.Clamp(manager.AimSensitivity, 0.5f, 2.0f);
            SliderField.value = clampedValue;
            SliderNumber?.SetText(clampedValue.ToString("F2"));
        }
    }

    /// <summary>
    /// 注册 UI 事件监听
    /// </summary>
    private void RegisterUIEvents()
    {
        // 先移除再添加，防止重复注册
        UnregisterUIEvents();

        if (DropdownField != null)
        {
            DropdownField.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        if (SliderField != null)
        {
            SliderField.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    /// <summary>
    /// 移除 UI 事件监听
    /// </summary>
    private void UnregisterUIEvents()
    {
        if (DropdownField != null)
        {
            DropdownField.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        if (SliderField != null)
        {
            SliderField.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    /// <summary>
    /// Dropdown 变化回调：保存瞄准模式设置
    /// </summary>
    private void OnDropdownValueChanged(int value)
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null) 
            return;

        if(value==0)
            manager.IsUseSinglePress_AimButton = true;
        else
            manager.IsUseSinglePress_AimButton = false;
    }

    /// <summary>
    /// Slider 变化回调：保存灵敏度设置
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null) return;

        // 钳制数值，防止异常
        float finalValue = Mathf.Clamp(value, 0.5f, 2.0f);
        manager.AimSensitivity = finalValue;
        // 更新数值显示，格式化2位小数
        SliderNumber?.SetText(finalValue.ToString("F2"));
    }
    #endregion
}