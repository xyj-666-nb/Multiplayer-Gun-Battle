using UnityEngine;

public class Monster : Base_Entity
{
    public float LastAttackTime = 0f;
    [Header("玩家图层")]
    public LayerMask WhatIsPlayer;

    [Header("攻击范围检测")]
    [Header("远距离")]
    public float AttackDistance_Far;
    public Transform AttackCheck_Far;
    [Header("近距离")]
    public float AttackDistance_Near;
    public Transform AttackCheck_Near;

    [Header("生物转向控制")]
    public float FlipTime = 3;//转向时间为3秒
    private float lastFlipTime = -10;
    public bool IsNeedImmed = false;//是否为立即转向
    [Header("内部的属性")]
    public CharacterStats me;
    public bool IsNeedCollider = true;


    #region 怪物初始化
    public override void Awake()
    {
        base.Awake();
        if (me == null)
            me = GetComponent<CharacterStats>();

        //关联玩家事件
        EventCenter.Instance.AddEventLister<GameObject>(E_EventType.E_PlayerInit, GetPlayer);
    }
    public virtual void Start()
    {

    }
    #endregion

    public override void Update()
    {
        CheckFilp();//检查翻转
    }


    #region 敌对生物检测

    public virtual bool IsPlayerDetected_Far()
    {
        if (Player == null || AttackCheck_Far == null)
            return false;

        float dx = Player.transform.position.x - AttackCheck_Far.position.x;

        bool inFront = dx * FacingDir > 0f;
        float absDx = Mathf.Abs(dx);

        return inFront && absDx <= AttackDistance_Far;
    }

    public virtual bool IsPlayerDetected_Near()
    {
        if (Player == null || AttackCheck_Near == null)
            return false;

        float dx = Player.transform.position.x - AttackCheck_Near.position.x;

        bool inFront = dx * FacingDir > 0f;
        float absDx = Mathf.Abs(dx);

        return inFront && absDx <= AttackDistance_Near;
    }

    #endregion

    #region 生物翻转检测
    public void CheckFilp()
    {
        if (IsWallDetected())//撞墙直接转向
            Flip();

        //首先是这里，对于玩家的检查的范围是有距离限度的
        if(Mathf.Abs( Player.transform.position.x - AttackCheck_Far.position.x)> AttackDistance_Far * 1.5f||Player==null)//距离过远就进入常规的巡航
        {
            if (Time.time > lastFlipTime + FlipTime)
            {
                 Flip();
                lastFlipTime = Time.time;
            }
        }
        else//近距离再瞄准玩家
        {
            if (IsNeedImmed)
            {
                if ((Player.transform.position.x - AttackCheck_Far.position.x) > 0 && FacingDir != 1)
                    Flip();
                else if ((Player.transform.position.x - AttackCheck_Far.position.x) < 0 && FacingDir != -1)
                    Flip();
                return;
            }
            else
            {
                if (Time.time > lastFlipTime + FlipTime )
                {
                    if ((Player.transform.position.x - AttackCheck_Far.position.x) > 0 && FacingDir != 1)
                        Flip();
                    else if ((Player.transform.position.x - AttackCheck_Far.position.x) < 0 && FacingDir != -1)
                        Flip();
                    lastFlipTime = Time.time;
                }
            }     
        }

       

    }
    #endregion

    #region 检测可视化

    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(AttackCheck_Far.position, new Vector3(AttackCheck_Far.position.x + AttackDistance_Far * FacingDir, AttackCheck_Far.position.y));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(AttackCheck_Near.position, new Vector3(AttackCheck_Near.position.x + AttackDistance_Near * FacingDir, AttackCheck_Near.position.y));
    }
    #endregion

    #region 玩家对象获取

    private GameObject Player;//玩家

    public void GetPlayer(GameObject _Player)
    {
        Player = _Player;
    }
    #endregion

  
    public virtual void OnTriggerEnter2D(Collider2D collision)
    {

    }

    public virtual void OnDestroy()
    {
        
    }
}

