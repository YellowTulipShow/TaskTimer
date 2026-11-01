
namespace YTS.Logic
{
    /// <summary>
    /// 接口: 工具实例获取
    /// </summary>
    public interface IDIFactoryRead
    {
        /// <summary>
        /// 获取实例
        /// </summary>
        /// <typeparam name="T">实例类型</typeparam>
        T GetCase<T>() where T : class;
    }
}
