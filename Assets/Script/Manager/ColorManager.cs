using UnityEngine;

/// <summary>
/// 颜色管理类：存储所有项目常用颜色
/// 按颜色类别分组，便于查找和维护
/// </summary>
public static class ColorManager
{
    #region 一、 基础颜色
    /// <summary>
    /// 纯黑
    /// </summary>
    public static readonly Color32 Black = new Color32(0, 0, 0, 255);
    /// <summary>
    /// 纯白
    /// </summary>
    public static readonly Color32 White = new Color32(255, 255, 255, 255);
    /// <summary>
    /// 纯红
    /// </summary>
    public static readonly Color32 Red = new Color32(255, 0, 0, 255);
    /// <summary>
    /// 纯绿
    /// </summary>
    public static readonly Color32 Green = new Color32(0, 255, 0, 255);
    /// <summary>
    /// 纯蓝
    /// </summary>
    public static readonly Color32 Blue = new Color32(0, 0, 255, 255);
    /// <summary>
    /// 纯黄
    /// </summary>
    public static readonly Color32 Yellow = new Color32(255, 255, 0, 255);
    /// <summary>
    /// 纯青
    /// </summary>
    public static readonly Color32 Cyan = new Color32(0, 255, 255, 255);
    /// <summary>
    /// 纯紫
    /// </summary>
    public static readonly Color32 Magenta = new Color32(255, 0, 255, 255);
    #endregion

    #region 二、 灰色系
    /// <summary>
    /// 极浅灰（接近白色，用作页面背景）
    /// </summary>
    public static readonly Color32 UltraLightGray = new Color32(245, 245, 245, 255);
    /// <summary>
    /// 浅灰（用作卡片背景/次要文本）
    /// </summary>
    public static readonly Color32 LightGray = new Color32(224, 224, 224, 255);
    /// <summary>
    /// 中灰（用作分割线/禁用状态背景）
    /// </summary>
    public static readonly Color32 MiddleGray = new Color32(192, 192, 192, 255);
    /// <summary>
    /// 深灰（用作次要文本/阴影）
    /// </summary>
    public static readonly Color32 DarkGray = new Color32(128, 128, 128, 255);
    /// <summary>
    /// 极深灰（接近黑色，用作标题阴影/深色背景）
    /// </summary>
    public static readonly Color32 UltraDarkGray = new Color32(64, 64, 64, 255);
    /// <summary>
    /// 透明灰（半透明，用作遮罩/弹窗背景）
    /// </summary>
    public static readonly Color32 TransparentGray = new Color32(128, 128, 128, 128);
    #endregion

    #region 三、 红色系
    /// <summary>
    /// 浅红（淡粉，用作提示背景/轻微警告）
    /// </summary>
    public static readonly Color32 LightRed = new Color32(255, 179, 186, 255);
    /// <summary>
    /// 粉红（玫瑰粉，用作装饰/女性向UI）
    /// </summary>
    public static readonly Color32 Pink = new Color32(255, 192, 203, 255);
    /// <summary>
    /// 砖红（珊瑚红，用作按钮强调色）
    /// </summary>
    public static readonly Color32 BrickRed = new Color32(220, 20, 60, 255);
    /// <summary>
    /// 深红（酒红，用作标题/重要提示文本）
    /// </summary>
    public static readonly Color32 DarkRed = new Color32(139, 0, 0, 255);
    /// <summary>
    /// 番茄红（用作错误提示/删除按钮）
    /// </summary>
    public static readonly Color32 TomatoRed = new Color32(255, 99, 71, 255);
    #endregion

    #region 四、 绿色系
    /// <summary>
    /// 极浅绿（淡绿，用作成功提示背景）
    /// </summary>
    public static readonly Color32 UltraLightGreen = new Color32(224, 255, 224, 255);
    /// <summary>
    /// 浅绿（薄荷绿，改键等待按钮颜色/正常状态提示）
    /// </summary>
    public static readonly Color32 LightGreen = new Color32(152, 251, 152, 255);
    /// <summary>
    /// 嫩绿（草绿，用作装饰/进度条完成色）
    /// </summary>
    public static readonly Color32 FreshGreen = new Color32(144, 238, 144, 255);
    /// <summary>
    /// 翠绿（用作按钮正常状态/成功文本）
    /// </summary>
    public static readonly Color32 EmeraldGreen = new Color32(0, 255, 127, 255);
    /// <summary>
    /// 深绿（森林绿，用作标题/重要成功提示）
    /// </summary>
    public static readonly Color32 DarkGreen = new Color32(0, 128, 0, 255);
    #endregion

    #region 五、 蓝色系
    /// <summary>
    /// 极浅蓝（淡蓝，用作页面背景/输入框默认背景）
    /// </summary>
    public static readonly Color32 UltraLightBlue = new Color32(224, 240, 255, 255);
    /// <summary>
    /// 浅蓝（天蓝，用作默认按钮/导航栏背景）
    /// </summary>
    public static readonly Color32 LightBlue = new Color32(173, 216, 230, 255);
    /// <summary>
    /// 天蓝（晴空蓝，用作装饰/次要按钮）
    /// </summary>
    public static readonly Color32 SkyBlue = new Color32(135, 206, 235, 255);
    /// <summary>
    /// 深蓝（海军蓝，用作标题/链接文本/主要按钮）
    /// </summary>
    public static readonly Color32 DarkBlue = new Color32(0, 0, 139, 255);
    /// <summary>
    /// 宝蓝（用作强调按钮/重要导航项）
    /// </summary>
    public static readonly Color32 RoyalBlue = new Color32(65, 105, 225, 255);
    #endregion

    #region 六、 黄色系
    /// <summary>
    /// 极浅黄（米黄，用作页面背景/卡片背景）
    /// </summary>
    public static readonly Color32 UltraLightYellow = new Color32(255, 250, 224, 255);
    /// <summary>
    /// 浅黄（奶油黄，用作提示背景/次要装饰）
    /// </summary>
    public static readonly Color32 LightYellow = new Color32(255, 248, 225, 255);
    /// <summary>
    /// 柠檬黄（用作高亮提示/警告按钮）
    /// </summary>
    public static readonly Color32 LemonYellow = new Color32(255, 250, 0, 255);
    /// <summary>
    /// 橙黄（用作警告提示/进度条加载色）
    /// </summary>
    public static readonly Color32 OrangeYellow = new Color32(255, 165, 0, 255);
    /// <summary>
    /// 深黄（土黄，用作装饰/复古风格UI）
    /// </summary>
    public static readonly Color32 DarkYellow = new Color32(184, 134, 11, 255);
    #endregion

    #region 七、 紫色系
    /// <summary>
    /// 极浅紫（淡紫，用作装饰/女性向UI背景）
    /// </summary>
    public static readonly Color32 UltraLightPurple = new Color32(240, 230, 255, 255);
    /// <summary>
    /// 浅紫（薰衣草紫，用作次要按钮/装饰）
    /// </summary>
    public static readonly Color32 LightPurple = new Color32(204, 204, 255, 255);
    /// <summary>
    /// 紫罗兰（用作特殊功能按钮/标题装饰）
    /// </summary>
    public static readonly Color32 Violet = new Color32(138, 43, 226, 255);
    /// <summary>
    /// 深紫（茄紫，用作重要特殊功能/标题文本）
    /// </summary>
    public static readonly Color32 DarkPurple = new Color32(128, 0, 128, 255);
    #endregion

    #region 八、 功能色
    /// <summary>
    /// 成功色（浅绿，用作成功提示背景/图标）
    /// </summary>
    public static readonly Color32 Success = new Color32(152, 251, 152, 255);
    /// <summary>
    /// 成功色（深绿，用作成功提示文本/按钮）
    /// </summary>
    public static readonly Color32 SuccessDark = new Color32(0, 128, 0, 255);
    /// <summary>
    /// 警告色（浅黄，用作警告提示背景）
    /// </summary>
    public static readonly Color32 Warning = new Color32(255, 248, 225, 255);
    /// <summary>
    /// 警告色（橙黄，用作警告提示文本/按钮）
    /// </summary>
    public static readonly Color32 WarningDark = new Color32(255, 165, 0, 255);
    /// <summary>
    /// 错误色（浅红，用作错误提示背景）
    /// </summary>
    public static readonly Color32 Error = new Color32(255, 179, 186, 255);
    /// <summary>
    /// 错误色（深红，用作错误提示文本/删除按钮）
    /// </summary>
    public static readonly Color32 ErrorDark = new Color32(220, 20, 60, 255);
    /// <summary>
    /// 默认色（浅蓝，用作默认按钮/输入框）
    /// </summary>
    public static readonly Color32 Default = new Color32(173, 216, 230, 255);
    /// <summary>
    /// 默认色（深蓝，用作默认按钮hover/导航栏）
    /// </summary>
    public static readonly Color32 DefaultDark = new Color32(65, 105, 225, 255);
    /// <summary>
    /// 改键等待色
    /// </summary>
    public static readonly Color32 ChangeKeyWaiting = new Color32(152, 251, 152, 255);
    #endregion

    #region 颜色处理常用工具方法
    public static Color  SetColorAlpha( Color color,float AlphaValue)
    {
        return new Color(color.r, color.g, color.b, AlphaValue);
    }
    #endregion
}