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
    public static string AimButtonButtonGroupName = "Aim_Button";

    [Header("Buff预制体")]
    public GameObject BuffPrefabs;
    public Transform BuffUIParent;//BuffUI父物体
    public List<GameObject> BuffObjList = new List<GameObject>();//BuffObj列表，方便后续管理

    [Header("射击按钮")]
    public ShootButton shootButton;//射击按钮脚本

    [Header("交互按钮")]
    public CanvasGroup InteractButton;
    private Sequence InteractBackGroundSequence;
    public Image InteractButtonFillButtonImage;//射击按钮填充图片

    [Header("换弹按钮提示图片")]
    public Image ReloadProcessImage;
    private int TimerID=-1;//换弹计时器ID
    public bool IsInReloadProcess = false;//是否在换弹过程中

    [Header("对捡起枪械按钮的管理")]
    public CanvasGroup PickupCanvasGroup;//对捡起枪械默认进行隐藏
    private Sequence PickupCanvasGroupSequence;

    [Header("移动控制")]
    public GameObject Joystick;//摇杆控制
    public GameObject MoveButton;//移动按钮

    public void UpdateMoveButton()
    {
        if(PlayerAndGameInfoManger.Instance.IsUseJoyStickMove)
        {
            Joystick.SetActive(true);
            MoveButton.SetActive(false);
        }
        else
        {
            Joystick.SetActive(false);
            MoveButton.SetActive(true);
        }
    }

    //键入换弹提示
    public void EnterReloadPrompt(float Time)//换弹的时候就触发
    {
        if(TimerID!=-1)
            SimpleAnimatorTool.Instance.StopFloatLerpById(TimerID);//如果之前有计时器了就先停掉，避免重复叠加
        IsInReloadProcess = true;
        TimerID = SimpleAnimatorTool.Instance.StartFloatLerp(0, 1, Time, (v) => {
            ReloadProcessImage.fillAmount = v;//持续更新换弹提示的填充量
        }, () => { IsInReloadProcess = false; });
    }

    public void IsTriggerPickUpGunButton(bool IsActive)
    {
        PickupCanvasGroup.interactable=IsActive;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(PickupCanvasGroup, ref PickupCanvasGroupSequence, IsActive, () => { });
    }

    public void StopReloadPrompt()
    {
        if(IsInReloadProcess)
        {
            if (TimerID != -1)
                SimpleAnimatorTool.Instance.StopFloatLerpById(TimerID);//如果之前有计时器了就先停掉，避免重复叠加
            ReloadProcessImage.fillAmount = 1;//设置未填充状态
            IsInReloadProcess = false;
        }
    }

    public void UpdateInteractButtonCool(float amount)
    {
        InteractButtonFillButtonImage.fillAmount = amount;
    }

    public void SetActiveInteractButton(bool IsActive)
    {
        InteractButton.interactable = IsActive;
        InteractButton.blocksRaycasts = IsActive;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(InteractButton,ref InteractBackGroundSequence, IsActive, () => { 
        });
    }

    public List<CustomUI> AllCustomUIList = new List<CustomUI>();

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
        //判断当前的枪械做定制化的图标
        if(CurrentGun.gunInfo.Name== "FAMAS")
        {
            GunImage.rectTransform.localScale = new Vector3(-1, 1.3f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(0, 0, 0);
        }
        else if(CurrentGun.gunInfo.Name == "P90")
        {
            GunImage.rectTransform.localScale = new Vector3(-1.3f, 1.3f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-60, 0, 0);
        }
        else if (CurrentGun.gunInfo.Name == "UZI")
        {
            GunImage.rectTransform.localScale = new Vector3(-1.2f, 1f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-60, -8, 0);
        }
        else if (CurrentGun.gunInfo.Name == "AWP")
        {
            GunImage.rectTransform.localScale = new Vector3(-1.1f, 1f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-33, -12, 0);
        }
        else if (CurrentGun.gunInfo.Name == "M110")
        {
            GunImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-20, -12, 0);
        }
        else if (CurrentGun.gunInfo.Name == "AUG")
        {
            GunImage.rectTransform.localScale = new Vector3(-0.8f, 0.8f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(0, -7, 0);
        }
        else if (CurrentGun.gunInfo.Name == "98K")
        {
            GunImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-16, -7, 0);
        }
        else if (CurrentGun.gunInfo.Name == "AKM")
        {
            GunImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-20, 0, 0);
        }
        else if (CurrentGun.gunInfo.Name == "Vector-45")
        {
            GunImage.rectTransform.localScale = new Vector3(-0.9f, 0.9f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-11, 0, 0);
        }
        else if (CurrentGun.gunInfo.Name == "M249")
        {
            GunImage.rectTransform.localScale = new Vector3(-1f, 0.8f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-30, 0, 0);
        }
        else if (CurrentGun.gunInfo.Name == "M762")
        {
            GunImage.rectTransform.anchoredPosition = new Vector3(-10, -20, 0);
        }
        else
        {
            GunImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            GunImage.rectTransform.anchoredPosition = new Vector3(-0, 0, 0);
        }
       
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
        ButtonGroupManager.Instance.AddToggleButtonToGroup(AimButtonButtonGroupName, controlDic[AimButtonButtonGroupName] as Button, isManualTrigger: true);
        SetActiveInteractButton(false);//关闭交互
        IsTriggerPickUpGunButton(false);//开始隐藏
        UpdateMoveButton();
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
        if (controlName == "SettingButton")
            UImanager.Instance.ShowPanel<GamePausePanel>();//打开游戏暂停面板
        else if(controlName == "ScreenFlipButton")
        {
            //点击翻转按钮就提供翻转服务
            GlobalPictureFlipManager.Instance.TriggerGlobalFlip();//触发翻转
        }
    }
    #endregion

    #region 面板显隐以及特殊动画设置

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        //添加按钮交互效果
     
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
    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
        canvasGroup.blocksRaycasts = false;
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
        canvasGroup.blocksRaycasts = true;
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

  
    public void UpdateAllCustomUIInfo()
    {
        foreach (var UI in AllCustomUIList)
        {
            UI.ApplicationInfo();//更新一下信息
        }
    }
}
