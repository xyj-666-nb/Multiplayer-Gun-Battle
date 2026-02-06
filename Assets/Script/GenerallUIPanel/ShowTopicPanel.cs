using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class ShowTopicPanel : BasePanel
{
    public RectTransform ShowArea;//显示区域

    public void SetDurationTime(float Time)
    {
        CountDownManager.Instance.CreateTimer(false, (int)(Time * 1000), () => { UImanager.Instance.HidePanel<ShowTopicPanel>(); });
    }
  
    #region 生命周期
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    #region 控件处理
    public override void ClickButton(string controlName)
    {
        base.ClickButton(controlName);
    }
    #endregion

    #region 面板显隐
    public override void HideMe(UnityAction callback, bool isNeedDefaultAnimator = true)
    {
        ShowArea.DOAnchorPos(new Vector2(0, ShowArea.rect.height), 1f);
        base.HideMe(callback, isNeedDefaultAnimator);
    }

    public override void ShowMe(bool isNeedDefaultAnimator = true)
    {
        ShowArea.anchoredPosition = new Vector2(0,ShowArea.rect.height);
        ShowArea.DOAnchorPos(Vector2.zero, 1f);
        base.ShowMe(isNeedDefaultAnimator);
    }
    #endregion

    #region 特殊动画
    protected override void SpecialAnimator_Hide()
    {
    }

    protected override void SpecialAnimator_Show()
    {

    }
    #endregion
}
