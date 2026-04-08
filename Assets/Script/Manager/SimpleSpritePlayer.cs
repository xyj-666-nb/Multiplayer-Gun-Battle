using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public static class SimpleSpritePlayer
{
    private class PlayerState
    {
        public int TimerKey;
        public Sprite[] Sprites;
        public int CurrentIndex;
        public object Target; // 存 SpriteRenderer 或 Image
        public bool IsLooping; // 是否是无限循环模式
    }

    // 两个字典分别用于正向和反向查找
    private static Dictionary<int, PlayerState> _keyToState = new Dictionary<int, PlayerState>();
    private static Dictionary<object, int> _targetToKey = new Dictionary<object, int>();

    #region 公共播放 API

    // --- 基础播放 (指定时长，播放完自动停止) ---

    public static void Play(SpriteRenderer renderer, Sprite[] sprites, float duration, bool useRealTime = false)
    {
        InternalPlay(renderer, sprites, duration, false, useRealTime);
    }

    public static void Play(Image image, Sprite[] sprites, float duration, bool useRealTime = false)
    {
        InternalPlay(image, sprites, duration, false, useRealTime);
    }

    // --- 无限循环播放 (不指定时长，一直播到手动停止或目标丢失) ---

    public static void PlayLoop(SpriteRenderer renderer, Sprite[] sprites, float frameInterval, bool useRealTime = false)
    {
        InternalPlay(renderer, sprites, frameInterval, true, useRealTime);
    }

    public static void PlayLoop(Image image, Sprite[] sprites, float frameInterval, bool useRealTime = false)
    {
        InternalPlay(image, sprites, frameInterval, true, useRealTime);
    }

    #endregion

    #region 公共停止 API

    /// <summary>
    /// 通过 TimerKey 停止
    /// </summary>
    public static void Stop(int timerKey)
    {
        if (_keyToState.TryGetValue(timerKey, out var state))
        {
            CleanupState(state);
        }
    }

    /// <summary>
    /// 通过目标组件 (SpriteRenderer) 停止
    /// </summary>
    public static void Stop(SpriteRenderer renderer)
    {
        StopByTarget(renderer);
    }

    /// <summary>
    /// 通过目标组件 (Image) 停止
    /// </summary>
    public static void Stop(Image image)
    {
        StopByTarget(image);
    }

    #endregion

    #region 内部逻辑

    private static void InternalPlay(object target, Sprite[] sprites, float timeParam, bool isLoop, bool useRealTime)
    {
        if (!CheckIsValid(target, sprites)) return;

        StopByTarget(target);

        var state = new PlayerState
        {
            Sprites = sprites,
            CurrentIndex = 0,
            Target = target,
            IsLooping = isLoop
        };

        ApplySprite(state);

        int intervalMs;
        if (isLoop)
        {
            // 循环模式：timeParam 代表 每帧间隔(秒)
            intervalMs = Mathf.RoundToInt(timeParam * 1000);
            // 使用永久计时器
            state.TimerKey = CountDownManager.Instance.CreateTimer_Permanent(useRealTime, intervalMs, () => OnTimerTick(state.TimerKey));
        }
        else
        {
            // 普通模式：timeParam 代表 总时长(秒)
            int totalMs = Mathf.RoundToInt(timeParam * 1000);
            intervalMs = Mathf.RoundToInt(totalMs / (float)sprites.Length);
            // 使用普通计时器
            state.TimerKey = CountDownManager.Instance.CreateTimer(useRealTime, totalMs, () => OnPlayComplete(state.TimerKey), intervalMs, () => OnTimerTick(state.TimerKey));
        }

        _keyToState.Add(state.TimerKey, state);
        _targetToKey.Add(target, state.TimerKey);
    }

    private static bool CheckIsValid(object target, Sprite[] sprites)
    {
        if (target == null) { Debug.LogError("播放目标为空！"); return false; }
        if (sprites == null || sprites.Length == 0) { Debug.LogError("Sprite 数组为空！"); return false; }
        return true;
    }

    private static void OnTimerTick(int timerKey)
    {
        if (!_keyToState.TryGetValue(timerKey, out var state)) return;

        // 检查目标是否还存在，如果被销毁了，自动停止
        if (!IsTargetAlive(state.Target))
        {
            Debug.LogWarning("播放目标已丢失，自动停止动画。");
            CleanupState(state);
            return;
        }

        state.CurrentIndex++;

        // 处理索引循环或结束
        if (state.CurrentIndex >= state.Sprites.Length)
        {
            if (state.IsLooping)
            {
                state.CurrentIndex = 0; // 循环模式：重置索引
            }
            else
            {
                // 普通模式：这里其实不需要处理，因为 CountDownManager 会触发 OnPlayComplete
                return;
            }
        }

        ApplySprite(state);
    }

    private static bool IsTargetAlive(object target)
    {
        // 利用 Unity 的特殊 null 检查
        if (target is UnityEngine.Object unityObj)
        {
            return unityObj != null;
        }
        return target != null;
    }

    private static void OnPlayComplete(int timerKey)
    {
        if (_keyToState.TryGetValue(timerKey, out var state))
        {
            // 确保播到最后一帧
            state.CurrentIndex = state.Sprites.Length - 1;
            ApplySprite(state);

            CleanupState(state);
        }
    }

    private static void StopByTarget(object target)
    {
        if (target != null && _targetToKey.TryGetValue(target, out int key))
        {
            if (_keyToState.TryGetValue(key, out var state))
            {
                CleanupState(state);
            }
        }
    }

    private static void CleanupState(PlayerState state)
    {
        CountDownManager.Instance.RemoveTimer(state.TimerKey);

        if (_keyToState.ContainsKey(state.TimerKey))
            _keyToState.Remove(state.TimerKey);

        if (state.Target != null && _targetToKey.ContainsKey(state.Target))
            _targetToKey.Remove(state.Target);
    }

    private static void ApplySprite(PlayerState state)
    {
        // 这里再做一次安全检查，防止在这一帧间隙被销毁
        if (state.Target is SpriteRenderer sr && sr != null)
            sr.sprite = state.Sprites[state.CurrentIndex];
        else if (state.Target is Image img && img != null)
            img.sprite = state.Sprites[state.CurrentIndex];
    }

    #endregion
}