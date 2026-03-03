using Mirror;
using System;
using UnityEngine;

public abstract class BaseBulletInteract_NetWork : NetworkBehaviour//需要全局进行同步
{
    [SyncVar(hook = nameof(OnChangeState))]
    public bool IsTrigger = false;//是否触发

    public override void OnStartServer()
    {
        base.OnStartServer();

        Transform rootTransform = transform.root;
        string rootName = rootTransform.name;

        int currentMapIndex = ExtractMapIndexFromName(rootName);

        if (currentMapIndex != -1)
        {
            PlayerRespawnManager.Instance?.InitInteractObj(netIdentity, currentMapIndex);
        }
        else
        {
            Debug.LogError($"无法从根物体名称 '{rootName}' 中提取地图索引！请确保物体在 Map1 或 Map2 下。", this);
        }
    }

    /// <summary>
    ///从字符串中提取地图数字
    /// </summary>
    private int ExtractMapIndexFromName(string name)
    {

        string numberStr = name.Replace("Map", "");

        if (int.TryParse(numberStr, out int result))
        {
            return result;
        }

        return -1;
    }

    private void OnChangeState(bool OldValue, bool NewValue)
    {
        if (NewValue)
            EffectTrigger();//触发效果
    }

    //基础子弹交互物体 
    public abstract void Wound(Vector3 HitPos, float BulletDamage);//传入交互（子弹打中调用这个。至于什么时候触发看具体的需求）

    //效果触发
    public abstract void EffectTrigger();

    public abstract void ResetObj();//强制实现还原函数，场景会自动在结束的时候调用重置

    //初始化函数
    public virtual void Init()
    {

    }
}

