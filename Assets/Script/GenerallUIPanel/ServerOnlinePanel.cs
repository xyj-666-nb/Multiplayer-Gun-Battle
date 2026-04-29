using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ServerOnlinePanel : BasePanel
{
    // 简化委托：都不带参数，逻辑在外部写
    public UnityAction OnCancelAction;
    public UnityAction OnSuccessAction;
    public TextMeshProUGUI PromptText;//提示文本

    [Header("循环动画设置")]
    [Tooltip("文本切换间隔（秒）")]
    public float textInterval = 0.5f;

    // 文本列表和循环控制变量
    private List<string> _connectingTextList;
    private Coroutine _textLoopCoroutine;
    private int _currentTextIndex = 0;

    // 提供一个简单的隐藏方法
    public void HidePanel()
    {
        UImanager.Instance.HidePanel<ServerOnlinePanel>();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "CancelButton")
        {
            // 触发取消事件
            OnCancelAction?.Invoke();
            HidePanel();
        }
    }

    public override void Awake()
    {
        base.Awake();
        // 初始化空列表，防止空引用
        _connectingTextList = new List<string>();
    }

    public void TriggerRemoteCheck()
    {
        // 初始化文本列表
        _connectingTextList = new List<string>
        {
            "正在连接服务器",
            "正在连接服务器.",
            "正在连接服务器..",
            "正在连接服务器..."
        };
        StartTextLoop();
    }

    //触发匹配检查
    public void TriggerMatchCheck()
    {
        // 初始化文本列表
        _connectingTextList = new List<string>
        {
            "正在寻找公共房间",
            "正在寻找公共房间.",
            "正在寻找公共房间..",
            "正在寻找公共房间..."
        };
        StartTextLoop();
    }


    // 启动文本循环（安全版：只有列表有值才启动）
    private void StartTextLoop()
    {
        // 先停止之前的循环
        StopTextLoop();

        if (_connectingTextList == null || _connectingTextList.Count == 0 || PromptText == null)
        {
            /* Debug.LogWarning("文本列表未赋值或PromptText为空，无法开启动画"); */
            return;
        }

        // 重置索引
        _currentTextIndex = 0;

        // 启动协程
        if (gameObject.activeInHierarchy)
        {
            _textLoopCoroutine = StartCoroutine(TextLoopCoroutine());
        }
    }

    // 停止文本循环
    private void StopTextLoop()
    {
        if (_textLoopCoroutine != null)
        {
            StopCoroutine(_textLoopCoroutine);
            _textLoopCoroutine = null;
        }
    }

    // 协程：循环切换文本
    private IEnumerator TextLoopCoroutine()
    {
        while (true)
        {
            // 双重保险：防止越界+空对象
            if (PromptText != null && _currentTextIndex < _connectingTextList.Count)
            {
                PromptText.text = _connectingTextList[_currentTextIndex];
            }

            // 索引+1
            _currentTextIndex = (_currentTextIndex + 1) % _connectingTextList.Count;

            yield return new WaitForSeconds(textInterval);
        }
    }

    // 清理引用
    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnCancelAction = null;
        OnSuccessAction = null;
        StopTextLoop();
    }
    protected override void SpecialAnimator_Show()
    {

    }

    protected override void SpecialAnimator_Hide()
    {

    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
        StartTextLoop();
    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        StopTextLoop();
    }
}
