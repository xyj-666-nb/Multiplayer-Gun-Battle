using UnityEngine;

/// <summary>
/// 动画音效控制器
/// 支持10组音效轨道，可通过动画事件调用，支持2D/3D音效
/// 【优化后】兼容MusicManager的effectPrefab未赋值场景
/// </summary>
public class AnimatorSoundController : MonoBehaviour
{
    #region 轨道音频配置
    [Header("=== 10组音效轨道 ===")]
    [Tooltip("轨道1 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip1;
    [Tooltip("轨道2 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip2;
    [Tooltip("轨道3 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip3;
    [Tooltip("轨道4 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip4;
    [Tooltip("轨道5 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip5;
    [Tooltip("轨道6 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip6;
    [Tooltip("轨道7 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip7;
    [Tooltip("轨道8 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip8;
    [Tooltip("轨道9 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip9;
    [Tooltip("轨道10 - 2D/3D音效文件（可空）")]
    public AudioClip SoundClip10;

    // 10组轨道的AudioSource缓存（0-9对应轨道1-10）
    private AudioSource[] _trackAudioSources = new AudioSource[10];
    // MusicManager有效性标记
    private bool _isMusicManagerValid;
    // 自身Transform缓存
    private Transform _selfTransform;
    #endregion

    #region 全局播放配置
    [Header("=== 全局播放配置 ===")]
    [Tooltip("默认是否循环播放")]
    public bool DefaultIsLoop = false;
    [Tooltip("默认音效音量缩放（0-1，叠加全局音量）")]
    [Range(0f, 1f)] public float DefaultVolumeScale = 1f;
    [Tooltip("3D音效默认最大衰减距离")]
    public float Default3dMaxDistance = 10f;
    [Tooltip("3D音效默认最小无衰减距离")]
    public float Default3dMinDistance = 1f;
    [Tooltip("3D音效是否跟随当前物体")]
    public bool Is3dSoundFollowOwner = true;

    #endregion

    #region 初始化
    private void Awake()
    {
        _selfTransform = transform;

        // 校验MusicManager（仅提示，不禁用组件）
        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("场景中未找到MusicManager单例！音效播放功能将不可用，但组件仍保留", this);
            _isMusicManagerValid = false;
            return;
        }
        _isMusicManagerValid = true;

        // 初始化缓存数组
        System.Array.Clear(_trackAudioSources, 0, _trackAudioSources.Length);
    }

    private void OnDestroy()
    {
        StopAllTrackSounds(); // 销毁时清理所有音效
    }
    #endregion

    #region 核心播放逻辑
    /// <summary>
    /// 播放2D音效（对外/动画事件调用）
    /// </summary>
    private void Play2DSound(AudioClip clip, int trackNumber, bool? isLoop = null, float? volumeScale = null)
    {
        // 基础校验（空值仅提示，不报错）
        if (!_isMusicManagerValid)
        {
            Debug.LogWarning($"轨道{trackNumber}：MusicManager未初始化，无法播放音效", this);
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning($"轨道{trackNumber}：音频文件未赋值，跳过播放", this);
            return;
        }

        // 转换轨道号为缓存索引（轨道1→0，轨道10→9）
        int cacheIndex = trackNumber - 1;
        if (cacheIndex < 0 || cacheIndex >= 10)
        {
            Debug.LogError($"轨道号错误！仅支持1-10，当前传入：{trackNumber}", this);
            return;
        }

        // 停止当前轨道已有音效
        StopSingleTrackSound(cacheIndex);

        // 配置参数
        bool loop = isLoop ?? DefaultIsLoop;
        float volScale = Mathf.Clamp01(volumeScale ?? DefaultVolumeScale);

        // 调用MusicManager播放（提前设置音量缩放）
        MusicManager.Instance.SetSpecificEffectVolume(clip.name, volScale);
        MusicManager.Instance.PlayEffect(clip, loop, (source) =>
        {
            if (source == null)
            {
                Debug.LogWarning($"轨道{trackNumber}：音效播放回调返回空AudioSource", this);
                return;
            }

            // 确保音量正确（叠加全局音量）
            source.volume = volScale * MusicManager.Instance.CurrentEffectGlobalVolume;
            _trackAudioSources[cacheIndex] = source;
        });
    }

    /// <summary>
    /// 播放3D音效
    /// </summary>
    private void Play3DSound(AudioClip clip, int trackNumber, float? max3dDistance = null, float? min3dDistance = null, bool? isLoop = null, float? volumeScale = null)
    {
        if (!_isMusicManagerValid)
        {
            Debug.LogWarning($"轨道{trackNumber}：MusicManager未初始化，无法播放音效", this);
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning($"轨道{trackNumber}：音频文件未赋值，跳过播放", this);
            return;
        }

        int cacheIndex = trackNumber - 1;
        if (cacheIndex < 0 || cacheIndex >= 10)
        {
            Debug.LogError($"轨道号错误！仅支持1-10，当前传入：{trackNumber}", this);
            return;
        }

        // 停止当前轨道已有音效
        StopSingleTrackSound(cacheIndex);

        // 配置参数
        bool loop = isLoop ?? DefaultIsLoop;
        float volScale = Mathf.Clamp01(volumeScale ?? DefaultVolumeScale);
        float maxDist = max3dDistance ?? Default3dMaxDistance;
        float minDist = min3dDistance ?? Default3dMinDistance;

        // 调用MusicManager播放3D音效（提前设置音量缩放）
        MusicManager.Instance.SetSpecificEffectVolume(clip.name, volScale);
        MusicManager.Instance.PlayEffect3D(
            clip,
            maxDist,
            minDist,
            Is3dSoundFollowOwner ? _selfTransform : null,
            loop,
            (source) =>
            {
                if (source == null)
                {
                    Debug.LogWarning($"轨道{trackNumber}：3D音效播放回调返回空AudioSource", this);
                    return;
                }

                source.volume = volScale * MusicManager.Instance.CurrentEffectGlobalVolume;
                _trackAudioSources[cacheIndex] = source;
            });
    }
    #endregion

    #region 轨道控制核心方法
    /// <summary>
    /// 暂停指定轨道的音效（轨道号1-10）
    /// </summary>
    private void PauseSingleTrackSound(int trackNumber)
    {
        if (!_isMusicManagerValid) return;

        int cacheIndex = trackNumber - 1;
        if (cacheIndex < 0 || cacheIndex >= 10)
        {
            Debug.LogError($"轨道号错误！仅支持1-10，当前传入：{trackNumber}", this);
            return;
        }

        if (_trackAudioSources[cacheIndex] != null && _trackAudioSources[cacheIndex].isPlaying)
        {
            _trackAudioSources[cacheIndex].Pause();
        }
        else
        {
            Debug.LogWarning($"轨道{trackNumber}无正在播放的音效", this);
        }
    }

    /// <summary>
    /// 停止指定缓存索引的音效（内部调用，0-9）
    /// </summary>
    private void StopSingleTrackSound(int cacheIndex)
    {
        if (cacheIndex < 0 || cacheIndex >= 10) return;

        if (_trackAudioSources[cacheIndex] != null)
        {
            if (_isMusicManagerValid)
            {
                MusicManager.Instance.StopEffect(_trackAudioSources[cacheIndex]);
            }
            else
            {
                // 无MusicManager时直接停止
                _trackAudioSources[cacheIndex].Stop();
            }
            _trackAudioSources[cacheIndex] = null;
        }
    }
    #endregion

    #region 2D音效播放接口（动画事件直接调用）
    public void PlaySound1(bool isLoop = false) => Play2DSound(SoundClip1, 1, isLoop);
    public void PlaySound2(bool isLoop = false) => Play2DSound(SoundClip2, 2, isLoop);
    public void PlaySound3(bool isLoop = false) => Play2DSound(SoundClip3, 3, isLoop);
    public void PlaySound4(bool isLoop = false) => Play2DSound(SoundClip4, 4, isLoop);
    public void PlaySound5(bool isLoop = false) => Play2DSound(SoundClip5, 5, isLoop);
    public void PlaySound6(bool isLoop = false) => Play2DSound(SoundClip6, 6, isLoop);
    public void PlaySound7(bool isLoop = false) => Play2DSound(SoundClip7, 7, isLoop);
    public void PlaySound8(bool isLoop = false) => Play2DSound(SoundClip8, 8, isLoop);
    public void PlaySound9(bool isLoop = false) => Play2DSound(SoundClip9, 9, isLoop);
    public void PlaySound10(bool isLoop = false) => Play2DSound(SoundClip10, 10, isLoop);

    // 带音量缩放的2D播放
    public void PlaySound1WithVolume(float volumeScale) => Play2DSound(SoundClip1, 1, DefaultIsLoop, volumeScale);
    public void PlaySound2WithVolume(float volumeScale) => Play2DSound(SoundClip2, 2, DefaultIsLoop, volumeScale);
    public void PlaySound3WithVolume(float volumeScale) => Play2DSound(SoundClip3, 3, DefaultIsLoop, volumeScale);
    public void PlaySound4WithVolume(float volumeScale) => Play2DSound(SoundClip4, 4, DefaultIsLoop, volumeScale);
    public void PlaySound5WithVolume(float volumeScale) => Play2DSound(SoundClip5, 5, DefaultIsLoop, volumeScale);
    public void PlaySound6WithVolume(float volumeScale) => Play2DSound(SoundClip6, 6, DefaultIsLoop, volumeScale);
    public void PlaySound7WithVolume(float volumeScale) => Play2DSound(SoundClip7, 7, DefaultIsLoop, volumeScale);
    public void PlaySound8WithVolume(float volumeScale) => Play2DSound(SoundClip8, 8, DefaultIsLoop, volumeScale);
    public void PlaySound9WithVolume(float volumeScale) => Play2DSound(SoundClip9, 9, DefaultIsLoop, volumeScale);
    public void PlaySound10WithVolume(float volumeScale) => Play2DSound(SoundClip10, 10, DefaultIsLoop, volumeScale);
    #endregion

    #region 3D音效播放接口
    public void PlaySound1_3D(bool isLoop = false) => Play3DSound(SoundClip1, 1, null, null, isLoop);
    public void PlaySound2_3D(bool isLoop = false) => Play3DSound(SoundClip2, 2, null, null, isLoop);
    public void PlaySound3_3D(bool isLoop = false) => Play3DSound(SoundClip3, 3, null, null, isLoop);
    public void PlaySound4_3D(bool isLoop = false) => Play3DSound(SoundClip4, 4, null, null, isLoop);
    public void PlaySound5_3D(bool isLoop = false) => Play3DSound(SoundClip5, 5, null, null, isLoop);
    public void PlaySound6_3D(bool isLoop = false) => Play3DSound(SoundClip6, 6, null, null, isLoop);
    public void PlaySound7_3D(bool isLoop = false) => Play3DSound(SoundClip7, 7, null, null, isLoop);
    public void PlaySound8_3D(bool isLoop = false) => Play3DSound(SoundClip8, 8, null, null, isLoop);
    public void PlaySound9_3D(bool isLoop = false) => Play3DSound(SoundClip9, 9, null, null, isLoop);
    public void PlaySound10_3D(bool isLoop = false) => Play3DSound(SoundClip10, 10, null, null, isLoop);

    // 自定义3D参数播放
    public void PlaySound1_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip1, 1, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound2_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip2, 2, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound3_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip3, 3, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound4_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip4, 4, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound5_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip5, 5, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound6_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip6, 6, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound7_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip7, 7, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound8_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip8, 8, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound9_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip9, 9, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    public void PlaySound10_3D(float max3dDistance, float min3dDistance, float volumeScale) => Play3DSound(SoundClip10, 10, max3dDistance, min3dDistance, DefaultIsLoop, volumeScale);
    #endregion

    #region 暂停/停止接口
    // 暂停接口
    public void PauseSound1() => PauseSingleTrackSound(1);
    public void PauseSound2() => PauseSingleTrackSound(2);
    public void PauseSound3() => PauseSingleTrackSound(3);
    public void PauseSound4() => PauseSingleTrackSound(4);
    public void PauseSound5() => PauseSingleTrackSound(5);
    public void PauseSound6() => PauseSingleTrackSound(6);
    public void PauseSound7() => PauseSingleTrackSound(7);
    public void PauseSound8() => PauseSingleTrackSound(8);
    public void PauseSound9() => PauseSingleTrackSound(9);
    public void PauseSound10() => PauseSingleTrackSound(10);

    // 停止接口
    public void StopSound1() => StopSingleTrackSound(0);  // 轨道1→缓存索引0
    public void StopSound2() => StopSingleTrackSound(1);  // 轨道2→缓存索引1
    public void StopSound3() => StopSingleTrackSound(2);
    public void StopSound4() => StopSingleTrackSound(3);
    public void StopSound5() => StopSingleTrackSound(4);
    public void StopSound6() => StopSingleTrackSound(5);
    public void StopSound7() => StopSingleTrackSound(6);
    public void StopSound8() => StopSingleTrackSound(7);
    public void StopSound9() => StopSingleTrackSound(8);
    public void StopSound10() => StopSingleTrackSound(9); // 轨道10→缓存索引9
    #endregion

    #region 批量控制
    /// <summary>
    /// 暂停所有轨道音效
    /// </summary>
    public void PauseAllTrackSounds()
    {
        if (!_isMusicManagerValid) return;

        for (int i = 1; i <= 10; i++)
        {
            PauseSingleTrackSound(i);
        }
    }

    /// <summary>
    /// 停止所有轨道音效
    /// </summary>
    public void StopAllTrackSounds()
    {
        if (!_isMusicManagerValid) return;

        for (int i = 0; i < 10; i++)
        {
            StopSingleTrackSound(i);
        }
        System.Array.Clear(_trackAudioSources, 0, _trackAudioSources.Length);
    }

    /// <summary>
    /// 恢复所有暂停的音效
    /// </summary>
    public void ResumeAllTrackSounds()
    {
        if (!_isMusicManagerValid) return;

        for (int i = 0; i < 10; i++)
        {
            if (_trackAudioSources[i] != null && !_trackAudioSources[i].isPlaying)
            {
                _trackAudioSources[i].UnPause();
            }
        }
    }
    #endregion
}