using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音效/背景音乐管理器
/// 功能：统一管理背景音乐播放、音效播放（2D/3D）、音量控制、对象池回收
/// 支持音效音量放大（特殊音量可>1），可配置最大放大倍数防止失真
/// </summary>
public class MusicManager : SingleMonoAutoBehavior<MusicManager>
{
    #region 新增：音效放大配置（可在Inspector面板调整）
    [Header("音效放大设置")]
    [Tooltip("特殊音量最大放大倍数（默认2倍，建议不超过3倍避免严重失真）")]
    [SerializeField] private float MaxEffectAmplification = 2f;
    #endregion

    #region 背景音乐管理
    #region 背景音乐相关变量
    private AudioSource backgroundAudioSource; // 背景音乐AudioSource
    private string currentBgmPath; // 当前播放的背景音乐路径
    private float bgmGlobalVolume = 0.5f; // 背景音乐全局音量
    private readonly Dictionary<string, float> specificBgmVolumes = new Dictionary<string, float>(); // 特定BGM的音量配置
    private GameObject backgroundMusicObj; // 背景音乐载体物体
    #endregion

    #region 初始化背景音乐播放器
    /// <summary>
    /// 初始化背景音乐系统
    /// </summary>
    private void InitializeBackgroundMusic()
    {
        if (backgroundMusicObj == null)
        {
            backgroundMusicObj = new GameObject("BackgroundMusic");
            backgroundMusicObj.transform.SetParent(transform);
            backgroundAudioSource = backgroundMusicObj.AddComponent<AudioSource>();
            backgroundAudioSource.loop = true;
            backgroundAudioSource.volume = bgmGlobalVolume;
            // 背景音乐默认2D播放（全局无衰减）
            backgroundAudioSource.spatialBlend = 0f;
        }
    }
    #endregion

    #region 对当前背景音乐进行播放，暂停，停止等操作
    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="audioPath">背景音乐资源路径</param>
    public void PlayBgm(string audioPath = null)
    {
        SetBgmGlobalVolume(1f);
        Debug.Log("播放音乐");
        // 空路径：继续播放当前音乐
        if (string.IsNullOrEmpty(audioPath))
        {
            if (backgroundAudioSource.clip != null && !backgroundAudioSource.isPlaying)
            {
                backgroundAudioSource.Play();
            }
            else if (backgroundAudioSource.clip == null)
            {
                Debug.LogWarning("没有可播放的背景音乐剪辑！");
            }
            return;
        }

        // 同一首音乐正在播放：直接返回
        if (audioPath == currentBgmPath && backgroundAudioSource.isPlaying)
            return;

        currentBgmPath = audioPath;
        ResourcesManager.Instance.LoadAsync<AudioClip>(audioPath, (audioClip) =>
        {
            if (audioClip == null || backgroundAudioSource == null)
            {
                Debug.LogError($"加载背景音乐失败: {audioPath}");
                return;
            }

            backgroundAudioSource.clip = audioClip;
            // 计算最终音量：特定音量（有则用） * 全局音量
            float finalVolume = specificBgmVolumes.TryGetValue(audioPath, out float specificVol)
                ? specificVol * bgmGlobalVolume
                : bgmGlobalVolume;
            backgroundAudioSource.volume = finalVolume;
            backgroundAudioSource.Play();
        });
    }

    /// <summary>
    /// 暂停/继续播放背景音乐
    /// </summary>
    /// <param name="isPause">true=暂停，false=继续</param>
    public void PauseOrResumeBgm(bool isPause)
    {
        if (backgroundAudioSource == null)
        {
            Debug.LogWarning("背景音乐组件未初始化！");
            return;
        }

        if (isPause)
            backgroundAudioSource.Pause();
        else
            backgroundAudioSource.Play();
    }

    /// <summary>
    /// 停止播放背景音乐并清空资源
    /// </summary>
    public void StopBgm()
    {
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.Stop();
            backgroundAudioSource.clip = null;
            currentBgmPath = null;
        }
    }
    #endregion

    #region 背景音乐音量控制
    /// <summary>
    /// 修改背景音乐全局音量
    /// </summary>
    /// <param name="value">0-1的音量值</param>
    public void SetBgmGlobalVolume(float value)
    {
        bgmGlobalVolume = Mathf.Clamp01(value);

        if (backgroundAudioSource == null || string.IsNullOrEmpty(currentBgmPath))
        {
            Debug.LogWarning("背景音乐未播放，无法修改全局音量！");
            return;
        }

        // 重新计算最终音量
        float finalVolume = specificBgmVolumes.TryGetValue(currentBgmPath, out float specificVol)
            ? specificVol * bgmGlobalVolume
            : bgmGlobalVolume;
        backgroundAudioSource.volume = finalVolume;
    }

    public float GetGlobalVolume()
    {
        return bgmGlobalVolume;
    }

    /// <summary>
    /// 设置指定背景音乐的独立音量
    /// </summary>
    /// <param name="bgmName">背景音乐路径/名称</param>
    /// <param name="volume">0-1的音量值</param>
    public void SetSpecificBgmVolume(string bgmName, float volume)
    {
        if (string.IsNullOrEmpty(bgmName))
        {
            Debug.LogWarning("BGM名称不能为空！");
            return;
        }

        volume = Mathf.Clamp01(volume);
        if (specificBgmVolumes.ContainsKey(bgmName))
            specificBgmVolumes[bgmName] = volume;
        else
            specificBgmVolumes.Add(bgmName, volume);

        if (backgroundAudioSource != null && currentBgmPath == bgmName)
            backgroundAudioSource.volume = volume * bgmGlobalVolume;
    }
    #endregion
    #endregion

    #region 音效管理
    #region 音效相关变量以及配置
    [Header("音效配置")]
    [SerializeField] private List<AudioSource> activeEffectSources = new List<AudioSource>(); // 活跃音效列表
    [SerializeField] private GameObject effectPrefab; // 音效预制体（需挂载AudioSource或动态添加）

    private float effectGlobalVolume = 0.8f; // 音效全局音量（0-1）
    private readonly Dictionary<string, float> specificEffectVolumes = new Dictionary<string, float>(); // 特定音效的音量配置
    private const float DefaultMin3dDistance = 1f; // 3D音效默认最小无衰减距离
    private const float DefaultMax3dDistance = 10f; // 3D音效默认最大衰减距离
    private GameObject _dynamicEffectRoot; // 动态创建的音效根物体（无预制体时使用）
    #endregion

    #region 初始化音效系统
    /// <summary>
    /// 初始化音效系统
    /// </summary>
    private void InitializeEffectSystem()
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("音效预制体未赋值！将使用动态创建的音效物体（无对象池优化），建议在Inspector面板赋值effectPrefab", this);
            // 创建动态音效根物体，统一管理动态创建的音效
            _dynamicEffectRoot = new GameObject("DynamicEffectRoot");
            _dynamicEffectRoot.transform.SetParent(transform);
        }

        // 注册帧更新回调：清理已播放完成的音效（无论是否有预制体都注册）
        if (MonoMange.Instance != null)
        {
            MonoMange.Instance.AddLister_Update(CleanupFinishedEffects);
            MonoMange.Instance.AddLister_OnDestroy(ClearAllEffects);
        }
        else
        {
            Debug.LogWarning("MonoMange.Instance为空，无法注册音效清理回调！", this);
        }
    }
    #endregion

    #region 私有的(3D)音效播放核心逻辑
    /// <summary>
    /// 音效播放核心逻辑（支持音量放大）
    /// </summary>
    /// <param name="clip">要播放的音频剪辑</param>
    /// <param name="is3d">是否启用3D音效</param>
    /// <param name="SpecialVolume">特殊音量（可>1放大，受MaxEffectAmplification限制）</param>
    /// <param name="max3dDistance">3D音效最大衰减距离</param>
    /// <param name="min3dDistance">3D音效最小无衰减距离</param>
    /// <param name="owner">音效跟随的父物体</param>
    /// <param name="isLoop">是否循环播放</param>
    /// <param name="callback">播放完成回调</param>
    /// <param name="isDetach">是否分离播放（默认true：仅赋值位置，不跟随物体运动）</param>
    private void PlayEffectCore(
        AudioClip clip,
        bool is3d,
        float SpecialVolume = 1f,
        float max3dDistance = DefaultMax3dDistance,
        float min3dDistance = DefaultMin3dDistance,
        Transform owner = null,
        bool isLoop = false,
        UnityAction<AudioSource> callback = null,
        bool isDetach = true) // 新增：是否分离播放参数
    {
        if (clip == null)
        {
            Debug.LogError("AudioClip为空，无法播放音效！");
            callback?.Invoke(null);
            return;
        }

        float clampedVolume = Mathf.Clamp(SpecialVolume, 0f, MaxEffectAmplification);
        if (SpecialVolume > MaxEffectAmplification)
        {
            Debug.LogWarning($"特殊音量{SpecialVolume}超过最大放大倍数{MaxEffectAmplification}，已自动限制");
        }
        else if (SpecialVolume > 1f)
        {
            Debug.Log($"为音效【{clip.name}】设置放大音量：{clampedVolume}倍");
        }
        SetSpecificEffectVolume(clip.name, clampedVolume);

        GameObject effectObj = null;
        bool isDynamicObj = false; // 是否是动态创建的物体（无预制体）

        if (effectPrefab != null)
        {
            effectObj = PoolManage.Instance?.GetObj(effectPrefab);
            if (effectObj == null)
            {
                Debug.LogWarning($"对象池获取音效物体失败，将动态创建：{clip.name}", this);
                isDynamicObj = true;
            }
        }
        else
        {
            isDynamicObj = true;
        }

        if (isDynamicObj)
        {
            effectObj = new GameObject($"DynamicEffect_{clip.name}");
            // 挂载到动态根物体下统一管理
            if (_dynamicEffectRoot != null)
            {
                effectObj.transform.SetParent(_dynamicEffectRoot.transform);
            }
            // 2D游戏适配：固定Z轴为0
            Vector3 pos = effectObj.transform.position;
            pos.z = 0f;
            effectObj.transform.position = pos;
        }

        if (effectObj == null)
        {
            Debug.LogError($"无法创建/获取音效物体，播放失败：{clip.name}", this);
            callback?.Invoke(null);
            return;
        }

        AudioSource audioSource = effectObj.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = effectObj.AddComponent<AudioSource>();
        }

        // 音量计算：特殊音量(可>1) * 全局音量(0-1)
        float specificVol = specificEffectVolumes.TryGetValue(clip.name, out float vol) ? vol : 1f;
        float finalVolume = specificVol * effectGlobalVolume;
        Debug.Log($"音效【{clip.name}】最终音量：{finalVolume} (特殊音量{specificVol} × 全局音量{effectGlobalVolume})");

        if (is3d)
            // 传递新增的isDetach参数
            Configure3dEffect(effectObj, audioSource, max3dDistance, min3dDistance, owner, isDetach);
        else
            audioSource.spatialBlend = 0f;//2D音效：关闭3D空间混合

        audioSource.clip = clip;
        audioSource.loop = isLoop;
        audioSource.volume = finalVolume;
        audioSource.Play();

        if (!activeEffectSources.Contains(audioSource))
        {
            activeEffectSources.Add(audioSource);
        }

        if (isDynamicObj)
        {
            audioSource.gameObject.AddComponent<DynamicEffectMarker>();
        }

        callback?.Invoke(audioSource);
    }

    /// <summary>
    /// 配置3D音效参数
    /// </summary>
    private void Configure3dEffect(GameObject effectObj, AudioSource audioSource, float maxDistance, float minDistance, Transform owner, bool isDetach)
    {
        // 参数合法性校验
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance); // 确保最大距离>最小距离

        // 3D音效核心配置
        audioSource.spatialBlend = 1f; // 纯3D音效
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // 对数衰减（更自然）

        if (owner != null)
        {

            effectObj.transform.position = owner.position;
            Vector3 pos = effectObj.transform.position;
            pos.z = 0f;
            effectObj.transform.position = pos;

            if (!isDetach)
            {
                effectObj.transform.SetParent(owner);
                // 设为子对象后，本地位置Z轴也固定0
                Vector3 localPos = effectObj.transform.localPosition;
                localPos.z = 0f;
                effectObj.transform.localPosition = localPos;
            }
            else
            {
                effectObj.transform.SetParent(null);
            }
        }
        else
        {
            // 无owner时，固定Z轴为0
            Vector3 pos = effectObj.transform.position;
            pos.z = 0f;
            effectObj.transform.position = pos;
        }
    }
    #endregion

    #region 音效播放重载方法
    /// <summary>
    /// 播放2D音效（传资源路径）
    /// </summary>
    /// <param name="clipPath">音效资源路径</param>
    /// <param name="SpecialVolume">特殊音量（可>1放大，默认1f）</param>
    /// <param name="isLoop">是否循环播放</param>
    /// <param name="callback">播放完成回调</param>
    public void PlayEffect(string clipPath, float SpecialVolume = 1f, bool isLoop = false, UnityAction<AudioSource> callback = null)
    {
        if (string.IsNullOrEmpty(clipPath))
        {
            Debug.LogWarning("音效路径不能为空！");
            callback?.Invoke(null);
            return;
        }

        ResourcesManager.Instance.LoadAsync<AudioClip>(clipPath, (clip) =>
        {
            PlayEffectCore(clip, false, SpecialVolume, 0, 0, null, isLoop, callback);
        });
    }

    /// <summary>
    /// 播放3D音效（传资源路径）
    /// </summary>
    /// <param name="clipPath">音效资源路径</param>
    /// <param name="SpecialVolume">特殊音量（可>1放大，默认1f）</param>
    /// <param name="maxDistance">3D音效最大衰减距离</param>
    /// <param name="minDistance">3D音效最小无衰减距离</param>
    /// <param name="owner">音效跟随的父物体</param>
    /// <param name="isLoop">是否循环播放</param>
    /// <param name="callback">播放完成回调</param>
    /// <param name="isDetach">是否分离播放（默认true：仅赋值位置，不跟随物体运动）</param>
    public void PlayEffect3D(
        string clipPath,
        float SpecialVolume = 1f,
        float maxDistance = DefaultMax3dDistance,
        float minDistance = DefaultMin3dDistance,
        Transform owner = null,
        bool isLoop = false,
        UnityAction<AudioSource> callback = null,
        bool isDetach = true) // 新增：是否分离播放参数
    {
        if (string.IsNullOrEmpty(clipPath))
        {
            Debug.LogWarning("音效路径不能为空！");
            callback?.Invoke(null);
            return;
        }

        ResourcesManager.Instance.LoadAsync<AudioClip>(clipPath, (clip) =>
        {
            // 传递isDetach参数
            PlayEffectCore(clip, true, SpecialVolume, maxDistance, minDistance, owner, isLoop, callback, isDetach);
        });
    }

    /// <summary>
    /// 播放2D音效（传AudioClip）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="SpecialVolume">特殊音量（可>1放大，默认1f）</param>
    /// <param name="isLoop">是否循环播放</param>
    /// <param name="callback">播放完成回调</param>
    public void PlayEffect(AudioClip clip, float SpecialVolume = 1f, bool isLoop = false, UnityAction<AudioSource> callback = null)
    {
        PlayEffectCore(clip, false, SpecialVolume, 0, 0, null, isLoop, callback);
    }

    /// <summary>
    /// 播放3D音效（传AudioClip）
    /// </summary>
    /// <param name="clip">音频剪辑</param>
    /// <param name="SpecialVolume">特殊音量（可>1放大，默认1f）</param>
    /// <param name="maxDistance">3D音效最大衰减距离</param>
    /// <param name="minDistance">3D音效最小无衰减距离</param>
    /// <param name="owner">音效跟随的父物体</param>
    /// <param name="isLoop">是否循环播放</param>
    /// <param name="callback">播放完成回调</param>
    /// <param name="isDetach">是否分离播放（默认true：仅赋值位置，不跟随物体运动）</param>
    public void PlayEffect3D(
        AudioClip clip,
        float SpecialVolume = 1f,
        float maxDistance = DefaultMax3dDistance,
        float minDistance = DefaultMin3dDistance,
        Transform owner = null,
        bool isLoop = false,
        UnityAction<AudioSource> callback = null,
        bool isDetach = true) // 新增：是否分离播放参数
    {
        PlayEffectCore(clip, true, SpecialVolume, maxDistance, minDistance, owner, isLoop, callback, isDetach);
    }

    /// <summary>
    /// 使用外部AudioSource播放音效（复用已有组件，支持音量放大）
    /// </summary>
    /// <param name="externalSource">外部AudioSource组件</param>
    /// <param name="SpecialVolume">特殊音量（可>1放大，默认1f）</param>
    /// <param name="isLoop">是否循环播放</param>
    /// <param name="callback">播放完成回调</param>
    public void PlayEffect(AudioSource externalSource, float SpecialVolume = 1f, bool isLoop = false, UnityAction<AudioSource> callback = null)
    {
        if (externalSource == null)
        {
            Debug.LogError("外部AudioSource为空！");
            callback?.Invoke(null);
            return;
        }

        if (externalSource.clip == null)
        {
            Debug.LogError($"外部AudioSource【{externalSource.gameObject.name}】未赋值AudioClip！");
            callback?.Invoke(externalSource);
            return;
        }

        // 支持放大音量并限制最大值
        float clampedVolume = Mathf.Clamp(SpecialVolume, 0f, MaxEffectAmplification);
        SetSpecificEffectVolume(externalSource.clip.name, clampedVolume);

        // 音量计算：特殊音量(可>1) * 全局音量(0-1)
        float specificVol = specificEffectVolumes.TryGetValue(externalSource.clip.name, out float vol) ? vol : 1f;
        externalSource.volume = specificVol * effectGlobalVolume;
        externalSource.loop = isLoop;
        externalSource.Play();

        // 加入管理列表
        if (!activeEffectSources.Contains(externalSource))
        {
            activeEffectSources.Add(externalSource);
        }

        callback?.Invoke(externalSource);
    }
    #endregion

    #region 对音效的管理操作
    /// <summary>
    /// 清理已播放完成的非循环音效
    /// </summary>
    private void CleanupFinishedEffects()
    {
        for (int i = activeEffectSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeEffectSources[i];
            if (source == null)
            {
                activeEffectSources.RemoveAt(i);
                continue;
            }

            // 非循环+未播放：归还/销毁物体
            if (!source.isPlaying && !source.loop)
            {
                ReturnEffectToPool(source);
                activeEffectSources.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 停止指定音效并归还/销毁物体
    /// </summary>
    /// <param name="source">要停止的音效AudioSource</param>
    public void StopEffect(AudioSource source)
    {
        if (source == null || !activeEffectSources.Contains(source))
        {
            Debug.LogWarning("音效未在活跃列表中，无需停止！");
            return;
        }

        source.Stop();
        ReturnEffectToPool(source);
        activeEffectSources.Remove(source);
    }

    /// <summary>
    /// 暂停/继续所有活跃音效
    /// </summary>
    /// <param name="isPause">true=暂停，false=继续</param>
    public void PauseOrResumeAllEffects(bool isPause)
    {
        foreach (var source in activeEffectSources)
        {
            if (source == null) continue;

            if (isPause)
                source.Pause();
            else
                source.UnPause();
        }
    }
    #endregion

    #region 音效的音量全局以及特殊控制（支持放大）
    /// <summary>
    /// 设置音效全局音量
    /// </summary>
    /// <param name="value">0-1的音量值（统一控制所有音效的基础音量）</param>
    public void SetEffectGlobalVolume(float value)
    {
        effectGlobalVolume = Mathf.Clamp01(value);
        Debug.Log($"音效全局音量已设置为：{effectGlobalVolume}，活跃音效数：{activeEffectSources.Count}");

        // 批量更新所有活跃音效音量（保留放大效果）
        foreach (var source in activeEffectSources)
        {
            if (source == null || source.clip == null) continue;

            float specificVol = specificEffectVolumes.TryGetValue(source.clip.name, out float vol) ? vol : 1f;
            source.volume = specificVol * effectGlobalVolume;
            Debug.Log($"更新音效【{source.clip.name}】音量为：{source.volume}");
        }
    }

    /// <summary>
    /// 获取音效全局音量
    /// </summary>
    /// <returns>0-1的音量值</returns>
    public float GetEffectGlobalVolume()
    {
        return effectGlobalVolume;
    }

    /// <summary>
    /// 设置指定音效的独立音量
    /// </summary>
    /// <param name="effectName">音效名称（对应AudioClip.name）</param>
    /// <param name="volume">音量值（0~MaxEffectAmplification）</param>
    public void SetSpecificEffectVolume(string effectName, float volume)
    {
        if (string.IsNullOrEmpty(effectName))
        {
            Debug.LogWarning("音效名称不能为空！");
            return;
        }

        // 限制音量范围：0 ~ 最大放大倍数
        float clampedVolume = Mathf.Clamp(volume, 0f, MaxEffectAmplification);
        if (volume != clampedVolume)
        {
            Debug.Log($"音效【{effectName}】音量{volume}已限制在0~{MaxEffectAmplification}");
        }

        if (specificEffectVolumes.ContainsKey(effectName))
            specificEffectVolumes[effectName] = clampedVolume;
        else
            specificEffectVolumes.Add(effectName, clampedVolume);

        // 实时更新活跃的该音效音量
        foreach (var source in activeEffectSources)
        {
            if (source != null && source.clip != null && source.clip.name == effectName)
            {
                source.volume = clampedVolume * effectGlobalVolume;
            }
        }
    }

    /// <summary>
    /// 批量设置所有活跃音效的音量（快速调节）
    /// </summary>
    /// <param name="volume">音量值（0~MaxEffectAmplification）</param>
    public void AddAllSpecificEffectVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp(volume, 0f, MaxEffectAmplification);
        foreach (var source in activeEffectSources)
        {
            source.volume = clampedVolume * effectGlobalVolume;
        }
    }
    #endregion

    #region 归还/销毁音效物体
    /// <summary>
    /// 清空所有活跃音效并归还/销毁物体
    /// </summary>
    public void ClearAllEffects()
    {
        for (int i = activeEffectSources.Count - 1; i >= 0; i--)
        {
            ReturnEffectToPool(activeEffectSources[i]);
        }
        activeEffectSources.Clear();
    }

    /// <summary>
    /// 将音效物体归还对象池（预制体）或销毁（动态创建）
    /// </summary>
    /// <param name="audioSource">音效的AudioSource组件</param>
    private void ReturnEffectToPool(AudioSource audioSource)
    {
        if (audioSource == null) return;

        GameObject effectObj = audioSource.gameObject;
        // 清理音效组件状态
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;

        // 判断是否是动态创建的物体
        if (effectObj.GetComponent<DynamicEffectMarker>() != null)
        {
            // 动态物体直接销毁
            Destroy(effectObj);
            return;
        }

        // 预制体物体：归还对象池
        if (effectPrefab != null && PoolManage.Instance != null)
        {
            // 归还前解除父物体引用
            effectObj.transform.SetParent(null);
            PoolManage.Instance.PushObj(effectPrefab, effectObj);
        }
        else if (effectPrefab == null)
        {
            // 无预制体但不是动态物体：直接销毁
            Destroy(effectObj);
        }
    }

    // 标记动态创建的音效物体
    private class DynamicEffectMarker : MonoBehaviour { }
    #endregion
    #endregion

    #region 生命周期
    protected override void Awake()
    {
        base.Awake();
        InitializeBackgroundMusic();
        InitializeEffectSystem();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 销毁动态音效根物体
        if (_dynamicEffectRoot != null)
        {
            Destroy(_dynamicEffectRoot);
        }
    }
    #endregion

    #region 只读属性
    /// <summary>
    /// 背景音乐是否正在播放
    /// </summary>
    public bool IsBgmPlaying => backgroundAudioSource != null && backgroundAudioSource.isPlaying;

    /// <summary>
    /// 当前背景音乐全局音量
    /// </summary>
    public float CurrentBgmGlobalVolume => bgmGlobalVolume;

    /// <summary>
    /// 当前音效全局音量
    /// </summary>
    public float CurrentEffectGlobalVolume => effectGlobalVolume;

    /// <summary>
    /// 活跃音效数量
    /// </summary>
    public int ActiveEffectCount => activeEffectSources.Count;
    #endregion
}