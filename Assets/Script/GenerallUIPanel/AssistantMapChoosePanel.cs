using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AssistantMapChoosePanel : BasePanel
{
    [Header("大选项导航")]
    public CanvasGroup BigChooseCanvasGroup;
    private Sequence BigChooseCanvasGroupSequence;

    [Header("小选项")]
    public CanvasGroup ChooseCanvasGroup;
    private Sequence ChooseCanvasGroupSequence;

    [Header("面板启动上抬动画")]
    public float ShowUpMoveY = 0;
    public float DefaultMoveY = -133;

    // ===================== 新增状态变量 =====================
    [Header("运行时状态")]
    [Tooltip("当前选中的地图ID (1或2)，返回不清除")]
    private int _selectedMapIndex = -1;
    private bool _hasConfirmed = false; // 是否已确认

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        // 初始化：显示大选项，隐藏小选项
        IsTriggerPanel(false, ChooseCanvasGroup);
        IsTriggerPanel(true, BigChooseCanvasGroup);
        _selectedMapIndex = -1;
        _hasConfirmed = false;
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

    // ===================== 核心按钮逻辑 =====================
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        // 如果已经确认过，禁止操作
        if (_hasConfirmed) return;

        if (controlName == "Map1")
        {
            HandleMapSelect(1);
        }
        else if (controlName == "Map2")
        {
            HandleMapSelect(2);
        }
        else if (controlName == "ReturnButton")
        {
            HandleReturnToMainPanel();
        }
        else if (controlName == "ChooseButton")
        {
            HandleConfirmSelection();
        }
    }

    /// <summary>
    /// 处理：点击地图1/2
    /// </summary>
    private void HandleMapSelect(int mapId)
    {
        _selectedMapIndex = mapId; // 记录选择，不清除

        // 切换相机预览
        if (MapChooseWall.Instance != null)
        {
            MapChooseWall.Instance.Public_PreviewMap(mapId);
        }

        // 切换UI：关闭大选项，打开小选项
        SwitchToSubPanel();
    }

    /// <summary>
    /// 处理：点击返回（不取消选择）
    /// </summary>
    private void HandleReturnToMainPanel()
    {
        // 【关键】不重置 _selectedMapIndex

        // 切回总览相机
        if (MapChooseWall.Instance != null)
        {
            MapChooseWall.Instance.Public_ReturnToOverview();
        }

        // 切换UI：关闭小选项，打开大选项
        SwitchToMainPanel();
    }

    /// <summary>
    /// 处理：点击确认
    /// </summary>
    private void HandleConfirmSelection()
    {
        if (_selectedMapIndex == -1)
        {
            Debug.LogWarning("还没有选择地图！");
            return;
        }

        _hasConfirmed = true;

        // 调用 MapChooseWall 的确认逻辑
        if (MapChooseWall.Instance != null)
        {
            if (_selectedMapIndex == 1)
                MapChooseWall.Instance.Public_ConfirmMap1();
            else if (_selectedMapIndex == 2)
                MapChooseWall.Instance.Public_ConfirmMap2();
        }

        // UI表现：确认按钮变灰
        // (你可以在这里加一个按钮变灰的逻辑)
        Debug.Log($"已确认选择地图 {_selectedMapIndex}");
    }

    // ===================== UI 切换辅助方法 =====================
    private void SwitchToMainPanel()
    {
        IsTriggerPanel(true, BigChooseCanvasGroup);
        IsTriggerPanel(false, ChooseCanvasGroup);
    }

    private void SwitchToSubPanel()
    {
        IsTriggerPanel(false, BigChooseCanvasGroup);
        IsTriggerPanel(true, ChooseCanvasGroup);
    }

    public void IsTriggerPanel(bool IsTrigger, CanvasGroup Group)
    {
        Group.blocksRaycasts = IsTrigger;
        if (Group == BigChooseCanvasGroup)
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(BigChooseCanvasGroup, ref BigChooseCanvasGroupSequence, IsTrigger, () => { });
        }
        else
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ChooseCanvasGroup, ref ChooseCanvasGroupSequence, IsTrigger, () => { });
        }
    }

    // ===================== 面板显隐 =====================
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        // 隐藏面板时，通知 MapChooseWall 也退出
        if (MapChooseWall.Instance != null)
        {
            MapChooseWall.Instance.ExitMapChooseSystem();
        }
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);

        // 重置状态
        _selectedMapIndex = -1;
        _hasConfirmed = false;

        // 显示大选项
        IsTriggerPanel(true, BigChooseCanvasGroup);
        IsTriggerPanel(false, ChooseCanvasGroup);

        // 启动 MapChooseWall 系统
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