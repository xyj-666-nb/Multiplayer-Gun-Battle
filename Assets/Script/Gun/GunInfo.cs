using UnityEngine;

[CreateAssetMenu(
    fileName = "NewGunInfo",
    menuName = "Game/Gun Info",
    order = 100
)]
[System.Serializable]
public class GunInfo : ScriptableObject
{
    [Header("基础信息")]
    public string Name;
    public GunType type;
    [TextArea(3, 5)]
    public string description;//枪械描述
    public int Bullet_capacity;//弹夹子弹上限
    public int AllBulletAmount;//本枪所有的子弹上限
    public int RateOfFires;
    [Header("枪械精度")]
    [Range(0, 100)] // Accuracy：0到100的滑动条
    public float Accuracy;
    [Header("伤害")]
    [Range(0, 100)] // Damage：0到100的滑动条
    public float Damage;
    [Header("射程")]
    [Range(100, 500)] // Range：100到500的滑动条
    public float Range;
    [Header("换弹时间，子弹速度")]
    public float ReloadTime;//换弹时间
    public float BulletSpeed;//子弹初速
    [Header("枪械后坐力和枪械对敌人的后坐力")]
    public float Recoil;//自身枪械的后座力
    public float Recoil_Enemy;//打到敌人身上的后坐力

    [Header("枪械的自身贴图")]
    public Sprite GunBodySprite;
    [Header("其他信息")]
    public AudioClip ShootAudio;//射击音效
    public AudioClip BulletFill;//子弹掉落的声音
    public Sprite GunSprite;//枪械的UI贴图
    [Header("枪械对屏幕的震动")]
    public float ShackStrength;
    public float ShackTime;

    [Header("散弹枪的专属信息")]
    public int ShotgunBulletAmount;//散弹的弹丸量
    [Header("散弹枪分批发射参数")]
    public float shotgunBatchInterval = 0.05f; // 批次之间的间隔时间
    public float shotgunScatterAngle = 15f; // 5波总散射角度范围

    [Header("烟雾效果参数")]
    public Color smokeColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // 烟雾颜色（默认灰色半透明）
    public float smokeSizeMin = 0.5f; // 烟雾最小大小
    public float smokeSizeMax = 1.2f; // 烟雾最大大小
    public float smokeDuration = 0.3f; // 烟雾总持续时间
    public float smokeDecaySpeed = 10f; // 烟雾衰减速度系数
}

[System.Serializable]
public enum GunType
{
    Rifle,
    Charge,
    Snipe,
    Shotgun,
    DMR,//射手步枪
}