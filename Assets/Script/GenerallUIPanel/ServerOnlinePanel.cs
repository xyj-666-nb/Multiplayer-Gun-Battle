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
        // 初始化文本列表
        _connectingTextList = new List<string>
        {
            "正在连接服务器...",
            "正在连接服务器..",
            "正在连接服务器.",
            "正在连接服务器"
        };
    }


    // 启动文本循环
    private void StartTextLoop()
    {
        // 先停止之前的循环，避免重复启动
        StopTextLoop();

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
            // 更新文本
            if (PromptText != null)
            {
                PromptText.text = _connectingTextList[_currentTextIndex];
            }

            // 索引+1，取模实现循环
            _currentTextIndex = (_currentTextIndex + 1) % _connectingTextList.Count;

            // 等待指定时间
            yield return new WaitForSeconds(textInterval);
        }
    }

    // 清理引用
    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnCancelAction = null;
        OnSuccessAction = null;
        StopTextLoop(); // 确保销毁时停止协程
    }

    protected override void Update()
    {
        base.Update();
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