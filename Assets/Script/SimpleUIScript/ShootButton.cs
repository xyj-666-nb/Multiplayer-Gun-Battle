using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ShootButton : MonoBehaviour
{

    public Sprite DefaultIcon; // 默认图标

    public Image IconImage;
    //射击按钮的脚本（主要的服务是换根据当前的装备换图标）

    private void Awake()
    {
    }

    public void ChangeIcon(Sprite newIcon)
    {
        IconImage.DOKill();//停止上次的动画
        //进行一个小动画
        IconImage.DOFade(0, 0.3f).OnComplete(() =>
        {
            //播放音效
            IconImage.sprite = newIcon;
            IconImage.DOFade(1, 0.3f);

        });
    }

    public void ResetIcon()
    {
        //重置图标为默认状态
        IconImage.DOKill();
        IconImage.DOFade(0, 0.3f).OnComplete(() =>
        {
            //播放音效
            IconImage.sprite = DefaultIcon; // 设置为默认图标
            IconImage.DOFade(1, 0.3f);
        });
    }

}
