using UnityEngine;

public class Door : BaseSceneInteract
{
    public Transform TransmitPos;//自己的传送点（放在自己脚下）
    public Door RelatedDoor;//关联门(外部进行关联)

    private void Awake()
    {
       
    }

    public override void TriggerEffect()
    {
        if (RelatedDoor.IsInCoolTime)
            return;
        Player.LocalPlayer.Transmit(RelatedDoor.TransmitPos.position);
    }

    public override void triggerEnterRange()
    {

    }

    public override void triggerExitRange()
    {
   
    }
}
