using UnityEngine;

public class FllowPlayer : MonoBehaviour
{
    Vector3 Playerpos;
    void Update()
    {
        if (Player.LocalPlayer!=null)
        {
            Playerpos.x = Player.LocalPlayer.transform.position.x;
            Playerpos.y = Player.LocalPlayer.transform.position.y;
            Playerpos.z = this.transform.position.z;
            this.transform.position = Playerpos;
        }


    }
}
