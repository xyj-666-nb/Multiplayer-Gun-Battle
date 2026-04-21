using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AssistantMapChoosePanel : BasePanel
{
    private const string UiSelectSound = "Music/update415/ui选择";
    private const string UiBackSound = "Music/update415/ui返回";
    private const string MapSelectSound = "Music/正式/交互/选地图";

    [Header("大选项导航")]
    public CanvasGroup BigChooseCanvasGroup;
    private Sequence BigChooseCanvasGroupSequence;

    [Header("小选项")]
    public CanvasGroup ChooseCanvasGroup;
    private Sequence ChooseCanvasGroupSequence;

    [Header("面板启动上抬动画")]
    public float ShowUpMoveY = 0;
    public float DefaultMoveY = -133;

    [Header("运行时状态")]
    [Tooltip("当前选中的地图ID (1或2)，返回不清除")]
    private int _selectedMapIndex = -1;
    private int _preparedMapIndex = -1;
    private bool _hasConfirmed = false;
    private TextMeshProUGUI _chooseButtonText;

    #region 生命周期
    public override void Awake()
    {
        base.Awake();

        if (controlDic.ContainsKey("ChooseButton") && controlDic["ChooseButton"] != null)
        {
            Button chooseButton = controlDic["ChooseButton"] as Button;
            _chooseButtonText = chooseButton != null
                ? chooseButton.GetComponentInChildren<TextMeshProUGUI>(true)
                : controlDic["ChooseButton"].GetComponentInChildren<TextMeshProUGUI>(true);
        }

        IsTriggerPanel(false, ChooseCanvasGroup);
        IsTriggerPanel(true, BigChooseCanvasGroup);
        _selectedMapIndex = -1;
        _preparedMapIndex = -1;
        _hasConfirmed = false;
        RefreshChooseButtonState();
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();
    }
    #endregion

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (_hasConfirmed && controlName != "ReturnButton") return;

        if (controlName == "Map1")
        {
            MusicManager.Instance?.PlayEffect(MapSelectSound);
            HandleMapSelect(1);
        }
        else if (controlName == "Map2")
        {
            MusicManager.Instance?.PlayEffect(MapSelectSound);
            HandleMapSelect(2);
        }
        else if (controlName == "ReturnButton")
        {
            MusicManager.Instance?.PlayEffect(UiBackSound);
            HandleReturnToMainPanel();
        }
        else if (controlName == "ChooseButton")
        {
            MusicManager.Instance?.PlayEffect(UiSelectSound);
            HandleConfirmSelection();
        }
    }

    private void HandleMapSelect(int mapId)
    {
        _selectedMapIndex = mapId;

        if (MapChooseWall.Instance != null)
        {
            MapChooseWall.Instance.Public_PreviewMap(mapId);
        }

        SwitchToSubPanel();
        RefreshChooseButtonState();
    }

    private void HandleReturnToMainPanel()
    {
        _hasConfirmed = false;

        if (MapChooseWall.Instance != null)
        {
            MapChooseWall.Instance.Public_ReturnToOverview();
        }

        SwitchToMainPanel();
    }

    private void HandleConfirmSelection()
    {
        if (_selectedMapIndex == -1)
        {
            Debug.LogWarning("还没有选择地图！");
            return;
        }

        _hasConfirmed = true;
        _preparedMapIndex = _selectedMapIndex;
        RefreshChooseButtonState();

        if (MapChooseWall.Instance != null)
        {
            if (_selectedMapIndex == 1)
                MapChooseWall.Instance.Public_ConfirmMap1();
            else if (_selectedMapIndex == 2)
                MapChooseWall.Instance.Public_ConfirmMap2();
        }

        Debug.Log($"已确认选择地图 {_selectedMapIndex}");
    }

    private void SwitchToMainPanel()
    {
        IsTriggerPanel(true, BigChooseCanvasGroup);
        IsTriggerPanel(false, ChooseCanvasGroup);
    }

    private void SwitchToSubPanel()
    {
        IsTriggerPanel(false, BigChooseCanvasGroup);
        IsTriggerPanel(true, ChooseCanvasGroup);
        RefreshChooseButtonState();
    }

    private void RefreshChooseButtonState()
    {
        if (_chooseButtonText == null)
            return;

        bool isPreparedCurrentMap = _selectedMapIndex != -1 && _selectedMapIndex == _preparedMapIndex;
        _chooseButtonText.text = isPreparedCurrentMap ? "已准备" : "选择";
    }

    public void IsTriggerPanel(bool IsTrigger, CanvasGroup Group)
    {
        if (Group == null)
            return;

        Group.blocksRaycasts = IsTrigger;
        Group.interactable = IsTrigger;
        if (Group == BigChooseCanvasGroup)
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(BigChooseCanvasGroup, ref BigChooseCanvasGroupSequence, IsTrigger, () => { });
        }
        else
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ChooseCanvasGroup, ref ChooseCanvasGroupSequence, IsTrigger, () => { });
        }
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);

        _selectedMapIndex = -1;
        _preparedMapIndex = -1;
        _hasConfirmed = false;

        IsTriggerPanel(true, BigChooseCanvasGroup);
        IsTriggerPanel(false, ChooseCanvasGroup);
        RefreshChooseButtonState();

        if (MapChooseWall.Instance != null)
        {
            MapChooseWall.Instance.Public_EnterSystem();
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
}