using UnityEngine;

public class AllManager : MonoBehaviour
{

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

}
