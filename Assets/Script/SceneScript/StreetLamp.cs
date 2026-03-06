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
    public float slightFlickerInterval = 2f;  // 轻微破坏闪烁间隔
    public float seriousFlickerInterval = 0.5f;// 严重破坏闪烁间隔
    public float loseFlickerInterval = 0.15f;  // 完全失效闪烁间隔
    public float flickerDuration = 0.1f;       // 单次闪烁时长

    // 网络同步：最终显示的破坏程度
    [SyncVar(hook = nameof(OnDegreeChanged))]
    public StreetLampDegreeOfDestruction DegreeOfDestruction;

    // 服务端私有：分别存储自身状态和电闸状态
    private StreetLampDegreeOfDestruction _selfDegree; // 路灯自己被打坏的程度
    private StreetLampDegreeOfDestruction _switchDegree; // 电闸影响的程度

    [SyncVar]
    private double _flickerStartTime; // 闪烁起始的网络时间戳

    private bool _isFlickering;        // 客户端是否正在闪烁
    private Coroutine _flickerCoroutine;


    #region 网络生命周期（适配基类）
    public override void OnStartServer()
    {
        base.OnStartServer();
        Init();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Init();
    }
    #endregion


    #region 服务端逻辑：状态计算核心

    [Server]
    private void RefreshFinalState()
    {
        // 取两者中最坏的（枚举值最大的）
        StreetLampDegreeOfDestruction worstDegree = (StreetLampDegreeOfDestruction)Mathf.Max((int)_selfDegree, (int)_switchDegree);

        if (worstDegree != DegreeOfDestruction)
        {
            DegreeOfDestruction = worstDegree;
            // 如果进入了需要闪烁的状态，刷新时间戳
            if (worstDegree is StreetLampDegreeOfDestruction.Slight or StreetLampDegreeOfDestruction.Serious or StreetLampDegreeOfDestruction.Lose)
            {
                _flickerStartTime = NetworkTime.time;
            }
        }
    }

    // 1. 路灯自身受到攻击时调用
    public override void HealthChangeEffect(float health)
    {
        if (!isServer)
            return;

        float healthPercent = health / InitHealthValue;
        _selfDegree = healthPercent switch
        {
            > 0.7f => StreetLampDegreeOfDestruction.Normal,
            > 0.3f => StreetLampDegreeOfDestruction.Slight,
            > 0f => StreetLampDegreeOfDestruction.Serious,
            _ => StreetLampDegreeOfDestruction.Lose
        };

        // 状态改变后，重新计算最终显示
        RefreshFinalState();
    }

    // 2. 电闸调用此方法来设置影响
    [Server]
    public void SetSwitchInfluence(StreetLampDegreeOfDestruction switchState)
    {
        if (_switchDegree == switchState) return;

        _switchDegree = switchState;

        // 电闸状态改变后，重新计算最终显示
        RefreshFinalState();
    }

    public override void EffectTrigger()
    {
        if (isServer)
        {
            RpcPlayDestroyEffect();
        }
    }

    [ClientRpc]
    private void RpcPlayDestroyEffect()
    {
        Debug.Log("路灯完全破坏！");
    }
    #endregion


    #region 客户端逻辑：基于网络时间同步闪烁（保持不变）
    private void OnDegreeChanged(StreetLampDegreeOfDestruction oldDegree, StreetLampDegreeOfDestruction newDegree)
    {
        UpdateLightBaseState(newDegree);
        UpdateFlickerState(newDegree);
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
    public override void Init()
    {
        if (isServer)
        {
            _selfDegree = StreetLampDegreeOfDestruction.Normal;
            _switchDegree = StreetLampDegreeOfDestruction.Normal;
            DegreeOfDestruction = StreetLampDegreeOfDestruction.Normal;
            _flickerStartTime = 0;
        }
        else
        {
            UpdateLightBaseState(DegreeOfDestruction);
            UpdateFlickerState(DegreeOfDestruction);
        }
    }

    public override void ResetObj()
    {
        if (isServer)
        {
            // 重置时，自身状态和电闸影响都恢复正常
            _selfDegree = StreetLampDegreeOfDestruction.Normal;
            _switchDegree = StreetLampDegreeOfDestruction.Normal;

            CurrentHealthValue = InitHealthValue;
            IsTrigger = false;

            // 手动刷新一次最终状态
            RefreshFinalState();
        }
        else
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }
            _isFlickering = false;
            Light?.DOKill();
            UpdateLightBaseState(StreetLampDegreeOfDestruction.Normal);
        }
    }
    #endregion
}

// 破坏程度枚举
public enum StreetLampDegreeOfDestruction
{
    Normal,  // 普通（0）- 最好
    Slight,  // 轻微（1）
    Serious, // 严重（2）
    Lose     // 失效（3）- 最坏
}