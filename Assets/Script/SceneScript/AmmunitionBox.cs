using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmunitionBox : BaseSceneInteract
{
    [Header("弹药箱设置")]
    public float CollTime = 40f; // 冷却时间
    private float _currentRemainTime; // 当前剩余时间
    public bool _isCooling=false; // 是否正在冷却

    [Header("世界UI提示")]
    public CanvasGroup IconImageCanvas; // 图标画布组
    public Image FillImage; // 冷却填充图片

    private Sequence _iconAnimaSequence; // UI动画序列
    private bool _isInShow; // UI是否正在显示
    private RectTransform _uiRect; // UI的RectTransform缓存

    public TextMeshProUGUI PromptImage;

    public override void Awake()
    {
        base.Awake();
        if (IconImageCanvas != null)
            _uiRect = IconImageCanvas.GetComponent<RectTransform>();
    }

 

    public override void triggerEnterRange()
    {
        // 玩家进入范围：显示UI
        _isInShow = true;
        PlayEnterIconAnima();
    }

    public override void triggerExitRange()
    {
        // 玩家离开范围：隐藏UI
        _isInShow = false;
        PlayExitIconAnima();
    }



    // 播放进入动画
    private void PlayEnterIconAnima()
    {
        if (IconImageCanvas == null || _uiRect == null) 
            return;

        // 杀死之前的动画，防止冲突
        _uiRect.DOKill();
        IconImageCanvas.DOKill();
        _iconAnimaSequence?.Kill();

        // 初始化状态
        IconImageCanvas.alpha = 0;
        _uiRect.anchoredPosition = new Vector2(0, 0);

        // 创建动画序列
        _iconAnimaSequence = DOTween.Sequence();
        // 淡入
        _iconAnimaSequence.Append(IconImageCanvas.DOFade(1, 0.3f));
        // 同时向上移动
        _iconAnimaSequence.Join(_uiRect.DOAnchorPosY(0.34f, 0.5f).SetEase(Ease.OutBack));
    }

    // 播放退出动画
    private void PlayExitIconAnima()
    {
        if (IconImageCanvas == null || _uiRect == null) return;

        _uiRect.DOKill();
        IconImageCanvas.DOKill();
        _iconAnimaSequence?.Kill();

        // 创建退出动画序列
        _iconAnimaSequence = DOTween.Sequence();
        // 淡出
        _iconAnimaSequence.Append(IconImageCanvas.DOFade(0, 0.3f));
        // 同时向下移动复位
        _iconAnimaSequence.Join(_uiRect.DOAnchorPosY(0, 0.3f).SetEase(Ease.InQuad));
        // 动画结束后重置位置
        _iconAnimaSequence.OnComplete(() =>
        {
            if (_uiRect != null)
                _uiRect.anchoredPosition = new Vector2(0, 0);
        });
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        _uiRect?.DOKill();
        IconImageCanvas?.DOKill();
        _iconAnimaSequence?.Kill();
    }

    public override void TriggerEffect()
    {
        if (_isCooling || Player.LocalPlayer.currentGun == null)//枪械为空直接返回
            return;

        _isCooling = true;
        _currentRemainTime = CollTime;
        PromptImage.text = "正在冷却";
        Debug.Log("弹药已补满！");
        //调用弹药补充
        Player.LocalPlayer.CmdBulletSupplement();//补充弹药
        MusicManager.Instance.PlayEffect("Music/正式/交互/补充子弹",1f);
    }

    public override void Update()
    {
        base.Update();
        // 冷却计时逻辑
        if (_isCooling)
        {
            _currentRemainTime -= Time.deltaTime;
            if (_currentRemainTime <= 0)
            {
                _currentRemainTime = 0;
                _isCooling = false;
                PromptImage.text = "补充弹药";
            }
        }

        // 更新UI填充进度
        if (_isInShow && FillImage != null)
        {
            FillImage.fillAmount = _currentRemainTime / CollTime;
        }
    }
}