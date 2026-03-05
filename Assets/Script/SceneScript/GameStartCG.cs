using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GameStartCG : MonoBehaviour
{
    public static GameStartCG Instance;
    [Header("TimeLine")]
     public PlayableDirector StartTimeLine;
    //游戏开始动画控制
    public float FirstStartTime;//(第一段)循环开始时间
    public bool IsPlayAnima1=false;
    public bool IsPlayAnima2 = false;

    [Header("第一个随机动作：更换弹匣")]
    public float AnimaStartTime_1;//动画开始时间
    [Header("第二个随机动作：举枪瞄准")]
    public float AnimaStartTime_2;//动画开始时间
    private int AnimaId=-1;

    [Header("跳过功能提示面板")]
    public float SkipTime;//跳跃动画的时间节点
    public Button IsSkipButton;//是否跳跃
    public CanvasGroup IsSkipPanelCanvasGroup;
    private Sequence IsSkipPanelSequence;

    private void Awake()
    {
        //先注册一下提示消息
        SimpleAnimatorTool.Instance.AddFadeLoopTask(IsSkipButton.GetComponentInChildren<TextMeshProUGUI>());
        IsSkipButton.onClick.AddListener(() =>
        {
            //点击跳跃并关闭当前面板
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IsSkipPanelCanvasGroup, ref IsSkipPanelSequence, false, () => {

                IsSkipPanelCanvasGroup.interactable = false;//无法交互
                IsSkipPanelCanvasGroup.blocksRaycasts = false;
                //跳过动画
                StartTimeLine.time = SkipTime;
            });

        });
        Instance =this;
        CountDownManager.Instance.CreateTimer(false, (int)(FirstStartTime * 1000), () => {
            Debug.Log("开始随机动画");
            AnimaId = CountDownManager.Instance.CreateTimer_Permanent(false, 6000, () =>
            {

                if (Random.Range(0, 1f) > 0.6f)
                    PlayAnima1();
                else
                    PlayAnima2();
            });
        
        });
    }

    public void StopTimeLine()
    {
        StartTimeLine.Stop();
    }

    public void LoopReturn()//循环返回函数
    {
        if (IsPlayAnima1 || IsPlayAnima2)
            return;
        StartTimeLine.time = FirstStartTime;//回到预设时间
    }

    //第一个动画结束回到待机状态
    public void AnimaEnd()
    {
        IsPlayAnima1 = false;
        LoopReturn();
    }

    public void PlayAnima1()
    {
        IsPlayAnima1 = true;
        IsPlayAnima2 = false;
        StartTimeLine.time = AnimaStartTime_1;//直接进入第一个动画的时间
    }

    public void PlayAnima2()
    {
        IsPlayAnima2 = true;
        IsPlayAnima1 = false;
        StartTimeLine.time = AnimaStartTime_2;//直接进入第二个动画的时间
    }

    public void Anima2Check()
    {
        if(!IsPlayAnima2)
        {
            StartTimeLine.time = FirstStartTime;//回到预设时间
            return;
        }
        IsPlayAnima1 = false;
    }

    private void OnDestroy()
    {
        if(AnimaId!=-1)
        {
            CountDownManager.Instance.StopTimer(AnimaId);
        }
    }

    public void UIHide()
    {
        //点击跳跃并关闭当前面板
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IsSkipPanelCanvasGroup, ref IsSkipPanelSequence, false, () => {

            IsSkipPanelCanvasGroup.interactable = false;//无法交互
            IsSkipPanelCanvasGroup.blocksRaycasts = false;
            //跳过动画
        });
    }

}
