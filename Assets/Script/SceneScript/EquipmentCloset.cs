using UnityEngine;

public class EquipmentCloset : BaseSceneInteract
{
    public bool IsTrigger=false;
    public override void TriggerEffect()
    {
        if (IsTrigger)
            return;
        IsTrigger = true;
        CountDownManager.Instance.CreateTimer(false, 500, () =>
        {
            IsTrigger=false;
        });
        //触发交互后就获取当前的战备
        PlayerAndGameInfoManger.Instance.EquipCurrentSlot();//获取战备
        Debug.Log("已经获取到当前的战备");
    }

    public override void triggerEnterRange()
    {

    }

    public override void triggerExitRange()
    {

    }
}
