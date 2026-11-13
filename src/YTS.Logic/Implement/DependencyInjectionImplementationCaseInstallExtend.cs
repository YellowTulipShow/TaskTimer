using Microsoft.Extensions.DependencyInjection;

namespace YTS.Logic.Implement
{
    /// <summary>
    /// 依赖注入实现实例安装扩展
    /// </summary>
    public static class DependencyInjectionImplementationCaseInstallExtend
    {
        public static void RegisterExecute(IServiceCollection services)
        {
            // 通用日志
            services.AddSingleton(s =>
            {
                FileInfo fileInfo;
                IPrint print;
                ILog log;

                fileInfo = ILogExtend.GetLogFilePath(string.Empty);
                print = new PrintFile(fileInfo, System.Text.Encoding.UTF8);
                log = new LogBasicJSON(print);

                return log;
            });

            // 注册: 回调管理工具注册
            services.AddTransient(typeof(ICallbackLibraryManage<>), typeof(CallbackLibraryManage<>));
        }

        /// <summary>
        /// 测试项目使用的注入
        /// </summary>
        public static void RegisterExecute_Test(IServiceCollection services)
        {
        }
    }
}
