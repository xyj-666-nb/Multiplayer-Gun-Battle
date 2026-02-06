using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class UImanager : SingleBehavior<UImanager>
{
    #region 核心字段
    public Transform canvassform;
    private Dictionary<string, BasePanel> PanelDic = new Dictionary<string, BasePanel>();
    #endregion

    #region 构造函数
    public UImanager()
    {
        //一开始就从预设体中创建一个canvas并且不能让他随场景的改变而删除
        GameObject canvas = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("UI/Canvas"));
        //过场景不要删除
        canvassform = canvas.transform;
        Object.DontDestroyOnLoad(canvas);
    }
    #endregion

    #region 显示面板
    //显示面板
    public T ShowPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;

        //寻找一下字典里有没有这个值，如果有就代表已经显示在界面上面了，这时候就不需要去再显示了
        if (PanelDic.ContainsKey(panelName))
        {
            //如果有就直接返回这个面板
            return PanelDic[panelName] as T;
        }
        GameObject panelObj = null;

        if (Resources.Load<GameObject>("UI/" + panelName).GetComponent<BasePanel>().IsCanDestroy)
        {
            //如果界面上没有面板那则需要代码去找到这个预设体去使用他
            panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + panelName));
        }
        else
        {
            //如果创建过了那就直接去找这个面板
            if (GameObject.Find(panelName))
                panelObj = GameObject.Find(panelName);
            else
                panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + panelName));//如果没有创建过那就创建一个
        }

        if (!panelObj.GetComponent<BasePanel>().IsUseSpecialCanvas)
            panelObj.transform.SetParent(canvassform, false); //由于我们的的所有的面板都要创建再canvas里面这里我们需要设置父对象

        T panel = panelObj.GetComponent<T>();
        //然后再存到字典里面
        PanelDic.Add(panelName, panel);
        //最后调用这个面板的显示函数
        panel.ShowMe();
        UpdatePriorityPanel();//然后进行一个排序
        return panel;
    }
    #endregion

    #region 隐藏面板
    //隐藏面板
    //这里提供一个是否需要淡入淡出的bool值
    //这里不需要返回值
    public void HidePanel<T>(bool isFade = true, UnityAction CallBack = null) where T : BasePanel//加入一个约束就是只T必须要继承这个BasePanel
    {
        string panelName = typeof(T).Name;//获取T的类型并提取名字
        if (PanelDic.ContainsKey(panelName))
        {
            if (isFade)
            {
                //如果我们需要去有淡出这个效果那我们就使用hideMe这个函数
                //里面提供了当淡出完毕所执行的委托函数
                PanelDic[panelName].HideMe(() =>
                {
                    if (PanelDic[panelName].IsCanDestroy)//先判断是否能被删除
                        GameObject.Destroy(PanelDic[panelName].gameObject);
                    CallBack?.Invoke();//然后执行我们传入的回调函数
                    //然后记得还要删除字典里存的信息
                    PanelDic.Remove(panelName);
                });
            }
            else
            {
                //直接删除这个对象就欧克了
                //有的面板有特殊需求不能被删除
                if (PanelDic[panelName].IsCanDestroy)
                    GameObject.Destroy(PanelDic[panelName].gameObject);
                //然后记得还要删除字典里存的信息
                PanelDic.Remove(panelName);
            }
        }
    }
    #endregion

    #region 获取面板
    //获取面板
    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;//获取T的类型并提取名字
        if (PanelDic.ContainsKey(panelName))
        {
            return PanelDic[panelName] as T;
        }
        return null;
    }
    #endregion

    #region 更新面板的覆盖优先级
    /// <summary>
    /// 根据面板PriorityIndex排序，优先级越大，显示在越上层
    /// </summary>
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
}