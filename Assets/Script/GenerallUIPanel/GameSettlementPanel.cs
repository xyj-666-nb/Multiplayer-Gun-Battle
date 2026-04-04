using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GameSettlementPanel : BasePanel
{
    public Image RedImage;
    public Image BlueImage;
    public TextMeshProUGUI WinText;//胜利宣判文本
    public TextMeshProUGUI RedScore;
    public TextMeshProUGUI BlueScore;
    public PlayableDirector TimeLine;//时间线
    //时间线触发

    public Team WinTeam;
    public void TimeLineTrigger()
    {
        RectTransform Rect;
        //获取胜利的一方
        if(WinTeam == Team.Red)
        {
            Rect = RedImage.GetComponent<RectTransform>();
        }
        else
        {
            Rect = BlueImage.GetComponent<RectTransform>();
        }

        Rect
       .DOScale(Vector3.one * 1.2f, 1f)
       .SetEase(Ease.OutQuad)
       .SetLink(BlueImage.gameObject);
        ModeChooseSystem.instance.EnterSystem();//进入系统
    }

    public void TimeLineEnd()
    {
        TimeLine.Pause();//暂停一下
        //然后打开战绩面板
        UImanager.Instance.ShowPanel<WarRecordPanel>(); //打开战绩面板
        controlDic["ExitButton"].gameObject.SetActive(true);//打开退出面板
    }

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        if (PlayerRespawnManager.Instance != null)
        {
            //获取胜利的一方
            if (WinTeam == Team.Red)
                WinText.text = "红方胜利";
            else
                WinText.text = "蓝方胜利";

            //获取一下当前的比分
            RedScore.text = PlayerRespawnManager.Instance.RedTeamScoreCount.ToString();
            BlueScore.text = PlayerRespawnManager.Instance.BlueTeamScoreCount.ToString();
        }
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

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ExitButton")
        {
            Debug.Log("房间退出");

            AllMapManager.Instance?.TriggerMap(MapType.StartCG, true);
            ModeChooseSystem.instance?.EnterSystem_Quick();

            UImanager.Instance?.HidePanel<GameSettlementPanel>();
            UImanager.Instance.ShowPanel<GameStartPanel>();

            if (PlayerRespawnManager.Instance != null)
            {
                PlayerRespawnManager.Instance.CleanupAndExitGame();
            }
            else
            {
                Debug.LogWarning("PlayerRespawnManager 已不存在，执行强制UI清理");
            }
        }
    }
    #endregion

    #region 面板显隐以及特殊动画

    //面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    //简单视觉动画
    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }

    //特殊动画
    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }
    #endregion


}
