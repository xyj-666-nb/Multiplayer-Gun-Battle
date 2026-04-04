using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ExpressionOption : MonoBehaviour,IPoolObject
{
    //单个表情按钮
    public Button button;
    private ExpressionPack InfoPack;//表情1信息包
    public CanvasGroup MyCanvasGroup;
    private Sequence mySequence;
    public Image ExpressionImage;

    public void Awake()
    {
        button = GetComponent<Button>();//自动获取
        button.onClick.AddListener(TriggerExpression);
    }

    public void InitExpressionOption(ExpressionPack InfoPack)
    {
        MyCanvasGroup.alpha = 0;
        //初始化数据
        ExpressionImage.sprite = InfoPack.ExpressionSprite;//设置表情图
        this.InfoPack = InfoPack;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(MyCanvasGroup, ref mySequence, true,() => { });//显示
    }

    public void ReSetDate()
    {
        //重置数据
    }


    public void TriggerExpression()
    {
        Player.LocalPlayer.TriggerExpression(InfoPack.ExpressionID);//触发表情
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }
}
