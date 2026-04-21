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

    [Header("统一枪械显示")]
    public CanvasGroup RifleCanvasGroup;

    [Header("子弹捆绑包交互对象")]
    public Image Bullet;
    public List<Image> CartridgeCaseImageList;
    public Image GunLightImage;

    [Header("显示屏幕")]
    public RawImage DisplayScreen;
    private float DefaultTop = 300;
    public float DefaultLeft = 800;

    [Header("疑惑面板")]
    public CanvasGroup questionPanel;
    private Sequence questionPanelSequence;

    #region 控制变量
    private GunType _currentGunType = GunType.Rifle;
    private const float GUN_FADE_DURATION = 0.15f;
    private bool _isFirstInit = true;

    private bool IsScale = false;

    [Header("枪械大图标显示")]
    public List<Image> bigGunShowImageList;

    public void IsTriggerQuestionPanel(bool IsTrigger)
    {
        questionPanel.blocksRaycasts = IsTrigger;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(questionPanel, ref questionPanelSequence, IsTrigger, () => { });
    }

    private class ButtonOriginalState
    {
        public Color showImageColor;
        public float bulletCanvasAlpha;
        public Vector3 headLocalScale;
        public Vector3 showImageLocalScale;
    }
    private Dictionary<GameObject, ButtonOriginalState> _btnOriginalStateCache = new Dictionary<GameObject, ButtonOriginalState>();

    private void SetSkinChooseButtonDisplay(GameObject obj, bool showImageActive, bool bulletActive, bool gunImageActive)
    {
        if (obj == null)
        {
            return;
        }

        Transform showImageTrans = obj.transform.Find("ShowImage");
        if (showImageTrans != null)
        {
            showImageTrans.gameObject.SetActive(showImageActive);
        }

        Transform bulletTrans = obj.transform.Find("Bullet");
        if (bulletTrans != null)
        {
            bulletTrans.gameObject.SetActive(bulletActive);
        }

        Transform gunImageTrans = obj.transform.Find("GunImage");
        if (gunImageTrans != null)
        {
            gunImageTrans.gameObject.SetActive(gunImageActive);
        }
    }

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

    #region 核心：枪械皮肤装备按钮 显隐控制
    /// <summary>
    /// 控制：枪械皮肤专属装备按钮
    /// </summary>
    private void SetGunSkinEquipButtonActive(bool isActive)
    {
        if (controlDic != null && controlDic.ContainsKey("GunSkipButton"))
        {
            GameObject btnObj = controlDic["GunSkipButton"].gameObject;
            btnObj.SetActive(isActive);
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
            VCTopic.text = "默认";
            currentPanelType = GunViewType.Normal;

            ClearBulletBindButton();
            ClearHitEffectButton();
            ClearGunSkinButton();
            ClearButtonGroup();

            // ========== 返回默认：主选单开启，关闭皮肤按钮，关闭演示面板 ==========
            IsActiveButtonGroup(true);
            SetGunSkinEquipButtonActive(false);
            IsActiveEffectShowCanvasGroup(false);
        }
        else if (controlName == "BulletButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            gunViewZoom.ChangeView(GunViewType.BulletConfig);
            VCTopic.text = "子弹配置";
            currentPanelType = GunViewType.BulletConfig;
            EnsureGunTypeButtonsCreated();

            ClearHitEffectButton();
            ClearGunSkinButton();
            CreateBulletBind(_currentGunType);
            PlayDemoGunByCurrentPanel();

            // ========== 子弹配置：主选单隐藏，关闭皮肤按钮，打开演示面板 ==========
            IsActiveButtonGroup(false);
            SetGunSkinEquipButtonActive(false);
            IsActiveEffectShowCanvasGroup(true);
        }
        else if (controlName == "GunSkipButton_Test")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            gunViewZoom.ChangeView(GunViewType.GunSkin);
            VCTopic.text = "枪械皮肤";
            currentPanelType = GunViewType.GunSkin;
            EnsureGunTypeButtonsCreated();

            ClearBulletBindButton();
            ClearHitEffectButton();
            CreateGunSkinButton(_currentGunType);
            PlayDemoGunByCurrentPanel();

            // ========== 枪械皮肤：主选单隐藏，激活皮肤按钮，关闭演示面板 ==========
            IsActiveButtonGroup(false);
            SetGunSkinEquipButtonActive(true);
            IsActiveEffectShowCanvasGroup(false);
        }
        else if (controlName == "HitObjtButton")
        {
            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            gunViewZoom.ChangeView(GunViewType.HitParticle);
            VCTopic.text = "打击粒子";
            currentPanelType = GunViewType.HitParticle;
            EnsureGunTypeButtonsCreated();

            ClearBulletBindButton();
            ClearGunSkinButton();
            CreateHitEffectBind();
            PlayDemoGunByCurrentPanel();

            // ========== 主选单隐藏，关闭皮肤按钮，打开演示面板 ==========
            IsActiveButtonGroup(false);
            SetGunSkinEquipButtonActive(false);
            IsActiveEffectShowCanvasGroup(true);
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
                    GameSkinManager.Instance.EquipBulletBindPack(CurrentChooseSpecialBulletBindPack.BulletBindID);//装备子弹包
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
        else if (controlName == "ProblemButton")
        {
            IsTriggerQuestionPanel(true);
        }
        else if (controlName == "ConfirmButton")
        {
            IsTriggerQuestionPanel(false);
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
            SetSkinChooseButtonDisplay(obj, true, false, false);
            Transform showImageTrans = obj.transform.Find("ShowImage");
            TextMeshProUGUI[] allTexts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (allTexts != null)
            {
                foreach (var text in allTexts)
                {
                    if (text == null)
                    {
                        continue;
                    }

                    text.gameObject.SetActive(true);
                    text.text = hitData.HitName;
                    text.color = ColorManager.SetColorAlpha(text.color, 1f);
                    text.ForceMeshUpdate();
                    text.SetAllDirty();
                }
            }
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

                    Transform gunImageTrans = item.transform.Find("GunImage");
                    if (gunImageTrans != null && gunImageTrans.TryGetComponent(out Image gunImage))
                    {
                        gunImage.sprite = null;
                        gunImage.color = Color.white;
                    }

                    SetSkinChooseButtonDisplay(item, true, false, false);
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

            // 仅清空由于枪械类型而产生变化的选项，打击特效因为是全枪械通用所以不清除！
            ClearBulletBindButton();
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

            // 因为切换了测试武器大类，所以立刻更新Demo靶场的表现效果
            if (currentPanelType != GunViewType.Normal)
            {
                PlayDemoGunByCurrentPanel();
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

        bool isRifleSeries = Type == GunType.Rifle || Type == GunType.LightMachineGun;

        foreach (var InfoPack in bulletList)
        {
            GameObject obj = PoolManage.Instance.GetObj(skinChoosePrefabs);
            obj.transform.SetParent(skinChooseParent, false);
            obj.name = InfoPack.name;

            ButtonOriginalState originalState = new ButtonOriginalState();
            SetSkinChooseButtonDisplay(obj, true, true, false);
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
                    headImg.color = InfoPack.bulletVisualConfig.bulletColor;
                    headImg.SetAllDirty();
                }
                Transform caseTrans = bulletTrans.Find("Case");
                if (caseTrans != null && caseTrans.TryGetComponent(out Image caseImg))
                {
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

                    Transform gunImageTrans = item.transform.Find("GunImage");
                    if (gunImageTrans != null && gunImageTrans.TryGetComponent(out Image gunImage))
                    {
                        gunImage.sprite = null;
                        gunImage.color = Color.white;
                    }

                    SetSkinChooseButtonDisplay(item, true, true, false);
                }

                PoolManage.Instance.PushObj(skinChoosePrefabs, item);
            }
        }

        DicObjToBulletBind.Clear();
        _btnOriginalStateCache.Clear();
        CurrentChooseSpecialBulletBindPack = null;
    }
    #endregion

    #region 枪械皮肤按钮逻辑
    /// <summary>
    /// 生成枪械皮肤交互按钮
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
                SetSkinChooseButtonDisplay(obj, false, false, true);
                Transform showImageTrans = obj.transform.Find("ShowImage");
                Transform gunImageTrans = obj.transform.Find("GunImage");
                TextMeshProUGUI btnText = obj.GetComponentInChildren<TextMeshProUGUI>();

                btnText.text = skinPack.skinName;
                if (showImageTrans != null && showImageTrans.TryGetComponent(out Image showImage))
                {
                    originalState.showImageColor = showImage.color;
                    showImage.sprite = null;
                    showImage.color = Color.white;
                    showImage.transform.localScale = Vector3.one;
                    showImage.SetAllDirty();
                }

                if (gunImageTrans != null && gunImageTrans.TryGetComponent(out Image gunImage))
                {
                    gunImage.sprite = null;
                    if (skinPack.standardSprite != null)
                    {
                        gunImage.sprite = skinPack.standardSprite;
                    }
                    gunImage.color = Color.white;
                    gunImage.transform.localScale = Vector3.one;
                    gunImage.SetAllDirty();
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
                ApplySelectedGunSkinToBigImages();
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

                    Transform gunImageTrans = item.transform.Find("GunImage");
                    if (gunImageTrans != null && gunImageTrans.TryGetComponent(out Image gunImage))
                    {
                        gunImage.sprite = null;
                        gunImage.color = Color.white;
                        gunImage.transform.localScale = Vector3.one;
                    }

                    SetSkinChooseButtonDisplay(item, true, true, false);
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

    private void EnsureGunTypeButtonsCreated()
    {
        if (GunTypeButtonList.Count == 0)
        {
            CreateGunTypeButton();
        }
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

        // 隐藏面板时关闭额外组件
        SetGunSkinEquipButtonActive(false);
        IsActiveEffectShowCanvasGroup(false);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);


        if (_isFirstInit)
        {
            _isFirstInit = false;
            RefreshGunDisplay();
            IsActiveEffectShowCanvasGroup(false);
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
        EnsureRifleCanvasGroupVisible();
    }

    private void EnsureRifleCanvasGroupVisible()
    {
        if (RifleCanvasGroup == null)
        {
            return;
        }

        RifleCanvasGroup.blocksRaycasts = true;
        RifleCanvasGroup.interactable = true;
        RifleCanvasGroup.DOKill();
        RifleCanvasGroup.alpha = 1f;
    }

    private void ApplySelectedGunSkinToBigImages()
    {
        if (CurrentChooseGunSkinPack == null || CurrentChooseGunSkinPack.standardSprite == null || bigGunShowImageList == null)
        {
            return;
        }

        foreach (var image in bigGunShowImageList)
        {
            if (image == null)
            {
                continue;
            }

            image.sprite = CurrentChooseGunSkinPack.standardSprite;
            image.SetAllDirty();
        }
    }
    #endregion

    #region 子弹/弹壳图片替换逻辑
    private void SetBulletSpriteImmediately()
    {
    }

    private void RefreshBulletSpriteWithFade()
    {
    }

    private void KillAllSpriteTweens()
    {
    }
    #endregion
}
