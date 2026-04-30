using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExpressionPanel : BasePanel
{
    [Header("生成布局父对象")]
    public Transform AllExpressionContent;
    public Transform EquipExpressionContent;

    [Header("表情交互预制体")]
    public GameObject ExpressionButton;

    [Header("表情预览框")]
    public RectTransform ExpressionBoxRect;
    public CanvasGroup ExpressionCanvasGroup;
    public Image ExpressionImage;

    // 按钮关联字典
    private Dictionary<GameObject, ExpressionPack> ButtonToInfoPackList = new Dictionary<GameObject, ExpressionPack>();
    private Dictionary<GameObject, ExpressionPack> EquipButtonToInfoPackList = new Dictionary<GameObject, ExpressionPack>();

    // ------------------ 改这里：只用一个按钮组 ------------------
    private const string EXPRESSION_BUTTON_GROUP = "ExpressionButtonGroup";

    private ExpressionPack CurrentSelectExpression; // 当前选中的表情

    // 预览框动画参数（从你的参考代码移植）
    [Header("预览弹出动画")]
    public float showScale = 1.2f;
    public float showTime = 0.4f;
    public Ease showEase = Ease.OutBack;

    private Vector3 _originalScale;
    private Sequence _previewSequence;

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        _originalScale = ExpressionBoxRect.localScale;
    }

    public override void Start()
    {
        base.Start();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        ExpressionCanvasGroup?.DOKill();
        _previewSequence?.Kill();
    }
    #endregion

    #region 核心创建逻辑
    public void CreateAllList()
    {
        ClearAllExpressionButtons();

        if (ExpressionSystem.Instance == null) return;
        var allPlayerExpression = ExpressionSystem.Instance.GetAllPlayerExpression();
        var equipExpressionIds = ExpressionSystem.Instance.EquipmentExpressionList;
        if (allPlayerExpression == null || allPlayerExpression.Count == 0) return;

        var equipIdSet = new HashSet<int>(equipExpressionIds ?? Enumerable.Empty<int>());
        var filteredList = allPlayerExpression.Where(x => !equipIdSet.Contains(x.ExpressionID)).ToList();

        foreach (var expressionPack in filteredList)
        {
            if (expressionPack == null) continue;
            // 共用同一个按钮组
            CreateExpressionButton(expressionPack, AllExpressionContent, ButtonToInfoPackList, EXPRESSION_BUTTON_GROUP, OnAllExpressionButtonClicked);
        }
    }

    public void CreateEquipExpressionButton()
    {
        ClearEquipExpressionButtons();

        if (ExpressionSystem.Instance == null) return;
        var equipExpressionIds = ExpressionSystem.Instance.EquipmentExpressionList;
        if (equipExpressionIds == null || equipExpressionIds.Count == 0) return;

        foreach (var id in equipExpressionIds)
        {
            var pack = ExpressionSystem.Instance.GetExpressionPack(id);
            if (pack == null) continue;
            // 共用同一个按钮组
            CreateExpressionButton(pack, EquipExpressionContent, EquipButtonToInfoPackList, EXPRESSION_BUTTON_GROUP, OnEquipExpressionButtonClicked);
        }
    }

    private void CreateExpressionButton(ExpressionPack pack, Transform parent, Dictionary<GameObject, ExpressionPack> dict, string buttonGroupName, UnityAction<string> clickCallback)
    {
        GameObject buttonObj = PoolManage.Instance.GetObj(ExpressionButton);
        buttonObj.transform.SetParent(parent, false);
        buttonObj.name = ExpressionButton.name + "_" + pack.ExpressionID;

        Image[] allImages = buttonObj.GetComponentsInChildren<Image>();
        Image childImage = null;

        if (allImages.Length > 1)
            childImage = allImages[1];
        else if (allImages.Length == 1)
            childImage = allImages[0];

        if (childImage != null && pack.ExpressionSprite != null)
        {
            childImage.sprite = pack.ExpressionSprite;
            childImage.SetAllDirty();
        }

        dict.Add(buttonObj, pack);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(buttonGroupName, button, clickCallback);
        }
    }
    #endregion

    #region 按钮点击逻辑
    private void OnAllExpressionButtonClicked(string buttonName)
    {
        foreach (var kvp in ButtonToInfoPackList)
        {
            if (kvp.Key.name == buttonName)
            {
                CurrentSelectExpression = kvp.Value;
                ShowExpressionPreview(CurrentSelectExpression);
                UpdateButtonStateBySelect();
                break;
            }
        }
    }

    private void OnEquipExpressionButtonClicked(string buttonName)
    {
        foreach (var kvp in EquipButtonToInfoPackList)
        {
            if (kvp.Key.name == buttonName)
            {
                CurrentSelectExpression = kvp.Value;
                ShowExpressionPreview(CurrentSelectExpression);
                UpdateButtonStateBySelect();
                break;
            }
        }
    }



    /// <summary>
    /// 普通预览：渐变切换图片
    /// </summary>
    private void ShowExpressionPreview(ExpressionPack pack)
    {
        if (pack == null || ExpressionImage == null || ExpressionCanvasGroup == null) return;

        ExpressionCanvasGroup.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(ExpressionCanvasGroup.DOFade(0, 0.15f));
        seq.AppendCallback(() =>
        {
            ExpressionImage.sprite = pack.ExpressionSprite;
            ExpressionImage.SetAllDirty();
        });
        seq.Append(ExpressionCanvasGroup.DOFade(1, 0.15f));
        seq.Play();
    }

    /// <summary>
    /// 点击播放：立刻隐藏 → 弹射弹出（不自动消失）
    /// </summary>
    public void PlayExpressionPopup()
    {
        if (CurrentSelectExpression == null) return;

        _previewSequence?.Kill();
        ExpressionCanvasGroup.alpha = 0;
        ExpressionBoxRect.localScale = Vector3.zero;

        ExpressionImage.sprite = CurrentSelectExpression.ExpressionSprite;

        _previewSequence = DOTween.Sequence();
        _previewSequence.Join(ExpressionCanvasGroup.DOFade(1, showTime));
        _previewSequence.Join(ExpressionBoxRect.DOScale(_originalScale * showScale, showTime).SetEase(showEase));
        _previewSequence.Append(ExpressionBoxRect.DOScale(_originalScale, 0.1f));
        _previewSequence.Play();
    }
    #endregion

    #region 装备 / 移除 逻辑
    private void EquipCurrentExpression()
    {
        if (CurrentSelectExpression == null) return;

        var sys = ExpressionSystem.Instance;
        if (sys == null) return;

        if (!sys.EquipmentExpressionList.Contains(CurrentSelectExpression.ExpressionID))
        {
            sys.EquipmentExpressionList.Add(CurrentSelectExpression.ExpressionID);
            sys.SaveEquipmentList();
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "装备成功！");
        }

        RefreshAllUI();
    }

    private void RemoveCurrentExpression()
    {
        if (CurrentSelectExpression == null) return;

        var sys = ExpressionSystem.Instance;
        if (sys == null) return;

        if (sys.EquipmentExpressionList.Contains(CurrentSelectExpression.ExpressionID))
        {
            sys.EquipmentExpressionList.Remove(CurrentSelectExpression.ExpressionID);
            sys.SaveEquipmentList();
            WarnTriggerManager.Instance.TriggerNoInteractionWarn(1f, "已卸下");
        }

        RefreshAllUI();
    }

    public void RefreshAllUI()
    {
        CurrentSelectExpression = null; // 刷新后清空选中
        UpdateButtonStateBySelect();

        CreateAllList();
        CreateEquipExpressionButton();
    }
    #endregion

    #region 按钮显隐逻辑（核心）
    private void UpdateButtonStateBySelect()
    {
        Button equipBtn = controlDic["EquipButton"] as Button;
        Button removeBtn = controlDic["RemoveButton"] as Button;
        Button playerBtn = controlDic["PlayerButton"] as Button;

        if (CurrentSelectExpression == null)
        {
            equipBtn?.gameObject.SetActive(false);
            removeBtn?.gameObject.SetActive(false);
            playerBtn?.gameObject.SetActive(false);
            return;
        }

        bool isEquipped = ExpressionSystem.Instance.EquipmentExpressionList.Contains(CurrentSelectExpression.ExpressionID);

        if (isEquipped)
        {
            equipBtn?.gameObject.SetActive(false);
            removeBtn?.gameObject.SetActive(true);
            playerBtn?.gameObject.SetActive(true);
        }
        else
        {
            equipBtn?.gameObject.SetActive(true);
            removeBtn?.gameObject.SetActive(false);
            playerBtn?.gameObject.SetActive(false);
        }
    }
    #endregion

    #region 清理逻辑
    public void ClearAllExpressionButtons()
    {
        foreach (var item in ButtonToInfoPackList.Keys)
        {
            if (item != null)
                PoolManage.Instance.PushObj(ExpressionButton, item);
        }
        ButtonToInfoPackList.Clear();
    }

    public void ClearEquipExpressionButtons()
    {
        foreach (var item in EquipButtonToInfoPackList.Keys)
        {
            if (item != null)
                PoolManage.Instance.PushObj(ExpressionButton, item);
        }
        EquipButtonToInfoPackList.Clear();
    }
    #endregion

    #region UI控件总触发
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (controlName == "EquipButton")
        {
            EquipCurrentExpression();
        }
        else if (controlName == "PlayerButton")
        {
            PlayExpressionPopup();
        }
        else if (controlName == "RemoveButton")
        {
            RemoveCurrentExpression();
        }
    }
    #endregion

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        ExpressionCanvasGroup?.DOKill();
        _previewSequence?.Kill();

        // 销毁唯一的按钮组
        ButtonGroupManager.Instance.DestroyRadioGroup(EXPRESSION_BUTTON_GROUP);

        ClearAllExpressionButtons();
        ClearEquipExpressionButtons();
        CurrentSelectExpression = null;

        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        CreateAllList();
        CreateEquipExpressionButton();
        UpdateButtonStateBySelect();
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