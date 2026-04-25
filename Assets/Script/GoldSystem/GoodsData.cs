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
    public GunSkinPack gunSkinPack;
    #endregion
    public bool ValidateData(out string errorMessage)
    {
        if (goodsPrice < 0)
        {
            errorMessage = "商品价格不能为负数";
            return false;
        }

        int activeBindingCount = 0;

        if (bulletPack != null) activeBindingCount++;
        if (expressionPacks != null && expressionPacks.Count > 0) activeBindingCount++;
        if (playerSkinPack != null) activeBindingCount++;
        if (gunHitData != null) activeBindingCount++;
        if (gunSkinPack != null) activeBindingCount++;

        if (activeBindingCount == 0)
        {
            errorMessage = $"商品 {name} 没有关联任何有效资源";
            return false;
        }

        if (activeBindingCount > 1)
        {
            errorMessage = $"商品 {name} 同时配置了多个资源引用，请只保留和 skinType 对应的一项";
            return false;
        }

        bool typeMatches =
            (skinType == SkinType.PlayerCharacter && playerSkinPack != null) ||
            (skinType == SkinType.SpecialBullet && bulletPack != null) ||
            (skinType == SkinType.GunHitEffect && gunHitData != null) ||
            (skinType == SkinType.GunAppearance && gunSkinPack != null) ||
            (skinType == SkinType.Expression && expressionPacks != null && expressionPacks.Count > 0);

        if (!typeMatches)
        {
            errorMessage = $"商品 {name} 的 skinType 和关联资源不匹配";
            return false;
        }

        errorMessage = null;
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ValidateData(out string errorMessage))
            return;

        /* Debug.LogWarning($"[GoodsData] {errorMessage}", this); */
    }
#endif
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