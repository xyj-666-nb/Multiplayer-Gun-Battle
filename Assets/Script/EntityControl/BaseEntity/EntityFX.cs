using System.Collections;
using UnityEngine;

public class EntityFX : MonoBehaviour
{
    private SpriteRenderer Sr;

    private void Start()
    {
        Sr = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    ///  ‹…À…¡À∏–ßπ˚
    /// </summary>
    public void WoundFlash(float Duration, float Speed)
    {
       StartCoroutine(FiashFx(Duration, Speed));
    }

    private IEnumerator FiashFx(float Duration, float Speed)
    {
        float NowTime = 0;
        while (NowTime <= Duration)
        {
            Sr.color = new Color(1, 0.9f, 0.9f, 0.8f);
            yield return new WaitForSeconds(Speed);
            Sr.color = new Color(1, 0.7f, 0.7f, 0.5f);
            yield return new WaitForSeconds(Speed);
            Sr.color = new Color(1, 0.4f, 0.4f, 0.3f);
            yield return new WaitForSeconds(Speed);
            Sr.color = new Color(1, 1, 1, 1f);
            NowTime += Speed * 3;
        }
        Sr.color = new Color(1, 1, 1, 1f);
    }

}
