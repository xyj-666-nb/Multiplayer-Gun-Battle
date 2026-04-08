using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewGoodInfo",
    menuName = "Game/Good Info",
    order = 100
)]
public class GoodsData : ScriptableObject
{
    #region 【基础核心信息】
    [Header("基础配置")]
    public int goodsPrice;
    [HideInInspector] public string goodsGuid;
    public SkinType skinType;
    #endregion

    #region 【UI展示信息】
    [Header("UI展示")]
    public Sprite goodsIcon;
    public string goodsName;
    [TextArea(1, 3)] public string goodsDescription;
    public GoodsQuality quality;
    #endregion

    #region 数据关联
    public SpecialBulletBindPack bulletPack;
    public List<ExpressionPack> expressionPacks; // 表情列表（你要的List）
    public PlayerSkinPack playerSkinPack;
    public GunHitData gunHitData;
    #endregion
}

// 皮肤类型
public enum SkinType
{
    PlayerCharacter,
    SpecialBullet,
    GunHitEffect,
    GunAppearance,
    Expression,
    GunObject,
    TacticEffect
}

// 商品品质
public enum GoodsQuality
{
    Normal,
    Rare,
    Epic
}

