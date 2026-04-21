using UnityEngine;
using UnityEngine.Timeline;

//枪械皮肤包
[CreateAssetMenu(
    fileName = "NewGunSkinInfo",
    menuName = "Game/GunSkin Info",
    order = 100
)]

public class GunSkinPack : ScriptableObject
{
   [Header("枪械皮肤唯一ID")]
   public int skinGuid; //枪械皮肤唯一ID
   [Header("枪械皮肤基础信息")]
   public string skinName; //皮肤名称
   public Sprite skinIcon; //皮肤图标
   public string description; //皮肤描述
   [Header("定位信息")]
   public string GunRealName; //枪械真实名称
   [Header("标准图")]
   public Sprite standardSprite; //枪械标准图(这里的图是统一调整过的)

    [Header("枪械皮肤的两种关联动画")]
    public TimelineAsset GunReload;
}
