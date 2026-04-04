using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeChooseSystem : MonoBehaviour
{
    public static ModeChooseSystem instance;

    [Header("提示组件")]
    public TextMeshProUGUI LeftPromptText;
    public TextMeshProUGUI RightPromptText;
    public Image LeftPromptImage;
    public Image RightPromptImage;

    [Header("虚拟摄像机")]
    public CinemachineVirtualCamera MainCV;      // 主视角 
    public CinemachineVirtualCamera OnlineCV;    // 多人模式选择视角
    public CinemachineVirtualCamera StandaloneCV;// 单机模式选择视角
    public CinemachineVirtualCamera GameVc;      // 游戏视角
    public CinemachineVirtualCamera StartCG;     // 开始的视角

    [Header("Cinemachine切换配置")]
    [Tooltip("视角切换的混合时间（秒）")]
    public float defaultBlendTime = 1f;

    // 相机优先级配置
    private readonly int _activePriority = 20;
    private readonly int _inactivePriority = -10;

    // Cinemachine Brain缓存
    private CinemachineBrain _cinemachineBrain;
    private FadeLoopTask task1;
    private FadeLoopTask task2;
    private FadeLoopTask task3;
    private FadeLoopTask task4;

    private void Awake()
    {
        instance = this;

        // 获取Cinemachine Brain
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (_cinemachineBrain == null)
        {
            Debug.LogError("场景中未找到Cinemachine Brain！");
        }

        InitStartView();
    }

    private void InitStartView()
    {
        SetAllCamerasInactive();
        IsTriggerPromptAnima(false);

        if (StartCG != null)
        {
            StartCG.Priority = _activePriority + 200;
        }
    }

    private void OnDestroy()
    {
        IsTriggerPromptAnima(false);
    }

    public void EnterSystem()
    {
        GlobalPictureFlipManager.Instance.TriggerGlobalFlip(false);
        GameStartCG.Instance.StopTimeLine();

        SetCinemachineBlendTime(defaultBlendTime);
        SwitchToMainCamera();
    }

    public void EnterSystem_Quick()
    {
        GameStartCG.Instance.StopTimeLine();
        SetCinemachineBlendTime(0f);

        SetAllCamerasInactive();

        if (MainCV != null)
        {
            MainCV.Priority = _activePriority;
            MainCV.ForceCameraPosition(MainCV.transform.position, MainCV.transform.rotation);
        }

        IsTriggerPromptAnima(true);
    }

    // 系统出口
    public void ExitSystem()
    {
        SetCinemachineBlendTime(0f);

        IsTriggerPromptAnima(false);

        if (GameVc != null)
        {
            SetAllCamerasInactive();
            GameVc.Priority = _activePriority + 100;
            GameVc.ForceCameraPosition(GameVc.transform.position, GameVc.transform.rotation);
        }
        Debug.Log("[ModeChooseSystem] 已瞬间退出并切换到游戏视角");
    }

    public void IsTriggerPromptAnima(bool IsActive)
    {
        if (IsActive)
        {
            task1 = SimpleAnimatorTool.Instance.AddFadeLoopTask(LeftPromptText);
            task2 = SimpleAnimatorTool.Instance.AddFadeLoopTask(RightPromptText);
            task3 = SimpleAnimatorTool.Instance.AddFadeLoopTask(LeftPromptImage, waitTime: 0);
            task4 = SimpleAnimatorTool.Instance.AddFadeLoopTask(RightPromptImage, waitTime: 0);
        }
        else
        {
            SimpleAnimatorTool.Instance.StopFadeLoopTask(task1);
            SimpleAnimatorTool.Instance.StopFadeLoopTask(task2);
            SimpleAnimatorTool.Instance.StopFadeLoopTask(task3);
            SimpleAnimatorTool.Instance.StopFadeLoopTask(task4);
        }
    }

    #region 简化后的核心逻辑
    // 点击多人模式：直接切换视角 → 延迟后自动确认
    public void OnClickOnline()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(OnlineCV);

        // 等待视角切换完成后，直接触发确认逻辑
        StartCoroutine(DelayExecute(defaultBlendTime + 0.1f, ConfirmOnline));
    }

    // 点击单人模式：直接切换视角 → 延迟后自动确认
    public void OnClickStandalone()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(StandaloneCV);

        // 等待视角切换完成后，直接触发确认逻辑
        StartCoroutine(DelayExecute(defaultBlendTime + 0.1f, ConfirmStandalone));
    }

    // 简单的延迟执行协程
    private IEnumerator DelayExecute(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
    #endregion

    #region 视角切换
    public void SwitchToMainCamera()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(MainCV);

        IsTriggerPromptAnima(true);

        Debug.Log("[ModeChooseSystem] 回到主视角，按钮已激活");
    }

    private void SwitchCamera(CinemachineVirtualCamera targetVC)
    {
        if (targetVC == null)
        {
            Debug.LogError("目标虚拟相机未赋值！");
            return;
        }

        SetAllCamerasInactive();
        targetVC.Priority = _activePriority;

        if (targetVC != MainCV)
        {
            IsTriggerPromptAnima(false);
        }

        Debug.Log($"视角切换至：{targetVC.name}");
    }

    private void SetAllCamerasInactive()
    {
        if (MainCV != null) MainCV.Priority = _inactivePriority;
        if (OnlineCV != null) OnlineCV.Priority = _inactivePriority;
        if (StandaloneCV != null) StandaloneCV.Priority = _inactivePriority;
        if (GameVc != null) GameVc.Priority = _inactivePriority;
        if (StartCG != null) StartCG.Priority = _inactivePriority;
    }

    private void SetCinemachineBlendTime(float blendTime)
    {
        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.m_DefaultBlend.m_Time = blendTime;
        }
    }
    #endregion

    #region 确认逻辑
    private void ConfirmOnline()
    {
        Debug.Log("确认进入多人模式");
        UImanager.Instance.ShowPanel<RoomPanel>();
    }

    private void ConfirmStandalone()
    {
        //打开面板单人选择面板
        UImanager.Instance.ShowPanel<SinglePlayerPanel>();
    }
    #endregion
}