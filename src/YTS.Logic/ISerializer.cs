using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace YTS.Logic
{
    public interface ISerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string content);
    }

    public class TextJSONSerializer : ISerializer
    {
        private readonly JsonSerializerOptions jsonSerializerOptions;
        public TextJSONSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
        }
        public TextJSONSerializer()
        {
            jsonSerializerOptions = new JsonSerializerOptions()
            {
                // 整齐打印
                WriteIndented = true,
                IndentCharacter = ' ',
                IndentSize = 4,
                // 设置Json字符串支持的编码，默认情况下，序列化程序会转义所有非 ASCII 字符。 即，会将它们替换为 \uxxxx，其中 xxxx 为字符的 Unicode
                // 代码。 可以通过设置Encoder来让生成的josn字符串不转义指定的字符集而进行序列化 下面指定了基础拉丁字母和中日韩统一表意文字的基础Unicode 块
                // (U+4E00-U+9FCC)。 基本涵盖了除使用西里尔字母以外所有西方国家的文字和亚洲中日韩越的文字
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs),
                // 忽略只读属性，因为只读属性只能序列化而不能反序列化，所以在以json为储存数据的介质的时候，序列化只读属性意义不大
                IgnoreReadOnlyFields = true,
                // 允许结尾有逗号的不标准json
                AllowTrailingCommas = true,
                // 允许有注释的不标准json
                ReadCommentHandling = JsonCommentHandling.Skip,
                // 允许在反序列化的时候原本应为数字的字符串（带引号的数字）转为数字
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                // 处理循环引用类型，比如Book类里面有一个属性也是Book类
                //ReferenceHandler = ReferenceHandler.Preserve,
            };
        }

        public T Deserialize<T>(string content)
        {
            return JsonSerializer.Deserialize<T>(content, jsonSerializerOptions)
                ?? throw new Exception("反序列化值为空");
        }

        public string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, jsonSerializerOptions);
        }
    }
}
