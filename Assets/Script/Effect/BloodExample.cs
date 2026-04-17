using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodExample : MonoBehaviour
{
    int tick = 0;
    bool start = false;
    Rigidbody2D rigidbody;

  
    private readonly Vector3 CHEST_OFFSET = new Vector3(0, 0.3f, 0);
    private readonly Vector3 BLOOD_BACKGROUND_OFFSET = new Vector3(0, 0, 1);
    private readonly Vector3 BLOOD_PARTICLE_OFFSET = new Vector3(0, 0, -1);

    private Camera mainCamera;

    Vector3 ChestPosition => transform.position + CHEST_OFFSET;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        // 初始化缓存主相机
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && !start)
        {
            start = true;
            // 使用缓存的相机，无GC开销
            Vector2 velocity = ((Vector2)(mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position)).normalized * 5;
            rigidbody.velocity = velocity;
            for (int i = 0; i < 3; i++)
                // 使用缓存的偏移量
                BloodParticleGenerator.Instance.GenerateBloodOnBackground(ChestPosition + BLOOD_BACKGROUND_OFFSET);
        }

    }

    private void FixedUpdate()
    {
        if (start)
        {
            tick++;

            if (tick % 3 == 0 && tick < 50)
            {
                // 使用缓存的偏移量
                BloodParticleGenerator.Instance.GenerateBloodParticle(ChestPosition + BLOOD_PARTICLE_OFFSET,
                     new Vector2(Random.Range(-2f, 2f), Random.Range(1f, 3f)));
            }

        }
    }
}