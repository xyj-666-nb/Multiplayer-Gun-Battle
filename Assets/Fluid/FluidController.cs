using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FluidController : SingleMonoAutoBehavior<FluidController>
{
    public RenderTexture colorTexture;
    public RenderTexture velocityTexture;
    public RenderTexture obstacleTexture;
    public RenderTexture obstacleTexturePre;
    public Camera obstacleCamera;
    public Color drawColor = Color.black;
    public float brushSize = 5f;
    public float obstacleForceStrength = 5.0f;
    public Shader drawShader;
    public Shader colorFieldShader;
    public Shader velocityFieldShader;
    public Shader offsetTextureShader;
    public bool simulation = true;
    private Vector2 FluidDomainOffset = new Vector2(0.0f, 0.0f);
    private Material drawMaterial;
    private Material colorFieldMaterial;
    private Material velocityFieldMaterial;
    private Material offsetTextureMaterial;
    private Texture2D brushTexture;
    [SerializeField] private Texture2D CustomTexture;
    private Renderer rend;
    private Bounds objectBounds;
    [SerializeField] private GameObject followObject;
    private Vector3 previousPosition;

    private class DrawRequest
    {
        public Vector2 worldPos;
        public Color color;
        public Vector2 initialVelocity;
        public float colorRadius;
        public float velocityRadius;
        public VelocityType velocityType;
    }

    // 优化：初始化容量避免动态扩容
    private List<DrawRequest> drawRequests = new List<DrawRequest>(1000);
    private RenderTexture tempColorRT;
    private RenderTexture tempVelocityRT;
    private bool useCommandBuffer = true;
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer velocitiesBuffer;
    private ComputeBuffer radiiBuffer;
    private ComputeBuffer velocityRadiiBuffer;
    private ComputeBuffer colorsBuffer;
    private Material batchDrawMaterial;
    private Texture2D exploreTexture;
    private ComputeBuffer velocityTypesBuffer;

    public enum VelocityType
    {
        Direct,
        Explore
    }

    private Vector2? lastWorldPoint = null;

    private CommandBuffer cachedCmdBuffer;
    private Vector2[] tempPositionsArray = new Vector2[1000];
    private Vector2[] tempVelocitiesArray = new Vector2[1000];
    private float[] tempColorRadiiArray = new float[1000];
    private float[] tempVelocityRadiiArray = new float[1000];
    private Vector4[] tempColorsArray = new Vector4[1000];
    private int[] tempVelocityTypesArray = new int[1000];

    void Start()
    {
        // 完全还原原始初始化逻辑
        if (colorTexture == null)
        {
            colorTexture = new RenderTexture(1280, 720, 24);
            colorTexture.enableRandomWrite = true;
            colorTexture.Create();
        }

        if (obstacleTexture == null)
        {
            obstacleTexture = new RenderTexture(1280, 720, 24);
            obstacleTexture.enableRandomWrite = true;
            obstacleTexture.Create();
        }

        if (obstacleTexturePre == null)
        {
            obstacleTexturePre = new RenderTexture(1280, 720, 24);
            obstacleTexturePre.enableRandomWrite = true;
            obstacleTexturePre.Create();
        }

        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.enabled = true;
            Material materialInstance = new Material(rend.sharedMaterial);
            rend.material = materialInstance;

            if (materialInstance != null)
            {
                materialInstance.SetTexture("_ColorField", colorTexture);
                materialInstance.SetTexture("_VelocityField", velocityTexture);
                objectBounds = rend.bounds;
            }
        }

        // 完全还原原始画笔纹理生成逻辑 (SetPixel)
        int brushTextureSize = 128;
        if (CustomTexture != null)
        {
            brushTexture = CustomTexture;
        }
        else
        {
            brushTexture = new Texture2D(brushTextureSize, brushTextureSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < brushTextureSize; y++)
            {
                for (int x = 0; x < brushTextureSize; x++)
                {
                    float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(brushTextureSize / 2, brushTextureSize / 2));
                    float normalizedDistance = distanceFromCenter / (brushTextureSize / 2);
                    float alpha = Mathf.Clamp01(1.0f - normalizedDistance);
                    alpha = Mathf.Pow(alpha, 2.0f);
                    brushTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            brushTexture.Apply();
        }

        // 完全还原原始爆炸纹理生成逻辑 (SetPixel)
        int exploreTextureSize = 128;
        exploreTexture = new Texture2D(exploreTextureSize, exploreTextureSize, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(exploreTextureSize / 2, exploreTextureSize / 2);

        for (int y = 0; y < exploreTextureSize; y++)
        {
            for (int x = 0; x < exploreTextureSize; x++)
            {
                Vector2 currentPos = new Vector2(x, y);
                Vector2 directionFromCenter = (currentPos - center).normalized;
                float distanceFromCenter = Vector2.Distance(currentPos, center);
                float normalizedDistance = distanceFromCenter / (exploreTextureSize / 2);
                float intensity = Mathf.Clamp01(1.0f - normalizedDistance);
                intensity = Mathf.Pow(intensity, 0.5f);
                intensity = 5f;

                float r = directionFromCenter.x * intensity * 0.5f + 0.5f;
                float g = directionFromCenter.y * intensity * 0.5f + 0.5f;
                float b = intensity;
                float a = Mathf.Clamp01(1 - normalizedDistance);

                exploreTexture.SetPixel(x, y, new Color(r, g, b, a));
            }
        }

        exploreTexture.Apply();
        exploreTexture.filterMode = FilterMode.Bilinear;
        exploreTexture.wrapMode = TextureWrapMode.Clamp;

        if (drawShader == null) Debug.LogError("未找到Custom/DrawBrush着色器");
        if (colorFieldShader == null) Debug.LogError("未找到Custom/ColorField着色器");
        if (velocityFieldShader == null) Debug.LogError("未找到Custom/VelocityField着色器");
        if (offsetTextureShader == null) Debug.LogError("未找到Custom/OffsetTexture着色器");

        drawMaterial = new Material(drawShader);
        colorFieldMaterial = new Material(colorFieldShader);
        velocityFieldMaterial = new Material(velocityFieldShader);
        offsetTextureMaterial = new Material(offsetTextureShader);

        RenderTexture.active = colorTexture;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        RenderTexture.active = null;

        RenderTexture.active = velocityTexture;
        GL.Clear(true, true, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        RenderTexture.active = null;

        RenderTexture.active = obstacleTexture;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        RenderTexture.active = null;

        RenderTexture.active = obstacleTexturePre;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        RenderTexture.active = null;

        InitCommandBufferResources();

        if (obstacleCamera != null)
            obstacleCamera.enabled = false;
    }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        // 完全还原原始Update逻辑，不做任何改动
        if (followObject == null)
        {
            followObject = GameObject.Find("Main Camera");
            return;
        }

        rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            objectBounds = rend.bounds;
        }

        Vector3 followPosition = followObject.transform.position;
        this.transform.position = new Vector3(followPosition.x, followPosition.y, this.transform.position.z);

        FluidDomainOffset = (this.transform.position - previousPosition);
        FluidDomainOffset = new Vector2(
            FluidDomainOffset.x * 0.03125f,
            FluidDomainOffset.y * 0.05555f
        );
        previousPosition = this.transform.position;

        UpdateObstacleTexture();
        UpdateOffsetField(colorTexture, FluidDomainOffset);
        UpdateOffsetField(velocityTexture, FluidDomainOffset);

        if (simulation)
        {
            UpdateColorField();
            UpdateVelocityField();
            UpdateVelocityField();
            UpdateVelocityField();
        }

        if (simulation)
        {
            Graphics.Blit(obstacleTexture, obstacleTexturePre);
        }
    }

    private Vector2? GetMouseWorldPoint()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return new Vector2(worldPos.x, worldPos.y);
    }

    private bool IsPointInsideObject(Vector2 point)
    {
        return objectBounds.Contains(new Vector3(point.x, point.y, transform.position.z));
    }

    // 完全还原原始UV计算逻辑
    private Vector2 WorldToUV(Vector2 worldPos)
    {
        float u = (worldPos.x - objectBounds.min.x) / objectBounds.size.x;
        float v = (worldPos.y - objectBounds.min.y) / objectBounds.size.y;
        return new Vector2(u, v);
    }

    public void QueueDrawAtPoint(Vector2 worldPos, Color color, Vector2 initialVelocity, float colorRadius, float velocityRadius, VelocityType velocityType = VelocityType.Direct)
    {
        drawRequests.Add(new DrawRequest
        {
            worldPos = worldPos,
            color = color,
            initialVelocity = initialVelocity,
            colorRadius = colorRadius,
            velocityRadius = velocityRadius,
            velocityType = velocityType
        });
    }

    private void LateUpdate()
    {
        if (drawRequests.Count > 0)
        {
            // 保留原始时间记录（注释掉的也保留原样）
            float startTime = Time.realtimeSinceStartup;

            if (useCommandBuffer)
            {
                ProcessDrawRequestsWithCommandBuffer();
            }
            else
            {
                InitTemporaryTextures();
                ProcessDrawRequestsBatch();
            }

            drawRequests.Clear();

            float endTime = Time.realtimeSinceStartup;
            //Debug.Log($"绘制处理耗时: {(endTime - startTime) * 1000:F2}ms, 使用CommandBuffer: {useCommandBuffer}");
        }
    }

    // 完全还原原始批量处理逻辑，包括Debug.LogError
    private void ProcessDrawRequestsBatch()
    {
        Graphics.Blit(colorTexture, tempColorRT);
        Graphics.Blit(velocityTexture, tempVelocityRT);

        foreach (var request in drawRequests)
        {
            Vector2 uv = WorldToUV(request.worldPos) - FluidDomainOffset;
            Debug.LogError("FluidDomainOffset: " + FluidDomainOffset); // 还原这行

            drawMaterial.SetColor("_Color", request.color);
            drawMaterial.SetFloat("_BrushSize", brushSize * request.colorRadius);
            drawMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
            drawMaterial.SetFloat("_NOT_DRAW_ALPHA", 0.0f);
            drawMaterial.SetTexture("_MainTex", tempColorRT);

            Graphics.Blit(tempColorRT, colorTexture, drawMaterial);
            Graphics.Blit(colorTexture, tempColorRT);
        }

        foreach (var request in drawRequests)
        {
            Vector2 uv = WorldToUV(request.worldPos) - FluidDomainOffset;

            Color velocityColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            velocityColor.r += request.initialVelocity.x * 0.05f;
            velocityColor.g += request.initialVelocity.y * 0.05f;

            drawMaterial.SetColor("_Color", velocityColor);
            drawMaterial.SetFloat("_BrushSize", brushSize * request.velocityRadius);
            drawMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
            drawMaterial.SetFloat("_NOT_DRAW_ALPHA", 1.0f);
            drawMaterial.SetTexture("_MainTex", tempVelocityRT);

            Graphics.Blit(tempVelocityRT, velocityTexture, drawMaterial);
            Graphics.Blit(velocityTexture, tempVelocityRT);
        }
    }

    private void InitTemporaryTextures()
    {
        if (tempColorRT == null)
            tempColorRT = RenderTexture.GetTemporary(colorTexture.width, colorTexture.height, 0, colorTexture.format);
        if (tempVelocityRT == null)
            tempVelocityRT = RenderTexture.GetTemporary(velocityTexture.width, velocityTexture.height, 0, velocityTexture.format);
    }

    private void ReleaseTemporaryTextures()
    {
        if (tempColorRT != null)
        {
            RenderTexture.ReleaseTemporary(tempColorRT);
            tempColorRT = null;
        }
        if (tempVelocityRT != null)
        {
            RenderTexture.ReleaseTemporary(tempVelocityRT);
            tempVelocityRT = null;
        }
    }

    private void OnDestroy()
    {
        ReleaseTemporaryTextures();
        ReleaseCommandBufferResources();
    }

    // 完全还原原始DrawAtPoint逻辑
    public void DrawAtPoint(Vector2 worldPos, Color color, Vector2 initialVelocity, float radius)
    {
        Vector2 uv = WorldToUV(worldPos);
        Debug.LogError("uv: " + uv);

        RenderTexture tempRT = RenderTexture.GetTemporary(colorTexture.width, colorTexture.height, 0, colorTexture.format);
        Graphics.Blit(colorTexture, tempRT);

        Vector2 offset = (this.transform.position - previousPosition) * 0.1f;

        drawMaterial.SetColor("_Color", color);
        drawMaterial.SetTexture("_BrushTex", CustomTexture);
        drawMaterial.SetFloat("_BrushSize", brushSize * radius);
        drawMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
        drawMaterial.SetFloat("_NOT_DRAW_ALPHA", 0.0f);
        drawMaterial.SetTexture("_MainTex", tempRT);

        Graphics.Blit(tempRT, colorTexture, drawMaterial, 0);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    // 完全还原原始UpdateColorField逻辑
    private void UpdateColorField()
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(colorTexture.width, colorTexture.height, 0, colorTexture.format);
        Graphics.Blit(colorTexture, tempRT);

        // 保留原始代码，即使offset是0
        Vector2 offset = (this.transform.position - previousPosition) * 0.0f;

        colorFieldMaterial.SetTexture("_MainTex", tempRT);
        colorFieldMaterial.SetTexture("_VelocityTex", velocityTexture);
        colorFieldMaterial.SetTexture("_ObstacleTex", obstacleTexture);
        colorFieldMaterial.SetColor("_Velocity", new Vector4(offset.x, offset.y, 0, 0));
        colorFieldMaterial.SetVector("_FluidDomainOffset", new Vector4(FluidDomainOffset.x, FluidDomainOffset.y, 0, 0));

        Graphics.Blit(tempRT, colorTexture, colorFieldMaterial);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    // 完全还原原始UpdateVelocityField逻辑
    private void UpdateVelocityField()
    {
        float dt = 0;
        float startTime = Time.realtimeSinceStartup;

        Vector2 offset = (this.transform.position - previousPosition) * dt;

        RenderTexture tempRT = RenderTexture.GetTemporary(velocityTexture.width, velocityTexture.height, 0, velocityTexture.format);
        Graphics.Blit(velocityTexture, tempRT);

        velocityFieldMaterial.SetTexture("_MainTex", tempRT);
        velocityFieldMaterial.SetTexture("_ObstacleTex", obstacleTexture);
        velocityFieldMaterial.SetTexture("_ObstacleTexPre", obstacleTexturePre);
        velocityFieldMaterial.SetFloat("_ObstacleForceStrength", obstacleForceStrength);
        velocityFieldMaterial.SetVector("_Velocity", new Vector4(offset.x, offset.y, 0, 0));
        velocityFieldMaterial.SetVector("_FluidDomainOffset", new Vector4(FluidDomainOffset.x, FluidDomainOffset.y, 0, 0));

        Graphics.Blit(tempRT, velocityTexture, velocityFieldMaterial);
        RenderTexture.ReleaseTemporary(tempRT);

        float endTime = Time.realtimeSinceStartup;
        //Debug.Log("速度场更新耗时: " + ((endTime - startTime) * 1000f) + "ms");
    }

    private void UpdateOffsetField(RenderTexture Texture, Vector2 offset)
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(Texture.width, Texture.height, 0, Texture.format);
        Graphics.Blit(Texture, tempRT);
        offsetTextureMaterial.SetTexture("_MainTex", tempRT);
        offsetTextureMaterial.SetVector("_Offset", new Vector4(offset.x, offset.y, 0, 0));
        Graphics.Blit(tempRT, Texture, offsetTextureMaterial);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    private void UpdateObstacleTexture()
    {
        if (obstacleCamera == null) return;

        obstacleCamera.enabled = false;
        obstacleCamera.targetTexture = obstacleTexture;
        obstacleCamera.Render();

        // 保留原始的无用代码
        RenderTexture.active = obstacleTexture;
        RenderTexture.active = null;
    }

    public void ClearTexture()
    {
        RenderTexture rt1 = RenderTexture.active;
        RenderTexture.active = colorTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt1;

        RenderTexture rt2 = RenderTexture.active;
        RenderTexture.active = velocityTexture;
        GL.Clear(true, true, new Color(0.5f, 0.5f, 0.515f, 0.5f));
        RenderTexture.active = rt2;
    }

    private void InitCommandBufferResources()
    {
        if (drawShader == null) return;

        if (batchDrawMaterial == null)
            batchDrawMaterial = new Material(drawShader);

        // 优化：创建缓存的CommandBuffer
        if (cachedCmdBuffer == null)
            cachedCmdBuffer = new CommandBuffer { name = "Batch Draw Processing" };

        ReleaseCommandBufferResources();
    }

    private void ReleaseCommandBufferResources()
    {
        if (positionsBuffer != null) { positionsBuffer.Release(); positionsBuffer = null; }
        if (velocitiesBuffer != null) { velocitiesBuffer.Release(); velocitiesBuffer = null; }
        if (radiiBuffer != null) { radiiBuffer.Release(); radiiBuffer = null; }
        if (velocityRadiiBuffer != null) { velocityRadiiBuffer.Release(); velocityRadiiBuffer = null; }
        if (colorsBuffer != null) { colorsBuffer.Release(); colorsBuffer = null; }
        if (velocityTypesBuffer != null) { velocityTypesBuffer.Release(); velocityTypesBuffer = null; }
    }

    private void ProcessDrawRequestsWithCommandBuffer()
    {
        if (drawRequests.Count == 0) return;

        int count = drawRequests.Count;

        // 确保数组容量足够
        if (count > tempPositionsArray.Length)
        {
            int newSize = Mathf.Max(count, tempPositionsArray.Length * 2);
            tempPositionsArray = new Vector2[newSize];
            tempVelocitiesArray = new Vector2[newSize];
            tempColorRadiiArray = new float[newSize];
            tempVelocityRadiiArray = new float[newSize];
            tempColorsArray = new Vector4[newSize];
            tempVelocityTypesArray = new int[newSize];
        }

        // 确保Buffer容量足够
        if (positionsBuffer == null || positionsBuffer.count < count)
        {
            ReleaseCommandBufferResources();
            int bufSize = Mathf.Max(1000, count);
            positionsBuffer = new ComputeBuffer(bufSize, sizeof(float) * 2);
            velocitiesBuffer = new ComputeBuffer(bufSize, sizeof(float) * 2);
            radiiBuffer = new ComputeBuffer(bufSize, sizeof(float));
            velocityRadiiBuffer = new ComputeBuffer(bufSize, sizeof(float));
            colorsBuffer = new ComputeBuffer(bufSize, sizeof(float) * 4);
            velocityTypesBuffer = new ComputeBuffer(bufSize, sizeof(int));
        }

        // 填充数据（逻辑完全不变，只是用了预分配数组）
        for (int i = 0; i < count; i++)
        {
            tempPositionsArray[i] = WorldToUV(drawRequests[i].worldPos) - FluidDomainOffset;
            tempVelocitiesArray[i] = drawRequests[i].initialVelocity;
            tempColorRadiiArray[i] = drawRequests[i].colorRadius;
            tempVelocityRadiiArray[i] = drawRequests[i].velocityRadius;
            tempColorsArray[i] = drawRequests[i].color;
            tempVelocityTypesArray[i] = (int)drawRequests[i].velocityType;
        }

        // 更新缓冲区
        positionsBuffer.SetData(tempPositionsArray, 0, 0, count);
        velocitiesBuffer.SetData(tempVelocitiesArray, 0, 0, count);
        radiiBuffer.SetData(tempColorRadiiArray, 0, 0, count);
        velocityRadiiBuffer.SetData(tempVelocityRadiiArray, 0, 0, count);
        colorsBuffer.SetData(tempColorsArray, 0, 0, count);
        velocityTypesBuffer.SetData(tempVelocityTypesArray, 0, 0, count);

        // 优化：复用CommandBuffer，而不是每次new
        cachedCmdBuffer.Clear();

        InitTemporaryTextures();
        cachedCmdBuffer.Blit(colorTexture, tempColorRT);

        batchDrawMaterial.SetBuffer("_Positions", positionsBuffer);
        batchDrawMaterial.SetBuffer("_Colors", colorsBuffer);
        batchDrawMaterial.SetBuffer("_ColorRadii", radiiBuffer);
        batchDrawMaterial.SetInt("_PointCount", count);
        batchDrawMaterial.SetTexture("_BrushTex", brushTexture);
        batchDrawMaterial.SetTexture("_ExploreTex", exploreTexture);
        batchDrawMaterial.SetFloat("_BrushSize", brushSize);

        cachedCmdBuffer.Blit(tempColorRT, colorTexture, batchDrawMaterial, 0);
        cachedCmdBuffer.Blit(velocityTexture, tempVelocityRT);

        batchDrawMaterial.SetBuffer("_Velocities", velocitiesBuffer);
        batchDrawMaterial.SetBuffer("_VelocityRadii", velocityRadiiBuffer);
        batchDrawMaterial.SetBuffer("_VelocityTypes", velocityTypesBuffer);

        cachedCmdBuffer.Blit(tempVelocityRT, velocityTexture, batchDrawMaterial, 1);

        // 还原原始重复设置的代码
        batchDrawMaterial.SetBuffer("_VelocityTypes", velocityTypesBuffer);

        Graphics.ExecuteCommandBuffer(cachedCmdBuffer);
    }
}