using DG.Tweening;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

[System.Serializable]
public class MapButton
{
    public Image Image_UI;
    public TextMeshProUGUI MapNameText;
    public TextMeshProUGUI PlayerChooseCountText;
    public MapInfo CurrentMapInfo;

    public int CurrentIndex = 0;

    public bool IsSelected = false;
    public void Init()
    {
        CurrentIndex = 0;
        if (CurrentMapInfo == null) return;
        if (CurrentMapInfo.mapSprite_UI != null)
            Image_UI.sprite = CurrentMapInfo.mapSprite_UI;
        MapNameText.text = CurrentMapInfo.Name;
        PlayerChooseCountText.text = "当前选择人数：0";
    }

    public void UpdatePlayerCount(int PlayerCount)
    {
        PlayerChooseCountText.text = "当前选择人数：" + PlayerCount.ToString();
    }

    public void NextMapDetail()
    {
        if (CurrentMapInfo == null || CurrentMapInfo.MapDetailPackList.Count == 0)
            return;
        CurrentIndex = (CurrentIndex + 1) % CurrentMapInfo.MapDetailPackList.Count;
    }

    public MapDetailPack GetCurrentMapDetailPack()
    {
        if (CurrentMapInfo == null || CurrentMapInfo.MapDetailPackList.Count == 0)
            return null;
        CurrentIndex = Mathf.Clamp(CurrentIndex, 0, CurrentMapInfo.MapDetailPackList.Count - 1);
        NextMapDetail();
        return CurrentMapInfo.MapDetailPackList[CurrentIndex];
    }
}
public class MapChoosePanel : BasePanel
{
    public CanvasGroup IntroducePanel;
    private string MapButtonFileName = "MapButtonGroup";
    public MapButton MapButton_1;
    public MapButton MapButton_2;
    public int CurrentChooseMap = 1;

    [Header("地图面板")]
    public TextMeshProUGUI DescribeText;
    public Image IntroduceMapImage;
    public TextMeshProUGUI PanelPromptText;

    [Header("面板持续时间")]
    public TextMeshProUGUI CountDownText;
    public float Duration = 20;

    [Header("TimeLine")]
    public PlayableDirector TimeLine;

    private Sequence _countdownColorSequence;

    private Button _chooseMapButton;
    private Color _originalButtonColor=Color.white;
    private Vector3 _originalButtonScale=new Vector3(1,1,1);

    [Header("总结信息")]
    public TextMeshProUGUI MapName;//地图名字
    public TextMeshProUGUI gameMode;//游戏模式
    public TextMeshProUGUI WinCondition;//胜利条件介绍文本

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        GetMapInfo();
        InitMapButton();

        _chooseMapButton = controlDic["ChooseMapButton"] as Button;
    }

    public void GetMapInfo()
    {
        MapButton_1.CurrentMapInfo = PlayerAndGameInfoManger.Instance.AllMapInfoList[0];
        MapButton_2.CurrentMapInfo = PlayerAndGameInfoManger.Instance.AllMapInfoList[1];
    }

    public MapButton getCurrentChooseMap()
    {
        if (CurrentChooseMap == 1) return MapButton_1;
        else if (CurrentChooseMap == 2) return MapButton_2;
        Debug.Log("未找到对应的地图按钮信息");
        return null;
    }

    public void TriggerMapAnima()
    {
        PlayerAndGameInfoManger.Instance.AllMapManagerList[CurrentChooseMap-1].TriggerAnima();//触发动画
    }

    public void InitMapButton()
    {
        MapButton_1.Init();
        MapButton_2.Init();
    }

    public void RegisterAMapButton()
    {
      
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(MapButtonFileName, controlDic["MapButton_1"] as Button, TriggerMapButton, null, 1.05f, 0.4f);
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(MapButtonFileName, controlDic["MapButton_2"] as Button, TriggerMapButton, null, 1.05f, 0.4f);
    }

    public void TriggerFirstButton()
    {
        RegisterAMapButton();
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(MapButtonFileName);
        UpdateIntroducePanel(MapButton_1.CurrentMapInfo, false);

        // 面板打开时强制同步当前投票数据
        if (PlayerRespawnManager.Instance != null)
        {
            MapButton_1.UpdatePlayerCount(PlayerRespawnManager.Instance.Map1ChooseCount);
            MapButton_2.UpdatePlayerCount(PlayerRespawnManager.Instance.Map2ChooseCount);
            Debug.Log($"[MapChoosePanel] 初始化UI数据: 地图1={PlayerRespawnManager.Instance.Map1ChooseCount}, 地图2={PlayerRespawnManager.Instance.Map2ChooseCount}");
        }

        ResetChooseButtonVisual();
        MapButton_1.IsSelected = false;
        MapButton_2.IsSelected = false;

        SimpleAnimatorTool.Instance.AddRollValueTask(Duration, 0, Duration, CountDownText, "F2", SimpleAnimatorTool.EaseType.Linear, () => { });
        StartCountdownColorAnimation();
        TimeLine.Pause();
    }

    private void StartCountdownColorAnimation()
    {
        _countdownColorSequence?.Kill();
        CountDownText.color = Color.black;
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

    private void OnCountdownFinished()
    {
        TimeLine.Play();
        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.CmdRequestDecideFinalMap();
        }
        CountDownManager.Instance.CreateTimer(false, 1000, () => {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IntroducePanel, ref IntroducePanelAnima, false, () => { });
            //在这里更新文本
            if(PlayerRespawnManager.Instance.CurrentMapIndex == 1)
                MapName.text=MapButton_1.CurrentMapInfo.Name;//赋值当前的名字
            else
                MapName.text = MapButton_2.CurrentMapInfo.Name;//赋值当前的名字


        });
    }


    //触发玩家传送
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

    public void TimeLineEnd()
    {
        UImanager.Instance.HidePanel<MapChoosePanel>();
    }

    public void TriggerMapButton(string ButtonName)
    {
        string numStr = Regex.Replace(ButtonName, @"[^\d]", "");
        if (int.TryParse(numStr, out int index))
        {
            CurrentChooseMap = index;
            UpdateIntroducePanel(GetMapInfo(index), true);

            UpdateChooseButtonVisual();
        }
    }
    public MapInfo GetMapInfo(int index)
    {
        if (index == 1) return MapButton_1.CurrentMapInfo;
        else if (index == 2) return MapButton_2.CurrentMapInfo;
        Debug.LogError("未找到当前地图");
        return null;
    }

    private Sequence IntroducePanelAnima;
    private FadeLoopTask promptTextFadeLoopTask;

    public void UpdateIntroducePanel(MapInfo mapInfo, bool needFade = true)
    {
        if (needFade)
        {
            IntroducePanel.DOKill();
            IntroducePanel.DOFade(0, 0.2f).OnComplete(() =>
            {
                RefreshPanelData(mapInfo);
                IntroducePanel.DOFade(1, 0.2f);
            });
        }
        else
        {
            RefreshPanelData(mapInfo);
            if (IntroducePanel.alpha != 1)
            {
                SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IntroducePanel, ref IntroducePanelAnima, true, () => { });
                promptTextFadeLoopTask = SimpleAnimatorTool.Instance.AddFadeLoopTask(PanelPromptText);
            }
        }
    }

    private void RefreshPanelData(MapInfo mapInfo)
    {
        MapButton_1.CurrentIndex = 0;
        MapButton_2.CurrentIndex = 0;
        UpdatePanel(getCurrentChooseMap().GetCurrentMapDetailPack());
    }

    public TypingWritingTask typingWritingTask;
    public void UpdatePanel(MapDetailPack mapPack)
    {
        if (typingWritingTask != null)
            typingWritingTask.StopTyping();
        typingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask(mapPack.mapDetailDescription, DescribeText);
        IntroduceMapImage.sprite = mapPack.mapSprite;
    }

    public override void Start() { base.Start(); }
    protected override void Update() { base.Update(); }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _countdownColorSequence?.Kill();
        promptTextFadeLoopTask.StopAnimatorLoop();
    }
    #endregion

    #region UI控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "ChangeSpriteButton")
        {
            UpdatePanel(getCurrentChooseMap().GetCurrentMapDetailPack());
        }
        else if (controlName == "ChooseMapButton")
        {
            if (PlayerRespawnManager.Instance != null)
            {
                PlayerRespawnManager.Instance.CmdPlayerChooseMap(CurrentChooseMap);
                //设置bool值
                if (CurrentChooseMap == 1)
                {
                    MapButton_1.IsSelected = true;
                    MapButton_2.IsSelected = false;
                }
                else
                {
                    MapButton_1.IsSelected = false;
                    MapButton_2.IsSelected = true;
                }

                UpdateChooseButtonVisual();
            }
        }
    }
    #endregion

    #region 按钮动画处理
    /// <summary>
    /// 根据当前查看的地图和 IsSelected 状态更新按钮外观
    /// </summary>
    private void UpdateChooseButtonVisual()
    {
        if (_chooseMapButton == null) 
            return;

        bool shouldBeSelected = false;
        if (CurrentChooseMap == 1 && MapButton_1.IsSelected)
        {
            shouldBeSelected = true;
        }
        else if (CurrentChooseMap == 2 && MapButton_2.IsSelected)
        {
            shouldBeSelected = true;
        }

        if (shouldBeSelected)
        {
            SetChooseButtonSelected();
        }
        else
        {
            ResetChooseButtonVisual();
        }
    }

    /// <summary>
    /// 按钮变绿，放大到1.05倍
    /// </summary>
    private void SetChooseButtonSelected()
    {
        if (_chooseMapButton == null)
            return;

        _chooseMapButton.image.DOKill();
        _chooseMapButton.transform.DOKill();

        _chooseMapButton.image.DOColor(Color.green, 0.2f);
        _chooseMapButton.transform.DOScale(_originalButtonScale * 1.05f, 0.2f);
    }

    /// <summary>
    /// 按钮还原
    /// </summary>
    private void ResetChooseButtonVisual()
    {
        if (_chooseMapButton == null)
            return;

        _chooseMapButton.image.DOKill();
        _chooseMapButton.transform.DOKill();

        _chooseMapButton.image.DOColor(_originalButtonColor, 0.2f);
        _chooseMapButton.transform.DOScale(_originalButtonScale, 0.2f);
    }
    #endregion

    #region 面板显隐以及特殊动画
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true) { base.HideMe(callback, isNeedDefaultAnimator); }
    public override void ShowMe(bool isNeedDefaultAnimator = true) { base.ShowMe(isNeedDefaultAnimator); }
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion
}