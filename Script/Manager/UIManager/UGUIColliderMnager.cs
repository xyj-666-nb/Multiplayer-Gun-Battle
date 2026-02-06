using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UGUI.Tools//对当前的UGUI进行了扩展
{
    /// <summary>
    /// UGUI碰撞/重叠检测工具库
    /// </summary>
    public static class UGUIColliderManager
    {
        #region 常量定义
        /// <summary>
        /// 默认中心检测阈值
        /// </summary>
        public const float DEFAULT_CENTER_THRESHOLD = 20f;

        /// <summary>
        /// 复用Corners数组
        /// </summary>
        private static readonly Vector3[] _cachedCorners = new Vector3[4];
        #endregion

        #region 基础工具：RectTransform转世界/屏幕矩形
        /// <summary>
        /// 获取RectTransform的世界坐标矩形
        /// </summary>
        /// <param name="rt">目标RectTransform</param>
        /// <param name="ignoreInactive">是否忽略非激活对象（默认true）</param>
        /// <returns>世界坐标Rect（无效返回Rect.zero）</returns>
        public static Rect GetWorldRect(this RectTransform rt, bool ignoreInactive = true)
        {
            // 容错校验
            if (rt == null || (ignoreInactive && !rt.gameObject.activeInHierarchy))
                return Rect.zero;

            // 复用缓存数组，避免重复new
            rt.GetWorldCorners(_cachedCorners);

            // 计算世界坐标的最小/最大值
            float minX = Mathf.Min(_cachedCorners[0].x, _cachedCorners[1].x, _cachedCorners[2].x, _cachedCorners[3].x);
            float maxX = Mathf.Max(_cachedCorners[0].x, _cachedCorners[1].x, _cachedCorners[2].x, _cachedCorners[3].x);
            float minY = Mathf.Min(_cachedCorners[0].y, _cachedCorners[1].y, _cachedCorners[2].y, _cachedCorners[3].y);
            float maxY = Mathf.Max(_cachedCorners[0].y, _cachedCorners[1].y, _cachedCorners[2].y, _cachedCorners[3].y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 获取RectTransform的屏幕坐标矩形
        /// </summary>
        /// <param name="rt">目标RectTransform</param>
        /// <param name="camera">Canvas的Render Camera</param>
        /// <returns>屏幕坐标Rect</returns>
        public static Rect GetScreenRect(this RectTransform rt, Camera camera = null)
        {
            Rect worldRect = rt.GetWorldRect();
            if (worldRect == Rect.zero) return Rect.zero;

            // 适配不同Canvas渲染模式
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Overlay模式
                return new Rect(worldRect.x, Screen.height - worldRect.y - worldRect.height, worldRect.width, worldRect.height);
            }

            // WorldSpace/ScreenSpace-Camera模式：世界坐标转屏幕坐标
            Vector2 minScreen = camera == null ? Camera.main.WorldToScreenPoint(new Vector2(worldRect.xMin, worldRect.yMin))
                                               : camera.WorldToScreenPoint(new Vector2(worldRect.xMin, worldRect.yMin));
            Vector2 maxScreen = camera == null ? Camera.main.WorldToScreenPoint(new Vector2(worldRect.xMax, worldRect.yMax))
                                               : camera.WorldToScreenPoint(new Vector2(worldRect.xMax, worldRect.yMax));

            return new Rect(minScreen.x, Screen.height - maxScreen.y, maxScreen.x - minScreen.x, maxScreen.y - minScreen.y);
        }
        #endregion

        #region 核心检测：重叠/包含判断
        /// <summary>
        /// 检测两个RectTransform是否重叠
        /// </summary>
        /// <param name="self">当前RectTransform</param>
        /// <param name="target">目标RectTransform</param>
        /// <param name="checkCenter">是否检测中心</param>
        /// <param name="centerThreshold">中心检测阈值）</param>
        /// <param name="ignoreInactive">是否忽略非激活对象</param>
        /// <returns>是否重叠</returns>
        public static bool IsOverlappingWith(this RectTransform self, RectTransform target,   bool checkCenter = false, float centerThreshold = DEFAULT_CENTER_THRESHOLD,  bool ignoreInactive = true)
        {
            // 快速容错
            if (self == null || target == null || self == target)
                return false;

            Rect selfRect = self.GetWorldRect(ignoreInactive);
            Rect targetRect = target.GetWorldRect(ignoreInactive);

            if (selfRect == Rect.zero || targetRect == Rect.zero)
                return false;

            // 中心检测
            if (checkCenter)
            {
                Vector2 selfCenter = selfRect.center;
                Vector2 targetCenter = targetRect.center;
                return selfRect.Contains(targetCenter) || targetRect.Contains(selfCenter) ||
                       Vector2.Distance(selfCenter, targetCenter) < centerThreshold;
            }

            // 普通重叠检测
            return selfRect.Overlaps(targetRect, true);
        }

        /// <summary>
        /// 检测点是否在RectTransform内
        /// </summary>
        /// <param name="rt">目标RectTransform</param>
        /// <param name="screenPos">屏幕坐标</param>
        /// <param name="camera">Canvas的Render Camera（Overlay传null）</param>
        /// <returns>是否包含该点</returns>
        public static bool ContainsScreenPoint(this RectTransform rt, Vector2 screenPos, Camera camera = null)
        {
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, camera);
        }

        /// <summary>
        /// 检测点（世界坐标）是否在RectTransform内
        /// </summary>
        /// <param name="rt">目标RectTransform</param>
        /// <param name="worldPos">世界坐标</param>
        /// <returns>是否包含该点</returns>
        public static bool ContainsWorldPoint(this RectTransform rt, Vector3 worldPos)
        {
            Rect worldRect = rt.GetWorldRect();
            return worldRect.Contains(new Vector2(worldPos.x, worldPos.y));
        }

        #endregion

        #region 批量检测处理多个UI对象
        /// <summary>
        /// 检测指定RectTransform在列表中重叠的所有对象
        /// </summary>
        /// <param name="self">当前RectTransform</param>
        /// <param name="targetList">待检测的RectTransform列表</param>
        /// <param name="checkCenter">是否检测中心</param>
        /// <param name="resultList">输出结果列表</param>
        public static void GetAllOverlappingTargets(this RectTransform self, List<RectTransform> targetList, bool checkCenter, List<RectTransform> resultList)
        {
            if (self == null || targetList == null || resultList == null) return;

            // 清空结果列表
            resultList.Clear();

            // 预计算自身矩形，避免重复计算（性能优化）
            Rect selfRect = self.GetWorldRect();
            if (selfRect == Rect.zero) return;

            // 批量检测
            for (int i = 0; i < targetList.Count; i++)
            {
                RectTransform target = targetList[i];
                if (target == null || target == self || !target.gameObject.activeInHierarchy)
                    continue;

                Rect targetRect = target.GetWorldRect();
                if (targetRect == Rect.zero) continue;

                // 重叠判断
                bool isOverlap = checkCenter
                    ? selfRect.Contains(targetRect.center) || targetRect.Contains(selfRect.center)
                    : selfRect.Overlaps(targetRect);

                if (isOverlap)
                    resultList.Add(target);
            }
        }

        /// <summary>
        /// 检测列表中所有重叠的RectTransform对
        /// </summary>
        /// <param name="targetList">待检测列表</param>
        /// <param name="overlapPairs">输出重叠对（Key=A，Value=B）</param>
        public static void GetAllOverlapPairs(List<RectTransform> targetList, Dictionary<RectTransform, RectTransform> overlapPairs)
        {
            if (targetList == null || overlapPairs == null) return;
            overlapPairs.Clear();

            // 双层循环（i < j，避免重复检测A-B和B-A）
            for (int i = 0; i < targetList.Count; i++)
            {
                RectTransform a = targetList[i];
                if (a == null || !a.gameObject.activeInHierarchy) continue;

                Rect aRect = a.GetWorldRect();
                if (aRect == Rect.zero) continue;

                for (int j = i + 1; j < targetList.Count; j++)
                {
                    RectTransform b = targetList[j];
                    if (b == null || !b.gameObject.activeInHierarchy) continue;

                    Rect bRect = b.GetWorldRect();
                    if (bRect == Rect.zero) continue;

                    if (aRect.Overlaps(bRect))
                        overlapPairs.Add(a, b);
                }
            }
        }
        #endregion

        #region 圆形区域/Canvas缩放适配
        /// <summary>
        /// 检测圆形区域（世界坐标）是否与RectTransform重叠
        /// </summary>
        /// <param name="rt">目标RectTransform</param>
        /// <param name="circleCenter">圆形中心（世界坐标）</param>
        /// <param name="circleRadius">圆形半径（世界单位）</param>
        /// <returns>是否重叠</returns>
        public static bool IsOverlappingWithCircle(this RectTransform rt, Vector2 circleCenter, float circleRadius)
        {
            Rect worldRect = rt.GetWorldRect();
            if (worldRect == Rect.zero) return false;

            // 计算矩形到圆心的最近点
            float closestX = Mathf.Clamp(circleCenter.x, worldRect.xMin, worldRect.xMax);
            float closestY = Mathf.Clamp(circleCenter.y, worldRect.yMin, worldRect.yMax);
            float distance = Vector2.Distance(new Vector2(closestX, closestY), circleCenter);

            return distance <= circleRadius;
        }

        /// <summary>
        /// 适配Canvas缩放的重叠检测
        /// </summary>
        /// <param name="self">当前RectTransform</param>
        /// <param name="target">目标RectTransform</param>
        /// <param name="canvas">UI所属Canvas</param>
        /// <returns>是否重叠</returns>
        public static bool IsOverlappingWithCanvasScale(this RectTransform self, RectTransform target, Canvas canvas)
        {
            if (canvas == null) return self.IsOverlappingWith(target);

            // 获取Canvas缩放比例
            Vector2 canvasScale = canvas.GetComponent<CanvasScaler>().referenceResolution / new Vector2(Screen.width, Screen.height);
            canvasScale = Vector2.one / canvasScale;

            // 缩放后矩形
            Rect selfRect = self.GetWorldRect();
            selfRect.width *= canvasScale.x;
            selfRect.height *= canvasScale.y;

            Rect targetRect = target.GetWorldRect();
            targetRect.width *= canvasScale.x;
            targetRect.height *= canvasScale.y;

            return selfRect.Overlaps(targetRect);
        }
        #endregion

        #region 矩形交集计算
        /// <summary>
        /// 计算两个矩形的交集（Unity Rect无Intersect方法，手动实现）
        /// </summary>
        /// <param name="a">矩形A</param>
        /// <param name="b">矩形B</param>
        /// <returns>交集矩形（无交集返回Rect.zero）</returns>
        private static Rect CalculateRectIntersection(Rect a, Rect b)
        {
            // 计算交集的最小/最大X/Y
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            // 无交集（宽/高为负），返回空矩形
            if (xMin >= xMax || yMin >= yMax)
                return Rect.zero;

            // 返回交集矩形
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
        #endregion

        #region GetOverlapRatio方法,计算两个RectTransform的重叠比例
        /// <summary>
        /// 计算两个RectTransform的重叠比例
        /// </summary>
        /// <param name="self">当前RectTransform</param>
        /// <param name="target">目标RectTransform</param>
        /// <returns>重叠比例）</returns>
        public static float GetOverlapRatio(this RectTransform self, RectTransform target)
        {
            if (self == null || target == null) return 0f;

            Rect selfRect = self.GetWorldRect();
            Rect targetRect = target.GetWorldRect();

            if (selfRect == Rect.zero || targetRect == Rect.zero || !selfRect.Overlaps(targetRect))
                return 0f;

            // 替换为手动实现的交集计算
            Rect overlapRect = CalculateRectIntersection(selfRect, targetRect);
            float overlapArea = overlapRect.width * overlapRect.height;
            float selfArea = selfRect.width * selfRect.height;

            // 避免除零
            return selfArea <= 0f ? 0f : Mathf.Clamp01(overlapArea / selfArea);
        }
        #endregion
    }
}

