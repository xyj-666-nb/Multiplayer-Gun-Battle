using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 刚体信息GUI显示管理器（单例）
/// 支持2D/3D刚体注册/注销、自定义显示名称、GUI自动排版绘制刚体关键信息
/// </summary>
public class RigidbodyGUITestManager : SingleMonoAutoBehavior<RigidbodyGUITestManager>
{
    #region 全局配置
    [Header("=== 全局显示开关 ===")]
    [Tooltip("是否显示所有注册的刚体信息")]
    public bool IsShowRigInfo = false;

    [Header("=== GUI绘制样式配置 ===")]
    [Tooltip("GUI字体大小")]
    public int guiFontSize = 14;
    [Tooltip("每行文本的高度（像素）")]
    public int lineHeight = 30;
    [Tooltip("绘制起始位置X（屏幕左上角为原点）")]
    public int startPosX = 10;
    [Tooltip("绘制起始位置Y（屏幕左上角为原点）")]
    public int startPosY = 90;
    [Tooltip("每个信息区域的宽度（像素）")]
    public int contentWidth = 450;
    [Tooltip("不同刚体信息区域的间距（像素）")]
    public int areaSpacing = 20;
    [Tooltip("错误提示文字颜色")]
    public Color errorColor = Color.red;
    [Tooltip("标题文字颜色")]
    public Color titleColor = new Color(0, 0.8f, 1); // 深蓝色
    #endregion

    #region 控制显示
    public void IsActiveInfo(bool IsActive)
    {
        IsShowRigInfo=IsActive;
    }
    #endregion

    #region 内部存储结构
    /// <summary>
    /// 3D刚体显示信息封装
    /// </summary>
    private class Rig3DShowInfo
    {
        public Rigidbody rig;       // 绑定的3D刚体
        public string showName;     // 自定义显示区域名称
    }

    /// <summary>
    /// 2D刚体显示信息封装
    /// </summary>
    private class Rig2DShowInfo
    {
        public Rigidbody2D rig;     // 绑定的2D刚体
        public string showName;     // 自定义显示区域名称
    }
    #endregion

    #region 注册信息存储字典
    // 3D刚体注册字典：Key=刚体对象，Value=显示信息
    private Dictionary<Rigidbody, Rig3DShowInfo> _rig3DShowDic = new Dictionary<Rigidbody, Rig3DShowInfo>();
    // 2D刚体注册字典：Key=刚体对象，Value=显示信息
    private Dictionary<Rigidbody2D, Rig2DShowInfo> _rig2DShowDic = new Dictionary<Rigidbody2D, Rig2DShowInfo>();
    #endregion

    #region 对外公开接口
    /// <summary>
    /// 注册2D刚体，添加GUI信息显示
    /// </summary>
    /// <param name="rig">要显示的2D刚体</param>
    /// <param name="customName">自定义显示区域名称（用于区分不同刚体）</param>
    public void AddRigInfoShow_2D(Rigidbody2D rig, string customName)
    {
        // 空值校验
        if (rig == null)
        {
            Debug.LogError("[RigidbodyGUITestManager] 注册2D刚体失败：刚体对象为Null！");
            return;
        }
        // 重复注册校验
        if (_rig2DShowDic.ContainsKey(rig))
        {
            Debug.LogWarning($"[RigidbodyGUITestManager] 2D刚体{rig.name}已注册，自定义名称：{_rig2DShowDic[rig].showName}，无需重复注册");
            return;
        }
        // 添加到字典
        _rig2DShowDic.Add(rig, new Rig2DShowInfo
        {
            rig = rig,
            showName = string.IsNullOrEmpty(customName) ? $"2D刚体_{rig.name}" : customName
        });
        Debug.Log($"[RigidbodyGUITestManager] 2D刚体注册成功，显示名称：{customName}");
    }

    /// <summary>
    /// 注册3D刚体，添加GUI信息显示
    /// </summary>
    /// <param name="rig">要显示的3D刚体</param>
    /// <param name="customName">自定义显示区域名称（用于区分不同刚体）</param>
    public void AddRigInfoShow_3D(Rigidbody rig, string customName)
    {
        // 空值校验
        if (rig == null)
        {
            Debug.LogError("[RigidbodyGUITestManager] 注册3D刚体失败：刚体对象为Null！");
            return;
        }
        // 重复注册校验
        if (_rig3DShowDic.ContainsKey(rig))
        {
            Debug.LogWarning($"[RigidbodyGUITestManager] 3D刚体{rig.name}已注册，自定义名称：{_rig3DShowDic[rig].showName}，无需重复注册");
            return;
        }
        // 添加到字典
        _rig3DShowDic.Add(rig, new Rig3DShowInfo
        {
            rig = rig,
            showName = string.IsNullOrEmpty(customName) ? $"3D刚体_{rig.name}" : customName
        });
        Debug.Log($"[RigidbodyGUITestManager] 3D刚体注册成功，显示名称：{customName}");
    }

    /// <summary>
    /// 注销2D刚体，移除GUI信息显示
    /// </summary>
    /// <param name="rig">要移除的2D刚体</param>
    public void RemoveRigInfoShow_2D(Rigidbody2D rig)
    {
        if (rig == null)
        {
            Debug.LogError("[RigidbodyGUITestManager] 注销2D刚体失败：刚体对象为Null！");
            return;
        }
        if (_rig2DShowDic.ContainsKey(rig))
        {
            string showName = _rig2DShowDic[rig].showName;
            _rig2DShowDic.Remove(rig);
            Debug.Log($"[RigidbodyGUITestManager] 2D刚体注销成功，显示名称：{showName}");
        }
        else
        {
            Debug.LogWarning($"[RigidbodyGUITestManager] 2D刚体{rig.name}未注册，无需注销");
        }
    }

    /// <summary>
    /// 注销3D刚体，移除GUI信息显示
    /// </summary>
    /// <param name="rig">要移除的3D刚体</param>
    public void RemoveRigInfoShow_3D(Rigidbody rig)
    {
        if (rig == null)
        {
            Debug.LogError("[RigidbodyGUITestManager] 注销3D刚体失败：刚体对象为Null！");
            return;
        }
        if (_rig3DShowDic.ContainsKey(rig))
        {
            string showName = _rig3DShowDic[rig].showName;
            _rig3DShowDic.Remove(rig);
            Debug.Log($"[RigidbodyGUITestManager] 3D刚体注销成功，显示名称：{showName}");
        }
        else
        {
            Debug.LogWarning($"[RigidbodyGUITestManager] 3D刚体{rig.name}未注册，无需注销");
        }
    }

    /// <summary>
    /// 清空所有注册的刚体信息（批量注销）
    /// </summary>
    public void ClearAllRigShowInfo()
    {
        _rig3DShowDic.Clear();
        _rig2DShowDic.Clear();
        Debug.Log("[RigidbodyGUITestManager] 已清空所有注册的刚体显示信息");
    }
    #endregion

    #region 生命周期（单例初始化+资源清理）
    protected override void Awake()
    {
        base.Awake();
        // 初始化字典（防止空引用）
        _rig3DShowDic = new Dictionary<Rigidbody, Rig3DShowInfo>();
        _rig2DShowDic = new Dictionary<Rigidbody2D, Rig2DShowInfo>();
        Debug.Log("[RigidbodyGUITestManager] 单例初始化完成，刚体信息显示管理器已就绪");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 清理字典，避免内存泄漏
        ClearAllRigShowInfo();
    }
    #endregion

    #region 核心GUI绘制逻辑
    private void OnGUI()
    {
        // 总开关关闭，直接不绘制
        if (!IsShowRigInfo) return;

        int originalFontSize = GUI.skin.label.fontSize;
        Color originalColor = GUI.color;
        try
        {
            // 设置全局GUI样式
            GUI.skin.label.fontSize = guiFontSize;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            float currentDrawY = startPosY;

            DrawAll3DRigidbodyInfo(ref currentDrawY);

            DrawAll2DRigidbodyInfo(ref currentDrawY);
        }
        finally
        {
            // 恢复原始GUI样式和颜色
            GUI.skin.label.fontSize = originalFontSize;
            GUI.color = originalColor;
        }
    }

    /// <summary>
    /// 绘制所有3D刚体信息（自动排版）
    /// </summary>
    /// <param name="currentY">当前绘制Y坐标（引用传递，实现累加）</param>
    private void DrawAll3DRigidbodyInfo(ref float currentY)
    {
        if (_rig3DShowDic.Count == 0) return;

        // 遍历所有注册的3D刚体
        foreach (var kvp in _rig3DShowDic)
        {
            Rig3DShowInfo showInfo = kvp.Value;
            Rigidbody rig = showInfo.rig;
            string showName = showInfo.showName;

            // 刚体被销毁/为空，标红提示并跳过
            if (rig == null)
            {
                GUI.color = errorColor;
                GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight), $"{showName} | 刚体已销毁/未赋值！");
                GUI.color = Color.white;
                // 累加Y坐标，保留间距
                currentY += lineHeight + areaSpacing;
                continue;
            }

            GUI.color = titleColor;
            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight), $"【{showName} - 3D刚体信息】");
            currentY += lineHeight;
            GUI.color = Color.white;

            Vector3 vel = rig.velocity;
            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"实时速度 | X：{vel.x:F2} | Y（垂直）：{vel.y:F2} | Z：{vel.z:F2}");
            currentY += lineHeight;

            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"物理状态 | 受重力：{rig.useGravity} | 运动学：{rig.isKinematic} | 检测碰撞：{!rig.detectCollisions}");
            currentY += lineHeight;

            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"基础属性 | 质量：{rig.mass:F1} | 线性阻力：{rig.drag:F1} | 角阻力：{rig.angularDrag:F1}");
            currentY += lineHeight;

            Vector3 angularVel = rig.angularVelocity;
            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"角速度 | X：{angularVel.x:F2} | Y：{angularVel.y:F2} | Z：{angularVel.z:F2}");
            currentY += lineHeight;

            // 刚体信息区域之间添加间距
            currentY += areaSpacing;
        }
    }

    /// <summary>
    /// 绘制所有2D刚体信息（自动排版，适配2D刚体属性）
    /// </summary>
    /// <param name="currentY">当前绘制Y坐标</param>
    private void DrawAll2DRigidbodyInfo(ref float currentY)
    {
        if (_rig2DShowDic.Count == 0)
            return;

        // 遍历所有注册的2D刚体
        foreach (var kvp in _rig2DShowDic)
        {
            Rig2DShowInfo showInfo = kvp.Value;
            Rigidbody2D rig = showInfo.rig;
            string showName = showInfo.showName;

            // 刚体被销毁/为空，标红提示并跳过
            if (rig == null)
            {
                GUI.color = errorColor;
                GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight), $"{showName} | 刚体已销毁/未赋值！");
                GUI.color = Color.white;
                // 累加Y坐标，保留间距
                currentY += lineHeight + areaSpacing;
                continue;
            }

            GUI.color = titleColor;
            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight), $"【{showName} - 2D刚体信息】");
            currentY += lineHeight;
            GUI.color = Color.white;

            Vector2 vel = rig.velocity;
            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"实时速度 | X：{vel.x:F2} | Y：{vel.y:F2}");
            currentY += lineHeight;

            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"物理状态 | 受重力：{rig.gravityScale > 0} | 运动学：{rig.isKinematic} | 检测碰撞：{rig.collisionDetectionMode != CollisionDetectionMode2D.Discrete}");
            currentY += lineHeight;

            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"基础属性 | 质量：{rig.mass:F1} | 线性阻力：{rig.drag:F1} | 角阻力：{rig.angularDrag:F1} | 重力缩放：{rig.gravityScale:F1}");
            currentY += lineHeight;

            float angularVel = rig.angularVelocity;
            GUI.Label(new Rect(startPosX, currentY, contentWidth, lineHeight),
                $"角速度（绕Z轴）：{angularVel:F2} °/s");
            currentY += lineHeight;

            // 刚体信息区域之间添加间距
            currentY += areaSpacing;
        }
    }
    #endregion
}