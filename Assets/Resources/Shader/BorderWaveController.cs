using UnityEngine;
using UnityEngine.UI;

public class HealBorderEffect : MonoBehaviour
{
    public Image borderImage;
    public Color color = Color.green;
    public float borderWidth = 0.1f;
    public float distort = 0.02f;
    public float maxAlpha = 1f;
    public float fadeSpeed = 3f;
    public int flashCount = 2;

    private Material mat;
    private bool isPlaying = false;
    private int currentFlash;
    private float alpha;
    private float dir = 1;

    void Awake()
    {
        mat = borderImage.material;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && !isPlaying)
        {
            StartEffect();
        }

        if (isPlaying)
        {
            Animate();
        }
    }

    void StartEffect()
    {
        isPlaying = true;
        currentFlash = 0;
        alpha = 0;
        dir = 1;
    }

    void Animate()
    {
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

            if (currentFlash >= flashCount)
            {
                isPlaying = false;
            }
        }

        mat.SetColor("_BorderColor", color);
        mat.SetFloat("_BorderWidth", borderWidth);
        mat.SetFloat("_Distort", distort);
        mat.SetFloat("_Alpha", alpha);
    }
}