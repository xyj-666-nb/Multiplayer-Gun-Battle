using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Linq;
using System;
using System.Linq.Expressions;
using System.Buffers;

public class FluidShadow : MonoBehaviour
{
    [SerializeField] private GameObject followObject;
    [SerializeField] private RenderTexture rt;
    [SerializeField] private ComputeShader cs;
    [SerializeField] private RenderTexture visualizeResult;

    [Header("Shadow Caster Settings")]
    [SerializeField] private bool castsShadows = true;
    [SerializeField] private bool selfShadows = true;
    [SerializeField] private bool simulation = true;
    [SerializeField] private int frequency = 30;
    [SerializeField] private int MaxExistShadowEntity = 8;

    private List<GameObject> contourObjects = new List<GameObject>();
    private Queue<GameObject> contourPool = new Queue<GameObject>();

    // 核心优化：Mesh对象池，彻底修复内存泄漏
    private Queue<Mesh> meshPool = new Queue<Mesh>();
    private List<Mesh> activeMeshes = new List<Mesh>();

    // 替代反射的委托（性能提升核心）
    private static Action<ShadowCaster2D, int[]> setApplyToSortingLayers;
    private static Action<ShadowCaster2D, Vector3[]> setShapePath;
    private static Action<ShadowCaster2D, int> setShapePathHash;
    private static Action<ShadowCaster2D, int> setPreviousShapePathHash;
    private static Action<ShadowCaster2D, Mesh> setMesh;
    private static Action<ShadowCaster2D, Bounds> setLocalBounds;

    bool hasReq = false;
    ComputeShaderRequset req;
    ShadowMeshJobHelper.JobData[] jobDatas;
    List<List<Vector3>> lastFrameContours;
    bool hasRequest = false;
    float lastTime = 0;

    void Start()
    {
        InitializeReflectionDelegates();
        // 预分配容器容量，避免扩容
        contourObjects.Capacity = MaxExistShadowEntity * 2;
        activeMeshes.Capacity = MaxExistShadowEntity * 2;
    }

    private void InitializeReflectionDelegates()
    {
        System.Type shadowCasterType = typeof(ShadowCaster2D);

        FieldInfo layerField = shadowCasterType.GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        setApplyToSortingLayers = CreateFieldSetter<ShadowCaster2D, int[]>(layerField);

        FieldInfo shapePathField = shadowCasterType.GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        setShapePath = CreateFieldSetter<ShadowCaster2D, Vector3[]>(shapePathField);

        FieldInfo shapePathHashField = shadowCasterType.GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
        setShapePathHash = CreateFieldSetter<ShadowCaster2D, int>(shapePathHashField);

        FieldInfo previousShapePathHashField = typeof(ShadowCaster2D).GetField("m_PreviousPathHash", BindingFlags.NonPublic | BindingFlags.Instance);
        setPreviousShapePathHash = CreateFieldSetter<ShadowCaster2D, int>(previousShapePathHashField);

        FieldInfo meshField = shadowCasterType.GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
        setMesh = CreateFieldSetter<ShadowCaster2D, Mesh>(meshField);

        FieldInfo localBoundsField = typeof(ShadowCaster2D).GetField("m_LocalBounds", BindingFlags.NonPublic | BindingFlags.Instance);
        setLocalBounds = CreateFieldSetter<ShadowCaster2D, Bounds>(localBoundsField);
    }

    private static Action<TTarget, TValue> CreateFieldSetter<TTarget, TValue>(FieldInfo fieldInfo)
    {
        if (fieldInfo == null) return null;

        ParameterExpression targetParam = Expression.Parameter(typeof(TTarget), "target");
        ParameterExpression valueParam = Expression.Parameter(typeof(TValue), "value");

        MemberExpression fieldAccess = Expression.Field(targetParam, fieldInfo);
        BinaryExpression assign = Expression.Assign(fieldAccess, valueParam);

        return Expression.Lambda<Action<TTarget, TValue>>(assign, targetParam, valueParam).Compile();
    }

    void Update()
    {
        Mesh[] meshs = null;
        Bounds[] bounds = null;
        int meshArrayLength = 0;

        if (jobDatas != null && jobDatas.Length > 0)
        {
            meshArrayLength = jobDatas.Length;
            meshs = ArrayPool<Mesh>.Shared.Rent(meshArrayLength);
            bounds = ArrayPool<Bounds>.Shared.Rent(meshArrayLength);

            for (int i = 0; i < jobDatas.Length; i++)
            {
                jobDatas[i].handle.Complete();
                ShadowMeshJobHelper.MeshData meshdata = jobDatas[i].GetResult();

                // 核心优化：从对象池获取Mesh，而不是每帧new
                meshs[i] = GetMeshFromPool();
                meshdata.ApplyToMesh(meshs[i]);
                bounds[i] = meshdata.bounds;
            }
            jobDatas = null;
        }

        if (meshs != null && meshArrayLength > 0 && lastFrameContours != null && lastFrameContours.Count > 0 && bounds != null)
        {
            CreateShadowCaster2D(lastFrameContours, meshs, bounds, meshArrayLength);
        }

        lastFrameContours = null;

        // 归还数组到池
        if (meshs != null) ArrayPool<Mesh>.Shared.Return(meshs, clearArray: true);
        if (bounds != null) ArrayPool<Bounds>.Shared.Return(bounds);

        int[] boolData;

        if (hasReq)
        {
            boolData = Stage1(req);
            hasReq = false;
            List<List<Vector3>> contours = Stage2(boolData, (rt.width / 4) - 1, (rt.height / 4) - 1);
            List<List<Vector3>> filtedContours = contours.Where(c => c.Count <= 1000).OrderByDescending(c => c.Count).Take(MaxExistShadowEntity).ToList();
            List<List<Vector3>> simplifiedContours = SimplifyContours(filtedContours, 0.1f);

            if (simplifiedContours.Count > 0)
            {
                jobDatas = new ShadowMeshJobHelper.JobData[simplifiedContours.Count];
                for (int i = 0; i < simplifiedContours.Count; i++)
                {
                    jobDatas[i] = ShadowMeshJobHelper.AsyncMeshGen(simplifiedContours[i].ToArray());
                }
                lastFrameContours = simplifiedContours;
            }
            else
            {
                CreateShadowCaster2D(new List<List<Vector3>>(), null, null, 0);
            }
        }

        if (!hasReq && simulation && lastTime >= 1.0f / (float)frequency)
        {
            this.transform.position = followObject.transform.position;
            req = Stage0(rt);
            hasReq = true;
        }

        if (lastTime >= (1.0f / (float)frequency))
        {
            lastTime = 0;
        }

        lastTime += Time.deltaTime;
    }

    // 核心优化：从对象池获取Mesh
    private Mesh GetMeshFromPool()
    {
        if (meshPool.Count > 0)
        {
            Mesh mesh = meshPool.Dequeue();
            mesh.Clear(); // 清空旧数据
            return mesh;
        }
        return new Mesh();
    }

    // 核心优化：归还Mesh到对象池
    private void ReturnMeshToPool(Mesh mesh)
    {
        if (mesh != null)
        {
            mesh.Clear();
            meshPool.Enqueue(mesh);
        }
    }

    void OnDestroy()
    {
        if (req.buffer != null)
        {
            req.buffer.Release();
        }

        if (jobDatas != null)
        {
            for (int i = 0; i < jobDatas.Length; i++)
            {
                jobDatas[i].handle.Complete();
                jobDatas[i].GetResult();
            }
        }

        // 核心优化：销毁所有池中的Mesh
        foreach (Mesh mesh in meshPool)
        {
            if (mesh != null) Destroy(mesh);
        }
        meshPool.Clear();

        foreach (Mesh mesh in activeMeshes)
        {
            if (mesh != null) Destroy(mesh);
        }
        activeMeshes.Clear();

        CleanupObjectPool();
    }

    void VisualizeBool(int[] boolData, int width, int height)
    {
        if (visualizeResult == null || visualizeResult.width != width || visualizeResult.height != height)
        {
            visualizeResult = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        }

        Texture2D temp = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] colors = ArrayPool<Color>.Shared.Rent(width * height);

        for (int i = 0; i < boolData.Length; i++)
        {
            if (boolData[i] == 0)
            {
                colors[i] = Color.black;
            }
            else if (boolData[i] <= 14)
            {
                colors[i] = new Color(boolData[i] * 0.07f, 0.0f, 0.0f, 1.0f);
            }
            else
            {
                colors[i] = Color.white;
            }
        }

        temp.SetPixels(colors);
        temp.Apply();

        Graphics.Blit(temp, visualizeResult);
        DestroyImmediate(temp);
        ArrayPool<Color>.Shared.Return(colors);
    }

    private void CleanupObjectPool()
    {
        foreach (GameObject obj in contourObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        contourObjects.Clear();

        while (contourPool.Count > 0)
        {
            GameObject obj = contourPool.Dequeue();
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
    }

    public void CreateShadowCaster2D(List<List<Vector3>> contours, Mesh[] meshes, Bounds[] bounds, int meshArrayLength)
    {
        if (contours == null) return;

        // 核心优化：先归还上一帧的Mesh到池
        foreach (Mesh mesh in activeMeshes)
        {
            ReturnMeshToPool(mesh);
        }
        activeMeshes.Clear();

        int validContourCount = 0;
        int maxCount = Mathf.Min(contours.Count, meshArrayLength);
        for (int i = 0; i < maxCount; i++)
        {
            if (contours[i] != null && contours[i].Count >= 3)
            {
                validContourCount++;
            }
        }

        int contourIndex = 0;
        for (int i = 0; i < maxCount; i++)
        {
            List<Vector3> contour = contours[i];
            if (contour == null || contour.Count < 3) continue;

            GameObject contourObj;
            if (contourIndex < contourObjects.Count)
            {
                contourObj = contourObjects[contourIndex];
                contourObj.SetActive(true);
            }
            else
            {
                if (contourPool.Count > 0)
                {
                    contourObj = contourPool.Dequeue();
                    contourObj.SetActive(true);
                }
                else
                {
                    contourObj = new GameObject();
                    contourObj.AddComponent<ShadowCaster2D>();
                }
                contourObjects.Add(contourObj);
            }

            contourObj.name = $"MooreShadowContour_{contourIndex}";
            contourObj.transform.SetParent(transform);
            contourObj.transform.localPosition = Vector3.zero;

            Vector3[] shapePath = ArrayPool<Vector3>.Shared.Rent(contour.Count);
            for (int j = 0; j < contour.Count; j++)
            {
                shapePath[j] = new Vector3(contour[j].x, contour[j].y, 0f);
            }

            ShadowCaster2D shadowCaster = contourObj.GetComponent<ShadowCaster2D>();
            shadowCaster.castsShadows = castsShadows;
            shadowCaster.selfShadows = selfShadows;

            if (setApplyToSortingLayers != null) setApplyToSortingLayers(shadowCaster, new int[] { 0 });
            if (setShapePath != null) setShapePath(shadowCaster, shapePath);
            int rand = 69;
            if (setShapePathHash != null) setShapePathHash(shadowCaster, rand);
            if (setPreviousShapePathHash != null) setPreviousShapePathHash(shadowCaster, rand);

            if (setMesh != null && meshes != null && i < meshes.Length)
            {
                setMesh(shadowCaster, meshes[i]);
                activeMeshes.Add(meshes[i]); // 记录当前激活的Mesh
            }
            if (setLocalBounds != null && bounds != null && i < bounds.Length)
            {
                setLocalBounds(shadowCaster, bounds[i]);
            }

            ArrayPool<Vector3>.Shared.Return(shapePath);

            contourIndex++;
        }

        int requiredCount = validContourCount;
        int currentActiveCount = contourObjects.Count;
        if (currentActiveCount > requiredCount)
        {
            for (int i = currentActiveCount - 1; i >= requiredCount; i--)
            {
                GameObject unusedObj = contourObjects[i];
                unusedObj.SetActive(false);
                contourPool.Enqueue(unusedObj);
                contourObjects.RemoveAt(i);
            }
        }
    }

    public struct ComputeShaderRequset
    {
        public UnityEngine.Rendering.AsyncGPUReadbackRequest request;
        public ComputeBuffer buffer;
    }

    ComputeShaderRequset Stage0(RenderTexture rt)
    {
        int targetWidth = (rt.width / 4) - 1;
        int targetHeight = (rt.height / 4) - 1;
        int totalPixels = targetWidth * targetHeight;

        ComputeBuffer BoolBuffer = new ComputeBuffer(totalPixels, sizeof(int));
        int kernelHandle = cs.FindKernel("MarchingSquareExtract");
        cs.SetBuffer(kernelHandle, "BoolOutput", BoolBuffer);
        cs.SetTexture(kernelHandle, "InputTexture", rt);
        cs.SetInts("TargetSize", targetWidth, targetHeight);
        cs.SetInts("InputSize", rt.width, rt.height);
        cs.SetFloat("threshold", 1.01f);

        cs.Dispatch(kernelHandle, targetWidth, targetHeight, 1);
        var request = UnityEngine.Rendering.AsyncGPUReadback.Request(BoolBuffer);

        ComputeShaderRequset req = new ComputeShaderRequset();
        req.request = request;
        req.buffer = BoolBuffer;
        return req;
    }

    int[] Stage1(ComputeShaderRequset req)
    {
        int targetWidth = (rt.width / 4) - 1;
        int targetHeight = (rt.height / 4) - 1;
        int totalPixels = targetWidth * targetHeight;

        req.request.WaitForCompletion();
        int[] boolData = ArrayPool<int>.Shared.Rent(totalPixels);
        if (!req.request.hasError)
        {
            var data = req.request.GetData<int>();
            for (int i = 0; i < totalPixels; i++)
            {
                boolData[i] = data[i];
            }
        }
        req.buffer.Release();
        return boolData;
    }

    List<List<Vector3>> Stage2(int[] BoolData, int width, int height)
    {
        List<List<Vector3>> contours = new List<List<Vector3>>();
        bool[,] visited = new bool[width, height];

        int prevValue = 0;

        for (int y = 0; y < height; y += 3)
        {
            prevValue = 0;

            for (int x = 0; x < width; x += 3)
            {
                int index = y * width + x;
                int currentValue = BoolData[index];

                if (currentValue != prevValue)
                {
                    if (prevValue == 0 && currentValue >= 1 && currentValue <= 14)
                    {
                        if (!visited[x, y])
                        {
                            List<Vector3> contour = TraceMarchingSquareContour(BoolData, width, height, x, y, visited);
                            if (contour.Count >= 3)
                            {
                                contours.Add(contour);
                            }
                        }
                    }
                    else if (prevValue == 0 && currentValue == 15)
                    {
                        for (int backX = x - 3; backX < x; backX++)
                        {
                            if (backX >= 0 && backX < width)
                            {
                                int backIndex = y * width + backX;
                                if (BoolData[backIndex] == 2 && !visited[backX, y])
                                {
                                    List<Vector3> contour = TraceMarchingSquareContour(BoolData, width, height, backX, y, visited);
                                    if (contour.Count >= 3)
                                    {
                                        contours.Add(contour);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                prevValue = currentValue;
            }
        }

        for (int i = 0; i < contours.Count; i++)
        {
            var contour = contours[i];
            for (int j = 0; j < contour.Count; j++)
            {
                Vector3 p = contour[j];
                contour[j] = new Vector3(p.x * 0.1f, p.y * 0.1f, 0f) - new Vector3(16f, 9f, 0f);
            }
        }

        ArrayPool<int>.Shared.Return(BoolData);

        return contours;
    }

    static readonly int[,] marchingSquareTable = new int[16, 4]
    {
        {-1, -1, -1, -1},
        { 1,  2, -1, -1},
        { 0,  1, -1, -1},
        { 0,  2, -1, -1},
        { 0,  3, -1, -1},
        { 2,  3,  0,  1},
        { 1,  3, -1, -1},
        { 2,  3, -1, -1},
        { 2,  3, -1, -1},
        { 1,  3, -1, -1},
        { 1,  2,  0,  3},
        { 0,  3, -1, -1},
        { 0,  2, -1, -1},
        { 0,  1, -1, -1},
        { 1,  2, -1, -1},
        {-1, -1, -1, -1},
    };

    List<Vector3> TraceMarchingSquareContour(int[] IntData, int width, int height, int startX, int startY, bool[,] visited)
    {
        List<Vector3> contour = new List<Vector3>();

        int currentX = startX;
        int currentY = startY;
        int direction = 0;

        Vector2Int[] dxdy = new Vector2Int[4]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
        };

        Vector3 startPoint = new Vector3(startX, startY, 0);
        contour.Add(startPoint);

        int StartIndex = startX + startY * width;
        int StartConfig = IntData[StartIndex];

        direction = marchingSquareTable[StartConfig, 0];

        do
        {
            int nextIndex = currentX + dxdy[direction].x + (currentY + dxdy[direction].y) * width;
            int nextConfig = IntData[nextIndex];
            int lastDirection = direction;
            direction = FindNextDirection(nextConfig, (direction + 2) % 4);

            currentX += dxdy[lastDirection].x;
            currentY += dxdy[lastDirection].y;
            contour.Add(new Vector3(currentX, currentY, 0));

            if (nextConfig != 5 && nextConfig != 10)
            {
                visited[currentX, currentY] = true;
            }

        } while (!(currentX == startX && currentY == startY) && contour.Count < 10000);

        return contour;
    }

    int FindNextDirection(int Config, int InDir)
    {
        for (int i = 0; i < 4; i++)
        {
            if (marchingSquareTable[Config, i] == InDir)
            {
                switch (i)
                {
                    case 0:
                        return marchingSquareTable[Config, 1];
                    case 1:
                        return marchingSquareTable[Config, 0];
                    case 2:
                        return marchingSquareTable[Config, 3];
                    case 3:
                        return marchingSquareTable[Config, 2];
                    default:
                        return -1;
                }
            }
        }
        return -2;
    }

    List<List<Vector3>> SimplifyContours(List<List<Vector3>> contours, float epsilon)
    {
        List<List<Vector3>> simplifiedContours = new List<List<Vector3>>();

        foreach (var contour in contours)
        {
            if (contour.Count <= 2)
            {
                simplifiedContours.Add(new List<Vector3>(contour));
                continue;
            }

            List<Vector3> simplified = DouglasPeucker(contour, epsilon);

            if (simplified.Count >= 3)
            {
                simplifiedContours.Add(simplified);
            }
        }

        return simplifiedContours;
    }

    List<Vector3> DouglasPeucker(List<Vector3> points, float epsilon)
    {
        if (points.Count <= 2)
            return new List<Vector3>(points);

        float maxDistance = 0;
        int maxIndex = 0;
        Vector3 start = points[0];
        Vector3 end = points[points.Count - 1];

        for (int i = 1; i < points.Count - 1; i++)
        {
            float distance = PointToLineDistance(points[i], start, end);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        List<Vector3> result = new List<Vector3>();

        if (maxDistance > epsilon)
        {
            List<Vector3> firstHalf = new List<Vector3>();
            for (int i = 0; i <= maxIndex; i++)
            {
                firstHalf.Add(points[i]);
            }
            List<Vector3> rec1 = DouglasPeucker(firstHalf, epsilon);

            List<Vector3> secondHalf = new List<Vector3>();
            for (int i = maxIndex; i < points.Count; i++)
            {
                secondHalf.Add(points[i]);
            }
            List<Vector3> rec2 = DouglasPeucker(secondHalf, epsilon);

            result.AddRange(rec1);
            for (int i = 1; i < rec2.Count; i++)
            {
                result.Add(rec2[i]);
            }
        }
        else
        {
            result.Add(start);
            result.Add(end);
        }

        return result;
    }

    float PointToLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineVec = lineEnd - lineStart;
        Vector3 pointVec = point - lineStart;

        float crossProduct = Mathf.Abs(lineVec.x * pointVec.y - lineVec.y * pointVec.x);
        float lineLength = lineVec.magnitude;

        if (lineLength < 0.0001f)
            return Vector3.Distance(point, lineStart);

        return crossProduct / lineLength;
    }
}