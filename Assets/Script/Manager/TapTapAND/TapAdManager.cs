using System;
using Dirichlet.Ad;
using UnityEngine;

public class TapAdManager : SingleMonoAutoBehavior<TapAdManager>
{
    [Header("广告配置（请替换为你后台的真实数据）")]
    [Tooltip("MediaId")]
    public long MediaId = 1000007;
    [Tooltip("MediaKey")]
    public string MediaKey = "1AjDOjD0F3SDDmgTuBQHbCRULSizYPHV17viZObHvhDjf7Pq1rlarueOX1cYBucn";
    [Tooltip("激励视频广告位ID")]
    public long RewardSlotId = 1001253;

    [Header("调试设置")]
    [Tooltip("开启调试日志")]
    public bool EnableDebugLog = true;

    // 广告核心对象
    private DirichletAdNative _adNative;
    private DirichletRewardVideoAd _rewardAd;
    private bool _isAdShowing = false; // 防止广告重复展示

    // 缓存的回调
    private Action _onRewardedCallback;
    private Action _onFailedCallback;

    protected override void Awake()
    {
        base.Awake();
        // 初始化时自动预加载广告
        InitSdk();
    }

    #region 核心初始化
    /// <summary>
    /// 初始化广告SDK
    /// </summary>
    public void InitSdk()
    {
        if (DirichletAdSdk.IsInitialized)
        {
            Debug.Log("[TapAd] SDK已初始化，跳过重复操作");
            PreloadRewardAd();
            return;
        }

        var config = new DirichletAdConfig.Builder()
            .WithMediaId(MediaId)
            .WithMediaKey(MediaKey)
            .WithMediaName("你的游戏名称")
            .EnableDebug(EnableDebugLog)
            .Build();

        Debug.Log($"[TapAd] 开始初始化SDK，MediaId: {MediaId}");

        DirichletAdSdk.Init(config,
            result =>
            {
                Debug.Log("[TapAd] SDK初始化成功");
                _adNative = DirichletAdManager.CreateAdNative();
                PreloadRewardAd();
            },
            error =>
            {
                Debug.LogError($"[TapAd] SDK初始化失败: {error}");
            });
    }
    #endregion

    #region 激励视频广告
    /// <summary>
    /// 预加载激励视频广告（建议在游戏启动、对局中提前调用）
    /// </summary>
    public void PreloadRewardAd()
    {
        if (!DirichletAdSdk.IsInitialized || _adNative == null)
        {
            Debug.LogWarning("[TapAd] SDK未初始化，无法预加载广告");
            return;
        }

        // 清理旧广告
        CleanupAd(ref _rewardAd);

        var userId = string.IsNullOrEmpty(SystemInfo.deviceUniqueIdentifier) ? "unity_user" : SystemInfo.deviceUniqueIdentifier;

        var request = new DirichletAdRequest.Builder()
            .WithSpaceId(RewardSlotId)
            .WithUserId(userId)
            .WithRewardName("金币")
            .WithRewardAmount(10)
            .Build();

        Debug.Log("[TapAd] 开始预加载激励视频广告");

        _adNative.LoadRewardVideoAd(request,
            ad =>
            {
                _rewardAd = ad;
                AttachRewardEvents(_rewardAd);
                Debug.Log("[TapAd] 激励视频广告加载成功");
            },
            error =>
            {
                Debug.LogError($"[TapAd] 激励视频广告加载失败: {error}");
            });
    }

    /// <summary>
    /// 展示激励视频广告
    /// </summary>
    /// <param name="onRewarded">广告观看完成，可发放奖励的回调</param>
    /// <param name="onFailed">广告加载/播放失败的回调</param>
    public void ShowRewardAd(Action onRewarded, Action onFailed = null)
    {
        if (_isAdShowing)
        {
            Debug.LogWarning("[TapAd] 广告正在展示中，跳过重复请求");
            return;
        }

        if (_rewardAd == null)
        {
            Debug.LogWarning("[TapAd] 激励视频广告未加载，尝试重新预加载");
            onFailed?.Invoke();
            PreloadRewardAd();
            return;
        }

        // 缓存回调
        _onRewardedCallback = onRewarded;
        _onFailedCallback = onFailed;
        _isAdShowing = true;

        var shown = _rewardAd.Show();
        if (!shown)
        {
            Debug.LogError("[TapAd] 激励视频广告展示调用失败");
            _isAdShowing = false;
            _onFailedCallback?.Invoke();
            PreloadRewardAd();
        }
    }
    #endregion

    #region 广告事件绑定
    private void AttachRewardEvents(DirichletRewardVideoAd ad)
    {
        if (ad == null) return;

        // 广告展示
        ad.Shown += () =>
        {
            Debug.Log("[TapAd] 激励视频广告展示");
        };

        // 广告点击
        ad.Clicked += () =>
        {
            Debug.Log("[TapAd] 激励视频广告被点击");
        };

        // 广告关闭
        ad.Closed += () =>
        {
            Debug.Log("[TapAd] 激励视频广告关闭");
            _isAdShowing = false;
            // 广告关闭后重新预加载下一条
            PreloadRewardAd();
        };

        // 广告跳过
        ad.Skipped += () =>
        {
            Debug.Log("[TapAd] 激励视频广告被跳过");
        };

        // 核心：奖励验证回调
        ad.RewardVerified += args =>
        {
            if (args.IsVerified)
            {
                Debug.Log($"[TapAd] 激励视频奖励验证通过！奖励: {args.RewardName} x{args.RewardAmount}");
                _onRewardedCallback?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[TapAd] 激励视频奖励验证失败: {args.Message}");
                _onFailedCallback?.Invoke();
            }
        };
    }
    #endregion

    #region 清理逻辑
    private void CleanupAd<T>(ref T ad) where T : DirichletAd
    {
        if (ad != null)
        {
            try
            {
                ad.Destroy();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TapAd] 清理广告时异常: {e.Message}");
            }
            ad = null;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CleanupAd(ref _rewardAd);
    }
    #endregion
}