 using UnityEngine;

public class Bullseye : MonoBehaviour
{
    public GameObject DamageFloatObj;

    //传入伤害
    public void Wound(float Damage)
    {
        //每次调用自动触发函数
        var Obj = PoolManage.Instance.GetObj(DamageFloatObj);
        Obj.GetComponent<DamageFloat>().Init(Damage, this.transform);//传入坐标
       //播放击中音效
    }

}
