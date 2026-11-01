namespace YTS.Logic.Implement
{
    /// <summary>
    /// 依赖注入实现实例安装扩展
    /// </summary>
    public static class DependencyInjectionImplementationCaseInstallExtend
    {
        public static void RegisterExecute(IDIFactoryManage ci)
        {
            // 通用日志
            ci.Register(EDIFactoryRegisterType.Singleton, () =>
            {
                FileInfo fileInfo;
                IPrint print;
                ILog log;

                fileInfo = ILogExtend.GetLogFilePath(string.Empty);
                print = new PrintFile(fileInfo, System.Text.Encoding.UTF8);
                log = new LogBasicJSON(print);

                return log;
            });
        }

        /// <summary>
        /// 测试项目使用的注入
        /// </summary>
        public static void RegisterExecute_Test(IDIFactoryManage ci)
        {
        }
    }
}
