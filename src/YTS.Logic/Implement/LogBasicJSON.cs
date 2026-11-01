
using Newtonsoft.Json;

namespace YTS.Logic.Implement
{
    /// <summary>
    /// 实现类: 执行最基础最简单的JSON序列化打印日志方法
    /// </summary>
    public class LogBasicJSON : ILog
    {
        private const string null_message = "<null_message>";
        private readonly IPrint print;
        /// <summary>
        /// 实例化实现类
        /// </summary>
        /// <param name="print">打印对象</param>
        public LogBasicJSON(IPrint print)
        {
            this.print = print;
        }

        /// <inheritdoc />
        public virtual void Info(string message, params IDictionary<string, object>[] args)
        {
            print.PrintLines($"[Info] {message ?? null_message}, args: {ToJSON(args)}");
        }
        private string ToJSON(object args)
        {
            try
            {
                string args_json = JsonConvert.SerializeObject(args);
                return args_json;
            }
            catch (Exception ex)
            {
                return $"序列化参数出错: {ex.Message}";
            }
        }

        /// <inheritdoc />
        public virtual void Error(string message, params IDictionary<string, object>[] args)
        {
            print.PrintLines($"[Error] {message ?? null_message}, args: {ToJSON(args)}");
        }

        /// <inheritdoc />
        public virtual void Error(string message, Exception ex, params IDictionary<string, object>[] args)
        {
            if (ex == null)
            {
                Error(message, args);
                return;
            }

            IList<string> msglist = new List<string>();
            if (ex is ILogParamException logParamEx)
            {
                string msg = string.Join(", ", new string[]
                {
                    $"[ErrorException] {message ?? null_message}",
                    $"Exception: {ex.Message + ex.StackTrace}",
                    $"args: {ToJSON(args)}",
                    $"logParam: {ToJSON(logParamEx.GetParam())}",
                });
                msglist.Add(msg);
            }
            else
            {
                string msg = string.Join(", ", new string[]
                {
                    $"[ErrorException] {message ?? null_message}",
                    $"Exception: {ex.Message + ex.StackTrace}",
                    $"args: {ToJSON(args)}",
                });
                msglist.Add(msg);
            }
            Exception inex = ex.InnerException;
            while (inex != null)
            {
                if (inex is ILogParamException inlogParamEx)
                {
                    string msg = string.Join(", ", new string[]
                    {
                        $"[ErrorException.InnerException] {inex.Message}",
                        $"StackTrace: {inex.StackTrace}",
                        $"logParam: {ToJSON(inlogParamEx.GetParam())}",
                    });
                    msglist.Add(msg);
                }
                else
                {
                    string msg = string.Join(", ", new string[]
                    {
                        $"[ErrorException.InnerException] {inex.Message}",
                        $"StackTrace: {inex.StackTrace}",
                    });
                    msglist.Add(msg);
                }
                inex = inex.InnerException;
            }
            print.PrintLines(msglist.ToArray());
        }
    }
}
