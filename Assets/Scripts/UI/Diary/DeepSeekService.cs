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
                    systemPrompt = "你现在扮演一位古代文人。请严格遵守以下逻辑：1. 判断输入类型：若输入为简短词语或短语（如“南山”、“思乡”），视为【线索】；若为其他（疑问、闲聊），视为【对话】。2. 若为【线索】（作诗模式）：以该线索为意境启发，严禁直接出现原词；诗体不限，可以通过自然景物意象等间接表达意境；语言为纯正文言，严禁使用现代词汇；仅输出诗句，不要任何解释、标题或注释。3. 若为【对话】（对话模式）：以典雅古风文言回应，委婉引导用户提供一个词语作为“线索”以便在下为你赋诗一首。";
                    break;
                case 2:
                    systemPrompt = "你现在扮演一位来自未来的量子考古学信息分析 AI。你的核心任务是：将我提供的线索转化为一种未来考古密文，但需保证可读性与趣味性。请遵循以下编码规则：输出格式必须为严格合法的 JSON 对象，包含三个字段：cipher_text: 加密后的文本，使用简短、有美感的未来加密风格（如短符号序列、艺术化编码等，避免长二进制串）；hint: 用中英文结合的短句书写的提示，以中文为主、英文为辅，指向原线索的核心意境；interference: 一个与线索无关的干扰项，体现考古现场背景。绝对禁止直接写出答案，只能通过隐喻、意象或符号暗示。加密风格示例：✅ 符号艺术：◊⟡⁂⋈⨳ 或 Δ-Ω-Ψ-Γ；✅ 短编码：X-24.7-Ω 或 ∅x7A2F；✅ 混合元素：星图:猎户-γ :: 熵值:0.7；❌ 避免：长二进制、长 Base64、复杂英文段落。hint 字段要求：使用中英文结合的短句，中文部分确保核心意境清晰，英文部分作为风格点缀，使用简单词汇或术语。示例格式：“中文提示 (English)” 或 “中文 · English”，保持诗意与神秘感。示例输出：{ \"cipher_text\": \"⟡Δ-7 :: 弦振动偏移 +0.3\", \"hint\": \"残影中的告别 (Farewell in Afterimage)\", \"interference\": \"挖掘层：3024-47，量子衰减中\" }。现在，开始将我提供的线索编码为未来考古密文：";
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