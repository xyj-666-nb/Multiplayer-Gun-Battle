using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameIntroducePanel : BasePanel
{
    private const string ViewButtonGroup = "GameIntroducePanel_ViewGroup";

    private enum ViewPanelType
    {
        Train,
        Operate,
        Online,
        ChooseMap
    }

    [System.Serializable]
    public class IntroducePageData
    {
        public string Topic;
        [TextArea(3, 8)]
        public string Content;
        public Sprite TopicSprite;
        public Sprite ShowSprite;
    }

    public List<IntroducePageData> IntroducePageDataList = new List<IntroducePageData>();

    public float TopicTypingSpeed = 0.05f;
    public float ContentTypingSpeed = 0.02f;
    public float ImageInfoTypingSpeed = 0.03f;
    public float TextFadeDuration = 0.25f;
    public float ImageFadeDuration = 0.25f;

    private const string UI_SELECT_SOUND = "Music/update415/ui\u9009\u62e9";
    private const string UI_BACK_SOUND = "Music/update415/ui\u8fd4\u56de";
    [Header("Training Intro")]
    public TextMeshProUGUI _topicText;
    public TextMeshProUGUI _contentText;
    public Image _topicImage;
    public Image _showImage;
    public CanvasGroup TrainPanelCanvas;
    [Header("\u8bad\u7ec3\u573a\u4ecb\u7ecd")]
    public CanvasGroup TrainPanelCanvasGroup;
    [Header("联机说明面板")]
    public CanvasGroup OnlinePanelCanvasGroup;
    [Header("地图选择面板")]
    public CanvasGroup MapPanelCanvasGroup;

    private TypingWritingTask _topicTypingTask;
    private TypingWritingTask _contentTypingTask;
    private TypingWritingTask _imageInfoTypingTask;
    private Sequence _imageSequence;
    private Sequence _trainPanelSequence;
    private Sequence _operatePanelSequence;
    private Sequence _onlinePanelSequence;
    private Sequence _mapPanelSequence;
    private int _currentPageIndex;
    private bool _hasRegisteredViewButtonGroup;

    public override void Awake()
    {
        base.Awake();
        AutoBindOnlinePanel();
        EnsureDefaultPageData();
    }

    public override void Start()
    {
        base.Start();
        RegisterViewButtonGroup();
        RefreshCurrentPage(false);
        SetTrainPanelVisible(true, false);
    }
    protected override void OnDestroy()
    {
        StopAllTypingTasks();
        _imageSequence?.Kill();
        _trainPanelSequence?.Kill();
        _operatePanelSequence?.Kill();
        _onlinePanelSequence?.Kill();
        _mapPanelSequence?.Kill();
        if (ButtonGroupManager.Instance != null)
        {
            ButtonGroupManager.Instance.DestroyRadioGroup(ViewButtonGroup);
        }
        _hasRegisteredViewButtonGroup = false;
        base.OnDestroy();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (controlName == "LeftButton")
        {
            MusicManager.Instance?.PlayEffect(UI_SELECT_SOUND);
            SwitchPage(-1);
        }
        else if (controlName == "RightButton")
        {
            MusicManager.Instance?.PlayEffect(UI_SELECT_SOUND);
            SwitchPage(1);
        }
        else if (controlName == "ReturnButton")
        {
            MusicManager.Instance?.PlayEffect(UI_BACK_SOUND);
            UImanager.Instance.HidePanel<GameIntroducePanel>();
        }
        else if (controlName == "TrainButton")
        {
            MusicManager.Instance?.PlayEffect(UI_SELECT_SOUND);
        }
        else if (controlName == "PoerateButton")
        {
            MusicManager.Instance?.PlayEffect(UI_SELECT_SOUND);
        }
        else if (controlName == "OnlineButton")
        {
            MusicManager.Instance?.PlayEffect(UI_SELECT_SOUND);
        }
        else if (controlName == "ChooseMapButton")
        {
            MusicManager.Instance?.PlayEffect(UI_SELECT_SOUND);
        }
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        StopAllTypingTasks();
        _imageSequence?.Kill();
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        RegisterViewButtonGroup();
        RefreshCurrentPage(true);
        TriggerDefaultViewButton();
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
        RegisterViewButtonGroup();
        RefreshCurrentPage(true);
        TriggerDefaultViewButton();
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }

    private void RegisterViewButtonGroup()
    {
        if (_hasRegisteredViewButtonGroup || ButtonGroupManager.Instance == null)
            return;

        if (controlDic.ContainsKey("TrainButton") && controlDic["TrainButton"] is Button trainButton)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ViewButtonGroup, trainButton, OnTrainButtonSelected, OnTrainButtonCanceled);
        }

        if (controlDic.ContainsKey("PoerateButton") && controlDic["PoerateButton"] is Button operateButton)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ViewButtonGroup, operateButton, OnOperateButtonSelected, OnOperateButtonCanceled);
        }

        if (controlDic.ContainsKey("OnlineButton") && controlDic["OnlineButton"] is Button onlineButton)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ViewButtonGroup, onlineButton, OnOnlineButtonSelected, OnOnlineButtonCanceled);
        }

        if (controlDic.ContainsKey("ChooseMapButton") && controlDic["ChooseMapButton"] is Button chooseMapButton)
        {
            ButtonGroupManager.Instance.AddRadioButtonToGroup(ViewButtonGroup, chooseMapButton, OnChooseMapButtonSelected, OnChooseMapButtonCanceled);
        }

        _hasRegisteredViewButtonGroup = true;
    }

    private void AutoBindOnlinePanel()
    {
        if (OnlinePanelCanvasGroup != null && MapPanelCanvasGroup != null)
            return;

        CanvasGroup[] canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            if (canvasGroup == null)
                continue;

            string panelName = canvasGroup.gameObject.name;
            if (panelName == "OnLinePanel" || panelName == "OnlinePanel")
            {
                OnlinePanelCanvasGroup = canvasGroup;
            }

            if (panelName == "ChooseMapPanel" || panelName == "MapPanel" || panelName == "MapIntroducePanel")
            {
                MapPanelCanvasGroup = canvasGroup;
            }

            if (OnlinePanelCanvasGroup != null && MapPanelCanvasGroup != null)
                return;
        }
    }

    private void TriggerDefaultViewButton()
    {
        if (ButtonGroupManager.Instance == null)
            return;

        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(ViewButtonGroup, true);
    }

    private void OnTrainButtonSelected()
    {
        SetActiveViewPanel(ViewPanelType.Train, true);
    }

    private void OnTrainButtonCanceled()
    {
        SetCanvasGroupVisible(TrainPanelCanvas, ref _trainPanelSequence, false, true);
    }

    private void OnOperateButtonSelected()
    {
        SetActiveViewPanel(ViewPanelType.Operate, true);
    }

    private void OnOperateButtonCanceled()
    {
        SetCanvasGroupVisible(TrainPanelCanvasGroup, ref _operatePanelSequence, false, true);
    }

    private void OnOnlineButtonSelected()
    {
        SetActiveViewPanel(ViewPanelType.Online, true);
    }

    private void OnOnlineButtonCanceled()
    {
        SetCanvasGroupVisible(OnlinePanelCanvasGroup, ref _onlinePanelSequence, false, true);
    }

    private void OnChooseMapButtonSelected()
    {
        SetActiveViewPanel(ViewPanelType.ChooseMap, true);
    }

    private void OnChooseMapButtonCanceled()
    {
        SetCanvasGroupVisible(MapPanelCanvasGroup, ref _mapPanelSequence, false, true);
    }

    private void SetTrainPanelVisible(bool showTrainPanel, bool playFade)
    {
        SetActiveViewPanel(showTrainPanel ? ViewPanelType.Train : ViewPanelType.Operate, playFade);
    }

    private void SetActiveViewPanel(ViewPanelType viewPanelType, bool playFade)
    {
        SetCanvasGroupVisible(TrainPanelCanvas, ref _trainPanelSequence, viewPanelType == ViewPanelType.Train, playFade);
        SetCanvasGroupVisible(TrainPanelCanvasGroup, ref _operatePanelSequence, viewPanelType == ViewPanelType.Operate, playFade);
        SetCanvasGroupVisible(OnlinePanelCanvasGroup, ref _onlinePanelSequence, viewPanelType == ViewPanelType.Online, playFade);
        SetCanvasGroupVisible(MapPanelCanvasGroup, ref _mapPanelSequence, viewPanelType == ViewPanelType.ChooseMap, playFade);
    }

    private void SetCanvasGroupVisible(CanvasGroup canvasGroup, ref Sequence sequence, bool isShow, bool playFade)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = isShow;
        canvasGroup.blocksRaycasts = isShow;

        if (!playFade || SimpleAnimatorTool.Instance == null)
        {
            sequence?.Kill();
            canvasGroup.alpha = isShow ? 1f : 0f;
            return;
        }

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(canvasGroup, ref sequence, isShow, () => { });
    }

    private void EnsureDefaultPageData()
    {
        if (IntroducePageDataList != null && IntroducePageDataList.Count > 0)
            return;

        IntroducePageDataList = new List<IntroducePageData>
        {
            new IntroducePageData
            {
                Topic = _topicText != null ? _topicText.text : string.Empty,
                Content = _contentText != null ? _contentText.text : string.Empty,
                TopicSprite = _topicImage != null ? _topicImage.sprite : null,
                ShowSprite = _showImage != null ? _showImage.sprite : null
            }
        };
    }

    private void SwitchPage(int step)
    {
        if (IntroducePageDataList == null || IntroducePageDataList.Count == 0)
            return;

        _currentPageIndex += step;
        if (_currentPageIndex < 0)
            _currentPageIndex = IntroducePageDataList.Count - 1;
        else if (_currentPageIndex >= IntroducePageDataList.Count)
            _currentPageIndex = 0;

        RefreshCurrentPage(true);
    }

    private void RefreshCurrentPage(bool playImageFade)
    {
        if (IntroducePageDataList == null || IntroducePageDataList.Count == 0)
            return;

        if (_currentPageIndex < 0 || _currentPageIndex >= IntroducePageDataList.Count)
            _currentPageIndex = 0;

        IntroducePageData pageData = IntroducePageDataList[_currentPageIndex];
        RefreshTextWithTyping(_topicText, pageData.Topic, TopicTypingSpeed, ref _topicTypingTask);
        RefreshTextWithTyping(_contentText, pageData.Content, ContentTypingSpeed, ref _contentTypingTask);
        RefreshImages(pageData.TopicSprite, pageData.ShowSprite, playImageFade);
    }

    private void RefreshTextWithTyping(TextMeshProUGUI targetText, string targetContent, float typingSpeed, ref TypingWritingTask typingTask)
    {
        if (targetText == null)
            return;

        if (typingTask != null)
        {
            typingTask.StopTyping();
            typingTask = null;
        }

        targetText.DOKill();
        targetText.text = string.Empty;
        Color color = targetText.color;
        color.a = 0f;
        targetText.color = color;

        targetText.DOFade(1f, TextFadeDuration);
        if (SimpleAnimatorTool.Instance != null)
        {
            typingTask = SimpleAnimatorTool.Instance.AddTypingTask(targetContent ?? string.Empty, targetText, typingSpeed);
        }
    }

    private void RefreshImages(Sprite topicSprite, Sprite showSprite, bool playImageFade)
    {
        if (_topicImage == null && _showImage == null)
            return;

        _imageSequence?.Kill();
        _topicImage?.DOKill();
        _showImage?.DOKill();

        if (!playImageFade)
        {
            if (_topicImage != null)
            {
                _topicImage.sprite = topicSprite;
                Color topicImageColor = _topicImage.color;
                topicImageColor.a = 1f;
                _topicImage.color = topicImageColor;
            }

            if (_showImage != null)
            {
                _showImage.sprite = showSprite;
                Color showImageColor = _showImage.color;
                showImageColor.a = showSprite != null ? 1f : 0f;
                _showImage.color = showImageColor;
            }
            return;
        }

        _imageSequence = DOTween.Sequence();
        if (_topicImage != null)
            _imageSequence.Join(_topicImage.DOFade(0f, ImageFadeDuration));
        if (_showImage != null)
            _imageSequence.Join(_showImage.DOFade(0f, ImageFadeDuration));

        _imageSequence.AppendCallback(() =>
        {
            if (_topicImage != null)
                _topicImage.sprite = topicSprite;
            if (_showImage != null)
                _showImage.sprite = showSprite;
        });

        if (_topicImage != null)
            _imageSequence.Append(_topicImage.DOFade(1f, ImageFadeDuration));
        if (_showImage != null)
            _imageSequence.Join(_showImage.DOFade(showSprite != null ? 1f : 0f, ImageFadeDuration));
    }

    private void StopAllTypingTasks()
    {
        if (_topicTypingTask != null)
        {
            _topicTypingTask.StopTyping();
            _topicTypingTask = null;
        }

        if (_contentTypingTask != null)
        {
            _contentTypingTask.StopTyping();
            _contentTypingTask = null;
        }

        if (_imageInfoTypingTask != null)
        {
            _imageInfoTypingTask.StopTyping();
            _imageInfoTypingTask = null;
        }
    }
}
