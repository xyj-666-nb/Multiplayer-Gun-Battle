using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MapChooseWall : MonoBehaviour
{
    public static MapChooseWall Instance;

    [Header("===== 虚拟相机赋值 =====")]
    public CinemachineVirtualCamera playerVC;
    public CinemachineVirtualCamera mapSelectVC;
    public CinemachineVirtualCamera map1VC;
    public CinemachineVirtualCamera map2VC;
    public CinemachineVirtualCamera helicopterVC;
    public CinemachineVirtualCamera Map2SceneVC;
    public CinemachineVirtualCamera Scene2RealVac;

    [Header("===== UI组件赋值 (保留用于人数/倒计时显示) =====")]
    [Header("选中人数提示文本")]
    public TextMeshProUGUI Map1ChoosePlayerCountText;
    public TextMeshProUGUI Map2ChoosePlayerCountText;

    [Header("倒计时文本")]
    public TextMeshProUGUI CountDownText;
    public CanvasGroup CountDownCanvasGroup;

    [Header("面板持续时间")]
    public float Duration = 20;

    private Sequence _countdownColorSequence;
    public CanvasGroup MainCanvasGroup;

    [Header("===== 动画配置 =====")]
    [Tooltip("透明度渐变的最小值")]
    public float minAlpha = 0f;
    [Tooltip("透明度渐变的最大值")]
    public float maxAlpha = 1f;
    [Tooltip("单次渐变的时长（秒）")]
    public float fadeDuration = 1f;

    [Header("===== Cinemachine切换速度配置 =====")]
    [Tooltip("默认切换速度（秒）")]
    public float defaultBlendTime = 1f;
    [Tooltip("快速切换速度（秒）")]
    public float fastBlendTime = 0.6f;

    // 优先级配置
    private readonly int _activePriority = 20;
    private readonly int _inactivePriority = -10;

    // 状态变量
    private CameraView _lastView;
    private CameraView _currentView = CameraView.Player;

    // Cinemachine Brain缓存
    private CinemachineBrain _cinemachineBrain;

    private void Awake()
    {
        Instance = this;
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (_cinemachineBrain == null)
        {
            Debug.LogError("场景中未找到Cinemachine Brain！");
        }

        InitPlayerCountText();
        CountDownCanvasGroup.alpha = 0;
    }

    void Start()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchToPlayerView();
    }

    // ===================== 公开API供面板调用 =====================
    #region 公开API
    /// <summary>
    /// 外部调用：进入地图选择系统
    /// </summary>
    public void Public_EnterSystem()
    {
        EnterMapChooseSystem();
    }

    /// <summary>
    /// 外部调用：确认选择地图1
    /// </summary>
    public void Public_ConfirmMap1()
    {
        OnConfirmMap1Clicked();
    }

    /// <summary>
    /// 外部调用：确认选择地图2
    /// </summary>
    public void Public_ConfirmMap2()
    {
        OnConfirmMap2Clicked();
    }

    /// <summary>
    /// 外部调用：仅切换相机预览地图
    /// </summary>
    public void Public_PreviewMap(int mapIndex)
    {
        SetCinemachineBlendTime(fastBlendTime);
        if (mapIndex == 1)
        {
            SwitchCamera(CameraView.Map1, map1VC);
        }
        else if (mapIndex == 2)
        {
            SwitchCamera(CameraView.Map2, map2VC);
        }
    }

    /// <summary>
    /// 外部调用：返回地图选择总览相机
    /// </summary>
    public void Public_ReturnToOverview()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchCamera(CameraView.MapSelect, mapSelectVC);
    }
    #endregion

    // ===================== 核心业务逻辑 =====================
    #region 原有核心逻辑
    public void TriggerMapAnima()
    {
        if (PlayerRespawnManager.Instance == null) return;
        if (PlayerAndGameInfoManger.Instance == null) return;

        int mapIndex = PlayerRespawnManager.Instance.CurrentMapIndex;
        var mapList = PlayerAndGameInfoManger.Instance.AllMapManagerList;

        if (mapList == null || mapList.Count == 0) return;
        if (mapIndex < 0 || mapIndex >= mapList.Count) return;

        if (mapIndex == 1)
        {
            SetCinemachineBlendTime(0f);
            SwitchCamera(CameraView.MapScene2Real, Scene2RealVac);
        }

        mapList[mapIndex].TriggerAnima();

        Player.LocalPlayer.MyHandControl.SetAimPointActive(false);
        PlayerTacticControl.Instance.ResetTacticUI();
        Player.LocalPlayer.MyHandControl.SetHolsterState(false);
    }

    private void InitPlayerCountText()
    {
        if (Map1ChoosePlayerCountText != null)
            Map1ChoosePlayerCountText.text = "当前选择人数：0";
        if (Map2ChoosePlayerCountText != null)
            Map2ChoosePlayerCountText.text = "当前选择人数：0";
    }

    public void UpdatePlayerCount(int map1Count, int map2Count)
    {
        Debug.Log($"[MapChooseWall] 更新人数显示: 地图1={map1Count}, 地图2={map2Count}");
        if (Map1ChoosePlayerCountText != null)
            Map1ChoosePlayerCountText.text = "当前选择人数：" + map1Count.ToString();
        if (Map2ChoosePlayerCountText != null)
            Map2ChoosePlayerCountText.text = "当前选择人数：" + map2Count.ToString();
    }

    private void StartCountdownColorAnimation()
    {
        _countdownColorSequence?.Kill();
        CountDownText.color = Color.white;
        _countdownColorSequence = DOTween.Sequence();

        float timeStartYellow = Duration * 0.5f;
        float timeStartRed = Duration * 0.8f;
        float yellowFadeDuration = Duration * 0.1f;
        float redFadeDuration = Duration - timeStartRed;

        _countdownColorSequence.Insert(timeStartYellow, CountDownText.DOColor(Color.yellow, yellowFadeDuration));
        _countdownColorSequence.Insert(timeStartRed, CountDownText.DOColor(Color.red, redFadeDuration));
        _countdownColorSequence.AppendCallback(() =>
        {
            CountDownText.color = Color.red;
            OnCountdownFinished();
        });
        UImanager.Instance.HidePanel<AssistantMapChoosePanel>();//显示地图选择辅助面板
    }

    private void OnCountdownFinished()
    {
        EnterVC();
    }

    private void SetCinemachineBlendTime(float blendTime)
    {
        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.m_DefaultBlend.m_Time = blendTime;
        }
    }

    private void OnConfirmMap1Clicked()
    {
        Debug.Log("确认选择地图1");
        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CmdPlayerChooseMap(1);
        }
    }

    private void OnConfirmMap2Clicked()
    {
        Debug.Log("确认选择地图2");
        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CmdPlayerChooseMap(2);
        }
    }

    public void EnterMapChooseSystem()
    {
        if (_currentView == CameraView.MapSelect) return;

        UImanager.Instance.HidePanel<ArmamentPanel>();
        UImanager.Instance.HidePanel<EquipmentConfigurationPanel>();

        CountDownCanvasGroup.alpha = 1;
        SetCinemachineBlendTime(defaultBlendTime);
        Debug.Log("进入地图选择系统");
        SwitchCamera(CameraView.MapSelect, mapSelectVC);

        UImanager.Instance.GetPanel<PlayerPanel>().SimpleHidePanel();

        SimpleAnimatorTool.Instance.AddRollValueTask(Duration, 0, Duration, CountDownText, "F2", SimpleAnimatorTool.EaseType.Linear, () => { });
        StartCountdownColorAnimation();
        UImanager.Instance.HidePanel<PlayerPreparaPanel>();

        if (PlayerRespawnManager.Instance != null)
        {
            UpdatePlayerCount(PlayerRespawnManager.Instance.Map1ChooseCount, PlayerRespawnManager.Instance.Map2ChooseCount);
        }
        MainCanvasGroup.blocksRaycasts = true;

        GlobalPictureFlipManager.Instance.TriggerGlobalFlip(false);

        UImanager.Instance.ShowPanel<AssistantMapChoosePanel>();//显示地图选择辅助面板
    }

    public void ExitMapChooseSystem()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        SwitchToPlayerView();
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
        _countdownColorSequence?.Kill();
    }

    private void SwitchToPlayerView()
    {
        SwitchCamera(CameraView.Player, playerVC);
    }

    public void EnterVC()
    {
        PlayerRespawnManager.Instance.CmdRequestDecideFinalMap();

        CountDownManager.Instance.CreateTimer(false, 300, () => {
            SetCinemachineBlendTime(defaultBlendTime + 3f);
            if (PlayerRespawnManager.Instance.CurrentMapIndex == 0)
            {
                Debug.Log("切换到地图1");
                AllMapManager.Instance.TriggerMap(MapType.map1, true);
                SwitchCamera(CameraView.Helicopter, helicopterVC);
            }
            else
            {
                Debug.Log("切换到地图2");
                AllMapManager.Instance.TriggerMap(MapType.map2, true);
                SwitchCamera(CameraView.MapScene2, Map2SceneVC);
            }
            CountDownManager.Instance.CreateTimer(false, (int)((defaultBlendTime + 3.5f) * 1000), () => {
                TriggerMapAnima();
            });
        });
    }

    private void SwitchCamera(CameraView targetView, CinemachineVirtualCamera targetVC)
    {
        if (targetVC == null)
        {
            Debug.LogError($"[{targetView}] 对应的虚拟相机未赋值！");
            return;
        }

        _lastView = _currentView;
        _currentView = targetView;

        SetAllCameraInactive();
        targetVC.Priority = _activePriority;

        Debug.Log($"视角切换：{_lastView} → {_currentView}");
    }

    private void SetAllCameraInactive()
    {
        if (playerVC != null) playerVC.Priority = _inactivePriority;
        if (mapSelectVC != null) mapSelectVC.Priority = _inactivePriority;
        if (map1VC != null) map1VC.Priority = _inactivePriority;
        if (map2VC != null) map2VC.Priority = _inactivePriority;
        if (helicopterVC != null) helicopterVC.Priority = _inactivePriority;
        if (Map2SceneVC != null) Map2SceneVC.Priority = _inactivePriority;
        if (Scene2RealVac != null) Scene2RealVac.Priority = _inactivePriority;
    }
    #endregion
}

public enum CameraView
{
    Player,
    MapSelect,
    Map1,
    Map2,
    Helicopter,
    MapScene2,
    MapScene2Real
}