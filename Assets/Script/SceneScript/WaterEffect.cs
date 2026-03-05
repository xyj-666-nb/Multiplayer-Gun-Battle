using UnityEngine;
public class WaterEffect : MonoBehaviour
{
    public ParticleSystem ShallowWater;
    public ParticleSystem DeepWater;
    public ParticleSystem BurstWater;

    public void triggerParticleSystem(WaterType Type)
    {
        //对应触发粒子
        switch (Type)
        {
            case WaterType.DeepWater:
                DeepWater.Play();
                break;

            case WaterType.BurstWater:
                BurstWater.Play();
                break;
            case WaterType.ShallowWater:
                ShallowWater.Play();
                break;
        }

    }


    public void StopAll()
    {
        ShallowWater.Stop();
        BurstWater.Stop();
        ShallowWater.Stop();
    }

}

public enum WaterType
{
    // 浅水区
    ShallowWater,
    // 深水区
    DeepWater,
    // 爆发/溅射水域（比如子弹击中产生水花的区域）
    BurstWater
}