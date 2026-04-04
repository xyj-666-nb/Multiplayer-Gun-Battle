using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

//玩家表情系统
public class playerWorldExpressionSystem : NetworkBehaviour
{
    private Sequence _expressionSequence;
    public CanvasGroup MyCanvasGroup;
    private ExpressionPack expressionPack;
    private RectTransform MyRectTransform;
    public Image ExpressionImage;
    public RectTransform ParentRect;//父对象的RectTransform，用于计算位置等

    [Header("动画参数设置")]
    public float ShowWidth = 200f;
    public float ShowHeight = 150f;
    public float ShowTime = 1f;
    public float DefaultHideTime = 0.5f;
    public float DefaultDuration = 2f; // 表情显示持续时间
    public Ease ShowEase = Ease.OutBack;
    public Ease HideEase = Ease.InQuad;

    private Vector2 _originalSizeDelta;
    private bool IsInShow = false;

    private void Awake()
    {
        MyRectTransform = GetComponent<RectTransform>();
        _originalSizeDelta = MyRectTransform.sizeDelta;
    }

    /// <summary>
    /// 设置父对象的翻转
    /// </summary>
    /// <param name="Dir">传入1或者-1</param>
    public void SetParentRectDir(int Dir)//设置父对象的翻转
    {
        ParentRect.localScale = new Vector2( Dir, ParentRect.localScale.y);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    [ClientRpc]
    public void RpcPlayExpression(int ExpressionID)
    {
        expressionPack = ExpressionSystem.Instance.GetExpressionPack(ExpressionID);
        if (expressionPack != null)
        {
            ShowExpression();
        }
        else
        {
            Debug.LogWarning($"未找到ID为 {ExpressionID} 的表情");
        }
    }

    [Command]
    public void CmdPlayExpression(int ExpressionID)
    {
        RpcPlayExpression(ExpressionID);
    }

    #region 动画管理
    public void ShowExpression()
    {
        // 如果正在显示，先快速隐藏
        if (IsInShow)
        {
            QuickHideExpression();
        }

        IsInShow = true;
        ExpressionImage.sprite = expressionPack.ExpressionSprite;

        _expressionSequence?.Kill();

        _expressionSequence = DOTween.Sequence();

        _expressionSequence.Join(MyCanvasGroup.DOFade(1f, ShowTime).SetEase(ShowEase));
        _expressionSequence.Join(MyRectTransform.DOSizeDelta(new Vector2(ShowWidth, ShowHeight), ShowTime).SetEase(ShowEase));

        _expressionSequence.AppendInterval(DefaultDuration);

        _expressionSequence.AppendCallback(() => {
            CommonHideExpression();
        });

        Debug.Log("表情显示动画开始");
    }

    public void CommonHideExpression()
    {
        // 先杀掉之前的动画
        _expressionSequence?.Kill();

        // 创建隐藏动画 Sequence
        _expressionSequence = DOTween.Sequence();

        // 同时播放隐藏动画
        _expressionSequence.Join(MyCanvasGroup.DOFade(0f, DefaultHideTime).SetEase(HideEase));
        _expressionSequence.Join(MyRectTransform.DOSizeDelta(_originalSizeDelta, DefaultHideTime).SetEase(HideEase));

        // 隐藏动画完成后，设置状态
        _expressionSequence.OnComplete(() => {
            IsInShow = false;
            Debug.Log("表情普通隐藏动画完成");
        });
    }

    public void QuickHideExpression()
    {
        _expressionSequence?.Kill();

        MyCanvasGroup.alpha = 0f;
        MyRectTransform.sizeDelta = _originalSizeDelta;
        IsInShow = false;
        Debug.Log("表情快速隐藏完成（直接设置值）");
    }
    #endregion

    private void OnDestroy()
    {
        _expressionSequence?.Kill();
    }
}