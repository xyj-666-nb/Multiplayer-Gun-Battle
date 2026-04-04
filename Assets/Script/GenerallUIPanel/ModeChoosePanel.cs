using UnityEngine.UI;

public class ModeChoosePanel : BasePanel
{
    public override void Awake()
    {
        base.Awake();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName=="LeftButton")
        {
            //调用对应的代码
            ModeChooseSystem.instance.OnClickStandalone();
            UImanager.Instance.HidePanel<ModeChoosePanel>();
        }
        else if(controlName == "RightButton")
        {
            ModeChooseSystem.instance.OnClickOnline();
            UImanager.Instance.HidePanel<ModeChoosePanel>();
        }

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        (controlDic["LeftButton"] as Button).onClick.RemoveAllListeners();
        (controlDic["RightButton"] as Button).onClick.RemoveAllListeners();

    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }


}
