using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantArea : MonoBehaviour
{
    public float CoolTime = 10;//对话触发冷却时间
    private bool IsInCoolTime = false;

    [Header("商人台词")]
    public List<string> lines = new List<string>
    {
        "伙计!好久不见",
        "要不要来点什么.",
        "今日打折了很多商品"
    };

    private Coroutine coolDownCoroutine; // 冷却协程

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 只触发玩家（可根据你的项目修改标签）
        if (collision.CompareTag("Player") && !IsInCoolTime)
        {
            PlayMerchantTalk();
        }
    }

    /// <summary>
    /// 播放商人对话 + 开启冷却
    /// </summary>
    public void PlayMerchantTalk()
    {
        if ( lines == null || lines.Count == 0) return;

        MerchantPeople.instance.PlayMultiLine(lines);

        IsInCoolTime = true;

        if (coolDownCoroutine != null)
            StopCoroutine(coolDownCoroutine);

        coolDownCoroutine = StartCoroutine(CoolDownCoroutine());
    }

    /// <summary>
    /// 冷却协程
    /// </summary>
    private IEnumerator CoolDownCoroutine()
    {
        yield return new WaitForSeconds(CoolTime);

        // 冷却结束
        IsInCoolTime = false;
    }

    /// <summary>
    /// 外部强制重置冷却
    /// </summary>
    public void ResetCoolDown()
    {
        IsInCoolTime = false;
        if (coolDownCoroutine != null)
            StopCoroutine(coolDownCoroutine);
    }
}