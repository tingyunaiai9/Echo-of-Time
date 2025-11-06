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

    /*
     * =======================================================
     * 即梦 AI v3.1 (Jimeng v3.1) / 火山引擎 (VolcEngine)
     * =======================================================
     */

    // ---- 1. 提交任务 (Submit Task) DTOs ----

    /* 提交任务 请求体 (Body) */
    [Serializable]
    public class JimengSubmitBody
    {
        public string req_key;
        public string prompt;
        public bool use_pre_llm;
        public int seed;
        public int width;
        public int height;
    }

    /* Tijiao 任务 响应体 (Response) */
    [Serializable]
    public class JimengSubmitResponse
    {
        public int code;
        public JimengSubmitData data;
        public string message;
        public string request_id;
    }

    /* 提交任务 响应数据 (用于获取 task_id) */
    [Serializable]
    public class JimengSubmitData
    {
        public string task_id;
    }

    // ---- 2. 查询任务 (Query Task) DTOs ----

    /* 查询任务 请求体 (Body) */
    [Serializable]
    public class JimengQueryBody
    {
        public string req_key;
        public string task_id;
        public string req_json; // 这是一个被序列化为字符串的JSON对象
    }

    /* 用于生成 req_json 字符串的辅助类 */
    [Serializable]
    public class JimengQueryReqJson
    {
        public bool return_url;
        public LogoInfo logo_info;
        public AIGCMeta aigc_meta;
    }
    
    // (LogoInfo 和 AIGCMeta 与上一版文档相同，可以保留)
    [Serializable]
    public class LogoInfo
    {
        public bool add_logo;
        public int position;
        public int language;
        public float opacity;
    }

    [Serializable]
    public class AIGCMeta
    {
        public string producer_id;
    }

    /* 查询任务 响应体 (Response) */
    [Serializable]
    public class JimengQueryResponse
    {
        public int code;
        public JimengQueryData data;
        public string message;
        public string request_id;
    }

    /* 查询任务 响应数据 (用于获取 status 和 urls) */
    [Serializable]
    public class JimengQueryData
    {
        public string status; // 关键字段: "in_queue", "generating", "done"
        public List<string> image_urls;
        // public List<string> binary_data_base64;
    }
}