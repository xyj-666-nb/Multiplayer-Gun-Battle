using DG.Tweening;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class MapButton
{
    public Image Image_UI;
    public TextMeshProUGUI MapNameText;
    public TextMeshProUGUI PlayerChooseCountText;
    public MapInfo CurrentMapInfo;

    private int CurrentIndex = 0;
  

    public void Init()
    {
        // 初始化时重置索引，确保从第一个详情开始
        CurrentIndex = 0;

        if (CurrentMapInfo == null) return;

        if (CurrentMapInfo.mapSprite_UI != null)
            Image_UI.sprite = CurrentMapInfo.mapSprite_UI;

        MapNameText.text = CurrentMapInfo.Name;
        PlayerChooseCountText.text = "当前选择人数：0";
    }

    public void NextMapDetail()
    {
        // 安全检查
        if (CurrentMapInfo == null || CurrentMapInfo.MapDetailPackList.Count == 0)
            return;

        CurrentIndex = (CurrentIndex + 1) % CurrentMapInfo.MapDetailPackList.Count;
    }

    public MapDetailPack GetCurrentMapDetailPack()
    {
        // 安全检查
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

    private string MapButtonFileName= "MapButtonGroup";
    public MapButton MapButton_1;
    public MapButton MapButton_2;

    public int CurrentChooseMap=1;

    [Header("地图面板")]
    public TextMeshProUGUI DescribeText;//描述文本
    public Image IntroduceMapImage;
    public TextMeshProUGUI PanelPromptText;//面板提示文本

    [Header("面板持续时间")]
    public TextMeshProUGUI CountDownText;
    public float Duration=30;

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        GetMapInfo();
        InitMapButton();//初始化信息
    }

    public void GetMapInfo()
    {
        //获取地图信息(先简单获取)
        MapButton_1.CurrentMapInfo = PlayerAndGameInfoManger.Instance.AllMapInfoList[0];
        MapButton_2.CurrentMapInfo = PlayerAndGameInfoManger.Instance.AllMapInfoList[1];
    }

    public MapButton getCurrentChooseMap()
    {
        if (CurrentChooseMap == 1)
            return MapButton_1;
        else if (CurrentChooseMap == 2)
            return MapButton_2;

        Debug.Log("未找到对应的地图按钮信息");
        return null;
    }

    //初始化当前的地图按钮
    public void InitMapButton()
    {
        MapButton_1.Init();
        MapButton_2.Init();
    }

    public void RegisterAMapButton()//注册地图按钮
    {
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(MapButtonFileName, controlDic["MapButton_1"] as Button, TriggerMapButton,null,1.05f,0.4f);
        ButtonGroupManager.Instance.AddRadioButtonToGroup_Str(MapButtonFileName, controlDic["MapButton_2"] as Button, TriggerMapButton, null, 1.05f, 0.4f);
    }

    //触发第一个按钮(外部TimeLine调用)
    public void TriggerFirstButton()
    {
        RegisterAMapButton();
        ButtonGroupManager.Instance.SelectFirstRadioButtonInGroup(MapButtonFileName);
        //更新一下面板
        UpdateIntroducePanel(MapButton_1.CurrentMapInfo);//默认显示第一个地图按钮的信息

        //开始进行倒计时
        SimpleAnimatorTool.Instance.AddRollValueTask(Duration,0, Duration, CountDownText, "F2", SimpleAnimatorTool.EaseType.Linear, () => { });//写入确定地图的逻辑
    }

    public void TriggerMapButton(string ButtonName)
    {
        string numStr = Regex.Replace(ButtonName, @"[^\d]", "");//提取按钮的数字

        if (int.TryParse(numStr, out int index))//转化为int
        {
            //判断索引更新面板
            UpdateIntroducePanel(GetMapInfo(index));
            CurrentChooseMap = index;
        }

    }
    public MapInfo GetMapInfo(int index)
    {
        if(index==1)
            return MapButton_1.CurrentMapInfo;
        else if(index == 2)
            return MapButton_2.CurrentMapInfo;

        Debug.LogError("未找到当前地图");
        return null;
    }

    private Sequence IntroducePanelAnima;
    private FadeLoopTask promptTextFadeLoopTask;
    public void UpdateIntroducePanel(MapInfo mapInfo)//更新介绍面板
    {
        //最开始可能面板是透明的这里进行显示
        if(IntroducePanel.alpha!=1)
        {
            SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(IntroducePanel, ref IntroducePanelAnima, true, () => { });
            //给与提示文本动画
            promptTextFadeLoopTask=SimpleAnimatorTool.Instance.AddFadeLoopTask(PanelPromptText);
        }
        //更新对应的信息

    }


    public TypingWritingTask typingWritingTask;
    public void UpdatePanel(MapDetailPack mapPack)
    {
        if(typingWritingTask!=null)
            typingWritingTask.StopTyping();

        typingWritingTask = SimpleAnimatorTool.Instance.AddTypingTask(mapPack.mapDetailDescription, DescribeText);//进行打字机播放文本

        //更新sprite
        IntroduceMapImage.sprite = mapPack.mapSprite;//进行更新
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        promptTextFadeLoopTask.StopAnimatorLoop();//暂停任务
    }
    #endregion

    #region UI控件逻辑

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if(controlName== "ChangeSpriteButton")
        {
            //点击读取sprite和2text去更新
            //UpdatePanel(getCurrentChooseMap().GetCurrentMapDetailPack());//赋值信息后进行更新
        }
    }
    #endregion

    #region 面板显隐以及特殊动画

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    protected override void SpecialAnimator_Hide()
    {

    }

    protected override void SpecialAnimator_Show()
    {

    }
    #endregion

}
