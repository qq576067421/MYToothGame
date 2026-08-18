using System;
using UnityEngine;

namespace YouDooSDK.Utils
{
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 线程锁
    private static readonly object lockObject = new object();

    // 单例实例·
    private static T instance;

    // 是否应用程序正在退出
    private static bool isApplicationQuitting = false;

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static T Instance
    {
        get
        {
            // 应用退出时不再创建新实例
            if (isApplicationQuitting)
            {
                Debug.LogWarning($"[MonoSingleton] 应用程序正在退出，无法获取 {typeof(T)} 的实例。");
                return null;
            }

            lock (lockObject)
            {
                if (instance == null)
                {
                    // 在场景中查找已存在的实例
                    instance = GameObject.FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        // 创建新的单例对象
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        instance = singletonObject.AddComponent<T>();

                        // 确保单例在场景切换时不被销毁
                        DontDestroyOnLoad(singletonObject);

                        Debug.Log($"[MonoSingleton] 创建 {typeof(T)} 的单例实例。");
                    }
                    else
                    {
                        // 确保已存在的实例不被销毁
                        DontDestroyOnLoad(instance.gameObject);
                    }
                }

                return instance;
            }
        }
    }

    /// <summary>
    /// 是否已初始化
    /// </summary>
    public static bool IsInitialized => instance != null;

    /// <summary>
    /// 唤醒方法，用于初始化
    /// </summary>
    void Awake()
    {
        // 防止重复实例
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);

            // 可选：初始化操作
            OnAwake();
        }
        else if (instance != this)
        {
            Debug.LogWarning($"[MonoSingleton] 检测到 {typeof(T)} 的重复实例。销毁新实例。");
            Destroy(gameObject);
        }
    }

    

    /// <summary>
    /// 应用程序退出时的处理
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    /// <summary>
    /// 销毁时的处理
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 只有当被销毁的是当前实例时才清空引用
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 可重写的初始化方法
    /// </summary>
    protected virtual void OnAwake()
    {
        // 子类可重写此方法实现初始化逻辑
    }

}
}
