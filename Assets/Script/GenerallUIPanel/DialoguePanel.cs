using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class DialoguePanel : BasePanel
{
    #region 控件关联
    [Header("控件关联")]
    public TextMeshProUGUI SpeakerName;//说话人名称
    public TextMeshProUGUI DialogueContent;//说话内容
    public TextMeshProUGUI PromptText;//提示文本
    public Image SpeakerImage;//说话人头像

    public Image SkipProgressImage;//跳过进度条
    private CanvasGroup SkipProgressGroup;//跳过进度条CanvasGroup  
    public TextMeshProUGUI SkipPromptText;//跳过提示文本

    private bool IsCanSkipDialogue = false;//是否可以跳过当前对话
    [Header("跳过进度条填充的速度")]
    public float SkipProgressFillSpeed = 0.6f;//跳过进度条填充的速度
    private bool IsSkippingComplete = false;//是否强制跳过完成

    // 记录当前进度条填充值
    private float currentSkipProgress = 0f;

    [Header("跳过提示文本动画参数")]
    public float skipTextFadeDuration = 1f;//单次淡入/淡出时长

    [Header("打字机速度（越小越快）")]
    public float SpeakerNameTypewriterSpeed = 0.1f; // 说话人名称速度
    public float DialogueContentTypewriterSpeed = 0.03f; // 对话内容速度

    #endregion

    #region 当前数据包
    private DialogueDataPack CurrentDialogueDataPack;//当前对话数据包
    private int CurrentDialogueIndex = 0;//当前对话数据包中的对话索引
    private DialoguePlayType _dialoguePlayType;
    private DialoguePlayType dialoguePlayType
    {
        get => _dialoguePlayType;
        set
        {
            if (_dialoguePlayType != value)
            {
                DialoguePlayType oldType = _dialoguePlayType;
                _dialoguePlayType = value;

                controlDic["ChangeModeButton"].GetComponentInChildren<TextMeshProUGUI>().text = _dialoguePlayType == DialoguePlayType.AutoPlay ? "切换为交互模式" : "切换为自动模式";

                if (_dialoguePlayType == DialoguePlayType.AutoPlay && CurrentSentenceIsFinished)
                {
                    if (IsLastSentence)
                    {
                        float waitTimeMs = CurrentDialogueDataPack.dialogueInfoPacks[CurrentDialogueIndex].WaitTimer * 1000;
                        lastAutoCloseTimerId = CountDownManager.Instance.CreateTimer(
                            false,
                            (int)waitTimeMs,
                            CloseDialoguePanel
                        );
                    }
                    else if (CurrentDialogueIndex < CurrentDialogueDataPack.dialogueInfoPacks.Count - 1)
                    {
                        float waitTimeMs = CurrentDialogueDataPack.dialogueInfoPacks[CurrentDialogueIndex].WaitTimer * 1000;
                        CountDownManager.Instance.CreateTimer(false, (int)waitTimeMs, PlayNextText);
                    }
                }
                else if (_dialoguePlayType == DialoguePlayType.Interact && oldType == DialoguePlayType.AutoPlay)
                {
                    if (lastAutoCloseTimerId != -1)
                    {
                        CountDownManager.Instance.RemoveTimer(lastAutoCloseTimerId);
                        lastAutoCloseTimerId = -1;
                    }
                }

                UpdatePromptTextState();
            }
        }
    }

    public bool IsInDialoguePlay
    {
        get => DialogueManager.Instance.IsPlayDialogue;
        set => DialogueManager.Instance.IsPlayDialogue = value;
    }

    private bool _currentSentenceIsFinished = false;
    public bool CurrentSentenceIsFinished
    {
        get => _currentSentenceIsFinished;
        set
        {
            _currentSentenceIsFinished = value;
            UpdatePromptTextState();
        }
    }

    private int lastAutoCloseTimerId = -1;
    private bool IsLastSentence = false;//标记是否是最后一句对话

    #endregion

    #region 核心交互逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "InteractButton")
        {
            // 交互模式 + 句子完成 才响应点击
            if (dialoguePlayType == DialoguePlayType.Interact && CurrentSentenceIsFinished)
            {
                if (IsLastSentence)
                {
                    // 最后一句点击：关闭面板
                    CloseDialoguePanel();
                }
                else
                {
                    // 非最后一句：播放下一句
                    PlayNextText();
                }
            }
        }

        if(controlName=="ChangeModeButton")
        {
            dialoguePlayType = dialoguePlayType == DialoguePlayType.AutoPlay ? DialoguePlayType.Interact : DialoguePlayType.AutoPlay;
        }
    }

    /// <summary>
    /// 播放下一句对话
    /// </summary>
    public void PlayNextText()
    {
        Debug.Log("索引加加播放下一句");
        CurrentSentenceIsFinished = false;//重置句子完成状态
        IsLastSentence = false;

        CurrentDialogueIndex++;
        if (CurrentDialogueIndex < CurrentDialogueDataPack.dialogueInfoPacks.Count)
        {
            UpdateCurrentSentence(CurrentDialogueDataPack);
        }
        else
        {
            // 自动模式下所有对话播放完成
            CloseDialoguePanel();
        }
    }

    /// <summary>
    /// 关闭对话面板
    /// </summary>
    private void CloseDialoguePanel()
    {
        IsInDialoguePlay = false;
        CurrentSentenceIsFinished = false;
        IsLastSentence = false;

        // 清理动画任务
        StopPromptTextAnimation();
        StopSkipPromptTextAnimation();

        // 清理计时器
        if (lastAutoCloseTimerId != -1)
        {
            CountDownManager.Instance.RemoveTimer(lastAutoCloseTimerId);
            lastAutoCloseTimerId = -1;
        }

        UImanager.Instance.HidePanel<DialoguePanel>();
    }
    #endregion

    #region 面板生命周期
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        // 隐藏面板时清理所有状态
        if (lastAutoCloseTimerId != -1)
        {
            CountDownManager.Instance.RemoveTimer(lastAutoCloseTimerId);
            lastAutoCloseTimerId = -1;
        }
        IsLastSentence = false;
        CurrentSentenceIsFinished = false;
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        // 显示面板时重置状态
        IsSkippingComplete = false;
        currentSkipProgress = 0f;
        SkipProgressImage.fillAmount = 0f;
        lastAutoCloseTimerId = -1;
        IsLastSentence = false;
        CurrentSentenceIsFinished = false;
    }

    public override void Awake()
    {
        base.Awake();
        // 初始化跳过进度条CanvasGroup
        SkipProgressGroup = SkipProgressImage.GetComponent<CanvasGroup>();
        if (SkipProgressGroup == null)
        {
            SkipProgressGroup = SkipProgressImage.gameObject.AddComponent<CanvasGroup>();
        }
        SkipProgressGroup.alpha = 0;//初始隐藏
        currentSkipProgress = 0f;
        SkipProgressImage.fillAmount = 0f;

        // 初始隐藏PromptText
        PromptText.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁时清理所有动画和计时器
        StopSkipPromptTextAnimation();
        StopPromptTextAnimation();

        if (lastAutoCloseTimerId != -1)
        {
            CountDownManager.Instance.RemoveTimer(lastAutoCloseTimerId);
        }
        DOTween.Clear(true);
    }
    #endregion

    #region 对话播放逻辑
    /// <summary>
    /// 启动对话包播放
    /// </summary>
    public void StartDialoguePack(DialogueDataPack InfoPack, DialoguePlayType DialogueType, bool IsCanSkip)
    {
        SetActiveSkipText(IsCanSkip);
        CurrentDialogueDataPack = InfoPack;
        CurrentDialogueIndex = 0;
        dialoguePlayType = DialogueType;
        IsInDialoguePlay = true;
        IsLastSentence = false;
        CurrentSentenceIsFinished = false;

        UpdateCurrentSentence(CurrentDialogueDataPack);
    }

    /// <summary>
    /// 更新当前句子显示
    /// </summary>
    public void UpdateCurrentSentence(DialogueDataPack InfoPack)
    {
        var currentInfo = InfoPack.dialogueInfoPacks[CurrentDialogueIndex];

        // 更新说话人名称
        if (SpeakerName.text != currentInfo.speakerName)
        {
            SimpleAnimatorTool.Instance.AddTypingTask(
                currentInfo.speakerName,
                SpeakerName,
                SpeakerNameTypewriterSpeed
            );
        }

        // 更新说话人头像
        if (currentInfo.speakerAvatar != null)
        {
            SpeakerImage.sprite = currentInfo.speakerAvatar;
            SpeakerImage.gameObject.SetActive(true);
        }
        else
        {
            SpeakerImage.gameObject.SetActive(false);
        }

        // 更新对话内容（完成后触发CurrentSentenceComplete）
        SimpleAnimatorTool.Instance.AddTypingTask(
            currentInfo.dialogueContent,
            DialogueContent,
            DialogueContentTypewriterSpeed,
            CurrentSentenceComplete
        );
    }

    /// <summary>
    /// 句子播放完成回调
    /// </summary>
    private FadeLoopTask PromptTextTask;
    public void CurrentSentenceComplete()
    {
        CurrentSentenceIsFinished = true;//设置为完成（自动触发UpdatePromptTextState）

        // 判断是否是最后一句
        if (CurrentDialogueIndex >= CurrentDialogueDataPack.dialogueInfoPacks.Count - 1)
        {
            IsInDialoguePlay = false;
            IsLastSentence = true;

            // 自动模式：等待最后一句的WaitTimer后关闭
            if (dialoguePlayType == DialoguePlayType.AutoPlay)
            {
                float waitTimeMs = CurrentDialogueDataPack.dialogueInfoPacks[CurrentDialogueIndex].WaitTimer * 1000;
                lastAutoCloseTimerId = CountDownManager.Instance.CreateTimer(
                    false,
                    (int)waitTimeMs,
                    CloseDialoguePanel
                );
            }
            // 交互模式：UpdatePromptTextState已处理PromptText显示，无需额外操作
            return;
        }

        // 非最后一句：根据模式处理下一步
        switch (dialoguePlayType)
        {
            case DialoguePlayType.AutoPlay:
                // 自动模式：等待后播放下一句
                float waitTimeMs = CurrentDialogueDataPack.dialogueInfoPacks[CurrentDialogueIndex].WaitTimer * 1000;
                CountDownManager.Instance.CreateTimer(false, (int)waitTimeMs, PlayNextText);
                break;
            case DialoguePlayType.Interact:
                // 交互模式：UpdatePromptTextState已处理PromptText显示
                break;
        }
    }
    #endregion

    #region PromptText 核心优化逻辑
    /// <summary>
    /// 统一更新PromptText显示状态（核心优化）
    /// 显示条件：交互模式 + 句子完成 + 未跳过 + 不是强制跳过状态
    /// </summary>
    private void UpdatePromptTextState()
    {
        // 显示条件：交互模式 + 句子完成 + 未强制跳过
        bool shouldShow = dialoguePlayType == DialoguePlayType.Interact
                        && CurrentSentenceIsFinished
                        && !IsSkippingComplete;

        // 设置激活状态
        PromptText.gameObject.SetActive(shouldShow);

        // 显示时启动闪烁动画，隐藏时停止
        if (shouldShow)
        {
            PromptText.color=ColorManager.SetColorAlpha(PromptText.color, 0f);//确保可见
            PromptText.DOFade(1, 0.5f).OnComplete(StartPromptTextAnimation);
        }
        else
        {
            StopPromptTextAnimation();
        }
    }

    /// <summary>
    /// 启动PromptText闪烁动画
    /// </summary>
    private void StartPromptTextAnimation()
    {
        if (PromptTextTask == null)
        {
            PromptTextTask = SimpleAnimatorTool.Instance.AddFadeLoopTask(PromptText);
        }
    }

    /// <summary>
    /// 停止PromptText闪烁动画
    /// </summary>
    private void StopPromptTextAnimation()
    {
        if (PromptTextTask != null)
        {
            SimpleAnimatorTool.Instance.RemoveFadeLoopTask(PromptTextTask);
            PromptText.alpha = 1f;//恢复透明度
            PromptTextTask = null;
        }
    }
    #endregion

    #region 跳过相关逻辑
    private FadeLoopTask SkipPromptTextTask;//跳过提示文本动画任务

    /// <summary>
    /// 控制跳过提示文本显示/隐藏
    /// </summary>
    public void SetActiveSkipText(bool IsActive)
    {
        SkipPromptText.gameObject.SetActive(IsActive);
        IsCanSkipDialogue = IsActive;

        if (IsActive)
        {
            StartSkipPromptTextAnimation();
        }
        else
        {
            StopSkipPromptTextAnimation();
        }
    }

    /// <summary>
    /// 启动跳过提示文本动画
    /// </summary>
    private void StartSkipPromptTextAnimation()
    {
        if (SkipPromptTextTask == null)
        {
            SkipPromptTextTask = SimpleAnimatorTool.Instance.AddFadeLoopTask(SkipPromptText);
        }
    }

    /// <summary>
    /// 停止跳过提示文本动画
    /// </summary>
    private void StopSkipPromptTextAnimation()
    {
        if (SkipPromptTextTask != null)
        {
            SimpleAnimatorTool.Instance.RemoveFadeLoopTask(SkipPromptTextTask);
            SkipPromptText.alpha = 1f;
            SkipPromptTextTask = null;
        }
    }

    private bool IsSkipProgressShow = false;//跳过进度条显示状态
    public void ShowSkipProgressImage()
    {
        if (!IsSkipProgressShow)
        {
            IsSkipProgressShow = true;
            // 捕获当前的CanvasGroup引用，避免后续销毁后引用丢失
            CanvasGroup currentGroup = SkipProgressGroup;
            SimpleAnimatorTool.Instance.StartFloatLerp(0, 1, 0.3f, (Value) =>
            {
                // 关键检查：组件是否存在且未被销毁
                if (currentGroup != null && currentGroup.gameObject != null)
                {
                    currentGroup.alpha = Value;
                    UpdatePromptTextState();
                }
            });
        }
    }

    public void HideSkipProgressImage()
    {
        if (IsSkipProgressShow)
        {
            IsSkipProgressShow = false;
            // 捕获当前的CanvasGroup引用
            CanvasGroup currentGroup = SkipProgressGroup;
            SimpleAnimatorTool.Instance.StartFloatLerp(1, 0, 0.2f, (Value) =>
            {
                // 关键检查：组件是否存在且未被销毁
                if (currentGroup != null && currentGroup.gameObject != null)
                {
                    currentGroup.alpha = Value;
                    UpdatePromptTextState();
                }
            });
        }
    }

    protected override void Update()
    {
        base.Update();

        Debug.Log(IsInDialoguePlay && IsCanSkipDialogue && !IsSkippingComplete);

        if (IsInDialoguePlay && IsCanSkipDialogue && !IsSkippingComplete)
        {
            bool isPressed = InputInfoManager.Instance.CheckActionKeyHeld(E_InputAction.DialogueSkip);
            if (InputInfoManager.Instance.CheckActionKeyHeld(E_InputAction.DialogueSkip))
            {
                ShowSkipProgressImage();
                // 填充进度条
                currentSkipProgress += SkipProgressFillSpeed * Time.deltaTime;
                currentSkipProgress = Mathf.Clamp01(currentSkipProgress);
                SkipProgressImage.fillAmount = currentSkipProgress;

                // 进度条填满：强制跳过
                if (currentSkipProgress >= 1f)
                {
                    IsSkippingComplete = true;
                    CloseDialoguePanel();
                    HideSkipProgressImage();
                }
            }
            else
            {
                // 松开按键：进度条减少
                if (currentSkipProgress > 0f)
                {
                    currentSkipProgress -= SkipProgressFillSpeed * Time.deltaTime * 1.5f;
                    currentSkipProgress = Mathf.Clamp01(currentSkipProgress);
                    SkipProgressImage.fillAmount = currentSkipProgress;
                }
                else if (IsSkipProgressShow)
                {
                    HideSkipProgressImage();
                    IsSkippingComplete = false;
                }
            }
        }
    }
    #endregion

    #region 特殊动画实现
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion
}