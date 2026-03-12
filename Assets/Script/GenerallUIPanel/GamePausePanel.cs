using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

public class GamePausePanel : BasePanel
{
    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        //进行按钮的注册
        List<Button> SimpleEffectButtonGroupList = new List<Button>();
        SimpleEffectButtonGroupList.Add(controlDic["ReturnGameButton"] as Button);
        SimpleEffectButtonGroupList.Add(controlDic["SettingButton"] as Button);
        SimpleEffectButtonGroupList.Add(controlDic["EnterEquipPanelButton"] as Button);
        SimpleEffectButtonGroupList.Add(controlDic["ExitCurrentRoom"] as Button);
        SimpleEffectButtonGroupList.Add(controlDic["OperationSettingButton"] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("GamePausePanel", SimpleEffectButtonGroupList);
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
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("GamePausePanel");
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
                PlayerRespawnManager.Instance.CleanupAndExitGame();//退出链接
                UImanager.Instance.ShowPanel<RoomPanel>();
                //返回视角系统
                ModeChooseSystem.instance.EnterSystem();
                break;
            case "OperationSettingButton":
                //打开自定义UI面板
                UImanager.Instance.ShowPanel<PlayerCustomPanel>();
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