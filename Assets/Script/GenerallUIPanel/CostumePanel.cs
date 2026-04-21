using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CostumePanel : BasePanel
{
    [Header("子面板填充区域")]
    public RectTransform PanelArea;

    private string _currentActiveTab = "";

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (controlName == "PlayerSkipButton")
        {
            if (_currentActiveTab == "PlayerSkipButton") return;

            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            HideAllPanel();

            // 显示玩家面板并填充
            UImanager.Instance.ShowPanel<PlayerSkipPanel>();
            BasePanel playerPanel = UImanager.Instance.GetPanel<PlayerSkipPanel>();
            FillParentPanel(playerPanel.GetComponent<RectTransform>());

            // 更新当前状态
            _currentActiveTab = "PlayerSkipButton";
        }
        else if (controlName == "GunSkipButton")
        {

            if (_currentActiveTab == "GunSkipButton") return;

            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            HideAllPanel();

            // 显示枪械面板并填充
            UImanager.Instance.ShowPanel<GunSkipPanel>();
            BasePanel gunPanel = UImanager.Instance.GetPanel<GunSkipPanel>();
            FillParentPanel(gunPanel.GetComponent<RectTransform>());

            // 更新当前状态
            _currentActiveTab = "GunSkipButton";
        }
        else if (controlName == "ExpressionButton")
        {
            if (_currentActiveTab == "ExpressionButton") return;

            // UI选择音效
            MusicManager.Instance.PlayEffect("Music/update415/ui选择");
            //打开表情面板
            HideAllPanel();
            UImanager.Instance.ShowPanel<ExpressionPanel>();
            BasePanel expressionPanel = UImanager.Instance.GetPanel<ExpressionPanel>();
            FillParentPanel(expressionPanel.GetComponent<RectTransform>());

            // 更新当前状态
            _currentActiveTab = "ExpressionButton";
        }
        else if (controlName == "ReturnButton")
        {
            // UI返回音效
            MusicManager.Instance.PlayEffect("Music/update415/ui返回");
            UImanager.Instance.HidePanel<CostumePanel>();//关闭面板
        }
    }

    public void HideAllPanel()
    {
        UImanager.Instance.HidePanel<PlayerSkipPanel>();
        UImanager.Instance.HidePanel<GunSkipPanel>();
        UImanager.Instance.HidePanel<ExpressionPanel>();
    }
    #endregion

    /// <summary>
    /// 将子面板自动填充到父物体(全屏拉伸)
    /// </summary>
    private void FillParentPanel(RectTransform targetRect)
    {
        if (targetRect == null || PanelArea == null) return;

        // 设置父物体
        targetRect.SetParent(PanelArea);

        //设置锚点为全屏拉伸
        targetRect.anchorMin = Vector2.zero;
        targetRect.anchorMax = Vector2.one;

        // 清空偏移量，完全贴合父物体
        targetRect.offsetMin = Vector2.zero;
        targetRect.offsetMax = Vector2.zero;

        // 重置轴心、位置、缩放
        targetRect.pivot = new Vector2(0.5f, 0.5f);
        targetRect.anchoredPosition = Vector2.zero;
        targetRect.localScale = Vector3.one;
    }

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        //注册按钮组
        ButtonGroupManager.Instance.AddRadioButtonToGroup("CostumePanel", controlDic["PlayerSkipButton"] as Button);
        ButtonGroupManager.Instance.AddRadioButtonToGroup("CostumePanel", controlDic["GunSkipButton"] as Button);
        ButtonGroupManager.Instance.AddRadioButtonToGroup("CostumePanel", controlDic["ExpressionButton"] as Button);
        ButtonGroupManager.Instance.ManualSelectToggleButton("CostumePanel");

        // 显示玩家面板并填充
        UImanager.Instance.ShowPanel<PlayerSkipPanel>();
        BasePanel playerPanel = UImanager.Instance.GetPanel<PlayerSkipPanel>();
        FillParentPanel(playerPanel.GetComponent<RectTransform>());

        _currentActiveTab = "PlayerSkipButton";
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

    #region 面板显隐以及特殊动画
    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        HideAllPanel();
        ButtonGroupManager.Instance.DestroyRadioGroup("CostumePanel");

        _currentActiveTab = "";
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }
    #endregion
}