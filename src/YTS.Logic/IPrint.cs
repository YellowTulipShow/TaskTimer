namespace YTS.Logic
{
    /// <summary>
    /// 接口: 打印接口对象
    /// </summary>
    public interface IPrint
    {
        /// <summary>
        /// 打印输出多行内容
        /// </summary>
        /// <param name="msglist">需要打印的多行消息列表</param>
        void PrintLines(params string[] msglist);
    }
}
