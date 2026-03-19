using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWordUI : MonoBehaviour//玩家世界UI控制
{
    public float ReturnTime = 3f;//长时间不受伤害就自主消失
    public Player MyPlayer;//我的玩家 
    public bool IsInShow = false;//是否处于显示状态
    [Header("UI关联")]
    public TextMeshProUGUI PlayerName;//玩家名字
    public Image HealthFillImage;//血量填充图片
    public CanvasGroup MyCanvasGroup;
    private Sequence MyAnima;

    private int CountDownTaskID = -1;

    private void Start()
    {
    }

    public void ShowInfo()
    {
        if (MyPlayer == null || MyPlayer.myStats == null) return;

        IsInShow = true;
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(MyCanvasGroup, ref MyAnima, true, () => { });
        ResetTask();//启动任务
        UpdateInfo(); // 显示UI时立即更新血条
    }

    public void ResetTask()//每次受伤就自动调用任务重启
    {
        if (!IsInShow)//不处于显示就退出
            return;

        if (CountDownTaskID != -1)
        {
            CountDownManager.Instance.StopTimer(CountDownTaskID);
            CountDownTaskID = -1; // 重置ID
        }

        CountDownTaskID = CountDownManager.Instance.CreateTimer(false, (int)(ReturnTime * 1000), () =>
        {
            IsInShow = false;
            HideUI();//关闭UI
        });
    }

    public void HideUI()
    {
        SimpleAnimatorTool.Instance.CommonFadeDefaultAnima(MyCanvasGroup, ref MyAnima, false, () => { });
    }

    public void UpdateInfo()
    {
        if (MyPlayer == null || MyPlayer.myStats == null || HealthFillImage == null || PlayerName == null)
        {
            Debug.LogWarning("血条更新失败：关键组件未初始化！", this);
            return;
        }

        // 更新信息
        PlayerName.text = MyPlayer.PlayerName;

        float healthRatio = MyPlayer.myStats.maxHealth == 0
            ? 0
            : MyPlayer.myStats.CurrentHealth / MyPlayer.myStats.maxHealth;

        // 确保数值在0~1之间
        healthRatio = Mathf.Clamp01(healthRatio);

        // 播放动画更新血条
        HealthFillImage.DOFillAmount(healthRatio, 1f);
    }

    private void OnDestroy()
    {
        // 销毁的时候取消任务和动画
        if (HealthFillImage != null)
            HealthFillImage.DOKill();

        if (MyAnima != null)
            MyAnima.Kill();

        if (CountDownTaskID != -1)
            CountDownManager.Instance.StopTimer(CountDownTaskID);
    }
}