using UnityEngine;

public class cartridgeCase : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.CompareTag("Ground"))
        {
            MusicManager.Instance.PlayEffect("Music/BulletFill/µ°¿ÇµôÂä1",0.75f);
        }
    }
}
