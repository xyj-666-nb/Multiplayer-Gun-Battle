using UnityEngine;

public class TapTapLoginPanel : BasePanel
{
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName== "LoginButton")
        {
            /* Debug.Log("°´ÏÂµÇÂ¼°´Å¥"); */
            if (TapTapGameLogin.Instance != null)
            {
                TapTapGameLogin.Instance.OnTapLoginClick();
            }
        }
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }

   
}
