using UnityEngine.Events;

public class ServerOnlinePanel : BasePanel
{

    // 简化委托：都不带参数，逻辑在外部写
    public UnityAction OnCancelAction;
    public UnityAction OnSuccessAction; // 改个简单的名字

    // 提供一个简单的隐藏方法
    public void HidePanel()
    {
        UImanager.Instance.HidePanel<ServerOnlinePanel>();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "CancelButton")
        {
            // 触发取消事件
            OnCancelAction?.Invoke();
            HidePanel();
        }
    }

    // 清理引用
    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnCancelAction = null;
        OnSuccessAction = null;
    }

    protected override void SpecialAnimator_Show()
    {
        throw new System.NotImplementedException();
    }

    protected override void SpecialAnimator_Hide()
    {
        throw new System.NotImplementedException();
    }
}