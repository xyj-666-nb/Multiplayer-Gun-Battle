using DG.Tweening;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//商店商人的控制脚本
public class MerchantPeople : MonoBehaviour
{
    public static MerchantPeople instance;
    public CanvasGroup SpeechBubbleCanvasGroup;//对话框的CanvasGroup组件
    public TextMeshProUGUI SpeakText;//对话框中的文本组件
    private Sequence CanvasGroupSequence;
    private Transform merchantTrans; // 商人自身Transform（用来抖动）

    [Header("说话抖动参数")]
    public float ScaleY = 1.6f;
    public float DefaultScaleY = 1.5f;
    public float ShakeDuration = 0.2f; // 抖动一次的时间

    [Header("对话框单句显示时间")]
    public float TimeDuration = 2f;//2秒

    [Header("句子之间间隔时间")]
    public float SentenceInterval = 1f; // 播完一句等1秒

    [Header("淡入淡出速度")]
    public float FadeSpeed = 0.3f;

    // 缓存当前说话的任务
    private TypingWritingTask currentTypingTask;
    private Tween currentShakeTween;
    private Tween autoCloseTween;
    private Coroutine textSequenceCoroutine; // 多句子顺序播放

    void Awake()
    {
        instance=this;
        merchantTrans = transform;
        // 初始化默认隐藏
        if (SpeechBubbleCanvasGroup != null)
        {
            SpeechBubbleCanvasGroup.alpha = 0;
            SpeechBubbleCanvasGroup.blocksRaycasts = false;
        }
    }

    //打开对话显示
    public void ShowSpeechBubble()
    {
        if (SpeechBubbleCanvasGroup == null) return;

        // 停止旧动画
        CanvasGroupSequence?.Kill();
        SpeechBubbleCanvasGroup.alpha = 0;

        // 淡入
        SpeechBubbleCanvasGroup.DOFade(1, FadeSpeed).SetLink(gameObject);
        SpeechBubbleCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// 播放单句台词
    /// </summary>
    public void MerchantPeopleSpeak(string Content)
    {
        if (SpeakText == null || string.IsNullOrEmpty(Content)) 
            return;

        StopAllSpeakEffects();

        ShowSpeechBubble();

        SpeakText.text = "";
        currentTypingTask = SimpleAnimatorTool.Instance.AddTypingTask(Content, SpeakText);
        PlaySpeakShakeLoop();

        autoCloseTween = DOVirtual.DelayedCall(TimeDuration, () =>
        {
            HideSpeechBubble();
        }).SetLink(gameObject);
    }

    /// <summary>
    /// 按顺序播放一组台词，句间间隔1秒
    /// </summary>
    /// <param name="contentList">句子列表</param>
    public void PlaySpeechList(List<string> contentList)
    {
        if (contentList == null || contentList.Count == 0) return;

        // 停止旧的序列
        if (textSequenceCoroutine != null)
            StopCoroutine(textSequenceCoroutine);

        textSequenceCoroutine = StartCoroutine(PlaySequence(contentList));
    }

    // 顺序播放协程
    private IEnumerator PlaySequence(List<string> contentList)
    {
        ShowSpeechBubble(); // 一开始就显示气泡

        foreach (string content in contentList)
        {
            // 播当前句
            MerchantPeopleSpeak(content);

            // 等待这句显示完 + 间隔
            yield return new WaitForSeconds(TimeDuration + SentenceInterval);
        }

        // 全部播完关闭
        HideSpeechBubble();
    }

    /// <summary>
    /// 播放说话时的缩放抖动
    /// </summary>
    private void PlaySpeakShakeLoop()
    {
        if (merchantTrans == null) return;

        currentShakeTween = merchantTrans
            .DOScaleY(ScaleY, ShakeDuration)
            .SetLoops(-1, LoopType.Yoyo) // 来回循环
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
    }

    /// <summary>
    /// 停止所有说话相关动画
    /// </summary>
    private void StopAllSpeakEffects()
    {
        // 停止打字
        if (currentTypingTask != null)
            SimpleAnimatorTool.Instance.RemoveTypingTask(currentTypingTask);

        // 停止抖动
        currentShakeTween?.Kill();
        if (merchantTrans != null)
            merchantTrans.localScale = new Vector3(1.5f, DefaultScaleY, 1); // 恢复默认缩放

        // 停止自动关闭
        autoCloseTween?.Kill();
    }

    //关闭对话显示
    public void HideSpeechBubble()
    {
        if (SpeechBubbleCanvasGroup == null) return;

        // 停止所有效果
        StopAllSpeakEffects();

        // 淡出
        CanvasGroupSequence?.Kill();
        SpeechBubbleCanvasGroup.DOFade(0, FadeSpeed).SetLink(gameObject);
        SpeechBubbleCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 外部调用：播放一组句子
    /// </summary>
    public void PlayMultiLine(List<string> lines)
    {
        PlaySpeechList(lines);
    }

    // 打断说话
    public void StopSpeaking()
    {
        if (textSequenceCoroutine != null)
            StopCoroutine(textSequenceCoroutine);

        StopAllSpeakEffects();
        HideSpeechBubble();
    }
}