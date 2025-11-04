/* Core/Engine/Singleton.cs
 * 适用于 Unity MonoBehaviour 的泛型单例模式基类
 * 提供线程安全的单例实现，支持自动创建和持久化选项
 * 使用方法:
 * 1. 让你的管理器类继承自 Singleton<T>，例如: public class YourManager : Singleton<YourManager> { ... }
 * 2. 在场景中创建一个空 GameObject，并将 YourManager.cs 脚本挂载上去
 * 3. 在任何其他脚本中，通过 YourManager.Instance 来访问该单例实例
 * 特点:
 * - 自动在场景中查找实例，如果找不到则会自动创建一个新的 GameObject 并挂载脚本
 * - 保证场景中只有一个实例存在，防止重复创建
 * - 提供一个持久化选项，使得单例可以在切换场景时不被销毁
 * - 线程安全
 */

using UnityEngine;

/*
 * Unity MonoBehaviour 泛型单例基类
 * 提供线程安全的单例实现，支持自动实例化和持久化
 */
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    // 添加一个静态标志，用于标记程序是否正在退出
    private static bool _isQuitting = false;

    [Tooltip("如果勾选，该单例将在场景加载时保留。")]
    [SerializeField]
    private bool _persistent = true;

    public static T Instance
    {
        get
        {
            // 如果程序正在退出，任何对实例的访问都应返回 null
            if (_isQuitting)
            {
                //Debug.LogWarning($"[{typeof(T).Name}]: 实例已被销毁，无法在应用退出时访问。");
                return null;
            }

            lock (_lock)
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindFirstObjectByType<T>();

                if (_instance != null)
                {
                    // (省略了重复检查的代码，保持与你原版一致)
                    return _instance;
                }
                
                // 在创建新实例前，再次检查是否正在退出
                // (这可以防止在多线程中出现竞态条件)
                if (_isQuitting)
                {
                    return null;
                }

                GameObject singletonObject = new GameObject();
                _instance = singletonObject.AddComponent<T>();
                singletonObject.name = $"{typeof(T).Name} (Singleton)";
                
                DontDestroyOnLoad(singletonObject);
                
                Debug.Log($"[{typeof(T).Name}]: 实例被创建。");
                
                return _instance;
            }
        }
    }

    /*
     * 单例初始化方法
     * 确保场景中只有一个实例存在，处理重复实例的销毁
     */
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            if (_persistent)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"场景中已存在 {typeof(T).Name} 的实例。销毁重复的 '{gameObject.name}'。");
            Destroy(gameObject);
        }
    }

    /*
     * 当单例实例被销毁时，设置退出标志
     */
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _isQuitting = true;
        }
    }

    /*
     * 当应用程序退出时，设置退出标志
     * OnApplicationQuit 会在所有 OnDestroy 之前被调用
     */
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}