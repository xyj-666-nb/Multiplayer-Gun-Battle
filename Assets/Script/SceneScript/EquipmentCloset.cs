using TMPro;
using UnityEngine;

public class EquipmentCloset : BaseSceneInteract
{
    public bool IsTrigger=false;
    //定义冷却时间30秒
    public bool IsInCooldown=false;
    public TextMeshProUGUI PromptName;
    public override void TriggerEffect()
    {
        if (IsTrigger|| IsInCooldown)
            return;
        //开启冷却
        CountDownManager.Instance.CreateTimer(false, 30000, () =>
        {
            IsInCooldown = false;
            PromptName.text = "获取当前装备";//获取当前装备
        });

        PromptName.text = "正在冷却";
        IsInCooldown = true;//设置冷却
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
