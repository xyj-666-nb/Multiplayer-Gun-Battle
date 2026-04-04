//using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
//public class SniperAimLine : MonoBehaviour
//{
//    public bool IsOpenLine = false;//是否开启瞄准辅助线

//    [Header("瞄准线最大射程")]
//    public float aimDistance = 100f;
//    [Header("忽略检测的层（比如自己、队友）")]
//    public LayerMask ignoreLayer;

//    private LineRenderer _lineRenderer;
//    private RaycastHit _hitInfo;

//    void Awake()
//    {
//        _lineRenderer = GetComponent<LineRenderer>();
//        _lineRenderer.positionCount = 2;
//        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
//    }

//    public void IsActiveLine(bool IsActive)
//    {
//        IsOpenLine=IsActive;
//        _lineRenderer.enabled = IsActive;
//    }

//    void Update()
//    {
//        UpdateAimLineWithCollision();
//    }

//    // 带碰撞检测的瞄准线
//    void UpdateAimLineWithCollision()
//    {
//        if (!IsOpenLine)
//            return;
//        // 起点 = 枪口
//        _lineRenderer.SetPosition(0, transform.position);

//        // 射线检测：向前发射射线，碰到物体就停
//        if (Physics.Raycast(transform.position, transform.forward, out _hitInfo, aimDistance, ~ignoreLayer))
//        {
//            // 终点 = 碰撞点
//            _lineRenderer.SetPosition(1, _hitInfo.point);
//        }
//        else
//        {
//            // 没碰到物体 = 无限延伸
//            _lineRenderer.SetPosition(1, transform.position + transform.forward * aimDistance);
//        }
//    }
//}