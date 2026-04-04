using DG.Tweening;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SinglePlayerPanel : BasePanel
{
    private string SinglePlayerPanelToggleButtonGroup = "SinglePlayerPanel";

    [Header("面板动画")]
    public RectTransform LeftRect;      // 左边区域 (隐藏x=-450, 显示x=0)
    public RectTransform ButtomRect;    // 下方区域

    [Header("下方区域动画参数")]
    [Tooltip("显示位置的Y坐标")]
    public float buttomShowY = 110;     // 显示位置
    [Tooltip("隐藏/下沉位置的Y坐标")]
    public float buttomHideY = -110;     // 隐藏位置
    [Tooltip("下沉动画时长（秒）")]
    public float buttomHideDuration = 0.25f;
    [Tooltip("弹出动画时长（秒）")]
    public float buttomShowDuration = 0.35f;

    public CanvasGroup CanvasGroupLeftRect;
    public CanvasGroup CanvasGroupButtomRect;

    private Sequence LeftRectSequence;
    private Sequence ButtomRectSequence;

    [Header("背景图片")]
    public Sprite TrainSprite;
    public Sprite SingleSprite;

    [Header("背景改变功能")]
    public Image BackGround;

    [Header("文本组件")]
    public TextMeshProUGUI TopicText;
    public TextMeshProUGUI DescribeText;
    [TextArea(2,3)]
    public string DescribeTraintext;
    [TextArea(2, 3)]
    public string DescribeSinglePlayerText;

    private bool IsSelectTrain = true;

    private bool _isButtonGroupRegistered = false;

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        // 初始化位置
        if (LeftRect != null)
            LeftRect.anchoredPosition = new Vector2(-450, LeftRect.anchoredPosition.y);
        if (ButtomRect != null)
            ButtomRect.anchoredPosition = new Vector2(ButtomRect.anchoredPosition.x, buttomShowY);
        if (CanvasGroupLeftRect != null)
            CanvasGroupLeftRect.alpha = 0;
        if (CanvasGroupButtomRect != null)
            CanvasGroupButtomRect.alpha = 1;

        typingWritingTask1 = SimpleAnimatorTool.Instance.AddTypingTask("单人训练场", TopicText);
    }

    public override void Start()
    {
        base.Start();
        SafeRegisterButtonGroups();
    }

    private void SafeRegisterButtonGroups()
    {
        if (_isButtonGroupRegistered)
            return;

        try
        {
            if (ButtonGroupManager.Instance != null && controlDic != null)
            {
                // 注册火车按钮
                TryAddRadio("TrainButton", OnTrainButtonSelected);
                // 注册单人按钮
                TryAddRadio("SingleButton", OnSingleButtonSelected);
                // 安全选中第一个
                SafeSelectFirst();

            }
            _isButtonGroupRegistered = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SinglePlayerPanel] 注册按钮组时跳过: {e.Message}");
        }
    }

    private void TryAddRadio(string controlName, UnityAction call)
    {
        if (controlDic == null || !controlDic.ContainsKey(controlName)) 
            return;

        Button btn = controlDic[controlName] as Button;
        if (btn != null && ButtonGroupManager.Instance != null)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(
                SinglePlayerPanelToggleButtonGroup,
                btn,
                call
            );
        }
    }

    private void SafeSelectFirst()
    {
        try
        {
            if (ButtonGroupManager.Instance != null)
            {
                ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(SinglePlayerPanelToggleButtonGroup);
            }
        }
        catch
        {

        }
    }

    private void OnTrainButtonSelected()
    {
        ReFreshBackGround(TrainSprite);
        PlayButtomRefreshAnimation();
    }

    private void OnSingleButtonSelected()
    {
        ReFreshBackGround(SingleSprite);
        PlayButtomRefreshAnimation();
    }

    // 下方区域刷新动画：先上去隐藏，再下来显示
    private TypingWritingTask typingWritingTask;
    private void PlayButtomRefreshAnimation()
    {
        if (ButtomRectSequence != null && ButtomRectSequence.IsActive())
            ButtomRectSequence.Kill();
        DescribeText.text = "";
        ButtomRectSequence = DOTween.Sequence();

        ButtomRectSequence.Append(ButtomRect.DOAnchorPosY(buttomHideY, buttomHideDuration).SetEase(Ease.InQuad)).OnComplete(() => {

            //播放文字解说动画
            if(typingWritingTask!=null)
               SimpleAnimatorTool.Instance.RemoveTypingTask(typingWritingTask);

            //开启打字任务
           typingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask(IsSelectTrain? DescribeTraintext: DescribeSinglePlayerText, DescribeText);
        });
        ButtomRectSequence.Join(CanvasGroupButtomRect.DOFade(0, buttomHideDuration));

        ButtomRectSequence.Append(ButtomRect.DOAnchorPosY(buttomShowY, buttomShowDuration).SetEase(Ease.OutQuad));
        ButtomRectSequence.Join(CanvasGroupButtomRect.DOFade(1, buttomShowDuration));
    }

    public void IsActiveLeftArea(bool IsActive)
    {
        if (LeftRectSequence != null && LeftRectSequence.IsActive())
            LeftRectSequence.Kill();

        LeftRectSequence = DOTween.Sequence();

        if (IsActive)
        {
            LeftRectSequence.Join(CanvasGroupLeftRect.DOFade(1, 0.5f).SetEase(Ease.OutQuad));
            LeftRectSequence.Join(LeftRect.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutQuad));
        }
        else
        {
            LeftRectSequence.Join(CanvasGroupLeftRect.DOFade(0, 0.5f).SetEase(Ease.InQuad));
            LeftRectSequence.Join(LeftRect.DOAnchorPosX(-450, 0.5f).SetEase(Ease.InQuad));
        }

    }

    // 保留原有的显隐方法
    public void IsActiveButtomArea(bool IsActive)
    {
        if (ButtomRectSequence != null && ButtomRectSequence.IsActive())
            ButtomRectSequence.Kill();

        ButtomRectSequence = DOTween.Sequence();

        if (IsActive)
        {
            ButtomRectSequence.Join(CanvasGroupButtomRect.DOFade(1, 0.5f).SetEase(Ease.OutQuad));
            ButtomRectSequence.Join(ButtomRect.DOAnchorPosY(buttomShowY, 0.5f).SetEase(Ease.OutQuad));
        }
        else
        {
            ButtomRectSequence.Join(CanvasGroupButtomRect.DOFade(0, 0.5f).SetEase(Ease.InQuad));
            ButtomRectSequence.Join(ButtomRect.DOAnchorPosY(buttomHideY, 0.5f).SetEase(Ease.InQuad));
        }
    }

    // 刷新背景
    private TypingWritingTask typingWritingTask1;
    public void ReFreshBackGround(Sprite sprite)
    {
        BackGround.DOKill();
        TopicText.text = "";
        if (BackGround.color.a >= 0.9f)
        {
            BackGround.DOFade(0, 0.5f).OnComplete(() =>
            {
                BackGround.sprite = sprite;
                BackGround.DOFade(1, 0.5f);
            });
        }
        else
        {
            BackGround.sprite = sprite;
            BackGround.DOFade(1, 0.5f);
        }
        if (typingWritingTask1 != null)
            SimpleAnimatorTool.Instance.RemoveTypingTask(typingWritingTask1);
        typingWritingTask1 = SimpleAnimatorTool.Instance.AddTypingTask(IsSelectTrain ? "单人训练场" : "单人战役", TopicText);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 清理所有动画
        LeftRectSequence?.Kill();
        ButtomRectSequence?.Kill();
        BackGround?.DOKill();

        _isButtonGroupRegistered = false;
        try
        {
            ButtonGroupManager.Instance.DestroyRadioGroup(SinglePlayerPanelToggleButtonGroup);
        }
        catch {
        
        }
    }
    #endregion

    #region UI控件
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ReturnButton")
        {
            UImanager.Instance.HidePanel<SinglePlayerPanel>();
            ModeChooseSystem.instance.SwitchToMainCamera();
            // 延迟1秒后显示开始面板

            CountDownManager.Instance.CreateTimer(false, 1000, () =>
            {
                UImanager.Instance.ShowPanel<GameStartPanel>();
            });

        }
        else if(controlName == "TrainButton")
            IsSelectTrain=true;
        else if (controlName == "SingleButton")
            IsSelectTrain = false;
        else if(controlName == "EnterButton")
        {
            //进入
            if(IsSelectTrain)
            {
                //开启单人服务进入训练场
                NetworkManager.singleton.StartHost();
                Main.Instance.IsInSingleMode=true;
                UImanager.Instance.HidePanel<SinglePlayerPanel>();
                CountDownManager.Instance.CreateTimer(false, 100, () => {
                    UImanager.Instance.HidePanel<PlayerPreparaPanel>();
                });
            }   
            else
                WarnTriggerManager.Instance.TriggerSingleInteractionWarn("当前模式未开放","很抱歉影响你的体验，我们正在赶工制作，敬请期待！");
        }
    }
    #endregion

    #region UI面板显隐以及特殊动画
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        IsActiveLeftArea(false);
        IsActiveButtomArea(false);

        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        IsActiveLeftArea(true);

        // 打开面板时直接复位下方区域位置和透明度
        if (ButtomRect != null)
            ButtomRect.anchoredPosition = new Vector2(ButtomRect.anchoredPosition.x, buttomShowY);
        if (CanvasGroupButtomRect != null)
            CanvasGroupButtomRect.alpha = 1;
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
        IsActiveLeftArea(false);
        IsActiveButtomArea(false);
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
        IsActiveLeftArea(true);
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
    #endregion
}