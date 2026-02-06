using UnityEngine;
using UnityEngine.Events;

public class EntityAnimatorTrigger : MonoBehaviour
{
    public Base_Entity Entity;
    [Header("特殊事件需求")]
    public UnityAction SpecialEvent;//特殊事件需求,需要就在外部进行赋值

    private void Awake()
    {
        if (Entity == null)
            Entity = GetComponentInParent<Base_Entity>();
    }

    public void OnAnimatorTrigger()
    {
        Entity.AnimatorFinish();
    }
    public void OnAnimatorTriggerHit()
    {
        Entity.AnimatorFinish();
    }

    public void Destoryme()
    {
        Entity.DestroyMe();
    }

    public void TriggerSpecialEvent()
    {
        SpecialEvent?.Invoke();
    }

}
