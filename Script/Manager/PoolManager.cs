using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

#region 对象池数据类
public class PoolDate
{
    private Stack<GameObject> DataStack; // 存储抽屉中的对象
    private GameObject RootObj; // 对象池根节点

    public PoolDate(GameObject root, string name)
    {
        DataStack = new Stack<GameObject>();
        RootObj = new GameObject(name + "_Pool");
        RootObj.transform.SetParent(root.transform);
    }

    public int Count => DataStack.Count;

    // 从池子里取出对象
    public GameObject Pop()
    {
        if (DataStack.Count == 0)
            return null;

        GameObject obj = DataStack.Pop();
        obj.SetActive(true);
        obj.transform.SetParent(null);
        return obj;
    }

    // 回收对象到池子
    public void Pushobj(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(RootObj.transform);
        DataStack.Push(obj);
    }
}

public abstract class CustomizeDataBase { };

public class CustomizeData<T> : CustomizeDataBase where T : class
{
    public Queue<T> PoolObj = new Queue<T>();
}

#endregion 

/// <summary>
/// 标准单例对象池管理器
/// </summary>
public class PoolManage : SingleMonoAutoBehavior<PoolManage>
{
    #region 数据结构以及变量

    //这里回收的是游戏对象
    private Dictionary<string, PoolDate> objPoolDic; // 预制体名 → 对象池
    //这里是回收自定义的数据结构类
    private Dictionary<string, CustomizeDataBase> CustomizePoolDic; // 类型名 → 对象池
    private GameObject PoolRoot; // 所有对象池的根节点
    #endregion

    #region 生命周期

    protected override void Awake()
    {
       base.Awake();
        objPoolDic = new Dictionary<string, PoolDate>();
        CustomizePoolDic = new Dictionary<string, CustomizeDataBase>();
        PoolRoot = new GameObject("PoolRoot");
        DontDestroyOnLoad(PoolRoot);
    }
    #endregion

    #region 游戏对象对象池
    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    public GameObject GetObj(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        string prefabName = prefab.name;
        GameObject obj = null;

        // 池子里有闲置对象，直接复用
        if (objPoolDic.ContainsKey(prefabName) && objPoolDic[prefabName].Count > 0)
        {
            obj = objPoolDic[prefabName].Pop();
        }
        // 池子没有，实例化新对象
        else
        {
            obj = Instantiate(prefab);
            obj.name = prefabName;
        }

        return obj;
    }

    /// <summary>
    /// 清空指定预制体的对象池
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        if (prefab == null || !objPoolDic.ContainsKey(prefab.name))
            return;

        objPoolDic.Remove(prefab.name);
        Debug.Log($"[对象池] 清空 {prefab.name} 池");
    }

    /// <summary>
    /// 回收对象到池
    /// </summary>
    public void PushObj(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null)
        {
            Debug.LogError("PushObj：预制体或对象为空！");
            return;
        }

        string prefabName = prefab.name;

        if (!objPoolDic.ContainsKey(prefabName))
        {
            objPoolDic.Add(prefabName, new PoolDate(PoolRoot, prefabName));
        }

        ResetObjectToPrefab(prefab, obj);
        if (obj.transform.parent != null)
        {
            obj.transform.SetParent(null);
        }

        objPoolDic[prefabName].Pushobj(obj);
    }

    /// <summary>
    /// 回收自定义数据对象到池
    /// </summary>
    public void PushObj<T>(T obj, string nameSpace = "") where T : class, IPoolObject, new()
    {
        string PoolName = nameSpace + "_" + typeof(T).Name;
        CustomizeData<T> Pool = null;

        // 不存在则创建新池并添加到字典
        if (!CustomizePoolDic.ContainsKey(PoolName))
        {
            Pool = new CustomizeData<T>();
            CustomizePoolDic.Add(PoolName, Pool);
        }
        else
        {
            Pool = CustomizePoolDic[PoolName] as CustomizeData<T>;
        }

        // 重置数据后入池
        obj.ReSetDate();
        Pool.PoolObj.Enqueue(obj);
    }

    /// <summary>
    /// 将回收的物体恢复为预制体的默认状态
    /// </summary>
    // 改写PoolManage中的ResetObjectToPrefab方法
    private void ResetObjectToPrefab(GameObject prefab, GameObject obj)
    {
        // 先脱离父物体，避免缩放继承
        obj.transform.SetParent(null);

        obj.transform.localPosition = prefab.transform.localPosition;
        obj.transform.localRotation = prefab.transform.localRotation;
        obj.transform.localScale = Vector3.one;

        RectTransform prefabRT = prefab.GetComponent<RectTransform>();
        RectTransform objRT = obj.GetComponent<RectTransform>();
        if (prefabRT != null && objRT != null)
        {
            objRT.anchorMin = prefabRT.anchorMin;
            objRT.anchorMax = prefabRT.anchorMax;
            objRT.pivot = prefabRT.pivot;
            objRT.anchoredPosition = prefabRT.anchoredPosition;
            objRT.sizeDelta = prefabRT.sizeDelta;
            objRT.anchoredPosition3D = prefabRT.anchoredPosition3D;
        }

        var prefabImg = prefab.GetComponent<UnityEngine.UI.Image>();
        var objImg = obj.GetComponent<UnityEngine.UI.Image>();
        if (prefabImg != null && objImg != null)
        {
            objImg.color = prefabImg.color;
            objImg.sprite = prefabImg.sprite;
            objImg.material = prefabImg.material;
            objImg.raycastTarget = prefabImg.raycastTarget;

            objImg.DOKill();
            objImg.color = prefabImg.color;
        }

        var prefabCG = prefab.GetComponent<CanvasGroup>();
        var objCG = obj.GetComponent<CanvasGroup>();
        if (prefabCG != null && objCG != null)
        {
            objCG.alpha = prefabCG.alpha;
            objCG.interactable = prefabCG.interactable;
            objCG.blocksRaycasts = prefabCG.blocksRaycasts;
        }
        obj.transform.DOKill(true);
    }
    #endregion

    #region 自定义数据对象池子
    /// <summary>
    /// 获取自定义数据对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="nameSpace">不同命名空间但是定义的相同数据名的情况下填写</param>
    /// <returns></returns>
    public T GetObj<T>(string nameSpace = "") where T : class, IPoolObject, new()
    {
        string PoolName = nameSpace + "_" + typeof(T).Name;
        T obj = null;
        CustomizeData<T> Pool = null;

        // 不存在则创建新池并添加到字典
        if (!CustomizePoolDic.ContainsKey(PoolName))
        {
            Pool = new CustomizeData<T>();
            CustomizePoolDic.Add(PoolName, Pool);
        }
        else
        {
            Pool = CustomizePoolDic[PoolName] as CustomizeData<T>;
        }

        // 池中有闲置对象则复用，否则新建
        if (Pool.PoolObj.Count > 0)
        {
            obj = Pool.PoolObj.Dequeue() as T;
        }
        else
        {
            obj = new T();
        }

        return obj;
    }

    public void ClearDate<T>(string nameSpace = "")
    {
        string PoolName = nameSpace + "_" + typeof(T).Name;
        if (CustomizePoolDic.ContainsKey(PoolName))
            CustomizePoolDic.Remove(PoolName);
    }
    #endregion

    #region 清除所有的池子
    public void ClearAllPool()//清除所有的对象池
    {
        objPoolDic.Clear();
        if (PoolRoot != null)
            Destroy(PoolRoot);
        PoolRoot = null;
        CustomizePoolDic.Clear();
    }
    #endregion
}