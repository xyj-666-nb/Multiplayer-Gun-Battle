using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour//血量UI
{
    public static HealthUI Instance;

    public Image HPImage;//血量图片
    private int AnimaIndex = -1;//动画索引

    private void Awake()
    {
        Instance = this;//设置唯一 
    }

    public void SetValue(float Value)//设置数值
    {
        HPImage.DOKill();
        SimpleAnimatorTool.Instance.StopFloatLerpById(AnimaIndex);//传入一下唯一的id停止动画

        if (HPImage.fillAmount< Value)
        {
            HPImage.DOColor(ColorManager.DarkGreen,0.1f);//血量增加时变绿
        }
        AnimaIndex=SimpleAnimatorTool.Instance.StartFloatLerp(HPImage.fillAmount, Value, 0.5f, (float newValue) =>{  HPImage.fillAmount = newValue; }, () => { HPImage.DOColor(ColorManager.Red, 0.1f); });//结束变红
    }

}
