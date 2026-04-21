using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameSkinInteract : BaseSceneInteract
{
    [Header("交互防护配置")]
    [Tooltip("是否正在触发中（防止短时间重复触发）")]
    public bool IsTrigger = false;
    [Tooltip("是否在冷却时间中")]
    public bool IsInCooldown = false;
    [Tooltip("冷却时间（秒）")]
    public float CoolTime = 2f; // 皮肤界面交互可以稍微短一点，默认2秒

    [Header("UI提示")]
    [Tooltip("交互提示文本")]
    public TextMeshProUGUI PromptName;
    [Tooltip("冷却中显示的文本")]
    public string CoolingText = "正在冷却";
    [Tooltip("可交互时显示的文本")]
    public string ReadyText = "打开衣柜";

    public override void TriggerEffect()
    {
        // 如果正在触发中 或 正在冷却中，直接返回
        if (IsTrigger || IsInCooldown)
            return;

        // 开启冷却倒计时
        CountDownManager.Instance.CreateTimer(false, (int)(CoolTime * 1000), () =>
        {
            IsInCooldown = false;
            if (PromptName != null)
            {
                PromptName.text = ReadyText;
            }
        });

        //  更新UI状态
        if (PromptName != null)
        {
            PromptName.text = CoolingText;
        }
        IsInCooldown = true; // 设置冷却标记
        IsTrigger = true; // 设置触发中标记

        //短时间后重置触发标记
        CountDownManager.Instance.CreateTimer(false, 500, () =>
        {
            IsTrigger = false;
        });

        //核心逻辑：显示皮肤界面
        UImanager.Instance.ShowPanel<CostumePanel>();
        Debug.Log("[皮肤交互] 已打开衣柜界面");
    }

    public override void triggerEnterRange()
    {
        // 进入范围时，如果不在冷却，更新提示文本为可交互状态
        if (!IsInCooldown && PromptName != null)
        {
            PromptName.text = ReadyText;
        }
    }

    public override void triggerExitRange()
    {
        // 可以在这里做一些退出范围的清理，比如隐藏提示
    }
}