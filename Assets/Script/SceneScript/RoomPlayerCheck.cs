using System.Collections.Generic;
using UnityEngine;

public class RoomPlayerCheck : BaseSceneInteract
{
    [Header("视觉控制")]
    public SpriteGroup SpriteGroup;//控制房子外围

    [Header("门列表")]
    public List<Door> InnerAllDoors;//房间内部所有的门
    public List<Door> OutsideAllDoors;//房间外部所有的门

    [Header("碰撞体控制")]
    public Collider2D InterCollider;//内部碰撞体
    public Collider2D InterCollider2;//内部碰撞体
    public BoxCollider2D OutsideCollider1;//外部碰撞体1
    public BoxCollider2D OutsideCollider2;//外部碰撞体2

    [Header("离开判断配置")]
    public int EnterDir = 1;//内部方向

    private bool IsEnterRoom = false;//更语义化的命名：是否进入房间

    public override void Awake()
    {
        base.Awake();
        SetFalseAllInnerDoors();
        SetAllOutsideDoorsCanUse();

        // 碰撞体初始化
        if (InterCollider != null) InterCollider.isTrigger = true;
        if (OutsideCollider1 != null) OutsideCollider1.isTrigger = false;
        if (OutsideCollider2 != null) OutsideCollider2.isTrigger = false;
    }

    /// <summary>
    /// 玩家触发进入房间逻辑
    /// </summary>
    public override void TriggerEffect()
    {
        if (SpriteGroup != null) SpriteGroup.FadeOut();//快速淡出房子外围

        // 门状态切换：内部门可用，外部门禁用
        SetAllInnerDoorsCanUse();
        SetFalseAllOutsideDoors();

        if (InterCollider != null) InterCollider.isTrigger = false;
        if (OutsideCollider1 != null) OutsideCollider1.isTrigger = true;
        if (OutsideCollider2 != null) OutsideCollider2.isTrigger = true; 

        IsEnterRoom = true;//标记已进入房间
                           //设置玩家进入房间
        Player.LocalPlayer.CmdChangeEnterRoomState(true);
        Debug.Log("玩家进入房间：内部门启用，外部门禁用，房子外围隐藏");
    }

    #region 内部门控制方法（加判空保护）
    public void SetAllInnerDoorsCanUse()
    {
        if (InnerAllDoors == null || InnerAllDoors.Count == 0)
        {
            Debug.LogWarning("内部门列表为空，无法启用内部门！");
            return;
        }

        foreach (Door d in InnerAllDoors)
        {
            if (d != null) d.CanUse = true;
        }
    }

    public void SetFalseAllInnerDoors()
    {
        if (InnerAllDoors == null || InnerAllDoors.Count == 0)
        {
            Debug.LogWarning("内部门列表为空，无法禁用内部门！");
            return;
        }

        foreach (Door d in InnerAllDoors)
        {
            if (d != null) d.CanUse = false;
        }
    }
    #endregion

    #region 外部门控制方法
    public void SetAllOutsideDoorsCanUse()
    {
        if (OutsideAllDoors == null || OutsideAllDoors.Count == 0)
        {
            Debug.LogWarning("外部门列表为空，无法启用外部门！");
            return;
        }

        foreach (Door d in OutsideAllDoors)
        {
            if (d != null) d.CanUse = true;
        }
    }

    public void SetFalseAllOutsideDoors()
    {
        if (OutsideAllDoors == null || OutsideAllDoors.Count == 0)
        {
            Debug.LogWarning("外部门列表为空，无法禁用外部门！");
            return;
        }

        foreach (Door d in OutsideAllDoors)
        {
            if (d != null) d.CanUse = false;
        }
    }
    #endregion

    public override void triggerEnterRange() { }

    /// <summary>
    /// 玩家离开触发范围时的判断逻辑
    /// </summary>
    public override void triggerExitRange()
    {
        // 空引用保护：本地玩家不存在则直接返回
        if (Player.LocalPlayer == null)
            return;

        // 未进入房间则无需处理离开逻辑
        if (!IsEnterRoom)
            return;

        float playerPosX = Player.LocalPlayer.transform.position.x;
        float roomPosX = transform.position.x;
        bool isPlayerReallyLeave = false;

        Debug.Log($"离开判断日志 → 玩家X：{playerPosX} | 房间锚点X：{roomPosX} | EnterDir：{EnterDir}| ");

        if (EnterDir == 1)
        {
            isPlayerReallyLeave = playerPosX < roomPosX;
        }
        else if (EnterDir == -1)
        {
            isPlayerReallyLeave = playerPosX > roomPosX;
        }

        // 只有真正离开时，才执行恢复逻辑
        if (isPlayerReallyLeave)
        {
            if (SpriteGroup != null)
                SpriteGroup.FadeIn();//恢复房子外围显示
            SetFalseAllInnerDoors();//禁用内部门
            SetAllOutsideDoorsCanUse();//启用外部门

            // 恢复碰撞体状态
            if (InterCollider != null)
                InterCollider.isTrigger = true;
            if (OutsideCollider1 != null)
                OutsideCollider1.isTrigger = false;
            if (OutsideCollider2 != null)
                OutsideCollider2.isTrigger = false;

            IsEnterRoom = false;

            Debug.Log("玩家真正离开房间：恢复房子外围，禁用内部门，启用外部门");
            Player.LocalPlayer.CmdChangeEnterRoomState(false);
        }
    }
}