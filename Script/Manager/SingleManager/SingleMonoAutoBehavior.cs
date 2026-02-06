using UnityEngine;

/// <summary>
/// 自动单例基类：优先使用场景中预放的实例，无则在调用Instance时自动创建
/// </summary>
/// <typeparam name="T">继承MonoBehaviour的单例类型</typeparam>
public class SingleMonoAutoBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _isDestroyed = false; // 标记单例是否被永久销毁

    /// <summary>
    /// 单例实例访问入口
    /// </summary>
    public static T Instance
    {
        get
        {
            // 已销毁则直接返回null，避免重复创建
            if (_isDestroyed)
            {
                Debug.LogWarning($"[{typeof(T).Name}] 单例已被销毁，无法获取实例！");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 优先查找场景中所有实例
                    T[] sceneInstances = FindObjectsOfType<T>(includeInactive: true);

                    if (sceneInstances.Length > 0)
                    {
                        // 场景中有实例，取第一个
                        _instance = sceneInstances[0];
                        // 确保场景实例标记为跨场景保留
                        DontDestroyOnLoad(_instance.gameObject);

                        // 销毁多余的场景实例，保证唯一性
                        for (int i = 1; i < sceneInstances.Length; i++)
                        {
                            Debug.LogWarning($"[{typeof(T).Name}] 场景中存在多个实例，已销毁重复实例！");
                            Destroy(sceneInstances[i].gameObject);
                        }

                        Debug.Log($"[{typeof(T).Name}] 已使用场景中预放的实例");
                    }
                    else
                    {
                        // 场景无实例，自动创建并标记跨场景保留
                        GameObject singletonObj = new GameObject(typeof(T).Name + " (Auto Singleton)");
                        _instance = singletonObj.AddComponent<T>();
                        DontDestroyOnLoad(singletonObj);
                        Debug.Log($"[{typeof(T).Name}] 场景中无预放实例，已自动创建单例");
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// 场景实例Awake时校验唯一性
    /// </summary>
    protected virtual void Awake()
    {
        // 若_instance未初始化，当前场景实例作为单例
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[{typeof(T).Name}] 场景实例初始化完成，作为单例使用");
        }
        // 若已有单例且不是当前实例，销毁当前实例
        else if (_instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 检测到重复实例，销毁当前场景实例");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 单例销毁时标记状态，防止重复创建
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 仅当当前实例是全局单例时，标记销毁状态
        if (_instance == this)
        {
            _isDestroyed = true;
            _instance = null;
            Debug.Log($"[{typeof(T).Name}] 单例已销毁");
        }
    }

    /// <summary>
    /// 仅当实例是当前单例时，才重置销毁标记
    /// </summary>
    protected virtual void OnEnable()
    {
        if (_instance == this)
        {
            _isDestroyed = false;
        }
    }
}