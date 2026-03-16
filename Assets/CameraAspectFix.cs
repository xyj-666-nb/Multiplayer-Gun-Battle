using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFix : MonoBehaviour
{

    [Header("设计宽高比（宽/高）")]
    public float targetAspect =1.777f;

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        _cam.aspect = targetAspect;
    }

    void Update()
    {
        if (_cam.aspect != targetAspect)
            _cam.aspect = targetAspect;
    }
}