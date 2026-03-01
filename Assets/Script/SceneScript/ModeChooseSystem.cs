using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class ModeChooseSystem : MonoBehaviour
{
    public static ModeChooseSystem instance;

    [Header("按钮")]
    public Button OnlineButton;          // 多人模式选择
    public Button ConfirmOnlineButton;   // 确认进入多人模式
    public Button StandaloneButton;      // 单人选择
    public Button ConfirmStandaloneButton;// 确认进入单机模式
    public Button ReturnButton_Online;   // 多人模式返回
    public Button ReturnButton_Standalone;// 单机模式返回

    [Header("虚拟摄像机")]
    public CinemachineVirtualCamera MainCV;      // 主视角 
    public CinemachineVirtualCamera OnlineCV;    // 多人模式选择视角
    public CinemachineVirtualCamera StandaloneCV;// 单机模式选择视角
    public CinemachineVirtualCamera EnterCv;     // 进入视角
    public CinemachineVirtualCamera GameVc;      // 游戏视角

    [Header("Cinemachine切换配置")]
    [Tooltip("视角切换的混合时间（秒）")]
    public float defaultBlendTime = 1f;

    // 相机优先级配置
    private readonly int _activePriority = 10;
    private readonly int _inactivePriority = 0;

    // Cinemachine Brain缓存
    private CinemachineBrain _cinemachineBrain;

    private void Awake()
    {
        instance = this;

        // 获取Cinemachine Brain（用于控制混合速度）
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (_cinemachineBrain == null)
        {
            Debug.LogError("场景中未找到Cinemachine Brain！请确保Main Camera上有Cinemachine Brain组件！");
        }
    }

    public void EnterSystem()
    {
        GameStartCG.Instance.StopTimeLine();

        SwitchToMainCamera();

        // 绑定按钮事件
        OnlineButton.onClick.AddListener(SwitchToOnline);
        StandaloneButton.onClick.AddListener(SwitchToStandalone);
        ReturnButton_Online.onClick.AddListener(SwitchToMainCamera);
        ReturnButton_Standalone.onClick.AddListener(SwitchToMainCamera);

        // 绑定确认按钮事件
        ConfirmOnlineButton.onClick.AddListener(ConfirmOnline);
        ConfirmStandaloneButton.onClick.AddListener(ConfirmStandalone);
    }

    // 系统出口：退出模式选择系统
    public void ExitSystem()
    {
        // 解绑所有按钮事件
        OnlineButton.onClick.RemoveListener(SwitchToOnline);
        StandaloneButton.onClick.RemoveListener(SwitchToStandalone);
        ReturnButton_Online.onClick.RemoveListener(SwitchToMainCamera);
        ReturnButton_Standalone.onClick.RemoveListener(SwitchToMainCamera);
        ConfirmOnlineButton.onClick.RemoveListener(ConfirmOnline);
        ConfirmStandaloneButton.onClick.RemoveListener(ConfirmStandalone);

        // 【核心修改】退出时切换到游戏视角
        SwitchToGameCamera();

        // 退出时关闭所有UI
        HideAllUI();
    }

    #region 视角切换
    private void SwitchToOnline()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(OnlineCV);
        ShowOnlineUI();
    }

    private void SwitchToStandalone()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(StandaloneCV);
        ShowStandaloneUI();
    }

    private void SwitchToMainCamera()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(MainCV);
        HideAllUI();
    }

    // 【新增】切换到游戏视角
    private void SwitchToGameCamera()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(GameVc);
    }

    /// <summary>
    /// 通用相机切换方法（使用优先级而非SetActive）
    /// </summary>
    private void SwitchCamera(CinemachineVirtualCamera targetVC)
    {
        if (targetVC == null)
        {
            Debug.LogError("目标虚拟相机未赋值！");
            return;
        }

        SetAllCamerasInactive();
        targetVC.Priority = _activePriority;

        Debug.Log($"视角切换至：{targetVC.name}");
    }

    /// <summary>
    /// 【已更新】将所有虚拟相机优先级设为低（包含 GameVc）
    /// </summary>
    private void SetAllCamerasInactive()
    {
        if (MainCV != null) MainCV.Priority = _inactivePriority;
        if (OnlineCV != null) OnlineCV.Priority = _inactivePriority;
        if (StandaloneCV != null) StandaloneCV.Priority = _inactivePriority;
        if (EnterCv != null) EnterCv.Priority = _inactivePriority;
        if (GameVc != null) GameVc.Priority = _inactivePriority; // 新增这一行
    }

    /// <summary>
    /// 设置Cinemachine混合时间
    /// </summary>
    private void SetCinemachineBlendTime(float blendTime)
    {
        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.m_DefaultBlend.m_Time = blendTime;
        }
    }
    #endregion

    #region UI管理
    private void ShowOnlineUI()
    {
        ConfirmOnlineButton.gameObject.SetActive(true);
        ReturnButton_Online.gameObject.SetActive(true);
        ConfirmStandaloneButton.gameObject.SetActive(false);
        ReturnButton_Standalone.gameObject.SetActive(false);
    }

    private void ShowStandaloneUI()
    {
        ConfirmStandaloneButton.gameObject.SetActive(true);
        ReturnButton_Standalone.gameObject.SetActive(true);
        ConfirmOnlineButton.gameObject.SetActive(false);
        ReturnButton_Online.gameObject.SetActive(false);
    }

    private void HideAllUI()
    {
        ConfirmOnlineButton.gameObject.SetActive(false);
        ReturnButton_Online.gameObject.SetActive(false);
        ConfirmStandaloneButton.gameObject.SetActive(false);
        ReturnButton_Standalone.gameObject.SetActive(false);
    }
    #endregion

    #region 确认逻辑
    private void ConfirmOnline()
    {
        Debug.Log("确认进入多人模式");
        // 在这里添加进入多人模式的逻辑
        UImanager.Instance.ShowPanel<RoomPanel>();//打开联机房间
        ExitSystem();
    }

    private void ConfirmStandalone()
    {
        Debug.Log("确认进入单机模式");
        // 在这里添加进入单机模式的逻辑
        WarnTriggerManager.Instance.TriggerSingleInteractionWarn("敬请期待", "抱歉影响您的体验，作者正在赶工制作", () => { });
    }
    #endregion
}