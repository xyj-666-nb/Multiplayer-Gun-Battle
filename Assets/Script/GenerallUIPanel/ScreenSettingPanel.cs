using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class ScreenSettingPanel : BasePanel
{
    [Header("选中效果配置")]
    public Color SelectedColor = Color.green; // 选中时的颜色
    public Color NormalColor = Color.white; // 未选中时的颜色
    public float SelectedScale = 1.05f; // 选中时的放大比例
    public float AnimationDuration = 0.2f; // DOTween 动画时长

    #region 生命周期 
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();
        RefreshAllButtonStates();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 清理 DOTween 动画，防止内存泄漏
        KillAllButtonTweens();
    }
    #endregion

    #region UI控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            // --- 帧率设置 ---
            case "Button_FPS60":
                SetFpsType(FpsType.Standard);
                break;
            case "Button_FPS90":
                SetFpsType(FpsType.High);
                break;
            case "Button_FPS120":
                SetFpsType(FpsType.Ultra);
                break;

            // --- 画质设置 ---
            case "Button_ScreenLow":
                SetScreenType(ScreenType.Standard);
                break;
            case "Button_ScreenMiddle":
                SetScreenType(ScreenType.High);
                break;
            case "Button_ScreenHight":
                SetScreenType(ScreenType.Ultra);
                break;
        }
    }
    #endregion

    #region 核心逻辑：设置并记录数据
    private void SetFpsType(FpsType fpsType)
    {
        PlayerAndGameInfoManger.Instance.CurrentFPS = fpsType;
        Debug.Log($"已记录帧率: {fpsType}");

        RefreshFpsButtons(fpsType);
    }

    private void SetScreenType(ScreenType screenType)
    {
        PlayerAndGameInfoManger.Instance.CurrentScreen = screenType;
        Debug.Log($"已记录画质: {screenType}");

        RefreshScreenButtons(screenType);
    }
    #endregion

    #region DOTween 选中效果逻辑
    // 初始化时刷新所有按钮
    private void RefreshAllButtonStates()
    {
        if (PlayerAndGameInfoManger.Instance == null)
            return;

        RefreshFpsButtons(PlayerAndGameInfoManger.Instance.CurrentFPS);
        RefreshScreenButtons(PlayerAndGameInfoManger.Instance.CurrentScreen);
    }

    // 刷新帧率按钮组
    private void RefreshFpsButtons(FpsType currentType)
    {
        AnimateButton(GetButtonFromDic("Button_FPS60"), currentType == FpsType.Standard);
        AnimateButton(GetButtonFromDic("Button_FPS90"), currentType == FpsType.High);
        AnimateButton(GetButtonFromDic("Button_FPS120"), currentType == FpsType.Ultra);
    }

    // 刷新画质按钮组
    private void RefreshScreenButtons(ScreenType currentType)
    {
        AnimateButton(GetButtonFromDic("Button_ScreenLow"), currentType == ScreenType.Standard);
        AnimateButton(GetButtonFromDic("Button_ScreenMiddle"), currentType == ScreenType.High);
        AnimateButton(GetButtonFromDic("Button_ScreenHight"), currentType == ScreenType.Ultra);
    }

    // 统一从字典获取按钮的封装方法
    private Button GetButtonFromDic(string key)
    {
        if (controlDic == null || !controlDic.ContainsKey(key))
        {
            Debug.LogWarning($"未在 controlDic 中找到按钮: {key}");
            return null;
        }
        return controlDic[key] as Button;
    }

    // 通用：单个按钮的 DOTween 动画
    private void AnimateButton(Button btn, bool isSelected)
    {
        if (btn == null) return;

        btn.transform.DOKill();
        btn.image.DOKill();

        if (isSelected)
        {
            btn.transform.DOScale(SelectedScale, AnimationDuration).SetEase(Ease.OutBack);
            btn.image.DOColor(SelectedColor, AnimationDuration);
        }
        else
        {
            btn.transform.DOScale(1f, AnimationDuration).SetEase(Ease.OutQuad);
            btn.image.DOColor(NormalColor, AnimationDuration);
        }
    }

    // 清理所有动画
    private void KillAllButtonTweens()
    {
        KillButtonTween("Button_FPS60");
        KillButtonTween("Button_FPS90");
        KillButtonTween("Button_FPS120");

        KillButtonTween("Button_ScreenLow");
        KillButtonTween("Button_ScreenMiddle");
        KillButtonTween("Button_ScreenHight");
    }

    // 清理单个按钮动画的封装方法
    private void KillButtonTween(string key)
    {
        Button btn = GetButtonFromDic(key);
        if (btn != null)
        {
            btn.transform.DOKill();
            btn.image.DOKill();
        }
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
        RefreshAllButtonStates();
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
}

public enum FpsType
{
    Standard,
    High,
    Ultra
}

public enum ScreenType
{
    Standard,
    High,
    Ultra
}