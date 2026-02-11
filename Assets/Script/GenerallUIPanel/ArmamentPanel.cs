using UnityEngine.Events;

public class ArmamentPanel : BasePanel
{

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
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

    #region UI逻辑处理

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }

    #endregion

    #region 面板显隐以及特殊动画实现

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void SliderValueChange(string sliderName, float value)
    {
        base.SliderValueChange(sliderName, value);
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {

    }

    #endregion
}
