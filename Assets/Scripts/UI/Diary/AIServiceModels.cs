/* AIServiceModels.cs
 * 存放所有 AI API (DeepSeek, DALL-E, etc.)
 * 的 JSON 响应/请求数据结构
 */
using System;

namespace AI.Models
{
    /* DeepSeek 流式响应数据结构 */
    [Serializable]
    public class StreamChunk
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public StreamChoice[] choices;
    }

    [Serializable]
    public class StreamChoice
    {
        public int index;
        public Delta delta;
        public string finish_reason;
    }

    [Serializable]
    public class Delta
    {
        public string role;
        public string content;
    }

    /* DeepSeek 原有的完整响应结构 */
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

    [Serializable]
    public class Choice
    {
        public int index;
        public Message message;
        public string finish_reason;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }
}