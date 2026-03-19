using UnityEngine;
using UnityEngine.Playables;

public class Helmet : MonoBehaviour
{
    //玩家头盔控制
    public PlayableDirector HelmetTimeLine;//头盔动画
    public Collider2D MyCollider;//我的碰撞体
    public float RecycleTime = 4f;//回收时间

    public void TriggerHelmetDrop()
    {
        //触发头盔的掉落
        MyCollider.gameObject.SetActive(true);//打开碰撞
        HelmetTimeLine.Stop();//停止动画
        gameObject.AddComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(3f,6f),3f),ForceMode2D.Impulse);//添加刚体模拟重力
        //给与随机力
        transform.parent = null;//清空父对象
        //4秒后销毁
        Destroy(gameObject, RecycleTime);
    }
}
