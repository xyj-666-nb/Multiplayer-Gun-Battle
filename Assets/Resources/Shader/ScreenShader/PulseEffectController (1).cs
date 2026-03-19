using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScreenPulseController : MonoBehaviour
{
    public static ScreenPulseController Instance;
    [Header("脉冲设置")]
    public Color pulseColor = Color.green;
    [Range(0.01f, 2f)] public float pulseSpeed = 0.8f;
    [Range(0.01f, 1f)] public float pulseWidth = 0.15f;
    [Range(0.1f, 5f)] public float pulseIntensity = 2f;

    private Image _pulseImage;
    private Material _pulseMat;
    private bool _isPulsing = false;
    private float _currentRadius = 0f;

    void Awake()
    {
        Instance= this;
        // 获取Image
        _pulseImage = GetComponent<Image>();

        // 创建材质（不破坏原材质）
        _pulseMat = new Material(Shader.Find("Custom/ScreenPulse_UI"));
        _pulseImage.material = _pulseMat;

        // 初始隐藏
        _pulseImage.enabled = false;
    }

    void Update()
    {
        // 执行脉冲动画
        if (_isPulsing)
        {
            PulseAnimation();
        }
    }

    // 开始播放脉冲
    public void StartPulse()
    {
        if (_isPulsing) return;

        // 设置参数
        _pulseMat.SetColor("_PulseColor", pulseColor);
        _pulseMat.SetFloat("_PulseWidth", pulseWidth);
        _pulseMat.SetFloat("_PulseIntensity", pulseIntensity);
        _pulseMat.SetVector("_PulseCenter", new Vector2(0.5f, 0.5f));

        // 重置状态
        _currentRadius = 0f;
        _isPulsing = true;
        _pulseImage.enabled = true;
    }

    // 脉冲动画逻辑
    void PulseAnimation()
    {
        // 半径扩大
        _currentRadius += Time.deltaTime * pulseSpeed;
        _pulseMat.SetFloat("_PulseRadius", _currentRadius);

        // 扩散超出屏幕后结束
        if (_currentRadius > 1.5f)
        {
            StopPulse();
        }
    }

    // 结束脉冲
    void StopPulse()
    {
        _isPulsing = false;
        _pulseImage.enabled = false;
    }
}