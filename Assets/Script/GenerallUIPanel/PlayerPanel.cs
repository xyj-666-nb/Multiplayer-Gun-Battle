using DG.Tweening;
using System.Collections.Generic;
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
    public  BaseGun CurrentGun => Player.LocalPlayer.currentGun;
    [HideInInspector]
    public static string AimButtonButtonGroupName = "AimButton";

    [Header("Buff预制体")]
    public GameObject BuffPrefabs;
    public Transform BuffUIParent;//BuffUI父物体
    public List<GameObject> BuffObjList = new List<GameObject>();//BuffObj列表，方便后续管理

    [Header("射击按钮")]
    public ShootButton shootButton;//射击按钮脚本

    public static string GetAimButtonButtonGroupName()
    {
        return AimButtonButtonGroupName;
    }

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

        //注册一下射击按钮单选按钮
        ButtonGroupManager.Instance.AddToggleButtonToGroup(AimButtonButtonGroupName, controlDic[AimButtonButtonGroupName] as Button);//不需要事件。事件由输入系统管理器触发

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
        ClearAllBuff();//销毁时清理一下BuffUI
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

    #region Buff显示
    public void CreateBuff(Sprite BuffSprite,float Duration )
    {
       GameObject Pbj = Instantiate(BuffPrefabs, BuffUIParent);//生成BuffUI预制体
       Pbj.transform.localPosition = Vector3.zero;//重置位置
       BuffUI BuffUI = Pbj.GetComponent<BuffUI>();
       BuffUI.SetBuff(BuffSprite, Duration);
       BuffObjList.Add(Pbj);//添加到列表中，方便后续管理
    }

    public void ClearAllBuff()
    {
        for (int i = BuffObjList.Count-1; i >= 0; i--)
        {
            var obj = BuffObjList[i];
            BuffObjList.Remove(obj);//从列表中移除
            Destroy(obj);//销毁BuffUI对象
        }
    }

    #endregion

  

}
