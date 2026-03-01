using System.Collections.Generic;
using UnityEngine;

public class RoomPlayerCheck : BaseSceneInteract
{
    [Header("视觉控制")]
    public SpriteGroup SpriteGroup;//控制房子外围

    [Header("门列表")]
    public List<Door> InnerAllDoors;//房间内部所有的门（复数命名，更易读）
    public List<Door> OutsideAllDoors;//房间外部所有的门（修复命名）

    [Header("碰撞体控制")]
    public Collider2D InterCollider;//内部碰撞体
    public BoxCollider2D OutsideCollider1;//外部碰撞体1（大写首字母）
    public BoxCollider2D OutsideCollider2;//外部碰撞体2（大写首字母）

    [Header("离开判断配置")]
    public int EnterDir = 1;//内部方向(1为右，-1为左)（修复命名）
    [Tooltip("位置判断阈值（避免微小偏移误判，建议0.1~0.5）")]
    public float PositionCheckThreshold = 0.1f;

    private bool IsEnterRoom = false;//更语义化的命名：是否进入房间

    private void Awake()
    {
        // 初始化：内部门禁用，外部门启用（加判空保护）
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

        // 门状态切换：内部门可用，外部门禁用（加判空）
        SetAllInnerDoorsCanUse();
        SetFalseAllOutsideDoors();

        // 碰撞体切换
        if (InterCollider != null) InterCollider.isTrigger = false;
        if (OutsideCollider1 != null) OutsideCollider1.isTrigger = true;
        if (OutsideCollider2 != null) OutsideCollider2.isTrigger = false;

        IsEnterRoom = true;//标记已进入房间
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

    #region 外部门控制方法（修复遍历列表+加判空）
    public void SetAllOutsideDoorsCanUse()
    {
        if (OutsideAllDoors == null || OutsideAllDoors.Count == 0)
        {
            Debug.LogWarning("外部门列表为空，无法启用外部门！");
            return;
        }

        // 修复：遍历OutsideAllDoors而非InnerAllDoors
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

        // 修复：遍历OutsideAllDoors而非InnerAllDoors
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
        if (Player.LocalPlayer == null) return;

        // 未进入房间则无需处理离开逻辑
        if (!IsEnterRoom) return;

        // 计算玩家是否真正离开房间（结合方向+阈值）
        bool isPlayerReallyLeave = false;
        float playerPosX = Player.LocalPlayer.transform.position.x;
        float roomPosX = transform.position.x;

        if (EnterDir > 0)
        {
            // 内部朝右：玩家x < 房间x - 阈值 → 真正离开（跑到房间左边）
            isPlayerReallyLeave = (playerPosX < roomPosX - PositionCheckThreshold);
        }
        else
        {
            // 内部朝左：玩家x > 房间x + 阈值 → 真正离开（跑到房间右边）
            isPlayerReallyLeave = (playerPosX > roomPosX + PositionCheckThreshold);
        }

        // 只有真正离开时，才执行恢复逻辑
        if (isPlayerReallyLeave)
        {
            if (SpriteGroup != null) SpriteGroup.FadeIn();//恢复房子外围显示
            SetFalseAllInnerDoors();//禁用内部门
            SetAllOutsideDoorsCanUse();//启用外部门

            // 恢复碰撞体状态
            if (InterCollider != null) InterCollider.isTrigger = true;
            if (OutsideCollider1 != null) OutsideCollider1.isTrigger = false;
            if (OutsideCollider2 != null) OutsideCollider2.isTrigger = false;

            // 关键：重置进入标记，否则下次进入后离开判断失效
            IsEnterRoom = false;

            Debug.Log("玩家真正离开房间：恢复房子外围，禁用内部门，启用外部门");
        }
    }
}