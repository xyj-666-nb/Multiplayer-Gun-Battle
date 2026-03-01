using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class TeamBornPosList
{
    public List<Transform> RedTeamBornPosList;//红队出生位置列表
    public List<Transform> BlueTeamBornPosList;//蓝队出生位置列表
}

public class MapManager : MonoBehaviour
{
    public TeamBornPosList teamBornPosList = new TeamBornPosList();

    public GameObject helicopter;//直升机
    [Header("入场动画")]
    public PlayableDirector TimeLine;//红队入场动画

    public CinemachineVirtualCamera helicopterVC;//直升机虚拟相机

    [Header("直升机震动配置（仅作用于当前相机）")]
    [Tooltip("震动强度（0-2为宜，根据效果调整）")]
    public float shakeAmplitude = 0.6f;
    [Tooltip("震动频率（控制震动快慢，1-5为宜）")]
    public float shakeFrequency = 2.5f;
    [Tooltip("震动平滑过渡速度（启停的顺滑度）")]
    public float shakeSmoothSpeed = 5f;

    // 内部震动状态变量
    private CinemachineBasicMultiChannelPerlin _helicopterNoise; // 相机噪声组件
    private float _targetAmplitude; // 目标震动强度
    private bool _isShaking; // 是否正在震动

    private void Awake()
    {
        // 初始化：获取/添加相机的噪声组件（核心）
        if (helicopterVC != null)
        {
            // 获取噪声组件，没有则自动添加
            _helicopterNoise = helicopterVC.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (_helicopterNoise == null)
            {
                _helicopterNoise = helicopterVC.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
            // 初始化震动参数（默认关闭震动）
            _helicopterNoise.m_AmplitudeGain = 0f;
            _helicopterNoise.m_FrequencyGain = shakeFrequency;
            _targetAmplitude = 0f;
        }
        else
        {
            Debug.LogError("请为 MapManager 赋值 helicopterVC（直升机虚拟相机）！");
        }
    }

    private void Update()
    {
        // 平滑过渡震动强度（避免震动启停太突兀）
        if (_helicopterNoise != null && Mathf.Abs(_helicopterNoise.m_AmplitudeGain - _targetAmplitude) > 0.01f)
        {
            _helicopterNoise.m_AmplitudeGain = Mathf.Lerp(
                _helicopterNoise.m_AmplitudeGain,
                _targetAmplitude,
                Time.deltaTime * shakeSmoothSpeed
            );
        }
    }

    public void TriggerAnima()
    {
        TimeLine.Play();//触发动画
    }

    public void AnimaEnd()
    {
        //动画结束
        UImanager.Instance.GetPanel<PlayerPanel>().SimpleShowPanel();
        UImanager.Instance.ShowPanel<GameScorePanel>();//打开比分面板
        PlayerAndGameInfoManger.Instance.EquipCurrentSlot();//获取战备
        // 动画结束可顺便停止震动（可选）
        StopShack();
    }

    // 启动直升机相机震动（仅作用于helicopterVC）
    public void StartShack()
    {
        if (helicopterVC == null || _helicopterNoise == null)
        {
            Debug.LogWarning("直升机相机未初始化，无法启动震动！");
            return;
        }

        _isShaking = true;
        _targetAmplitude = shakeAmplitude; // 设置目标震动强度
        _helicopterNoise.m_FrequencyGain = shakeFrequency; // 确保频率生效
        Debug.Log("直升机相机震动已启动");
    }

    // 停止直升机相机震动
    public void StopShack()
    {
        if (_helicopterNoise == null) 
            return;

        _isShaking = false;
        _targetAmplitude = 0f; // 目标强度置0，Update中会平滑过渡
        Debug.Log("直升机相机震动已停止");
    }

    public void SethelicopterActive()
    {
        helicopter.SetActive(false);//设置直升机失活
        // 隐藏直升机时强制停止震动
        StopShack();
    }

    // 可选：快速强制停止震动（无平滑过渡）
    public void StopShackImmediate()
    {
        if (_helicopterNoise != null)
        {
            _helicopterNoise.m_AmplitudeGain = 0f;
            _targetAmplitude = 0f;
            _isShaking = false;
        }
    }
}