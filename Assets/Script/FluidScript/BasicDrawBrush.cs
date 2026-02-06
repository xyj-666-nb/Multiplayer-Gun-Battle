using UnityEngine;

public class BasicDrawBrush : MonoBehaviour
{
    [Header("绘制设置")]
    public float velocityMultiplier = 5f; // 速度倍数
    public float minVelocityThreshold = 0.1f; // 最小速度阈值
    [SerializeField] Color drawColor = Color.white;

    [Header("相机设置（关键！手动指定场景中的2D相机）")]
    [SerializeField] private Camera mainCamera;

    private Vector2? lastMouseWorldPoint;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("BasicDrawBrush: 场景中没有找到带有MainCamera标签的相机，请手动指定相机！");
                enabled = false; // 禁用脚本，避免持续报错
                return;
            }
        }
    }

    void Update()
    {
        Vector2? mouseWorldPoint = GetMouseWorldPoint();
        // 增加空值检查：鼠标世界坐标无效时直接返回
        if (!mouseWorldPoint.HasValue)
        {
            Debug.Log("未检测到鼠标世界坐标");

            return;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 mouseVelocity = CalculateMouseVelocity(mouseWorldPoint.Value);
            Color col = new Color(1f, 1f, 1f, 1.5f);

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    if (mouseVelocity.magnitude > 0.1f || true)
                    {
                        // 增加FluidController.Instance的空值检查
                        if (FluidController.Instance != null)
                        {
                            FluidController.Instance.QueueDrawAtPoint(
                                                mouseWorldPoint.Value + new Vector2(i * 0.05f, j * 0.05f),
                                                col,
                                                mouseVelocity,
                                                1f,
                                                1f,
                                                FluidController.VelocityType.Direct
                                            );
                        }
                        else
                        {
                            Debug.LogError("BasicDrawBrush: FluidController.Instance 为null，请检查场景中是否有FluidController组件！");
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            Vector2 mouseVelocity = CalculateMouseVelocity(mouseWorldPoint.Value);
            if (mouseVelocity.magnitude > 0.1f || true)
            {
                if (FluidController.Instance != null)
                {
                    FluidController.Instance.QueueDrawAtPoint(
                                        mouseWorldPoint.Value,
                                        drawColor,
                                        mouseVelocity,
                                        0f,
                                        1f,
                                        FluidController.VelocityType.Direct
                                    );
                }
                else
                {
                    Debug.LogError("BasicDrawBrush: FluidController.Instance 为null，请检查场景中是否有FluidController组件！");
                }
            }
        }

        // 更新上一帧位置
        lastMouseWorldPoint = mouseWorldPoint;
    }

    private Vector2 CalculateMouseVelocity(Vector2 currentMousePos)
    {
        if (!lastMouseWorldPoint.HasValue)
        {
            // 第一帧或者鼠标刚开始点击，返回零向量
            return Vector2.zero;
        }

        // 计算鼠标移动向量
        Vector2 deltaPos = currentMousePos - lastMouseWorldPoint.Value;

        // 计算速度（考虑帧率）
        Vector2 velocity = deltaPos / Time.deltaTime;

        // 应用速度倍数
        velocity *= velocityMultiplier;

        // 如果速度太小，可以选择不施加速度或使用最小速度
        if (velocity.magnitude < minVelocityThreshold)
        {
            return Vector2.zero;
        }

        // 可以选择限制最大速度
        float maxVelocity = 10f;
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }

        return velocity;
    }

    private Vector2? GetMouseWorldPoint()
    {
        // 空值检查：相机为空时返回null
        if (mainCamera == null)
        {
            return null;
        }

        Vector3 mousePos = Input.mousePosition;
        // 修正z深度计算：取相机到世界z=0平面的距离
        mousePos.z = mainCamera.nearClipPlane; // 或使用 -mainCamera.transform.position.z
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        return new Vector2(worldPos.x, worldPos.y);
    }
}