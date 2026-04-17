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

    private readonly Vector3 BLOOD_Z_OFFSET = new Vector3(0, 0, -0.6f);
    private readonly float BACKGROUND_OFFSET_SCALE = 2.5f;

    private const float RAD_TO_DEG = 180f / Mathf.PI;

    private WaitForSeconds _fadeDelayWait;

    protected override void Awake()
    {
        base.Awake();
        transform.parent = null;
        _fadeDelayWait = new WaitForSeconds(FADE_DELAY);
    }

    public void GenerateBloodOnBackground(Vector3 position)
    {
        if (bloodOnBackground == null) return;

        position += new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.5f, 0.5f), 0) * BACKGROUND_OFFSET_SCALE;
        float angle = Random.Range(-20f, 20f);
        Vector2 size = new Vector2(Random.Range(0.8f, 1.2f), Random.Range(0.8f, 1.2f));

        GameObject blood = PoolManage.Instance.GetObj(bloodOnBackground);
        blood.transform.position = position + BLOOD_Z_OFFSET;
        blood.transform.rotation = Quaternion.Euler(0, 0, angle);
        blood.transform.localScale = size;
        blood.transform.SetParent(transform);

        SpriteRenderer sr = blood.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (bloodsOnBackground.Length > 0)
                sr.sprite = bloodsOnBackground[Random.Range(0, bloodsOnBackground.Length)];
            ResetAlpha(sr);
        }

        StartCoroutine(RecycleWithFade(blood, bloodOnBackground, sr));
    }

    public void GenerateBloodOnWall(Vector3 position, Vector2 normal)
    {
        if (bloodOnWall == null) return;

        float angle = Mathf.Atan2(normal.y, normal.x) * RAD_TO_DEG - 90;

        GameObject blood = PoolManage.Instance.GetObj(bloodOnWall);
        blood.transform.position = position + BLOOD_Z_OFFSET;
        blood.transform.rotation = Quaternion.Euler(0, 0, angle);
        blood.transform.SetParent(transform);

        SpriteRenderer sr = blood.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (bloodsOnWall.Length > 0)
                sr.sprite = bloodsOnWall[Random.Range(0, bloodsOnWall.Length)];
            ResetAlpha(sr);
        }

        StartCoroutine(RecycleWithFade(blood, bloodOnWall, sr));
    }

    public void GenerateBloodParticle(Vector3 position, Vector2 velocity)
    {
        if (bloodParticle == null) return;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * RAD_TO_DEG;

        GameObject blood = PoolManage.Instance.GetObj(bloodParticle);
        blood.transform.position = position;
        blood.transform.rotation = Quaternion.Euler(0, 0, angle);
        blood.transform.SetParent(transform);

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

    // 协程回收
    private IEnumerator RecycleWithFade(GameObject obj, GameObject prefab, SpriteRenderer sr)
    {
        yield return _fadeDelayWait;

        if (obj == null || sr == null)
            yield break;

        Tween fadeTween = sr.DOFade(0f, FADE_DURATION).SetEase(Ease.Linear);
        yield return fadeTween.WaitForCompletion();

        if (obj != null && prefab != null)
        {
            PoolManage.Instance.PushObj(prefab, obj);
        }
    }
}