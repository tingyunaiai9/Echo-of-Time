/* UI/Diary/DeepSeekService.cs
 * DeepSeek 聊天服务调用封装
 * 独立负责处理 DeepSeek Chat API 的所有网络请求，包含连接预热、流式请求和错误处理
 * 提供静态方法调用 AI 服务，支持流式响应回调机制
 */
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AI.DTOs; // 引入新的模型命名空间
using Newtonsoft.Json;
using UnityEngine;

/*
 * DeepSeek AI 服务静态类，提供流式聊天 API 调用功能
 * 负责封装请求体构造、SSE 流解析和错误回调
 */
public static class DeepSeekService
{
    // API 配置
    private const string DeepSeekApiUrl = "https://api.deepseek.com/v1/chat/completions";
    // private const string ApiKey = ""; // 您的密钥

    // 静态共享的 HttpClient 实例
    private static readonly HttpClient s_sharedClient;

    /*
     * 静态构造函数，在类首次被访问时自动调用
     * 初始化HttpClient并进行连接预热
     */
    static DeepSeekService()
    {
        s_sharedClient = new HttpClient();
        // 使用 ApiKeyManager.DeepSeekKey
        if (string.IsNullOrEmpty(ApiKeyManager.DeepSeekKey))
        {
            Debug.LogError("[DeepSeekService] DeepSeekKey 为空！请检查 api_keys.json。");
            return;
        }
        
        s_sharedClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKeyManager.DeepSeekKey}");
        s_sharedClient.Timeout = System.TimeSpan.FromMinutes(2);

        // 预热连接 (在后台线程启动，不阻塞主线程)
        Task.Run(async () =>
        {
            try
            {
                Debug.Log("[DeepSeekService] 开始预热连接...");
                var warmupBody = new
                {
                    model = "deepseek-chat",
                    messages = new[] { new { role = "user", content = "hi" } },
                    max_tokens = 1,
                    stream = false
                };
                string jsonBody = JsonConvert.SerializeObject(warmupBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                await s_sharedClient.PostAsync(DeepSeekApiUrl, content);
                Debug.Log("[DeepSeekService] 连接预热完成");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeepSeekService] 连接预热失败: {ex.Message}");
            }
        });
    }

    /*
     * 调用 DeepSeek API，通过回调函数返回流式数据
     * 支持实时接收AI响应内容，适用于聊天对话场景
     * @param name="prompt">用户输入的提示词</param>
     * @param name="onChunkReceived">每收到一个数据块时调用 (string: deltaContent)</param>
     * @param name="onStreamEnd">流式传输正常结束时调用</param>
     * @param name="onError">发生错误时调用 (string: errorMessage)</param>
     */
    public static async Task GetChatCompletionStreaming(
        string prompt,
        int timeline,
        Action<string> onChunkReceived,
        Action onStreamEnd,
        Action<string> onError)
    {
        try
        {
            string systemPrompt;
            switch (timeline)
            {
                case 0:
                    systemPrompt = "你现在扮演一位古代文人。重要规则（请严格遵守）：1. 你将收到一条“线索含义”，此线索只用于启发，不得直接出现，但可以适当的改写2. 不得引用原诗，而是运用相似的意境或描绘类似的场景。3. 输出诗体不限。4. 诗中需采用自然景物意象进行“间接表达”该线索的意境，让人能通过联想得到线索。5. 语言为文言，不得使用现代词汇，不要解释，不要注释，不要添加额外文本。";
                    break;
                case 2:
                    systemPrompt = "你现在扮演一位来自未来的量子考古学信息分析 AI。你绝不可以直接给出答案，而是要将我给你的线索编码为一个 JSON 对象。其中必须包含：-一个看似无意义的 \"raw_signal\"- 一个模糊但可推理的 \"decoded_hint\"- 一个与背景无关的干扰字段要求：1.输出必须是 **合法 JSON**2.不允许出现中文3.不允许直接写出答案4. \"decoded_hint\" 字段中应保留与答案相关的指向信息";
                    break;
                default:
                    systemPrompt = "你是一个友好的 AI 助手，帮助用户解答问题。";
                    break;
            }
            
            Debug.Log($"[DeepSeekService] 使用的 System Prompt: {systemPrompt}");

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1000,
                stream = true
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] [DeepSeekService] 开始发送 API 请求");

            // 使用共享的 s_sharedClient 实例
            HttpResponseMessage response = await s_sharedClient.PostAsync(DeepSeekApiUrl, content);

            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] [DeepSeekService] 收到响应头，状态码: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (StreamReader reader = new StreamReader(stream))
                {
                    Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] [DeepSeekService] 开始读取流式数据");
                    while (!reader.EndOfStream)
                    {
                        string line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

                        string jsonData = line.Substring(6);
                        if (jsonData.Trim() == "[DONE]")
                        {
                            Debug.Log("[DeepSeekService] 流式输出完成");
                            break;
                        }

                        try
                        {
                            var chunk = JsonConvert.DeserializeObject<StreamChunk>(jsonData);
                            if (chunk?.choices != null && chunk.choices.Length > 0)
                            {
                                string deltaContent = chunk.choices[0].delta?.content;
                                if (!string.IsNullOrEmpty(deltaContent))
                                {
                                    // 通过回调将数据块传递出去
                                    onChunkReceived?.Invoke(deltaContent);
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            Debug.LogWarning($"[DeepSeekService] 解析失败: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Debug.LogError($"[DeepSeekService] API 请求失败: {response.StatusCode}, 错误: {errorBody}");
                onError?.Invoke($"抱歉，AI 服务暂时不可用 ({response.StatusCode})。");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DeepSeekService] 调用 API 异常: {ex.Message}");
            onError?.Invoke("抱歉，发生了网络错误。");
        }
        finally
        {
            // 标记流式输出结束
            onStreamEnd?.Invoke();
        }
    }
}