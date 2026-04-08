using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 枪械视角类型
/// </summary>
public enum GunViewType
{
    Normal,         // 常规视角/默认全景
    GunSkin,        // 枪械皮肤视角
    HitParticle,    // 打击粒子视角
    BulletConfig    // 子弹配置视角
}

/// <summary>
/// 视角配置参数
/// </summary>
[System.Serializable]
public class ViewSettings
{
    [Header("对应视角")]
    public GunViewType viewType;
    [Header("相对于视口中心的偏移像素")]
    public Vector2 centerOffset;
    [Header("目标缩放倍数")]
    public float targetScale = 1.5f;
    [Header("目标旋转值（Z轴角度）")]
    public float targetRotation = 0f;

    [Header("过渡动画时长")]
    public float duration = 0.3f;

    [Header("动画曲线 | OutQuad = 先快后慢")]
    public Ease moveEase = Ease.OutQuad; 
}

public class UIGunViewZoom : MonoBehaviour
{
    [Header("绑定你的视口")]
    public RectTransform viewport;

    [Header("视角配置列表")]
    public List<ViewSettings> viewSettingsList = new List<ViewSettings>();

    private RectTransform _targetRect;
    private Vector2 _defaultPos;
    private Vector3 _defaultScale;
    private Quaternion _defaultRot;

    private void Awake()
    {
        _targetRect = GetComponent<RectTransform>();
        _defaultPos = _targetRect.anchoredPosition;
        _defaultScale = _targetRect.localScale;
        _defaultRot = _targetRect.rotation;
    }

    public void ChangeView(GunViewType targetView)
    {
        _targetRect.DOKill();

        if (targetView == GunViewType.Normal)
        {
            float defaultDuration = 0.3f;
            Ease defaultEase = Ease.OutQuad;

            _targetRect.DOAnchorPos(_defaultPos, defaultDuration).SetEase(defaultEase);
            _targetRect.DOScale(_defaultScale, defaultDuration).SetEase(defaultEase);
            _targetRect.DORotateQuaternion(_defaultRot, defaultDuration).SetEase(defaultEase);
            return;
        }

        ViewSettings setting = viewSettingsList.Find(i => i.viewType == targetView);
        if (setting == null)
        {
            Debug.LogError($"未配置 {targetView} 视角参数！");
            return;
        }

        Vector2 targetWorldPos = (Vector2)viewport.position + setting.centerOffset;
        Vector3 finalScale = _defaultScale * setting.targetScale;

        _targetRect.DOMove(targetWorldPos, setting.duration).SetEase(setting.moveEase);
        _targetRect.DOScale(finalScale, setting.duration).SetEase(setting.moveEase);
        _targetRect.DORotate(new Vector3(0, 0, setting.targetRotation), setting.duration).SetEase(setting.moveEase);
    }

    public void ResetToNormalView()
    {
        ChangeView(GunViewType.Normal);
    }
}