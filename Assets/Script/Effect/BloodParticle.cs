using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodParticle : MonoBehaviour
{
    public Sprite[] sprites;
    public Color startColor;
    public Color endColor;

    public Vector2 velocity;
    public float existTime = 0.75f;

    SpriteRenderer renderer;
    float t = 0;

    private readonly Vector2 GRAVITY = new Vector2(0, 5f);
    private readonly float RAY_MULTIPLIER = 1.5f;
    private readonly float RAD_TO_DEG = 180f / Mathf.PI;

    private LayerMask collisionLayerMask;

    private const string LAYER_BACKGROUND = "BackGround";
    private const string LAYER_WALL = "Wall";
    private const string LAYER_GROUND = "Ground";

    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        collisionLayerMask = LayerMask.GetMask(LAYER_BACKGROUND, LAYER_WALL, LAYER_GROUND);
    }

    // Update is called once per frame
    void Update()
    {
        t += Time.deltaTime;

        //根据t选择对应的图片
        int spriteIndex = Mathf.Clamp((int)(t * sprites.Length / existTime), 0, sprites.Length - 1);
        renderer.sprite = sprites[spriteIndex];
        //根据t选择对应的颜色
        Color color = Color.Lerp(startColor, endColor, Mathf.Clamp01(t / existTime));
        renderer.color = color;

        //模拟重力，保持粒子始终朝向运动方向
        velocity -= GRAVITY * Time.deltaTime;
        float angle = Mathf.Atan2(velocity.y, velocity.x);
        transform.position += (Vector3)velocity * Time.deltaTime;
        // 使用缓存的角度转换常量，无重复计算
        transform.rotation = Quaternion.Euler(0, 0, angle * RAD_TO_DEG);

        //用射线检测是否碰撞到地形
        // 使用缓存的层掩码+常量，零GC开销
        RaycastHit2D raycastHit = Physics2D.Raycast(transform.position, velocity.normalized,
            velocity.magnitude * RAY_MULTIPLIER * Time.deltaTime, collisionLayerMask);

        if (raycastHit)
        {
            BloodParticleGenerator.Instance.GenerateBloodOnWall(raycastHit.point, raycastHit.normal);
            Destroy(gameObject);
        }

        //超时则自毁
        if (t >= existTime + 0.2f)
        {
            Destroy(gameObject);
        }
    }
}