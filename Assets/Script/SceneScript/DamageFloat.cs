using UnityEngine;
using DG.Tweening;

public class DamageFloat : MonoBehaviour
{
    [Header("预制体")]
    public GameObject prefabs;

    [Header("关键组件")]
    public TextMesh textMesh;
    public Rigidbody2D rig2D;

    [Header("动画配置")]
    public float showDuration = 0.5f;
    public Vector3 startScale = new Vector3(0.8f, 0.8f, 0.8f);
    public Vector3 targetScale = Vector3.one;

    [Header("物理配置")]
    public float upForce = 4f;
    public float horizontalForceRange = 2f;
    public bool useImpulse = true;

    private Color currentTextColor;

    private float MaxDamage = 100;
    private int MaxSize = 30;
    private int MinSize = 10;

    private void Awake()
    {
        if (rig2D == null) 
            rig2D = GetComponent<Rigidbody2D>();
        if (textMesh == null)
            textMesh = GetComponent<TextMesh>();
        if (textMesh != null)
            currentTextColor = textMesh.color;
    }

    public void Init(float damage, Transform pos)
    {

        textMesh.text = Mathf.CeilToInt(damage).ToString(); // 伤害数值向上取整显示
        transform.position = pos.position;


        transform.localScale = startScale;
        currentTextColor.a = 0;
        textMesh.color = currentTextColor;

        SetColorByDamage(damage);

        float t = Mathf.Clamp01(damage / MaxDamage);
        textMesh.fontSize = Mathf.RoundToInt(Mathf.Lerp(MinSize, MaxSize, t));

        AddRandomUpwardForce();

        Sequence showSequence = DOTween.Sequence();

        showSequence.Append(transform.DOScale(targetScale, showDuration).SetEase(Ease.OutCubic));

        // 淡入
        showSequence.Join(DOTween.To(
            () => currentTextColor.a,
            alpha => {
                currentTextColor.a = alpha;
                textMesh.color = currentTextColor;
            },
            1f,
            showDuration)
            .SetEase(Ease.OutCubic));

        // 延迟一会儿再淡出 
        showSequence.AppendInterval(0.3f);

        // 淡出并回收
        showSequence.Append(DOTween.To(
            () => currentTextColor.a,
            alpha => {
                currentTextColor.a = alpha;
                textMesh.color = currentTextColor;
            },
            0f,
            0.2f)
            .SetEase(Ease.Linear));

        showSequence.OnComplete(() =>
        {
            PoolManage.Instance.PushObj(prefabs, this.gameObject);
        });

        showSequence.Play();
    }

    /// <summary>
    /// 根据伤害值计算颜色
    /// </summary>
    private void SetColorByDamage(float damage)
    {
        float percentage = Mathf.Clamp01(damage / MaxDamage);

        Color damageColor = Color.Lerp(Color.yellow, Color.red, percentage);

        currentTextColor.r = damageColor.r;
        currentTextColor.g = damageColor.g;
        currentTextColor.b = damageColor.b;

        textMesh.color = currentTextColor;
    }

    private void AddRandomUpwardForce()
    {
        if (rig2D == null) return;

        rig2D.velocity = Vector2.zero;
        rig2D.angularVelocity = 0;

        float randomHorizontal = Random.Range(-horizontalForceRange, horizontalForceRange);
        Vector2 force = new Vector2(randomHorizontal, upForce);

        ForceMode2D forceMode = useImpulse ? ForceMode2D.Impulse : ForceMode2D.Force;
        rig2D.AddForce(force, forceMode);
    }
}