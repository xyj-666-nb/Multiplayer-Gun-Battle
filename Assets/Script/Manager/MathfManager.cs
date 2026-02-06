using UnityEngine;
using UnityEngine.Events;

public class MathfManager
{
    #region 贝塞尔曲线相关
    //工具一：贝塞尔曲线
    public static Vector2 BezierCure(float precent, Vector2 Pos1, Vector2 pos2, Vector2 pos3)
    {
        var p1 = Vector2.Lerp(Pos1, pos2, precent);
        var p2 = Vector2.Lerp(pos2, pos3, precent);
        return Vector2.Lerp(p1, p2, precent);
    }
    /// <summary>
    /// 四阶贝塞尔曲线
    /// </summary>
    /// <param name="precent">百分比</param>
    /// <param name="Pos1">起点</param>
    /// <param name="pos2">中间点1</param>
    /// <param name="pos3">中间点2</param>
    /// <param name="po4">终点</param>
    /// <returns></returns>
    public static Vector2 BezierCure(float precent, Vector2 Pos1, Vector2 pos2, Vector2 pos3, Vector2 po4)
    {
        var p1 = BezierCure(precent, Pos1, pos2, pos3);
        var p2 = BezierCure(precent, pos2, pos3, po4);
        return Vector2.Lerp(p1, p2, precent);
    }

    public static Vector2 getMiddlePos_BezierCure(Vector2 Pos1, Vector2 pos2)
    {
        //随机中间的垂直的点
        var precent = Random.Range(0f, 1f);

        Vector2 mid = Vector2.Lerp(Pos1, pos2, precent);
        // 计算垂直方向
        Vector2 perp = Vector2.Perpendicular(pos2 - Pos1).normalized;
        // 随机偏移
        float offset = Random.Range(-1.5f, 1.5f);
        return mid + perp * offset;
    }
    #endregion

    #region 弧度于角度的便捷转换
    /// <summary>
    /// 角度转弧度
    /// </summary>
    /// <param name="Deg"></param>
    /// <returns></returns>
    public static float DegToRad(float Deg)
    {
        return Deg * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 弧度转换角度
    /// </summary>
    /// <param name="Rad"></param>
    /// <returns></returns>
    public static float RadToDeg(float Rad)
    {
        return Rad * Mathf.Deg2Rad;
    }

    #endregion

    #region 判断是否在屏幕外

    /// <summary>
    /// 判断点是否在屏幕范围外，如果是就是true，不在就是false。
    /// </summary>
    /// <param name="Pos"></param>
    /// <returns></returns>
    public static bool judgeIsInCameraScene(Vector3 Pos)
    {
        Vector3 ScenePos = Camera.main.WorldToScreenPoint(Pos);
        if (ScenePos.x > 0 && ScenePos.x <= Screen.width && ScenePos.y > 0 && ScenePos.x <= Screen.height)
            return false;
        return true;
    }

    #endregion

    #region 是否在扇形范围内

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public static bool IsInSectorRangeXY(Vector3 Mypos, Vector3 forward, Vector3 TargetPos, float radius, float Angle)
    {
        Mypos.z = 0;
        forward.z = 0;
        return Vector3.Distance(Mypos, TargetPos) < radius && Vector3.Angle(forward, TargetPos - Mypos) <= Angle / 2;
    }
    /// <summary>
    /// 在XZ轴上面判断目标物体是否处于扇形范围内
    /// </summary>
    /// <param name="Mypos">我的位置</param>
    /// <param name="forward">我的朝向</param>
    /// <param name="TargetPos">目标位置</param>
    /// <param name="radius">半径</param>
    /// <param name="Angle">角度</param>
    /// <returns></returns>
    public static bool IsInSectorRangeXZ(Vector3 Mypos, Vector3 forward, Vector3 TargetPos, float radius, float Angle)
    {
        Mypos.y = 0;
        forward.y = 0;
        return Vector3.Distance(Mypos, TargetPos) < radius && Vector3.Angle(forward, TargetPos - Mypos) <= Angle / 2;
    }


    #endregion

    #region 射线检测相关

    /// <summary>
    /// 射线检测，自动触发处理逻辑，有最大的距离和指定层级
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="CallBack">传出去的是整个碰撞的信息</param>
    /// <param name="MaxDistance"></param>
    /// <param name="layerMask"></param>
    public static void RayCast(Ray ray, UnityAction<RaycastHit> CallBack, float MaxDistance, int layerMask)
    {
        RaycastHit HitInfo;
        if (Physics.Raycast(ray, out HitInfo, MaxDistance, layerMask))
            CallBack?.Invoke(HitInfo);

    }

    /// <summary>
    /// 射线检测，自动触发处理逻辑，有最大的距离和指定层级
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="CallBack">只获得碰撞的对象</param>
    /// <param name="MaxDistance"></param>
    /// <param name="layerMask"></param>
    public static void RayCast(Ray ray, UnityAction<GameObject> CallBack, float MaxDistance, int layerMask)
    {
        RaycastHit HitInfo;
        if (Physics.Raycast(ray, out HitInfo, MaxDistance, layerMask))
            CallBack?.Invoke(HitInfo.collider.gameObject);

    }

    /// <summary>
    /// 射线检测，自动触发处理逻辑，有最大的距离和指定层级(得到碰撞物体身上的一个脚本)
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="CallBack">只获得碰撞的对象</param>
    /// <param name="MaxDistance"></param>
    /// <param name="layerMask"></param>
    public static void RayCast<T>(Ray ray, UnityAction<T> CallBack, float MaxDistance, int layerMask)
    {
        RaycastHit HitInfo;
        if (Physics.Raycast(ray, out HitInfo, MaxDistance, layerMask))
            CallBack?.Invoke(HitInfo.collider.gameObject.GetComponent<T>());
    }

    /// <summary>
    /// 射线检测，自动触发处理逻辑，有最大的距离和指定层级,获取所有的碰撞对象的信息
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="CallBack">这里传入的是对很多对象进行处理的单独函数</param>
    /// <param name="MaxDistance"></param>
    /// <param name="layerMask"></param>
    public static void RayCastAll(Ray ray, UnityAction<RaycastHit> CallBack, float MaxDistance, int layerMask)
    {
        RaycastHit[] HitInfo = Physics.RaycastAll(ray, MaxDistance, layerMask);
        foreach (RaycastHit hitInfo in HitInfo)
        {
            CallBack?.Invoke(hitInfo);
        }


    }

    #endregion


    /// <summary>
    /// 计算一个向量的角度，与原点构成的三角形的的以原点为顶点的角度
    /// </summary>
    /// <param name="vector">这个向量</param>
    /// <returns>这里传回来的是角度</returns>
    public static float CalculateAngle(Vector2 vector)
    {
        float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        return angle;
    }
}
