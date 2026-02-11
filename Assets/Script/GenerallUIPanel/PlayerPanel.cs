using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerPanel : BasePanel
{
    public Image GunImage;
    public TextMeshProUGUI BulletAmount;
    public TextMeshProUGUI GunName;
    public CanvasGroup GunBackGround;//枪械背景
    private Sequence GunBackGroundSequence;
    public BaseGun CurrentGun => Player.LocalPlayer.currentGun;

    public void ShowGunBackGround()//捡起枪就更新一下
    {
        UpdateGunInfo();//更新一下
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GunBackGround,ref GunBackGroundSequence, true, () => { });
    }

    public void HideGunBackGround()//丢弃当前枪就放弃
    {
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GunBackGround, ref GunBackGroundSequence, false, () => { });
    }

    public void UpdateGunInfo()
    {
        if (CurrentGun == null)
            return;

        GunImage.sprite = CurrentGun.gunInfo.GunSprite;//赋值枪械UI图标
        GunName.text = CurrentGun.gunInfo.Name;
        UpdateGunBulletAmountText();//更新一下子弹数
    }

    public void UpdateGunBulletAmountText()
    {
        if (CurrentGun == null)
            return;
        BulletAmount.text = CurrentGun.CurrentMagazineBulletCount.ToString() + "/" + CurrentGun.AllReserveBulletCount.ToString();//显示当前的子弹数
    }


    #region 生命周期
    public override void Awake()
    {
        base.Awake();
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

    #region 控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }
    #endregion

    #region 面板显隐以及特殊动画设置

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
