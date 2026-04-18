using UnityEngine.UI;

public class StartPanel : BasePanel
{
    public override void Awake()
    {
        base.Awake();
        SimpleAnimatorTool.Instance.AddFadeLoopTask((controlDic["StartButton"] as Button).GetComponent<Image>());//按钮闪烁动画
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName== "StartButton")
        {
           MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            //进入游戏
            UImanager.Instance.HidePanel<StartPanel>();
            Main.Instance.StartCG();
        }
    }

    protected override void SpecialAnimator_Hide()
    {
       
    }

    protected override void SpecialAnimator_Show()
    {
        
    }
}
