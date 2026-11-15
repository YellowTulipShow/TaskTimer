using System.Text;

using YTS.Logic.Models;

namespace YTS.Logic
{
    /// <summary>
    /// 接口: 配置文件管理
    /// </summary>
    public interface IConfigManage<T>
    {
        /// <summary>
        /// 读取配置
        /// </summary>
        T Read();

        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="appConfig">配置内容</param>
        void Save(T appConfig);
    }

    /// <summary>
    /// 接口: 配置文件管理
    /// </summary>
    public interface IAppConfigManage : IConfigManage<AppConfig>
    {
    }

    /// <summary>
    /// 实现类: JSON格式的配置文件管理工具
    /// </summary>
    public class AppConfigManage : IAppConfigManage
    {
        private readonly ISerializer serializer;
        public AppConfigManage(ISerializer serializer)
        {
            this.serializer = serializer;
        }
        private static AppConfig DefalutGet()
        {
            return new AppConfig()
            {
                Schedules = [],
                System = new AppConfigSystem()
                {
                    IsDebugTest = false,
                    CloseToTray = true,
                },
                LogConfig = new AppConfigLogControl()
                {
                    LogMaxLineCount = 100,
                    IsOuputDebugLog = false,
                },
            };
        }

        private static readonly Encoding encoding = Encoding.UTF8;

        public AppConfig Read()
        {
            FileInfo file = GetConfigFileInfo();
            AppConfig defaultConfigs = DefalutGet();
            if (!file.Exists)
            {
                file.Create().Close();
                string json = serializer.Serialize(defaultConfigs);
                File.WriteAllText(file.FullName, json, encoding);
                return defaultConfigs;
            }
            string content = File.ReadAllText(file.FullName, encoding);
            if (string.IsNullOrEmpty(content))
            {
                string json = serializer.Serialize(defaultConfigs);
                File.WriteAllText(file.FullName, json, encoding);
                return defaultConfigs;
            }
            AppConfig config = serializer.Deserialize<AppConfig>(content);
            return config;
        }
        private static FileInfo GetConfigFileInfo()
        {
            string file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runConfig.json");
            FileInfo file = new(file_path);
            DirectoryInfo dire = file.Directory ?? throw new Exception("文件目录为空");
            if (!dire.Exists)
                dire.Create();
            return file;
        }
        public void Save(AppConfig config)
        {
            FileInfo file = GetConfigFileInfo();
            if (!file.Exists)
                file.Create().Close();
            string json = serializer.Serialize(config);
            File.WriteAllText(file.FullName, json, encoding);
        }
    }

    public class AppConfigManageCache : IAppConfigManage
    {
        private readonly object objlock = new();
        private AppConfig? config;
        private readonly IAppConfigManage configManage;
        public AppConfigManageCache(IAppConfigManage configManage)
        {
            this.configManage = configManage;
            config = null;
        }

        public AppConfig Read()
        {
            lock (objlock)
            {
                if (config == null)
                {
                    AppConfig c = configManage.Read();
                    config = c;
                }
                return config;
            }
        }

        public void Save(AppConfig appConfig)
        {
            lock (objlock)
            {
                config = appConfig;
            }
            configManage.Save(config);
        }
    }
}
