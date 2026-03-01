
public class MilitaryWall : BaseSceneInteract
{

    public override void TriggerEffect()
    {
        UImanager.Instance.ShowPanel<ArmamentPanel>();
        //显示战备面版

    }

    public override void triggerEnterRange()
    {

    }

    public override void triggerExitRange()
    {

    }
}
