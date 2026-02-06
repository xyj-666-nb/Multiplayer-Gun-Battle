using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SceneLoadProgressPanel : BasePanel
{
    #region 核心组件
    public Image ProgressImage;//进度条图片
    public TextMeshProUGUI ProgressNumberText;//进度百分数文本
    public List<string> PromptTextList = new List<string>();//滚动提示文本列表
    public List<string> PromptTextList_English = new List<string>();//滚动提示文本列表
    public TextMeshProUGUI PromptText;//提示文本显示组件
    public TextMeshProUGUI SceneName;
    public Image BackGroundImage;
    public TextMeshProUGUI TopicText;
    #endregion

    #region 加载时长配置（核心：强制2秒平滑进度）
    [Header("加载进度配置")]
    [Tooltip("进度条从0→100%的强制时长（秒）")]
    public float ProgressSmoothDuration = 2f; // 进度条平滑时长，固定2秒
    [Tooltip("面板最低显示总时长（秒），包含进度条+加载完毕提示")]
    public float MinLoadDuration = 2f; // 面板最低显示时长

    private float _displayStartTime; // 进度条开始显示的时间（面板显示时记录）
    private float _targetProgress; // 实际的加载进度（0~1）
    private float _displayProgress; // 用于显示的平滑进度（0~1）
    private bool _isLoadCompleted; // 是否已经加载完成（实际进度到1）
    private bool _isWaitingForMinTime; // 是否正在等待最低时长
    #endregion
    public UnityAction HideAction;

    #region 设置场景名称
    public void SetInfoPanel(string sceneName, Sprite BackGround)
    {
        SceneName.text = sceneName;
        BackGroundImage.sprite = BackGround;
    }
    #endregion

    #region 核心修改：分离实际进度和显示进度
    /// <summary>
    /// 接收实际加载进度
    /// </summary>
    /// <param name="progress">实际加载进度（0~1）</param>
    public void UpdateInfo(float progress)
    {
        // 仅更新目标进度，限制在0~1之间
        _targetProgress = Mathf.Clamp01(progress);

        // 当实际进度达到100%时，标记加载完成
        if (_targetProgress >= 1f && !_isLoadCompleted)
        {
            _isLoadCompleted = true;
            CheckMinLoadTimeAndHide(); // 检查最低显示时长
        }
    }

    /// <summary>
    /// 每帧更新平滑显示进度（核心：强制2秒从0→1）
    /// </summary>
    private void UpdateDisplayProgress()
    {
        if (_displayStartTime == -1) return; // 未开始显示，直接返回

        // 计算面板已显示时长
        float elapsedTime = Time.time - _displayStartTime;

        // 计算「2秒内应该显示的进度」（线性增长，0→1）
        float timeBasedProgress = Mathf.Clamp01(elapsedTime / ProgressSmoothDuration);

        // 显示进度 = 取「时间计算的进度」和「实际进度」的较小值（避免显示进度超过实际加载进度）
        _displayProgress = Mathf.Min(timeBasedProgress, _targetProgress);

        // 更新UI：用平滑的显示进度赋值
        ProgressImage.fillAmount = _displayProgress;
        ProgressNumberText.text = (_displayProgress * 100).ToString("F0") + "%";
    }
    #endregion

    #region 最低时长检查逻辑
    /// <summary>
    /// 检查是否满足最低加载时长，不足则等待，满足则执行隐藏逻辑
    /// </summary>
    private void CheckMinLoadTimeAndHide()
    {
        // 计算已经加载的总时长
        float elapsedTime = Time.time - _displayStartTime;

        // 如果已加载时长 < 最低时长，等待剩余时间；否则直接执行隐藏逻辑
        if (elapsedTime < MinLoadDuration)
        {
            if (!_isWaitingForMinTime)
            {
                _isWaitingForMinTime = true;
                StartCoroutine(WaitForMinLoadTime(MinLoadDuration - elapsedTime));
            }
        }
        else
        {
            ExecuteLoadCompleteLogic();
        }
    }

    /// <summary>
    /// 等待剩余的最低加载时间后，执行加载完成逻辑
    /// </summary>
    private IEnumerator WaitForMinLoadTime(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        _isWaitingForMinTime = false;
        ExecuteLoadCompleteLogic();
    }

    /// <summary>
    /// 加载完成的最终逻辑
    /// </summary>
    private void ExecuteLoadCompleteLogic()
    {
        if (Main.Instance.CurrentLanguageType == LanguageType.Chinese)
        {
            // 原有打字机+倒计时逻辑
            SimpleAnimatorTool.Instance.AddTypingTask(
                "加载完毕",
                TopicText,
                0.1f,
                () =>
                {
                    CountDownManager.Instance.CreateTimer(
                        false,
                        2 * 1000,
                        () => { UImanager.Instance.HidePanel<SceneLoadProgressPanel>(); }
                    );
                }
            );
        }
        else
        {
            // 原有打字机+倒计时逻辑
            SimpleAnimatorTool.Instance.AddTypingTask(
                "Loading complete",
                TopicText,
                0.1f,
                () =>
                {
                    CountDownManager.Instance.CreateTimer(
                        false,
                        2 * 1000,
                        () => { UImanager.Instance.HidePanel<SceneLoadProgressPanel>(); }
                    );
                }
            );
        }
    }

    #endregion

    #region 生命周期函数
    public override void Awake()
    {
        base.Awake();
        ProgressImage.fillAmount = 0;
        ProgressNumberText.text = "0" + "%";
        // 【修复1：Awake中仅初始化，不创建滚动任务（避免提前绑定中文列表）】
        if (PromptText == null)
        {
            Debug.LogError("PromptText组件未赋值！请在Inspector中拖入TextMeshProUGUI组件");
            return;
        }
        // 移除Awake中的滚动任务创建代码，统一移到ShowMe中处理
        Debug.Log("加载面板已初始化，滚动任务将在显示时创建");

        // 注册场景加载进度事件
        EventCenter.Instance.AddEventLister<float>(E_EventType.E_LoadSceneChange, UpdateInfo);
    }

    protected override void Update()
    {
        base.Update();
        // 每帧更新平滑显示进度
        UpdateDisplayProgress();
    }

    public override void Start()
    {
        base.Start();
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        MusicManager.Instance.StopBgm();
        // 重置所有进度状态
        _displayStartTime = Time.time; // 记录进度条开始显示的时间
        _targetProgress = 0f; // 实际进度重置为0
        _displayProgress = 0f; // 显示进度重置为0
        _isLoadCompleted = false;
        _isWaitingForMinTime = false;

        // 重置UI显示
        ProgressImage.fillAmount = 0f;
        ProgressNumberText.text = "0%";
        PromptText.text = string.Empty; // 【新增】重置提示文本，避免残留上一次的内容

        // 【修复2：ShowMe中强制停止旧任务，重新创建新任务（核心切换逻辑）】
        // 先停止已有滚动任务，防止多任务叠加或列表绑定错误
        if (SimpleAnimatorTool.Instance != null && TaskID != -1)
        {
            SimpleAnimatorTool.Instance.StopScrollingTextTask(TaskID);
            TaskID = -1;
        }
        // 判空Main和PromptText，避免空引用
        if (Main.Instance == null || PromptText == null || SimpleAnimatorTool.Instance == null)
        {
            Debug.LogError("滚动任务创建失败：Main单例/PromptText/SimpleAnimatorTool 未赋值");
            return;
        }
        // 根据当前语言，创建对应语言的滚动文本任务
        if (Main.Instance.CurrentLanguageType == LanguageType.Chinese)
        {
            // 中文：使用中文列表
            TaskID = SimpleAnimatorTool.Instance.AddScrollingTextTask(
                PromptTextList,
                PromptText,
                ScrollingType.Random,
                2f // 每条文本停留2秒
            );
            Debug.Log($"创建中文滚动提示任务，任务ID：{TaskID}，共{PromptTextList.Count}条提示");
        }
        else
        {
            // 英文：使用英文列表
            TaskID = SimpleAnimatorTool.Instance.AddScrollingTextTask(
                PromptTextList_English,
                PromptText,
                ScrollingType.Random,
                2f // 每条文本停留2秒
            );
            Debug.Log($"创建英文滚动提示任务，任务ID：{TaskID}，共{PromptTextList_English.Count}条提示");
        }
        // 任务创建失败提示
        if (TaskID == -1)
        {
            Debug.LogError("滚动文本任务创建失败！请检查提示文本列表是否为空或SimpleAnimatorTool是否正常");
        }
    }

    protected override void OnDestroy()
    {
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.RemoveEventLister<float>(E_EventType.E_LoadSceneChange, UpdateInfo);
        }
        if (SimpleAnimatorTool.Instance != null && TaskID != -1)
        {
            SimpleAnimatorTool.Instance.StopScrollingTextTask(TaskID);
        }
        // 停止所有协程，避免内存泄漏
        StopAllCoroutines();
        base.OnDestroy();
    }
    #endregion

    #region 控件响应
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }
    #endregion

    #region 面板显隐以及面板特殊动画
    private int TaskID = -1; // 初始化默认值，避免空引用
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        // 停止滚动文本任务
        if (SimpleAnimatorTool.Instance != null && TaskID != -1)
        {
            SimpleAnimatorTool.Instance.StopScrollingTextTask(TaskID);
            TaskID = -1;
        }
        // 移除事件监听
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.RemoveEventLister<float>(E_EventType.E_LoadSceneChange, UpdateInfo);
        }
        // 停止等待协程
        StopAllCoroutines();
        callback += HideAction;
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
    #endregion
}