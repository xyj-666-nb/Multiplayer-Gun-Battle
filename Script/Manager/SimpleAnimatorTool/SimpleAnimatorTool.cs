using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Splines;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using static SimpleAnimatorTool;

/// <summary>
/// 该工具脚本脚本目前封装了
/// 1.对Float值的渐变操作
/// 2.对Spines插件进行一个轨迹封装，你可以去让物体沿着你设计的轨道运行
/// 3.对文字提供打字机效果
/// 4.提供通用渐变
/// 5.提供滚动播放文本列表
/// 6.提供循环渐变功能
/// </summary>
public class SimpleAnimatorTool : SingleMonoAutoBehavior<SimpleAnimatorTool>
{
    #region 1. Float插值滑动功能
    // 存储所有插值任务
    private List<FloatLerpTask> _lerpTasks = new List<FloatLerpTask>();

    #region Float插值任务结构体
    /// <summary>
    /// 单个float插值任务的状态
    /// </summary>
    private struct FloatLerpTask
    {
        public float startValue;       // 起始值
        public float targetValue;      // 目标值
        public float totalDuration;    // 总时长
        public float elapsedTime;      // 已流逝时间
        public EaseType easeType;      // 缓动类型
        public Action<float> onUpdate; // 插值更新回调
        public Action onComplete;      // 插值完成回调
    }
    #endregion

    #region 外部接口：启动Float插值
    /// <summary>
    /// 启动float插值,这里可以在Update里面获取剩余时间
    /// </summary>
    /// <param name="startValue">起始值</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="totalDuration">总插值时长）</param>
    /// <param name="onUpdate">插值更新回调</param>
    /// <param name="onComplete">插值完成回调）</param>
    /// <param name="easeType">缓动类型</param>
    public void StartFloatLerp(float startValue, float targetValue, float totalDuration, Action<float> onUpdate, Action onComplete = null, EaseType easeType = EaseType.Linear)
    {
        if (onUpdate == null)
        {
            Debug.LogError("插值更新回调 onUpdate 不能为空！");
            return;
        }
        if (totalDuration <= 0)
        {
            // 时长为0，直接返回目标值并触发完成
            onUpdate.Invoke(targetValue);
            onComplete?.Invoke();
            return;
        }

        // 创建插值任务，加入任务列表
        FloatLerpTask newTask = new FloatLerpTask
        {
            startValue = startValue,
            targetValue = targetValue,
            totalDuration = totalDuration,
            elapsedTime = 0,
            easeType = easeType,
            onUpdate = onUpdate,
            onComplete = onComplete
        };
        _lerpTasks.Add(newTask);
    }
    #endregion

    #region 内部逻辑：更新Float插值任务
    /// <summary>
    /// 内部自动更新：依托Update生命周期，无需外部干预
    /// </summary>
    public void UpdateFloatLerpTask()
    {
        if (_lerpTasks.Count > 0)
        {
            for (int i = _lerpTasks.Count - 1; i >= 0; i--)
            {
                FloatLerpTask task = _lerpTasks[i];
                task.elapsedTime += Time.deltaTime;
                if (task.elapsedTime >= task.totalDuration)
                {
                    task.onUpdate.Invoke(task.targetValue);//最后执行一次Update并把最终值传入进去
                    task.onComplete?.Invoke();//检查是否有完成触发，存在就触发
                    _lerpTasks.RemoveAt(i);//移除当前任务
                    continue;
                }

                float t = task.elapsedTime / task.totalDuration;
                t = ApplyEase(t, task.easeType);

                float currentValue = Mathf.Lerp(task.startValue, task.targetValue, t);
                task.onUpdate.Invoke(currentValue);//传入当前的更新值

                _lerpTasks[i] = task;
            }
        }
    }
    #endregion

    #region 内部逻辑：缓动函数计算
    /// <summary>
    /// 简单缓动函数
    /// </summary>
    /// <param name="t">基础插值比例（0 ~ 1）</param>
    /// <param name="easeType">缓动类型</param>
    /// <returns>修正后的缓动比例</returns>
    public float ApplyEase(float t, EaseType easeType)
    {
        switch (easeType)
        {
            case EaseType.Linear:
                return t;
            case EaseType.EaseIn:
                return t * t;
            case EaseType.EaseOut:
                return 1 - (1 - t) * (1 - t);
            case EaseType.EaseInOut:
                return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
            default:
                return t;
        }
    }
    #endregion

    #region 辅助接口：终止Float插值
    /// <summary>
    /// 终止所有float插值任务
    /// </summary>
    public void StopAllFloatLerp()
    {
        _lerpTasks.Clear();
    }
    #endregion

    #region 枚举：基础枚举定义
    /// <summary>
    /// 缓动类型枚举
    /// </summary>
    public enum EaseType
    {
        Linear,     // 匀速
        EaseIn,     // 缓入
        EaseOut,    // 缓出
        EaseInOut   // 缓入缓出
    }
    #endregion
    #endregion

    #region 2. 样条曲线轨迹运动功能
    #region 曲线任务管理字段
    public List<SplineLerpTask> SplineLerpTaskList = new List<SplineLerpTask>();
    public List<SplineLerpTask> RemoveSplineLerpTaskList = new List<SplineLerpTask>();//临时寄存待移除列表
    #endregion

    #region 曲线运动的数据结构体（移除MoveMode，保留核心参数）
    /// <summary>
    /// 曲线运动任务参数结构体
    /// </summary>
    public struct struct_SplineLerpTaskInfo
    {
        public GameObject obj; // 待移动的物体
        public SplineContainer splineContainer; // 样条容器
        public float totalMoveDuration;//单次插值（从头到尾/从尾到头）的总时间
        public bool isFaceSplineOutside;//确定转向模式
        public bool Reverse;//首次插值是否反向
        public bool IsLoop;//是否循环插值（到头后反向再次插值）
        public bool IsNeedImmediate;//撤销的时候是否立即撤销
        public EaseType easeType; //缓动类型
        public bool IsContinuous;//是否连续（只在循环模式有效果）
    }
    #endregion

    #region 外部接口：添加曲线运动任务（仅接收结构体参数）
    /// <summary>
    /// 添加一个曲线移动任务,返还一个任务包
    /// </summary>
    /// <param name="taskInfo">曲线运动任务完整参数结构体</param>
    /// <returns>曲线任务包</returns>
    public SplineLerpTask AddSplineLerpTask(struct_SplineLerpTaskInfo taskInfo)
    {
        // 参数校验
        if (taskInfo.totalMoveDuration <= 0)
        {
            Debug.LogError("曲线单次移动时间必须为正数！");
            return null;
        }
        if (taskInfo.obj == null || taskInfo.splineContainer == null)
        {
            Debug.LogError("物体或样条容器不能为空！");
            return null;
        }

        // 创建并初始化任务
        SplineLerpTask task = new SplineLerpTask();
        task.Init(taskInfo);
        SplineLerpTaskList.Add(task);
        return task;
    }
    #endregion

    #region 外部接口：移除曲线运动任务
    /// <summary>
    /// 根据任务包进行撤销任务
    /// </summary>
    /// <param name="task">待移除的曲线任务</param>
    public void RemoveSplineLerpTask(SplineLerpTask task)
    {
        if (task == null)
            return;

        if (SplineLerpTaskList.Contains(task))
        {
            if (task.IsNeedImmediate)
            {
                // 立即移除任务
                SplineLerpTaskList.Remove(task);
                if (RemoveSplineLerpTaskList.Contains(task))
                {
                    RemoveSplineLerpTaskList.Remove(task);
                }
            }
            else
            {
                // 非立即移除，加入待移除列表
                if (!RemoveSplineLerpTaskList.Contains(task) && task.IsLoop == false)
                {
                    RemoveSplineLerpTaskList.Add(task);
                }
            }
        }
    }
    #endregion

    #region 内部逻辑：更新所有曲线运动任务
    /// <summary>
    /// 统一更新所有曲线移动任务
    /// </summary>
    public void UpdateSplineLerpTask()
    {
        if (SplineLerpTaskList.Count <= 0)
            return;

        // 倒序遍历，避免移除任务导致索引异常
        for (int i = SplineLerpTaskList.Count - 1; i >= 0; i--)
        {
            SplineLerpTask task = SplineLerpTaskList[i];

            // 物体已销毁或失活，直接移除
            if (task.Obj == null || !task.Obj.activeSelf)
            {
                SplineLerpTaskList.RemoveAt(i);
                continue;
            }

            // 更新物体位置和旋转
            task.MoveObjAloneSpline();
        }

        // 处理待移除列表
        if (RemoveSplineLerpTaskList.Count > 0)
        {
            foreach (var task in RemoveSplineLerpTaskList)
            {
                SplineLerpTaskList.Remove(task);
            }
            RemoveSplineLerpTaskList.Clear();
        }
    }
    #endregion

    #region 辅助接口：清空所有曲线运动任务
    /// <summary>
    /// 移除所有曲线移动任务
    /// </summary>
    public void ClearAllSplineLerpTask()
    {
        SplineLerpTaskList.Clear();
        RemoveSplineLerpTaskList.Clear();
    }
    #endregion
    #endregion

    #region 3. 文字打字机效果功能

    private List<TypingWritingTask> typingWritingTasksList = new List<TypingWritingTask>();

    #region 添加打字机任务
    /// <summary>
    /// 添加并启动打字机任务
    /// </summary>
    /// <param name="targetText">目标文本</param>
    /// <param name="showText">显示文本的TMP组件</param>
    /// <param name="speed">打字速度（秒/字符，数值越小越快，默认0.05）</param>
    /// <param name="onComplete">完成回调</param>
    /// <returns>创建的打字任务</returns>
    public TypingWritingTask AddTypingTask(string targetText, TextMeshProUGUI showText, float speed = 0.05f, UnityAction onComplete = null)
    {
        var oldTask = typingWritingTasksList.Find(t => t.ShowText == showText);
        if (oldTask != null) RemoveTypingTask(oldTask);

        // 原有逻辑不变
        if (showText == null)
        {
            Debug.LogError("TextMeshProUGUI组件不能为空！");
            return null;
        }
        if (string.IsNullOrEmpty(targetText))
        {
            showText.text = string.Empty;
            onComplete?.Invoke();
            return null;
        }
        if (speed <= 0) speed = 0.05f;

        TypingWritingTask newTask = PoolManage.Instance.GetObj<TypingWritingTask>();
        newTask.init(targetText, showText, speed, onComplete);

        typingWritingTasksList.Add(newTask);
        newTask.StartTyping();
        return newTask;
    }
    #endregion

    #region 移除打字机任务
    /// <summary>
    /// 移除打字机任务
    /// </summary>
    /// <param name="task">待移除的任务</param>
    public void RemoveTypingTask(TypingWritingTask task)
    {
        if (task == null)
            return;

        task.StopTyping(); // 先停止协程
        if (typingWritingTasksList.Contains(task))
        {
            typingWritingTasksList.Remove(task);
            PoolManage.Instance.PushObj(task);
        }
    }

    #endregion

    #region 清空所有打字任务

    /// <summary>
    /// 清空所有打字任务
    /// </summary>
    public void ClearAllTypingTasks()
    {
        foreach (var task in typingWritingTasksList)
        {
            task.StopTyping();
            PoolManage.Instance.PushObj(task);
        }
        typingWritingTasksList.Clear();
    }
    #endregion

    #endregion

    #region 4. 循环淡入淡出动画功能

    #region 循环淡入淡出任务列表
    [SerializeField] private List<FadeLoopTask> fadeLoopTasksList = new List<FadeLoopTask>();// 循环淡入淡出任务列表
    #endregion

    #region 添加循环淡入淡出任务
    /// <summary>
    /// 添加并启动UI循环淡入淡出动画任务
    /// </summary>
    /// <param name="targetGraphic">目标UI组件（TMP/Image/Text等，继承自Graphic）</param>
    /// <param name="fadeInDuration">淡入时长（秒）</param>
    /// <param name="fadeOutDuration">淡出时长（秒）</param>
    /// <param name="waitTime">每次淡入完成后等待的时间（秒）</param>
    /// <param name="easeType">缓动类型（默认线性）</param>
    /// <param name="loopType">循环模式（默认Yoyo：淡入→淡出→淡入...）</param>
    /// <returns>创建的淡入淡出任务</returns>
    public FadeLoopTask AddFadeLoopTask(Graphic targetGraphic,
                                       float fadeInDuration = 0.5f,
                                       float fadeOutDuration = 0.5f,
                                       float waitTime = 0.5f,
                                       Ease easeType = Ease.Linear,
                                       LoopType loopType = LoopType.Yoyo)
    {
        // 前置参数校验
        if (targetGraphic == null)
        {
            Debug.LogError("AddFadeLoopTask：目标Graphic组件不能为空！");
            return null;
        }
        if (fadeInDuration <= 0)
        {
            Debug.LogWarning("AddFadeLoopTask：淡入时长不能≤0，已自动设为0.5秒");
            fadeInDuration = 0.5f;
        }
        if (fadeOutDuration <= 0)
        {
            Debug.LogWarning("AddFadeLoopTask：淡出时长不能≤0，已自动设为0.5秒");
            fadeOutDuration = 0.5f;
        }
        if (waitTime < 0)
        {
            Debug.LogWarning("AddFadeLoopTask：等待时间不能为负，已自动设为0秒");
            waitTime = 0f;
        }

        // 创建任务并初始化参数
        FadeLoopTask newTask = PoolManage.Instance.GetObj<FadeLoopTask>();
        newTask.InitTask(targetGraphic, fadeInDuration, fadeOutDuration, waitTime, easeType, loopType);

        // 添加到任务列表并启动动画
        fadeLoopTasksList.Add(newTask);
        newTask.StartAnimatorLoop();

        return newTask;
    }
    #endregion

    #region 停止/移除循环淡入淡出任务
    /// <summary>
    /// 停止单个循环淡入淡出任务（不移除）
    /// </summary>
    /// <param name="task">目标任务</param>
    public void StopFadeLoopTask(FadeLoopTask task)
    {
        if (task == null)
            return;
        task.StopAnimatorLoop();
    }

    /// <summary>
    /// 移除单个循环淡入淡出任务（先停止动画）
    /// </summary>
    /// <param name="task">目标任务</param>
    public void RemoveFadeLoopTask(FadeLoopTask task)
    {
        if (task == null)
            return;

        // 先停止动画，再移除任务
        task.StopAnimatorLoop();
        if (fadeLoopTasksList.Contains(task))
        {
            fadeLoopTasksList.Remove(task);
            PoolManage.Instance.PushObj(task);
        }
    }

    /// <summary>
    /// 清空所有循环淡入淡出任务
    /// </summary>
    public void ClearAllFadeLoopTasks()
    {
        foreach (var task in fadeLoopTasksList)
        {
            task.StopAnimatorLoop();
            PoolManage.Instance.PushObj(task);
        }
        fadeLoopTasksList.Clear();
    }
    #endregion

    #endregion

    #region 5.通用淡入淡出方法
    /// <summary>
    /// 通用默认动画（淡入/淡出）
    /// </summary>
    /// <param name="canvasGroup">目标面板的CanvasGroup</param>
    /// <param name="sequence">目标面板的动画序列</param>
    /// <param name="isShow">显示/隐藏</param>
    /// <param name="callBack">动画完成回调</param>
    public void CommonFadeDefaultAnima(CanvasGroup canvasGroup, ref Sequence sequence, bool isShow, UnityAction callBack)
    {
        // 空值校验
        if (canvasGroup == null)
        {
            Debug.LogError($"CanvasGroup 为空！");
            callBack?.Invoke();
            return;
        }

        // 销毁残留动画 + 重建序列
        sequence?.Kill();
        sequence = DOTween.Sequence();

        if (isShow)
        {
            // 通用淡入逻辑：激活面板 → 透明度0→1
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0;

            sequence.Append(
                DOTween.To(() => canvasGroup.alpha,
                           v => canvasGroup.alpha = v,
                           1,
                           0.3f)
                .SetEase(Ease.OutQuad)
            )
            .OnComplete(() =>
            {
                canvasGroup.alpha = 1; // 兜底
                callBack?.Invoke();
            });
        }
        else
        {
            // 通用淡出逻辑：确保面板激活 → 透明度1→0→ 隐藏
            if (!canvasGroup.gameObject.activeSelf)
            {
                canvasGroup.gameObject.SetActive(true);
            }

            sequence.Append(
                DOTween.To(() => canvasGroup.alpha,
                           v => canvasGroup.alpha = v,
                           0,
                           0.3f)
                .SetEase(Ease.InQuad)
            )
            .OnComplete(() =>
            {
                canvasGroup.alpha = 0; // 兜底
                if (canvasGroup.gameObject.activeSelf)
                {
                    canvasGroup.gameObject.SetActive(false);
                }
                callBack?.Invoke();
            });
        }
    }
    #endregion

    #region 6.滚动文本播放(轮番播放stringList里面的预制文本)
    public int _nextScrollTaskIndex = 1;// 滚动任务自增索引
    public List<ScrollingTextTask> ScrollingTextTaskList = new List<ScrollingTextTask>();

    /// <summary>
    /// 添加并启动滚动文本播放任务
    /// </summary>
    /// <param name="textList">播放的文本列表</param>
    /// <param name="showText">显示文本的TMP组件</param>
    /// <param name="scrollType">滚动模式（顺序/随机）</param>
    /// <param name="stayTime">每条文本停留时间（秒）</param>
    /// <param name="fadeInDuration">淡入时长（秒）</param>
    /// <param name="fadeOutDuration">淡出时长（秒）</param>
    /// <param name="easeType">缓动类型</param>
    /// <returns>任务唯一索引（用于停止任务）</returns>
    public int AddScrollingTextTask(List<string> textList, TextMeshProUGUI showText, ScrollingType scrollType,
                                float stayTime = 1f, float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f,
                                Ease easeType = Ease.Linear)
    {
        // 停止同目标的旧任务
        var oldTask = ScrollingTextTaskList.Find(t => t.ShowText == showText);
        if (oldTask != null)
        {
            Debug.Log($"发现同目标的旧任务，先停止：{oldTask.Index}");
            StopScrollingTextTask(oldTask.Index);
        }

        ScrollingTextTask newTask = PoolManage.Instance.GetObj<ScrollingTextTask>();//对象池获取对象
        newTask.InitTask(textList, showText, scrollType, _nextScrollTaskIndex, stayTime, fadeInDuration, fadeOutDuration, easeType);

        // 加入任务列表并启动
        ScrollingTextTaskList.Add(newTask);
        newTask.StartTask();

        // 索引自增
        _nextScrollTaskIndex++;
        Debug.Log($"=== 滚动文本任务创建完成，返回索引：{newTask.Index} ===");
        return newTask.Index;
    }

    /// <summary>
    /// 停止指定索引的滚动文本任务
    /// </summary>
    /// <param name="taskIndex">任务唯一索引</param>
    public void StopScrollingTextTask(int taskIndex)
    {
        var targetTask = ScrollingTextTaskList.Find(t => t.Index == taskIndex);
        if (targetTask != null)
        {
            targetTask.StopTask();
            ScrollingTextTaskList.Remove(targetTask);
            PoolManage.Instance.PushObj<ScrollingTextTask>(targetTask);//回收
        }
        else
        {
            Debug.LogWarning($"StopScrollingTextTask：未找到索引为 {taskIndex} 的滚动文本任务！");
        }
    }

    /// <summary>
    /// 停止所有滚动文本任务
    /// </summary>
    public void StopAllScrollingTextTasks()
    {
        foreach (var task in ScrollingTextTaskList)
        {
            task.StopTask();
            //回收
            PoolManage.Instance.PushObj<ScrollingTextTask>(task);//回收
        }
        ScrollingTextTaskList.Clear();
    }
    #endregion

    #region 生命周期更新
    private void Update()
    {
        UpdateFloatLerpTask();
        UpdateSplineLerpTask();
        fadeLoopTasksList.RemoveAll(task =>
            task == null ||
            task.TargetGraphic == null ||
            !task.TargetGraphic.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// 销毁时清空所有动画任务，防止内存泄漏
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearAllFadeLoopTasks();
        // 额外清理DOTween残留动画
        DOTween.KillAll();
    }
    #endregion

}

#region 打字机任务数据类
/// <summary>
/// 打字机任务类
/// </summary>
public class TypingWritingTask : IPoolObject
{
    private Coroutine _currentTypingCoroutine; // 协程引用
    public string TargetText;
    public TextMeshProUGUI ShowText;
    public float CurrentProgress;
    public float CurrentSpeed;
    public UnityAction CallBack;
    public bool IsInTyping { get; private set; }

    public void init(string targetText, TextMeshProUGUI showText, float speed, UnityAction onComplete)
    {
        TargetText = targetText;
        ShowText = showText;
        CurrentSpeed = speed;
        CurrentProgress = 0f;
        CallBack = onComplete;
        IsInTyping = false;
        _currentTypingCoroutine = null;
    }

    // 启动打字：先停止旧协程，再启动新协程
    public void StartTyping()
    {
        // 已在打字且文本完成，直接返回
        if (IsInTyping && ShowText.text == TargetText)
            return;
        StopTyping();
        IsInTyping = true;
        _currentTypingCoroutine = MonoMange.Instance.StartCoroutine(TypingWriting());
    }

    public void StopTyping()
    {
        // 重置状态
        IsInTyping = false;
        if (_currentTypingCoroutine != null)
        {
            MonoMange.Instance.StopCoroutine(_currentTypingCoroutine);
        }
        _currentTypingCoroutine = null;
    }

    // 打字机核心协程
    private IEnumerator TypingWriting()
    {
        float interval = Mathf.Max(0.01f, CurrentSpeed);
        string content = "";
        ShowText.text = content;
        for (int i = 0; i < TargetText.Length; i++)
        {
            // 中途停止则退出协程
            if (!IsInTyping)
                yield break;

            content += TargetText[i];
            ShowText.text = content;
            CurrentProgress = (i + 1f) / TargetText.Length;
            yield return new WaitForSeconds(interval);
        }

        // 打字完成，重置状态
        IsInTyping = false;
        CallBack?.Invoke();
        // 自动移除任务
        if (SimpleAnimatorTool.Instance != null)
        {
            SimpleAnimatorTool.Instance.RemoveTypingTask(this);
        }
    }

    // 重置数据
    public void ReSetDate()
    {
        // 安全停止协程
        if (_currentTypingCoroutine != null && SimpleAnimatorTool.Instance != null)
        {
            SimpleAnimatorTool.Instance.StopCoroutine(_currentTypingCoroutine);
        }
        // 重置所有状态
        _currentTypingCoroutine = null;
        IsInTyping = false;
        CurrentSpeed = 0f;
        CallBack = null;
        CurrentProgress = 0;
        ShowText = null;
        TargetText = null; // 补充重置目标文本
    }
}
#endregion

#region 循环淡入淡出动画任务类
/// <summary>
/// 循环淡入淡出动画任务类
/// </summary>
[Serializable]
public class FadeLoopTask : IPoolObject
{
    #region 任务控制变量
    public Graphic TargetGraphic;// 目标UI组件

    public float FadeInDuration = 0.5f;// 淡入时长（秒

    public float FadeOutDuration = 0.5f;// 淡出时长（秒）

    public float WaitTime = 1f;// 淡入完成后等待的时间（秒）

    public Ease EaseType = Ease.Linear;// 缓动类型

    public LoopType LoopType = LoopType.Yoyo;// 循环模式（Yoyo/Restart）

    public Sequence FadeSequence;//动画序列

    public bool IsRunning;// 是否正在运行动画

    public void InitTask(Graphic targetGraphic, float fadeInDuration, float fadeOutDuration, float waitTime, Ease easeType, LoopType loopType)
    {
        TargetGraphic = targetGraphic;
        FadeInDuration = fadeInDuration;
        FadeOutDuration = fadeOutDuration;
        WaitTime = waitTime;
        EaseType = easeType;
        LoopType = loopType;
    }

    public void ReSetDate()
    {
        TargetGraphic = null;
        FadeInDuration = 0.5f;
        FadeOutDuration = 0.5f;
        WaitTime = 1f;
        EaseType = Ease.Linear;
        LoopType = LoopType.Yoyo;
        FadeSequence.Kill();
        IsRunning = false;
    }

    #endregion

    #region 启动动画方法
    /// <summary>
    /// 启动循环淡入淡出动画
    /// </summary>
    public void StartAnimatorLoop()
    {
        // 先停止原有动画，避免叠加
        StopAnimatorLoop();

        // 校验目标组件是否有效
        if (TargetGraphic == null)
        {
            Debug.LogError("StartAnimatorLoop：目标Graphic组件为空，无法启动动画！");
            return;
        }

        // 重置目标组件的初始透明度为不透明
        Color targetColor = TargetGraphic.color;
        targetColor = ColorManager.SetColorAlpha(targetColor, 1f);
        TargetGraphic.color = targetColor;

        // 构建动画序列：淡入→等待→淡出
        FadeSequence = DOTween.Sequence();
        // 淡入（从0→1）
        FadeSequence.Append(TargetGraphic.DOFade(1f, FadeInDuration).SetEase(EaseType));
        // 等待指定时间
        if (WaitTime > 0)
        {
            FadeSequence.AppendInterval(WaitTime);
        }
        // 淡出（从1→0）
        FadeSequence.Append(TargetGraphic.DOFade(0f, FadeOutDuration).SetEase(EaseType));

        // 设置循环模式
        FadeSequence.SetLoops(-1, LoopType);
        // 绑定到目标对象：对象销毁时自动停止动画，防止内存泄漏
        FadeSequence.SetLink(TargetGraphic.gameObject);
        // 标记运行状态
        IsRunning = true;

        // 启动序列
        FadeSequence.Play();
    }

    #endregion

    #region 停止动画的方法
    /// <summary>
    /// 停止循环淡入淡出动画
    /// </summary>
    public void StopAnimatorLoop()
    {
        // 停止并清理动画序列
        if (FadeSequence != null && FadeSequence.IsPlaying())
        {
            FadeSequence.Kill(); // 销毁序列
        }
        FadeSequence = null;
        // 重置运行状态
        IsRunning = false;

        if (TargetGraphic != null)
        {
            Color resetColor = TargetGraphic.color;
            resetColor = ColorManager.SetColorAlpha(resetColor, 1f);
            TargetGraphic.color = resetColor;
        }
    }
    #endregion
}
#endregion

#region  滚动文本播放任务类
/// <summary>
/// 滚动文本播放任务类
/// </summary>
#region  滚动文本播放任务类
/// <summary>
/// 滚动文本播放任务类
/// </summary>
public class ScrollingTextTask : IPoolObject
{
    // 核心配置参数
    public List<string> PrefabsStringList;//预制播放文本列表
    public TextMeshProUGUI ShowText;//显示文本的TMP组件
    public Sequence TextAnimaSequence;//文本动画序列
    public ScrollingType scrollingType;//滚动模式
    public int Index;//唯一标识（由管理器分配）
    public float StayTime = 1f;//每条文本停留时间（秒）
    public float FadeInDuration = 0.5f;//淡入时长（秒）
    public float FadeOutDuration = 0.5f;//淡出时长（秒）
    public Ease EaseType = Ease.Linear;//缓动类型

    // 内部状态
    private int _currentTextIndex = -1;//当前播放的文本索引

    #region 构造函数
    /// <summary>
    /// 构造函数：初始化滚动文本任务
    /// </summary>
    /// <param name="textList">文本列表</param>
    /// <param name="showText">显示文本的TMP组件</param>
    /// <param name="scrollType">滚动模式</param>
    /// <param name="index">唯一标识</param>
    /// <param name="stayTime">文本停留时间</param>
    /// <param name="fadeInTime">淡入时长</param>
    /// <param name="fadeOutTime">淡出时长</param>
    /// <param name="easeType">缓动类型</param>
    public void InitTask(List<string> textList, TextMeshProUGUI showText, ScrollingType scrollType, int index,
                         float stayTime = 1f, float fadeInTime = 0.5f, float fadeOutTime = 0.5f, Ease easeType = Ease.Linear)
    {
        // 仅赋值基础参数，不获取单例
        PrefabsStringList = textList ?? new List<string>();
        ShowText = showText;
        scrollingType = scrollType;
        StayTime = Mathf.Max(0f, stayTime);
        FadeInDuration = Mathf.Max(0.1f, fadeInTime);
        FadeOutDuration = Mathf.Max(0.1f, fadeOutTime);
        EaseType = easeType;
        Index = index;
    }
    #endregion

    #region 启动滚动播放任务
    public void StartTask()
    {
        if (ShowText == null)
        {
            Debug.LogError("滚动文本任务：显示文本的TMP组件为空！");
            return;
        }
        if (PrefabsStringList == null || PrefabsStringList.Count == 0)
        {
            Debug.LogError("滚动文本任务：播放文本列表为空！");
            return;
        }

        // 停止原有序列（防止叠加）
        StopTask(false);
        PlayNextText();
    }
    #endregion

    #region 播放下一条文本
    private void PlayNextText()
    {
        // 增加异常捕获，确保日志能打印
        try
        {
            string nextText = getString();
            // 强制赋值文本并重置透明度
            ShowText.text = nextText;
            Color textColor = ShowText.color;
            textColor.a = 0;
            ShowText.color = textColor;

            // 销毁旧序列，避免叠加
            if (TextAnimaSequence != null && TextAnimaSequence.IsActive())
            {
                TextAnimaSequence.Kill();
            }

            // 重新创建序列
            TextAnimaSequence = DOTween.Sequence();
            TextAnimaSequence.Append(ShowText.DOFade(1f, FadeInDuration).SetEase(EaseType));
            if (StayTime > 0)
            {
                TextAnimaSequence.AppendInterval(StayTime);
            }
            TextAnimaSequence.Append(ShowText.DOFade(0f, FadeOutDuration).SetEase(EaseType));
            TextAnimaSequence.OnComplete(() =>
            {
                PlayNextText();
            });
            // 绑定到文本对象，防止对象销毁导致序列异常
            TextAnimaSequence.SetLink(ShowText.gameObject);
            // 强制播放序列
            TextAnimaSequence.Play();
            Debug.Log("文本序列已启动");
        }
        catch (Exception e)
        {
            Debug.LogError($"PlayNextText执行异常：{e.Message}\n{e.StackTrace}");
        }
    }
    #endregion

    #region 获取下一条文本
    public string getString()
    {
        if (PrefabsStringList == null || PrefabsStringList.Count == 0)
        {
            Debug.LogError("getString：文本列表为空");
            return string.Empty;
        }

        if (scrollingType == ScrollingType.Random)
        {
            int randomIndex = _currentTextIndex;
            while (randomIndex == _currentTextIndex && PrefabsStringList.Count > 1)
            {
                randomIndex = UnityEngine.Random.Range(0, PrefabsStringList.Count);
            }
            _currentTextIndex = randomIndex;
        }
        else
        {
            _currentTextIndex = (_currentTextIndex + 1) % PrefabsStringList.Count;
        }

        Debug.Log($"getString：当前索引{_currentTextIndex}，文本：{PrefabsStringList[_currentTextIndex]}");
        return PrefabsStringList[_currentTextIndex];
    }
    #endregion

    #region 停止滚动播放任务
    /// <summary>
    /// 停止滚动播放
    /// </summary>
    /// <param name="resetState">是否重置文本状态</param>
    public void StopTask(bool resetState = true)
    {
        // 停止并销毁动画序列
        if (TextAnimaSequence != null && TextAnimaSequence.IsPlaying())
        {
            TextAnimaSequence.Kill();
        }
        TextAnimaSequence = null;

        // 重置文本状态
        if (resetState && ShowText != null)
        {
            Color resetColor = ShowText.color;
            resetColor.a = 1f;
            ShowText.color = resetColor;
        }
    }
    #endregion

    #region 重置数据

    public void ReSetDate()
    {
        try
        {
            StopTask(true);
            PrefabsStringList?.Clear(); // 清空文本列表
            ShowText = null; // 解除TMP组件引用
            scrollingType = default; // 重置滚动模式为枚举默认值
            Index = -1; // 重置唯一标识
            StayTime = 1f; // 恢复默认停留时间
            FadeInDuration = 0.5f; // 恢复默认淡入时长
            FadeOutDuration = 0.5f; // 恢复默认淡出时长
            EaseType = Ease.Linear; // 恢复默认缓动类型

            _currentTextIndex = -1;

            if (ShowText != null)
            {
                ShowText.text = string.Empty; // 清空文本
                Color defaultColor = ShowText.color;
                defaultColor.a = 1f; // 恢复默认透明度
                ShowText.color = defaultColor;
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"ResetData执行异常：{e.Message}\n{e.StackTrace}");
        }
    }

    #endregion
}

/// <summary>
/// 滚动播放的模式
/// </summary>
public enum ScrollingType
{
    Random,  // 随机播放
    Order    // 顺序播放
}
#endregion

#endregion

#region 曲线运动任务类
public class SplineLerpTask//曲线运动任务包,沿曲线运动
{
    #region 任务私有字段
    private Spline spline;//当前曲线
    private SplineContainer _splineContainer;
    private bool IsFirstEnterTrack = true;//是否是第一次进入轨道
    private bool IsInAnimator = true;
    private SimpleAnimatorTool _animatorTool;
    private bool _isFaceSplineOutside = true; // 内部存储朝向状态
    private struct_SplineLerpTaskInfo _taskInfo; // 保存任务配置信息
    private float _firstStartValue; // 保存首次插值的起始值（用于非连续循环从头开始）
    private float _firstTargetValue; // 保存首次插值的目标值（用于非连续循环从头开始）
    #endregion

    #region 任务公共字段
    public EaseType easeType;      // 缓动类型
    public Action<float> onUpdate; // 插值更新回调
    public Action onComplete;      // 单次插值完成回调
    public float Proportion = 0;   // 当前线段的归一化比例（0 ~ 1）
    public GameObject Obj;
    public bool IsLoop = false;
    public bool IsNeedImmediate;
    public float totalMoveDuration; // 单次插值总时长
    public bool IsContinuous = true; // 是否连续（循环模式下生效）
    #endregion

    #region 任务初始化方法
    /// <summary>
    /// 初始化方法：启动首次FloatLerp插值
    /// </summary>
    /// <param name="taskInfo">曲线运动任务完整参数结构体</param>
    public void Init(struct_SplineLerpTaskInfo taskInfo)
    {
        // 保存配置信息
        _taskInfo = taskInfo;
        Obj = taskInfo.obj;
        _splineContainer = taskInfo.splineContainer;
        _isFaceSplineOutside = taskInfo.isFaceSplineOutside;
        IsLoop = taskInfo.IsLoop;
        IsNeedImmediate = taskInfo.IsNeedImmediate;
        totalMoveDuration = taskInfo.totalMoveDuration;
        easeType = taskInfo.easeType;
        IsContinuous = taskInfo.IsContinuous; // 补充赋值IsContinuous配置

        // 获取样条曲线
        spline = null;
        if (_splineContainer != null && _splineContainer.Splines.Count > 0)
            spline = _splineContainer.Splines[0];

        // 确定首次插值的起始值和目标值，并保存（用于非连续循环）
        _firstStartValue = taskInfo.Reverse ? 0.999f : 0.001f;
        _firstTargetValue = taskInfo.Reverse ? 0f : 1f;

        // 启动首次FloatLerp插值
        StartProportionLerp(_firstStartValue, _firstTargetValue);
    }
    #endregion

    #region 启动Proportion插值（含完成后分情况循环逻辑）
    /// <summary>
    /// 启动Proportion的FloatLerp插值，完成后根据IsContinuous处理循环
    /// </summary>
    /// <param name="startVal">起始比例</param>
    /// <param name="targetVal">目标比例</param>
    private void StartProportionLerp(float startVal, float targetVal)
    {
        if (_animatorTool == null)
            _animatorTool = SimpleAnimatorTool.Instance;

        // 注册FloatLerp，驱动Proportion更新
        _animatorTool.StartFloatLerp(
            startVal,
            targetVal,
            totalMoveDuration,
            // onUpdate：更新Proportion并触发外部回调
            (currentVal) =>
            {
                Proportion = currentVal;
                onUpdate?.Invoke(currentVal);
            },
            // onComplete：单次插值完成后，分情况处理循环
            () =>
            {
                // 触发外部单次完成回调
                onComplete?.Invoke();

                // 如果开启循环，根据IsContinuous判断循环类型
                if (IsLoop)
                {
                    float newStartVal;
                    float newTargetVal;

                    if (IsContinuous)
                    {
                        // 连续模式（勾选）：反向取反（1-value），来回循环
                        newStartVal = targetVal;
                        newTargetVal = Mathf.Approximately(targetVal, 1f) ? 0f : 1f; // 等价于1 - targetVal
                    }
                    else
                    {
                        // 非连续模式（未勾选）：不反向，直接复用首次插值参数，从头开始循环
                        newStartVal = _firstStartValue;
                        newTargetVal = _firstTargetValue;
                    }

                    // 再次注册FloatLerp，实现对应循环效果
                    StartProportionLerp(newStartVal, newTargetVal);
                }
                else
                {
                    // 不循环时，任务完成后加入移除列表
                    if (_animatorTool != null && !_animatorTool.RemoveSplineLerpTaskList.Contains(this))
                    {
                        _animatorTool.RemoveSplineLerpTaskList.Add(this);
                    }
                }
            },
            easeType
        );
    }
    #endregion

    #region 外部逻辑：物体移动与朝向控制
    /// <summary>
    /// 让物体沿样条移动并控制朝向
    /// </summary>
    /// <param name="overrideFaceOutside">覆盖当前朝向设置</param>
    public void MoveObjAloneSpline(bool? overrideFaceOutside = null)
    {
        // 安全校验
        if (spline == null || Obj == null || _splineContainer == null)
        {
            return;
        }

        bool finalIsFaceOutside = overrideFaceOutside ?? _isFaceSplineOutside;

        Vector3 splineLocalPos = spline.EvaluatePosition(Proportion);
        Vector3 splineWorldPos = _splineContainer.transform.TransformPoint(splineLocalPos);
        if (IsFirstEnterTrack)
        {
            IsInAnimator = true;
            Obj.transform.DOMove(splineWorldPos, 0.4f).OnComplete(() =>
            {
                IsInAnimator = false;
                IsFirstEnterTrack = false;
            });
            return;
        }

        if (IsInAnimator)
            return;

        // 更新物体位置
        Obj.transform.position = splineWorldPos;

        // 更新物体旋转
        Vector3 Tangent = spline.EvaluateTangent(Proportion);
        Vector3 Vertical = spline.EvaluateUpVector(Proportion);
        Vector3 crossVec = Vector3.Cross(Vertical, Tangent).normalized; // 样条副法线

        if (finalIsFaceOutside)
        {
            // 朝向样条外侧
            Obj.transform.rotation = Quaternion.LookRotation(Vertical, crossVec);
        }
        else
        {
            // 顺着样条摆放（沿切线方向）
            Obj.transform.rotation = Quaternion.LookRotation(crossVec, Vertical);
        }
    }
    #endregion
}
#endregion
