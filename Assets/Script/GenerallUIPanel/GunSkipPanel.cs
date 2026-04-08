using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GunSkipPanel : BasePanel
{
    public UIGunViewZoom gunViewZoom;
    public CanvasGroup ChooseButtonCanvasGroup;
    private Sequence ChooseButtonCanvasGroupSequence;
    [Header("控件关联")]
    public TextMeshProUGUI VCTopic;
    [Header("展示面板")]
    public CanvasGroup EffectShowCanvasGroup;
    private Sequence EffectShowCanvasGroupSequence;
    private GunViewType currentPanelType = GunViewType.Normal;
    [Header("枪械类型按钮")]
    public GameObject GunTypeButton;
    [Header("枪械按钮父对象")]
    public Transform GunTypeButtonParent;
    [Header("展示交互按钮")]
    public GameObject skinChoosePrefabs;
    [Header("展示按钮父对象")]
    public Transform skinChooseParent;

    #region 生命周期    
    public override void Awake()
    {
        base.Awake();
        // 初始化列表和字典，防止空引用
        GunTypeButtonList = new List<GameObject>();
        DicObjToBulletBind = new Dictionary<GameObject, SpecialBulletBindPack>();
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

    #region UI控件

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ReturnButton_Test")
        {
            gunViewZoom.ChangeView(GunViewType.Normal);
            IsActiveButtonGroup(true);
            VCTopic.text = "默认";
            currentPanelType = GunViewType.Normal;
            // 返回时清空子弹配置按钮
            ClearBulletBindButton();
        }
        else if (controlName == "BulletButton")
        {
            gunViewZoom.ChangeView(GunViewType.BulletConfig);
            IsActiveButtonGroup(false);
            VCTopic.text = "子弹配置";
            currentPanelType = GunViewType.BulletConfig;
            CreateGunTypeButton();//创建枪械类型按钮
        }
        else if (controlName == "GunSkipButton_Test")
        {
            gunViewZoom.ChangeView(GunViewType.GunSkin);
            IsActiveButtonGroup(false);
            VCTopic.text = "枪械皮肤";
            currentPanelType = GunViewType.GunSkin;
        }
        else if (controlName == "HitObjtButton ")
        {
            gunViewZoom.ChangeView(GunViewType.HitParticle);
            IsActiveButtonGroup(false);
            VCTopic.text = "打击粒子";
            currentPanelType = GunViewType.HitParticle;
        }
    }
    #endregion

    private List<GameObject> GunTypeButtonList;
    private Dictionary<GameObject, SpecialBulletBindPack> DicObjToBulletBind;
    public SpecialBulletBindPack CurrentChooseSpecialBulletBindPack;

    public void CreateGunTypeButton()
    {
        // 先清空旧按钮，防止重复创建
        ClearButtonGroup();

        // 根据枪械类型枚举创建按钮
        foreach (GunType gunType in System.Enum.GetValues(typeof(GunType)))
        {
            GameObject button = PoolManage.Instance.GetObj(GunTypeButton);
            button.transform.localScale = new Vector3(Math.Abs(button.transform.localScale.x), button.transform.localScale.y, button.transform.localScale.z);
            button.transform.SetParent(GunTypeButtonParent, false);
            button.name = gunType.ToString();
            button.GetComponentInChildren<TextMeshProUGUI>().text = gunType.ToString();
            GunTypeButtonList.Add(button);

            //注册按钮组
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str("GunSkipPanelGunTypeButton", button.GetComponent<Button>(), OnGunTypeButtonClicked, CancelGunTypeButton);
        }

        // 默认选中第一个按钮
        if (GunTypeButtonList.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup("GunSkipPanelGunTypeButton");
        }
    }

    public void OnGunTypeButtonClicked(string gunTypeName)
    {
        // 安全转换枚举
        if (Enum.TryParse<GunType>(gunTypeName, out GunType type))
        {
            // 切换枪械类型时，先清空上一个枪械的子弹按钮
            ClearBulletBindButton();

            // 对应逻辑
            switch (currentPanelType)
            {
                case GunViewType.BulletConfig:
                    // 双重保险：防止空值调用
                    if (GameSkinManager.Instance != null)
                    {
                        CreateBulletBind(type);
                    }
                    break;
            }
        }
    }

    //创建子弹捆绑包
    public void CreateBulletBind(GunType Type)
    {
        // 先清空旧子弹按钮
        ClearBulletBindButton();

        if (GameSkinManager.Instance == null)
        {
            Debug.LogError("GameSkinManager 单例未初始化！");
            return;
        }

        var bulletList = GameSkinManager.Instance.GetSpecialBulletBindPackList(Type);
        if (bulletList == null || bulletList.Count == 0)
        {
            Debug.LogWarning("当前枪械无子弹配置数据！");
            return;
        }

        foreach (var InfoPack in bulletList)
        {
            //创建按钮
            GameObject obj = PoolManage.Instance.GetObj(skinChoosePrefabs);
            obj.transform.SetParent(skinChooseParent, false);
            obj.name = InfoPack.name;

            obj.GetComponentInChildren<TextMeshProUGUI>().text = InfoPack.name;

            if (InfoPack.Sprite != null)
            {
                Image iconImg = obj.transform.Find("ShowImage").GetComponent<Image>();
                if (InfoPack.Sprite != null && iconImg != null)
                {
                    iconImg.sprite = InfoPack.Sprite;
                }
            }

            DicObjToBulletBind.Add(obj, InfoPack);

            //添加到按钮组
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str("BulletBind", obj.GetComponent<Button>(), UpdateBulletInfo);
        }

        // 默认选中第一个子弹按钮
        if (DicObjToBulletBind.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup("BulletBind");
        }
    }

    /// <summary>
    ///遍历字典匹配名称，不直接用string查GameObject
    /// </summary>
    public void UpdateBulletInfo(string ButtonName)
    {
        foreach (var item in DicObjToBulletBind)
        {
            // 用GameObject的名称匹配传入的按钮名
            if (item.Key.name == ButtonName)
            {
                CurrentChooseSpecialBulletBindPack = item.Value;
                Debug.Log("选中子弹配置：" + CurrentChooseSpecialBulletBindPack.name);
                break;
            }
        }
    }

    /// <summary>
    /// 清空子弹配置按钮
    /// </summary>
    public void ClearBulletBindButton()
    {
        // 销毁按钮组
        ButtonGroupManager.Instance.DestroyRadioGroup("BulletBind");

        // 对象池回收按钮
        foreach (var item in DicObjToBulletBind.Keys)
        {
            if (item != null)
            {
                PoolManage.Instance.PushObj(skinChoosePrefabs, item);
            }
        }

        // 清空字典
        DicObjToBulletBind.Clear();
    }

    public void ClearButtonGroup()
    {
        ButtonGroupManager.Instance.DestroyRadioGroup("GunSkipPanelGunTypeButton");
        for (int i = GunTypeButtonList.Count - 1; i >= 0; i--)
        {
            PoolManage.Instance.PushObj(GunTypeButton, GunTypeButtonList[i]);
        }
        GunTypeButtonList.Clear();
    }

    public void CancelGunTypeButton(string gunTypeName)
    {

    }

    //是否激活旋转按钮组
    public void IsActiveButtonGroup(bool IsTrigger)
    {
        ChooseButtonCanvasGroup.blocksRaycasts = IsTrigger;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ChooseButtonCanvasGroup, ref ChooseButtonCanvasGroupSequence, IsTrigger, () => { });
        IsActiveEffectShowCanvasGroup(!IsTrigger);
    }

    public void IsActiveEffectShowCanvasGroup(bool IsTrigger)
    {
        EffectShowCanvasGroup.blocksRaycasts = IsTrigger;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(EffectShowCanvasGroup, ref EffectShowCanvasGroupSequence, IsTrigger, () => { });
    }

    #region 面板显隐以及特殊动画

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        ClearButtonGroup();
        ClearBulletBindButton();
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }
    #endregion
}