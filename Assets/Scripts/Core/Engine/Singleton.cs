/* Core/Engine/Singleton.cs
 * 单例模式基类，提供全局唯一的实例访问点
 * 确保特定管理器类在游戏中有且仅有一个实例
 */
using System;

/*
 * 单例模式基类，提供线程安全的单例实现
 */
public abstract class Singleton<T> where T : class, new()
{
    // 私有静态实例与锁对象（使用 volatile 确保多线程可见性）
    private static volatile T _instance;
    private static readonly object _syncRoot = new object();
    private static bool _initialized = false;

    /* 获取单例实例 */
    public static T Instance
    {
        get
        {
            // 双重检查锁定以减少锁开销
            if (_instance == null)
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                    {
                        _instance = new T();

                        // 如果 T 实际继承自 Singleton<T>，则调用初始化方法
                        // 使用 pattern matching 保证安全调用
                        if (_instance is Singleton<T> asSingleton)
                        {
                            try
                            {
                                asSingleton.Initialize();
                                _initialized = true;
                            }
                            catch (Exception)
                            {
                                // 初始化失败时清理实例，避免不一致状态
                                _instance = null;
                                _initialized = false;
                                throw;
                            }
                        }
                        else
                        {
                            // 若 T 未继承自 Singleton<T>，仍返回构造的实例（兼容性处理）
                            _initialized = true;
                        }
                    }
                }
            }
            return _instance;
        }
    }

    /* 初始化单例实例（子类可重写以添加自定义初始化逻辑） */
    protected virtual void Initialize()
    {
        // 执行初始化逻辑
        // 注册事件监听器
        // 设置初始状态
    }

    /* 销毁单例实例（子类可重写以清理资源） */
    public virtual void Dispose()
    {
        lock (_syncRoot)
        {
            // 子类清理逻辑应在重写中执行，然后调用 base.Dispose()
            _instance = null;
            _initialized = false;
        }
    }

    /* 检查单例是否已初始化 */
    public static bool IsInitialized()
    {
        return _initialized && _instance != null;
    }
}