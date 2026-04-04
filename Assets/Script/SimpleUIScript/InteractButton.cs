using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 继承UI触摸事件接口，实现按下、抬起、移出检测
public class InteractButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public static InteractButton Instance;

    // 交互填充图片
    public Image FillImage;

    [Header("基础交互设置")]
    [Tooltip("长按触发交互的时间，为0时点击直接触发")]
    private float interactTime = 0f;

    // 当前长按进度
    private float currentPressTimer;
    // 是否正在长按
    private bool isPressing;
    // 存储协程实例，用于精准停止
    private Coroutine pressCoroutine;

    private void Awake()
    {
        // 静态单例赋值
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region UI触摸事件
    // 按下按钮
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        currentPressTimer = 0;

        // 交互时间=0，直接触发
        if (interactTime <= 0)
        {
            TriggerInteract();
            return;
        }

        // 长按模式：启动协程（替代Update）
        if (pressCoroutine == null)
        {
            pressCoroutine = StartCoroutine(PressInteractCoroutine());
        }
    }

    // 抬起按钮
    public void OnPointerUp(PointerEventData eventData)
    {
        ResetPressState();
    }

    // 移出按钮区域
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPressState();
    }
    #endregion

    #region 协程长按
    /// <summary>
    /// 长按协程：仅在按下时运行，无空耗
    /// </summary>
    private System.Collections.IEnumerator PressInteractCoroutine()
    {
        while (isPressing && FillImage != null)
        {
            // 每帧累加计时
            currentPressTimer += Time.deltaTime;
            // 更新填充进度
            float fillPercent = currentPressTimer / interactTime;
            FillImage.fillAmount = Mathf.Clamp01(fillPercent);

            // 进度满，触发交互
            if (currentPressTimer >= interactTime)
            {
                TriggerInteract();
                ResetPressState();
                break;
            }

            // 等待下一帧（协程核心）
            yield return null;
        }

        // 协程结束清空引用
        pressCoroutine = null;
    }

    /// <summary>
    /// 触发交互逻辑
    /// </summary>
    private void TriggerInteract()
    {
        if (Player.LocalPlayer != null)
        {
            Player.LocalPlayer.myInputSystem.ExtraInteractTrigger();
        }
    }

    /// <summary>
    /// 重置长按状态和UI填充
    /// </summary>
    private void ResetPressState()
    {
        isPressing = false;
        currentPressTimer = 0;

        // 停止协程
        if (pressCoroutine != null)
        {
            StopCoroutine(pressCoroutine);
            pressCoroutine = null;
        }

        // 清空填充UI
        if (FillImage != null)
        {
            FillImage.fillAmount = 0;
        }
    }

    /// <summary>
    /// 外部设置交互时间的接口
    /// </summary>
    public void SetInteractTime(float time)
    {
        interactTime = Mathf.Max(0, time);
        ResetPressState();
    }
    #endregion
}