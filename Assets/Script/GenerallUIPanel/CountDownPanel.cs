using TMPro;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class CountDownPanel : BasePanel
{
    [Header("UI引用")]
    public TextMeshProUGUI TopicText;
    public TextMeshProUGUI CountDownText;

    private UnityAction Callback;
    private float CurrentTime = 0;
    private bool _isCounting = false;

    private Sequence _colorTweenSequence;

    public void InitPanel(string Topic, float Duration, UnityAction CallBack)
    {
        TopicText.text = Topic;
        CurrentTime = Duration;
        Callback = CallBack;

        CountDownText.color = Color.white;
        UpdateCountDownText();
        _isCounting = true;

        SetupColorTween(Duration);
    }

    private void SetupColorTween(float totalDuration)
    {
        _colorTweenSequence?.Kill();
        _colorTweenSequence = DOTween.Sequence();
        _colorTweenSequence.Insert(
            totalDuration * 0.5f,
            CountDownText.DOColor(Color.yellow, totalDuration * 0.1f)
        );

        _colorTweenSequence.Insert(
            totalDuration * 0.8f,
            CountDownText.DOColor(Color.red, totalDuration * 0.1f)
        );

        _colorTweenSequence.SetAutoKill(false);
    }

    #region 生命周期
    protected override void Update()
    {
        base.Update();

        if (_isCounting)
        {
            CurrentTime -= Time.deltaTime;
            if (CurrentTime < 0) CurrentTime = 0;

            UpdateCountDownText();

            if (CurrentTime <= 0)
            {
                OnCountDownFinish();
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _colorTweenSequence?.Kill();
        Callback = null;
    }
    #endregion

    #region 其他逻辑 (保持不变)
    public override void ClickButton(string controlName) { base.ClickButton(controlName); }
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true) { base.HideMe(callback, isNeedDefaultAnimator); }
    public override void ShowMe(bool isNeedDefaultAnimator = true) { base.ShowMe(isNeedDefaultAnimator); }
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion

    #region 倒计时核心逻辑
    private void UpdateCountDownText()
    {
        CountDownText.text = CurrentTime.ToString("F2");
    }

    private void OnCountDownFinish()
    {
        _isCounting = false;
        _colorTweenSequence?.Pause();
        Callback?.Invoke();
        UImanager.Instance.HidePanel<CountDownPanel>();
    }
    #endregion
}