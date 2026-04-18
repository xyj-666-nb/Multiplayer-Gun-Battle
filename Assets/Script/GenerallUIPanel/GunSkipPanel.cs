using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

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
    [Header("展示按钮父对象 (ScrollRect-Content)")]
    public RectTransform skinChooseParent;

    [Header("代表枪械的图")]
    public CanvasGroup RifleCanvasGroup;
    public CanvasGroup ChargeCanvasGroup;
    public CanvasGroup DMRCanvasGroup;
    public CanvasGroup LightMachineCanvasGroup;
    public CanvasGroup SnipeCanvasGroup;

    [Header("子弹捆绑包交互对象")]
    public Image Bullet;
    public List<Image> CartridgeCaseImageList;
    public Image GunLightImage;

    [Header("子弹图片配置（按枪械类型分组）")]
    public Sprite ChargeBullet;
    public Sprite ChargeCartridgeCase;
    [Space(10)]
    public Sprite RifleBullet;
    public Sprite RifleCartridgeCase;
    [Space(10)]
    public Sprite SnipeBullet;
    public Sprite SnipeCartridgeCase;

    [Header("显示屏幕")]
    public RawImage DisplayScreen;
    private float DefaultTop = 300;
    public float DefaultLeft = 800;


    #region 控制变量
    private GunType _currentGunType = GunType.Rifle;
    private const float GUN_FADE_DURATION = 0.15f;
    private const float SPRITE_FADE_DURATION = 0.12f;
    private bool _isFirstInit = true;

    private bool IsScale = false;

    private class ButtonOriginalState
    {
        public Color showImageColor;
        public float bulletCanvasAlpha;
        public Vector3 headLocalScale;
        public Vector3 showImageLocalScale;
    }
    private Dictionary<GameObject, ButtonOriginalState> _btnOriginalStateCache = new Dictionary<GameObject, ButtonOriginalState>();

    // 子弹配置相关
    private List<GameObject> GunTypeButtonList;
    private Dictionary<GameObject, SpecialBulletBindPack> DicObjToBulletBind;
    public SpecialBulletBindPack CurrentChooseSpecialBulletBindPack;

    // 打击特效相关
    private Dictionary<GameObject, GunHitData> DicObjToHitData;
    public GunHitData CurrentChooseGunHitData;
    private const string HIT_BIND_GROUP = "HitBind";

    // 枪械皮肤相关
    private Dictionary<GameObject, GunSkinPack> DicObjToGunSkinPack;
    public GunSkinPack CurrentChooseGunSkinPack;
    private const string GUN_SKIN_BIND_GROUP = "GunSkinBind";
    #endregion

    #region 生命周期    
    public override void Awake()
    {
        base.Awake();
        GunTypeButtonList = new List<GameObject>();
        DicObjToBulletBind = new Dictionary<GameObject, SpecialBulletBindPack>();
        DicObjToHitData = new Dictionary<GameObject, GunHitData>();
        DicObjToGunSkinPack = new Dictionary<GameObject, GunSkinPack>();
    }

    public override void Start()
    {
        base.Start();
        // 初始化默认隐藏枪械皮肤装备按钮
        SetGunSkinEquipButtonActive(false);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillAllSpriteTweens();
        _btnOriginalStateCache.Clear();
        DicObjToHitData.Clear();
        DicObjToBulletBind.Clear();
        DicObjToGunSkinPack.Clear();
    }
    #endregion

    #region 核心：枪械皮肤装备按钮 + 演示面板 显隐控制
    /// <summary>
    /// 统一控制：演示面板 + 枪械皮肤专属装备按钮
    /// </summary>
    /// <param name="isGunSkinPanel">是否是枪械皮肤面板</param>
    private void SetGunSkinEquipButtonActive(bool isGunSkinPanel)
    {
        IsActiveEffectShowCanvasGroup(!isGunSkinPanel);

        if (controlDic != null && controlDic.ContainsKey("GunSkipButton"))
        {
            GameObject btnObj = controlDic["GunSkipButton"].gameObject;
            btnObj.SetActive(isGunSkinPanel);

        }
    }
    #endregion

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ReturnButton_Test")
        {
            // UI返回音效
            MusicManager.Instance.PlayEffect("Music/update415/ui返回");
            gunViewZoom.ChangeView(GunViewType.Normal);
            IsActiveButtonGroup(true);
            VCTopic.text = "默认";
            currentPanelType = GunViewType.Normal;
            ClearBulletBindButton();
            ClearHitEffectButton();
            ClearGunSkinButton();

            // ========== 返回默认：关闭皮肤按钮，打开演示面板 ==========
            SetGunSkinEquipButtonActive(false);
        }
        else if (controlName == "BulletButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            gunViewZoom.ChangeView(GunViewType.BulletConfig);
            IsActiveButtonGroup(false);
            VCTopic.text = "子弹配置";
            currentPanelType = GunViewType.BulletConfig;
            ClearHitEffectButton();
            ClearGunSkinButton();
            CreateBulletBind(_currentGunType);
            PlayDemoGunByCurrentPanel();

            // ========== 子弹配置：关闭皮肤按钮，打开演示面板 ==========
            SetGunSkinEquipButtonActive(false);
        }
        else if (controlName == "GunSkipButton_Test")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            gunViewZoom.ChangeView(GunViewType.GunSkin);
            IsActiveButtonGroup(false);
            VCTopic.text = "枪械皮肤";
            currentPanelType = GunViewType.GunSkin;
            ClearBulletBindButton();
            ClearHitEffectButton();
            CreateGunSkinButton(_currentGunType);
            PlayDemoGunByCurrentPanel();

            // ========== 枪械皮肤：激活皮肤按钮，关闭演示面板 ==========
            SetGunSkinEquipButtonActive(true);
        }
        else if (controlName == "HitObjtButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            gunViewZoom.ChangeView(GunViewType.HitParticle);
            IsActiveButtonGroup(false);
            VCTopic.text = "打击粒子";
            currentPanelType = GunViewType.HitParticle;
            ClearBulletBindButton();
            ClearGunSkinButton();
            CreateHitEffectBind();
            PlayDemoGunByCurrentPanel();

            // ========== 打击粒子：关闭皮肤按钮，打开演示面板 ==========
            SetGunSkinEquipButtonActive(false);
        }
        else if (controlName == "EffectScreen")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            EffectShowCanvasGroup.DOKill();
            if (IsScale)
            {
                EffectShowCanvasGroup.transform.DOScale(Vector2.one, 0.5f);
            }
            else
            {
                EffectShowCanvasGroup.transform.DOScale(2.2f * Vector2.one, 0.5f);
            }
            IsScale = !IsScale;
        }
        else if (controlName == "TestButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            if (currentPanelType == GunViewType.BulletConfig || currentPanelType == GunViewType.HitParticle || currentPanelType == GunViewType.GunSkin)
            {
                PlayDemoGunByCurrentPanel();
            }
        }
        else if (controlName == "EquipButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            if (currentPanelType == GunViewType.BulletConfig)
            {
                if (CurrentChooseSpecialBulletBindPack != null)
                {
                    GameSkinManager.Instance.SetPlayerSkinPack(CurrentChooseSpecialBulletBindPack.BulletBindID);
                    WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "已装备子弹配置");
                }
            }
            else if (currentPanelType == GunViewType.HitParticle)
            {
                WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "已装备打击粒子");
                GameSkinManager.Instance.CurrentOwnerHitObj = CurrentChooseGunHitData;
            }
        }
        else if (controlName == "GunSkipButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            //枪械皮肤专属安装按钮
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "已装备枪械皮肤");
            GameSkinManager.Instance.EquipmentGunSkin(CurrentChooseGunSkinPack.skinGuid);//传入ID自动装备
        }
    }
    #endregion

    #region 联动 DemoGun 核心
    private void PlayDemoGunByCurrentPanel()
    {
        if (DemoGun.Instance == null)
        {
            Debug.LogWarning("[GunSkipPanel] 场景中未找到 DemoGun！");
            return;
        }

        switch (currentPanelType)
        {
            case GunViewType.BulletConfig:
                if (CurrentChooseSpecialBulletBindPack != null)
                {
                    DemoGun.Instance.TestShoot(CurrentChooseSpecialBulletBindPack);
                }
                break;
            case GunViewType.HitParticle:
                if (CurrentChooseGunHitData != null)
                {
                    DemoGun.Instance.HitEffect = CurrentChooseGunHitData.HitObj;
                    var defaultBullet = GameSkinManager.Instance.ReturnBulletVisualConfig(_currentGunType);
                    DemoGun.Instance.TestShoot(defaultBullet);
                }
                else
                {
                    DemoGun.Instance.DebugTestShoot();
                }
                break;
            case GunViewType.GunSkin:
                DemoGun.Instance.DebugTestShoot();
                break;
            case GunViewType.Normal:
            default:
                break;
        }
    }
    #endregion

    #region 打击特效按钮逻辑
    /// <summary>
    /// 生成打击特效交互按钮
    /// </summary>
    public void CreateHitEffectBind()
    {
        ClearHitEffectButton();

        if (GameSkinManager.Instance == null)
        {
            Debug.LogError("GameSkinManager 未初始化！");
            return;
        }

        var hitList = GameSkinManager.Instance.CurrentGunHitDataList;
        if (hitList == null || hitList.Count == 0)
        {
            Debug.LogWarning("当前无打击特效数据！");
            return;
        }

        foreach (var hitData in hitList)
        {
            GameObject obj = PoolManage.Instance.GetObj(skinChoosePrefabs);
            obj.transform.SetParent(skinChooseParent, false);
            obj.name = hitData.HitName;

            ButtonOriginalState originalState = new ButtonOriginalState();
            Transform showImageTrans = obj.transform.Find("ShowImage");
            TextMeshProUGUI btnText = obj.GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = hitData.HitName;
            if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
            {
                originalState.showImageColor = showImage.color;
                showImage.sprite = hitData.HitIcon;
                showImage.color = Color.white;
                showImage.SetAllDirty();
            }

            _btnOriginalStateCache.Add(obj, originalState);
            DicObjToHitData.Add(obj, hitData);

            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(HIT_BIND_GROUP, obj.GetComponent<Button>(), UpdateHitEffectInfo);
        }

        if (DicObjToHitData.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(HIT_BIND_GROUP);
        }

        StartCoroutine(RefreshContentHeightCoroutine());
    }

    /// <summary>
    /// 选中打击特效按钮
    /// </summary>
    public void UpdateHitEffectInfo(string buttonName)
    {
        foreach (var item in DicObjToHitData)
        {
            if (item.Key.name == buttonName)
            {
                CurrentChooseGunHitData = item.Value;
                PlayDemoGunByCurrentPanel();
                break;
            }
        }
    }

    /// <summary>
    /// 清空打击特效按钮
    /// </summary>
    public void ClearHitEffectButton()
    {
        ButtonGroupManager.Instance.DestroyRadioGroup(HIT_BIND_GROUP);

        foreach (var item in DicObjToHitData.Keys)
        {
            if (item != null)
            {
                if (_btnOriginalStateCache.TryGetValue(item, out ButtonOriginalState originalState))
                {
                    Transform showImageTrans = item.transform.Find("ShowImage");
                    if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
                    {
                        showImage.color = originalState.showImageColor;
                        showImage.sprite = null;
                    }
                }
                PoolManage.Instance.PushObj(skinChoosePrefabs, item);
            }
        }

        DicObjToHitData.Clear();
        _btnOriginalStateCache.Clear();
        CurrentChooseGunHitData = null;
    }
    #endregion

    #region 子弹配置按钮逻辑
    public void CreateGunTypeButton()
    {
        ClearButtonGroup();

        foreach (GunType gunType in System.Enum.GetValues(typeof(GunType)))
        {
            GameObject button = PoolManage.Instance.GetObj(GunTypeButton);
            button.transform.localScale = new Vector3(Math.Abs(button.transform.localScale.x), button.transform.localScale.y, button.transform.localScale.z);
            button.transform.SetParent(GunTypeButtonParent, false);
            button.name = gunType.ToString();
            button.GetComponentInChildren<TextMeshProUGUI>().text = MilitaryManager.Instance.GetChineseGunTypeName(gunType);
            GunTypeButtonList.Add(button);

            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str("GunSkipPanelGunTypeButton", button.GetComponent<Button>(), OnGunTypeButtonClicked, CancelGunTypeButton);
        }

        if (GunTypeButtonList.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup("GunSkipPanelGunTypeButton");
        }
    }

    public void OnGunTypeButtonClicked(string gunTypeName)
    {
        if (Enum.TryParse<GunType>(gunTypeName, out GunType type))
        {
            _currentGunType = type;
            RefreshGunDisplay();
            RefreshBulletSpriteWithFade();

            ClearBulletBindButton();
            ClearHitEffectButton();
            ClearGunSkinButton();

            switch (currentPanelType)
            {
                case GunViewType.BulletConfig:
                    if (GameSkinManager.Instance != null)
                    {
                        CreateBulletBind(type);
                    }
                    break;
                case GunViewType.GunSkin:
                    if (GameSkinManager.Instance != null)
                    {
                        CreateGunSkinButton(type);
                    }
                    break;
            }
        }
    }

    public void CreateBulletBind(GunType Type)
    {
        ClearBulletBindButton();

        if (GameSkinManager.Instance == null)
        {
            Debug.LogError("GameSkinManager 未初始化！");
            return;
        }

        var bulletList = GameSkinManager.Instance.GetSpecialBulletBindPackList(Type);
        if (bulletList == null || bulletList.Count == 0)
        {
            Debug.LogWarning("当前枪械无子弹配置数据！");
            return;
        }

        var (bulletSprite, caseSprite) = GetSpriteByGunType(Type);
        bool isRifleSeries = Type == GunType.Rifle || Type == GunType.LightMachineGun;

        foreach (var InfoPack in bulletList)
        {
            GameObject obj = PoolManage.Instance.GetObj(skinChoosePrefabs);
            obj.transform.SetParent(skinChooseParent, false);
            obj.name = InfoPack.name;

            ButtonOriginalState originalState = new ButtonOriginalState();
            Transform showImageTrans = obj.transform.Find("ShowImage");
            Transform bulletTrans = obj.transform.Find("Bullet");
            Transform headTrans = bulletTrans?.Find("Head");
            obj.GetComponentInChildren<TextMeshProUGUI>().text = InfoPack.name;
            if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
            {
                originalState.showImageColor = showImage.color;
                Color tempColor = showImage.color;
                tempColor.a = 0;
                showImage.color = tempColor;
            }

            CanvasGroup bulletCanvasGroup = null;
            if (bulletTrans != null && bulletTrans.TryGetComponent(out bulletCanvasGroup))
            {
                originalState.bulletCanvasAlpha = bulletCanvasGroup.alpha;
                bulletCanvasGroup.alpha = 1;
                bulletCanvasGroup.blocksRaycasts = true;
                bulletCanvasGroup.interactable = true;
            }

            if (headTrans != null)
            {
                originalState.headLocalScale = headTrans.localScale;
                if (isRifleSeries)
                {
                    headTrans.localScale = new Vector3(0.8f, 1f, 1f);
                }
            }

            _btnOriginalStateCache.Add(obj, originalState);

            if (bulletTrans != null && InfoPack.bulletVisualConfig != null)
            {
                if (headTrans != null && headTrans.TryGetComponent(out Image headImg))
                {
                    headImg.sprite = bulletSprite;
                    headImg.color = InfoPack.bulletVisualConfig.bulletColor;
                    headImg.SetAllDirty();
                }
                Transform caseTrans = bulletTrans.Find("Case");
                if (caseTrans != null && caseTrans.TryGetComponent(out Image caseImg))
                {
                    caseImg.sprite = caseSprite;
                    caseImg.color = InfoPack.bulletVisualConfig.cartridgeCaseColor;
                    caseImg.SetAllDirty();
                }
            }

            DicObjToBulletBind.Add(obj, InfoPack);
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str("BulletBind", obj.GetComponent<Button>(), UpdateBulletInfo);
        }

        if (DicObjToBulletBind.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup("BulletBind");
        }

        StartCoroutine(RefreshContentHeightCoroutine());
    }

    public void UpdateBulletInfo(string ButtonName)
    {
        foreach (var item in DicObjToBulletBind)
        {
            if (item.Key.name == ButtonName)
            {
                CurrentChooseSpecialBulletBindPack = item.Value;
                ApplyBulletPackColorToUI();
                PlayDemoGunByCurrentPanel();
                break;
            }
        }
    }

    private void ApplyBulletPackColorToUI()
    {
        if (CurrentChooseSpecialBulletBindPack == null) return;

        var bulletConfig = CurrentChooseSpecialBulletBindPack.bulletVisualConfig;
        var flashConfig = CurrentChooseSpecialBulletBindPack.muzzleFlashConfig;

        if (Bullet != null && bulletConfig != null)
        {
            Bullet.color = bulletConfig.bulletColor;
            Bullet.SetAllDirty();
        }

        if (CartridgeCaseImageList != null && bulletConfig != null)
        {
            foreach (var img in CartridgeCaseImageList)
            {
                if (img != null)
                {
                    img.color = bulletConfig.cartridgeCaseColor;
                    img.SetAllDirty();
                }
            }
        }

        if (GunLightImage != null && flashConfig != null)
        {
            GunLightImage.color = flashConfig.lightStartColor;
            GunLightImage.SetAllDirty();
        }
    }

    public void ClearBulletBindButton()
    {
        ButtonGroupManager.Instance.DestroyRadioGroup("BulletBind");

        foreach (var item in DicObjToBulletBind.Keys)
        {
            if (item != null)
            {
                if (_btnOriginalStateCache.TryGetValue(item, out ButtonOriginalState originalState))
                {
                    Transform showImageTrans = item.transform.Find("ShowImage");
                    if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
                    {
                        showImage.color = originalState.showImageColor;
                    }

                    Transform bulletTrans = item.transform.Find("Bullet");
                    if (bulletTrans != null && bulletTrans.TryGetComponent(out CanvasGroup bulletCanvasGroup))
                    {
                        bulletCanvasGroup.alpha = originalState.bulletCanvasAlpha;
                        bulletCanvasGroup.blocksRaycasts = originalState.bulletCanvasAlpha > 0.5f;
                        bulletCanvasGroup.interactable = originalState.bulletCanvasAlpha > 0.5f;
                    }

                    Transform headTrans = bulletTrans?.Find("Head");
                    if (headTrans != null)
                    {
                        headTrans.localScale = originalState.headLocalScale;
                    }
                }

                PoolManage.Instance.PushObj(skinChoosePrefabs, item);
            }
        }

        DicObjToBulletBind.Clear();
        _btnOriginalStateCache.Clear();
        CurrentChooseSpecialBulletBindPack = null;
    }
    #endregion

    #region 枪械皮肤按钮逻辑（含特殊缩放+重置）
    /// <summary>
    /// 生成枪械皮肤交互按钮（特殊缩放规则）
    /// </summary>
    public void CreateGunSkinButton(GunType Type)
    {
        ClearGunSkinButton();

        if (GameSkinManager.Instance == null)
        {
            Debug.LogError("GameSkinManager 未初始化！");
            return;
        }

        var skinList = GameSkinManager.Instance.CurrentGunSkinPackList;
        if (skinList == null || skinList.Count == 0)
        {
            Debug.LogWarning("当前无枪械皮肤数据！");
            return;
        }

        foreach (var skinPack in skinList)
        {
            if (MilitaryManager.Instance.GetGunType(skinPack.GunRealName) == Type)
            {
                GameObject obj = PoolManage.Instance.GetObj(skinChoosePrefabs);
                obj.transform.SetParent(skinChooseParent, false);
                obj.name = skinPack.skinName;

                ButtonOriginalState originalState = new ButtonOriginalState();
                originalState.showImageLocalScale = Vector3.one;
                Transform showImageTrans = obj.transform.Find("ShowImage");
                TextMeshProUGUI btnText = obj.GetComponentInChildren<TextMeshProUGUI>();

                btnText.text = skinPack.skinName;
                if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
                {
                    originalState.showImageColor = showImage.color;
                    showImage.sprite = skinPack.skinIcon;
                    showImage.color = Color.white;
                    showImage.SetAllDirty();

                    Vector3 targetScale = Vector3.one;
                    switch (Type)
                    {
                        case GunType.Rifle:
                            targetScale = new Vector3(2.5f, 3.5f, 1);
                            break;
                        case GunType.Charge:
                            if (skinPack.GunRealName.Equals("P90", StringComparison.OrdinalIgnoreCase))
                                targetScale = new Vector3(3, 4, 1);
                            else if (skinPack.GunRealName.Equals("UZI", StringComparison.OrdinalIgnoreCase))
                                targetScale = new Vector3(1.5f, 1.5f, 1);
                            else if (skinPack.GunRealName.Equals("Vector-45", StringComparison.OrdinalIgnoreCase))
                                targetScale = new Vector3(2, 3, 1);
                            break;
                        case GunType.LightMachineGun:
                        case GunType.Snipe:
                        case GunType.DMR:
                            targetScale = new Vector3(2, 3, 1);
                            break;
                        default:
                            targetScale = Vector3.one;
                            break;
                    }
                    showImage.transform.localScale = targetScale;
                }

                _btnOriginalStateCache.Add(obj, originalState);
                DicObjToGunSkinPack.Add(obj, skinPack);

                ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(GUN_SKIN_BIND_GROUP, obj.GetComponent<Button>(), UpdateGunSkinInfo);
            }
        }

        if (DicObjToGunSkinPack.Count > 0)
        {
            ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(GUN_SKIN_BIND_GROUP);
        }

        StartCoroutine(RefreshContentHeightCoroutine());
    }

    /// <summary>
    /// 选中枪械皮肤按钮
    /// </summary>
    public void UpdateGunSkinInfo(string buttonName)
    {
        foreach (var item in DicObjToGunSkinPack)
        {
            if (item.Key.name == buttonName)
            {
                CurrentChooseGunSkinPack = item.Value;
                break;
            }
        }
    }

    /// <summary>
    /// 清空枪械皮肤按钮（重置缩放为1,1,1）
    /// </summary>
    public void ClearGunSkinButton()
    {
        ButtonGroupManager.Instance.DestroyRadioGroup(GUN_SKIN_BIND_GROUP);

        foreach (var item in DicObjToGunSkinPack.Keys)
        {
            if (item != null)
            {
                if (_btnOriginalStateCache.TryGetValue(item, out ButtonOriginalState originalState))
                {
                    Transform showImageTrans = item.transform.Find("ShowImage");
                    if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
                    {
                        showImage.color = originalState.showImageColor;
                        showImage.sprite = null;
                        showImage.transform.localScale = originalState.showImageLocalScale;
                    }
                }
                PoolManage.Instance.PushObj(skinChoosePrefabs, item);
            }
        }

        DicObjToGunSkinPack.Clear();
        _btnOriginalStateCache.Clear();
        CurrentChooseGunSkinPack = null;
    }
    #endregion

    private IEnumerator RefreshContentHeightCoroutine()
    {
        yield return null;
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

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        ClearButtonGroup();
        ClearBulletBindButton();
        ClearHitEffectButton();
        ClearGunSkinButton();
        // 隐藏面板时重置皮肤按钮
        SetGunSkinEquipButtonActive(false);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);

        if (GunTypeButtonList.Count == 0)
        {
            CreateGunTypeButton();
        }

        if (_isFirstInit)
        {
            _isFirstInit = false;
            RefreshGunDisplay();
            SetBulletSpriteImmediately();
        }
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

    #region 枪械显示控制
    private void RefreshGunDisplay()
    {
        foreach (GunType gunType in System.Enum.GetValues(typeof(GunType)))
        {
            CanvasGroup cg = GetGunCanvasGroupByType(gunType);
            if (cg == null) continue;

            if (gunType == _currentGunType)
            {
                ShowCanvasGroup(cg);
            }
            else
            {
                HideCanvasGroup(cg);
            }
        }
    }

    private void ShowCanvasGroup(CanvasGroup cg)
    {
        if (cg == null) return;

        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.DOKill();
        cg.DOFade(1, GUN_FADE_DURATION).SetEase(Ease.OutQuad);
    }

    private void HideCanvasGroup(CanvasGroup cg)
    {
        if (cg == null) return;

        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.DOKill();
        cg.DOFade(0, GUN_FADE_DURATION).SetEase(Ease.OutQuad);
    }

    private CanvasGroup GetGunCanvasGroupByType(GunType gunType)
    {
        return gunType switch
        {
            GunType.Rifle => RifleCanvasGroup,
            GunType.Charge => ChargeCanvasGroup,
            GunType.DMR => DMRCanvasGroup,
            GunType.LightMachineGun => LightMachineCanvasGroup,
            GunType.Snipe => SnipeCanvasGroup,
            _ => null
        };
    }
    #endregion

    #region 子弹/弹壳图片替换逻辑
    private (Sprite bulletSprite, Sprite caseSprite) GetSpriteByGunType(GunType gunType)
    {
        return gunType switch
        {
            GunType.Charge => (ChargeBullet, ChargeCartridgeCase),
            GunType.Rifle or GunType.LightMachineGun => (RifleBullet, RifleCartridgeCase),
            GunType.Snipe or GunType.DMR => (SnipeBullet, SnipeCartridgeCase),
            _ => (RifleBullet, RifleCartridgeCase)
        };
    }

    private void SetBulletSpriteImmediately()
    {
        var (bulletSprite, caseSprite) = GetSpriteByGunType(_currentGunType);

        if (Bullet != null && bulletSprite != null)
        {
            Bullet.sprite = bulletSprite;
            Bullet.SetAllDirty();
        }

        if (CartridgeCaseImageList != null && caseSprite != null)
        {
            foreach (var img in CartridgeCaseImageList)
            {
                if (img != null)
                {
                    img.sprite = caseSprite;
                    img.SetAllDirty();
                }
            }
        }
    }

    private void RefreshBulletSpriteWithFade()
    {
        var (targetBulletSprite, targetCaseSprite) = GetSpriteByGunType(_currentGunType);

        KillAllSpriteTweens();

        Sequence spriteSequence = DOTween.Sequence();

        if (Bullet != null)
        {
            spriteSequence.Join(Bullet.DOFade(0, SPRITE_FADE_DURATION).SetEase(Ease.OutQuad));
        }
        if (CartridgeCaseImageList != null)
        {
            foreach (var img in CartridgeCaseImageList)
            {
                if (img != null)
                {
                    spriteSequence.Join(img.DOFade(0, SPRITE_FADE_DURATION).SetEase(Ease.OutQuad));
                }
            }
        }

        spriteSequence.AppendCallback(() =>
        {
            if (Bullet != null && targetBulletSprite != null)
            {
                Bullet.sprite = targetBulletSprite;
                Bullet.SetAllDirty();
            }

            if (CartridgeCaseImageList != null && targetCaseSprite != null)
            {
                foreach (var img in CartridgeCaseImageList)
                {
                    if (img != null)
                    {
                        img.sprite = targetCaseSprite;
                        img.SetAllDirty();
                    }
                }
            }
        });

        if (Bullet != null)
        {
            spriteSequence.Append(Bullet.DOFade(1, SPRITE_FADE_DURATION).SetEase(Ease.InQuad));
        }
        if (CartridgeCaseImageList != null)
        {
            foreach (var img in CartridgeCaseImageList)
            {
                if (img != null)
                {
                    spriteSequence.Join(img.DOFade(1, SPRITE_FADE_DURATION).SetEase(Ease.InQuad));
                }
            }
        }

        spriteSequence.Play();
    }

    private void KillAllSpriteTweens()
    {
        if (Bullet != null)
        {
            Bullet.DOKill();
        }
        if (CartridgeCaseImageList != null)
        {
            foreach (var img in CartridgeCaseImageList)
            {
                if (img != null)
                {
                    img.DOKill();
                }
            }
        }
    }
    #endregion
}