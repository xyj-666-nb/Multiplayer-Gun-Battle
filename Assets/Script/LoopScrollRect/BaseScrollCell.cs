using UnityEngine;
using UnityEngine.UI;

public class BaseScrollCell : MonoBehaviour
{
    private LayoutElement _layoutElement;
    private RectTransform _rt;
    public int Index; // 当前的滚动位置
    private LayoutGroup _parentLayoutGroup; // 父布局组
    private LoopScrollRect _scrollRect; // 循环滚动核心组件


    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _layoutElement = GetComponent<LayoutElement>();
        _scrollRect = GetComponentInParent<LoopScrollRect>();

        SyncLayoutElementSize();

    }

    // 延迟到Start获取父布局组（此时父对象已设置）
    private void Start()
    {
        if (transform.parent != null)
        {
            _parentLayoutGroup = transform.parent.GetComponent<LayoutGroup>();
            if (_parentLayoutGroup == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 父对象没有LayoutGroup组件，尺寸修改可能不生效！");
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 父对象为空，无法获取LayoutGroup！");
        }
    }

    /// <summary>
    /// 自动把 RectTransform 的宽高同步给 LayoutElement 的最小/首选尺寸
    /// </summary>
    private void SyncLayoutElementSize()
    {
        // 空值防护：核心组件为空直接返回
        if (_rt == null || _layoutElement == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 缺少 RectTransform 或 LayoutElement 组件，无法自动同步尺寸！");
            return;
        }

        // 获取 RectTransform 的实际宽高
        float cellWidth = _rt.rect.width;
        float cellHeight = _rt.rect.height;

        // 确保尺寸为正
        cellWidth = Mathf.Max(cellWidth, 1f);
        cellHeight = Mathf.Max(cellHeight, 1f);

        // 同时设置最小/首选尺寸（确保布局组优先用这个值）
        _layoutElement.minWidth = cellWidth;
        _layoutElement.minHeight = cellHeight;
        _layoutElement.preferredWidth = cellWidth;
        _layoutElement.preferredHeight = cellHeight;

        Debug.Log($"[{gameObject.name}] 自动同步尺寸完成：宽={cellWidth:F1}，高={cellHeight:F1}");
    }


    /// <summary>
    /// 触发父布局组重建
    /// </summary>
    private void RebuildParentLayout()
    {
        if (_parentLayoutGroup == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 父布局组为空，无法触发布局重建！");
            return;
        }

        // 强制重建布局
        RectTransform parentRt = _parentLayoutGroup.GetComponent<RectTransform>();
        if (parentRt != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRt);
            Canvas.ForceUpdateCanvases();
        }
    }

    /// <summary>
    /// 刷新滚动视图
    /// </summary>
    private void RefreshScrollView()
    {
        if (_scrollRect == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 未找到LoopScrollRect组件，无法刷新滚动视图！");
            return;
        }

        _scrollRect.RefreshCells();

    }

    // 循环列表赋值索引的原始方法
    public void ScrollCellIndex(int idx)
    {
        if (_rt == null || _layoutElement == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 组件不完整，无法修改尺寸！");
            return;
        }

        Index = idx;
        RebuildParentLayout();
    }
}