using UnityEngine.Events;

public class GamePausePanel : BasePanel
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
        switch (controlName)
        {
            case "ReturnGameButton":
                UImanager.Instance.HidePanel<GamePausePanel>();
                break;
            case "SettingButton":
                UImanager.Instance.ShowPanel<SettingPanel>();//打开设置面板
                break;
            case "EnterEquipPanelButton":
                UImanager.Instance.ShowPanel<EquipmentConfigurationPanel>();//打开战备选择
                break;
            case "ExitCurrentRoom":
                //在这里退出链接
                UImanager.Instance.HidePanel<GamePausePanel>();
                break;
        }

    }
    #endregion

    #region 面板显隐特殊动画制作

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    #endregion

}