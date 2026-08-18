namespace YouDooSDK.Utils
{
public class Singleton<T>
    where T : new()
{
    protected static T _instance;
    protected static bool IsCreate = false;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (IsCreate == false)
            {
                CreateInstance();
            }

            return _instance;
        }
    }

    public static void CreateInstance()
    {
        if (IsCreate == true)
            return;

        lock (_lock)
        {
            if (IsCreate == true)
                return;

            IsCreate = true;
            _instance = new T();
        }
    }

    public static void ReleaseInstance()
    {
        lock (_lock)
        {
            _instance = default(T);
            IsCreate = false;
        }
    }
}
}
