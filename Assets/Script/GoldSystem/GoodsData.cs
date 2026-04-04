using UnityEngine;

// 继承ScriptableObject，删除多余的Serializable
[CreateAssetMenu(
    fileName = "NewGoodInfo",
    menuName = "Game/Good Info",
    order = 100
)]
public class GoodsData : ScriptableObject
{
    #region 【基础核心信息】
    [Header("基础配置")]
    public int goodsPrice; // 价格
    [HideInInspector] public string goodsGuid; //唯一字符串ID

    // 商品类型
    public SkinType skinType;
    #endregion

    #region 【UI展示信息】
    [Header("UI展示")]
    public Sprite goodsIcon; // 商品图标
    public string goodsName; // 商品名称
    [TextArea(1, 3)] public string goodsDescription; // 描述
    public GoodsQuality quality; // 品质
    #endregion

}

// 皮肤类型
public enum SkinType
{
    PlayerCharacter,  // 角色皮肤
    GunFireEffect,    // 开火特效
    GunHitEffect,     // 命中特效
    GunAppearance,   // 枪械外观
    Expression,      // 表情
    GunObject,       // 枪械实体（部分枪械进行锁定）
    TacticEffect,   // 战术装备特效
}

// 商品品质（UI美化用）
public enum GoodsQuality
{
    Normal,    // 普通
    Rare,      // 稀有
    Epic,      // 史诗
}
