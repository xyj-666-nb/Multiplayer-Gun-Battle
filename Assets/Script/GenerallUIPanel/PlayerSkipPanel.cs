using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerSkipPanel : BasePanel
{
    [Header("控件关联")]
    public Image PlayerShowImage;
    public TextMeshProUGUI PlayerSkinText;
    public TextMeshProUGUI PlayerSkinDescribe;
    [Header("滚动视图内容对象")]
    public RectTransform Content;
    [Header("自动布局组件")]
    public VerticalLayoutGroup LayoutGroup;
    [Header("皮肤按钮预制体")]
    public GameObject SkinButtonPrefab;
    [Header("单个按钮高度（用于计算Content高度）")]
    public float ButtonHeight = 100f;
    private Dictionary<Button, PlayerSkinPack> SkinButtonToDataMap = new Dictionary<Button, PlayerSkinPack>();

    // 有序存储按钮名字，用于左右切换
    private List<string> _skinButtonNames = new List<string>();
    private int _currentSelectedIndex = 0;

    [Header("皮肤品质提示图")]
    public TextMeshProUGUI QualityText;
    public Image QualityImage;
    [Header("品质提示颜色")]
    public Color NormalColor;
    public Color RareColor;
    public Color EpicColor;

    [Header("功能按钮动画配置")]
    public float ButtonSelectScale = 1.05f;
    public Color ButtonSelectColor = new Color(0.4f, 1f, 0.4f); // 浅绿
    public float ButtonAnimDuration = 0.2f;

    public PlayerSkinPack playerSkinPack;
    private string ButtonGroupName = "PlayerSkipPanelButtonGroup";

    public UIPlayerMove UIMove;
    public CanvasGroup ArmorCanvasGroup;
    private Sequence SequenceArmorCanvasGroup;
    public Image GunSprite;

    private Dictionary<Button, bool> _functionButtonStates = new Dictionary<Button, bool>();
    private Dictionary<Button, Vector3> _functionButtonOriginalScales = new Dictionary<Button, Vector3>();
    private Dictionary<Button, Color> _functionButtonOriginalColors = new Dictionary<Button, Color>();

    // 按钮名字常量
    private const string BTN_EQUIPMENT = "EquipmenPlayertButton";
    private const string BTN_GUN = "GunButton";
    private const string BTN_WALK = "WalkButton";

    //左右移动选择按钮
    private string LEFTBUTTOSTRING = "LeftButton";
    private string RIGHTBUTTONSTRING = "RightButton";

    public void SetQuality(GoodsQuality Quality)
    {
        QualityImage.DOKill();
        switch (Quality)
        {
            case GoodsQuality.Normal:
                if (QualityText != null) QualityText.text = "普通";
                QualityImage.DOColor(NormalColor, 0.5f).SetEase(Ease.OutQuad);
                break;
            case GoodsQuality.Rare:
                if (QualityText != null) QualityText.text = "稀有";
                QualityImage.DOColor(RareColor, 0.5f).SetEase(Ease.OutQuad);
                break;
            case GoodsQuality.Epic:
                if (QualityText != null) QualityText.text = "史诗";
                QualityImage.DOColor(EpicColor, 0.5f).SetEase(Ease.OutQuad);
                break;
        }
    }

    public Color GetQualityColor(GoodsQuality Quality)
    {
        switch (Quality)
        {
            case GoodsQuality.Normal:
                return NormalColor;
            case GoodsQuality.Rare:
                return RareColor;
            case GoodsQuality.Epic:
                return EpicColor;
            default:
                return Color.white;
        }
    }

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        // 初始化功能按钮状态
        InitFunctionButtons();
    }

    public override void Start()
    {
        base.Start();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 清理所有按钮动画
        CleanupButtonAnimations();
    }
    #endregion

    #region UI控件

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        // 处理功能按钮点击
        if (controlName == BTN_EQUIPMENT)
        {
            Button btn = GetButtonFromDic(BTN_EQUIPMENT);
            ToggleFunctionButton(btn, IsShowEquipment);
            
        }
        else if (controlName == BTN_GUN)
        {
            Button btn = GetButtonFromDic(BTN_GUN);
            ToggleFunctionButton(btn, IsTriggerGun);
        }
        else if (controlName == BTN_WALK)
        {
            Button btn = GetButtonFromDic(BTN_WALK);
            ToggleFunctionButton(btn, IsTriggerMove);
        }
        else if (controlName == LEFTBUTTOSTRING)
        {
            SelectPreviousSkin();
        }
        else if (controlName == RIGHTBUTTONSTRING)
        {
            SelectNextSkin();
        }
        else if(controlName == "EquipmentButton")
        {
            if (playerSkinPack == null || GameSkinManager.Instance == null)
            {
                WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "请先选择角色皮肤");
                return;
            }

            GameSkinManager.Instance.SetPlayerSkinPack(playerSkinPack);
            if (Player.LocalPlayer != null)
            {
                Player.LocalPlayer.LoadingPlayerSkip(playerSkinPack);
                Player.LocalPlayer.CmdLoadingPlayerSkip(playerSkinPack.PlayerSkinID);
            }
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "装备成功！");
        }

    }

    // 选择上一个皮肤
    private void SelectPreviousSkin()
    {
        if (_skinButtonNames.Count <= 1) return; // 只有1个或没有，失效

        _currentSelectedIndex--;
        if (_currentSelectedIndex < 0)
        {
            _currentSelectedIndex = _skinButtonNames.Count - 1; // 回到尾部
        }

        string targetBtnName = _skinButtonNames[_currentSelectedIndex];
        ButtonGroupManager.Instance.SelectRadioButtonByName(ButtonGroupName, targetBtnName, true);
    }

    // 选择下一个皮肤
    private void SelectNextSkin()
    {
        if (_skinButtonNames.Count <= 1) return; // 只有1个或没有，失效

        _currentSelectedIndex++;
        if (_currentSelectedIndex >= _skinButtonNames.Count)
        {
            _currentSelectedIndex = 0; // 回到头部
        }

        string targetBtnName = _skinButtonNames[_currentSelectedIndex];
        ButtonGroupManager.Instance.SelectRadioButtonByName(ButtonGroupName, targetBtnName, true);
    }

    // 从字典获取按钮的辅助方法
    private Button GetButtonFromDic(string btnName)
    {
        if (controlDic != null && controlDic.TryGetValue(btnName, out var obj))
        {
            return obj as Button;
        }
        return null;
    }

    // 初始化功能按钮：记录原始状态
    private void InitFunctionButtons()
    {
        _functionButtonStates.Clear();
        _functionButtonOriginalScales.Clear();
        _functionButtonOriginalColors.Clear();

        // 初始化装备按钮
        Button equipBtn = GetButtonFromDic(BTN_EQUIPMENT);
        if (equipBtn != null)
        {
            _functionButtonStates[equipBtn] = false;
            _functionButtonOriginalScales[equipBtn] = equipBtn.transform.localScale;
            var img = equipBtn.GetComponent<Image>();
            if (img != null) _functionButtonOriginalColors[equipBtn] = img.color;
        }

        // 初始化枪械按钮
        Button gunBtn = GetButtonFromDic(BTN_GUN);
        if (gunBtn != null)
        {
            _functionButtonStates[gunBtn] = false;
            _functionButtonOriginalScales[gunBtn] = gunBtn.transform.localScale;
            var img = gunBtn.GetComponent<Image>();
            if (img != null) _functionButtonOriginalColors[gunBtn] = img.color;
        }

        // 初始化行走按钮
        Button walkBtn = GetButtonFromDic(BTN_WALK);
        if (walkBtn != null)
        {
            _functionButtonStates[walkBtn] = false;
            _functionButtonOriginalScales[walkBtn] = walkBtn.transform.localScale;
            var img = walkBtn.GetComponent<Image>();
            if (img != null) _functionButtonOriginalColors[walkBtn] = img.color;
        }
    }

    // 通用的功能按钮切换逻辑
    private void ToggleFunctionButton(Button button, UnityAction<bool> onStateChanged)
    {
        if (button == null) return;
        if (!_functionButtonStates.ContainsKey(button)) return;

        // 切换状态
        bool newState = !_functionButtonStates[button];
        _functionButtonStates[button] = newState;

        // 应用视觉效果
        SetButtonState(newState, button);

        // 触发对应的功能
        onStateChanged?.Invoke(newState);
    }

    // 设置按钮状态：选中/取消
    public void SetButtonState(bool IsSelect, Button button)
    {
        if (button == null) return;

        // 杀掉之前的动画
        button.transform.DOKill();
        Image btnImg = button.GetComponent<Image>();
        if (btnImg != null) btnImg.DOKill();

        if (IsSelect)
        {
            // 选中：放大 + 变浅绿
            button.transform.DOScale(_functionButtonOriginalScales[button] * ButtonSelectScale, ButtonAnimDuration)
                .SetEase(Ease.OutQuad);

            if (btnImg != null)
            {
                btnImg.DOColor(ButtonSelectColor, ButtonAnimDuration)
                    .SetEase(Ease.OutQuad);
            }
        }
        else
        {
            // 取消：还原大小 + 还原颜色
            button.transform.DOScale(_functionButtonOriginalScales[button], ButtonAnimDuration)
                .SetEase(Ease.OutQuad);

            if (btnImg != null && _functionButtonOriginalColors.ContainsKey(button))
            {
                btnImg.DOColor(_functionButtonOriginalColors[button], ButtonAnimDuration)
                    .SetEase(Ease.OutQuad);
            }
        }
    }

    // 清理所有按钮动画
    private void CleanupButtonAnimations()
    {
        Button equipBtn = GetButtonFromDic(BTN_EQUIPMENT);
        if (equipBtn != null)
        {
            equipBtn.transform.DOKill();
            equipBtn.GetComponent<Image>()?.DOKill();
        }

        Button gunBtn = GetButtonFromDic(BTN_GUN);
        if (gunBtn != null)
        {
            gunBtn.transform.DOKill();
            gunBtn.GetComponent<Image>()?.DOKill();
        }

        Button walkBtn = GetButtonFromDic(BTN_WALK);
        if (walkBtn != null)
        {
            walkBtn.transform.DOKill();
            walkBtn.GetComponent<Image>()?.DOKill();
        }
    }

    public void IsShowEquipment(bool IsShow)
    {
        if (ArmorCanvasGroup == null) return;
        ArmorCanvasGroup.blocksRaycasts = IsShow;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ArmorCanvasGroup, ref SequenceArmorCanvasGroup, IsShow, () => { });
    }

    public void IsTriggerGun(bool IsTrigger)
    {
        if (GunSprite == null) return;
        GunSprite.DOKill();
        GunSprite.DOFade(IsTrigger ? 1f : 0f, 1f);
    }

    public void IsTriggerMove(bool IsTrigger)
    {
        if (UIMove == null) return;
        if (!IsTrigger)
            UIMove.StopMove();
        else
            UIMove.StartMove();
    }
    #endregion

    #region 左侧角色皮肤展示相关
    private float DefaultSpace = 40f;
    private float EnterSpace = 1000f;
    private float AnimaDuration = 1f;
    private float DefaultTopTop = 0f;
    private float EnterTopTop = 1000f;

    public void EnterButtonAnima()
{

    _skinButtonNames.Clear();
    _currentSelectedIndex = 0;

    CreatePlayerOwnerSkin();
    LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
    LayoutGroup.spacing = EnterSpace;
    LayoutGroup.padding.top = Mathf.RoundToInt(EnterTopTop);
    PlayLayoutEnterAnimation();
}

private void PlayLayoutEnterAnimation()
{
    DOTween.Kill("SkinPanelEnter");
    Sequence seq = DOTween.Sequence();
    seq.Append(DOTween.To(() => LayoutGroup.spacing, x => LayoutGroup.spacing = x, DefaultSpace, AnimaDuration)
        .SetEase(Ease.OutQuad));
    seq.Join(DOTween.To(() => LayoutGroup.padding.top, x =>
    {
        LayoutGroup.padding.top = (int)x;
        LayoutRebuilder.MarkLayoutForRebuild(Content);
    }, DefaultTopTop, AnimaDuration)
        .SetEase(Ease.OutQuad));
    seq.SetId("SkinPanelEnter");
}

public void ClearAllButton()
{
    foreach (Transform child in Content)
    {
        PoolManage.Instance.PushObj(SkinButtonPrefab, child.gameObject);
    }
    SkinButtonToDataMap.Clear();
    _skinButtonNames.Clear(); 
    _currentSelectedIndex = 0;
}

public void ReturnButtonColor(string Name)
{
    foreach (var kvp in SkinButtonToDataMap)
    {
        if (kvp.Key.gameObject.name == Name)
        {
            Image img = kvp.Key.GetComponent<Image>();
            if (img != null)
            {
                img.DOKill();
                img.DOColor(GetQualityColor(kvp.Value.SkinQuality), 0.5f).SetEase(Ease.OutQuad);
            }
        }
    }
}

public void CreatePlayerOwnerSkin()
{
    if (GameSkinManager.Instance == null || GameSkinManager.Instance.PlayerOwnerSkinPackList == null)
        return;

    if (SkinButtonPrefab == null || Content == null)
        return;

    int index = 1;
    int count = GameSkinManager.Instance.PlayerOwnerSkinPackList.Count;

    GameSkinManager.Instance.PlayerOwnerSkinPackList.ForEach(skinPack =>
    {
        if (skinPack == null)
            return;

        GameObject buttonObj = PoolManage.Instance.GetObj(SkinButtonPrefab);
        buttonObj.transform.SetParent(Content, false);
        string btnName = SkinButtonPrefab.name + index;
        buttonObj.name = btnName;
        index++;

        _skinButtonNames.Add(btnName);

        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        // 初始化按钮颜色（直接设置，不用动画）
        Image btnImage = buttonObj.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = GetQualityColor(skinPack.SkinQuality);
        }

        Image playerShowImage = null;
        foreach (Transform child in buttonObj.transform)
        {
            playerShowImage = child.GetComponent<Image>();
            if (playerShowImage != null)
                break;
        }

        if (playerShowImage != null)
        {
            playerShowImage.sprite = skinPack.IdleSprite;
        }

        if (buttonText != null)
            buttonText.text = skinPack.PlayerSkinName;

        if (button != null)
        {
            SkinButtonToDataMap[button] = skinPack;
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(ButtonGroupName, button, TriggerPlayerSkinButton);
        }
    });

    ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ButtonGroupName);
    UpdateContentHeight(count);
}

private void UpdateContentHeight(int buttonCount)
{
    if (Content == null)
        return;

    float totalHeight = buttonCount * ButtonHeight + LayoutGroup.padding.top + LayoutGroup.padding.bottom + (buttonCount - 1) * LayoutGroup.spacing;
    Content.sizeDelta = new Vector2(Content.sizeDelta.x, totalHeight);
    LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
}

public void TriggerPlayerSkinButton(string ButtonName)
{
    _currentSelectedIndex = _skinButtonNames.IndexOf(ButtonName);
    if (_currentSelectedIndex < 0) _currentSelectedIndex = 0;

    foreach (var kvp in SkinButtonToDataMap)
    {
        if (kvp.Key.gameObject.name == ButtonName)
        {
            playerSkinPack = kvp.Value;
        }
    }
    UpdatePanelInfo();
}

public void UpdatePanelInfo()
{
    SimpleSpritePlayer.Stop(PlayerShowImage);
    if (playerSkinPack != null)
    {
        PlayerShowImage.sprite = playerSkinPack.IdleSprite;
        PlayerSkinText.text = playerSkinPack.PlayerSkinName;
        PlayerSkinDescribe.text = playerSkinPack.PlayerSkinDescription;
        if (playerSkinPack.IsHaveAnima)
        {
            SimpleSpritePlayer.PlayLoop(PlayerShowImage, playerSkinPack.AnimaSpriteList, 0.1f);
        }
        SetQuality(playerSkinPack.SkinQuality);
    }
}
#endregion

#region 面板显隐以及特殊动画
public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
{
    CleanupButtonAnimations();
    base.HideMe(callback, isNeedDefaultAnimator);
    ButtonGroupManager.Instance.DestroyRadioGroup(ButtonGroupName);
    ClearAllButton();
}

public override void ShowMe(bool isNeedDefaultAnimator = true)
{
    base.ShowMe(isNeedDefaultAnimator);
    EnterButtonAnima();
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