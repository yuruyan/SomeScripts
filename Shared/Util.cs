namespace Shared;

public static class Utils {
    /// <summary>
    /// 简化 try 代码块
    /// </summary>
    /// <param name="task"></param>
    public static void Try(Action task) {
        try {
            task();
        } catch { }
    }

    /// <summary>
    /// 简化 try 代码块
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <param name="defaultValue">发生异常时的返回值</param>
    /// <returns></returns>
    public static T? Try<T>(Func<T> task, T? defaultValue = default) {
        try {
            return task();
        } catch {
            return defaultValue ?? default;
        }
    }

}
