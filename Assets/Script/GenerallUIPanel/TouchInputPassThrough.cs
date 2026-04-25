using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TouchInputPassThrough : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public TouchInputPassThroughTarget TargetType = TouchInputPassThroughTarget.General;
    public bool EnablePassThrough = true;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanPassThrough())
            return;

        TouchInputHandler.Instance?.ForwardPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanPassThrough())
            return;

        TouchInputHandler.Instance?.ForwardDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanPassThrough())
            return;

        TouchInputHandler.Instance?.ForwardPointerUp(eventData);
    }

    private bool CanPassThrough()
    {
        if (!EnablePassThrough)
            return false;

        var manager = PlayerAndGameInfoManger.Instance;
        if (manager == null)
            return true;

        switch (TargetType)
        {
            case TouchInputPassThroughTarget.ShootButton:
                return manager.IsUseShootButtonTouchPassThrough;
            case TouchInputPassThroughTarget.AimButton:
                return manager.IsUseAimButtonTouchPassThrough;
            default:
                return true;
        }
    }
}

public enum TouchInputPassThroughTarget
{
    General,
    ShootButton,
    AimButton
}
