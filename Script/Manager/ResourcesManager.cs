using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
///资源信息类，主要用于里氏替换原则 父类装载子类对象
/// </summary>
public abstract class BaseResinfo
{
    public int refCount = 0;//引用计数
}
public class ResInfo<T> : BaseResinfo
{
    public T Asset;
    //主要用于异步加载资源的回调函数
    public UnityAction<T> CallBack;
    //用于存储异步加载时开启的协同程序的函数的对象
    public Coroutine Coroutine;

    //当引用计数为零时是否需要移除，因为有的时候一个资源很大，如果用完马上移除的话就会出现卡顿
    public bool IsDel = false;
    /// <summary>
    /// 改变引用计数
    /// </summary>
    /// <param name="i">传入1或-1</param>
    public void ChangeRefCount(int i)
    {
        refCount += i;
        if (refCount < 0)
        {
            refCount = 0;
            Debug.LogError("引用计数小于零了，请检查引用与卸载是否配对");
        }
    }

}

public class ResourcesManager:SingleBehavior<ResourcesManager>
{
  
    //用于存储加载中的资源，和已经加载的资源
    private Dictionary<string, BaseResinfo> ResDic = new Dictionary<string, BaseResinfo>();

    /// <summary>
    /// 同步加载Resources文件夹下的资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).Name;
        ResInfo<T> Info;

        if (!ResDic.ContainsKey(resName))
        {
            //直接同步加载并且在字典中记录 
            T Res = Resources.Load<T>(path);
            Info = new ResInfo<T>();
            Info.Asset = Res; //将加载的资源赋值给资源信息对象
            ResDic.Add(resName, Info); //将资源信息对象添加到字典中
            Info.ChangeRefCount(1);
            return Info.Asset; //返回加载的资源
        }
        else
        {
            //这时候就是这个资源已经在字典中存在了
            Info = ResDic[resName] as ResInfo<T>;
            Info.ChangeRefCount(1);
            if (Info.Asset == null)
            {
                //停止异步加载，直接采用同步加载的方式
                MonoMange.Instance.StopCoroutine(Info.Coroutine);
                T Res = Resources.Load<T>(path);
                Info.Asset = Res; //将加载的资源赋值给资源信息对象
                Info.CallBack?.Invoke(Res); //调用回调函数
                Info.Coroutine = null; //清除协同程序引用
                Info.CallBack = null; //清除回调函数引用
                return Res;
            }
            else
            {
                return Info.Asset; //如果资源已经加载完毕,则直接返回这个资源
            }
        }
    }

    /// <summary>
    /// 异步加载资源的方法
    /// </summary>
    /// <typeparam name="T">资源的类型</typeparam>
    /// <param name="Path">在resources低下的文件路径</param>
    /// <param name="CallBack">加载结束后的回调函数，只有当加载完毕才会调用这个函数</param>
    public void LoadAsync<T>(string Path, UnityAction<T> CallBack) where T : UnityEngine.Object
    {
        //构建资源的唯一id,是由路径名_资源类型拼接而成的
        string resName = Path + "_" + typeof(T).Name;
        ResInfo<T> info;
        if (!ResDic.ContainsKey(resName))
        {
            //声明一个资源信息对象
            info = new ResInfo<T>();
            info.ChangeRefCount(1);
            ResDic.Add(resName, info);//将资源信息对象添加到字典中
            //记录委托函数一会加载完后就使用
            info.CallBack += CallBack;
            //开启协程进行异步加载,并且记录这个协程程序
            info.Coroutine = MonoMange.Instance.StartCoroutine(ReallyLoadAsync<T>(Path));
        }
        else
        {
            //如果字典中已经存在这个资源信息对象,则直接获取
            info = ResDic[resName] as ResInfo<T>;
            info.ChangeRefCount(1);
            //如果资源还没有加载完
            if (info.Asset == null)
                info.CallBack += CallBack; //记录回调函数
            else
                CallBack?.Invoke(info.Asset); //如果资源已经加载完毕,则直接调用回调函数

        }
        //通过协同程序加载异步资源 
    }
    private IEnumerator ReallyLoadAsync<T>(string Path) where T : UnityEngine.Object
    {
        ResourceRequest rq = Resources.LoadAsync<T>(Path);
        yield return rq;
        //资源加载结束,将资源传递给回调函数
        string resName = Path + "_" + typeof(T).Name;
        if (ResDic.ContainsKey(resName))
        {
            ResInfo<T> resInfo = ResDic[resName] as ResInfo<T>;
            resInfo.Asset = rq.asset as T; //将加载的资源赋值给资源信息对象
            if (resInfo.refCount == 0)
            {
                UnloadAsset<T>(Path); //如果标记为需要删除，则直接卸载资源
            }
            else
            {
                resInfo.CallBack?.Invoke(resInfo.Asset); //调用回调函数
                                                         //加载完毕后这些引用可以清空，防止出现内存泄漏
                resInfo.Coroutine = null; //清除协同程序引用
                resInfo.CallBack = null; //清除回调函数引用
            }
        }
    }

    /// <summary>
    /// 异步加载资源的方法
    /// </summary>
    /// <param name="Path">在resources低下的文件路径</param>
    /// <param name="CallBack">加载结束后的回调函数，只有当加载完毕才会调用这个函数</param>
    [Obsolete("注意最好还是使用泛型来加载资源，如果一定要用type进行加载就一定不能对这个资源混用加载")]
    public void LoadAsync(string Path, Type type, UnityAction<UnityEngine.Object> CallBack)
    {
        //构建资源的唯一id,是由路径名_资源类型拼接而成的
        string resName = Path + "_" + type.Name;
        ResInfo<UnityEngine.Object> info;
        if (!ResDic.ContainsKey(resName))
        {
            //声明一个资源信息对象
            info = new ResInfo<UnityEngine.Object>();
            ResDic.Add(resName, info);//将资源信息对象添加到字典中
            //记录委托函数一会加载完后就使用
            info.CallBack += CallBack;
            //开启协程进行异步加载,并且记录这个协程程序
            info.Coroutine = MonoMange.Instance.StartCoroutine(ReallyLoadAsync(Path, type));
        }
        else
        {
            //如果字典中已经存在这个资源信息对象,则直接获取
            info = ResDic[resName] as ResInfo<UnityEngine.Object>;
            //如果资源还没有加载完
            if (info.Asset == null)
                info.CallBack += CallBack; //记录回调函数
            else
                CallBack?.Invoke(info.Asset); //如果资源已经加载完毕,则直接调用回调函数

        }
        //通过协同程序加载异步资源 
    }
    private IEnumerator ReallyLoadAsync(string Path, Type type)
    {
        ResourceRequest rq = Resources.LoadAsync(Path, type);
        yield return rq;
        //资源加载结束,将资源传递给回调函数
        string resName = Path + "_" + type.Name;
        if (ResDic.ContainsKey(resName))
        {
            ResInfo<UnityEngine.Object> resInfo = ResDic[resName] as ResInfo<UnityEngine.Object>;
            resInfo.Asset = rq.asset; //将加载的资源赋值给资源信息对象

            resInfo.CallBack?.Invoke(resInfo.Asset); //调用回调函数
            //加载完毕后这些引用可以清空，防止出现内存泄漏
            if (resInfo.refCount == 0)
            {
                UnloadAsset(Path, type, resInfo.IsDel, null, true); //如果标记为需要删除，则直接卸载资源
            }
            else
            {
                resInfo.Coroutine = null; //清除协同程序引用
                resInfo.CallBack = null; //清除回调函数引用
            }
        }
    }

    /// <summary>
    /// 卸载Resources文件夹下的资源
    /// </summary>
    /// <param name="asset"></param>
    public void UnloadAsset<T>(string Path, bool IsDel = false, UnityAction<T> CallBack = null, bool isSub = false)
    {
        string resName = Path + "_" + typeof(T).Name;
        if (ResDic.ContainsKey(resName))
        {
            ResInfo<T> resInfo = ResDic[resName] as ResInfo<T>;
            resInfo.ChangeRefCount(-1);
            resInfo.IsDel = IsDel;
            if (resInfo.Asset != null && resInfo.refCount == 0 && resInfo.IsDel)
            {
                ResDic.Remove(resName); //从字典中移除这个资源信息对象
                Resources.UnloadAsset(resInfo.Asset as UnityEngine.Object); //卸载资源
            }
            else if (resInfo.Asset != null)//这里指的是资源正在加载中
            {
                if (CallBack != null)
                    resInfo.CallBack -= CallBack;
                // resInfo.IsDel = true; //标记为需要删除
            }
        }
    }
    public void UnloadAsset(string Path, Type type, bool IsDel = false, UnityAction<UnityEngine.Object> CallBack = null, bool isSub = false)
    {
        string resName = Path + "_" + type.Name;
        if (ResDic.ContainsKey(resName))
        {
            ResInfo<UnityEngine.Object> resInfo = ResDic[resName] as ResInfo<UnityEngine.Object>;
            if (isSub)
                resInfo.ChangeRefCount(-1);
            resInfo.IsDel = IsDel;
            if (resInfo.Asset != null && resInfo.refCount == 0 && resInfo.IsDel)
            {
                ResDic.Remove(resName); //从字典中移除这个资源信息对象
                Resources.UnloadAsset(resInfo.Asset); //卸载资源
            }
            else if (resInfo.Asset != null)//这里指的是资源正在加载中
            {
                if (CallBack != null)
                    resInfo.CallBack -= CallBack;
                // resInfo.IsDel = true; //标记为需要删除
            }
        }

    }
    /// <summary>
    /// 异步卸载未使用的资源
    /// </summary>
    /// <param name="CallBack"></param>
    /// <returns></returns>
    public void UnloadUnusedAssets(UnityAction CallBack)
    {
        MonoMange.Instance.StartCoroutine(UnloadUnusedAssetsCoroutine(CallBack));
    }

    private IEnumerator UnloadUnusedAssetsCoroutine(UnityAction CallBack)
    {
        //就是再真正的移除我们没有使用资源前，应该把我们自己记录的那些引用计数为零并且没有被移除的资源移除掉
        List<string> list = new List<string>();
        foreach (string path in ResDic.Keys)
        {
            if (ResDic[path].refCount == 0)
                list.Add(path);
        }
        foreach (string path in list)
        {
            ResDic.Remove(path);
        }
        AsyncOperation ao = Resources.UnloadUnusedAssets();
        yield return ao;
        CallBack();

    }
    public void ClearDic(UnityAction CallBack)
    {
        MonoMange.Instance.StartCoroutine(ReallyClearDic(CallBack));
    }

    private IEnumerator ReallyClearDic(UnityAction CallBack)
    {
        ResDic.Clear();
        AsyncOperation ao = Resources.UnloadUnusedAssets();
        yield return ao;
        CallBack();
    }
}
