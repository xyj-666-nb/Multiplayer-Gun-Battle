using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodExample : MonoBehaviour
{
    int tick = 0;                  // 计时器变量，用于固定时间间隔触发血粒子
    bool start = false;
    Rigidbody2D rigidbody;

    Vector3 ChestPosition => transform.position + new Vector3(0, 0.3f, 0);

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !start)
        {
            start = true;
            Vector2 velocity = ((Vector2)(Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position)).normalized * 5;
            rigidbody.velocity = velocity;
            for (int i = 0; i < 3; i++)
                BloodParticleGenerator.Instance.GenerateBloodOnBackground(ChestPosition + new Vector3(0, 0, 1));
        }

    }

    private void FixedUpdate()
    {
        if (start)
        {
            tick++;

            if (tick % 3 == 0 && tick < 50)
            // 每3个固定步（约0.06s）触发一次，且tick<50（限制触发次数）
            {
                BloodParticleGenerator.Instance.GenerateBloodParticle(ChestPosition + new Vector3(0, 0, -1),
                     new Vector2(Random.Range(-2f, 2f), Random.Range(1f, 3f)));
            }

        }
    }
}
