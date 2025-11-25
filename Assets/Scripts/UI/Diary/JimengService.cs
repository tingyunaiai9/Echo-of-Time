/* UI/Diary/JimengService.cs
 * 即梦 AI v3.1 图像生成服务
 * 独立负责处理火山引擎即梦图像生成 API，包括提交任务与轮询结果
 * 内部集成 SigV4 鉴权签名逻辑，并通过回调返回图像 URL 或错误信息
 */
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AI.DTOs; // 引入 DTO
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic; // 用于 Dictionary
using System.Linq; // 用于 OrderBy

/*
 * 即梦 AI 服务静态类
 * 对外提供 GenerateImage 接口，封装任务提交与结果查询流程
 */
public static class JimengService
{
    // API 配置
    private const string Host = "visual.volcengineapi.com";
    private const string SubmitTaskUrl = "https://visual.volcengineapi.com?Action=CVSync2AsyncSubmitTask&Version=2022-08-31";
    private const string QueryTaskUrl = "https://visual.volcengineapi.com?Action=CVSync2AsyncGetResult&Version=2022-08-31";
    private const string ReqKey = "jimeng_t2i_v31"; // v3.1 的固定值

    // !! 鉴权密钥 (!!不要硬编码, 应从安全位置读取!!)
    // private const string VolcAccessKey = "YOUR_ACCESS_KEY"; 
    // private const string VolcSecretKey = "YOUR_SECRET_KEY"; 
    private const string VolcRegion = "cn-north-1";
    private const string VolcService = "cv";

    private static readonly HttpClient s_sharedClient = new HttpClient();
    
    // 轮询配置
    private const int PollIntervalMs = 2000; // 轮询间隔 (2秒)
    private const int PollTimeoutSeconds = 60; // 总超时 (60秒)

    /// <summary>
    /// (公共) 调用 即梦 AI v3.1 API 生成图像 (包含提交 + 轮询)
    /// </summary>
    public static async Task GenerateImage(
        string prompt,
        Action<string> onSuccess,
        Action<string> onError)
    {
        // (此方法与上一版回复完全相同，它调用下面的内部方法)
        try
        {
            // 1. 提交任务
            string taskId = await SubmitTaskInternal(prompt);
            if (string.IsNullOrEmpty(taskId))
            {
                onError?.Invoke("任务提交失败。");
                return;
            }

            Debug.Log($"[JimengService] 任务提交成功, Task ID: {taskId}. 开始轮询...");

            // 2. 轮询结果
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(PollTimeoutSeconds);

            while (DateTime.UtcNow - startTime < timeout)
            {
                var queryResponse = await QueryTaskInternal(taskId);

                if (queryResponse == null)
                {
                    onError?.Invoke("查询失败: 未知错误");
                    return;
                }
                
                if (queryResponse.code != 10000) // 10000 = Success
                {
                    // 检查是否是 'ResponseMetadata' 风格的错误
                    if (queryResponse.ResponseMetadata?.Error != null)
                    {
                        onError?.Invoke($"查询失败: {queryResponse.ResponseMetadata.Error.Message} (Code: {queryResponse.ResponseMetadata.Error.Code})");
                    }
                    else
                    {
                        onError?.Invoke($"查询失败: {queryResponse.message ?? "未知错误"}");
                    }
                    return;
                }

                string status = queryResponse.data?.status;
                Debug.Log($"[JimengService] 轮询状态: {status}");

                switch (status)
                {
                    case "done":
                        if (queryResponse.data.image_urls != null && queryResponse.data.image_urls.Count > 0)
                        {
                            onSuccess?.Invoke(queryResponse.data.image_urls[0]);
                            return; // 成功！
                        }
                        else
                        {
                            onError?.Invoke("任务完成，但未返回图像 URL。");
                            return;
                        }
                    case "in_queue":
                    case "generating":
                        // 任务进行中，等待后继续轮询
                        await Task.Delay(PollIntervalMs);
                        break;
                    case "not_found":
                    case "expired":
                        onError?.Invoke($"任务失败: 状态 {status}");
                        return;
                    default:
                        // 包含审核失败等情况 (如 50511, 50412)
                        onError?.Invoke($"任务失败: {queryResponse.message} (Status: {status})");
                        return;
                }
            }
            
            // 循环结束 = 超时
            onError?.Invoke("图像生成超时。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JimengService] 调用 API 异常: {ex.Message}");
            onError?.Invoke("抱歉，发生了网络错误。");
        }
    }

    /// <summary>
    /// (私有) 步骤 1: 提交任务
    /// </summary>
    private static async Task<string> SubmitTaskInternal(string prompt)
    {
        var requestBody = new JimengSubmitBody
        {
            req_key = ReqKey,
            prompt = prompt,
            use_pre_llm = true,
            seed = -1,
            width = 1328,
            height = 1328
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);
        
        // (调用包含签名的完整请求)
        var request = await CreateSignedRequest(SubmitTaskUrl, jsonBody);
        if (request == null) return null; // 签名失败

        HttpResponseMessage response = await s_sharedClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"[JimengService.Submit] API 请求失败: {response.StatusCode}, 错误: {responseBody}");
            return null;
        }

        var submitResponse = JsonConvert.DeserializeObject<JimengSubmitResponse>(responseBody);
        if (submitResponse.code == 10000)
        {
            return submitResponse.data?.task_id;
        }
        else
        {
            Debug.LogError($"[JimengService.Submit] API 返回业务错误: {submitResponse.message}");
            return null;
        }
    }

    /// <summary>
    /// (私有) 步骤 2: 查询任务
    /// </summary>
    private static async Task<JimengQueryResponse> QueryTaskInternal(string taskId)
    {
        // 构造 req_json (JSON-in-a-string)
        var reqJson = new JimengQueryReqJson
        {
            return_url = true,
            logo_info = new LogoInfo { add_logo = false },
            aigc_meta = new AIGCMeta { producer_id = "echo_of_time_game" }
        };
        string reqJsonString = JsonConvert.SerializeObject(reqJson);

        var requestBody = new JimengQueryBody
        {
            req_key = ReqKey,
            task_id = taskId,
            req_json = reqJsonString
        };
        string jsonBody = JsonConvert.SerializeObject(requestBody);

        // (调用包含签名的完整请求)
        var request = await CreateSignedRequest(QueryTaskUrl, jsonBody);
        if (request == null) return null; // 签名失败

        HttpResponseMessage response = await s_sharedClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"[JimengService.Query] API 请求失败: {response.StatusCode}, 错误: {responseBody}");
            return null;
        }

        // 单次反序列化。JsonConvert 会自动填充 data (如果code=10000) 
        // 或 ResponseMetadata (如果code!=10000)
        return JsonConvert.DeserializeObject<JimengQueryResponse>(responseBody);
    }

    /// <summary>
    /// (私有) 核心鉴权：创建已签名的 HttpRequestMessage
    /// </summary>
    private static async Task<HttpRequestMessage> CreateSignedRequest(string url, string jsonBody)
    {
        try
        {
            // 按文档规则：显式构造带 Query 的 URL，避免解析误差
            var baseUri = new Uri(url);

            // 解析原始 URL 中的 Action / Version
            var originalQuery = baseUri.Query.TrimStart('?');
            var queryPairs = originalQuery.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(q => q.Split('='))
                                          .Where(p => p.Length == 2)
                                          .ToDictionary(p => p[0], p => p[1]);

            // 规范化 Query：排序 + RFC3986 编码
            var sortedQuery = new SortedDictionary<string, string>();
            foreach (var kv in queryPairs)
            {
                sortedQuery[kv.Key] = kv.Value;
            }

            string canonicalQuery = string.Join("&", sortedQuery.Select(kv =>
                $"{VolcSigner.Rfc3986Encode(kv.Key)}={VolcSigner.Rfc3986Encode(kv.Value)}"));

            // 用 canonicalQuery 作为真实请求的 query，保证与签名一致
            var fullUrl = $"https://{Host}{baseUri.AbsolutePath}?{canonicalQuery}";
            var requestUri = new Uri(fullUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // --- 步骤 1 & 2: 准备基础数据 ---
            DateTime utcNow = DateTime.UtcNow;
            string xDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
            string shortDate = utcNow.ToString("yyyyMMdd");
            string payloadHash = VolcSigner.HashSHA256(jsonBody);

            // 只将 host 与 x-date 参与签名（与官方示例一致）
            var headersToSign = new SortedDictionary<string, string>
            {
                { "host", Host },
                { "x-date", xDate }
            };

            string canonicalHeaders = string.Join("\n", headersToSign.Select(kv =>
                $"{kv.Key}:{kv.Value.Trim()}")) + "\n";
            string signedHeaders = string.Join(";", headersToSign.Keys);

            // --- 步骤 2: 创建规范请求 (CanonicalRequest) ---
            string canonicalRequest =
                "POST" + "\n" +             // HTTPRequestMethod
                baseUri.AbsolutePath + "\n" + // CanonicalURI (通常为 "/")
                canonicalQuery + "\n" +       // CanonicalQueryString
                canonicalHeaders + "\n" +     // CanonicalHeaders
                signedHeaders + "\n" +        // SignedHeaders
                payloadHash;                    // HexEncode(Hash(RequestPayload))

            // --- 步骤 3: 创建待签名字符串 (StringToSign) ---
            string credentialScope = $"{shortDate}/{VolcRegion}/{VolcService}/request";
            string stringToSign =
                "HMAC-SHA256" + "\n" +
                xDate + "\n" +
                credentialScope + "\n" +
                VolcSigner.HashSHA256(canonicalRequest);

            // --- 步骤 4: 派生签名密钥 (kSigning) ---
            byte[] kSigning = VolcSigner.GenSigningSecretKeyV4(ApiKeyManager.VolcSecretKey, shortDate, VolcRegion, VolcService);

            // --- 步骤 5: 计算签名 (Signature) ---
            byte[] signatureBytes = VolcSigner.HmacSHA256(kSigning, stringToSign);
            string signature = VolcSigner.ToHexString(signatureBytes);

            // --- 步骤 6: 构建 Authorization 标头 ---
            string authorization = $"HMAC-SHA256 Credential={ApiKeyManager.VolcAccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";

            // --- 7. 将所有标头添加到实际请求中 ---
            request.Headers.Host = Host;
            request.Headers.Add("X-Date", xDate);
            // 使用 TryAddWithoutValidation 避免在部分平台（如 macOS）上因格式校验过严导致的异常
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
            // Content-Type 由 HttpContent 设置

            return request;
        }
        catch (Exception authEx)
        {
            Debug.LogError($"[JimengService] 签名失败: {authEx.Message}");
            return null;
        }
    }
}