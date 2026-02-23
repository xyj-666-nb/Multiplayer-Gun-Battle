using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public abstract class BasePanel : MonoBehaviour
{
    #region 字段定义（核心数据/配置）

    #region 对UI控件的缓存
    public Dictionary<string, UIBehaviour> controlDic = new Dictionary<string, UIBehaviour>();
    private static List<string> DefaultNameList = new List<string>()
    {
       "Image",
       "Text (TMP)",
       "RawImage",
       "Background",
       "Checkmark",
       "Label",
       "Text (Legacy)",
       "Arrow",
       "Placeholder",
       "Fill",
       "Handle",
       "Viewport",
       "Scrollbar Horizontal",
       "Scrollbar Vertical"
    };

    #endregion

    #region 面板显示/隐藏核心配置
    protected CanvasGroup canvasGroup;
    [Header("默认动画的透明度变化速度")]
    public float alphaSpeed = 10;
    [Tooltip("面板当前是否处于显示状态")]
    protected bool isShow = false;
    private UnityAction HideCallback;

    [Header("是否可以销毁")]
    public bool IsCanDestroy = true;
    [Header("是否使用真实时间")]
    public bool IsUseRealTime = false;
    [Header("是否使用独立Canvas")]
    [Space(10)]
    public bool IsUseSpecialCanvas = false;
    [Header("当前面板的优先级(优先级高的面板无论先启动还是后启动都会显示在前面)")]
    public int PriorityIndex = 0;

    [SerializeField, HideInInspector] private bool _canvasPixelPerfect = false;
    [SerializeField, HideInInspector] private int _canvasSortingOrder = 10;
    [SerializeField, HideInInspector] private string _canvasSortingLayer = "UI";
    [SerializeField, HideInInspector] private Vector2 _canvasReferenceResolution = new Vector2(1920, 1080);
    #endregion

    #region 高级自定义动画状态管理

    //——————————————高级状态管理——————————————————————————————
    protected bool IsUseDefaultAnimator_Show = true;
    protected bool IsUseDefaultAnimator_Hide = true;
    protected bool IsInShowing = false;
    protected bool IsInHiding = false;
    protected DG.Tweening.Sequence SpecialShowAnima;
    protected DG.Tweening.Sequence SpecialHideAnima;

    #endregion

    public bool IsInAnimator
    {
        get => IsInShowing || IsInHiding;
    }
    #endregion

    #region Canvas配置（独立Canvas设置）
    protected void ApplyCanvasSettings(RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay)
    {
        if (!IsUseSpecialCanvas)
            return;

        Canvas panelCanvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();

        panelCanvas.renderMode = canvasRenderMode;
        panelCanvas.pixelPerfect = _canvasPixelPerfect;
        panelCanvas.sortingOrder = _canvasSortingOrder;
        panelCanvas.sortingLayerName = _canvasSortingLayer;

        CanvasScaler scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = _canvasReferenceResolution;
    }
    #endregion

    #region 面板初始化（Awake/Start）
    public virtual void Awake()
    {
        if (GetComponent<CanvasGroup>() == null)
            gameObject.AddComponent<CanvasGroup>();
        // 优先查找交互类UI组件
        FindChildControl<Button>();
        FindChildControl<Toggle>();
        FindChildControl<Slider>();
        FindChildControl<Scrollbar>();
        FindChildControl<Dropdown>();
        FindChildControl<InputField>();
        FindChildControl<TMP_InputField>();

        FindChildControl<Text>();
        FindChildControl<Image>();
        FindChildControl<TextMeshProUGUI>();

        // 初始化CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        // 初始化CanvasGroup交互状态
        canvasGroup.interactable = false;
    }

    public virtual void Start()
    {
        if (!isShow)
        {
            canvasGroup.alpha = 0;
        }
        else
        {
            canvasGroup.interactable = true;
        }
    }
    #endregion

    #region UI控件事件回调（虚方法，子类重写）
    // 按钮点击事件
    public virtual void ClickButton(string controlName) { }
    public virtual void SliderValueChange(string sliderName, float value) { }
    public virtual void ToggleValueChange(string toggleName, bool value) { }
    public virtual void ScrollbarValueChange(string scrollbarName, float value) { }
    public virtual void DropdownValueChange(string dropdownName, int value) { }
    public virtual void InputFieldValueChange(string inputFieldName, string value) { }
    #endregion

    #region 面板显示逻辑
    /// <summary>
    /// 显示面板
    /// </summary>
    /// <param name="isNeedDefaultAnimator">是否使用默认淡入动画</param>
    public virtual void ShowMe(bool isNeedDefaultAnimator = true)
    {
        gameObject.SetActive(true);
        IsUseDefaultAnimator_Show = isNeedDefaultAnimator;
        IsInHiding = false; // 终止隐藏动画

        if (isNeedDefaultAnimator)
        {
            canvasGroup.alpha = 0;
            IsInShowing = true; // 标记为正在显示
        }
        else
        {
            UseSpecialAnimator_Show();
            IsUseDefaultAnimator_Show = false;
        }
        ApplyCanvasSettings();
    }

    /// <summary>
    /// 显示动画完成回调
    /// </summary>
    public void ShowAnimationComplete()
    {
        // 重置CanvasGroup状态,显示完成才能交互
        canvasGroup.interactable = true;
        IsInShowing = false;
        isShow = true;
        IsUseDefaultAnimator_Show = false;
        // 确保交互状态开启
        canvasGroup.interactable = true;
    }

    /// <summary>
    /// 动画框架逻辑
    /// </summary>
    protected  void UseSpecialAnimator_Show()
    {
        // 先销毁旧动画，防止内存泄漏
        SpecialShowAnima?.Kill();
        // 初始化动画序列
        SpecialShowAnima = DOTween.Sequence();
        SpecialShowAnima.SetUpdate(IsUseRealTime); // 适配真实时间
        SpecialShowAnima.OnComplete(ShowAnimationComplete);

        // 调用子类必须实现的抽象动画方法
        SpecialAnimator_Show();
        SpecialShowAnima.Play();
        IsInShowing = true;
    }

    /// <summary>
    /// 抽象方法：子类必须实现
    /// </summary>
    protected abstract void SpecialAnimator_Show();//在这里对SpecialShowAnima进行编辑就够了
    #endregion

    #region 简单面板显隐功能

    private  Sequence MyPanelSimpleAnima;
    public virtual void SimpleHidePanel()
    {
        canvasGroup.interactable = false; 
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(canvasGroup,ref MyPanelSimpleAnima, false, () => { });
    }
    public virtual void SimpleShowPanel()
    {
        canvasGroup.interactable = true;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(canvasGroup, ref MyPanelSimpleAnima, true, () => { });
    }

    #endregion

    #region 面板隐藏逻辑
    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <param name="callback">隐藏完成回调</param>
    /// <param name="isNeedDefaultAnimator">是否使用默认淡出动画</param>
    public virtual void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        HideCallback = callback;
        IsUseDefaultAnimator_Hide = isNeedDefaultAnimator;
        IsInShowing = false; // 终止显示动画

        canvasGroup.interactable = false;

        if (isNeedDefaultAnimator)
        {
            canvasGroup.alpha = 1;
            IsInHiding = true; // 标记为正在隐藏
        }
        else
        {
            UseSpecialAnimator_Hide(callback);
            IsUseDefaultAnimator_Hide = false;
        }
    }

    /// <summary>
    /// 隐藏动画完成回调
    /// </summary>
    public void HideAnimationComplete()
    {
        IsInHiding = false;
        isShow = false;
        IsUseDefaultAnimator_Hide = false;

        // 执行回调
        HideCallback?.Invoke();

        if (!IsCanDestroy)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 动画框架逻辑（子类无需关注）
    /// </summary>
    /// <param name="callback">隐藏完成回调</param>
    protected  void UseSpecialAnimator_Hide(UnityAction callback)
    {
        // 先销毁旧动画，防止内存泄漏
        SpecialHideAnima?.Kill();
        // 初始化动画序列
        SpecialHideAnima = DOTween.Sequence();
        SpecialHideAnima.SetUpdate(IsUseRealTime);
        SpecialHideAnima.OnComplete(HideAnimationComplete);

        // 调用子类必须实现的抽象动画方法
        SpecialAnimator_Hide();

        SpecialHideAnima.Play();
        IsInHiding = true;
    }

    /// <summary>
    /// 抽象方法：子类必须实现
    /// </summary>
    protected abstract void SpecialAnimator_Hide();//对SpecialHideAnima进行编辑
    #endregion

    #region UI控件查找与获取
    /// <summary>
    /// 获取指定名称的UI控件
    /// </summary>
    public T GetControl<T>(string name) where T : UIBehaviour
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"【BasePanel】控件名称不能为空！（面板：{gameObject.name}）");
            return null;
        }

        if (controlDic.TryGetValue(name, out UIBehaviour control))
        {
            if (control is T targetControl)
            {
                return targetControl;
            }
            Debug.LogError($"【BasePanel】控件 {name} 类型不匹配！期望 {typeof(T).Name}，实际 {control.GetType().Name}（面板：{gameObject.name}）");
        }
        else
        {
            Debug.LogError($"【BasePanel】未找到控件 {name}（面板：{gameObject.name}）");
        }
        return null;
    }

    /// <summary>
    /// 递归查找所有子物体的UI控件，并绑定事件
    /// </summary>
    private void FindChildControl<T>() where T : UIBehaviour
    {
        T[] controls = GetComponentsInChildren<T>(true);
        foreach (T control in controls)
        {
            string controlName = control.name;
            // 排除默认名称的控件，避免重复添加
            if (!DefaultNameList.Contains(controlName))
            {
                if (controlDic.ContainsKey(controlName))
                {
                    continue;
                }
                controlDic.Add(controlName, control);
                BindControlEvent(control, controlName);
            }
        }
    }

    /// <summary>
    /// 绑定UI控件的事件
    /// </summary>
    private void BindControlEvent<T>(T control, string controlName) where T : UIBehaviour
    {
        switch (control)
        {
            case Button btn:
                btn.onClick.AddListener(() => ClickButton(controlName));
                break;
            case Slider slider:
                slider.onValueChanged.AddListener((val) => SliderValueChange(controlName, val));
                break;
            case Toggle toggle:
                toggle.onValueChanged.AddListener((val) => ToggleValueChange(controlName, val));
                break;
            case Scrollbar scrollbar:
                scrollbar.onValueChanged.AddListener((val) => ScrollbarValueChange(controlName, val));
                break;
            case Dropdown dropdown:
                dropdown.onValueChanged.AddListener((val) => DropdownValueChange(controlName, val));
                break;
            case InputField input:
                input.onValueChanged.AddListener((val) => InputFieldValueChange(controlName, val));
                break;
            case TMP_InputField tmpInput:
                tmpInput.onValueChanged.AddListener((val) => InputFieldValueChange(controlName, val));
                break;
        }
    }
    #endregion

    #region 帧更新与工具方法
    private float deltaTime;
    /// <summary>
    /// 淡入淡出逻辑
    /// </summary>
    protected virtual void Update()
    {
        if (IsInAnimator)//只有在动画播放的时候才进行赋值
            deltaTime = IsUseRealTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 默认淡入逻辑
        if (IsInShowing && IsUseDefaultAnimator_Show)
        {
            canvasGroup.alpha += alphaSpeed * deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);

            if (Mathf.Approximately(canvasGroup.alpha, 1f))
            {
                ShowAnimationComplete();
            }
        }
        // 默认淡出逻辑
        else if (IsInHiding && IsUseDefaultAnimator_Hide)
        {
            canvasGroup.alpha -= alphaSpeed * deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);

            if (Mathf.Approximately(canvasGroup.alpha, 0f))
            {
                HideAnimationComplete();
            }
        }
    }

    /// <summary>
    /// 取消按钮选中状态
    /// </summary>
    /// <param name="button">目标按钮</param>
    public void DeselectButton(Button button)
    {
        if (button == null) return;

        Selectable selectable = button.GetComponent<Selectable>();
        if (selectable == null) return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        // 触发Deselect事件
        selectable.OnDeselect(new BaseEventData(eventSystem));
        // 清除选中状态
        if (eventSystem.currentSelectedGameObject == button.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }
        // 重置按钮状态
        selectable.OnPointerExit(null);
    }
    #endregion

    #region 生命周期销毁
    protected virtual void OnDestroy()
    {
        SpecialShowAnima?.Kill();
        SpecialHideAnima?.Kill();
        SpecialShowAnima = null;
        SpecialHideAnima = null;
    }
    #endregion
}