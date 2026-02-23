using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class PlayerCustomUIInfo
{
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector3 localEulerAngles;
    public Vector3 localScale;
    public float Alpha;
    public NeedCustomUIType UIType;
}

public enum NeedCustomUIType
{
    AimButton,
    ShootButton,
    LefAndRightMoveButton,
    PickUpGunButton,
    JumpButton,
    ReloadButton,
    SettingButton,
    HealthAndGunButton,//健康以及枪械状态按钮
    ThrowObjButton,//战术控制按钮
}

public class PlayerCustomPanel : BasePanel
{
    private Slider Slider_ButtonScale;
    private Slider Slider_AlphaValue;
    private PlayerCustomUIInfo CurrentInfo;

    [Header("控制面板组件")]
    public TextMeshProUGUI ButtonName;

    private List<CustomUI> AllCustomUIList = new List<CustomUI>();

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        Slider_ButtonScale = controlDic["Slider_ButtonScale"] as Slider;
        Slider_AlphaValue = controlDic["Slider_AlphaValue"] as Slider;

        // 强制设置Slider范围
        Slider_ButtonScale.minValue = 0.5f;
        Slider_ButtonScale.maxValue = 1.5f;
        Slider_AlphaValue.minValue = 0f;
        Slider_AlphaValue.maxValue = 1f;

        CreateAllCustomUI();
    }

    public void CreateAllCustomUI()
    {
        foreach (var UI in PlayerAndGameInfoManger.Instance.AllCustomUIPrefabsList)
        {
            var Obj = GameObject.Instantiate(UI, this.transform);
            AllCustomUIList.Add(Obj.GetComponent<CustomUI>());
        }
        CustomUI.isEditModeEnabled = true;
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
        {
            UImanager.Instance.HidePanel<PlayerCustomPanel>();//直接关闭面板
        }
        else if (controlName == "SaveButton")
        {
            //保存数据
            SaveAllDate();
        }
    }


    public void SaveAllDate()
    {
        foreach(var UI in AllCustomUIList)
        {
            UI.UpdateInfo();//更新信息
        }
        WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "保存成功！");
    }

    public override void SliderValueChange(string sliderName, float value)
    {
        base.SliderValueChange(sliderName, value);


        if (CustomUI.currentSelectedUI == null) 
            return;

        if (sliderName == "Slider_ButtonScale")
        {
            // 修改物体缩放
            CustomUI.currentSelectedUI.RectTransform.localScale = Vector3.one * value;
            // 修改数据
            if (CurrentInfo != null)
                CurrentInfo.localScale = Vector3.one * value;
        }
        else if (sliderName == "Slider_AlphaValue")
        {
            // 修改物体透明度
            CustomUI.currentSelectedUI.CanvasGroup.alpha = value;
            // 修改数据
            if (CurrentInfo != null) 
                CurrentInfo.Alpha = value;
        }
    }
    #endregion

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        CustomUI.isEditModeEnabled = false;
        if (UImanager.Instance.GetPanel<PlayerPanel>() != null)
            UImanager.Instance.GetPanel<PlayerPanel>().UpdateAllCustomUIInfo();
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }

    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion

    public string GetTypeChineseName(NeedCustomUIType customUIType)
    {
        switch (customUIType)
        {
            case NeedCustomUIType.AimButton: return "瞄准按钮";
            case NeedCustomUIType.ShootButton: return "射击按钮";
            case NeedCustomUIType.LefAndRightMoveButton: return "左右移动按钮";
            case NeedCustomUIType.PickUpGunButton: return "捡枪按钮";
            case NeedCustomUIType.JumpButton: return "跳跃按钮";
            case NeedCustomUIType.ReloadButton: return "换弹按钮";
            case NeedCustomUIType.SettingButton: return "设置按钮";
            case NeedCustomUIType.HealthAndGunButton:return "玩家信息UI";
            case NeedCustomUIType.ThrowObjButton: return "战术控制UI";
            default: return "未知按钮";
        }
    }

    public void UpdateCurrentControlPanel(PlayerCustomUIInfo info)
    {
        if (info == null) return;

        CurrentInfo = info;
        ButtonName.text = GetTypeChineseName(CurrentInfo.UIType);

        // 更新 Scale 滑块
        float currentScale = CurrentInfo.localScale.x;
        currentScale = Mathf.Clamp(currentScale, Slider_ButtonScale.minValue, Slider_ButtonScale.maxValue);
        Slider_ButtonScale.value = currentScale;

        // 更新 Alpha 滑块
        float currentAlpha = CurrentInfo.Alpha;
        currentAlpha = Mathf.Clamp01(currentAlpha);
        Slider_AlphaValue.value = currentAlpha;
    }
}