using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class TeamStartAnimaPanel : BasePanel
{
    public PlayableDirector TimeLine;

    [Header("UI关联")]
    public TextMeshProUGUI TeamText;
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI TimeText;

    #region 生命周期

    public override void Awake()
    {
        base.Awake();
        Init();

        TimeLine.time = 0;
        TimeLine.Play();
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
    
    public void StopTimeLine()
    {
        UImanager.Instance.HidePanel<TeamStartAnimaPanel>();
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
    }

    public void Init()
    {
        TeamText.text = Player.LocalPlayer.CurrentTeam==Team.Red? "红队":"蓝队";
        ScoreText.text = "目标分数：" +PlayerRespawnManager.Instance.GoalScoreCount.ToString()+"分";
        TimeText.text = "任务时间：" + PlayerRespawnManager.Instance.GameTime.ToString() + "分钟";
    }

    #region UI控件逻辑

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }


    #endregion

    #region UI控件逻辑

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
