using UnityEngine;
using UnityEngine.UI;

public class BuffUI : MonoBehaviour
{
    public Image BuffSpite;//Buff图标
    public Image FillPromptImage;//填充提示图标
    public float Duration;//持续时间
    private int FloatLerpTaskID;//插值任务ID

    public void SetBuff(Sprite sprite, float duration)
    {
        BuffSpite.sprite = sprite;
        Duration = duration;

        SimpleAnimatorTool.Instance.StartFloatLerp(0, 1, Duration, (Value) => { FillPromptImage.fillAmount = Value; }, () => { Destroy(this.gameObject); });//开始插值动画
    }

    private void OnDestroy()
    {
        SimpleAnimatorTool.Instance.StopFloatLerpById(FloatLerpTaskID);//销毁时停止插值动画，避免内存泄漏
    }

}
