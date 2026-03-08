using TMPro;
using UnityEngine;

public class IntroducePanel : MonoBehaviour//介绍面板
{
    //三个提示文本(打字机动画分别播放)
    public TextMeshProUGUI Topic1;
    public TextMeshProUGUI Topic2;
    public TextMeshProUGUI Topic3;
    public CanvasGroup Topic4;
   
    //触发文本动画(Timeline调用)
    public void TriggerTextAnima()
    {
        SimpleAnimatorTool.Instance.AddTypingTask("任务调查", Topic1, 0.15f, () => {
        //开启第二个打字机
        SimpleAnimatorTool.Instance.AddTypingTask("战术行动", Topic2, 0.01f, () => {
        //开启第三个打字机
        SimpleAnimatorTool.Instance.AddTypingTask("任务地点：环非联合医学研究所", Topic3, 0.1f);

        });

        });

    }
}
