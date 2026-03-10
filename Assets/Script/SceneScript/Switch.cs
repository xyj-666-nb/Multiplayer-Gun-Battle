using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Switch : BaseBulletInteract_NetWork
{
    [Header("管理路灯列表")]
    public List<StreetLamp> ManageStreetLampList;
    public Sprite qinwei, yanzhong, wanquansunhuai;
    public override void EffectTrigger()
    {
       
        gameObject.tag = "BackGround";
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public override void HealthChangeEffect(float health)
    {
        if (!isServer) return;

    
        StreetLampDegreeOfDestruction switchInfluence = health switch
        {
            > 150 => StreetLampDegreeOfDestruction.Normal,   //正常   
            > 100 => StreetLampDegreeOfDestruction.Slight,   //轻微
            > 50 => StreetLampDegreeOfDestruction.Serious,   //严重
            _ => StreetLampDegreeOfDestruction.Lose         //完全  
        };

        SetStreetLampInfluence(switchInfluence);
    }

    [Server]
    public void SetStreetLampInfluence(StreetLampDegreeOfDestruction influence)
    {
        if (ManageStreetLampList == null) return;

        foreach (var lamp in ManageStreetLampList)
        {
            if (lamp != null)
            {
                lamp.SetSwitchInfluence(influence);
            }
        }
    }

    public override void Init()
    {
        if (isServer)
        {
        
            SetStreetLampInfluence(StreetLampDegreeOfDestruction.Normal);
        }
    }

    public override void ResetObj()
    {
        if (isServer)
        {
            CurrentHealthValue = InitHealthValue;
            IsTrigger = false;
 
            SetStreetLampInfluence(StreetLampDegreeOfDestruction.Normal);
            gameObject.tag = "BulletInteractObj";
            gameObject.layer = LayerMask.NameToLayer("BulletInteractObj");
      
            CmdHeal();
        }
    }
}