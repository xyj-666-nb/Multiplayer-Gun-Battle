using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class DeathPanel : BasePanel
{
    public float ChangeColorPercent = 0.4f;//到达这个百分比之后颜色开始变化
    private bool IsStartCountDown = false;
    private float CountDownTime;
    private float CurrentTime;//当前的时间
    private bool IsColorChanged = false;//颜色是否已经改变过了

    [Header("文本信息")]
    public TextMeshProUGUI DeathTime;//死亡时间文本
    public TextMeshProUGUI Killer;//击杀者文本
    public TextMeshProUGUI KillersGun;//击杀者武器文本

    public PlayableDirector TimeLine;

    #region 生命周期
    protected override void Update()
    {
        base.Update();
        if (IsStartCountDown && CountDownTime != 0)
        {
            CurrentTime -= Time.deltaTime;
            DeathTime.text = CurrentTime.ToString("F2"); // 保留两位小数

            // 倒计时颜色警告
            if (!IsColorChanged && CurrentTime / CountDownTime <= ChangeColorPercent)
            {
                IsColorChanged = true;
                DeathTime.DOColor(Color.red, 0.3f);
            }

            if (CurrentTime <= 0)
            {
                CurrentTime = 0;
                IsStartCountDown = false;
                UImanager.Instance.HidePanel<DeathPanel>();
            }
        }

        if (PlayerRespawnManager.Instance._isGameEnded)
        {
            UImanager.Instance.HidePanel<DeathPanel>();
            TimeLine.Stop();//停止
           
        }
    }
    #endregion

    #region UI逻辑处理
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        if (controlName == "StartPanelButton")
        {
            IsStartCountDown = false;
        }
    }
    #endregion

    #region 特殊动画以及显示隐藏逻辑
    protected override void SpecialAnimator_Hide() { }
    protected override void SpecialAnimator_Show() { }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        InitPanel();
        base.ShowMe(isNeedDefaultAnimator);
    }
    #endregion

    public void InitPanel()
    {
        IsStartCountDown = false;
        CountDownTime = 0;
        CurrentTime = 0;
        IsColorChanged = false;
        DeathTime.color = Color.white;
        DeathTime.text = "0.00";
        UImanager.Instance.HidePanel<PlayerPanel>();
    }

    /// <summary>
    /// 仅接收倒计时时间，由Timeline触发开始
    /// </summary>
    public void StartCountDown(float time,string GunName,string attackerName)
    {
        CountDownTime = time;
        CurrentTime = time;
        DeathTime.text = CurrentTime.ToString("F2");
        Killer.text = "击杀者:" + attackerName;
        KillersGun.text = "击杀枪械:" + GunName;
    }

    /// <summary>
    /// Timeline动画完成后调用，正式开启倒计时
    /// </summary>
    public void StartDeath()
    {
        IsStartCountDown = true;
        IsColorChanged = false;
        DeathTime.color = Color.white;
    }
}