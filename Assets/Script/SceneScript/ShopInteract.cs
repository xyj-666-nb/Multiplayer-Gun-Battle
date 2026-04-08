using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class ShopInteract : BaseSceneInteract
{
    public static ShopInteract Instance;

    [Header("===== 虚拟相机赋值 =====")]
    [Tooltip("玩家主相机")]
    public CinemachineVirtualCamera playerVC;
    [Tooltip("商店专属相机")]
    public CinemachineVirtualCamera shopVC;

    [Header("===== Cinemachine切换速度配置 =====")]
    [Tooltip("默认切换速度（秒）")]
    public float defaultBlendTime = 1f;

    [Header("事件回调")]
    [Tooltip("相机完全移动到位后触发")]
    public UnityEvent OnCameraArrived;
    [Tooltip("商店完全关闭后触发")]
    public UnityEvent OnShopClosed;

    // 优先级配置 (参考 MapChooseWall)
    private readonly int _activePriority = 20;
    private readonly int _inactivePriority = -10;

    // 状态变量
    private bool isShopActive = false;
    private Coroutine cameraBlendCoroutine;

    // Cinemachine Brain缓存
    private CinemachineBrain _cinemachineBrain;

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        // 单例安全
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 获取Brain
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (_cinemachineBrain == null)
        {
            Debug.LogError("场景中未找到Cinemachine Brain！");
        }

        // 绑定事件
        OnCameraArrived.AddListener(ShowUI);
    }

    #endregion

    #region 核心交互逻辑
    public override void TriggerEffect()
    {
        if (isShopActive || shopVC == null)
            return;
        if(playerVC==null)
        {
            playerVC=MyCameraControl.Instance.virtualCamera;//自动获取
        }

        EnterShopSystem();
    }

    public override void triggerEnterRange() { }
    public override void triggerExitRange() { }
    #endregion

    #region 系统入口/出口
    /// <summary>
    /// 进入商店系统
    /// </summary>
    private void EnterShopSystem()
    {
        isShopActive = true;
        Debug.Log("进入商店系统");

        UImanager.Instance.GetPanel<PlayerPanel>().SimpleHidePanel();
        UImanager.Instance.GetPanel<PlayerPreparaPanel>()?.SimpleHidePanel();//如果有准备面板的话也隐藏

        SetCinemachineBlendTime(defaultBlendTime);

        SwitchCamera(shopVC);

        if (cameraBlendCoroutine != null)
            StopCoroutine(cameraBlendCoroutine);
        cameraBlendCoroutine = StartCoroutine(WaitForCameraBlendComplete());
    }

    /// <summary>
    /// 退出商店系统，供外部（如GoodsPanel退出按钮）调用
    /// </summary>
    public void ExitShopSystem()
    {
        if (!isShopActive) 
            return;

        isShopActive = false;
        Debug.Log("退出商店系统");

        if (cameraBlendCoroutine != null)
            StopCoroutine(cameraBlendCoroutine);

        SetCinemachineBlendTime(defaultBlendTime);

        SwitchCamera(playerVC);
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
        UImanager.Instance.GetPanel<PlayerPreparaPanel>()?.SimpleShowPanel();
        OnShopClosed?.Invoke();
    }
    #endregion

    #region 核心相机切换 
    private void SwitchCamera(CinemachineVirtualCamera targetVC)
    {
        if (targetVC == null) return;

        SetAllCameraInactive();

        targetVC.Priority = _activePriority;

        Debug.Log($"商店相机切换至: {targetVC.name}");
    }

    private void SetAllCameraInactive()
    {
        if (playerVC == null)
            playerVC = MyCameraControl.Instance.virtualCamera;//自动获取

        playerVC.Priority = _inactivePriority;
        if (shopVC != null)
            shopVC.Priority = _inactivePriority;
    }
    #endregion

    #region 工具方法 
    private void SetCinemachineBlendTime(float blendTime)
    {
        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.m_DefaultBlend.m_Time = blendTime;
            // 强制把混合模式设为 EaseInOut，防止是 Cut
            _cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            Debug.Log($"Cinemachine混合时间已设置为：{blendTime}秒");
        }
    }

    private System.Collections.IEnumerator WaitForCameraBlendComplete()
    {
        yield return new WaitForSeconds(defaultBlendTime);
        OnCameraArrived?.Invoke();
    }

    public void ShowUI()
    {
        UImanager.Instance.ShowPanel<GoodsPanel>();
    }
    #endregion
}