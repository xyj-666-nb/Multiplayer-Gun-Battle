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
    [Tooltip("火光起始颜色（刚开枪时的颜色，推荐亮白/亮黄）")]
    public Color lightStartColor = Color.white;

    [Tooltip("火光结束颜色（熄灭时的颜色，推荐橙红/暗红）")]
    public Color lightEndColor = new Color(1f, 0.5f, 0f, 1f); // 橙红色

    [Tooltip("2D光源最大亮度")]
    [Range(0f, 15f)] public float lightMaxIntensity = 8f;

    [Header("2D光源范围控制")]
    [Tooltip("2D光源最大照射范围（推荐0.5~2.5）")]
    [Range(0.2f, 5f)] public float lightRadius = 1.5f;

    [Header("2D专属设置")]
    [Tooltip("自动锁定Z轴（必须开启）")]
    public bool lock2DZAxis = true;

    [Header("火光装备信息配置")]
    public GunType gunType; // 关联的枪械类型
    public int MuzzleFlashID; //火光的唯一ID

}