using UnityEngine;

public class TapTapLoginPanel : BasePanel
{
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName== "LoginButton")
        {
            /* Debug.Log("按下登录按钮"); */
            //TapTapGameLogin.Instance.OnTapLoginClick();//暂时不使用
        }
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

   
}
