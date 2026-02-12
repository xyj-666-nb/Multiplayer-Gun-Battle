using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 调试GUI管理器
/// 注册调试按钮、按组分类展示、独立折叠/展开每组按钮
/// 1. 先注册组(不注册也会自动注册)；2. 注册按钮时指定组名（不填=默认组）；3. 总开关控制是否显示
/// </summary>
public class Developer_GUITestManger : SingleMonoAutoBehavior<Developer_GUITestManger>
{
    #region 管理器内部逻辑
    #region 核心字段
    public bool IsStartGUI = true; // GUI总开关
    private bool _isMainPanelExpand = false; // 总面板是否展开
    private string _mainButtonName = "调试工具面板"; // 总管理按钮名称
    private const string DEFAULT_GROUP_NAME = "默认组"; // 默认组名称

    // 存储所有组的普通按钮：Key=组名，Value=该组的按钮列表
    private Dictionary<string, List<GuiButtonInfo>> _groupButtonDict = new Dictionary<string, List<GuiButtonInfo>>();
    // 存储每个组的折叠状态：Key=组名，Value=是否展开
    private Dictionary<string, bool> _groupExpandStates = new Dictionary<string, bool>();
    // 双向按钮相关存储
    private Dictionary<string, List<GuiTwoWayButtonInfo>> _groupTwoWayButtonDict = new Dictionary<string, List<GuiTwoWayButtonInfo>>(); // 按组存储双向按钮
    private readonly Color TwoWayBtnTriggerColor = ColorManager.LightGreen; // 触发态浅绿色

    // 普通按钮信息结构体
    private struct GuiButtonInfo
    {
        public string buttonName; // 按钮显示名称
        public Action onButtonClick; // 无参点击回调

        // 构造函数
        public GuiButtonInfo(string name, Action clickAction)
        {
            buttonName = name;
            onButtonClick = clickAction;
        }
    }

    // 双向按钮信息结构体
    private class GuiTwoWayButtonInfo
    {
        public string triggerName;   // 开启态显示名称
        public string cancelName;    // 关闭态显示名称
        public Action onTrigger;     // 开启逻辑
        public Action onCancel;      // 关闭逻辑
        public bool isTriggered;     // 当前是否为触发态（false=初始/关闭，true=开启）

        // 构造函数
        public GuiTwoWayButtonInfo(string trigName, string cancName, Action trigAction, Action cancAction)
        {
            triggerName = trigName;
            cancelName = cancName;
            onTrigger = trigAction;
            onCancel = cancAction;
            isTriggered = false; // 初始必为false，确保第一次点击是开启
        }
    }
    #endregion

    #region GUI绘制
    private void OnGUI()
    {
        if (!IsStartGUI)
            return;

        DrawMainControlButton();

        if (_isMainPanelExpand)
        {
            DrawAllGroupButtons();
        }
    }

    /// <summary>
    /// 绘制总管理按钮
    /// </summary>
    private void DrawMainControlButton()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 200, 20, 180, 50));
        {
            GUI.backgroundColor = _isMainPanelExpand ? Color.green : Color.gray;
            if (GUILayout.Button(_mainButtonName + (_isMainPanelExpand ? " [收起]" : " [展开]"), GUILayout.Height(40)))
            {
                _isMainPanelExpand = !_isMainPanelExpand;
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.EndArea();
    }

    /// <summary>
    /// 绘制所有分组的按钮
    /// </summary>
    private void DrawAllGroupButtons()
    {
        // 按钮矩阵区域：屏幕右上角，固定宽高
        GUILayout.BeginArea(new Rect(Screen.width - 200, 80, 180, Screen.height - 100));
        {
            GUILayout.BeginVertical("Box");
            {
                // 无任何按钮时的空提示
                if (_groupButtonDict.Count == 0 && _groupTwoWayButtonDict.Count == 0 || IsAllGroupsEmpty())
                {
                    GUILayout.Label("暂无注册的调试按钮", GUILayout.Height(30));
                }
                else
                {
                    // 遍历所有组，按组绘制折叠面板
                    foreach (var groupPair in _groupButtonDict)
                    {
                        string groupName = groupPair.Key;
                        List<GuiButtonInfo> buttonList = groupPair.Value;

                        // 绘制组的折叠/展开头部
                        DrawGroupHeader(groupName);

                        // 组展开时，绘制该组所有按钮
                        if (_groupExpandStates[groupName])
                        {
                            DrawGroupButtonList(groupName, buttonList);
                        }
                    }
                }
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }

    /// <summary>
    /// 绘制单个组的头部
    /// </summary>
    /// <param name="groupName">组名</param>
    private void DrawGroupHeader(string groupName)
    {
        GUILayout.BeginHorizontal();
        {
            // 计算组内总按钮数（普通+双向）
            int totalBtnCount = _groupButtonDict[groupName].Count;
            if (_groupTwoWayButtonDict.ContainsKey(groupName))
            {
                totalBtnCount += _groupTwoWayButtonDict[groupName].Count;
            }

            // 组折叠/展开按钮样式
            GUI.backgroundColor = _groupExpandStates[groupName] ? Color.cyan : Color.gray;
            string groupHeaderText = $"{groupName} [{totalBtnCount}个按钮]" + (_groupExpandStates[groupName] ? " ▼" : " ▶");
            if (GUILayout.Button(groupHeaderText, GUILayout.Height(30)))
            {
                _groupExpandStates[groupName] = !_groupExpandStates[groupName];
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.EndHorizontal();
    }


    /// <summary>
    /// 绘制单个组的按钮列表
    /// </summary>
    /// <param name="groupName">组名</param>
    /// <param name="buttonList">普通按钮列表</param>
    private void DrawGroupButtonList(string groupName, List<GuiButtonInfo> buttonList)
    {
        // 左侧缩进，区分组头部和按钮
        GUILayout.BeginHorizontal();
        {
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            {
                foreach (var btnInfo in buttonList)
                {
                    if (GUILayout.Button(btnInfo.buttonName, GUILayout.Height(35)))
                    {
                        try
                        {
                            btnInfo.onButtonClick?.Invoke();
                            Debug.Log($"点击调试按钮：[{groupName}] -> {btnInfo.buttonName}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"执行按钮[{groupName}/{btnInfo.buttonName}]回调出错：{e.Message}", this);
                        }
                    }
                }

                if (_groupTwoWayButtonDict.ContainsKey(groupName) && _groupTwoWayButtonDict[groupName].Count > 0)
                {
                    if (buttonList.Count > 0)
                    {
                        GUILayout.Space(4);
                        GUILayout.Label("▶ 双向按钮", GUILayout.Height(20));
                        GUILayout.Space(4);
                    }
                    for (int i = 0; i < _groupTwoWayButtonDict[groupName].Count; i++)
                    {
                        var twoWayBtn = _groupTwoWayButtonDict[groupName][i];
                        GUI.backgroundColor = Color.gray;
                        string showName = twoWayBtn.isTriggered ? twoWayBtn.cancelName : twoWayBtn.triggerName;
                        if (twoWayBtn.isTriggered)
                        {
                            GUI.backgroundColor = TwoWayBtnTriggerColor;
                        }

                        if (GUILayout.Button(showName, GUILayout.Height(35)))
                        {
                            twoWayBtn.isTriggered = !twoWayBtn.isTriggered;
                            if (twoWayBtn.isTriggered)
                            {
                                twoWayBtn.onTrigger?.Invoke(); // 触发开启逻辑
                            }
                            else
                            {
                                twoWayBtn.onCancel?.Invoke(); // 触发关闭逻辑
                            }

                            _groupTwoWayButtonDict[groupName][i] = twoWayBtn;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();

        // 组内按钮底部分隔线
        GUILayout.Space(5);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(5);
    }
    #endregion

    #region 内部辅助方法
    /// <summary>
    /// 校验并获取有效的组名
    /// </summary>
    /// <param name="inputGroupName">输入的组名</param>
    /// <returns>有效的组名</returns>
    private string GetValidGroupName(string inputGroupName)
    {
        // 空组名 → 归默认组
        if (string.IsNullOrEmpty(inputGroupName))
        {
            return DEFAULT_GROUP_NAME;
        }

        // 组名存在 → 直接返回
        if (_groupButtonDict.ContainsKey(inputGroupName))
        {
            return inputGroupName;
        }

        // 核心修改：非空且不存在的组名 → 自动创建该组
        Debug.Log($"组[{inputGroupName}]不存在，已自动创建该组！");
        RegisterGroup(inputGroupName);
        return inputGroupName;
    }

    /// <summary>
    /// 检查所有组是否都无按钮（普通+双向）
    /// </summary>
    /// <returns>true=全空，false=至少有一个按钮</returns>
    private bool IsAllGroupsEmpty()
    {
        // 检查普通按钮
        foreach (var groupPair in _groupButtonDict)
        {
            if (groupPair.Value.Count > 0)
            {
                return false;
            }
        }
        // 检查双向按钮
        foreach (var groupPair in _groupTwoWayButtonDict)
        {
            if (groupPair.Value.Count > 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 初始化默认组+单例跨场景保留
    /// </summary>
    protected override void Awake()
    {
        // 初始化普通按钮默认组
        if (!_groupButtonDict.ContainsKey(DEFAULT_GROUP_NAME))
        {
            RegisterGroup(DEFAULT_GROUP_NAME);
        }
        // 初始化双向按钮默认组存储
        if (!_groupTwoWayButtonDict.ContainsKey(DEFAULT_GROUP_NAME))
        {
            _groupTwoWayButtonDict.Add(DEFAULT_GROUP_NAME, new List<GuiTwoWayButtonInfo>());
        }
        // 单例对象跨场景保留，避免切换场景后调试面板消失
        DontDestroyOnLoad(gameObject);
    }
    #endregion
    #endregion

    #region 外部接口
    #region 注册按钮组
    /// <summary>
    /// 注册按钮组（自动初始化普通+双向按钮存储）
    /// </summary>
    /// <param name="groupName">组名称（不能为空）</param>
    public void RegisterGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogError("注册按钮组失败：组名称不能为空！");
            return;
        }
        if (_groupButtonDict.ContainsKey(groupName))
        {
            Debug.LogWarning($"按钮组[{groupName}]已存在，无需重复注册！");
            return;
        }
        // 初始化普通按钮列表和折叠状态
        _groupButtonDict.Add(groupName, new List<GuiButtonInfo>());
        _groupExpandStates.Add(groupName, true); // 新组默认展开
        // 初始化双向按钮列表
        _groupTwoWayButtonDict.Add(groupName, new List<GuiTwoWayButtonInfo>());
        Debug.Log($"成功创建按钮组：[{groupName}]（含普通+双向按钮存储）");
    }
    #endregion

    #region 注册普通无参按钮
    /// <summary>
    /// 注册无参普通按钮到指定组
    /// </summary>
    /// <param name="buttonShowName">按钮显示名称</param>
    /// <param name="noParamAction">无参点击回调</param>
    /// <param name="groupName">归属组名（默认=默认组）</param>
    public void RegisterGuiButton(string buttonShowName, Action noParamAction, string groupName = DEFAULT_GROUP_NAME)
    {
        if (string.IsNullOrEmpty(buttonShowName) || noParamAction == null)
        {
            Debug.LogError("注册普通按钮失败：按钮名称不能为空，回调函数不能为null！");
            return;
        }

        string targetGroupName = GetValidGroupName(groupName);
        // 避免同组重复注册同名按钮
        foreach (var btnInfo in _groupButtonDict[targetGroupName])
        {
            if (btnInfo.buttonName == buttonShowName)
            {
                Debug.LogWarning($"组[{targetGroupName}]已存在同名按钮：{buttonShowName}，无需重复注册！");
                return;
            }
        }
        // 添加到目标组
        _groupButtonDict[targetGroupName].Add(new GuiButtonInfo(buttonShowName, noParamAction));
        Debug.Log($"普通按钮[{buttonShowName}]已注册到组[{targetGroupName}]");
    }
    #endregion

    #region 注册双向切换按钮
    /// <summary>
    /// 注册双向切换按钮
    /// </summary>
    /// <param name="ButtonTriggerName">开启态名称</param>
    /// <param name="ButtonCancelName">关闭态名称</param>
    /// <param name="triggerAction">开启逻辑</param>
    /// <param name="CancelAction">关闭逻辑</param>
    /// <param name="groupName">归属组名</param>
    public void RegisterGuiButton_TwoWay(string ButtonTriggerName, string ButtonCancelName, Action triggerAction, Action CancelAction, string groupName = DEFAULT_GROUP_NAME)
    {
        if (string.IsNullOrEmpty(ButtonTriggerName) || string.IsNullOrEmpty(ButtonCancelName) || triggerAction == null || CancelAction == null)
        {
            Debug.LogError("注册双向按钮失败：名称/回调不能为空！");
            return;
        }

        string targetGroupName = GetValidGroupName(groupName);
        // 避免同组重复注册相同双向按钮
        string btnUniqueKey = $"{ButtonTriggerName}_{ButtonCancelName}";
        foreach (var twoWayBtn in _groupTwoWayButtonDict[targetGroupName])
        {
            string existKey = $"{twoWayBtn.triggerName}_{twoWayBtn.cancelName}";
            if (existKey == btnUniqueKey)
            {
                Debug.LogWarning($"组[{targetGroupName}]已存在双向按钮[{btnUniqueKey}]，无需重复注册！");
                return;
            }
        }
        // 添加双向按钮到目标组
        _groupTwoWayButtonDict[targetGroupName].Add(new GuiTwoWayButtonInfo(ButtonTriggerName, ButtonCancelName, triggerAction, CancelAction));
        Debug.Log($"双向按钮[{btnUniqueKey}]已注册到组[{targetGroupName}]");
    }
    #endregion

    #region 清空按钮
    /// <summary>
    /// 清空指定组的所有按钮（普通+双向）
    /// </summary>
    /// <param name="groupName">组名（默认=默认组）</param>
    public void ClearGroupButtons(string groupName = DEFAULT_GROUP_NAME)
    {
        string targetGroupName = GetValidGroupName(groupName);
        // 清空普通按钮
        if (_groupButtonDict.ContainsKey(targetGroupName))
        {
            _groupButtonDict[targetGroupName].Clear();
        }
        // 清空双向按钮
        if (_groupTwoWayButtonDict.ContainsKey(targetGroupName))
        {
            _groupTwoWayButtonDict[targetGroupName].Clear();
        }
        Debug.Log($"已清空组[{targetGroupName}]的所有普通按钮和双向按钮！");
    }

    /// <summary>
    /// 清空所有组的所有按钮（普通+双向），保留默认组
    /// </summary>
    public void ClearAllRegisteredButtons()
    {
        _groupButtonDict.Clear();
        _groupExpandStates.Clear();
        _groupTwoWayButtonDict.Clear();
        // 重建默认组，避免后续注册报错
        RegisterGroup(DEFAULT_GROUP_NAME);
        _groupTwoWayButtonDict.Add(DEFAULT_GROUP_NAME, new List<GuiTwoWayButtonInfo>());
        Debug.Log("已清空所有组的所有按钮（保留默认组，普通+双向均清空）！");
    }
    #endregion

    #region 统一打开所有的调试信息(FPS调试，物理信息调试。输入系统的调试)
    public void IsShowAllInfo(bool IsShow)
    {
        if (IsShow)
        {
            InputInfoManager.Instance.SetKeyCheckState(true);
            FPSDisplayPanel.Instance.ShowFPS();
            RigidbodyGUITestManager.Instance.IsActiveInfo(true);
        }
        else
        {
            RigidbodyGUITestManager.Instance.IsActiveInfo(false);
            InputInfoManager.Instance.SetKeyCheckState(false);
            FPSDisplayPanel.Instance.HideFPS();
        }

    }
    #endregion
    #endregion
}