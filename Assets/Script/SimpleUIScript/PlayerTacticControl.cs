using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class PlayerTacticControl : MonoBehaviour
{
    public static PlayerTacticControl Instance;

    [Header("战术道具按钮")]
    public Button CurrentTacticButton;//当前的战术道具位（主按钮）
    public Button ExpendButton;//展开按钮
    public Button ExtraTacticButton;//额外的战术道具按钮
    public Image tacticImage;//主按钮图标
    public Image ExtratacticImage;//额外按钮图标

    [Header("战术道具数据")]
    public TacticInfo CurrentTactInfo; // 主道具数据
    public TacticInfo ExtraTactInfo;   // 额外道具数据

    [Header("动画配置")]
    public float SelectScale = 1.05f; // 选中缩放比例
    public float AnimationDuration = 0.2f; // 动画时长
    public Color SelectColor = ColorManager.LightGreen; // 选中颜色
    [HideInInspector] public Color NormalColor; // 自动缓存初始化颜色，无需手动赋值

    private bool _isChooseButton = false;//主按钮是否被选中
    private Image _currentTacticImage;
    private Tween _scaleTween;
    private Tween _colorTween;

    private bool _IsPrepararingInjection = false;
    /// <summary>
    /// 是否正在准备针剂道具
    /// </summary>
    public bool IsPrepararingInjection
    {
        get => _IsPrepararingInjection;
        set
        {
            _IsPrepararingInjection = value;
            //更新射击按钮的图标
            if (UImanager.Instance != null && UImanager.Instance.GetPanel<PlayerPanel>()?.shootButton != null)
            {
                if (_IsPrepararingInjection && CurrentTactInfo != null)
                    UImanager.Instance.GetPanel<PlayerPanel>().shootButton.ChangeIcon(CurrentTactInfo.UISprite);//更新为针剂图标
                else
                    UImanager.Instance.GetPanel<PlayerPanel>().shootButton.ResetIcon();//还原默认图标
            }
            else
            {
                Debug.LogWarning("UImanager或PlayerPanel未找到，无法更新射击按钮图标", this);
            }
        }
    }

    public bool IsChooseButton
    {
        get => _isChooseButton;
        set
        {
            _isChooseButton = value;
            // 切换选中状态时执行动画
            UpdateButtonVisualState(value);
        }
    }

    #region 生命周期
    private void Awake()
    {
        Instance = this;

        // 初始化主按钮
        if (CurrentTacticButton != null)
        {
            _currentTacticImage = CurrentTacticButton.image;
            NormalColor = _currentTacticImage.color;
            CurrentTacticButton.onClick.AddListener(OnCurrentTacticButtonClick);
        }
        else
        {
            Debug.LogError("CurrentTacticButton 未赋值！", this);
            return;
        }

        // 初始化展开按钮
        if (ExpendButton != null)
        {
            ExpendButton.onClick.AddListener(() =>
            {
                if (ExtraTacticButton != null)
                {
                    bool isActive = ExtraTacticButton.gameObject.activeSelf;
                    ExtraTacticButton.gameObject.SetActive(!isActive);
                    // 更新展开按钮文本
                    TextMeshProUGUI expendText = ExpendButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (expendText != null)
                    {
                        expendText.text = isActive ? "<" : ">";
                    }
                }
            });
        }

        // 初始化额外道具按钮的点击事件
        if (ExtraTacticButton != null)
        {
            ExtraTacticButton.onClick.AddListener(OnExtraTacticButtonClick);
            // 初始隐藏额外按钮
            ExtraTacticButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("ExtraTacticButton 未赋值！", this);
        }

        // 初始化动画参数
        if (SelectScale <= 0)
            SelectScale = 1.05f;
        if (AnimationDuration <= 0)
            AnimationDuration = 0.2f;
    }

    private void Start()
    {
        Init();
    }

    private void OnDestroy()
    {
        // 清理动画
        if (_scaleTween != null) _scaleTween.Kill();
        if (_colorTween != null) _colorTween.Kill();
        DOTween.Kill(this);

        // 移除事件监听
        if (CurrentTacticButton != null) CurrentTacticButton.onClick.RemoveListener(OnCurrentTacticButtonClick);
        if (ExtraTacticButton != null) ExtraTacticButton.onClick.RemoveListener(OnExtraTacticButtonClick);
        if (ExpendButton != null) ExpendButton.onClick.RemoveAllListeners();

        // 重置状态
        IsPrepararingInjection = false;
        IsChooseButton = false;
    }
    #endregion

    #region 初始化
    public void Init()
    {
        // 加载道具数据
        if (PlayerAndGameInfoManger.Instance != null && PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList.Count > 0)
        {
            CurrentTactInfo = PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[0].CurrentTactic_1Info;
            ExtraTactInfo = PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[0].CurrentTactic_2Info;
        }
        else
        {
            Debug.LogWarning("PlayerSlotInfoPacksList 为空，无法初始化战术道具信息", this);
        }

        // 初始化UI显示
        UpdateTacticButtonIcons();

        // 初始化状态
        IsChooseButton = false;
        IsPrepararingInjection = false;
    }
    #endregion

    #region 核心交互逻辑
    /// <summary>
    /// 主战术道具按钮点击回调
    /// </summary>
    private void OnCurrentTacticButtonClick()
    {
        if (CurrentTactInfo == null)
        {
            Debug.LogWarning("当前战术道具信息为空，无法操作", this);
            return;
        }

        bool isThrowObj = MilitaryManager.Instance.GetTacticBigType(CurrentTactInfo.tacticType) == TacticBigType.throwobj;
        bool isInjection = MilitaryManager.Instance.GetTacticBigType(CurrentTactInfo.tacticType) == TacticBigType.injection;
        bool canOperate = true;

        // 投掷物专属校验
        if (isThrowObj)
        {
            if (Player.LocalPlayer == null || Player.LocalPlayer.MyHandControl == null)
            {
                Debug.LogWarning("玩家或手部控制组件为空，无法操作投掷物", this);
                canOperate = false;
            }
            else
            {
                var handControl = Player.LocalPlayer.MyHandControl;
                if (handControl.CurrentThrowObj != null)
                {
                    ThrowObj throwObj = handControl.CurrentThrowObj.GetComponent<ThrowObj>();
                    if (throwObj == null)
                    {
                        Debug.LogError("CurrentThrowObj 缺少 ThrowObj 组件！", handControl.CurrentThrowObj);
                        canOperate = false;
                    }
                    else
                    {
                        canOperate = !throwObj.IsInAnimation;
                        if (!canOperate) Debug.Log("投掷物动画未完成，暂时无法操作", this);
                    }
                }
            }
        }

        // 针剂无需校验动画
        if (isInjection) canOperate = true;

        if (!canOperate) return;

        // 切换选中状态
        IsChooseButton = !IsChooseButton;
        if (IsChooseButton) TriggerTactic();
        else CancelTactic();
    }

    /// <summary>
    /// 额外战术道具按钮点击回调（核心：交换道具+自动收起）
    /// </summary>
    private void OnExtraTacticButtonClick()
    {
        // 核心限制：主按钮被选中时，禁止交换
        if (IsChooseButton)
        {
            Debug.LogWarning("主战术道具按钮已选中，禁止交换道具！", this);
            return;
        }

        // 校验道具数据是否有效
        if (CurrentTactInfo == null || ExtraTactInfo == null)
        {
            Debug.LogWarning("主/额外战术道具数据为空，无法交换！", this);
            return;
        }

        TacticInfo tempTactInfo = CurrentTactInfo;
        CurrentTactInfo = ExtraTactInfo;
        ExtraTactInfo = tempTactInfo;

        UpdateTacticButtonIcons();

        Debug.Log($"已交换战术道具：主道具→{CurrentTactInfo.Name}，额外道具→{ExtraTactInfo.Name}", this);

        if (IsPrepararingInjection)
        {
            IsPrepararingInjection = true; // 重新赋值触发图标更新
        }

        // 自动隐藏额外按钮
        if (ExtraTacticButton != null)
        {
            ExtraTacticButton.gameObject.SetActive(false);
        }

        // 恢复展开按钮为未激活状态（文本变回<）
        if (ExpendButton != null)
        {
            TextMeshProUGUI expendText = ExpendButton.GetComponentInChildren<TextMeshProUGUI>();
            if (expendText != null)
            {
                expendText.text = "<";
            }
        }
    }
    #endregion

    #region UI更新逻辑
    /// <summary>
    /// 更新主/额外战术道具按钮的图标
    /// </summary>
    private void UpdateTacticButtonIcons()
    {
        // 更新主按钮图标
        if (tacticImage != null && CurrentTactInfo != null)
        {
            tacticImage.sprite = CurrentTactInfo.UISprite;
            tacticImage.SetNativeSize(); // 还原图标原始尺寸
        }

        // 更新额外按钮图标
        if (ExtratacticImage != null && ExtraTactInfo != null)
        {
            ExtratacticImage.sprite = ExtraTactInfo.UISprite;
            ExtratacticImage.SetNativeSize(); // 还原图标原始尺寸
        }
    }

    /// <summary>
    /// 更新主按钮的视觉状态（选中/未选中）
    /// </summary>
    /// <param name="isSelected">是否选中</param>
    private void UpdateButtonVisualState(bool isSelected)
    {
        if (_currentTacticImage == null || CurrentTacticButton.transform == null) return;

        // 停止之前的动画
        if (_scaleTween != null && _scaleTween.IsPlaying()) 
            _scaleTween.Kill();
        if (_colorTween != null && _colorTween.IsPlaying())
            _colorTween.Kill();

        // 缩放动画
        float targetScale = isSelected ? SelectScale : 1f;
        _scaleTween = CurrentTacticButton.transform.DOScale(Vector3.one * targetScale, AnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetId(this);

        // 颜色动画
        Color targetColor = isSelected ? SelectColor : NormalColor;
        _colorTween = _currentTacticImage.DOColor(targetColor, AnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetId(this);
    }
    #endregion

    #region 战术道具触发/取消逻辑
    /// <summary>
    /// 触发战术道具
    /// </summary>
    public void TriggerTactic()
    {
        if (CurrentTactInfo == null)
        {
            Debug.LogWarning("当前战术道具信息为空，无法触发", this);
            return;
        }

        TacticBigType bigType = MilitaryManager.Instance.GetTacticBigType(CurrentTactInfo.tacticType);

        if (bigType == TacticBigType.injection)
        {
            // 触发针剂准备
            Debug.Log($"开始准备针剂道具：{CurrentTactInfo.Name}");
            IsPrepararingInjection = true;

        }
        else if (bigType == TacticBigType.throwobj)
        {
            // 触发拿出投掷物
            if (Player.LocalPlayer?.MyHandControl != null)
            {
                Debug.Log($"拿出投掷物：{CurrentTactInfo.Name}");
                Player.LocalPlayer.MyHandControl.TriggerThrowObj(CurrentTactInfo.tacticType);
            }
            else
            {
                Debug.LogError("Player.LocalPlayer.MyHandControl 为空！", this);
            }
        }
    }

    /// <summary>
    /// 取消战术道具
    /// </summary>
    public void CancelTactic()
    {
        if (CurrentTactInfo == null)
        {
            Debug.LogWarning("当前战术道具信息为空，无法取消", this);
            return;
        }

        TacticBigType bigType = MilitaryManager.Instance.GetTacticBigType(CurrentTactInfo.tacticType);

        if (bigType == TacticBigType.injection)
        {
            // 取消针剂准备
            Debug.Log($"取消针剂道具：{CurrentTactInfo.Name}");
            IsPrepararingInjection = false;
        }
        else if (bigType == TacticBigType.throwobj)
        {
            // 回收投掷物
            if (Player.LocalPlayer?.MyHandControl != null)
            {
                Debug.Log($"回收投掷物：{CurrentTactInfo.Name}");
                Player.LocalPlayer.MyHandControl.CmdRecycleThrowObj();
            }
            else
            {
                Debug.LogError("Player.LocalPlayer.MyHandControl 为空！", this);
            }
        }
    }

    /// <summary>
    /// 外部调用：触发注射器
    /// </summary>
    public void triggerInjection()
    {
        if (CurrentTactInfo == null)
        {
            Debug.LogWarning("当前战术道具信息为空，无法触发注射器", this);
            return;
        }

        TacticBigType bigType = MilitaryManager.Instance.GetTacticBigType(CurrentTactInfo.tacticType);
        if (bigType != TacticBigType.injection)
        {
            Debug.LogWarning($"当前道具类型不是针剂：{CurrentTactInfo.tacticType}", this);
            return;
        }

        // 触发注射器逻辑
        if (Player.LocalPlayer?.MyHandControl != null)
        {
            Player.LocalPlayer.MyHandControl.TriggerInjection(CurrentTactInfo.tacticType);
        }
        else
        {
            Debug.LogError("Player.LocalPlayer.MyHandControl 为空，无法触发针剂！", this);
        }

        // 触发后立即取消选中（双重保险）
        IsChooseButton = false;
        IsPrepararingInjection = false;
    }
    #endregion

    #region 外部接口
    /// <summary>
    /// 外部设置选中状态（强制取消）
    /// </summary>
    /// <param name="IsActive">是否激活（此处仅用于兼容，实际强制取消）</param>
    public void SetIsChooseButton(bool IsActive)
    {
        IsChooseButton = false;
        IsPrepararingInjection = false;
    }

    /// <summary>
    /// 外部强制取消所有战术道具状态
    /// </summary>
    public void ForceCancelAllTacticState()
    {
        if (IsChooseButton) IsChooseButton = false;
        if (IsPrepararingInjection) IsPrepararingInjection = false;
        // 交换后默认隐藏额外按钮
        if (ExtraTacticButton != null) ExtraTacticButton.gameObject.SetActive(false);
        if (ExpendButton?.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            ExpendButton.GetComponentInChildren<TextMeshProUGUI>().text = "<";
        }
    }
    #endregion
}
