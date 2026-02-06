using UnityEngine;
using UnityEngine.Events;

public class MonoMange : SingleMonoAutoBehavior<MonoMange>
{

    #region Start事件注册,更新,移除
    private UnityAction OnStartAction;

    private void Start()
    {
        OnStartAction?.Invoke();
    }
    /// <summary>
    /// 在Start函数中添加函数
    /// </summary>
    /// <param name="OnStartAction"></param>
    public void AddLister_Start(UnityAction OnStartAction)
    {
        Debug.Log("添加Update监听");
        this.OnStartAction += OnStartAction;
    }
    /// <summary>
    /// 在Start函数中移除函数
    /// </summary>
    /// <param name="StartAction"></param>
    public void RemoveLister_Start(UnityAction OnStartAction)
    {
        this.OnStartAction -= OnStartAction;
    }
    #endregion

    #region Update事件注册,更新,移除

    public UnityAction UpdateAction;
    /// <summary>
    /// 在Update函数中添加函数
    /// </summary>
    /// <param name="UpdateAction"></param>
    public void AddLister_Update(UnityAction UpdateAction)
    {
        Debug.Log("添加Update监听");
        this.UpdateAction += UpdateAction;
    }

    private void Update()
    {
        UpdateAction?.Invoke();
    }

    /// <summary>
    /// 在Update函数中移除函数
    /// </summary>
    /// <param name="UpdateAction"></param>
    public void RemoveLister_Update(UnityAction UpdateAction)
    {
        this.UpdateAction -= UpdateAction;
    }
    #endregion

    #region FixedUpdate事件注册,更新,移除
    private UnityAction FixedUpdateAction;
    /// <summary>
    /// 在LateUpdate函数中添加函数
    /// </summary>
    /// <param name="FixedUpdateAction"></param>
    public void AddLister_FixedUpdate(UnityAction FixedUpdateAction)
    {
        this.FixedUpdateAction += FixedUpdateAction;
    }

    private void FixedUpdate()
    {
        FixedUpdateAction?.Invoke();
    }

    /// <summary>
    /// 在LateUpdate函数中移除函数
    /// </summary>
    /// <param name="FixedUpdateAction"></param>
    public void RemoveLister_FixedUpdate(UnityAction FixedUpdateAction)
    {
        this.FixedUpdateAction -= FixedUpdateAction;
    }

    #endregion

    #region LateUpdate事件注册,更新,移除
    private UnityAction LateUpdateAction;

    /// <summary>
    /// 在LateUpdate函数中添加函数
    /// </summary>
    /// <param name="LateUpdateAction"></param>
    public void AddLister_LateUpdate(UnityAction LateUpdateAction)
    {
        this.LateUpdateAction += LateUpdateAction;
    }

    private void LateUpdate()
    {
        LateUpdateAction?.Invoke();
    }


    /// <summary>
    /// 在LateUpdate函数中移除函数
    /// </summary>
    /// <param name="LateUpdateAction"></param>
    public void RemoveLister_LateUpdate(UnityAction LateUpdateAction)
    {
        this.LateUpdateAction -= LateUpdateAction;
    }

    #endregion

    #region OnDestroy事件注册,更新,移除

    private UnityAction OnDestroyAction;
    public void AddLister_OnDestroy(UnityAction _OnDestroyAction)
    {
        OnDestroyAction += _OnDestroyAction;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        OnDestroyAction?.Invoke();
    }

    public void RemoveLister_OnDestroy(UnityAction _OnDestroyAction)
    {
        OnDestroyAction -= _OnDestroyAction;
    }

    #endregion

    #region 提供初始化预制体
    public GameObject InitPrefab(string Name)
    {
        GameObject prefab = Resources.Load<GameObject>(Name);
        if (prefab == null)
        {
            Debug.LogError($"未找到预制体{Name}，请检查路径是否正确");
            return null;
        }
        GameObject instance = Instantiate(prefab);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    public GameObject iniPrefab(GameObject obj)
    {
        return Instantiate(obj);
    }
    #endregion
}
