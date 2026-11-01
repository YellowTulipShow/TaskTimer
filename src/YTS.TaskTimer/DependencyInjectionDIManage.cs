using Microsoft.Extensions.DependencyInjection;
using YTS.Logic;

namespace YTS.TaskTimer;

class DependencyInjectionDIManage : IDIFactoryManage
{
    /// <summary>
    /// 用于注入配置
    /// </summary>
    private readonly IServiceCollection service;
    /// <summary>
    /// 用于获取实例
    /// </summary>
    private IServiceProvider provider;

    public DependencyInjectionDIManage(IServiceCollection service)
    {
        this.service = service;
    }

    public void SetIServiceProvider(IServiceProvider provider)
    {
        this.provider = provider;
    }
    public T GetCase<T>() where T : class
    {
        return provider.GetRequiredService<T>();
    }

    public void Register<TInterface, TClass>(EDIFactoryRegisterType type)
        where TInterface : class
        where TClass : TInterface
    {
        switch (type)
        {
            case EDIFactoryRegisterType.Transient:
                service.AddTransient(typeof(TInterface), typeof(TClass));
                break;
            case EDIFactoryRegisterType.Singleton:
                service.AddSingleton(typeof(TInterface), typeof(TClass));
                break;
            case EDIFactoryRegisterType.Scoped:
                service.AddScoped(typeof(TInterface), typeof(TClass));
                break;
        }
    }

    public void Register<TInterface>(EDIFactoryRegisterType type, Func<TInterface> factoryMethod) where TInterface : class
    {
        switch (type)
        {
            case EDIFactoryRegisterType.Transient:
                service.AddTransient((d) =>
                {
                    return factoryMethod.Invoke();
                });
                break;
            case EDIFactoryRegisterType.Singleton:
                service.AddSingleton((d) =>
                {
                    return factoryMethod.Invoke();
                });
                break;
            case EDIFactoryRegisterType.Scoped:
                service.AddScoped((d) =>
                {
                    return factoryMethod.Invoke();
                });
                break;
        }
    }
}
