using UnityEngine.Events;
using UnityEngine.UI;

public class MusicPanel : BasePanel
{
    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        (controlDic["Slider_Music"] as Slider).value = MusicManager.Instance.GetGlobalVolume();
        (controlDic["Slider_MusicEffect"] as Slider).value = MusicManager.Instance.GetEffectGlobalVolume();
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

    #region 按钮点击事件处理
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }
    public override void SliderValueChange(string sliderName, float value)
    {
        base.SliderValueChange(sliderName, value);
        switch (sliderName)
        {
            case "Slider_Music":
                if (!IsDisableMusicControl)
                    MusicManager.Instance.SetBgmGlobalVolume(value);
                break;
            case "Slider_MusicEffect":
                if (!IsDisableMusicEffectControl)
                    MusicManager.Instance.SetEffectGlobalVolume(value);
                break;
        }
    }

    private bool IsDisableMusicControl = false;
    private bool IsDisableMusicEffectControl = false;

    public override void ToggleValueChange(string toggleName, bool value)
    {
        base.ToggleValueChange(toggleName, value);
        switch (toggleName)
        {
            case "Toggle_Music":
                IsDisableMusicControl = !value;
                if (IsDisableMusicControl)
                    MusicManager.Instance.SetBgmGlobalVolume(0);

                break;
            case "Toggle_MusicEffect":
                IsDisableMusicEffectControl = !value;
                if (IsDisableMusicEffectControl)
                    MusicManager.Instance.SetEffectGlobalVolume(0);

                break;
        }
    }


    #endregion


    #region 面板的显隐

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    #endregion

    #region 面板特殊动画
    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    #endregion

}
