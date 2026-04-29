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
    private RoomPlayerCheck[] _roomGroupChecks;

    public override void Awake()
    {
        base.Awake();
        ApplyExitState(false);
    }

    /// <summary>
    /// 玩家触发进入房间逻辑
    /// </summary>
    public override void TriggerEffect()
    {
        CacheRoomGroupChecks();

        foreach (RoomPlayerCheck check in _roomGroupChecks)
        {
            if (check != null)
                check.ApplyEnterState();
        }

        if (Player.LocalPlayer != null)
            Player.LocalPlayer.CmdChangeEnterRoomState(true);

        /* Debug.Log("Player entered room group"); */
    }

    private void ApplyEnterState()
    {
        if (SpriteGroup != null) SpriteGroup.FadeOut();

        SetAllInnerDoorsCanUse();
        SetFalseAllOutsideDoors();

        if (InterCollider != null) InterCollider.isTrigger = false;
        if (InterCollider2 != null) InterCollider2.isTrigger = false;
        if (OutsideCollider1 != null) OutsideCollider1.isTrigger = true;
        if (OutsideCollider2 != null) OutsideCollider2.isTrigger = true;

        IsEnterRoom = true;
    }

    private void ApplyExitState(bool isNeedFade)
    {
        if (isNeedFade && SpriteGroup != null)
            SpriteGroup.FadeIn();

        SetFalseAllInnerDoors();
        SetAllOutsideDoorsCanUse();

        if (InterCollider != null)
            InterCollider.isTrigger = true;
        if (InterCollider2 != null)
            InterCollider2.isTrigger = true;
        if (OutsideCollider1 != null)
            OutsideCollider1.isTrigger = false;
        if (OutsideCollider2 != null)
            OutsideCollider2.isTrigger = false;

        IsEnterRoom = false;
    }

    private void CacheRoomGroupChecks()
    {
        if (_roomGroupChecks != null && _roomGroupChecks.Length > 0)
            return;

        Transform root = GetRoomGroupRoot();
        _roomGroupChecks = root != null ? root.GetComponentsInChildren<RoomPlayerCheck>(true) : null;

        if (_roomGroupChecks == null || _roomGroupChecks.Length == 0)
            _roomGroupChecks = new[] { this };
    }

    private Transform GetRoomGroupRoot()
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.name.StartsWith("map") || current.name.StartsWith("Map"))
                return current;

            current = current.parent;
        }

        return transform.root;
    }

    private bool IsAnyRoomGroupEntered()
    {
        CacheRoomGroupChecks();

        foreach (RoomPlayerCheck check in _roomGroupChecks)
        {
            if (check != null && check.IsEnterRoom)
                return true;
        }

        return false;
    }

    #region 内部门控制方法（加判空保护）
    public void SetAllInnerDoorsCanUse()
    {
        if (InnerAllDoors == null || InnerAllDoors.Count == 0)
        {
            /* Debug.LogWarning("内部门列表为空，无法启用内部门！"); */
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
            /* Debug.LogWarning("内部门列表为空，无法禁用内部门！"); */
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
            /* Debug.LogWarning("外部门列表为空，无法启用外部门！"); */
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
            /* Debug.LogWarning("外部门列表为空，无法禁用外部门！"); */
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
        if (!IsAnyRoomGroupEntered())
            return;

        float playerPosX = Player.LocalPlayer.transform.position.x;
        float roomPosX = transform.position.x;
        bool isPlayerReallyLeave = false;

        /* Debug.Log($"离开判断日志 → 玩家X：{playerPosX} | 房间锚点X：{roomPosX} | EnterDir：{EnterDir}| "); */

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
            CacheRoomGroupChecks();

            foreach (RoomPlayerCheck check in _roomGroupChecks)
            {
                if (check != null)
                    check.ApplyExitState(true);
            }

            /* Debug.Log("Player left room group"); */
            Player.LocalPlayer.CmdChangeEnterRoomState(false);
        }
    }
}