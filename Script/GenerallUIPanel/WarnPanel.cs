using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

#region 面板类型枚举
public enum WarnPanelType
{
    NoInteraction,//无交互不阻断警告
    SingleInteraction,//阻断式单交互警告
    DoubleInteraction,//阻断式双交互警告
    DoubleInteraction2,//阻断式双交互警告,无Content版 
    DealInteraction,//无阻断式协议交互警告
}
#endregion

public class WarnPanel : BasePanel
{
    #region 面板组以及动画序列组
    //4个UI面板组 → 新增DoubleInteractionGroup2
    [Header("面板组")]
    public CanvasGroup NoInteractionGroup;
    public CanvasGroup SingleInteractionGroup;
    public CanvasGroup DoubleInteractionGroup;
    public CanvasGroup DealInteractionGroup;
    public CanvasGroup DoubleInteractionGroup2;

    [Header("动画组")]
    private Sequence NoInteraction_Sequence;
    private Sequence SingleInteraction_Sequence;
    private Sequence DoubleInteraction_Sequence;
    private Sequence DealInteraction_Sequence;
    private Sequence DoubleInteraction_Sequence2;

    private readonly System.Collections.Generic.Dictionary<WarnPanelType, bool> _isInAnimatorDict = new()
    {
        { WarnPanelType.NoInteraction, false },
        { WarnPanelType.SingleInteraction, false },
        { WarnPanelType.DoubleInteraction, false },
        { WarnPanelType.DoubleInteraction2, false }, 
        { WarnPanelType.DealInteraction, false }
    };

    private WarnPanelType CurrentShowWarnPanelType;//当前的显示面板类型
    private UnityAction ConfirmCallback;
    private UnityAction CancelCallback;
    #endregion

    #region 初始化与通用工具方法
    /// <summary>
    /// 批量设置所有面板组的激活状态 → 新增DoubleInteractionGroup2
    /// </summary>
    /// <param name="isActive">是否激活</param>
    private void SetAllGroupsActive(bool isActive)
    {
        if (NoInteractionGroup != null) 
            NoInteractionGroup.gameObject.SetActive(isActive);
        if (SingleInteractionGroup != null) 
            SingleInteractionGroup.gameObject.SetActive(isActive);
        if (DoubleInteractionGroup != null)
            DoubleInteractionGroup.gameObject.SetActive(isActive);
        if (DealInteractionGroup != null) 
            DealInteractionGroup.gameObject.SetActive(isActive);
        if (DoubleInteractionGroup2 != null) 
            DoubleInteractionGroup2.gameObject.SetActive(isActive);
    }

    /// <summary>
    /// 检查面板组是否有效
    /// </summary>
    /// <param name="group">目标面板组</param>
    /// <param name="groupName">面板组名称（用于日志）</param>
    /// <returns>是否有效</returns>
    private bool CheckGroupValid(CanvasGroup group, string groupName)
    {
        if (group == null)
        {
            Debug.LogError($"{groupName} 为空！");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 保证同一时间仅显示一个阻断式交互面板
    /// </summary>
    private void HideConflictPanels()
    {
        // 先停止所有冲突面板的动画
        SingleInteraction_Sequence?.Kill();
        DoubleInteraction_Sequence?.Kill();
        DealInteraction_Sequence?.Kill();
        DoubleInteraction_Sequence2?.Kill();

        // 隐藏所有冲突面板
        if (SingleInteractionGroup != null && SingleInteractionGroup.gameObject.activeSelf)
        {
            SetActive_SingleInteractionGroup(false, null, false);
        }
        if (DoubleInteractionGroup != null && DoubleInteractionGroup.gameObject.activeSelf)
        {
            SetActive_DoubleInteractionGroup(false, null, false);
        }
        if (DealInteractionGroup != null && DealInteractionGroup.gameObject.activeSelf)
        {
            SetActive_DealInteractionGroup(false, null, false);
        }
        if (DoubleInteractionGroup2 != null && DoubleInteractionGroup2.gameObject.activeSelf)
        {
            SetActive_DoubleInteractionGroup2(false, null, false);
        }

        // 清空所有冲突面板的回调
        ConfirmCallback = null;
        CancelCallback = null;
    }
    #endregion

    #region UI组动画方法

    #region 通用动画完成方法
    /// <summary>
    /// 动画完成通用回调
    /// </summary>
    private void AnimaComplete_Common(WarnPanelType panelType)
    {
        if (_isInAnimatorDict.ContainsKey(panelType))
        {
            _isInAnimatorDict[panelType] = false;
        }
    }
    #endregion

    #region 无交互警告面板
    public void SetActive_NoInteractionGroup(bool IsActive, UnityAction Callback = null, bool UseDefaultAnimator = true)
    {
        if (!CheckGroupValid(NoInteractionGroup, "NoInteractionGroup"))
        {
            AnimaComplete_Common(WarnPanelType.NoInteraction);
            Callback?.Invoke();
            return;
        }

        _isInAnimatorDict[WarnPanelType.NoInteraction] = true;
        if (UseDefaultAnimator)
        {
            DefaultAnima_NoInteraction(IsActive, () => {
                AnimaComplete_Common(WarnPanelType.NoInteraction);
                Callback?.Invoke(); // 触发自定义回调
            });
        }
        else
        {
            SpecialAnima_NoInteraction(IsActive, () => {
                AnimaComplete_Common(WarnPanelType.NoInteraction);
                Callback?.Invoke(); // 触发自定义回调
            });
        }
    }

    private void DefaultAnima_NoInteraction(bool IsShow, UnityAction CallBack)
    {
        RectTransform rt = NoInteractionGroup.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("NoInteractionGroup 未找到 RectTransform 组件！");
            CallBack?.Invoke();
            return;
        }

        Vector2 originalPos = rt.anchoredPosition;
        NoInteraction_Sequence?.Kill();
        NoInteraction_Sequence = DOTween.Sequence();

        if (IsShow)
        {
            // 显示时先激活面板
            NoInteractionGroup.gameObject.SetActive(true);
            NoInteractionGroup.alpha = 0;

            // 淡入
            SimpleAnimatorTool.Instance.StartFloatLerp(0, 1, 0.3f,
                (v) => { NoInteractionGroup.alpha = v; },
                () => { NoInteractionGroup.alpha = 1; });

            rt.anchoredPosition = new Vector2(200, -100);

            Debug.Log($"显示动画：起始位置={rt.anchoredPosition}，目标位置={originalPos}");

            // 位置动画
            NoInteraction_Sequence.Append(
                rt.DOAnchorPos(originalPos, 1f)
                .SetEase(Ease.OutQuad)
            )
            .OnComplete(() => {
                Debug.Log("显示动画完成，当前位置=" + rt.anchoredPosition);
                CallBack?.Invoke();
            });
        }
        else
        {
            if (!NoInteractionGroup.gameObject.activeSelf)
            {
                NoInteractionGroup.gameObject.SetActive(true);
            }

            rt.anchoredPosition = originalPos;

            // 打印日志验证位置
            Debug.Log($"隐藏动画：起始位置={rt.anchoredPosition}，目标位置=(200, -100)");

            // 位置动画 + 淡出同步
            NoInteraction_Sequence.Append(
                rt.DOAnchorPos(new Vector2(200, -100), 0.5f)
                .SetEase(Ease.InQuad)
            );

            NoInteraction_Sequence.Join(
                DOTween.To(() => NoInteractionGroup.alpha,
                           v => NoInteractionGroup.alpha = v,
                           0,
                           0.3f)
                .SetEase(Ease.Linear)
            );

            NoInteraction_Sequence.OnComplete(() =>
            {
                NoInteractionGroup.alpha = 0;
                rt.anchoredPosition = originalPos;

                // 隐藏完成后失活面板
                NoInteractionGroup.gameObject.SetActive(false);

                Debug.Log("隐藏动画完成，位置已重置为=" + originalPos);
                CallBack?.Invoke();
            });
        }
    }

    private void SpecialAnima_NoInteraction(bool IsShow, UnityAction CallBack)
    {
        if (IsShow)
        {
            NoInteractionGroup.gameObject.SetActive(true);
            NoInteractionGroup.alpha = 1;
        }
        else
        {
            NoInteractionGroup.alpha = 0;
            NoInteractionGroup.gameObject.SetActive(false);
        }
        CallBack?.Invoke();
    }
    #endregion

    #region 其余面板
    public void SetActive_SingleInteractionGroup(bool IsActive, UnityAction Callback = null, bool UseDefaultAnimator = true)
    {
        if (!CheckGroupValid(SingleInteractionGroup, "SingleInteractionGroup"))
        {
            AnimaComplete_Common(WarnPanelType.SingleInteraction);
            Callback?.Invoke();
            return;
        }

        _isInAnimatorDict[WarnPanelType.SingleInteraction] = true;
        if (UseDefaultAnimator)
        {
            // 显示时提前激活面板
            if (IsActive && !SingleInteractionGroup.gameObject.activeSelf)
            {
                SingleInteractionGroup.gameObject.SetActive(true);
            }

            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(SingleInteractionGroup, ref SingleInteraction_Sequence, IsActive,
                  () => {
                      AnimaComplete_Common(WarnPanelType.SingleInteraction);
                      SingleInteractionGroup.interactable = true;
                      // 隐藏完成后失活面板
                      if (!IsActive)
                      {
                          SingleInteractionGroup.gameObject.SetActive(false);
                      }
                      Callback?.Invoke(); // 触发自定义回调
                  });//结束后打开交互
        }
        else
        {
            SpecialAnima_SingleInteraction(IsActive, () => {
                AnimaComplete_Common(WarnPanelType.SingleInteraction);
                SingleInteractionGroup.interactable = true;
                // 隐藏完成后失活面板
                if (!IsActive)
                {
                    SingleInteractionGroup.gameObject.SetActive(false);
                }
                Callback?.Invoke(); // 触发自定义回调
            });
        }
    }

    public void SetActive_DoubleInteractionGroup(bool IsActive, UnityAction Callback = null, bool UseDefaultAnimator = true)
    {
        if (!CheckGroupValid(DoubleInteractionGroup, "DoubleInteractionGroup"))
        {
            AnimaComplete_Common(WarnPanelType.DoubleInteraction);
            Callback?.Invoke();
            return;
        }

        _isInAnimatorDict[WarnPanelType.DoubleInteraction] = true;
        if (UseDefaultAnimator)
        {
            // 显示时提前激活面板
            if (IsActive && !DoubleInteractionGroup.gameObject.activeSelf)
            {
                DoubleInteractionGroup.gameObject.SetActive(true);
            }

            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(DoubleInteractionGroup, ref DoubleInteraction_Sequence, IsActive,
                () => {
                    AnimaComplete_Common(WarnPanelType.DoubleInteraction);
                    DoubleInteractionGroup.interactable = true;
                    // 隐藏完成后失活面板
                    if (!IsActive)
                    {
                        DoubleInteractionGroup.gameObject.SetActive(false);
                    }
                    Callback?.Invoke(); // 触发自定义回调
                });
        }
        else
        {
            SpecialAnima_DoubleInteraction(IsActive, () => {
                AnimaComplete_Common(WarnPanelType.DoubleInteraction);
                DoubleInteractionGroup.interactable = true;
                // 隐藏完成后失活面板
                if (!IsActive)
                {
                    DoubleInteractionGroup.gameObject.SetActive(false);
                }
                Callback?.Invoke(); // 触发自定义回调
            });
        }
    }


    public void SetActive_DoubleInteractionGroup2(bool IsActive, UnityAction Callback = null, bool UseDefaultAnimator = true)
    {
        if (!CheckGroupValid(DoubleInteractionGroup2, "DoubleInteractionGroup2"))
        {
            AnimaComplete_Common(WarnPanelType.DoubleInteraction2);
            Callback?.Invoke();
            return;
        }

        _isInAnimatorDict[WarnPanelType.DoubleInteraction2] = true;
        if (UseDefaultAnimator)
        {
            // 显示时提前激活面板
            if (IsActive && !DoubleInteractionGroup2.gameObject.activeSelf)
            {
                DoubleInteractionGroup2.gameObject.SetActive(true);
            }

            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(DoubleInteractionGroup2, ref DoubleInteraction_Sequence2, IsActive,
                () => {
                    AnimaComplete_Common(WarnPanelType.DoubleInteraction2);
                    DoubleInteractionGroup2.interactable = true;
                    // 隐藏完成后失活面板
                    if (!IsActive)
                    {
                        DoubleInteractionGroup2.gameObject.SetActive(false);
                    }
                    Callback?.Invoke(); // 触发自定义回调
                });
        }
        else
        {
            SpecialAnima_DoubleInteractionGroup2(IsActive, () => {
                AnimaComplete_Common(WarnPanelType.DoubleInteraction2);
                DoubleInteractionGroup2.interactable = true;
                // 隐藏完成后失活面板
                if (!IsActive)
                {
                    DoubleInteractionGroup2.gameObject.SetActive(false);
                }
                Callback?.Invoke(); // 触发自定义回调
            });
        }
    }

    public void SetActive_DealInteractionGroup(bool IsActive, UnityAction Callback = null, bool UseDefaultAnimator = true)
    {
        if (!CheckGroupValid(DealInteractionGroup, "DealInteractionGroup"))
        {
            AnimaComplete_Common(WarnPanelType.DealInteraction);
            Callback?.Invoke();
            return;
        }

        _isInAnimatorDict[WarnPanelType.DealInteraction] = true;
        if (UseDefaultAnimator)
        {
            // 显示时提前激活面板
            if (IsActive && !DealInteractionGroup.gameObject.activeSelf)
            {
                DealInteractionGroup.gameObject.SetActive(true);
            }

            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(DealInteractionGroup, ref DealInteraction_Sequence, IsActive,
                () => {
                    AnimaComplete_Common(WarnPanelType.DealInteraction);
                    DealInteractionGroup.interactable = true;
                    // 隐藏完成后失活面板
                    if (!IsActive)
                    {
                        DealInteractionGroup.gameObject.SetActive(false);
                    }
                    Callback?.Invoke(); // 触发自定义回调
                });
        }
        else
        {
            SpecialAnima_DealInteraction(IsActive, () => {
                AnimaComplete_Common(WarnPanelType.DealInteraction);
                DealInteractionGroup.interactable = true;
                // 隐藏完成后失活面板
                if (!IsActive)
                {
                    DealInteractionGroup.gameObject.SetActive(false);
                }
                Callback?.Invoke(); // 触发自定义回调
            });
        }
    }
    #endregion

    #region UI组特殊动画实现
    private void SpecialAnima_SingleInteraction(bool IsShow, UnityAction CallBack)
    {
        if (IsShow)
        {
            SingleInteractionGroup.gameObject.SetActive(true);
            SingleInteractionGroup.alpha = 1;
        }
        else
        {
            SingleInteractionGroup.alpha = 0;
            SingleInteractionGroup.gameObject.SetActive(false);
        }
        CallBack?.Invoke();
    }

    private void SpecialAnima_DoubleInteraction(bool IsShow, UnityAction CallBack)
    {
        if (IsShow)
        {
            DoubleInteractionGroup.gameObject.SetActive(true);
            DoubleInteractionGroup.alpha = 1;
        }
        else
        {
            DoubleInteractionGroup.alpha = 0;
            DoubleInteractionGroup.gameObject.SetActive(false);
        }
        CallBack?.Invoke();
    }

    private void SpecialAnima_DoubleInteractionGroup2(bool IsShow, UnityAction CallBack)
    {
        if (IsShow)
        {
            DoubleInteractionGroup2.gameObject.SetActive(true);
            DoubleInteractionGroup2.alpha = 1;
        }
        else
        {
            DoubleInteractionGroup2.alpha = 0;
            DoubleInteractionGroup2.gameObject.SetActive(false);
        }
        CallBack?.Invoke();
    }

    private void SpecialAnima_DealInteraction(bool IsShow, UnityAction CallBack)
    {
        if (IsShow)
        {
            DealInteractionGroup.gameObject.SetActive(true);
            DealInteractionGroup.alpha = 1;
        }
        else
        {
            DealInteractionGroup.alpha = 0;
            DealInteractionGroup.gameObject.SetActive(false);
        }
        CallBack?.Invoke();
    }
    #endregion

    #endregion

    #region 开启一个警告事件

    /// <summary>
    /// 播放无交互警告面板
    /// </summary>
    /// <param name="Duration">显示时长（秒）</param>
    /// <param name="Content">警告内容</param>
    /// <param name="callback">面板隐藏后的自定义回调</param>
    /// <param name="IsUseDefaultAnima">是否使用默认动画</param>
    public void StartWarnPanel_NoInteraction(float Duration, string Content, UnityAction callback = null, bool IsUseDefaultAnima = true)
    {
        CurrentShowWarnPanelType = WarnPanelType.NoInteraction;

        if (!CheckGroupValid(NoInteractionGroup, "NoInteractionGroup"))
        {
            callback?.Invoke();
            return;
        }

        TextMeshProUGUI contentText = NoInteractionGroup.GetComponentInChildren<TextMeshProUGUI>(true);
        if (contentText != null)
        {
            contentText.text = Content;
        }
        else
        {
            Debug.LogError("NoInteractionGroup 下未找到 TextMeshProUGUI 组件！");
            callback?.Invoke();
            return;
        }

        SetActive_NoInteractionGroup(true,
            () => {
                CountDownManager.Instance.CreateTimer(
                    true,
                    (int)(Duration * 1000),
                    () => {
                        // 倒计时结束隐藏面板，隐藏完成后触发回调+关闭总面板
                        SetActive_NoInteractionGroup(false, () => {
                            callback?.Invoke();
                        }, IsUseDefaultAnima);
                    }
                );
            },
            IsUseDefaultAnima);
    }

    /// <summary>
    /// 播放单交互警告面板 → 新增冲突面板隐藏逻辑
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="Content">警告内容</param>
    /// <param name="ConfirmCallback">确认按钮回调</param>
    /// <param name="IsUseDefaultAnima">是否使用默认动画</param>
    public void StartWarnPanel_SingleInteractionGroup(string topic, string Content, UnityAction ConfirmCallback = null, bool IsUseDefaultAnima = false)
    {
        HideConflictPanels();

        CurrentShowWarnPanelType = WarnPanelType.SingleInteraction;
        this.ConfirmCallback = ConfirmCallback;

        if (!CheckGroupValid(SingleInteractionGroup, "SingleInteractionGroup"))
        {
            return;
        }

        // 赋值标题
        TextMeshProUGUI topicText = SingleInteractionGroup.transform.Find("TopicText")?.GetComponent<TextMeshProUGUI>();
        if (topicText != null)
        {
            topicText.text = topic;
        }
        else
        {
            Debug.LogError("SingleInteractionGroup 下未找到 TopicText 组件！");
            return;
        }

        // 赋值内容
        TextMeshProUGUI contentText = SingleInteractionGroup.transform.Find("WarnContent")?.GetComponent<TextMeshProUGUI>();
        if (contentText != null)
        {
            contentText.text = Content;
        }
        else
        {
            Debug.LogError("SingleInteractionGroup 下未找到 WarnContent 组件！");
            return;
        }

        SetActive_SingleInteractionGroup(true, null, IsUseDefaultAnima);
    }

    /// <summary>
    /// 播放双交互警告面板 → 新增冲突面板隐藏逻辑
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="Content">警告内容</param>
    /// <param name="CancelCallback">取消按钮回调</param>
    /// <param name="ConfirmCallback">确认按钮回调</param>
    /// <param name="IsUseDefaultAnima">是否使用默认动画</param>
    public void StartWarnPanel_DoubleInteractionGroup(string topic, string Content, UnityAction CancelCallback, UnityAction ConfirmCallback, bool IsUseDefaultAnima = false)
    {
        HideConflictPanels();

        CurrentShowWarnPanelType = WarnPanelType.DoubleInteraction;
        this.ConfirmCallback = ConfirmCallback;
        this.CancelCallback = CancelCallback;

        if (!CheckGroupValid(DoubleInteractionGroup, "DoubleInteractionGroup"))
        {
            return;
        }

        TextMeshProUGUI topicText = DoubleInteractionGroup.transform.Find("TopicText")?.GetComponent<TextMeshProUGUI>();
        if (topicText != null)
        {
            topicText.text = topic;
        }
        else
        {
            Debug.LogError("DoubleInteractionGroup 下未找到 TopicText 组件！");
            return;
        }

        TextMeshProUGUI contentText = DoubleInteractionGroup.transform.Find("WarnContent")?.GetComponent<TextMeshProUGUI>();
        if (contentText != null)
        {
            contentText.text = Content;
        }
        else
        {
            Debug.LogError("DoubleInteractionGroup 下未找到 WarnContent 组件！");
            return;
        }

        SetActive_DoubleInteractionGroup(true, null, IsUseDefaultAnima);
    }

    /// <summary>
    /// 逻辑与DoubleInteraction一致，仅无需赋值Content
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="CancelCallback">取消按钮回调</param>
    /// <param name="ConfirmCallback">确认按钮回调</param>
    /// <param name="IsUseDefaultAnima">是否使用默认动画</param>
    public void StartWarnPanel_DoubleInteractionGroup2(string topic, UnityAction CancelCallback, UnityAction ConfirmCallback, bool IsUseDefaultAnima = false)
    {
        // 显示前先隐藏冲突面板（保证互斥）
        HideConflictPanels();

        CurrentShowWarnPanelType = WarnPanelType.DoubleInteraction2;
        this.ConfirmCallback = ConfirmCallback;
        this.CancelCallback = CancelCallback;

        if (!CheckGroupValid(DoubleInteractionGroup2, "DoubleInteractionGroup2"))
        {
            return;
        }

        // 仅赋值标题（无Content）
        TextMeshProUGUI topicText = DoubleInteractionGroup2.transform.Find("TopicText")?.GetComponent<TextMeshProUGUI>();
        if (topicText != null)
        {
            topicText.text = topic;
        }
        else
        {
            Debug.LogError("DoubleInteractionGroup2 下未找到 TopicText 组件！");
            return;
        }

        SetActive_DoubleInteractionGroup2(true, null, IsUseDefaultAnima);
    }

    /// <summary>
    /// 播放协议交互警告面板 → 新增冲突面板隐藏逻辑
    /// </summary>
    /// <param name="topic">警告标题</param>
    /// <param name="Content">警告内容</param>
    /// <param name="CancelCallback">取消按钮回调</param>
    /// <param name="ConfirmCallback">确认按钮回调</param>
    /// <param name="IsUseDefaultAnima">是否使用默认动画</param>
    public void StartWarnPanel_DealInteractionGroup(string topic, string Content, UnityAction CancelCallback, UnityAction ConfirmCallback, bool IsUseDefaultAnima = false)
    {
        HideConflictPanels();

        CurrentShowWarnPanelType = WarnPanelType.DealInteraction;
        this.ConfirmCallback = ConfirmCallback;
        this.CancelCallback = CancelCallback;

        if (!CheckGroupValid(DealInteractionGroup, "DealInteractionGroup"))
        {
            return;
        }

        // 赋值标题
        TextMeshProUGUI topicText = DealInteractionGroup.transform.Find("TopicText")?.GetComponent<TextMeshProUGUI>();
        if (topicText != null)
        {
            topicText.text = topic;
        }
        else
        {
            Debug.LogError("DealInteractionGroup 下未找到 TopicText 组件！");
            return;
        }

        // 赋值内容
        TextMeshProUGUI contentText = DealInteractionGroup.transform.Find("WarnContent")?.GetComponent<TextMeshProUGUI>();
        if (contentText != null)
        {
            contentText.text = Content;
        }
        else
        {
            Debug.LogError("DealInteractionGroup 下未找到 WarnContent 组件！");
            return;
        }

        SetActive_DealInteractionGroup(true, null, IsUseDefaultAnima);
    }

    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        // 初始化所有面板为未激活状态
        SetAllGroupsActive(false);
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
        // 批量销毁序列 → 新增DoubleInteraction_Sequence2
        NoInteraction_Sequence?.Kill();
        SingleInteraction_Sequence?.Kill();
        DoubleInteraction_Sequence?.Kill();
        DealInteraction_Sequence?.Kill();
        DoubleInteraction_Sequence2?.Kill(); // 【新增】

        // 清空回调，避免内存泄漏
        ConfirmCallback = null;
        CancelCallback = null;
    }
    #endregion

    #region 按钮点击响应
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        // 根据按钮名称执行对应逻辑 → 新增DoubleInteraction2的按钮case
        switch (controlName)
        {
            case "ConfirmButton_SingleInteraction":
                HandleSingleInteractionConfirm();
                break;
            case "ConfirmButton_DoubleInteraction":
                HandleDoubleInteractionConfirm();
                break;
            case "CancelButton_DoubleInteraction":
                HandleDoubleInteractionCancel();
                break;
            case "ConfirmButton_DoubleInteraction2":
                HandleDoubleInteraction2Confirm();
                break;
            case "CancelButton_DoubleInteraction2":
                HandleDoubleInteraction2Cancel();
                break;
            case "ConfirmButton_DealInteraction":
                HandleDealInteractionConfirm();
                break;
            case "CancelButton_DealInteraction":
                HandleDealInteractionCancel();
                break;
        }
    }

    #region 按钮逻辑处理
    /// <summary>
    /// 单交互面板-确认按钮逻辑
    /// </summary>
    private void HandleSingleInteractionConfirm()
    {
        ConfirmCallback?.Invoke();

        SetActive_SingleInteractionGroup(false, () => {
            // 清空回调，避免重复触发
            ConfirmCallback = null;
        }, true);
    }

    /// <summary>
    /// 双交互面板-确认按钮逻辑
    /// </summary>
    private void HandleDoubleInteractionConfirm()
    {
        ConfirmCallback?.Invoke();

        SetActive_DoubleInteractionGroup(false, () => {
            // 清空回调
            ConfirmCallback = null;
            CancelCallback = null;
        }, true);
    }

    /// <summary>
    /// 双交互面板-取消按钮逻辑
    /// </summary>
    private void HandleDoubleInteractionCancel()
    {
        CancelCallback?.Invoke();

        SetActive_DoubleInteractionGroup(false, () => {
            // 清空回调
            ConfirmCallback = null;
            CancelCallback = null;
        }, true);
    }


    private void HandleDoubleInteraction2Confirm()
    {
        ConfirmCallback?.Invoke();

        SetActive_DoubleInteractionGroup2(false, () => {
            // 清空回调
            ConfirmCallback = null;
            CancelCallback = null;
        }, true);
    }

    private void HandleDoubleInteraction2Cancel()
    {
        CancelCallback?.Invoke();

        SetActive_DoubleInteractionGroup2(false, () => {
            // 清空回调
            ConfirmCallback = null;
            CancelCallback = null;
        }, true);
    }

    /// <summary>
    /// 协议交互面板-确认按钮逻辑
    /// </summary>
    private void HandleDealInteractionConfirm()
    {
        ConfirmCallback?.Invoke();

        SetActive_DealInteractionGroup(false, () => {
            // 清空回调
            ConfirmCallback = null;
            CancelCallback = null;
        }, true);
    }

    /// <summary>
    /// 协议交互面板-取消按钮逻辑
    /// </summary>
    private void HandleDealInteractionCancel()
    {
        CancelCallback?.Invoke();

        SetActive_DealInteractionGroup(false, () => {
            // 清空回调
            ConfirmCallback = null;
            CancelCallback = null;
        }, true);
    }
    #endregion
    #endregion

    #region 总面板动画
    protected override void SpecialAnimator_Show()
    {
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        // 停止所有动画 → 新增DoubleInteraction_Sequence2
        NoInteraction_Sequence?.Kill();
        SingleInteraction_Sequence?.Kill();
        DoubleInteraction_Sequence?.Kill();
        DealInteraction_Sequence?.Kill();
        DoubleInteraction_Sequence2?.Kill();

        // 强制失活所有子面板
        SetAllGroupsActive(false);

        // 清空回调
        ConfirmCallback = null;
        CancelCallback = null;

        base.HideMe(callback, isNeedDefaultAnimator);
    }
    #endregion
}