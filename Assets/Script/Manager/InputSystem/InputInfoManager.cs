using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputInfoManager : SingleMonoAutoBehavior<InputInfoManager>
{
    #region 新输入系统

    #region 关键变量
    private string InputInfoJson;
    public InputInfo inputInfo;
    private const string FILENAME = "GameInputInfo";
    public PlayerInput playerInput;
    private Dictionary<string, List<ActionEventCallbacks>> actionEventMapDic = new Dictionary<string, List<ActionEventCallbacks>>();//在这里进行按键的一个逻辑注册

    public string CurrentTriggerKey;//当前按下的按钮
    public string currentTriggerKeyState;//当前触发按钮的状态
    public string CurrentTriggerAction;//当前触发的事件
    #endregion

    #region 管理器初始化
    protected override void Awake()
    {
        base.Awake();
        getInfo();

        TextAsset inputTextAsset = ResourcesManager.Instance.Load<TextAsset>("InputInfo/GameInput");
        InputInfoJson = inputTextAsset?.text ?? string.Empty;

        if (playerInput == null)
            playerInput = gameObject.AddComponent<PlayerInput>();
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        if (!string.IsNullOrEmpty(InputInfoJson) && inputInfo != null && inputInfo.KeyInfoDict.Count > 0)
        {
            ChangeKey();
        }
    }

    private void Start()
    {
        //对当前触发关联事件
        playerInput.onActionTriggered += HandleInputLogic;
    }
    #endregion

    #region 动作逻辑绑定注册
    public class ActionEventCallbacks
    {
        public Action<InputAction.CallbackContext> OnStart;     // 按下开始阶段
        public Action<InputAction.CallbackContext> OnPerformed; // 执行阶段
        public Action<InputAction.CallbackContext> OnCancel;    // 取消阶段
        public Action<CustomInputContext> OnPerformedContinuous; // 使用自定义上下文，支持持续触发

        public bool IsNull()//判断当前包是否为空
        {
            return OnStart == null && OnPerformed == null && OnCancel == null && OnPerformedContinuous == null;
        }
    }

    public class CustomInputContext
    {
        public InputAction Action; // 对应的输入动作
        public InputActionPhase Phase; // 输入阶段
        public object Value; // 输入值
        public string ActionName; // 动作名

        // 构造函数
        public CustomInputContext(InputAction action, InputActionPhase phase, object value)
        {
            Action = action;
            Phase = phase;
            Value = value;
            ActionName = action?.name ?? string.Empty;
        }
    }

    #region 注册
    /// <summary>
    ///注册输入逻辑函数
    /// </summary>
    public ActionEventCallbacks RegisterInputLogicEvent(E_InputAction Name,
        Action<InputAction.CallbackContext> OnStart = null,
        Action<InputAction.CallbackContext> OnPerformed = null,
        Action<InputAction.CallbackContext> OnCancel = null,
        Action<CustomInputContext> OnPerformedContinuous = null)
    {
        if (actionEventMapDic == null)
        {
            actionEventMapDic = new Dictionary<string, List<ActionEventCallbacks>>();
            Debug.LogWarning("InputInfoManager.RegisterInputLogicEvent：actionEventMapDic 为空，已重新初始化！");
        }

        ActionEventCallbacks actionEventCallbacks = new ActionEventCallbacks();
        actionEventCallbacks.OnStart = OnStart;
        actionEventCallbacks.OnPerformed = OnPerformed;
        actionEventCallbacks.OnCancel = OnCancel;
        actionEventCallbacks.OnPerformedContinuous = OnPerformedContinuous;

        string key = Name.ToString();

        if (!actionEventMapDic.ContainsKey(key))
        {
            // 键不存在就创建新的List，添加事件包后存入字典
            List<ActionEventCallbacks> eventPackList = new List<ActionEventCallbacks>();
            eventPackList.Add(actionEventCallbacks);
            actionEventMapDic.Add(key, eventPackList);
        }
        else
        {
            List<ActionEventCallbacks> existingList = actionEventMapDic[key];
            if (existingList == null)
            {
                existingList = new List<ActionEventCallbacks>();
                actionEventMapDic[key] = existingList;
                Debug.LogWarning($"InputInfoManager.RegisterInputLogicEvent：动作「{key}」的事件列表为空，已重新初始化！");
            }
            existingList.Add(actionEventCallbacks);
        }

        // 返回当前事件包，方便后续精准移除
        return actionEventCallbacks;
    }
    #endregion

    #region 移除
    /// <summary>
    ///移除关联事件
    /// </summary>
    /// <param name="name">输入动作枚举</param>
    /// <param name="SpecialPack">要移除的指定事件包）</param>
    public void RemoveInputLogicEvent(E_InputAction name, ActionEventCallbacks SpecialPack = null)
    {
        string key = name.ToString();

        if (!actionEventMapDic.ContainsKey(key))
        {
            Debug.LogWarning($"InputInfoManager.RemoveInputLogicEvent：未找到动作「{key}」对应的注册信息，移除失败！");
            return;
        }

        // 传入了确切的SpecialPack
        if (SpecialPack != null)
        {
            // 获取该动作对应的事件包列表
            List<ActionEventCallbacks> eventPackList = actionEventMapDic[key];
            // 移除指定的事件包
            if (eventPackList.Contains(SpecialPack))
            {
                eventPackList.Remove(SpecialPack);
                //如果列表为空，自动删除该键
                if (eventPackList.Count == 0)
                    actionEventMapDic.Remove(key);
            }
            else
            {
                Debug.LogWarning($"InputInfoManager.RemoveInputLogicEvent：动作「{key}」中未找到指定事件包，移除失败！");
            }
        }
        else//未传入SpecialPack，移除所有事件包
        {
            actionEventMapDic.Remove(key);
            Debug.Log($"InputInfoManager.RemoveInputLogicEvent：成功移除动作「{key}」对应的所有事件包！");
        }
    }
    #endregion

    #endregion

    #region 逻辑处理以及更新
    // 输入动作触发回调处理
    public void HandleInputLogic(InputAction.CallbackContext Context)
    {
        if (Context.action == null)
            return;

        string actionName = Context.action.name;
        // 核心修改：将方向动作映射为Move，GUI显示Move
        CurrentTriggerAction = actionName is "Up" or "Down" or "Left" or "Right" ? "Move" : actionName;

        if (!actionEventMapDic.ContainsKey(actionName))
        {
            Debug.Log($"InputInfoManager.HandleInputLogic：输入动作「{actionName}」触发，未注册事件回调，跳过分发！");
            return;
        }

        List<ActionEventCallbacks> eventPackList = actionEventMapDic[actionName];

        if (eventPackList == null || eventPackList.Count == 0)
        {
            Debug.LogWarning($"InputInfoManager.HandleInputLogic：动作「{actionName}」的事件列表为空，跳过逻辑分发并清理！");
            actionEventMapDic.Remove(actionName); // 清理空列表，优化内存
            return;
        }

        // 分发对应阶段的事件
        foreach (var eventPack in eventPackList)
        {
            if (eventPack == null) continue;

            switch (Context.phase)
            {
                // 按下开始阶段
                case InputActionPhase.Started:
                    eventPack.OnStart?.Invoke(Context);
                    break;
                // 执行阶段（持续/单次）
                case InputActionPhase.Performed:
                    eventPack.OnPerformed?.Invoke(Context);
                    break;
                // 取消/抬起阶段
                case InputActionPhase.Canceled:
                    eventPack.OnCancel?.Invoke(Context);
                    break;
            }
        }
    }

    // 持续触发检测 Update 方法
    private void Update()
    {
        if (IsStartKeyCheck)
            CheckAnyKeyPress();

        if (actionEventMapDic == null || actionEventMapDic.Count == 0)
            return;

        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputInfoManager.Update：playerInput或动作集未初始化，跳过持续触发检测！");
            return;
        }

        foreach (var keyValue in actionEventMapDic)
        {
            string actionName = keyValue.Key;
            List<ActionEventCallbacks> eventPackList = keyValue.Value;

            if (eventPackList == null || eventPackList.Count == 0)
                continue;

            // 找到对应的 InputAction
            InputAction action = playerInput.actions.FindAction(actionName);
            if (action == null || !action.enabled)
                continue;

            // 检测按键按住状态，触发持续回调
            if (action.phase == InputActionPhase.Started || action.phase == InputActionPhase.Performed)
            {
                // 构造自定义上下文
                CustomInputContext customContext = new CustomInputContext(
                    action,
                    action.phase,
                    action.ReadValueAsObject()
                );

                // 遍历事件包，持续触发回调
                foreach (var eventPack in eventPackList)
                {
                    eventPack?.OnPerformedContinuous?.Invoke(customContext);
                }
            }
        }
    }

    #endregion

    #region 改变按键
    /// <summary>
    /// 动态替换按键配置，刷新PlayerInput的动作集
    /// </summary>
    public void ChangeKey()
    {
        if (string.IsNullOrEmpty(InputInfoJson) || inputInfo == null || inputInfo.KeyNameList.Count == 0)
        {
            Debug.LogWarning("InputInfoManager.ChangeKey：配置文件为空或输入信息未初始化，跳过按键替换！");
            return;
        }

        string str = InputInfoJson;//获取一次模板文本
        // 遍历替换所有键位占位符
        foreach (var name in inputInfo.KeyNameList)
        {
            if (inputInfo.KeyInfoDict.TryGetValue(name, out string keyPath) && !string.IsNullOrEmpty(keyPath))
            {
                str = str.Replace("<" + name + ">", keyPath);// 替换为实际按键路径
            }
            else
            {
                Debug.LogWarning($"InputInfoManager.ChangeKey：KeyInfoDict中未找到「{name}」对应的按键路径，跳过替换！");
            }
        }

        try
        {
            // 重新加载动作集并启用
            playerInput.actions = InputActionAsset.FromJson(str);
            playerInput.actions.Enable();
            Debug.Log("InputInfoManager.ChangeKey：按键配置替换成功，动作集已刷新并启用！");
        }
        catch (Exception e)
        {
            Debug.LogError($"InputInfoManager.ChangeKey：解析InputActionAsset失败！错误信息：{e.Message}");
        }
    }
    #endregion

    #region 数据本地化
    /// <summary>
    /// 保存当前键位配置到本地
    /// </summary>
    public void SaveInfo()
    {
        if (inputInfo == null)
        {
            Debug.LogWarning("InputInfoManager.SaveInfo：输入信息未初始化，保存失败！");
            return;
        }
        JsonManager.Instance.SaveData(inputInfo, FILENAME);
        Debug.Log($"InputInfoManager.SaveInfo：键位数据已保存到：{Application.persistentDataPath}/{FILENAME}.json");
    }

    /// <summary>
    /// 从本地加载键位配置，无配置则创建默认实例
    /// </summary>
    public void getInfo()
    {
        inputInfo = JsonManager.Instance.LoadData<InputInfo>(FILENAME);
        // 核心修复：加载失败则创建新实例并初始化默认键位
        if (inputInfo == null)
        {
            inputInfo = new InputInfo();
            inputInfo.InitDefaultKeyInfo();
            Debug.LogWarning("InputInfoManager.getInfo：本地未找到键位配置，已创建新实例并初始化默认键位！");
        }
        // 容错：实例存在但键位为空，重新初始化默认键位
        else if (inputInfo.KeyInfoDict == null || inputInfo.KeyInfoDict.Count == 0)
        {
            inputInfo.InitDefaultKeyInfo();
            Debug.LogWarning("InputInfoManager.getInfo：本地键位配置为空，已重新初始化默认键位！");
        }
        else
        {
            Debug.Log($"InputInfoManager.getInfo：成功从本地加载键位配置，共{inputInfo.KeyInfoDict.Count}个键位！");
        }
    }
    #endregion

    #region 当前输入信息更新

    #region 是否开启检测
    private bool IsStartKeyCheck=false;
    /// <summary>
    /// 设置是否开启全局按键检测（控制CheckAnyKeyPress和OnGUI）
    /// </summary>
    public void SetKeyCheckState(bool IsActive)
    {
        IsStartKeyCheck = IsActive;
        if (IsActive)
            ResetKeyState();// 开启时重置按键状态
        Debug.Log($"InputInfoManager.SetKeyCheckState：按键检测已{(IsActive ? "开启" : "关闭")}");
    }
    #endregion

    #region 关键变量
    // 缓存最后检测到的按键/鼠标键
    private Key? lastDetectedKey;
    private MouseButton? lastDetectedMouseButton;

    private float keyPressStartTime; // 按键按下的起始时间
    private Key? currentHoldingKey; // 当前按住的键盘键
    private MouseButton? currentHoldingMouseButton; // 当前按住的鼠标键
    private bool isKeyHolding = false; // 是否处于按键按住状态

    // 长按阈值
    private const float LongPressThreshold = 0.5f;

    // 鼠标按键枚举
    private enum MouseButton
    {
        LeftButton,
        RightButton,
        MiddleButton
    }
    #endregion

    #region 检测总函数
    /// <summary>
    /// 全局按键检测总入口，检测键盘/鼠标输入并更新状态
    /// </summary>
    public void CheckAnyKeyPress()
    {
        bool hasNewInput = false;

        if (Keyboard.current != null)
        {
            hasNewInput = DetectKeyboardInput();
            if (hasNewInput) return;
        }

        if (Mouse.current != null)
        {
            hasNewInput = DetectMouseInput();
            if (hasNewInput) return;
        }

        // 更新长按状态
        if (isKeyHolding)
        {
            float pressDuration = Time.time - keyPressStartTime;
            currentTriggerKeyState = pressDuration >= LongPressThreshold ? "长按" : "按下";
        }
    }
    #endregion

    #region 预定义键盘有效按键范围
    // 预定义键盘有效按键范围
    List<Key> validKeyboardKeys = new List<Key>()
    {
        // 字母键
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M,
        Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
        // 数字键
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        // 功能键
        Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
        // 常用控制键
        Key.Space, Key.Enter, Key.Escape, Key.Tab, Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl,
        Key.LeftAlt, Key.RightAlt, Key.Backspace, Key.Delete,
        // 小键盘数字
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };
    #endregion

    #region 单独检测键盘输入
    /// <summary>
    /// 检测键盘输入并更新按键状态，WASD同步设置CurrentTriggerAction为Move
    /// </summary>
    /// <returns>是否检测到新输入</returns>
    private bool DetectKeyboardInput()
    {
        foreach (Key key in validKeyboardKeys)
        {
            KeyControl keyControl = Keyboard.current[key];
            if (keyControl == null) continue;

            // 按键按下
            if (keyControl.wasPressedThisFrame)
            {
                CurrentTriggerKey = GetKeyDisplayName(key);
                currentTriggerKeyState = "按下";
                // 核心修改：WASD按下时同步设置为Move
                CurrentTriggerAction = key is Key.W or Key.A or Key.S or Key.D ? "Move" : CurrentTriggerAction;
                lastDetectedKey = key;
                currentHoldingKey = key;
                currentHoldingMouseButton = null;
                keyPressStartTime = Time.time;
                isKeyHolding = true;
                return true;
            }
            // 按键抬起
            else if (keyControl.wasReleasedThisFrame)
            {
                CurrentTriggerKey = GetKeyDisplayName(key);
                currentTriggerKeyState = "释放";
                // 核心修改：WASD抬起时同步设置为Move
                CurrentTriggerAction = key is Key.W or Key.A or Key.S or Key.D ? "Move" : CurrentTriggerAction;
                lastDetectedKey = key;
                currentHoldingKey = null;
                isKeyHolding = false;
                return true;
            }
            // 按键持续按住
            else if (keyControl.isPressed && currentHoldingKey == key)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取键盘按键的友好显示名称（如W→前进，A→左移）
    /// </summary>
    private string GetKeyDisplayName(Key key)
    {
        return key switch
        {
            Key.W => "前进(W)",
            Key.S => "后退(S)",
            Key.A => "左移(A)",
            Key.D => "右移(D)",
            Key.Space => "空格",
            Key.Tab => "Tab",
            Key.LeftShift => "左Shift",
            Key.RightShift => "右Shift",
            _ => key.ToString()
        };
    }
    #endregion

    #region 单独检测鼠标输入
    /// <summary>
    /// 单独检测鼠标输入并更新按键状态
    /// </summary>
    /// <returns>是否检测到新输入</returns>
    private bool DetectMouseInput()
    {
        // 检测鼠标左键
        if (Mouse.current.leftButton != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                CurrentTriggerKey = "鼠标左键";
                currentTriggerKeyState = "按下";
                CurrentTriggerAction = "Attack";// 左键绑定攻击
                lastDetectedMouseButton = MouseButton.LeftButton;
                currentHoldingMouseButton = MouseButton.LeftButton;
                currentHoldingKey = null;
                keyPressStartTime = Time.time;
                isKeyHolding = true;
                return true;
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                CurrentTriggerKey = "鼠标左键";
                currentTriggerKeyState = "释放";
                CurrentTriggerAction = "Attack";// 左键绑定攻击
                lastDetectedMouseButton = MouseButton.LeftButton;
                currentHoldingMouseButton = null;
                isKeyHolding = false;
                return true;
            }
            else if (Mouse.current.leftButton.isPressed && currentHoldingMouseButton == MouseButton.LeftButton)
            {
                return false; // 交给总函数判断长按
            }
        }

        // 检测鼠标右键
        if (Mouse.current.rightButton != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CurrentTriggerKey = "鼠标右键";
                currentTriggerKeyState = "按下";
                lastDetectedMouseButton = MouseButton.RightButton;
                currentHoldingMouseButton = MouseButton.RightButton;
                currentHoldingKey = null;
                keyPressStartTime = Time.time;
                isKeyHolding = true;
                return true;
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                CurrentTriggerKey = "鼠标右键";
                currentTriggerKeyState = "释放";
                lastDetectedMouseButton = MouseButton.RightButton;
                currentHoldingMouseButton = null;
                isKeyHolding = false;
                return true;
            }
            else if (Mouse.current.rightButton.isPressed && currentHoldingMouseButton == MouseButton.RightButton)
            {
                return false; // 交给总函数判断长按
            }
        }

        // 检测鼠标中键（滚轮键）
        if (Mouse.current.middleButton != null)
        {
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                CurrentTriggerKey = "鼠标中键";
                currentTriggerKeyState = "按下";
                lastDetectedMouseButton = MouseButton.MiddleButton;
                currentHoldingMouseButton = MouseButton.MiddleButton;
                currentHoldingKey = null;
                keyPressStartTime = Time.time;
                isKeyHolding = true;
                return true;
            }
            else if (Mouse.current.middleButton.wasReleasedThisFrame)
            {
                CurrentTriggerKey = "鼠标中键";
                currentTriggerKeyState = "释放";
                lastDetectedMouseButton = MouseButton.MiddleButton;
                currentHoldingMouseButton = null;
                isKeyHolding = false;
                return true;
            }
            else if (Mouse.current.middleButton.isPressed && currentHoldingMouseButton == MouseButton.MiddleButton)
            {
                return false; // 交给总函数判断长按
            }
        }

        return false;
    }
    #endregion

    #region 重置按键状态
    /// <summary>
    /// 重置所有按键状态为初始值
    /// </summary>
    public void ResetKeyState(string state = "无")
    {
        currentTriggerKeyState = state;
        CurrentTriggerKey = "";
        CurrentTriggerAction = "";
        lastDetectedKey = null;
        lastDetectedMouseButton = null;
        currentHoldingKey = null;
        currentHoldingMouseButton = null;
        isKeyHolding = false;
        keyPressStartTime = 0;
    }
    #endregion

    #region 绘制信息
    /// <summary>
    /// OnGUI 实时显示按键和状态变量（屏幕左上角）
    /// </summary>
    private void OnGUI()
    {
        if (!IsStartKeyCheck)
            return;

        // 设置GUI样式，让文字更清晰（带黑色描边更易读）
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(5, 5, 5, 5);
        // 添加黑色描边
        style.fontStyle = FontStyle.Bold;
        style.contentOffset = new Vector2(1, 1);

        // 绘制按键信息
        GUI.Label(new Rect(10, 30, 300, 30), $"当前按键：{CurrentTriggerKey}", style);
        GUI.Label(new Rect(10, 60, 300, 30), $"按键状态：{currentTriggerKeyState}", style);
        GUI.Label(new Rect(10, 90, 300, 30), $"输入事件：{CurrentTriggerAction}", style);
    }
    #endregion

    #endregion

    #region 新输入系统-按键状态快捷判断（按下/抬起/持续按住）
    /// <summary>
    /// 新输入系统：判断动作对应的按键是否【单次按下】（同Input.GetKeyDown）
    /// </summary>
    public bool CheckNewActionKeyDown(E_InputAction action)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputInfoManager.CheckNewActionKeyDown：playerInput或动作集未初始化！");
            return false;
        }

        string actionName = action.ToString();
        InputAction targetAction = playerInput.actions.FindAction(actionName);
        if (targetAction == null || !targetAction.enabled)
        {
            Debug.LogWarning($"InputInfoManager.CheckNewActionKeyDown：未找到/未启用动作「{actionName}」！");
            return false;
        }

        return targetAction.WasPressedThisFrame();
    }

    /// <summary>
    /// 新输入系统：判断动作对应的按键是否【单次抬起】（同Input.GetKeyUp）
    /// </summary>
    public bool CheckNewActionKeyUp(E_InputAction action)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputInfoManager.CheckNewActionKeyUp：playerInput或动作集未初始化！");
            return false;
        }

        string actionName = action.ToString();
        InputAction targetAction = playerInput.actions.FindAction(actionName);
        if (targetAction == null || !targetAction.enabled)
        {
            Debug.LogWarning($"InputInfoManager.CheckNewActionKeyUp：未找到/未启用动作「{actionName}」！");
            return false;
        }

        return targetAction.WasReleasedThisFrame();
    }

    /// <summary>
    /// 新输入系统：判断动作对应的按键是否【持续按住】（同Input.GetKey）
    /// </summary>
    public bool CheckNewActionKeyHeld(E_InputAction action)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("InputInfoManager.CheckNewActionKeyHeld：playerInput或动作集未初始化！");
            return false;
        }

        string actionName = action.ToString();
        InputAction targetAction = playerInput.actions.FindAction(actionName);
        if (targetAction == null || !targetAction.enabled)
        {
            Debug.LogWarning($"InputInfoManager.CheckNewActionKeyHeld：未找到/未启用动作「{actionName}」！");
            return false;
        }

        return targetAction.phase == InputActionPhase.Started || targetAction.phase == InputActionPhase.Performed;
    }
    #endregion

    #endregion

    #region 旧输入系统-兼容层
    /// <summary>
    /// 传入事件获取当前事件的旧输入系统形式的按键
    /// </summary>
    /// <param name="inputAction">新输入系统动作枚举</param>
    /// <returns>对应的旧输入系统KeyCode，无匹配则返回KeyCode.None</returns>
    public KeyCode GetActionkeyCode(E_InputAction inputAction)
    {
        if (inputInfo == null || inputInfo.KeyInfoDict == null || inputInfo.KeyInfoDict.Count == 0)
        {
            Debug.LogError("InputInfoManager.GetActionkeyCode：输入配置inputInfo或键位字典KeyInfoDict未初始化！");
            return KeyCode.None;
        }

        string actionKey = inputAction.ToString();
        // 兼容Move动作，映射到Up的键位（W）
        if (actionKey == "Move")
            actionKey = "Up";

        if (!inputInfo.KeyInfoDict.TryGetValue(actionKey, out string newInputPath))
        {
            Debug.LogWarning($"InputInfoManager.GetActionkeyCode：动作「{actionKey}」未在KeyInfoDict中配置，请先在InputInfo添加键位！");
            return KeyCode.None;
        }

        string[] pathParts = newInputPath.Split('/');
        if (pathParts.Length != 2)
        {
            Debug.LogError($"InputInfoManager.GetActionkeyCode：按键路径格式错误「{newInputPath}」，请遵循<设备>/按键名格式！");
            return KeyCode.None;
        }
        string deviceType = pathParts[0].Replace("<", "").Replace(">", "").ToLower();
        string rawKeyName = pathParts[1].ToLower().Trim();

        switch (deviceType)
        {
            case "keyboard":
                return ConvertKeyboardKeyToKeyCode(rawKeyName); // 键盘键自动匹配
            case "mouse":
                return ConvertMouseKeyToKeyCode(rawKeyName);   // 鼠标键特殊适配
            default:
                Debug.LogWarning($"InputInfoManager.GetActionkeyCode：暂不支持「{deviceType}」设备，仅支持Keyboard/Mouse！");
                return KeyCode.None;
        }
    }

    /// <summary>
    /// 键盘键自动转换：新输入按键名 → KeyCode
    /// </summary>
    private KeyCode ConvertKeyboardKeyToKeyCode(string rawKeyName)
    {
        // 特殊键名适配（新输入与KeyCode命名不一致的情况，仅需维护少量特例）
        var specialKeyMap = new Dictionary<string, string>()
        {
            { "space", "Space" },
            { "leftshift", "LeftShift" },
            { "rightshift", "RightShift" },
            { "leftctrl", "LeftControl" },
            { "rightctrl", "RightControl" },
            { "leftalt", "LeftAlt" },
            { "rightalt", "RightAlt" },
            { "enter", "Return" },
            { "backspace", "Backspace" },
            { "escape", "Escape" }
        };

        string keyCodeName = specialKeyMap.TryGetValue(rawKeyName, out var name)
            ? name
            : char.ToUpper(rawKeyName[0]) + rawKeyName.Substring(1); // 首字母大写（如w→W，f→F）

        if (Enum.TryParse<KeyCode>(keyCodeName, out KeyCode targetKey))
        {
            return targetKey;
        }

        Debug.LogWarning($"InputInfoManager.ConvertKeyboardKeyToKeyCode：未识别的键盘键「{rawKeyName}」，请检查InputInfo配置是否正确！");
        return KeyCode.None;
    }

    /// <summary>
    /// 鼠标键转换：新输入鼠标按键名 → KeyCode
    /// </summary>
    private KeyCode ConvertMouseKeyToKeyCode(string rawKeyName)
    {
        return rawKeyName switch
        {
            "leftbutton" => KeyCode.Mouse0,
            "rightbutton" => KeyCode.Mouse1,
            "middlebutton" => KeyCode.Mouse2,
            "button4" => KeyCode.Mouse3,
            "button5" => KeyCode.Mouse4,
            _ => KeyCode.None
        };
    }

    /// <summary>
    /// 旧输入系统：判断动作对应的按键是否【单次按下】（同Input.GetKeyDown）
    /// </summary>
    public bool CheckActionKeyDown(E_InputAction action)
    {
        KeyCode key = GetActionkeyCode(action);
        return key != KeyCode.None && Input.GetKeyDown(key);
    }

    /// <summary>
    /// 旧输入系统：判断动作对应的按键是否【单次抬起】（同Input.GetKeyUp）
    /// </summary>
    public bool CheckActionKeyUp(E_InputAction action)
    {
        KeyCode key = GetActionkeyCode(action);
        return key != KeyCode.None && Input.GetKeyUp(key);
    }

    /// <summary>
    /// 旧输入系统：判断动作对应的按键是否【持续按住】（同Input.GetKey）
    /// </summary>
    public bool CheckActionKeyHeld(E_InputAction action)
    {
        KeyCode key = GetActionkeyCode(action);
        return key != KeyCode.None && Input.GetKey(key);
    }

    /// <summary>
    /// 旧输入系统：兼容鼠标按键的精准判断（同Input.GetMouseButtonDown）
    /// </summary>
    public bool CheckActionMouseButtonDown(E_InputAction action)
    {
        KeyCode key = GetActionkeyCode(action);
        if (key == KeyCode.Mouse0) return Input.GetMouseButtonDown(0);
        if (key == KeyCode.Mouse1) return Input.GetMouseButtonDown(1);
        if (key == KeyCode.Mouse2) return Input.GetMouseButtonDown(2);
        return false;
    }
    #endregion

    #region 获取对应动作的回调包
    /// <summary>
    /// 根据输入动作枚举，获取InputInfoManager中已注册的回调包列表
    /// </summary>
    /// <param name="action">输入动作枚举</param>
    /// <returns>回调包列表（无则返回null）</returns>
    public List<ActionEventCallbacks> GetActionEventCallbacks(E_InputAction action)
    {
        if (actionEventMapDic == null)
        {
            Debug.LogWarning("InputInfoManager.GetActionEventCallbacks：回调字典未初始化！");
            return null;
        }

        string actionKey = action.ToString();

        if (actionEventMapDic.TryGetValue(actionKey, out var callbacksList))
        {
            return callbacksList;
        }
        else
        {
            Debug.LogWarning($"InputInfoManager.GetActionEventCallbacks：未找到「{action}」对应的回调包！");
            return null;
        }
    }
    #endregion
}

[System.Serializable]
public class InputInfo
{
    #region 信息字典以及动作名名字列表
    public Dictionary<string, string> KeyInfoDict = new Dictionary<string, string>();
    public List<string> KeyNameList = new List<string>();//行为动作名字
    #endregion

    #region 初始化信息
    public InputInfo()
    {
        // 确保集合初始化，避免null
        if (KeyInfoDict == null)
            KeyInfoDict = new Dictionary<string, string>();
        if (KeyNameList == null)
            KeyNameList = new List<string>();
    }

    /// <summary>
    /// 手动初始化默认键位（WASD=方向，左键=攻击，空格=跳跃等）
    /// </summary>
    public void InitDefaultKeyInfo()
    {
        if (KeyInfoDict == null) KeyInfoDict = new Dictionary<string, string>();
        if (KeyNameList == null) KeyNameList = new List<string>();

        KeyInfoDict.Clear();
        KeyNameList.Clear();

        // 方向键（对应WASD）
        KeyInfoDict.Add("LeftMove", "<Keyboard>/a");
        KeyInfoDict.Add("RightMove", "<Keyboard>/d");
        // 核心动作
        KeyInfoDict.Add("Attack", "<Mouse>/leftButton");
        KeyInfoDict.Add("Jump", "<Keyboard>/space");
        KeyInfoDict.Add("Interact", "<Keyboard>/f");
        KeyInfoDict.Add("DialogueSkip", "<Keyboard>/tab");

        // 更新名字列表（用于ChangeKey替换占位符）
        foreach (string keyName in KeyInfoDict.Keys)
        {
            KeyNameList.Add(keyName);
        }

        Debug.Log($"InputInfo.InitDefaultKeyInfo：默认键位初始化完成，共{KeyInfoDict.Count}个动作！");
    }
    #endregion

    #region 获取对应的按键路径
    /// <summary>
    /// 返回对应的按键路径
    /// </summary>
    /// <param name="Name">动作名</param>
    /// <returns>对应的按键路径，若未找到返回 null</returns>
    public string FindkeyInfo(string Name)
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning("InputInfo.FindkeyInfo：传入的动作名为空！");
            return null;
        }
        if (KeyInfoDict == null || KeyInfoDict.Count == 0)
        {
            Debug.LogError("InputInfo.FindkeyInfo：键位字典未初始化或为空！");
            return null;
        }

        if (KeyInfoDict.TryGetValue(Name, out string keyPath))
        {
            return keyPath;
        }
        else
        {
            Debug.LogWarning($"InputInfo.FindkeyInfo：未找到动作「{Name}」对应的按键路径！");
            return null;
        }
    }
    #endregion

    #region 改变当前的动作按键
    /// <summary>
    /// 单个修改动作的按键配置并刷新
    /// </summary>
    public void ChangeKeyInfo(string Name, string KeyNewPath)
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning("InputInfo.ChangeKeyInfo：传入的动作名为空！");
            return;
        }
        if (string.IsNullOrEmpty(KeyNewPath) || !KeyNewPath.Contains("/"))
        {
            Debug.LogWarning("InputInfo.ChangeKeyInfo：传入的按键路径无效，需遵循<设备>/按键名格式！");
            return;
        }
        if (KeyInfoDict == null || KeyInfoDict.Count == 0)
        {
            Debug.LogError("InputInfo.ChangeKeyInfo：键位字典未初始化或为空，无法修改！");
            return;
        }

        if (KeyInfoDict.ContainsKey(Name))
        {
            KeyInfoDict[Name] = KeyNewPath;
            if (InputInfoManager.Instance != null)
            {
                InputInfoManager.Instance.ChangeKey();
                Debug.Log($"InputInfo.ChangeKeyInfo：动作「{Name}」的按键已修改为「{KeyNewPath}」并刷新！");
            }
            else
            {
                Debug.LogError("InputInfo.ChangeKeyInfo：InputInfoManager.Instance 为空，无法刷新按键配置！");
            }
        }
        else
        {
            Debug.LogWarning($"InputInfo.ChangeKeyInfo：未找到动作「{Name}」，修改失败！");
        }

    }
    #endregion

    #region 检查重复按键
    /// <summary>
    /// 检查按键路径是否已被其他动作使用（避免重复绑定）
    /// </summary>
    public bool CheckRepeat_Key(string keyPath)
    {
        if (string.IsNullOrEmpty(keyPath))
        {
            Debug.LogWarning("InputInfo.CheckRepeat_Key：传入的按键路径为空！");
            return false;
        }
        if (KeyInfoDict == null || KeyInfoDict.Count == 0)
        {
            Debug.LogError("InputInfo.CheckRepeat_Key：键位字典未初始化或为空，无法检查！");
            return false;
        }

        foreach (string path in KeyInfoDict.Values)
        {
            if (path == keyPath)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region 重置信息回默认信息
    /// <summary>
    /// 重置所有键位为默认配置并刷新
    /// </summary>
    public void ReturnDefaultKeyInfo()
    {
        InitDefaultKeyInfo();
        if (InputInfoManager.Instance != null)
        {
            InputInfoManager.Instance.ChangeKey();
            InputInfoManager.Instance.ResetKeyState();
            Debug.Log("InputInfo.ReturnDefaultKeyInfo：所有键位已重置为默认配置并刷新！");
        }

    }
    #endregion
}

/// <summary>
/// 输入动作枚举
/// </summary>
public enum E_InputAction
{
    LeftMove,//左移
    RightMove,//右移
    Attack,  // 攻击
    Jump,    // 跳跃
    Interact,// 交互
    DialogueSkip// 跳过对话

}