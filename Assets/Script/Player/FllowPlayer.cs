using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    // 玩家位置缓存
    Vector3 Playerpos;
    // 帧率间隔（每隔多少帧执行一次），可在Inspector面板调整
    [Header("帧率限制设置")]
    [Tooltip("每隔多少帧更新一次跟随位置")]
    public int frameInterval = 5;
    // 帧计数器
    private int frameCounter = 0;

    void Update()
    {
        // 帧计数器自增
        frameCounter++;

        // 只有当计数器达到设定间隔，且玩家存在时才执行位置更新
        if (frameCounter >= frameInterval && Player.LocalPlayer != null)
        {
            // 重置计数器
            frameCounter = 0;

            // 复制玩家的X、Y坐标，保留相机自身的Z坐标
            Playerpos.x = Player.LocalPlayer.transform.position.x;
            Playerpos.y = Player.LocalPlayer.transform.position.y;
            Playerpos.z = this.transform.position.z;

            // 赋值给当前物体（相机）的位置
            this.transform.position = Playerpos;
        }
    }
}