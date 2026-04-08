using UnityEngine;

[CreateAssetMenu(
    fileName = "NewPlayerSkinInfo",
    menuName = "Game/Skin Info",
    order = 100
)]
public class PlayerSkinPack : ScriptableObject
{
    [Header("皮肤ID")]
    public int PlayerSkinID; // 角色皮肤ID
    [Header("皮肤名称")]
    public string PlayerSkinName; // 角色皮肤名称
    [Header("皮肤描述")]
    public string PlayerSkinDescription; // 角色皮肤描述

    [Header("角色待机图")]
    public Sprite IdleSprite; // 角色待机图
    [Header("皮肤品质")]
    public GoodsQuality SkinQuality; // 皮肤品质

    [Header("是否拥有角色动画")]
    public bool IsHaveAnima=false; // 是否拥有角色动画
    [Header("动画序列")]
    public Sprite[] AnimaSpriteList; // 角色动画序列
    [Header("是否有附属动画")]
    public bool IsHaveSubAnima = false; // 是否有附属动画
    [Header("特殊外加动画序列")]
    public Sprite[] SpecialAnimaSpriteList; // 角色特殊外加动画序列
}
