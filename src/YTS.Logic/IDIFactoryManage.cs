
namespace YTS.Logic
{
    /// <summary>
    /// 枚举: 依赖注入工厂注册类型
    /// </summary>
    public enum EDIFactoryRegisterType
    {
        /// <summary>
        /// 瞬时 - 每次请求时都创建新实例
        /// </summary>
        Transient = 1,

        /// <summary>
        /// 单例 - 应用启动后首次请求时创建，之后全局共享同一个实例
        /// </summary>
        Singleton = 2,

        /// <summary>
        /// 作用域 - 在同一个作用域内共享一个实例
        /// </summary>
        Scoped = 3,
    }

    /// <summary>
    /// 接口: 实例注入
    /// </summary>
    public interface IDIFactoryManage : IDIFactoryRead
    {
        /// <summary>
        /// 注册.类型指定
        /// </summary>
        /// <typeparam name="TInterface">接口</typeparam>
        /// <typeparam name="TClass">实例</typeparam>
        void Register<TInterface, TClass>(EDIFactoryRegisterType type)
            where TInterface : class
            where TClass : TInterface;

        /// <summary>
        /// 注册.动态实例
        /// </summary>
        /// <typeparam name="TInterface">接口</typeparam>
        /// <param name="factoryMethod">动态实例执行方法</param>
        void Register<TInterface>(EDIFactoryRegisterType type, Func<TInterface> factoryMethod)
            where TInterface : class;
    }
}
