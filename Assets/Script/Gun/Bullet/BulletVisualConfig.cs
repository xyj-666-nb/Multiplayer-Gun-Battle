using UnityEngine;

//视觉子弹数据结构

[CreateAssetMenu(
    fileName = "NewBulletVisualConfig",
    menuName = "Game/子弹视觉配置",
    order = 101
)]
public class BulletVisualConfig : ScriptableObject
{
    [Header("子弹线段外观")]
    [Tooltip("子弹线段颜色")]
    public Color bulletColor = new Color(0.83f, 0.68f, 0.22f); // 默认铜黄色

    [Tooltip("子弹线段长度")]
    [Range(0.05f, 1f)] public float bulletSegmentLength = 0.2f;

    [Tooltip("子弹线段宽度")]
    [Range(0.01f, 0.1f)] public float bulletLineWidth = 0.03f;

    [Header("子弹飞行参数")]
    [Tooltip("子弹飞行速度")]
    [Range(20f, 200f)] public float bulletFlySpeed = 80f;

    [Tooltip("子弹到达目标后显示的持续时间（淡出时间）")]
    [Range(0.1f, 2f)] public float bulletShowDuration = 0.5f;

    [Header("子弹装备信息配置")]
    public GunType gunType; // 关联的枪械类型(是什么枪械的子弹)
    public int BulletID; // 子弹的唯一ID(子弹的唯一ID)

}