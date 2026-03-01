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

    [Header("===== UI组件赋值 =====")]
    [Header("主视角相关UI")]
    public Image MainPromptImage;
    public TextMeshProUGUI MainViewReturnPromptText;
    public Button MainViewChooseButton;

    [Header("地图1相关UI")]
    public Image PromptImage_Map1;
    public CanvasGroup ChooseSign1;
    public Button ChooseMap1Button;
    public CanvasGroup ConfirmButton_Map1; // 确认选中按钮
    private Sequence Map1Anima;

    [Header("地图2相关UI")]
    public Image PromptImage_Map2;
    public CanvasGroup ChooseSign2;
    public Button ChooseMap2Button;
    public CanvasGroup ConfirmButton_Map2; // 确认选中按钮
    private Sequence Map2Anima;

    [Header("===== 动画配置 =====")]
    [Tooltip("透明度渐变的最小值")]
    public float minAlpha = 0f;
    [Tooltip("透明度渐变的最大值")]
    public float maxAlpha = 1f;
    [Tooltip("单次渐变的时长（秒）")]
    public float fadeDuration = 1f;

    [Header("===== Cinemachine切换速度配置 =====")]
    [Tooltip("默认切换速度（秒）：用于玩家<->地图选择")]
    public float defaultBlendTime = 1f;
    [Tooltip("快速切换速度（秒）：用于地图选择->地图1/2")]
    public float fastBlendTime = 0.6f;

    // 优先级配置
    private readonly int _activePriority = 10;
    private readonly int _inactivePriority = 0;

    // 状态变量
    private CameraView _lastView;
    private CameraView _currentView = CameraView.Player;
    private CameraView? _selectedMap;

    // 动画缓存
    private Tween _mainPromptFadeTween;
    private Tween _map1FadeTween;
    private Tween _map2FadeTween;

    // Cinemachine Brain缓存
    private CinemachineBrain _cinemachineBrain;

    [Header("选中人数提示文本")]
    public TextMeshProUGUI Map1ChoosePlayerCountText;
    public TextMeshProUGUI Map2ChoosePlayerCountText;

    [Header("倒计时文本")]
    public TextMeshProUGUI CountDownText;
    public CanvasGroup CountDownCanvasGroup;

    [Header("面板持续时间")]
    public float Duration = 20;

    public int CurrentChooseMap = 1;

    private Sequence _countdownColorSequence;

    public CanvasGroup MainCanvasGroup;


    private void Awake()
    {
        Instance = this;
        _cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (_cinemachineBrain == null)
        {
            Debug.LogError("场景中未找到Cinemachine Brain！请确保Main Camera上有Cinemachine Brain组件！");
        }
        ConfirmButton_Map1.gameObject.SetActive(false);
        ConfirmButton_Map2.gameObject.SetActive(false);

        InitPlayerCountText();
        CountDownCanvasGroup.alpha = 0;
    }

    public void TriggerMapAnima()
    {
        PlayerAndGameInfoManger.Instance.AllMapManagerList[CurrentChooseMap - 1].TriggerAnima();//触发动画（播放对应的动画）
    }

    void Start()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        BindAllButtons();
        SwitchToPlayerView();
        HideAllMapChooseUI();

        SetButtonInteractable(MainViewChooseButton, true);
        SetButtonInteractable(ChooseMap1Button, true);
        SetButtonInteractable(ChooseMap2Button, true);
    }

    // ===================== 从旧系统移植的逻辑 =====================
    #region 移植逻辑
    /// <summary>
    /// 初始化人数文本
    /// </summary>
    private void InitPlayerCountText()
    {
        if (Map1ChoosePlayerCountText != null)
            Map1ChoosePlayerCountText.text = "当前选择人数：0";
        if (Map2ChoosePlayerCountText != null)
            Map2ChoosePlayerCountText.text = "当前选择人数：0";
    }

    /// <summary>
    /// 更新两个地图的选择人数显示
    /// </summary>
    public void UpdatePlayerCount(int map1Count, int map2Count)
    {
        Debug.Log($"[MapChooseWall] 更新人数显示: 地图1={map1Count}, 地图2={map2Count}");
        if (Map1ChoosePlayerCountText != null)
            Map1ChoosePlayerCountText.text = "当前选择人数：" + map1Count.ToString();
        if (Map2ChoosePlayerCountText != null)
            Map2ChoosePlayerCountText.text = "当前选择人数：" + map2Count.ToString();
    }

    /// <summary>
    /// 启动倒计时颜色动画
    /// </summary>
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
    }

    /// <summary>
    /// 倒计时结束回调
    /// </summary>
    private void OnCountdownFinished()
    {
        Debug.Log("倒计时结束，进入直升机视角");
        EnterhelicopterVC();
    }
    #endregion

    // ===================== Cinemachine切换速度控制 =====================
    #region 切换速度控制
    private void SetCinemachineBlendTime(float blendTime)
    {
        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.m_DefaultBlend.m_Time = blendTime;
            Debug.Log($"Cinemachine混合时间已设置为：{blendTime}秒");
        }
    }
    #endregion

    // ===================== 按钮事件绑定（新增确认按钮绑定） =====================
    #region 按钮事件绑定
    private void BindAllButtons()
    {
        UnbindAllButtons();

        // 地图选择按钮
        if (ChooseMap1Button != null)
        {
            ChooseMap1Button.onClick.AddListener(SwitchToMap1View);
            Debug.Log("地图1选择按钮已绑定");
        }
        if (ChooseMap2Button != null)
        {
            ChooseMap2Button.onClick.AddListener(SwitchToMap2View);
            Debug.Log("地图2选择按钮已绑定");
        }

        if (ConfirmButton_Map1 != null)
        {
            ConfirmButton_Map1.gameObject.GetComponent<Button>().onClick.AddListener(OnConfirmMap1Clicked);
            Debug.Log("地图1确认按钮已绑定");
        }
        if (ConfirmButton_Map2 != null)
        {
            ConfirmButton_Map2.gameObject.GetComponent<Button>().onClick.AddListener(OnConfirmMap2Clicked);
            Debug.Log("地图2确认按钮已绑定");
        }

        // 主视角返回按钮
        if (MainViewChooseButton != null)
        {
            MainViewChooseButton.onClick.AddListener(ReturnToMapSelectFromMapView);
            Debug.Log("主视角返回按钮已绑定");
        }
    }

    private void UnbindAllButtons()
    {
        if (ChooseMap1Button != null)
            ChooseMap1Button.onClick.RemoveAllListeners();
        if (ChooseMap2Button != null)
            ChooseMap2Button.onClick.RemoveAllListeners();
        if (ConfirmButton_Map1 != null)
            ConfirmButton_Map1.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
        if (ConfirmButton_Map2 != null)
            ConfirmButton_Map2.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
        if (MainViewChooseButton != null)
            MainViewChooseButton.onClick.RemoveAllListeners();
    }
    #endregion

    // ===================== 确认按钮点击事件 =====================
    #region 确认按钮逻辑
    /// <summary>
    /// 点击地图1确认按钮：显示选中标识，记录选中状态，通知网络
    /// </summary>
    private void OnConfirmMap1Clicked()
    {
        Debug.Log("点击了地图1确认按钮");

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CmdPlayerChooseMap(1);
        }

        SetSelectedMap(CameraView.Map1);
    }

    /// <summary>
    /// 点击地图2确认按钮：显示选中标识，记录选中状态，通知网络
    /// </summary>
    private void OnConfirmMap2Clicked()
    {
        Debug.Log("点击了地图2确认按钮");

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CmdPlayerChooseMap(2);
        }

        SetSelectedMap(CameraView.Map2);
    }
    #endregion

    // ===================== 核心系统接口 =====================
    #region 系统入口/出口接口
    public void EnterMapChooseSystem()
    {
        if (_currentView == CameraView.MapSelect) return;

        CountDownCanvasGroup.alpha = 1;
        SetCinemachineBlendTime(defaultBlendTime);
        Debug.Log("进入地图选择系统");
        SwitchCamera(CameraView.MapSelect, mapSelectVC);
        SetUIState_MapSelect();
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleHidePanel();

        SimpleAnimatorTool.Instance.AddRollValueTask(Duration, 0, Duration, CountDownText, "F2", SimpleAnimatorTool.EaseType.Linear, () => { });
        StartCountdownColorAnimation();
        UImanager.Instance.HidePanel<PlayerPreparaPanel>();//关闭玩家准备面板
        if (PlayerRespawnManager.Instance != null)
        {
            UpdatePlayerCount(PlayerRespawnManager.Instance.Map1ChooseCount, PlayerRespawnManager.Instance.Map2ChooseCount);
            Debug.Log($"[MapChooseWall] 初始化UI数据: 地图1={PlayerRespawnManager.Instance.Map1ChooseCount}, 地图2={PlayerRespawnManager.Instance.Map2ChooseCount}");
        }
        MainCanvasGroup.blocksRaycasts = true;
    }

    public void ExitMapChooseSystem()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        Debug.Log("退出地图选择系统");
        SwitchToPlayerView();
        HideAllMapChooseUI();
        // 退出系统时才清空选中状态
        _selectedMap = null;
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();

        _countdownColorSequence?.Kill();
    }
    #endregion

    // ===================== 视角切换接口 =====================
    #region 视角切换接口
    private void SwitchToPlayerView()
    {
        SwitchCamera(CameraView.Player, playerVC);
    }

    public void SwitchToMap1View()
    {
        SetCinemachineBlendTime(fastBlendTime);
        Debug.Log("点击了地图1按钮");
        SwitchCamera(CameraView.Map1, map1VC);
        SetUIState_MapView(CameraView.Map1);
        //这里使用动画进行渐变
        ConfirmButton_Map1.gameObject.SetActive(true);
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ConfirmButton_Map1, ref Map1Anima, true, () => { });
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ConfirmButton_Map2, ref Map2Anima, false, () => { ConfirmButton_Map2.gameObject.SetActive(false); });
    }

    public void SwitchToMap2View()
    {
        SetCinemachineBlendTime(fastBlendTime);
        Debug.Log("点击了地图2按钮");
        SwitchCamera(CameraView.Map2, map2VC);
        SetUIState_MapView(CameraView.Map2);

        ConfirmButton_Map2.gameObject.SetActive(true);
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ConfirmButton_Map2, ref Map2Anima, true, () => { });
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ConfirmButton_Map1, ref Map1Anima, false, () => { ConfirmButton_Map1.gameObject.SetActive(false); });
    }

    public void ReturnToMapSelectFromMapView()
    {
        SetCinemachineBlendTime(defaultBlendTime);
        Debug.Log("点击了返回按钮");
        SwitchCamera(CameraView.MapSelect, mapSelectVC);
        SetUIState_MapSelect();
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ConfirmButton_Map1, ref Map1Anima, false, () => { ConfirmButton_Map1.gameObject.SetActive(false); });
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(ConfirmButton_Map2, ref Map2Anima, false, () => { ConfirmButton_Map2.gameObject.SetActive(false); });
    }

    public void EnterhelicopterVC()//进入直升机视角
    {
        PlayerRespawnManager.Instance.CmdRequestDecideFinalMap();//判断地图
        SetCinemachineBlendTime(defaultBlendTime + 3f); // 比默认慢1秒
        SwitchCamera(CameraView.Helicopter, helicopterVC);
        CountDownManager.Instance.CreateTimer(false,(int)((defaultBlendTime + 3.5f)*1000), () => {
            //播放动画
            TriggerMapAnima();
        });
    }

    public void TriggerPlayerTransmit()
    {
        // 客户端发请求给服务端
        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.TeleportAllPlayersToMap();
        }
        else
        {
            Debug.LogError("[客户端] 重生管理器单例为空，无法发送传送请求！");
        }
        UImanager.Instance.HidePanel<PlayerPreparaPanel>();//关闭准备面板
    }

    public void SwitchToLastView()
    {
        switch (_currentView)
        {
            case CameraView.Map1:
            case CameraView.Map2:
            case CameraView.Helicopter:
                ReturnToMapSelectFromMapView();
                break;
            case CameraView.MapSelect:
                ExitMapChooseSystem();
                break;
            case CameraView.Player:
                Debug.Log("当前已是玩家视角，无需返回");
                break;
        }
    }
    #endregion

    // ===================== UI状态管理（修改确认按钮逻辑） =====================
    #region UI状态管理
    private void HideAllMapChooseUI()
    {
        // 主视角UI
        StopMainPromptFadeAnimation();
        SetImageAlpha(MainPromptImage, 0f);
        SetUIVisible(MainPromptImage, false);
        SetTextMeshProVisible(MainViewReturnPromptText, false);

        // 地图1 UI
        StopMap1FadeAnimation();
        SetImageAlpha(PromptImage_Map1, 0f);
        SetUIVisible(PromptImage_Map1, false);
        SetCanvasGroupVisible(ChooseSign1, false);
        SetTextMeshProVisible(Map1ChoosePlayerCountText, false);

        // 地图2 UI
        StopMap2FadeAnimation();
        SetImageAlpha(PromptImage_Map2, 0f);
        SetUIVisible(PromptImage_Map2, false);
        SetCanvasGroupVisible(ChooseSign2, false);
        SetTextMeshProVisible(Map2ChoosePlayerCountText, false);
    }

    private void SetUIState_MapSelect()
    {
        StopMainPromptFadeAnimation();
        SetImageAlpha(MainPromptImage, 0f);
        SetUIVisible(MainPromptImage, false);
        SetTextMeshProVisible(MainViewReturnPromptText, false);

        SetUIVisible(PromptImage_Map1, true);
        SetUIVisible(PromptImage_Map2, true);

        // 【修复】确保人数文本可见
        SetTextMeshProVisible(Map1ChoosePlayerCountText, true);
        SetTextMeshProVisible(Map2ChoosePlayerCountText, true);

        StartMap1FadeAnimation();
        StartMap2FadeAnimation();

        RefreshChooseSigns();

        Debug.Log("地图选择UI已激活");
    }

    /// <summary>
    /// 设置地图视角UI，根据当前视角显示对应的确认按钮
    /// </summary>
    /// <param name="currentMapView">当前地图视角</param>
    private void SetUIState_MapView(CameraView currentMapView)
    {
        StopMap1FadeAnimation();
        StopMap2FadeAnimation();
        SetImageAlpha(PromptImage_Map1, 0f);
        SetImageAlpha(PromptImage_Map2, 0f);
        SetUIVisible(PromptImage_Map1, false);
        SetUIVisible(PromptImage_Map2, false);

        SetUIVisible(MainPromptImage, true);
        SetTextMeshProVisible(MainViewReturnPromptText, true);
        StartMainPromptFadeAnimation();

        SetButtonInteractable(ConfirmButton_Map1.gameObject.GetComponent<Button>(), currentMapView == CameraView.Map1);
        SetButtonInteractable(ConfirmButton_Map2.gameObject.GetComponent<Button>(), currentMapView == CameraView.Map2);

        RefreshChooseSigns();
    }

    private void SetSelectedMap(CameraView map)
    {
        _selectedMap = map;
        RefreshChooseSigns();
        Debug.Log($"已选中地图：{map}");
    }

    private void RefreshChooseSigns()
    {
        SetCanvasGroupVisible(ChooseSign1, _selectedMap == CameraView.Map1);
        SetCanvasGroupVisible(ChooseSign2, _selectedMap == CameraView.Map2);
    }
    #endregion

    // ===================== 动画控制 =====================
    #region 动画控制
    private void StartMainPromptFadeAnimation()
    {
        if (MainPromptImage == null) return;

        StopMainPromptFadeAnimation();
        SetImageAlpha(MainPromptImage, minAlpha);

        _mainPromptFadeTween = MainPromptImage.DOFade(maxAlpha, fadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(MainPromptImage.gameObject);

        Debug.Log("主视角提示图动画已启动");
    }

    private void StopMainPromptFadeAnimation()
    {
        if (_mainPromptFadeTween != null && _mainPromptFadeTween.IsActive())
        {
            _mainPromptFadeTween.Kill();
            _mainPromptFadeTween = null;
        }
    }

    private void StartMap1FadeAnimation()
    {
        if (PromptImage_Map1 == null) return;

        StopMap1FadeAnimation();
        SetImageAlpha(PromptImage_Map1, minAlpha);

        _map1FadeTween = PromptImage_Map1.DOFade(maxAlpha, fadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(PromptImage_Map1.gameObject);

        Debug.Log("地图1提示图动画已启动");
    }

    private void StopMap1FadeAnimation()
    {
        if (_map1FadeTween != null && _map1FadeTween.IsActive())
        {
            _map1FadeTween.Kill();
            _map1FadeTween = null;
        }
    }

    private void StartMap2FadeAnimation()
    {
        if (PromptImage_Map2 == null) return;

        StopMap2FadeAnimation();
        SetImageAlpha(PromptImage_Map2, minAlpha);

        _map2FadeTween = PromptImage_Map2.DOFade(maxAlpha, fadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(PromptImage_Map2.gameObject);

        Debug.Log("地图2提示图动画已启动");
    }

    private void StopMap2FadeAnimation()
    {
        if (_map2FadeTween != null && _map2FadeTween.IsActive())
        {
            _map2FadeTween.Kill();
            _map2FadeTween = null;
        }
    }
    #endregion

    // ===================== 底层工具方法 =====================
    #region 底层工具方法
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
    }

    // UI显隐/交互的简化方法
    private void SetUIVisible(Graphic ui, bool visible)
    {
        if (ui != null) ui.enabled = visible;
    }
    private void SetTextMeshProVisible(TextMeshProUGUI tmp, bool visible)
    {
        if (tmp != null) tmp.enabled = visible;
    }
    private void SetButtonInteractable(Button btn, bool interactable)
    {
        if (btn != null) btn.interactable = interactable;
    }
    private void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (cg != null)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }
    }
    // 设置Image透明度
    private void SetImageAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color color = img.color;
            color.a = alpha;
            img.color = color;
        }
    }

    // 物体销毁时清理所有事件和动画
    private void OnDestroy()
    {
        UnbindAllButtons();
        StopMainPromptFadeAnimation();
        StopMap1FadeAnimation();
        StopMap2FadeAnimation();
        _countdownColorSequence?.Kill();
    }
    #endregion
}

public enum CameraView
{
    Player,
    MapSelect,
    Map1,
    Map2,
    Helicopter
}