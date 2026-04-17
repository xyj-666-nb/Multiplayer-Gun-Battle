using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMuzzleFlashConfigInfo",
    menuName = "Game/MuzzleFlash Info",
    order = 100
)]
public class MuzzleFlashConfig : ScriptableObject
{
    [Header("火光基础参数")]
    [Tooltip("火光持续时间（秒）")]
    public float flashDuration = 0.06f;

    [Header("2D光源渐变控制")]
    [Tooltip("火光起始颜色")]
    public Color lightStartColor = Color.white;

    [Tooltip("火光结束颜色")]
    public Color lightEndColor = new Color(1f, 0.5f, 0f, 1f);

    [Tooltip("2D光源最大亮度")]
    [Range(0f, 15f)] public float lightMaxIntensity = 8f;

    [Header("火光装备信息配置")]
    public GunType gunType;
    public int MuzzleFlashID;
}