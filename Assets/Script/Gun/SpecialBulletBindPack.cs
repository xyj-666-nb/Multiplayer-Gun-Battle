// 子弹捆绑包

using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBulletBindInfo",
    menuName = "Game/BulletBind Info",
    order = 100
)]

public class SpecialBulletBindPack : ScriptableObject
{
    public int BulletBindID; // 子弹捆绑包ID
    public Sprite Sprite; // 子弹捆绑包图标
    public string BulletBindName;
    [TextArea(1,3)]
    public string description;
    public BulletVisualConfig bulletVisualConfig;
    public MuzzleFlashConfig muzzleFlashConfig;
    public GunType gunType; // 关联的枪械类型
}