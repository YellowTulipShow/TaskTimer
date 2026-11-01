
namespace YTS.Logic
{
    /// <summary>
    /// 接口: 开机自启动管理工具
    /// </summary>
    public interface IPowerOnStartup
    {
        /// <summary>
        /// 当前是否已配置开机自启
        /// </summary>
        /// <returns>查询结果</returns>
        bool IsAutoStartEnabled();
        /// <summary>
        /// 启用
        /// </summary>
        /// <returns>执行结果结果</returns>
        bool Enable();
        /// <summary>
        /// 禁用
        /// </summary>
        /// <returns>执行结果</returns>
        bool Disable();
    }
}
