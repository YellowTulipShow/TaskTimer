using System.Text;

using YTS.Logic.Models;

namespace YTS.Logic
{
    public static class ConfigManageExtend
    {
        private static AppConfig DefalutGet()
        {
            return new AppConfig()
            {
                Schedules = [],
                CloseToTray = true,
                LogMaxLineCount = 1000,
            };
        }

        private static readonly Encoding encoding = Encoding.UTF8;

        public static AppConfig ReadConfigFileContent(ISerializer serializer)
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
        public static void SaveConfigFileContent(AppConfig config, ISerializer serializer)
        {
            FileInfo file = GetConfigFileInfo();
            if (!file.Exists)
                file.Create().Close();
            string json = serializer.Serialize(config);
            File.WriteAllText(file.FullName, json, encoding);
        }
    }
}
