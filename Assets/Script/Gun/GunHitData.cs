using UnityEngine;

[CreateAssetMenu(
    fileName = "NewGunHitDataInfo",
    menuName = "Game/GunHitData",
    order = 100
)]
public class GunHitData : ScriptableObject
{
    public int HitID;//命中特效ID
    public string HitName;//命中特效名称
    [TextArea(1,3)]
    public string HitDescription;
    public Sprite HitIcon;//命中特效图标
    public GameObject HitObj;//打击特效

}
