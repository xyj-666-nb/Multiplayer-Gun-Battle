using Mirror;
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
        SimpleEffectButtonGroupList.Add(controlDic["SaveButton"] as Button);
        SimpleEffectButtonGroup.Instance.RegisterGroup("GamePausePanel", SimpleEffectButtonGroupList,false,1,0.9f);
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
            case "SaveButton":
                PlayerAndGameInfoManger.Instance.SavePlayerData();//保存一下玩家数据
                WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f,"保存成功");
                break;
            case "SettingButton":
                UImanager.Instance.ShowPanel<SettingPanel>();//打开设置面板
                break;
            case "EnterEquipPanelButton":
                UImanager.Instance.ShowPanel<EquipmentConfigurationPanel>();//打开战备选择
                break;
            case "ExitCurrentRoom":
                //先弹出提示
                string warnTopic = NetworkManager.singleton.mode == NetworkManagerMode.Host ? "是否关闭房间" : "是否退出房间";
                string warnText = NetworkManager.singleton.mode == NetworkManagerMode.Host ? "退出后会踢出所有玩家" : "退出后将返回大厅";
                WarnTriggerManager.Instance.TriggerDoubleInteractionWarn(warnTopic, warnText,()=> { }, () =>
                {
                    //在这里退出链接
                    //打开场景
                    AllMapManager.Instance.TriggerMap(MapType.StartCG, true);
                    UImanager.Instance.HidePanel<GamePausePanel>();
                    PlayerRespawnManager.Instance.CleanupAndExitGame();//退出链接
                    if (Main.Instance.IsInSingleMode)
                    {
                        UImanager.Instance.ShowPanel<GameStartPanel>();
                        Main.Instance.IsInSingleMode = false;
                    }
                    else
                        UImanager.Instance.ShowPanel<RoomPanel>();

                    //返回视角系统
                    ModeChooseSystem.instance.EnterSystem_Quick();//快速回到主界面
                });
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
        //销毁注册
        SimpleEffectButtonGroup.Instance.UnRegisterGroup("GamePausePanel");
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