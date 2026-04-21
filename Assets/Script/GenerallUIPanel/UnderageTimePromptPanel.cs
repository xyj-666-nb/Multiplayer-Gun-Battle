using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UnderageTimePromptPanel : BasePanel
{
    private const int AdultAge = 18;

    public CanvasGroup ExpandCanvasGroup;
    public TextMeshProUGUI TimeText1;
    public TextMeshProUGUI Text2;

    private Sequence _expandSequence;
    private bool _isExpanded = true;
    private bool _isPanelVisible;
    private int _lastRemainingTimeSeconds = int.MinValue;
    private float _text2OriginalFontSize;

    public override void Awake()
    {
        base.Awake();
        CacheReferences();
    }

    public override void Start()
    {
        base.Start();

        if (Text2 != null)
        {
            _text2OriginalFontSize = Text2.fontSize;
        }

        SetExpandedState(true, true);
        RefreshPanelVisibleState(true);
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);

        if (controlName == "OpenButton")
        {
            SetExpandedState(!_isExpanded, false);
        }
    }

    protected override void Update()
    {
        base.Update();
        RefreshPanelVisibleState(false);

        if (!_isPanelVisible)
        {
            return;
        }

        UpdateTimeDisplay();
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }

    private void CacheReferences()
    {
        if (ExpandCanvasGroup == null)
        {
            foreach (Transform child in transform)
            {
                if (child == null || child.name == "OpenButton")
                {
                    continue;
                }

                ExpandCanvasGroup = child.GetComponent<CanvasGroup>();
                if (ExpandCanvasGroup == null)
                {
                    ExpandCanvasGroup = child.gameObject.AddComponent<CanvasGroup>();
                }
                break;
            }
        }

        if (Text2 == null && controlDic.TryGetValue("Text2", out var text2Control))
        {
            Text2 = text2Control as TextMeshProUGUI;
        }

        if (Text2 == null && controlDic.TryGetValue("OpenButton", out var openButtonControl))
        {
            Text2 = openButtonControl.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (TimeText1 == null && controlDic.TryGetValue("TimeText1", out var timeTextControl))
        {
            TimeText1 = timeTextControl as TextMeshProUGUI;
        }

        if (TimeText1 == null && ExpandCanvasGroup != null)
        {
            var texts = ExpandCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts != null && texts.Length > 0)
            {
                TimeText1 = texts[texts.Length - 1];
            }
        }
    }

    private void RefreshPanelVisibleState(bool forceRefresh)
    {
        bool shouldShow = IsUnderagePlayer();
        if (!forceRefresh && shouldShow == _isPanelVisible)
        {
            return;
        }

        _isPanelVisible = shouldShow;

        if (shouldShow)
        {
            SimpleShowPanel();
            SetExpandedState(_isExpanded, true);
            UpdateTimeDisplay();
        }
        else
        {
            _lastRemainingTimeSeconds = int.MinValue;
            SetExpandedState(true, true);
            SimpleHidePanel();
        }
    }

    private bool IsUnderagePlayer()
    {
        if (TapTapGameLogin.Instance == null)
        {
            return false;
        }

        int ageRange = TapTapGameLogin.Instance.CurrentAgeRange;
        return ageRange >= 0 && ageRange < AdultAge;
    }

    private void UpdateTimeDisplay()
    {
        if (TapTapGameLogin.Instance == null)
        {
            return;
        }

        int remainingSeconds = Mathf.Max(TapTapGameLogin.Instance.CurrentRemainingTimeSeconds, 0);
        if (_lastRemainingTimeSeconds == remainingSeconds)
        {
            return;
        }

        _lastRemainingTimeSeconds = remainingSeconds;
        string formattedTime = FormatRemainingTime(remainingSeconds);

        if (TimeText1 != null)
        {
            TimeText1.text = formattedTime;
        }

        if (Text2 != null && !_isExpanded)
        {
            Text2.text = formattedTime;
        }
    }

    private void SetExpandedState(bool isExpanded, bool instant)
    {
        _isExpanded = isExpanded;

        if (ExpandCanvasGroup != null)
        {
            ExpandCanvasGroup.blocksRaycasts = isExpanded;
            ExpandCanvasGroup.interactable = isExpanded;

            if (instant)
            {
                ExpandCanvasGroup.alpha = isExpanded ? 1f : 0f;
            }
            else
            {
                SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(
                    ExpandCanvasGroup,
                    ref _expandSequence,
                    isExpanded,
                    () => { });
            }
        }

        if (Text2 != null)
        {
            if (isExpanded)
            {
                Text2.fontSize = _text2OriginalFontSize;
                Text2.text = ">";
            }
            else
            {
                Text2.fontSize = _text2OriginalFontSize * 0.5f;
                Text2.text = FormatRemainingTime(Mathf.Max(TapTapGameLogin.Instance != null ? TapTapGameLogin.Instance.CurrentRemainingTimeSeconds : 0, 0));
            }
        }
    }

    private string FormatRemainingTime(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;

        if (hours > 0)
        {
            return $"{hours:00}:{minutes:00}";
        }

        return $"{minutes:00}分";
    }
}
