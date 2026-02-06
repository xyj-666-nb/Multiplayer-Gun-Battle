using UnityEngine;

public class EntityStateMachine : MonoBehaviour
{
    public BaseEntityState CurrentState { get; private set; }

    public void Initialize(BaseEntityState DefaultState)
    {
        CurrentState = DefaultState;
        CurrentState.Enter();
    }

    public void ChangeState(BaseEntityState State)
    {
        CurrentState.Exit();
        CurrentState = State;
        CurrentState.Enter();
    }
}
