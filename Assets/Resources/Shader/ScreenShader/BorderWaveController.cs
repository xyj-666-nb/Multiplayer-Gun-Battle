using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BorderWaveController : MonoBehaviour
{
    public static BorderWaveController Instance;

    [Header("边框效果")]
    public Shader borderShader;
    public Color borderColor = new Color(0, 1, 0, 0.9f);
    public float borderWidth = 0.08f;
    public float distortStrength = 0.03f;
    public float maxAlpha = 1f;
    public float fadeSpeed = 2.5f;

    [Header("闪烁次数")]
    public int flashTimes = 2;

    private Material mat;
    private bool isFlashing = false;
    private int currentFlash;
    private float alpha;
    private float dir = 1;

    void Awake()
    {
        Instance=this;
        if (borderShader != null)
            mat = new Material(borderShader);
    }

    void Update()
    {
        if (isFlashing)
            Animate();
    }

   public void StartFlash()
    {
        isFlashing = true;
        currentFlash = 0;
        alpha = 0;
        dir = 1;
    }

    void Animate()
    {
        // 淡入淡出
        alpha += dir * Time.deltaTime * fadeSpeed;

        if (alpha >= maxAlpha)
        {
            alpha = maxAlpha;
            dir = -1;
        }
        if (alpha <= 0)
        {
            alpha = 0;
            dir = 1;
            currentFlash++;

            if (currentFlash >= flashTimes)
            {
                isFlashing = false;
            }
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (mat == null || !isFlashing)
        {
            Graphics.Blit(src, dest);
            return;
        }

        mat.SetColor("_BorderColor", borderColor);
        mat.SetFloat("_BorderWidth", borderWidth);
        mat.SetFloat("_Distort", distortStrength);
        mat.SetFloat("_Alpha", alpha);

        Graphics.Blit(src, dest, mat);
    }
}