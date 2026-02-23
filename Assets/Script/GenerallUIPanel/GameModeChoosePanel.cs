using UnityEngine;
using UnityEngine.Events;

public class GameModeChoosePanel : BasePanel
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

    #region UI控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
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
