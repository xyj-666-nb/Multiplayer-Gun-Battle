using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GlobalPictureFlipManager : SingleMonoAutoBehavior<GlobalPictureFlipManager>
{
    //全局画面翻转控制器
    public Material FlipMaterial;//翻转材质
    public bool IsFlipped=false;//是否翻转
    public RawImage MyRawImage;

    public UnityAction<bool> FlipCallBack;//翻转事件 

    protected override void Awake()
    {
        base.Awake();
    }

    //触发全局翻转
    public void TriggerGlobalFlip()
    {
        IsFlipped = !IsFlipped;
        if (IsFlipped)
            MyRawImage.material = FlipMaterial;
        else
            MyRawImage.material = null;
        FlipCallBack?.Invoke(IsFlipped);//触发事件
    }

    public void TriggerGlobalFlip(bool Flip)
    {
        //手动设置是否翻转
        IsFlipped = Flip;
        if (IsFlipped)
            MyRawImage.material = FlipMaterial;
        else
            MyRawImage.material = null;
        FlipCallBack?.Invoke(IsFlipped);//触发事件
    }
}
