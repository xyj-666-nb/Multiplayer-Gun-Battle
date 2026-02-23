using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Reflection; // 记得添加这个命名空间以使用反射

public class UImanager : SingleBehavior<UImanager>
{
    #region 核心字段
    public Transform canvassform;
    private Dictionary<string, BasePanel> PanelDic = new Dictionary<string, BasePanel>();
    #endregion

    #region 构造函数
    public UImanager()
    {
        GameObject canvas = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("UI/Canvas"));
        canvassform = canvas.transform;
        Object.DontDestroyOnLoad(canvas);
    }
    #endregion

    #region 显示面板
    public T ShowPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (PanelDic.ContainsKey(panelName))
        {
            return PanelDic[panelName] as T;
        }
        GameObject panelObj = null;

        if (Resources.Load<GameObject>("UI/" + panelName).GetComponent<BasePanel>().IsCanDestroy)
        {
            panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + panelName));
        }
        else
        {
            if (GameObject.Find(panelName))
                panelObj = GameObject.Find(panelName);
            else
                panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + panelName));
        }

        if (!panelObj.GetComponent<BasePanel>().IsUseSpecialCanvas)
            panelObj.transform.SetParent(canvassform, false);

        T panel = panelObj.GetComponent<T>();
        PanelDic.Add(panelName, panel);
        panel.ShowMe();
        UpdatePriorityPanel();
        return panel;
    }
    #endregion

    #region 隐藏面板
    public void HidePanel<T>(bool isFade = true, UnityAction CallBack = null) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (PanelDic.ContainsKey(panelName))
        {
            if (isFade)
            {
                PanelDic[panelName].HideMe(() =>
                {
                    if (PanelDic[panelName].IsCanDestroy)
                        GameObject.Destroy(PanelDic[panelName].gameObject);
                    CallBack?.Invoke();
                    PanelDic.Remove(panelName);
                });
            }
            else
            {
                if (PanelDic[panelName].IsCanDestroy)
                    GameObject.Destroy(PanelDic[panelName].gameObject);
                PanelDic.Remove(panelName);
            }
        }
    }
    #endregion

    #region 获取面板
    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (PanelDic.ContainsKey(panelName))
        {
            return PanelDic[panelName] as T;
        }
        return null;
    }
    #endregion

    #region 更新面板的覆盖优先级
    public void UpdatePriorityPanel()
    {
        var validPanels = PanelDic.Values
            .Where(panel => panel != null
                            && panel.gameObject != null
                            && panel.transform.parent == canvassform)
            .ToList();

        if (validPanels.Count == 0) return;

        var sortedPanels = validPanels.OrderByDescending(panel => panel.PriorityIndex).ToList();

        for (int i = 0; i < sortedPanels.Count; i++)
        {
            sortedPanels[i].transform.SetSiblingIndex(i);
        }

        Debug.Log($"UI面板优先级排序完成，共排序{sortedPanels.Count}个主Canvas面板");
    }
    #endregion

    #region 简化版：关闭所有面板
    /// <summary>
    /// 简化版：关闭所有已打开的面板
    /// </summary>
    /// <param name="isFade">是否使用淡出效果（默认false，直接关闭）</param>
    public void CloseAllPanels(bool isFade = false)
    {
        var panelsToClose = PanelDic.ToList();

        MethodInfo hideMethod = typeof(UImanager).GetMethod("HidePanel");

        foreach (var kvp in panelsToClose)
        {
            BasePanel panel = kvp.Value;

            if (panel == null)
                continue;

            MethodInfo genericMethod = hideMethod.MakeGenericMethod(panel.GetType());
            genericMethod.Invoke(this, new object[] { isFade, null });
        }
    }
    #endregion
}