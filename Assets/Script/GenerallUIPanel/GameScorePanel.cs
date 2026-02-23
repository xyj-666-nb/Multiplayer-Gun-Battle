using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameScorePanel : BasePanel
{
    public TextMeshProUGUI RedScoreText;
    public TextMeshProUGUI BlueScoreText;
    public TextMeshProUGUI TimeShowText;//时间显示文本
    public TextMeshProUGUI KDText;//玩家Kd显示(击杀数)

    public Image TeamSprite;//队伍标

    //更新当前的面板
    public void UpdateScoreInfo( )
    {
        RedScoreText.text = PlayerRespawnManager.Instance.RedTeamScoreCount.ToString();
        BlueScoreText.text = PlayerRespawnManager.Instance.BlueTeamScoreCount.ToString();
    }

    public void UpdateKDUI(int killCount, int deathCount)//只显示击杀数
    {
        KDText.text = killCount.ToString();
    }

    public void UpdateTime(float time)
    {
        // 1. 防止负数显示
        time = Mathf.Max(0, time);

        // 2. 计算分钟和秒
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        // 3. 格式化为 00:00 (补零)
        TimeShowText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        //获取本地玩家的队伍赋值队伍标
        UpdateScoreInfo();//开始就更新一次
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

    #region UI控件逻辑编写

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName== "ShowWarRecordButton")
        {
            //打开战绩面板
            if(UImanager.Instance.GetPanel<WarRecordPanel>()!=null)
            {
                //如果能够获取到当前的战绩面板就简单显示
                UImanager.Instance.GetPanel<WarRecordPanel>().SimpleShowPanel();
            }
            else
            {
                UImanager.Instance.ShowPanel<WarRecordPanel>();//第一次就显示
            }
        }
    }
    #endregion

    #region 面板显隐以及特殊动画

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
