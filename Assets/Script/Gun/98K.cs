
public class Gun_98K: BaseGun
{
    //对98K的换弹特殊实现
    private float EndAnimationTime = 3.6833f; //换弹结束动画时间(执行最后的拉枪击动画)
    private int ReloadCount = 0;//当前换弹次数

    public  void Awake()
    {
        ReloadSuccessAction += getReloadCount;//获取当前需要的换弹次数
    }

    public void CheckNeedReload()//检测是否需要继续换弹(每次换弹的时候Timeline调用)
    {
        ReloadCount--;//换弹次数-1
        if(ReloadCount<=0)
        {
            //直接跳转到换弹结束动画
            timelineDirector_Reload.time = EndAnimationTime;//跳转到换弹结束动画时间点
            timelineDirector_Reload.Play();//播放换弹动画
        }

    }

    public void  getReloadCount()//获取当前需要换弹的次数
    {
         ReloadCount = (int)(gunInfo.Bullet_capacity - _currentMagazineBulletCount);//获取需要换弹的子弹数量

        if (ReloadCount > _allReserveBulletCount)
            ReloadCount= (int)_allReserveBulletCount;//如果需要换弹的子弹数量大于剩余子弹数量，则只换剩余子弹数量

    }

}
