/*
 * Core/Engine/Singleton.cs
 * * 适用于 Unity MonoBehaviour 的泛型单例模式基类。
 * * 使用方法:
 * 1. 让你的管理器类继承自 Singleton<T>，例如: public class YourManager : Singleton<YourManager> { ... }
 * 2. 在场景中创建一个空 GameObject，并将 YourManager.cs 脚本挂载上去。
 * 3. 在任何其他脚本中，通过 YourManager.Instance 来访问该单例实例。
 * * 特点:
 * - 自动在场景中查找实例，如果找不到则会自动创建一个新的 GameObject 并挂载脚本。
 * - 保证场景中只有一个实例存在，防止重复创建。
 * - 提供一个持久化选项，使得单例可以在切换场景时不被销毁。
 * - 线程安全。
 */

using UnityEngine;

/// <summary>
/// 一个适用于 MonoBehaviour 的泛型单例基类。
/// 继承此类将确保在整个游戏生命周期中只有一个该类型的实例存在。
/// </summary>
/// <typeparam name="T">要实现单例模式的组件类型。</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 私有静态实例与锁对象，用于线程安全
    private static T _instance;
    private static readonly object _lock = new object();
    
    // 控制单例是否在场景切换时持久化
    [Tooltip("如果勾选，该单例将在场景加载时保留。")]
    [SerializeField]
    private bool _persistent = true;

    /// <summary>
    /// 获取单例实例的公共访问点。
    /// </summary>
    public static T Instance
    {
        get
        {
            // 在多线程环境中锁定，防止多个线程同时创建实例
            lock (_lock)
            {
                // 如果实例已经存在，直接返回
                if (_instance != null)
                {
                    return _instance;
                }

                // 如果实例不存在，尝试在当前场景中查找该类型的对象
                _instance = FindObjectOfType<T>();

                // 如果场景中找到了实例
                if (_instance != null)
                {
                    // 检查是否还有其他同类型的实例，这是冗余保护，通常不应发生
                    var others = FindObjectsOfType<T>();
                    if (others.Length > 1)
                    {
                        Debug.LogError($"场景中存在多个 {typeof(T).Name} 的实例，这是不允许的。请检查你的场景设置。");
                        // 销毁其他实例，只保留找到的第一个
                        for (int i = 1; i < others.Length; i++)
                        {
                            Destroy(others[i].gameObject);
                        }
                    }
                    return _instance;
                }

                // 如果场景中找不到实例，则动态创建一个新的GameObject，并把组件挂载上去
                GameObject singletonObject = new GameObject();
                _instance = singletonObject.AddComponent<T>();
                singletonObject.name = $"{typeof(T).Name} (Singleton)";

                // 这个动态创建的实例默认是持久化的
                DontDestroyOnLoad(singletonObject);
                
                Debug.Log($"[{typeof(T).Name}]: 实例被创建。");

                return _instance;
            }
        }
    }

    /// <summary>
    /// Unity 的生命周期函数，在对象加载时调用。
    /// 这是确保单例模式正确执行的关键。
    /// </summary>
    protected virtual void Awake()
    {
        // 检查是否已经有实例存在
        if (_instance == null)
        {
            // 如果没有，将自己赋值给静态实例
            _instance = this as T;

            // 根据 _persistent 字段决定是否在加载新场景时保留此对象
            if (_persistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            // 如果实例已存在且不是当前这个，说明场景中存在重复的实例。
            // 销毁当前这个重复的 GameObject，以保证单例的唯一性。
            Debug.LogWarning($"场景中已存在 {typeof(T).Name} 的实例。销毁重复的 '{gameObject.name}'。");
            Destroy(gameObject);
        }
    }
}