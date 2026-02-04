namespace Taschengeld_3;

public static class ServiceHelper
{
    private static IServiceProvider? _serviceProvider;

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T? GetService<T>()
    {
        return (T?)_serviceProvider?.GetService(typeof(T));
    }
}


