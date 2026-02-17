using UnityEngine;

public class BigExplorion : MonoBehaviour
{
    private float timeSpan = 0.2f;
    [SerializeField] UnityEngine.Rendering.Universal.Light2D flash;

    void Update()
    {
        FluidController.Instance.QueueDrawAtPoint(
                this.transform.position,
                new Color(1.0f * timeSpan * 5f, 1.0f * timeSpan * 5f, 1.0f * timeSpan * 5f, 1.2f),
                Vector2.zero,
                2.0f * timeSpan * 5f,
                3.2f * timeSpan * 5f,
                FluidController.VelocityType.Explore
            );
        flash.intensity = 3.0f * timeSpan * 5f;
        timeSpan -= Time.deltaTime;
        if (timeSpan <= 0)
        {
            Destroy(gameObject);
        }
    }
}
