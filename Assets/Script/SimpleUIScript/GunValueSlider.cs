using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // 必须引入DOTween命名空间

public class GunValueSlider : MonoBehaviour
{
    public Image ValueImage;// 用于显示数值比例的Image组件
    public TMPro.TextMeshProUGUI ValueText;// 用于显示原始数值的Text组件
    public TMPro.TextMeshProUGUI ValueNameText;// 数值名称文本
    public float Duration = 0.5f;//动画持续时间

    private int FloatLerpTaskID = -1;//当前FloatLerp任务ID
    private int RollValueTaskID = -1;//当前数值滚动任务ID
    private Tween _colorTween; // 颜色渐变的DOTween动画对象（用于管理/停止）

    // 定义颜色常量
    private readonly Color _greenColor = new Color(0.2f, 0.8f, 0.2f); // 绿色（比例<50%）
    private readonly Color _yellowColor = new Color(0.9f, 0.8f, 0.2f); // 黄色（50%≤比例<75%）
    private readonly Color _redColor = new Color(0.9f, 0.2f, 0.2f); // 红色（比例≥75%）
    // 初始颜色
    private readonly Color _initColor = Color.white;

    /// <summary>
    /// 设置数值
    /// </summary>
    /// <param name="targetValue">目标数值</param>
    /// <param name="valueName">数值名称</param>
    /// <param name="maxValue">最大值</param>
    public void SetValue(float targetValue, string valueName, string maxValue)
    {
        // 停止所有旧动画（包括颜色渐变）
        StopTask();

        if (!float.TryParse(maxValue, out float maxValueFloat) || maxValueFloat <= 0)
        {
            Debug.LogWarning($"无效的最大值：{maxValue}，默认按最大值1处理");
            maxValueFloat = 1f;
        }

        float fillRatio = targetValue / maxValueFloat;
        // 根据最终比例确定目标颜色
        Color targetColor = GetTargetColorByRatio(fillRatio);

        FloatLerpTaskID = SimpleAnimatorTool.Instance.StartFloatLerp(
            startValue: 0,
            targetValue: fillRatio,
            totalDuration: Duration,
            onUpdate: (float currentRatio) =>
            {
                ValueImage.fillAmount = Mathf.Clamp01(currentRatio);
            }
        );

        RollValueTaskID = SimpleAnimatorTool.Instance.AddRollValueTask(
            startValue: 0,
            targetValue: targetValue,
            duration: Duration,
            showText: ValueText,
            format: "F1" // 保留1位小数
        );

        // 先重置初始颜色
        ValueImage.color = _initColor;
        ValueText.color = _initColor;
        // 创建颜色渐变动画
        _colorTween = DOTween.To(
                () => _initColor, // 起始值
                (color) =>
                {
                    // 实时更新Image和Text的颜色
                    ValueImage.color = color;
                    ValueText.color = color;
                },
                targetColor, // 目标值
                Duration // 动画时长（和数值动画一致）
            )
            .SetEase(Ease.Linear) // 缓动类型
            .SetUpdate(true);

        ValueNameText.text = valueName;
    }

    /// <summary>
    /// 根据比例获取目标颜色
    /// </summary>
    private Color GetTargetColorByRatio(float ratio)
    {
        if (ratio < 0.5f) // < 50%
        {
            return _greenColor;
        }
        else if (ratio < 0.75f) // 50% ~ 75%
        {
            return _yellowColor;
        }
        else // ≥ 75%
        {
            return _redColor;
        }
    }

    /// <summary>
    /// 停止所有动画任务
    /// </summary>
    public void StopTask()
    {
        // 停止颜色渐变动画
        if (_colorTween != null && _colorTween.IsActive())
        {
            _colorTween.Kill(); // 销毁Tween
            _colorTween = null;
        }

        if (SimpleAnimatorTool.Instance == null) return;

        // 停止填充动画
        if (FloatLerpTaskID > 0)
        {
            SimpleAnimatorTool.Instance.StopFloatLerpById(FloatLerpTaskID);
            FloatLerpTaskID = -1;
        }

        // 停止数值滚动任务
        if (RollValueTaskID > 0)
        {
            SimpleAnimatorTool.Instance.StopRollValueTask(RollValueTaskID);
            RollValueTaskID = -1;
        }
    }

    /// <summary>
    /// 组件禁用/销毁时清理任务，避免内存泄漏
    /// </summary>
    private void OnDisable()
    {
        StopTask();
    }

    private void OnDestroy()
    {
        StopTask();
    }
}