using UnityEngine;
using System.Collections;
using DG.Tweening;

public class BloodParticleGenerator : Singleton<BloodParticleGenerator>
{
    [Header("血液预制体")]
    public GameObject bloodOnBackground;
    public GameObject bloodOnWall;
    public GameObject bloodParticle;

    [Header("血液精灵图集")]
    public Sprite[] bloodsOnBackground;
    public Sprite[] bloodsOnWall;

    // 时间配置
    private const float TOTAL_RECYCLE_TIME = 4f;
    private const float FADE_DELAY = 3f;
    private const float FADE_DURATION = 1f;

    protected override void Awake()
    {
        base.Awake();
        transform.parent = null;
    }

    public void GenerateBloodOnBackground(Vector3 position)
    {
        if (bloodOnBackground == null) return;

        // 随机参数
        position += new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.5f, 0.5f), 0) * 2.5f;
        float angle = Random.Range(-20f, 20f);
        Vector2 size = new Vector2(Random.Range(0.8f, 1.2f), Random.Range(0.8f, 1.2f));

        // 从对象池获取
        GameObject blood = PoolManage.Instance.GetObj(bloodOnBackground);
        blood.transform.position = position + new Vector3(0, 0, -0.6f);
        blood.transform.rotation = Quaternion.Euler(0, 0, angle);
        blood.transform.localScale = size;
        blood.transform.SetParent(transform);

        // 设置精灵并重置透明度
        SpriteRenderer sr = blood.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (bloodsOnBackground.Length > 0)
                sr.sprite = bloodsOnBackground[Random.Range(0, bloodsOnBackground.Length)];
            ResetAlpha(sr);
        }

        // 启动带淡出的自动回收
        StartCoroutine(RecycleWithFade(blood, bloodOnBackground, sr));
    }

    public void GenerateBloodOnWall(Vector3 position, Vector2 normal)
    {
        if (bloodOnWall == null) return;

        // 计算角度
        float angle = Mathf.Atan2(normal.y, normal.x) * 180 / Mathf.PI - 90;

        // 从对象池获取
        GameObject blood = PoolManage.Instance.GetObj(bloodOnWall);
        blood.transform.position = position + new Vector3(0, 0, -0.6f);
        blood.transform.rotation = Quaternion.Euler(0, 0, angle);
        blood.transform.SetParent(transform);

        // 设置精灵并重置透明度
        SpriteRenderer sr = blood.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (bloodsOnWall.Length > 0)
                sr.sprite = bloodsOnWall[Random.Range(0, bloodsOnWall.Length)];
            ResetAlpha(sr);
        }

        // 启动带淡出的自动回收
        StartCoroutine(RecycleWithFade(blood, bloodOnWall, sr));
    }

    public void GenerateBloodParticle(Vector3 position, Vector2 velocity)
    {
        if (bloodParticle == null) return;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * 180 / Mathf.PI;

        // 从对象池获取
        GameObject blood = PoolManage.Instance.GetObj(bloodParticle);
        blood.transform.position = position;
        blood.transform.rotation = Quaternion.Euler(0, 0, angle);
        blood.transform.SetParent(transform);

        // 初始化粒子逻辑并重置透明度
        SpriteRenderer sr = blood.GetComponent<SpriteRenderer>();
        BloodParticle particle = blood.GetComponent<BloodParticle>();
        if (particle != null)
        {
            particle.velocity = velocity;
        }
        if (sr != null)
        {
            ResetAlpha(sr);
        }

        // 启动带淡出的自动回收
        StartCoroutine(RecycleWithFade(blood, bloodParticle, sr));
    }

    // 重置透明度为1
    private void ResetAlpha(SpriteRenderer sr)
    {
        if (sr != null)
        {
            Color color = sr.color;
            color.a = 1f;
            sr.color = color;
        }
    }

    // 协程：等待3秒 → 1秒淡出 → 回收
    private IEnumerator RecycleWithFade(GameObject obj, GameObject prefab, SpriteRenderer sr)
    {
        // 前3秒保持不动
        yield return new WaitForSeconds(FADE_DELAY);

        if (obj == null || sr == null) 
            yield break;

        // 启动1秒透明度渐变动画
        Tween fadeTween = sr.DOFade(0f, FADE_DURATION).SetEase(Ease.Linear);

        // 等待动画完成
        yield return fadeTween.WaitForCompletion();

        // 回收对象
        if (obj != null && prefab != null)
        {
            PoolManage.Instance.PushObj(prefab, obj);
        }
    }
}