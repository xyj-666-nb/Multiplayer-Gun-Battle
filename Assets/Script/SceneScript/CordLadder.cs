using UnityEngine;

public class CordLadder : BaseSceneInteract//绳子以及梯子的交互
{
    //起始位置
    public Transform StartPos;
    //开始位置
    public Transform EndPos;

    public override void Awake()
    {
        base.Awake();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    public override void TriggerEffect()
    {
    }

    public override void triggerEnterRange()
    {

    }

    public override void triggerExitRange()
    {
    }

    public override void triggerExitRange_Player()
    {
        base.triggerExitRange_Player();
    }

    public override void Update()
    {
        base.Update();
    }
}
