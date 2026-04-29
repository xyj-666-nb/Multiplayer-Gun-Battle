using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeveloperSponsorPanel : BasePanel
{
    private const int SponsorRewardGold = 100;


    private bool _isWaitingRewardAd = false;

    public override void Awake()
    {
        base.Awake();
    }

    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
        
        if(controlName== "ADButton")
        {
            MusicManager.Instance?.PlayEffect("Music/update415/ui\u9009\u62e9");
            WatchRewardAd();
        }
        else if(controlName == "ReturnButton")
        {
            MusicManager.Instance?.PlayEffect("Music/update415/ui\u8fd4\u56de");
            ClosePanel();
        }

    }

    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        base.ShowMe(isNeedDefaultAnimator);
    }

    public override void SimpleHidePanel()
    {
        base.SimpleHidePanel();
    }

    public override void SimpleShowPanel()
    {
        base.SimpleShowPanel();
    }

    public override void Start()
    {
        base.Start();
        _isWaitingRewardAd = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {
    }
    private void WatchRewardAd()
    {
        if (_isWaitingRewardAd)
        {
            return;
        }

        _isWaitingRewardAd = true;
        /* Debug.Log("[DeveloperSponsorPanel] Request reward ad for sponsor panel"); */

        TapAdManager.Instance.ShowRewardAd(
            onRewarded: OnRewardAdSuccess,
            onFailed: OnRewardAdFailed
        );
    }

    private void OnRewardAdSuccess()
    {
        _isWaitingRewardAd = false;

        if (GoldSystem.Instance != null)
        {
            GoldSystem.Instance.AddGold(SponsorRewardGold, "\u5f00\u53d1\u8005\u8d5e\u52a9\u5956\u52b1");
            /* Debug.Log($"[DeveloperSponsorPanel] Reward ad success, grant {SponsorRewardGold} gold"); */
        }
        else
        {
            /* Debug.LogError("[DeveloperSponsorPanel] GoldSystem missing, cannot grant sponsor reward"); */
        }

        ClosePanel();
    }

    private void OnRewardAdFailed()
    {
        _isWaitingRewardAd = false;
        /* Debug.LogWarning("[DeveloperSponsorPanel] Reward ad failed"); */
    }

    private void ClosePanel()
    {
        UImanager.Instance.HidePanel<DeveloperSponsorPanel>();
    }
}
