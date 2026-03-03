
public class EquipmentConfiguration : BaseSceneInteract
{
    public override void TriggerEffect()
    {
        //打开战备配置面板
        UImanager.Instance.ShowPanel<EquipmentConfigurationPanel>();//打开战备配置面板
    }

    public override void triggerEnterRange()
    {

    }

    public override void triggerExitRange()
    {

    }
}
