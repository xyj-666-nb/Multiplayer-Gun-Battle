using UnityEngine;

public class Door : BaseSceneInteract
{
   public Transform TransmitPos;//自己的传送点（放在自己脚下）
   public Door RelatedDoor;//关联门(外部进行关联)
   public DoorType doorType;//门的类型

    public override void TriggerEffect()
    {
        if (RelatedDoor.IsInCoolTime)
            return;
        Player.LocalPlayer.Transmit(RelatedDoor.TransmitPos.position);
        //播放本地音效
        playDoorMusic();
    }
    public void playDoorMusic()
    {
        string MusciPath = "";
        switch (doorType)
        {
            case DoorType.Cord:
                MusciPath = "Music/正式/交互/绳子";
                break;
            case DoorType.WoodenDoor:
                MusciPath = "Music/正式/交互/开门";
                break;
            case DoorType.IronDoor:
                MusciPath = "Music/正式/交互/开隔离门";
                break;
            case DoorType.VentilationDuct:
                MusciPath = "Music/正式/交互/通风";
                break;
            case DoorType.LronLadder:
                MusciPath = "Music/正式/交互/梯子";
                break;
            case DoorType.WoodenLadder:
                break;


        }
        MusicManager.Instance.PlayEffect(MusciPath);
    }

    public override void triggerEnterRange()
    {

    }

    public override void triggerExitRange()
    {

    }
}

public enum DoorType
{
　 Cord,//绳子
   WoodenDoor,//木门
   IronDoor,//铁门
   VentilationDuct,//通风管道
   WoodenLadder,//木制梯子
    LronLadder//铁制梯子
}


