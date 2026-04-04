using DG.Tweening;
using UnityEngine;

public abstract class BaseSceneInteract : MonoBehaviour//基础场景交互
{
    public CanvasGroup UICanvas;
    private Sequence UICanvasAnima;
    public float CoolDownTime = 0.5f;
    public bool IsInCoolTime = false;
    [Header("是否响应")]
    public bool CanUse = true;//如果为false就不触发相关的代码

    private bool _isTriggerChecked = false;

    public bool IsNeedInteractive = true;
    public bool IsNeedShowUI = true;

    // 记录UI的初始Scale，用于绝对定位翻转
    private Vector3 _originalUIScale;

    public float InteractTime=0f;//交互时间，只有做完这个时间才能触发交互

    public virtual void Awake()
    {
        // 安全校验
        if (GlobalPictureFlipManager.Instance == null) return;
        if (UICanvas == null) return;

        // 记录初始的、正确的Scale
        _originalUIScale = UICanvas.GetComponent<RectTransform>().localScale;

        //注册翻转事件
        GlobalPictureFlipManager.Instance.FlipCallBack += TriggerFlip;

        // Awake时强制同步一次当前状态，避免初始不同步
        TriggerFlip(GlobalPictureFlipManager.Instance.IsFlipped);
    }

    public void TriggerFlip(bool IsFlip)
    {
        if (UICanvas == null) return;

        RectTransform rect = UICanvas.GetComponent<RectTransform>();

        rect.localScale = new Vector3(
            IsFlip ? -_originalUIScale.x : _originalUIScale.x,
            _originalUIScale.y,
            _originalUIScale.z
        );
    }

    private void Start()
    {
        if (IsNeedShowUI)
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UICanvas, ref UICanvasAnima, false, () => { });
    }

    public virtual void OnDestroy()
    {
        if (GlobalPictureFlipManager.Instance != null)
        {
            GlobalPictureFlipManager.Instance.FlipCallBack -= TriggerFlip;
        }
    }

    public bool IsStartCheck = false;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!CanUse)
            return;

        if (_isTriggerChecked)
            return;

        //显示提示文本
        if (collision.CompareTag("Player"))
        {
            if (collision.GetComponent<Player>() == Player.LocalPlayer)//本地玩家才显示
            {
                if (IsNeedShowUI)
                    SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UICanvas, ref UICanvasAnima, true, () => { });
                IsStartCheck = true;
                if (UImanager.Instance.GetPanel<PlayerPanel>() && IsNeedInteractive)
                {
                    //打开交互按钮
                    UImanager.Instance.GetPanel<PlayerPanel>().SetActiveInteractButton(true);
                    //设置当前交互时间
                    InteractButton.Instance.SetInteractTime(InteractTime);
                }
                if (IsInCoolTime)
                    return;
                IsInCoolTime = true;
                triggerEnterRange();
                CountDownManager.Instance.CreateTimer(false, (int)(1000 * CoolDownTime), () => { IsInCoolTime = false; });
                _isTriggerChecked = true;

            }
        }
    }

    public abstract void triggerEnterRange();

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!CanUse)
            return;
        //显示提示文本
        if (collision.CompareTag("Player"))
        {
            if (collision.GetComponent<Player>() == Player.LocalPlayer)//本地玩家才处理
            {
                if (IsNeedShowUI)
                    SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UICanvas, ref UICanvasAnima, false, () => { });
                IsStartCheck = false;
                if (UImanager.Instance.GetPanel<PlayerPanel>() && IsNeedInteractive)
                {
                    UImanager.Instance.GetPanel<PlayerPanel>().SetActiveInteractButton(false);
                }
                _isTriggerChecked = false;
                triggerExitRange_Player();// 触发玩家退出函数
            }
        }
        triggerExitRange();
    }

    public virtual void triggerExitRange_Player()
    {

    }

    public abstract void triggerExitRange();

    public virtual void Update()
    {
        if (!CanUse)
            return;

        if (!IsNeedInteractive)
            return;//不需要就退出

        if (IsStartCheck)//检测是否开启检测
        {
            //检测当前的交互键是否被按下
            if (Player.LocalPlayer != null && Player.LocalPlayer.myInputSystem.IsInteractButtonTrigger)//检测是否按下的交互键
            {
                TriggerEffect();//触发方法
            }
        }
    }

    //触发效果
    public abstract void TriggerEffect();//强制实现
}