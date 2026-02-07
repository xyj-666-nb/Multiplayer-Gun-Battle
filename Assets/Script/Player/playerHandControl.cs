using Mirror;
using UnityEngine;

public class playerHandControl : NetworkBehaviour//玩家手部控制
{
    [SyncVar(hook = nameof(OnRotationValueSynced))]
    public float CurrentRotationValue_Z = 0f;//目标旋转角度
    public float RotateSpeed = 100f;//手部旋转速度

    private void OnRotationValueSynced(float oldFloat, float newFloat)
    {
        CurrentRotationValue_Z = newFloat;
    }

    void Start()
    {
        CurrentRotationValue_Z = transform.eulerAngles.z;
    }

    void Update()
    {
        float currentEulerZ = transform.eulerAngles.z;


        float targetEulerZ = CurrentRotationValue_Z;
        float newEulerZ = Mathf.MoveTowardsAngle(
            currentEulerZ,       // 当前角度
            targetEulerZ,        // 目标角度
            RotateSpeed * Time.deltaTime // 每帧旋转的角度
        );

        transform.rotation = Quaternion.Euler(0, 0, newEulerZ);
    }
}