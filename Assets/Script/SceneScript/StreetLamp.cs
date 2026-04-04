using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public static class Light2DExtensions
{
    public static Tweener DOIntensity(this Light2D light2D, float endValue, float duration)
    {
        return DOTween.To(() => light2D.intensity, x => light2D.intensity = x, endValue, duration);
    }
}

public class StreetLamp : BaseBulletInteract_NetWork
{
    [Header("路灯配置")]
    public Light2D Light;
    public float normalIntensity = 3f;
    public float slightIntensity = 2f;
    public float seriousIntensity = 1f;
    public float loseIntensity = 0.3f;

    [Header("闪烁配置")]
    public float slightFlickerInterval = 2f;
    public float seriousFlickerInterval = 0.5f;
    public float loseFlickerInterval = 0.15f;
    public float flickerDuration = 0.1f;

    [Header("场景损坏图")]
    public SpriteRenderer spriteRenderer;
    public Sprite NormalSprite;
    public Sprite SlightSprite;
    public Sprite SeriousSprite;
    public Sprite LoseSprite;

    // 网络同步：最终显示的破坏程度
    [SyncVar(hook = nameof(OnDegreeChanged))]
    public StreetLampDegreeOfDestruction DegreeOfDestruction;

    // 服务端私有：仅服务器存储状态，不同步客户端
    private StreetLampDegreeOfDestruction _selfDegree;
    private StreetLampDegreeOfDestruction _switchDegree;

    [SyncVar]
    private double _flickerStartTime;

    private bool _isFlickering;
    private Coroutine _flickerCoroutine;


    #region 服务端逻辑：状态计算核心
    [Server]
    private void RefreshFinalState()
    {
        StreetLampDegreeOfDestruction worstDegree = (StreetLampDegreeOfDestruction)Mathf.Max((int)_selfDegree, (int)_switchDegree);

        if (worstDegree != DegreeOfDestruction)
        {
            DegreeOfDestruction = worstDegree;
            if (worstDegree is StreetLampDegreeOfDestruction.Slight or StreetLampDegreeOfDestruction.Serious or StreetLampDegreeOfDestruction.Lose)
            {
                _flickerStartTime = NetworkTime.time;
            }
        }
    }

    // 血量变化（仅服务器执行）
    public override void HealthChangeEffect(float health)
    {
        float healthPercent = health / InitHealthValue;
        _selfDegree = healthPercent switch
        {
            > 0.7f => StreetLampDegreeOfDestruction.Normal,
            > 0.3f => StreetLampDegreeOfDestruction.Slight,
            > 0f => StreetLampDegreeOfDestruction.Serious,
            _ => StreetLampDegreeOfDestruction.Lose
        };

        RefreshFinalState();
    }

    // 电闸影响（仅服务器调用）
    [Server]
    public void SetSwitchInfluence(StreetLampDegreeOfDestruction switchState)
    {
        if (_switchDegree == switchState) return;
        _switchDegree = switchState;
        RefreshFinalState();
    }

    public override void EffectTrigger()
    {
        // 仅服务器触发逻辑，客户端播放特效
        if (isServer)
        {
            RpcPlayDestroyEffect();
        }
        Light.gameObject.layer = LayerMask.NameToLayer("Default");
    }

    [ClientRpc]
    private void RpcPlayDestroyEffect()
    {
        Debug.Log("路灯完全破坏！");
    }
    #endregion

    #region 客户端逻辑：灯光&闪烁&Sprite（纯视觉，无数据修改）
    private void OnDegreeChanged(StreetLampDegreeOfDestruction oldDegree, StreetLampDegreeOfDestruction newDegree)
    {
        UpdateLightBaseState(newDegree);
        UpdateFlickerState(newDegree);
        UpdateSprite(newDegree); // 【新增】同步切换Sprite
    }

    private void UpdateLightBaseState(StreetLampDegreeOfDestruction degree)
    {
        if (Light == null) return;

        Light.DOKill();
        Light.intensity = degree switch
        {
            StreetLampDegreeOfDestruction.Normal => normalIntensity,
            StreetLampDegreeOfDestruction.Slight => slightIntensity,
            StreetLampDegreeOfDestruction.Serious => seriousIntensity,
            StreetLampDegreeOfDestruction.Lose => loseIntensity,
            _ => normalIntensity
        };
    }
    private void UpdateSprite(StreetLampDegreeOfDestruction degree)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = degree switch
        {
            StreetLampDegreeOfDestruction.Normal => NormalSprite,
            StreetLampDegreeOfDestruction.Slight => SlightSprite,
            StreetLampDegreeOfDestruction.Serious => SeriousSprite,
            StreetLampDegreeOfDestruction.Lose => LoseSprite,
            _ => NormalSprite
        };
    }

    private void UpdateFlickerState(StreetLampDegreeOfDestruction degree)
    {
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }
        _isFlickering = false;

        if (degree is StreetLampDegreeOfDestruction.Slight or StreetLampDegreeOfDestruction.Serious or StreetLampDegreeOfDestruction.Lose)
        {
            _isFlickering = true;
            float interval = degree switch
            {
                StreetLampDegreeOfDestruction.Slight => slightFlickerInterval,
                StreetLampDegreeOfDestruction.Serious => seriousFlickerInterval,
                StreetLampDegreeOfDestruction.Lose => loseFlickerInterval,
                _ => slightFlickerInterval
            };
            _flickerCoroutine = StartCoroutine(ClientSyncFlicker(interval));
        }
    }

    private IEnumerator ClientSyncFlicker(float interval)
    {
        while (_isFlickering)
        {
            double timeSinceStart = NetworkTime.time - _flickerStartTime;
            int cycleCount = Mathf.FloorToInt((float)(timeSinceStart / interval));
            double nextFlickerTime = _flickerStartTime + (cycleCount + 1) * interval;

            while (NetworkTime.time < nextFlickerTime && _isFlickering)
            {
                yield return null;
            }

            if (_isFlickering)
            {
                PlayFlickerAnimation();
            }
        }
    }

    private void PlayFlickerAnimation()
    {
        if (Light == null || !_isFlickering) return;

        float originalIntensity = Light.intensity;
        Light.DOIntensity(0f, flickerDuration / 2)
             .OnComplete(() =>
             {
                 if (_isFlickering && Light != null)
                 {
                     Light.DOIntensity(originalIntensity, flickerDuration / 2);
                 }
             });
    }
    #endregion


    #region 基类抽象方法实现
    /// <summary>
    /// 客户端视觉初始化
    /// </summary>
    public override void InitClient()
    {
        UpdateLightBaseState(DegreeOfDestruction);
        UpdateFlickerState(DegreeOfDestruction);
        UpdateSprite(DegreeOfDestruction); // 【新增】初始化时同步Sprite
    }

    /// <summary>
    /// 客户端视觉重置
    /// </summary>
    public override void ResetClient()
    {
        // 停止所有闪烁协程
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }
        _isFlickering = false;

        // 停止DOTween动画，恢复默认灯光
        Light?.DOKill();
        UpdateLightBaseState(StreetLampDegreeOfDestruction.Normal);
        UpdateSprite(StreetLampDegreeOfDestruction.Normal); // 【新增】重置时Sprite归位
    }
    #endregion

    #region 服务器数据管理
    [Server]
    public override void InitServer()
    {
        // 先执行基类服务器初始化
        base.InitServer();

        // 路灯专属服务器数据初始化
        _selfDegree = StreetLampDegreeOfDestruction.Normal;
        _switchDegree = StreetLampDegreeOfDestruction.Normal;
        _flickerStartTime = 0;
        RefreshFinalState();
    }

    [Server]
    public override void ResetServer()
    {
        // 先执行基类服务器重置
        base.ResetServer();

        // 路灯专属服务器数据重置
        _selfDegree = StreetLampDegreeOfDestruction.Normal;
        _switchDegree = StreetLampDegreeOfDestruction.Normal;
        RefreshFinalState();
    }
    #endregion
}

// 破坏程度枚举
public enum StreetLampDegreeOfDestruction
{
    Normal,
    Slight,
    Serious,
    Lose
}