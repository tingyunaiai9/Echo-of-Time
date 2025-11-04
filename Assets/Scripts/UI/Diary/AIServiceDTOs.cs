/* UI/Diary/AIServiceDTOs.cs
 * AI服务数据传输对象定义文件
 * 存放所有 AI API (DeepSeek, DALL-E, etc.) 的 JSON 响应/请求数据结构
 * 提供序列化和反序列化支持，用于网络通信和数据解析
 */
using System;

namespace AI.DTOs
{
    /*
     * DeepSeek 流式响应数据结构
     * 用于处理流式聊天API返回的数据块
     */
    [Serializable]
    public class StreamChunk
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public StreamChoice[] choices;
    }

    /*
     * 流式响应中的选择项数据结构
     * 包含索引、增量内容和完成状态
     */
    [Serializable]
    public class StreamChoice
    {
        public int index;
        public Delta delta;
        public string finish_reason;
    }

    /*
     * 增量内容数据结构
     * 存储流式响应中每个数据块的内容变化
     */
    [Serializable]
    public class Delta
    {
        public string role;
        public string content;
    }

    /*
     * DeepSeek 完整响应数据结构
     * 用于处理非流式API调用的完整响应
     */
    [Serializable]
    public class DeepSeekResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public Choice[] choices;
        public Usage usage;
    }

    /*
     * 完整响应中的选择项数据结构
     * 包含完整的消息内容和状态信息
     */
    [Serializable]
    public class Choice
    {
        public int index;
        public Message message;
        public string finish_reason;
    }

    /*
     * 消息内容数据结构
     * 存储角色和具体的文本内容
     */
    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    /*
     * API使用统计数据结构
     * 记录token使用情况和计费信息
     */
    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }
}