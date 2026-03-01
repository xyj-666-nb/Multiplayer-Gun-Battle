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
    public bool IsNeedShowUI=true;

    private void Start()
    {
        if(IsNeedShowUI)
          SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UICanvas, ref UICanvasAnima, false, () => { });
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
                if(IsNeedShowUI)
                   SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(UICanvas, ref UICanvasAnima, true, () => { });
                IsStartCheck = true;
                if (UImanager.Instance.GetPanel<PlayerPanel>()&& IsNeedInteractive)
                {
                    //打开交互按钮
                    UImanager.Instance.GetPanel<PlayerPanel>().SetActiveInteractButton(true);
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
            }
        }
        triggerExitRange();
    }

    public abstract void triggerExitRange();

    private void Update()
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