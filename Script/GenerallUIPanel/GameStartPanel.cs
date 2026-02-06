
using UnityEngine;
using UnityEngine.Events;

public class GameStartPanel : BasePanel
{

    #region  生命周期
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    #region 控件处理

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        switch (controlName)
        {
            case "GameStartButton":
                //进入游戏逻辑
                UImanager.Instance.HidePanel<GameStartPanel>();
                break;
            case "GameExitButton":
                //弹出警告
                WarnTriggerManager.Instance.TriggerDoubleInteraction2Warn("是否确认退出游戏？", null, ()=> { Application.Quit(); } );
                break;

        }
    }
    #endregion

    #region 面板显隐

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }
    #endregion

    #region 特殊动画实现
    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

    protected override void Update()
    {
        base.Update();
    }
    #endregion
}
