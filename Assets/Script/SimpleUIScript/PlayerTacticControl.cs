using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

// 你定义的数据包类
public class TacticPack
{
    public TacticInfo tactInfo;
    public int Index = 0; // 道具位索引 (1 或 2)
    public bool IsPlaying {
        get {
            if (Index == 1)
                return Player.LocalPlayer.MyHandControl.IsTrigger_tactic1;
            else
                return Player.LocalPlayer.MyHandControl.IsTrigger_tactic2;
        }
    
    }
    public float CoolPrecent {
    get
        {
            if (Index == 1)
                return Player.LocalPlayer.MyHandControl.tactic_1CoolTime_precent;
            else
                return Player.LocalPlayer.MyHandControl.tactic_2CoolTime_precent;
        }

        private set { }
    }
}

public class PlayerTacticControl : MonoBehaviour
{
    public static PlayerTacticControl Instance;

    [Header("战术道具按钮")]
    public Button CurrentTacticButton;
    public Button ExpendButton;
    public Button ExtraTacticButton;
    public Image tacticImage;
    public Image ExtratacticImage;

    [Header("战术道具数据")]

    public TacticPack CurrentTactPack; // 主道具数据包
    public TacticPack ExtraTactPack;   // 额外道具数据包

    [Header("动画配置")]
    public float SelectScale = 1.05f;
    public float AnimationDuration = 0.2f;
    public Color SelectColor = ColorManager.LightGreen;
    [HideInInspector] public Color NormalColor;

    [Header("冷却遮罩图")]
    public Image CoolImage_Tactic1;
    public Image CoolImage_Tactic2;

    private bool _isChooseButton = false;
    private Image _currentTacticImage;
    private Tween _scaleTween;
    private Tween _colorTween;

    private bool _IsPrepararingInjection = false;


    [Header("我的CanvasGroup")]
    public CanvasGroup MyCanvasGroup;
    private Sequence MyCanvasGroupAnima;

    public void SetTacticControl(bool IsActive)
    {
        if(IsActive)
            MyCanvasGroup.interactable = true;
        else
            MyCanvasGroup.interactable = false;
        //是否激活当前的战术道具位
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(MyCanvasGroup, ref MyCanvasGroupAnima, IsActive, () => {
        });

    }

    public int CurrentMainTacticIndex => CurrentTactPack?.Index ?? 1;

    public bool IsPrepararingInjection
    {
        get => _IsPrepararingInjection;
        set
        {
            _IsPrepararingInjection = value;
            if (UImanager.Instance != null && UImanager.Instance.GetPanel<PlayerPanel>()?.shootButton != null)
            {
                if (_IsPrepararingInjection && CurrentTactPack?.tactInfo != null)
                    UImanager.Instance.GetPanel<PlayerPanel>().shootButton.ChangeIcon(CurrentTactPack.tactInfo.UISprite);
                else
                    UImanager.Instance.GetPanel<PlayerPanel>().shootButton.ResetIcon();
            }
        }
    }

    public bool IsChooseButton
    {
        get => _isChooseButton;
        set
        {
            _isChooseButton = value;
            UpdateButtonVisualState(value);
        }
    }

    #region 生命周期
    private void Awake()
    {
        Instance = this;
        SetTacticControl(false);//初始化无法获取战术设备

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

        if (ExpendButton != null)
        {
            ExpendButton.onClick.AddListener(() =>
            {
                if (ExtraTacticButton != null)
                {
                    bool isActive = ExtraTacticButton.gameObject.activeSelf;
                    ExtraTacticButton.gameObject.SetActive(!isActive);
                    TextMeshProUGUI expendText = ExpendButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (expendText != null)
                    {
                        expendText.text = isActive ? "<" : ">";
                    }
                }
            });
        }

        if (ExtraTacticButton != null)
        {
            ExtraTacticButton.onClick.AddListener(OnExtraTacticButtonClick);
            ExtraTacticButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("ExtraTacticButton 未赋值！", this);
        }

        if (SelectScale <= 0) 
            SelectScale = 1.05f;
        if (AnimationDuration <= 0)
            AnimationDuration = 0.2f;
    }

    private void Update()
    {
       if(CurrentTactPack.IsPlaying)
        {
            //进行冷却更新
            CoolImage_Tactic1.fillAmount = CurrentTactPack.CoolPrecent;
        }
       if(ExtraTactPack.IsPlaying) 
       {
            //进行冷却更新
            CoolImage_Tactic2.fillAmount = ExtraTactPack.CoolPrecent;
        }
    }

    private void Start()
    {
        Init();
        
    }

    private void OnDestroy()
    {
        if (_scaleTween != null)
            _scaleTween.Kill();
        if (_colorTween != null)
            _colorTween.Kill();
        DOTween.Kill(this);

        if (CurrentTacticButton != null)
            CurrentTacticButton.onClick.RemoveListener(OnCurrentTacticButtonClick);
        if (ExtraTacticButton != null)
            ExtraTacticButton.onClick.RemoveListener(OnExtraTacticButtonClick);
        if (ExpendButton != null) ExpendButton.onClick.RemoveAllListeners();

        IsPrepararingInjection = false;
        IsChooseButton = false;

        SetTacticControl(false);
    }
    #endregion

    #region 初始化
    public void Init()
    {
        UpdateCurrentTactic();
        UpdateTacticButtonIcons();
        IsChooseButton = false;
        IsPrepararingInjection = false;
    }

    public void UpdateCurrentTactic()
    {
        if (PlayerAndGameInfoManger.Instance != null && PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList.Count > 0)
        {
            var Index = PlayerAndGameInfoManger.Instance.SlotCount - 1;

            CurrentTactPack = new TacticPack();
            CurrentTactPack.tactInfo = PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[Index].CurrentTactic_1Info;
            CurrentTactPack.Index = 1; // 主槽初始为 1

            ExtraTactPack = new TacticPack();
            ExtraTactPack.tactInfo = PlayerAndGameInfoManger.Instance.PlayerSlotInfoPacksList[Index].CurrentTactic_2Info;
            ExtraTactPack.Index = 2; // 额外槽初始为 2

            UpdateTacticButtonIcons();
        }
        else
        {
            Debug.LogWarning("PlayerSlotInfoPacksList 为空，无法初始化战术道具信息", this);
        }
    }
    #endregion

    #region 核心交互逻辑

    #region 主按钮交互
    private void OnCurrentTacticButtonClick()
    {
        if (JudgeCanUseTactic())
        {
            Debug.Log("当前战术道具正在冷却中，无法使用");
            return;
        }

        if (CurrentTactPack?.tactInfo == null)
        {
            Debug.LogWarning("当前战术道具信息为空，无法操作", this);
            return;
        }

        bool isThrowObj = MilitaryManager.Instance.GetTacticBigType(CurrentTactPack.tactInfo.tacticType) == TacticBigType.throwobj;
        bool isInjection = MilitaryManager.Instance.GetTacticBigType(CurrentTactPack.tactInfo.tacticType) == TacticBigType.injection;
        bool canOperate = true;

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

        if (isInjection)
            canOperate = true;

        if (!canOperate)
            return;

        IsChooseButton = !IsChooseButton;
        if (IsChooseButton)
            TriggerTactic();
        else
            CancelTactic();
    }
    #endregion

    private void OnExtraTacticButtonClick()
    {
        if (IsChooseButton)
        {
            Debug.LogWarning("主战术道具按钮已选中，禁止交换道具！", this);
            return;
        }

        if (CurrentTactPack?.tactInfo == null || ExtraTactPack?.tactInfo == null)
        {
            Debug.LogWarning("主/额外战术道具数据为空，无法交换！", this);
            return;
        }


        TacticPack tempPack = CurrentTactPack;
        CurrentTactPack = ExtraTactPack;
        ExtraTactPack = tempPack;

        UpdateTacticButtonIcons();

        Debug.Log($"已交换战术道具：主道具→{CurrentTactPack.tactInfo.Name}，原始槽位: {CurrentTactPack.Index}");
        //对于冷却图片的还原
        CoolImage_Tactic1.fillAmount = 0;
        CoolImage_Tactic2.fillAmount = 0;

        if (IsPrepararingInjection)
        {
            IsPrepararingInjection = true;
        }

        if (ExtraTacticButton != null)
            ExtraTacticButton.gameObject.SetActive(false);

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
    private void UpdateTacticButtonIcons()
    {        if (tacticImage != null && CurrentTactPack?.tactInfo != null)
        {
            tacticImage.sprite = CurrentTactPack.tactInfo.UISprite;
            tacticImage.SetNativeSize();
        }

        if (ExtratacticImage != null && ExtraTactPack?.tactInfo != null)
        {
            ExtratacticImage.sprite = ExtraTactPack.tactInfo.UISprite;
            ExtratacticImage.SetNativeSize();
        }
    }

    private void UpdateButtonVisualState(bool isSelected)
    {
        if (_currentTacticImage == null || CurrentTacticButton.transform == null) return;

        if (_scaleTween != null && _scaleTween.IsPlaying())
            _scaleTween.Kill();
        if (_colorTween != null && _colorTween.IsPlaying())
            _colorTween.Kill();

        float targetScale = isSelected ? SelectScale : 1f;
        _scaleTween = CurrentTacticButton.transform.DOScale(Vector3.one * targetScale, AnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetId(this);

        Color targetColor = isSelected ? SelectColor : NormalColor;
        _colorTween = _currentTacticImage.DOColor(targetColor, AnimationDuration)
            .SetEase(Ease.OutQuad)
            .SetId(this);
    }
    #endregion

    #region 战术道具触发/取消逻辑
    public void TriggerTactic()
    {
      
        if (CurrentTactPack?.tactInfo == null)
            return;

        TacticBigType bigType = MilitaryManager.Instance.GetTacticBigType(CurrentTactPack.tactInfo.tacticType);

        if (bigType == TacticBigType.injection)
        {
            Debug.Log($"开始准备针剂道具：{CurrentTactPack.tactInfo.Name}");
            IsPrepararingInjection = true;
        }
        else if (bigType == TacticBigType.throwobj)
        {
            if (Player.LocalPlayer?.MyHandControl != null)
            {
                Debug.Log($"拿出投掷物：{CurrentTactPack.tactInfo.Name}");
                Player.LocalPlayer.MyHandControl.TriggerThrowObj(CurrentTactPack.tactInfo.tacticType);
            }
            else
            {
                Debug.LogError("Player.LocalPlayer.MyHandControl 为空！", this);
            }
        }
    }

    public void CancelTactic()
    {
        if (CurrentTactPack?.tactInfo == null) return;

        TacticBigType bigType = MilitaryManager.Instance.GetTacticBigType(CurrentTactPack.tactInfo.tacticType);

        if (bigType == TacticBigType.injection)
        {
            Debug.Log($"取消针剂道具：{CurrentTactPack.tactInfo.Name}");
            IsPrepararingInjection = false;
        }
        else if (bigType == TacticBigType.throwobj)
        {
            if (Player.LocalPlayer?.MyHandControl != null)
            {
                Debug.Log($"回收投掷物：{CurrentTactPack.tactInfo.Name}");
                Player.LocalPlayer.MyHandControl.CmdRecycleThrowObj();
            }
            else
            {
                Debug.LogError("Player.LocalPlayer.MyHandControl 为空！", this);
            }
        }
    }

    public void triggerInjection()
    {
        if (CurrentTactPack?.tactInfo == null) 
            return;

        TacticBigType bigType = MilitaryManager.Instance.GetTacticBigType(CurrentTactPack.tactInfo.tacticType);
        if (bigType != TacticBigType.injection) return;

        if (Player.LocalPlayer?.MyHandControl != null)
        {
            Player.LocalPlayer.MyHandControl.TriggerInjection(CurrentTactPack.tactInfo.tacticType);
        }
        else
        {
            Debug.LogError("Player.LocalPlayer.MyHandControl 为空，无法触发针剂！", this);
        }

        IsChooseButton = false;
        IsPrepararingInjection = false;

        //判断当前的战术道具索引
        StartTacticCoolTime();
    }

    //开启战术道具的冷却
    public void StartTacticCoolTime()
    {
        if (CurrentTactPack.Index == 1)
            Player.LocalPlayer.MyHandControl.SetTactic_1Trigger();
        else
            Player.LocalPlayer.MyHandControl.SetTactic_2Trigger();
    }

    public bool JudgeCanUseTactic()
    {
        //判断当前的战术道具索引
        if (CurrentTactPack.Index == 1)
        {
            return Player.LocalPlayer.MyHandControl.IsTrigger_tactic1;
        }
        else if(CurrentTactPack.Index == 2)
        {
            return Player.LocalPlayer.MyHandControl.IsTrigger_tactic2;
        }
        Debug.LogError("接收到未知的索引");
        return false;

    }

    public void LaunchCurrentThrowObj()
    {
        Player.LocalPlayer.MyHandControl.LaunchCurrentThrowObj();
        //判断当前的战术道具索引
        StartTacticCoolTime();
    }
    #endregion

    #region 外部接口
    public void SetIsChooseButton(bool IsActive)
    {
        IsChooseButton = false;
        IsPrepararingInjection = false;
    }

    public void ForceCancelAllTacticState()
    {
        if (IsChooseButton) IsChooseButton = false;
        if (IsPrepararingInjection) IsPrepararingInjection = false;
        if (ExtraTacticButton != null) ExtraTacticButton.gameObject.SetActive(false);
        if (ExpendButton?.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            ExpendButton.GetComponentInChildren<TextMeshProUGUI>().text = "<";
        }
    }
    #endregion
}