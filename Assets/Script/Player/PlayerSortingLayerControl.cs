using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSortingLayerControl : MonoBehaviour
{
    [Header("层级设置")]
    [Tooltip("需要管理的2D图片渲染器列表")]
    public List<SpriteRenderer> NeedManagerSpriteRendererList = new List<SpriteRenderer>(); // 直接初始化

    private const string LayerName_Default = "Default";
    private const string LayerName_RoomInternal = "RoomInternal";
    private bool _currentLayerIsRoomInternal = false;

    #region 核心功能

    /// <summary>
    ///在 Default 和 RoomInternal 之间来回切换
    /// </summary>
    [ContextMenu("自动切换层级 (Switch)")]
    public void ChangeSortingLayer()
    {
        // 使用 .ToList() 创建副本，遍历的是快照，不怕原列表被修改
        foreach (var spriteRenderer in NeedManagerSpriteRendererList.ToList())
        {
            if (spriteRenderer == null) continue; // 过滤掉已销毁的对象

            // 切换逻辑
            if (spriteRenderer.sortingLayerName == LayerName_Default)
            {
                spriteRenderer.sortingLayerName = LayerName_RoomInternal;
            }
            else
            {
                spriteRenderer.sortingLayerName = LayerName_Default;
            }
        }

        // 同步自身状态标记
        _currentLayerIsRoomInternal = !_currentLayerIsRoomInternal;
    }

    /// <summary>
    /// 强制设置为某一层
    /// </summary>
    /// <param name="isSetToRoomInternal">true=设为RoomInternal, false=设为Default</param>
    [ContextMenu("设置为 RoomInternal")]
    public void SetSortingLayer(bool isSetToRoomInternal)
    {
        _currentLayerIsRoomInternal = isSetToRoomInternal;
        string targetLayer = isSetToRoomInternal ? LayerName_RoomInternal : LayerName_Default;

        // 优化点1：同样使用副本遍历
        foreach (var spriteRenderer in NeedManagerSpriteRendererList.ToList())
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = targetLayer;
            }
        }

        // 顺便清理一下列表里的空引用（可选）
        CleanUpNullReferences();
    }

    #endregion

    #region 列表管理

    /// <summary>
    /// 将传入的SpriteRenderer纳入管理
    /// </summary>
    public void AddSpriteRendererInManager(SpriteRenderer obj)
    {
        if (obj == null) return;

        // 优化点2：防止重复添加
        if (!NeedManagerSpriteRendererList.Contains(obj))
        {
            NeedManagerSpriteRendererList.Add(obj);
            // 立即同步当前层级
            obj.sortingLayerName = _currentLayerIsRoomInternal ? LayerName_RoomInternal : LayerName_Default;
        }
    }

    /// <summary>
    /// 将SpriteRenderer从管理中移除
    /// </summary>
    public void RemoveSpriteRendererFromManager(SpriteRenderer obj)
    {
        if (obj == null)
            return;

        // 直接移除即可，因为我们在遍历时使用的是副本
        NeedManagerSpriteRendererList.Remove(obj);
    }

    /// <summary>
    /// 清理列表中已销毁的对象（手动调用或自动调用）
    /// </summary>
    private void CleanUpNullReferences()
    {
        // 优化点3：使用倒序 for 循环移除，效率最高且安全
        for (int i = NeedManagerSpriteRendererList.Count - 1; i >= 0; i--)
        {
            if (NeedManagerSpriteRendererList[i] == null)
            {
                NeedManagerSpriteRendererList.RemoveAt(i);
            }
        }
    }

    #endregion
}