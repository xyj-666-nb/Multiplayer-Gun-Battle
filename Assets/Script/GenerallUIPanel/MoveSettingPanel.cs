using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

public class MoveSettingPanel : BasePanel
{
    [Header("UI控件引用")]
    public TMP_Dropdown DropdownField_Aim;  // 0=点击，1=长按
    public Slider SliderField;           // 瞄准灵敏度滑块 (0.5 ~ 2.0)
    public TextMeshProUGUI SliderNumber; // 灵敏度数值显示
    public TMP_Dropdown DropdownField_Move;  // 1=按钮移动，0=摇杆移动

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
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
        PlayerAndGameInfoManger.Instance?.SavePlayerData();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
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

    #region 数据与UI交互
    private void SetupSlider()
    {
        if (SliderField == null) return;

        SliderField.wholeNumbers = false;
        SliderField.minValue = 0.5f;
        SliderField.maxValue = 2.0f;
    }

    private void LoadDataToUI()
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null)
        {
            Debug.LogError("未找到 PlayerAndGameInfoManger 实例！");
            return;
        }

        if (DropdownField_Aim != null)
        {
            DropdownField_Aim.value = manager.IsUseSinglePress_AimButton ? 0 : 1;
            DropdownField_Aim.RefreshShownValue();
        }

        if (SliderField != null)
        {
            float clampedValue = Mathf.Clamp(manager.AimSensitivity, 0.5f, 2.0f);
            SliderField.value = clampedValue;
            SliderNumber?.SetText(clampedValue.ToString("F2"));
        }

        if (DropdownField_Move != null)
        {

            DropdownField_Move.value = manager.IsUseJoyStickMove ? 0 : 1;
            DropdownField_Move.RefreshShownValue();
        }
    }

    private void RegisterUIEvents()
    {
        UnregisterUIEvents();

        if (DropdownField_Aim != null)
        {
            DropdownField_Aim.onValueChanged.AddListener(OnAimDropdownValueChanged);
        }

        if (SliderField != null)
        {
            SliderField.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (DropdownField_Move != null)
        {
            DropdownField_Move.onValueChanged.AddListener(OnMoveDropdownValueChanged);
        }
    }

    private void UnregisterUIEvents()
    {
        if (DropdownField_Aim != null)
        {
            DropdownField_Aim.onValueChanged.RemoveListener(OnAimDropdownValueChanged);
        }

        if (SliderField != null)
        {
            SliderField.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        if (DropdownField_Move != null)
        {
            DropdownField_Move.onValueChanged.RemoveListener(OnMoveDropdownValueChanged);
        }
    }

    // 瞄准模式回调
    private void OnAimDropdownValueChanged(int value)
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null)
            return;

        manager.IsUseSinglePress_AimButton = (value == 0);
    }

    private void OnMoveDropdownValueChanged(int value)
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null)
            return;

        manager.IsUseJoyStickMove = (value == 0);
    }

    // 灵敏度回调
    private void OnSliderValueChanged(float value)
    {
        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null) return;

        float finalValue = Mathf.Clamp(value, 0.5f, 2.0f);
        manager.AimSensitivity = finalValue;
        SliderNumber?.SetText(finalValue.ToString("F2"));
    }
    #endregion
}