using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 灯光亮度缓动控制器
/// 控制灯光亮度在指定最小值和最大值之间平滑往复变动
/// </summary>
[RequireComponent(typeof(Light2D))] // 自动挂载Light2D组件（如果是3D灯光可改为Light）
public class LightIntensityController : MonoBehaviour
{
    [Header("亮度配置")]
    [Tooltip("最小亮度值")]
    [Range(0f, 5f)] public float minIntensity = 0.4f;

    [Tooltip("最大亮度值")]
    [Range(0f, 5f)] public float maxIntensity = 1f;

    [Tooltip("亮度变动速度（值越小越慢）")]
    [Range(0.1f, 5f)] public float speed = 0.5f;

    [Header("额外设置")]
    [Tooltip("是否启用随机初始相位（避免多个灯光同步变动）")]
    public bool randomStartPhase = true;

    private Light2D _light2D; // 2D灯光组件引用
    private float _currentPhase; // 用于计算正弦曲线的相位值

    private void Awake()
    {
        // 获取Light2D组件（如果用3D灯光，替换为GetComponent<Light>()）
        _light2D = GetComponent<Light2D>();

        // 安全检查：确保组件存在
        if (_light2D == null)
        {
            Debug.LogError($"[{gameObject.name}] 未找到Light2D组件，请确认挂载对象有该组件！", this);
            enabled = false;
            return;
        }

        // 初始化相位：随机初始值让多个灯光变动不同步
        _currentPhase = randomStartPhase ? Random.Range(0f, Mathf.PI * 2) : 0f;
    }

    private void Update()
    {
        // 更新相位值（基于时间流逝）
        _currentPhase += Time.deltaTime * speed;

        // 使用正弦函数计算当前亮度（正弦值范围[-1,1]，转换为[0,1]后映射到目标范围）
        float normalizedValue = Mathf.Sin(_currentPhase) * 0.5f + 0.5f; // 将[-1,1]转为[0,1]
        float targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, normalizedValue);

        // 应用亮度到灯光
        _light2D.intensity = targetIntensity;
    }

    // 编辑器扩展：快速测试亮度范围
    [ContextMenu("测试最小亮度")]
    private void TestMinIntensity()
    {
        if (_light2D != null)
        {
            _light2D.intensity = minIntensity;
        }
    }

    [ContextMenu("测试最大亮度")]
    private void TestMaxIntensity()
    {
        if (_light2D != null)
        {
            _light2D.intensity = maxIntensity;
        }
    }
}