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
        SimpleAnimatorTool.Instance.AddTypingTask("军事演习", Topic1, 0.4f, () => {
        //开启第二个打字机
        SimpleAnimatorTool.Instance.AddTypingTask("战术行动", Topic2, 0.3f, () => {
        //开启第三个打字机
        SimpleAnimatorTool.Instance.AddTypingTask("作战地图：XXX", Topic3, 0.2f);

        });

        });

    }
}
