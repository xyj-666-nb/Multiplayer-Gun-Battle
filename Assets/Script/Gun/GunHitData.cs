using UnityEngine;

[CreateAssetMenu(
    fileName = "NewGunHitDataInfo",
    menuName = "Game/GunHitData",
    order = 100
)]
public class GunHitData : ScriptableObject
{
    public int HitID;//命中特效ID
    public GameObject HitObj;//打击特效

}
