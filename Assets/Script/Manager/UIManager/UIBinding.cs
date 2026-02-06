using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIBinding : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("输入动作配置")]
    [SerializeField] private E_InputAction m_InputAction;

    public InputInfoManager.ActionEventCallbacks InputActionPack;

    private bool IsTrigger = false;
    private InputAction _targetInputAction;

    protected void Start()
    {
        InitTargetInputAction();
        AutoAssignInputActionPack();
    }

    /// <summary>
    /// 初始化目标 InputAction
    /// </summary>
    private void InitTargetInputAction()
    {
        // 根据枚举找到对应的 InputAction
        string actionName =  m_InputAction.ToString();
        _targetInputAction = InputInfoManager.Instance.playerInput.actions.FindAction(actionName);

    }

    /// <summary>
    /// 根据m_InputAction自动从InputInfoManager获取回调包，赋值给InputActionPack
    /// </summary>
    private void AutoAssignInputActionPack()
    {
        // 从InputInfoManager获取对应动作的回调包列表
        var callbacksList = InputInfoManager.Instance.GetActionEventCallbacks(m_InputAction);

        // 取第一个回调包赋值
        InputActionPack = callbacksList[0];
    }

    /// <summary>
    /// 构造 InputAction.CallbackContext
    /// </summary>
    private InputAction.CallbackContext CreateCallbackContext(InputActionPhase phase)
    {
        // 构造空上下文并手动赋值核心字段
        var context = new InputAction.CallbackContext();
        var phaseField = typeof(InputAction.CallbackContext).GetField("m_Phase",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var actionField = typeof(InputAction.CallbackContext).GetField("m_Action",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (phaseField != null) 
            phaseField.SetValue(context, phase);
        if (actionField != null)
            actionField.SetValue(context, _targetInputAction);

        return context;
    }

    /// <summary>
    /// 构造 CustomInputContext
    /// </summary>
    private InputInfoManager.CustomInputContext CreateCustomContext()
    {
        if (_targetInputAction == null) return null;

        return new InputInfoManager.CustomInputContext(
            _targetInputAction,
            _targetInputAction.phase,
            _targetInputAction.ReadValueAsObject()
        );
    }

    // 按钮按下：触发 OnStart + OnPerformed
    public void OnPointerDown(PointerEventData eventData)
    {
        IsTrigger = true;
        // 构造按下阶段的上下文
        var startContext = CreateCallbackContext(InputActionPhase.Started);
        var performContext = CreateCallbackContext(InputActionPhase.Performed);

        // 触发回调
        InputActionPack.OnStart?.Invoke(startContext);
        InputActionPack.OnPerformed?.Invoke(performContext);

    }

    // 按钮抬起：触发 OnCancel
    public void OnPointerUp(PointerEventData eventData)
    {
        IsTrigger = false;
        // 构造抬起阶段的上下文
        var cancelContext = CreateCallbackContext(InputActionPhase.Canceled);
        // 触发回调
        InputActionPack.OnCancel?.Invoke(cancelContext);

    }

    // 持续按住：触发 OnPerformedContinuous
    private void Update()
    {
        if (!IsTrigger || InputActionPack == null || _targetInputAction == null)
            return;

        // 构造持续触发的自定义上下文
        var customContext = CreateCustomContext();
        if (customContext == null) return;

        InputActionPack.OnPerformedContinuous?.Invoke(customContext);
    }

    // 销毁时移除注册的回调
    private void OnDestroy()
    {
        if (InputInfoManager.Instance == null || InputActionPack == null) 
            return;

        if (!InputActionPack.IsNull())
        {
            InputInfoManager.Instance.RemoveInputLogicEvent(m_InputAction, InputActionPack);
        }
    }

    public void RefreshControlPath()
    {
        InitTargetInputAction();
        // 刷新时重新自动赋值（适配改键后回调包变化）
        AutoAssignInputActionPack();
        Debug.Log($"UIBinding: 「{m_InputAction}」已刷新绑定！");
    }
}