using UnityEngine;

public class BaseEntityState
{
    public EntityStateMachine MyStateMachine;
    public Base_Entity Entity;
    private string StateName;

    public bool TriggerCalled = false;
    public float StateStartTime;

    public BaseEntityState(EntityStateMachine _MyStateMachine, Base_Entity _Entity, string _StateName)
    {
        MyStateMachine = _MyStateMachine;
        Entity = _Entity;
        StateName = _StateName;
    }

    public virtual void Enter()
    {
        TriggerCalled = false;
        Entity.MyAnimator.SetBool(StateName, true);
    }

    public virtual void update()
    {
        StateStartTime -= Time.deltaTime;
    }

    public virtual void Exit()
    {
        Entity.MyAnimator.SetBool(StateName, false);
    }
    public void AnimatorFinish()
    {
        TriggerCalled = true;
    }
}
