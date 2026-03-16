using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PulseEffectController : MonoBehaviour
{
    [Header("脉冲效果设置")]
    [Tooltip("拖入刚才创建的 ScreenPulseShader")]
    public Shader pulseShader;

    [ColorUsage(true, true)]
    public Color pulseColor = new Color(0, 1, 0, 1); // 绿色
    public float pulseSpeed = 2f;       // 扩散速度
    public float pulseMaxRadius = 1.5f; // 最大半径
    public float pulseWidth = 0.2f;     // 环宽度
    public float pulseIntensity = 2f;   // 亮度

    private Material pulseMat;
    private bool isPulsing = false;
    private float currentRadius = 0f;

    void Awake()
    {
        // 创建材质
        if (pulseShader != null)
        {
            pulseMat = new Material(pulseShader);
        }
    }

    void Update()
    {
        // 按 H 键触发脉冲
        if (Input.GetKeyDown(KeyCode.H) && !isPulsing)
        {
            StartPulse();
        }

        // 播放脉冲动画
        if (isPulsing)
        {
            currentRadius += Time.deltaTime * pulseSpeed;

            // 到达最大半径停止
            if (currentRadius >= pulseMaxRadius)
            {
                currentRadius = 0f;
                isPulsing = false;
            }
        }
    }

    // 开始触发效果
    public void StartPulse()
    {
        isPulsing = true;
        currentRadius = 0f;
    }

    // 屏幕后处理：把效果渲染到屏幕
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (pulseMat == null || !isPulsing)
        {
            // 无效果时直接输出原图
            Graphics.Blit(source, destination);
            return;
        }

        // 给Shader传参数
        pulseMat.SetColor("_PulseColor", pulseColor);
        pulseMat.SetVector("_PulseCenter", new Vector2(0.5f, 0.5f)); // 屏幕中心
        pulseMat.SetFloat("_PulseRadius", currentRadius);
        pulseMat.SetFloat("_PulseWidth", pulseWidth);
        pulseMat.SetFloat("_PulseIntensity", pulseIntensity);

        // 应用后处理
        Graphics.Blit(source, destination, pulseMat);
    }
}