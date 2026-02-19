using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerPreparaPanel : BasePanel
{
    public string PreparaButtonFile = "PreparaButton";
    private RectTransform MyRct;

    [Header("UI引用")]
    public TextMeshProUGUI RoomPlayerCount;
    public TextMeshProUGUI PlayerPreparaCount;
    public TextMeshProUGUI teamCompareText;
    public bool IsPrepara = false;

    [Header("游戏开始按钮")]
    public CanvasGroup GameStartCanvas;
    private Sequence GameStartCanvasAnima;

    #region 生命周期
    public override void Awake()
    {
        MyRct = GetComponent<RectTransform>();
        base.Awake();
        ButtonGroupManager.Instance.AddToggleButtonToGroup(PreparaButtonFile, controlDic["PreparaButton"] as Button, "", playerPrepara, CancelPrePara);
        //隐藏当前的开始游戏按钮（人数和队伍达到条件才开始）
        GameStartCanvas.interactable = false;
        GameStartCanvas.alpha = 0;
    }

    public void IsActiveGameStartButton(bool IsActive)
    {
        if (!IsActive)
        {
            GameStartCanvas.interactable = false;//无法交互
            PlayerRespawnManager.Instance.SendGlobalMessage("对局开始条件不满足", 1);
        }

        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(GameStartCanvas,ref GameStartCanvasAnima, IsActive, () => {
            if(IsActive)
            {
                GameStartCanvas.interactable = true;//允许交互
                                                    //全局播报允许游戏开始
                CountDownManager.Instance.CreateTimer(false, 300, () => { PlayerRespawnManager.Instance.SendGlobalMessage("对局开始条件达成，等待房主开始游戏", 1); });
            }
            });
    }    

    public override void Start()
    {
        base.Start();
        // 启动时手动刷新一次，防止已有数据不显示
        ManualRefreshUI();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MyRct.DOKill();
    }
    #endregion

    #region 核心逻辑
    public void ManualRefreshUI()
    {
        if (PlayerRespawnManager.Instance == null) 
            return;

        UpdateRoomPlayerCount(PlayerRespawnManager.Instance.CurrentPlayerCount);
        UpdateRoomPlayerPreparaCount(PlayerRespawnManager.Instance.CurrentpreparaCount);
    }

    //更新当前队伍人数比的函数
    public void UpdateteamCompareText(int RedCount, int BlueCount)
    {
        // 空值保护
        if (PlayerPreparaCount == null)
            return;

        teamCompareText.text = $"<color=#FF0000>{RedCount}</color>:<color=#0000FF>{BlueCount}</color>";
    }



    public void playerPrepara(string text)
    {
        if (IsPrepara) return;
        IsPrepara = true;

        if (Player.LocalPlayer != null)
        {
            PlayerRespawnManager.Instance.SendGlobalMessage("玩家：" + Player.LocalPlayer.GetDisplayName() + "已准备!", 1);
            Player.LocalPlayer.ChangePreparaState(true);
        }
    }

    public void CancelPrePara(string text)
    {
        if (!IsPrepara)
            return;
        IsPrepara = false;

        if (Player.LocalPlayer != null)
        {
            PlayerRespawnManager.Instance.SendGlobalMessage("玩家：" + Player.LocalPlayer.GetDisplayName() + "取消准备!", 1);
            Player.LocalPlayer.ChangePreparaState(false);
        }
    }
    #endregion

    #region UI控件逻辑
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "Changeteambutton")
        {
            Player.LocalPlayer.ChangeTeam();
            if (Player.LocalPlayer.CurrentTeam == Team.Red)
                (controlDic["Changeteambutton"] as Button).image.DOColor(Color.blue, 0.5f);
            else
                (controlDic["Changeteambutton"] as Button).image.DOColor(Color.red, 0.5f);

            //触发更新
            Player.LocalPlayer.RequestRefreshTeamUI();//更新UI

        }
        else if(controlName == "GameStartButton")
        {
            //在这里触发游戏开始
            //给全局发消息
            Player.LocalPlayer.CmdRequestStartGame();
        }
    }


    #endregion

    #region 面板的显隐以及特殊动画编写
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
        MyRct.DOKill();
        MyRct.DOAnchorPosY(80, 0.5f);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        MyRct.DOKill();
        MyRct.anchoredPosition = new Vector2(0, 80);
        base.ShowMe(isNeedDefaultAnimator);
        MyRct.DOAnchorPosY(0, 0.5f);

        // 显示面板时立即刷新
        ManualRefreshUI();
    }

    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }
    #endregion

    public void UpdateRoomPlayerCount(int Count)
    {
        if (RoomPlayerCount != null)
            RoomPlayerCount.text = "当前局内人数：" + Count.ToString();
    }

    public void UpdateRoomPlayerPreparaCount(int Count)
    {
        if (PlayerPreparaCount != null)
            PlayerPreparaCount.text = "当前局内准备人数：" + Count.ToString();
    }
}