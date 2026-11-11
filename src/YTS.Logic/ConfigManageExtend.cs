using System.IO;
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
                AutoStartWithWindows = true,
            };
        }

        public static AppConfig ReadConfigFileContent()
        {
            Encoding encoding = Encoding.UTF8;
            string file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "runConfig.json");
            FileInfo file = new(file_path);
            DirectoryInfo dire = file.Directory ?? throw new Exception("文件目录为空");
            if (!dire.Exists)
                dire.Create();
            ISerializer serializer = new TextJSONSerializer();
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
        public static void SaveConfigFileContent(AppConfig config)
        {
            Encoding encoding = Encoding.UTF8;
            string file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "runConfig.json");
            FileInfo file = new(file_path);
            DirectoryInfo dire = file.Directory ?? throw new Exception("文件目录为空");
            if (!dire.Exists)
                dire.Create();
            ISerializer serializer = new TextJSONSerializer();
            if (!file.Exists)
            {
                file.Create().Close();
            }
            string json = serializer.Serialize(config);
            File.WriteAllText(file.FullName, json, encoding);
        }
    }
}
